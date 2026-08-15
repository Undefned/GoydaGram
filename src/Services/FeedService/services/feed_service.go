package services

import (
	"context"
	"fmt"
	"log"
	"sync"

	"feed-service/clients"
	"feed-service/models"
)

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

	// Merge and deduplicate
	allIDs := make(map[string]bool)
	var mergedIDs []string

	// Add recommended first (prioritize recommendations)
	for _, id := range recommendedIDs {
		if !allIDs[id] {
			allIDs[id] = true
			mergedIDs = append(mergedIDs, id)
		}
	}

	// Add subscription videos
	for _, id := range subscriptionVideoIDs {
		if !allIDs[id] {
			allIDs[id] = true
			mergedIDs = append(mergedIDs, id)
		}
	}

	// Filter out seen videos
	seenMap := make(map[string]bool)
	for _, s := range seen {
		seenMap[s] = true
	}

	var filteredIDs []string
	for _, id := range mergedIDs {
		if !seenMap[id] {
			filteredIDs = append(filteredIDs, id)
		}
	}

	// Apply pagination
	start := offset
	end := offset + limit
	if start > len(filteredIDs) {
		start = len(filteredIDs)
	}
	if end > len(filteredIDs) {
		end = len(filteredIDs)
	}

	pagedIDs := filteredIDs[start:end]

	// Get video metadata
	var videos []models.Video
	if len(pagedIDs) > 0 {
		var err error
		videos, err = s.contentClient.GetVideoBatch(pagedIDs)
		if err != nil {
			log.Printf("Failed to get video metadata: %v", err)
			// Fallback to trending
			trending, err := s.contentClient.GetTrending(limit)
			if err == nil {
				videos = trending
			}
		}
	}

	// If no videos, return trending
	if len(videos) == 0 {
		log.Printf("No videos found for user %s, falling back to trending", userID)
		trending, err := s.contentClient.GetTrending(limit)
		if err != nil {
			return nil, fmt.Errorf("failed to get trending videos: %w", err)
		}
		videos = trending
	}

	// Get user details for each video
	userMap := make(map[string]*models.User)
	for _, v := range videos {
		if _, ok := userMap[v.UserID]; !ok {
			user, err := s.userClient.GetUser(v.UserID)
			if err == nil && user != nil {
				userMap[v.UserID] = user
			}
		}
	}

	// Enrich videos with user data
	enrichedVideos := make([]models.Video, len(videos))
	for i, v := range videos {
		enrichedVideos[i] = v
		if user, ok := userMap[v.UserID]; ok {
			enrichedVideos[i].User = user
		}
	}

	hasMore := end < len(filteredIDs)
	nextOffset := end

	return &models.FeedResponse{
		Videos:     enrichedVideos,
		NextOffset: nextOffset,
		HasMore:    hasMore,
		TotalCount: len(filteredIDs),
	}, nil
}

func (s *FeedService) GetTrending(ctx context.Context, limit int) ([]models.Video, error) {
	return s.contentClient.GetTrending(limit)
}
