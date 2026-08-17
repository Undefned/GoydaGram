package controllers

import (
	"context"
	"net/http"
	"os"
	"strconv"
	"strings"
	"time"

	"github.com/gin-gonic/gin"

	"feed-service/services"
	"feed-service/utils"
)

type FeedController struct {
	feedService     *services.FeedService
	prefetchService *services.PrefetchService
}

func NewFeedController(feedService *services.FeedService, prefetchService *services.PrefetchService) *FeedController {
	return &FeedController{
		feedService:     feedService,
		prefetchService: prefetchService,
	}
}

// resolveUserID extracts the authenticated user ID injected by the JWT middleware.
// The ?user_id= query fallback is a local-dev convenience ONLY — it never runs outside
// ENV=development, so it can't be used to read another user's feed in staging/prod.
func resolveUserID(ctx *gin.Context) string {
	if userID := ctx.GetString("user_id"); userID != "" {
		return userID
	}

	if os.Getenv("ENV") == "development" {
		return ctx.Query("user_id")
	}

	return ""
}

// GetFeed godoc
// @Summary Get user feed
// @Description Get personalized feed for a user with pagination
// @Tags feed
// @Accept json
// @Produce json
// @Param offset query int false "Pagination offset" default(0)
// @Param limit query int false "Items per page" default(30) maximum(50)
// @Param seen query string false "Comma-separated list of seen video IDs"
// @Param prefetch query bool false "Start prefetch in background"
// @Success 200 {object} map[string]interface{} "Feed items with pagination"
// @Failure 401 {object} map[string]interface{} "Unauthorized - User ID required"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /feed [get]
func (c *FeedController) GetFeed(ctx *gin.Context) {
	userID := resolveUserID(ctx)
	if userID == "" {
		utils.Error(ctx, http.StatusUnauthorized, "User ID required")
		return
	}

	// Parse query params
	offset, _ := strconv.Atoi(ctx.DefaultQuery("offset", "0"))
	limit, _ := strconv.Atoi(ctx.DefaultQuery("limit", "30"))
	if limit > 50 {
		limit = 50
	}

	seenParam := ctx.Query("seen")
	var seen []string
	if seenParam != "" {
		seen = strings.Split(seenParam, ",")
	}

	// Check if this is a prefetch request
	if prefetch := ctx.Query("prefetch"); prefetch == "true" {
		// Fire-and-forget prefetch must NOT reuse ctx.Request.Context() — that context
		// gets cancelled as soon as we write the response below, which would kill every
		// downstream call the background goroutine makes. Use a detached context with
		// its own timeout instead.
		bgCtx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
		go func() {
			defer cancel()
			_, _ = c.feedService.GetFeed(bgCtx, userID, offset, limit, seen)
		}()
		utils.Success(ctx, gin.H{"message": "Prefetch started"})
		return
	}

	// Get feed
	result, err := c.feedService.GetFeed(ctx.Request.Context(), userID, offset, limit, seen)
	if err != nil {
		utils.Error(ctx, http.StatusInternalServerError, "Failed to build feed: "+err.Error())
		return
	}

	utils.Success(ctx, result)
}

// GetTrending godoc
// @Summary Get trending videos
// @Description Get list of trending videos globally
// @Tags feed
// @Accept json
// @Produce json
// @Param limit query int false "Number of videos" default(30) maximum(50)
// @Success 200 {object} map[string]interface{} "List of trending videos"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /feed/trending [get]
func (c *FeedController) GetTrending(ctx *gin.Context) {
	limit, _ := strconv.Atoi(ctx.DefaultQuery("limit", "30"))
	if limit > 50 {
		limit = 50
	}

	videos, err := c.feedService.GetTrending(ctx.Request.Context(), limit)
	if err != nil {
		utils.Error(ctx, http.StatusInternalServerError, "Failed to get trending: "+err.Error())
		return
	}

	utils.Success(ctx, gin.H{"videos": videos})
}

// Prefetch godoc
// @Summary Prefetch feed for a user
// @Description Prefetch and cache feed for a specific user
// @Tags feed
// @Accept json
// @Produce json
// @Param offset query int false "Pagination offset" default(0)
// @Param seen query string false "Comma-separated list of seen video IDs"
// @Success 200 {object} map[string]interface{} "Prefetched feed data"
// @Failure 401 {object} map[string]interface{} "Unauthorized - User ID required"
// @Failure 408 {object} map[string]interface{} "Request timeout"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /feed/prefetch [get]
func (c *FeedController) Prefetch(ctx *gin.Context) {
	userID := resolveUserID(ctx)
	if userID == "" {
		utils.Error(ctx, http.StatusUnauthorized, "User ID required")
		return
	}

	offset, _ := strconv.Atoi(ctx.DefaultQuery("offset", "0"))
	seenParam := ctx.Query("seen")
	var seen []string
	if seenParam != "" {
		seen = strings.Split(seenParam, ",")
	}

	// Start prefetch and return future result
	resultCh := c.prefetchService.Prefetch(userID, offset, 10, seen)

	// Wait for result with timeout
	select {
	case result := <-resultCh:
		if result == nil {
			utils.Error(ctx, http.StatusInternalServerError, "Prefetch failed")
			return
		}
		utils.Success(ctx, result)
	case <-ctx.Request.Context().Done():
		utils.Error(ctx, http.StatusRequestTimeout, "Prefetch timeout")
	}
}
