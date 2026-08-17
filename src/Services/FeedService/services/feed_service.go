package services

import (
	"context"
	"fmt"
	"log"
	"sync"

	"feed-service/clients"
	"feed-service/models"
)

// Верхняя граница на размер trending-пула, который мы вытягиваем за один раз,
// чтобы эмулировать пагинацию поверх content-service, у которого GetTrending
// сам по себе не принимает offset. Не имеет смысла тянуть больше этого за раз.
const maxTrendingPoolSize = 200

type FeedService struct {
	userClient    *clients.UserClient
	contentClient *clients.ContentClient
	socialClient  *clients.SocialClient
	searchClient  *clients.SearchClient
	batchSize     int
}

func NewFeedService(
	userClient *clients.UserClient,
	contentClient *clients.ContentClient,
	socialClient *clients.SocialClient,
	searchClient *clients.SearchClient,
	batchSize int,
) *FeedService {
	return &FeedService{
		userClient:    userClient,
		contentClient: contentClient,
		socialClient:  socialClient,
		searchClient:  searchClient,
		batchSize:     batchSize,
	}
}

func (s *FeedService) GetFeed(ctx context.Context, userID string, offset, limit int, seen []string) (*models.FeedResponse, error) {
	log.Printf("Building feed for user %s, offset=%d, limit=%d", userID, offset, limit)

	if limit == 0 {
		limit = s.batchSize
	}

	seenMap := make(map[string]bool, len(seen))
	for _, id := range seen {
		seenMap[id] = true
	}

	// Get user's interests and subscriptions in parallel
	var interests []models.Interest
	var subscriptions []string
	var errInterest, errSub error

	var wg sync.WaitGroup
	wg.Add(2)

	go func() {
		defer wg.Done()
		interests, errInterest = s.socialClient.GetUserInterests(userID)
		if errInterest != nil {
			log.Printf("Failed to get interests for user %s: %v", userID, errInterest)
			interests = []models.Interest{}
		}
	}()

	go func() {
		defer wg.Done()
		subscriptions, errSub = s.userClient.GetSubscriptions(userID)
		if errSub != nil {
			log.Printf("Failed to get subscriptions for user %s: %v", userID, errSub)
			subscriptions = []string{}
		}
	}()

	wg.Wait()

	// Get recommendations from Search (based on interests)
	var recommendedIDs []string
	if len(interests) > 0 {
		var err error
		recommendedIDs, err = s.searchClient.GetRecommendations(interests, 60)
		if err != nil {
			log.Printf("Failed to get recommendations: %v", err)
		}
	}

	// Get videos from subscriptions
	var subscriptionVideoIDs []string
	if len(subscriptions) > 0 {
		videos, err := s.contentClient.GetVideoBatch(subscriptions)
		if err != nil {
			log.Printf("Failed to get subscription videos: %v", err)
		} else {
			for _, v := range videos {
				subscriptionVideoIDs = append(subscriptionVideoIDs, v.ID)
			}
		}
	}

	// Merge and deduplicate, applying the seen-filter up front so it's never skipped
	// by a later fallback branch.
	allIDs := make(map[string]bool)
	var filteredIDs []string

	appendIfNew := func(id string) {
		if allIDs[id] || seenMap[id] {
			return
		}
		allIDs[id] = true
		filteredIDs = append(filteredIDs, id)
	}

	for _, id := range recommendedIDs { // recommendations first (prioritized)
		appendIfNew(id)
	}
	for _, id := range subscriptionVideoIDs {
		appendIfNew(id)
	}

	// If there's no personalized signal at all for this page range, go straight to the
	// trending path — this is the common case for brand-new users and must NOT report
	// hasMore/nextOffset based on an unrelated (empty) filteredIDs slice.
	if len(filteredIDs) <= offset {
		return s.buildTrendingResponse(ctx, offset, limit, seenMap)
	}

	// Apply pagination over the personalized set
	start := offset
	end := offset + limit
	if end > len(filteredIDs) {
		end = len(filteredIDs)
	}
	pagedIDs := filteredIDs[start:end]

	videos, err := s.contentClient.GetVideoBatch(pagedIDs)
	if err != nil || len(videos) == 0 {
		if err != nil {
			log.Printf("Failed to get video metadata, falling back to trending: %v", err)
		}
		// Fall back to a properly paginated trending response instead of returning
		// trending videos alongside pagination fields computed from filteredIDs.
		return s.buildTrendingResponse(ctx, offset, limit, seenMap)
	}

	enrichedVideos := s.enrichWithAuthors(videos)

	hasMore := end < len(filteredIDs)

	return &models.FeedResponse{
		Videos:     enrichedVideos,
		NextOffset: end,
		HasMore:    hasMore,
		TotalCount: len(filteredIDs),
	}, nil
}

// buildTrendingResponse emulates pagination over content-service's trending endpoint,
// which itself only accepts a limit (no offset). We pull a bounded pool starting from 0,
// slice it locally to the requested window, filter out already-seen videos, and derive
// hasMore/nextOffset from that same pool — so the pagination contract stays honest even
// when nothing personalized is available.
func (s *FeedService) buildTrendingResponse(ctx context.Context, offset, limit int, seenMap map[string]bool) (*models.FeedResponse, error) {
	poolSize := offset + limit + 1 // +1 so we can tell whether there's a next page
	if poolSize > maxTrendingPoolSize {
		poolSize = maxTrendingPoolSize
	}

	pool, err := s.contentClient.GetTrending(poolSize)
	if err != nil {
		return nil, fmt.Errorf("failed to get trending videos: %w", err)
	}

	filtered := make([]models.Video, 0, len(pool))
	for _, v := range pool {
		if !seenMap[v.ID] {
			filtered = append(filtered, v)
		}
	}

	start := offset
	if start > len(filtered) {
		start = len(filtered)
	}
	end := offset + limit
	if end > len(filtered) {
		end = len(filtered)
	}

	page := filtered[start:end]
	enrichedVideos := s.enrichWithAuthors(page)

	return &models.FeedResponse{
		Videos:     enrichedVideos,
		NextOffset: end,
		HasMore:    end < len(filtered) && len(pool) >= poolSize, // more only if the pool wasn't already exhausted
		TotalCount: len(filtered),
	}, nil
}

// enrichWithAuthors fetches author metadata for each unique video author concurrently
// instead of sequentially — a feed page with N distinct authors previously meant N
// blocking round-trips to user-service in series.
func (s *FeedService) enrichWithAuthors(videos []models.Video) []models.Video {
	uniqueAuthorIDs := make(map[string]struct{})
	for _, v := range videos {
		uniqueAuthorIDs[v.UserID] = struct{}{}
	}

	userMap := make(map[string]*models.User, len(uniqueAuthorIDs))
	var mu sync.Mutex
	var wg sync.WaitGroup

	for authorID := range uniqueAuthorIDs {
		authorID := authorID
		wg.Add(1)
		go func() {
			defer wg.Done()
			user, err := s.userClient.GetUser(authorID)
			if err != nil || user == nil {
				return
			}
			mu.Lock()
			userMap[authorID] = user
			mu.Unlock()
		}()
	}
	wg.Wait()

	enriched := make([]models.Video, len(videos))
	for i, v := range videos {
		enriched[i] = v
		if user, ok := userMap[v.UserID]; ok {
			enriched[i].User = user
		}
	}
	return enriched
}

func (s *FeedService) GetTrending(ctx context.Context, limit int) ([]models.Video, error) {
	return s.contentClient.GetTrending(limit)
}
