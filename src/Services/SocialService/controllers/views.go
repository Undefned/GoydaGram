package controllers

import (
	"net/http"
	"strconv"

	"github.com/gin-gonic/gin"

	"social-service/models"
	"social-service/services"
	"social-service/utils"
)

type ViewsController struct {
	service *services.ViewService
}

func NewViewsController(service *services.ViewService) *ViewsController {
	return &ViewsController{service: service}
}

// View godoc
// @Summary Record a video view
// @Description Record that a user viewed a video
// @Tags views
// @Accept json
// @Produce json
// @Param request body models.ViewRequest true "View request"
// @Success 200 {object} map[string]interface{} "View recorded successfully"
// @Failure 400 {object} map[string]interface{} "Invalid request"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /views [post]
func (c *ViewsController) View(ctx *gin.Context) {
	var req models.ViewRequest
	if err := ctx.ShouldBindJSON(&req); err != nil {
		utils.Error(ctx, http.StatusBadRequest, "Invalid request: "+err.Error())
		return
	}

	if err := c.service.View(ctx.Request.Context(), req.VideoID, req.UserID); err != nil {
		utils.Error(ctx, http.StatusInternalServerError, "Failed to record view: "+err.Error())
		return
	}

	utils.Success(ctx, gin.H{"message": "View recorded successfully"})
}

// GetVideoViews godoc
// @Summary Get views for a video
// @Description Get paginated list of views for a specific video
// @Tags views
// @Produce json
// @Param videoId path string true "Video ID"
// @Param limit query int false "Limit" default(20)
// @Param offset query int false "Offset" default(0)
// @Success 200 {object} map[string]interface{} "List of views"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /videos/{videoId}/views [get]
func (c *ViewsController) GetVideoViews(ctx *gin.Context) {
	videoID := ctx.Param("videoId")
	limit, _ := strconv.ParseInt(ctx.DefaultQuery("limit", "20"), 10, 64)
	offset, _ := strconv.ParseInt(ctx.DefaultQuery("offset", "0"), 10, 64)

	views, total, err := c.service.GetVideoViews(ctx.Request.Context(), videoID, limit, offset)
	if err != nil {
		utils.Error(ctx, http.StatusInternalServerError, "Failed to get views: "+err.Error())
		return
	}

	utils.Success(ctx, gin.H{
		"data":   views,
		"total":  total,
		"limit":  limit,
		"offset": offset,
	})
}

// GetVideoViewsCount godoc
// @Summary Get views count for a video
// @Description Get total number of views for a specific video
// @Tags views
// @Produce json
// @Param videoId path string true "Video ID"
// @Success 200 {object} map[string]interface{} "Views count"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /videos/{videoId}/views/count [get]
func (c *ViewsController) GetVideoViewsCount(ctx *gin.Context) {
	videoID := ctx.Param("videoId")

	count, err := c.service.GetVideoViewsCount(ctx.Request.Context(), videoID)
	if err != nil {
		utils.Error(ctx, http.StatusInternalServerError, "Failed to get views count: "+err.Error())
		return
	}

	utils.Success(ctx, gin.H{"video_id": videoID, "views": count})
}
