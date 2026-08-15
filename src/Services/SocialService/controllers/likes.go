package controllers

import (
	"net/http"
	"strconv"

	"github.com/gin-gonic/gin"

	"social-service/models"
	"social-service/services"
	"social-service/utils"
)

type LikesController struct {
	service *services.LikeService
}

func NewLikesController(service *services.LikeService) *LikesController {
	return &LikesController{service: service}
}

// Like godoc
// @Summary Like a video
// @Description Add a like to a video
// @Tags likes
// @Accept json
// @Produce json
// @Param request body models.LikeRequest true "Like request"
// @Success 200 {object} map[string]interface{} "Like added successfully"
// @Failure 400 {object} map[string]interface{} "Invalid request"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /likes [post]
func (c *LikesController) Like(ctx *gin.Context) {
	var req models.LikeRequest
	if err := ctx.ShouldBindJSON(&req); err != nil {
		utils.Error(ctx, http.StatusBadRequest, "Invalid request: "+err.Error())
		return
	}

	if err := c.service.Like(ctx.Request.Context(), req.VideoID, req.UserID); err != nil {
		utils.Error(ctx, http.StatusInternalServerError, "Failed to like: "+err.Error())
		return
	}

	utils.Success(ctx, gin.H{"message": "Liked successfully"})
}

// Unlike godoc
// @Summary Unlike a video
// @Description Remove a like from a video
// @Tags likes
// @Accept json
// @Produce json
// @Param request body models.LikeRequest true "Unlike request"
// @Success 200 {object} map[string]interface{} "Unliked successfully"
// @Failure 400 {object} map[string]interface{} "Invalid request"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /likes [delete]
func (c *LikesController) Unlike(ctx *gin.Context) {
	var req models.LikeRequest
	if err := ctx.ShouldBindJSON(&req); err != nil {
		utils.Error(ctx, http.StatusBadRequest, "Invalid request: "+err.Error())
		return
	}

	if err := c.service.Unlike(ctx.Request.Context(), req.VideoID, req.UserID); err != nil {
		utils.Error(ctx, http.StatusInternalServerError, "Failed to unlike: "+err.Error())
		return
	}

	utils.Success(ctx, gin.H{"message": "Unliked successfully"})
}

// GetVideoLikes godoc
// @Summary Get likes for a video
// @Description Get paginated list of likes for a specific video
// @Tags likes
// @Produce json
// @Param videoId path string true "Video ID"
// @Param limit query int false "Limit" default(20)
// @Param offset query int false "Offset" default(0)
// @Success 200 {object} map[string]interface{} "List of likes"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /videos/{videoId}/likes [get]
func (c *LikesController) GetVideoLikes(ctx *gin.Context) {
	videoID := ctx.Param("videoId")
	limit, _ := strconv.ParseInt(ctx.DefaultQuery("limit", "20"), 10, 64)
	offset, _ := strconv.ParseInt(ctx.DefaultQuery("offset", "0"), 10, 64)

	likes, total, err := c.service.GetVideoLikes(ctx.Request.Context(), videoID, limit, offset)
	if err != nil {
		utils.Error(ctx, http.StatusInternalServerError, "Failed to get likes: "+err.Error())
		return
	}

	utils.Success(ctx, gin.H{
		"data":   likes,
		"total":  total,
		"limit":  limit,
		"offset": offset,
	})
}

// GetVideoLikesCount godoc
// @Summary Get likes count for a video
// @Description Get total number of likes for a specific video
// @Tags likes
// @Produce json
// @Param videoId path string true "Video ID"
// @Success 200 {object} map[string]interface{} "Likes count"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /videos/{videoId}/likes/count [get]
func (c *LikesController) GetVideoLikesCount(ctx *gin.Context) {
	videoID := ctx.Param("videoId")

	count, err := c.service.GetVideoLikesCount(ctx.Request.Context(), videoID)
	if err != nil {
		utils.Error(ctx, http.StatusInternalServerError, "Failed to get likes count: "+err.Error())
		return
	}

	utils.Success(ctx, gin.H{"video_id": videoID, "likes": count})
}
