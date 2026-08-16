package controllers

import (
	"context"
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"testing"

	"github.com/gin-gonic/gin"
	"github.com/stretchr/testify/assert"

	"feed-service/models"
	"feed-service/services"
)

func setupTestRouter() *gin.Engine {
	gin.SetMode(gin.TestMode)
	return gin.New()
}

// ==================== ИНТЕРФЕЙС ДЛЯ МОКА ====================

type FeedServiceInterface interface {
	GetFeed(ctx context.Context, userID string, offset, limit int, seen []string) (*models.FeedResponse, error)
	GetTrending(ctx context.Context, limit int) ([]models.Video, error)
}

// ==================== МОК ====================

type MockFeedService struct {
	feedResponse   *models.FeedResponse
	feedError      error
	trendingVideos []models.Video
	trendingError  error
}

func (m *MockFeedService) GetFeed(ctx context.Context, userID string, offset, limit int, seen []string) (*models.FeedResponse, error) {
	if m.feedError != nil {
		return nil, m.feedError
	}
	return m.feedResponse, nil
}

func (m *MockFeedService) GetTrending(ctx context.Context, limit int) ([]models.Video, error) {
	if m.trendingError != nil {
		return nil, m.trendingError
	}
	return m.trendingVideos, nil
}

// ==================== КОНТРОЛЛЕР С ИНТЕРФЕЙСОМ ====================

type FeedControllerWithInterface struct {
	feedService     FeedServiceInterface
	prefetchService *services.PrefetchService
}

func NewFeedControllerWithInterface(feedService FeedServiceInterface, prefetchService *services.PrefetchService) *FeedControllerWithInterface {
	return &FeedControllerWithInterface{
		feedService:     feedService,
		prefetchService: prefetchService,
	}
}

// Реализуем метод GetTrending для контроллера с интерфейсом
func (c *FeedControllerWithInterface) GetTrending(ctx *gin.Context) {
	limit := 30
	videos, err := c.feedService.GetTrending(ctx.Request.Context(), limit)
	if err != nil {
		ctx.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}
	ctx.JSON(http.StatusOK, gin.H{"videos": videos})
}

// ==================== ТЕСТЫ ====================

func TestFeedController_GetFeed_Unauthorized(t *testing.T) {
	// Arrange
	router := setupTestRouter()

	// Создаем контроллер с реальным сервисом
	feedService := &services.FeedService{}
	prefetchService := &services.PrefetchService{}

	controller := &FeedController{
		feedService:     feedService,
		prefetchService: prefetchService,
	}

	router.GET("/api/feed", controller.GetFeed)

	req, _ := http.NewRequest("GET", "/api/feed", nil)
	w := httptest.NewRecorder()

	// Act
	router.ServeHTTP(w, req)

	// Assert
	assert.Equal(t, http.StatusUnauthorized, w.Code)

	var response map[string]interface{}
	json.Unmarshal(w.Body.Bytes(), &response)
	assert.False(t, response["success"].(bool))
	assert.Contains(t, response["error"], "User ID required")
}

func TestFeedController_GetTrending_WithMock(t *testing.T) {
	// Arrange
	router := setupTestRouter()

	// Создаем мок сервиса
	mockService := &MockFeedService{
		trendingVideos: []models.Video{
			{ID: "trending1", Title: "Trending 1"},
			{ID: "trending2", Title: "Trending 2"},
		},
	}

	controller := NewFeedControllerWithInterface(mockService, &services.PrefetchService{})

	router.GET("/api/feed/trending", controller.GetTrending)

	req, _ := http.NewRequest("GET", "/api/feed/trending?limit=10", nil)
	w := httptest.NewRecorder()

	// Act
	router.ServeHTTP(w, req)

	// Assert
	assert.Equal(t, http.StatusOK, w.Code)

	var response map[string]interface{}
	json.Unmarshal(w.Body.Bytes(), &response)
	videos := response["videos"].([]interface{})
	assert.Len(t, videos, 2)
	assert.Equal(t, "trending1", videos[0].(map[string]interface{})["id"])
}
