package controllers

import (
	"net/http"
	"strconv"

	"github.com/gin-gonic/gin"

	"social-service/models"
	"social-service/services"
	"social-service/utils"
)

type CommentsController struct {
	service *services.CommentService
}

func NewCommentsController(service *services.CommentService) *CommentsController {
	return &CommentsController{service: service}
}

// Comment godoc
// @Summary Add a comment
// @Description Add a comment to a video
// @Tags comments
// @Accept json
// @Produce json
// @Param request body models.CommentRequest true "Comment request"
// @Success 200 {object} map[string]interface{} "Comment added successfully"
// @Failure 400 {object} map[string]interface{} "Invalid request"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /comments [post]
func (c *CommentsController) Comment(ctx *gin.Context) {
	var req models.CommentRequest
	if err := ctx.ShouldBindJSON(&req); err != nil {
		utils.Error(ctx, http.StatusBadRequest, "Invalid request: "+err.Error())
		return
	}

	id, err := c.service.Comment(ctx.Request.Context(), req)
	if err != nil {
		utils.Error(ctx, http.StatusInternalServerError, "Failed to comment: "+err.Error())
		return
	}

	utils.Success(ctx, gin.H{"comment_id": id, "message": "Comment added successfully"})
}

// DeleteComment godoc
// @Summary Delete a comment
// @Description Delete a comment by its ID
// @Tags comments
// @Produce json
// @Param commentId path string true "Comment ID"
// @Success 200 {object} map[string]interface{} "Comment deleted successfully"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /comments/{commentId} [delete]
func (c *CommentsController) DeleteComment(ctx *gin.Context) {
	commentID := ctx.Param("commentId")

	if err := c.service.DeleteComment(ctx.Request.Context(), commentID); err != nil {
		utils.Error(ctx, http.StatusInternalServerError, "Failed to delete comment: "+err.Error())
		return
	}

	utils.Success(ctx, gin.H{"message": "Comment deleted successfully"})
}

// GetVideoComments godoc
// @Summary Get comments for a video
// @Description Get paginated list of comments for a specific video
// @Tags comments
// @Produce json
// @Param videoId path string true "Video ID"
// @Param limit query int false "Limit" default(20)
// @Param offset query int false "Offset" default(0)
// @Success 200 {object} map[string]interface{} "List of comments"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /videos/{videoId}/comments [get]
func (c *CommentsController) GetVideoComments(ctx *gin.Context) {
	videoID := ctx.Param("videoId")
	limit, _ := strconv.ParseInt(ctx.DefaultQuery("limit", "20"), 10, 64)
	offset, _ := strconv.ParseInt(ctx.DefaultQuery("offset", "0"), 10, 64)

	comments, total, err := c.service.GetVideoComments(ctx.Request.Context(), videoID, limit, offset)
	if err != nil {
		utils.Error(ctx, http.StatusInternalServerError, "Failed to get comments: "+err.Error())
		return
	}

	utils.Success(ctx, gin.H{
		"data":   comments,
		"total":  total,
		"limit":  limit,
		"offset": offset,
	})
}

// GetVideoCommentsCount godoc
// @Summary Get comments count for a video
// @Description Get total number of comments for a specific video
// @Tags comments
// @Produce json
// @Param videoId path string true "Video ID"
// @Success 200 {object} map[string]interface{} "Comments count"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /videos/{videoId}/comments/count [get]
func (c *CommentsController) GetVideoCommentsCount(ctx *gin.Context) {
	videoID := ctx.Param("videoId")

	count, err := c.service.GetVideoCommentsCount(ctx.Request.Context(), videoID)
	if err != nil {
		utils.Error(ctx, http.StatusInternalServerError, "Failed to get comments count: "+err.Error())
		return
	}

	utils.Success(ctx, gin.H{"video_id": videoID, "comments": count})
}
