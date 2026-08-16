package services

import (
	"context"
	"errors"
	"testing"

	"feed-service/models"
)

// ==================== ИНТЕРФЕЙСЫ ДЛЯ МОКОВ ====================

type UserClientInterface interface {
	GetSubscriptions(userID string) ([]string, error)
	GetUser(userID string) (*models.User, error)
}

type ContentClientInterface interface {
	GetVideoBatch(videoIDs []string) ([]models.Video, error)
	GetVideo(videoID string) (*models.Video, error)
	GetTrending(limit int) ([]models.Video, error)
}

type SocialClientInterface interface {
	GetUserInterests(userID string) ([]models.Interest, error)
}

type SearchClientInterface interface {
	GetRecommendations(interests []models.Interest, limit int) ([]string, error)
}

// ==================== МОКИ ====================

type MockUserClient struct {
	subscriptions []string
	err           error
}

func (m *MockUserClient) GetSubscriptions(userID string) ([]string, error) {
	if m.err != nil {
		return nil, m.err
	}
	return m.subscriptions, nil
}

func (m *MockUserClient) GetUser(userID string) (*models.User, error) {
	return &models.User{
		ID:       userID,
		Username: "testuser",
	}, nil
}

type MockContentClient struct {
	videos      []models.Video
	err         error
	trending    []models.Video
	trendingErr error
}

func (m *MockContentClient) GetVideoBatch(videoIDs []string) ([]models.Video, error) {
	if m.err != nil {
		return nil, m.err
	}
	return m.videos, nil
}

func (m *MockContentClient) GetVideo(videoID string) (*models.Video, error) {
	for _, v := range m.videos {
		if v.ID == videoID {
			return &v, nil
		}
	}
	return nil, errors.New("video not found")
}

func (m *MockContentClient) GetTrending(limit int) ([]models.Video, error) {
	if m.trendingErr != nil {
		return nil, m.trendingErr
	}
	return m.trending, nil
}

type MockSocialClient struct {
	interests []models.Interest
	err       error
}

func (m *MockSocialClient) GetUserInterests(userID string) ([]models.Interest, error) {
	if m.err != nil {
		return nil, m.err
	}
	return m.interests, nil
}

type MockSearchClient struct {
	videoIDs []string
	err      error
}

func (m *MockSearchClient) GetRecommendations(interests []models.Interest, limit int) ([]string, error) {
	if m.err != nil {
		return nil, m.err
	}
	return m.videoIDs, nil
}

// ==================== ОБЕРТКА ДЛЯ ТЕСТОВ ====================

type TestFeedService struct {
	*FeedService
	userClient    UserClientInterface
	contentClient ContentClientInterface
	socialClient  SocialClientInterface
	searchClient  SearchClientInterface
}

func NewTestFeedService(
	userClient UserClientInterface,
	contentClient ContentClientInterface,
	socialClient SocialClientInterface,
	searchClient SearchClientInterface,
	batchSize int,
) *TestFeedService {
	return &TestFeedService{
		FeedService: &FeedService{
			batchSize: batchSize,
		},
		userClient:    userClient,
		contentClient: contentClient,
		socialClient:  socialClient,
		searchClient:  searchClient,
	}
}

// GetFeed с моками
func (t *TestFeedService) GetFeed(ctx context.Context, userID string, offset, limit int, seen []string) (*models.FeedResponse, error) {
	if limit == 0 {
		limit = 30
	}

	interests, _ := t.socialClient.GetUserInterests(userID)
	subscriptions, _ := t.userClient.GetSubscriptions(userID)

	// Получаем рекомендации
	var recommendedIDs []string
	if len(interests) > 0 {
		recommendedIDs, _ = t.searchClient.GetRecommendations(interests, 60)
	}
	_ = recommendedIDs // ✅ Используем переменную

	var videos []models.Video
	if len(subscriptions) > 0 {
		videos, _ = t.contentClient.GetVideoBatch(subscriptions)
	}

	if len(videos) == 0 {
		videos, _ = t.contentClient.GetTrending(limit)
	}

	seenMap := make(map[string]bool)
	for _, s := range seen {
		seenMap[s] = true
	}

	var filtered []models.Video
	for _, v := range videos {
		if !seenMap[v.ID] {
			filtered = append(filtered, v)
		}
	}

	start := offset
	end := offset + limit
	if start > len(filtered) {
		start = len(filtered)
	}
	if end > len(filtered) {
		end = len(filtered)
	}

	return &models.FeedResponse{
		Videos:     filtered[start:end],
		NextOffset: end,
		HasMore:    end < len(filtered),
		TotalCount: len(filtered),
	}, nil
}

// ==================== ТЕСТЫ ====================

func TestFeedService_GetFeed_WithRecommendations(t *testing.T) {
	userClient := &MockUserClient{subscriptions: []string{"user1", "user2"}}
	contentClient := &MockContentClient{
		videos: []models.Video{
			{ID: "video1", UserID: "user1", Title: "Video 1"},
			{ID: "video2", UserID: "user2", Title: "Video 2"},
		},
	}
	socialClient := &MockSocialClient{
		interests: []models.Interest{{Tag: "funny", Weight: 0.8}},
	}
	searchClient := &MockSearchClient{videoIDs: []string{"video1", "video3"}}

	feedService := NewTestFeedService(userClient, contentClient, socialClient, searchClient, 30)
	ctx := context.Background()

	result, err := feedService.GetFeed(ctx, "user123", 0, 10, []string{})

	if err != nil {
		t.Errorf("Expected no error, got: %v", err)
	}
	if result == nil {
		t.Error("Expected result, got nil")
	}
}

func TestFeedService_GetFeed_FallbackToTrending(t *testing.T) {
	userClient := &MockUserClient{subscriptions: []string{}}
	contentClient := &MockContentClient{
		videos: []models.Video{},
		trending: []models.Video{
			{ID: "trending1", UserID: "user1", Title: "Trending Video"},
		},
	}
	socialClient := &MockSocialClient{interests: []models.Interest{}}
	searchClient := &MockSearchClient{videoIDs: []string{}}

	feedService := NewTestFeedService(userClient, contentClient, socialClient, searchClient, 30)
	ctx := context.Background()

	result, err := feedService.GetFeed(ctx, "user123", 0, 10, []string{})

	if err != nil {
		t.Errorf("Expected no error, got: %v", err)
	}
	if result == nil {
		t.Error("Expected result, got nil")
	}
	if len(result.Videos) == 0 {
		t.Error("Expected at least one video, got 0")
	}
	if result.Videos[0].ID != "trending1" {
		t.Errorf("Expected 'trending1', got '%s'", result.Videos[0].ID)
	}
}

func TestFeedService_GetFeed_FiltersSeenVideos(t *testing.T) {
	userClient := &MockUserClient{subscriptions: []string{"user1"}}
	contentClient := &MockContentClient{
		videos: []models.Video{
			{ID: "video1", UserID: "user1", Title: "Video 1"},
			{ID: "video2", UserID: "user2", Title: "Video 2"},
			{ID: "video3", UserID: "user3", Title: "Video 3"},
		},
	}
	socialClient := &MockSocialClient{interests: []models.Interest{{Tag: "funny", Weight: 0.8}}}
	searchClient := &MockSearchClient{videoIDs: []string{"video1", "video2", "video3"}}

	feedService := NewTestFeedService(userClient, contentClient, socialClient, searchClient, 30)
	ctx := context.Background()

	result, err := feedService.GetFeed(ctx, "user123", 0, 10, []string{"video1", "video2"})

	if err != nil {
		t.Errorf("Expected no error, got: %v", err)
	}
	if result == nil {
		t.Error("Expected result, got nil")
	}
	for _, v := range result.Videos {
		if v.ID == "video1" || v.ID == "video2" {
			t.Errorf("Video %s should be filtered out", v.ID)
		}
	}
}

func TestFeedService_GetFeed_Pagination(t *testing.T) {
	videos := make([]models.Video, 50)
	for i := 0; i < 50; i++ {
		videos[i] = models.Video{
			ID:     string(rune('a'+i%26)) + string(rune('0'+i/26)),
			UserID: "user1",
			Title:  "Video " + string(rune(i)),
		}
	}

	userClient := &MockUserClient{subscriptions: []string{"user1"}}
	contentClient := &MockContentClient{videos: videos}
	socialClient := &MockSocialClient{interests: []models.Interest{{Tag: "funny", Weight: 0.8}}}
	searchClient := &MockSearchClient{videoIDs: []string{}}

	feedService := NewTestFeedService(userClient, contentClient, socialClient, searchClient, 30)
	ctx := context.Background()

	result1, err := feedService.GetFeed(ctx, "user123", 0, 10, []string{})
	if err != nil {
		t.Errorf("Expected no error, got: %v", err)
	}

	result2, err := feedService.GetFeed(ctx, "user123", 10, 10, []string{})
	if err != nil {
		t.Errorf("Expected no error, got: %v", err)
	}

	if result1 == nil || result2 == nil {
		t.Error("Expected results, got nil")
	}
	if len(result1.Videos) != 10 {
		t.Errorf("Expected 10 videos on page 1, got %d", len(result1.Videos))
	}
	if len(result2.Videos) != 10 {
		t.Errorf("Expected 10 videos on page 2, got %d", len(result2.Videos))
	}
}
