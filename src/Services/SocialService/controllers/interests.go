package controllers

import (
	"net/http"

	"github.com/gin-gonic/gin"

	"social-service/services"
	"social-service/utils"
)

type InterestsController struct {
	service *services.InterestService
}

func NewInterestsController(service *services.InterestService) *InterestsController {
	return &InterestsController{service: service}
}

// GetUserInterests godoc
// @Summary Get user interests
// @Description Get the list of inferred interests for a user
// @Tags interests
// @Produce json
// @Param userId path string true "User ID"
// @Success 200 {object} map[string]interface{} "User interests"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /users/{userId}/interests [get]
func (c *InterestsController) GetUserInterests(ctx *gin.Context) {
	userID := ctx.Param("userId")

	interests, err := c.service.GetUserInterests(ctx.Request.Context(), userID)
	if err != nil {
		utils.Error(ctx, http.StatusInternalServerError, "Failed to get interests: "+err.Error())
		return
	}

	utils.Success(ctx, gin.H{"user_id": userID, "interests": interests})
}

// RefreshInterests godoc
// @Summary Refresh user interests
// @Description Recompute the inferred interests for a user
// @Tags interests
// @Produce json
// @Param userId path string true "User ID"
// @Success 200 {object} map[string]interface{} "Interests refreshed successfully"
// @Failure 500 {object} map[string]interface{} "Internal server error"
// @Router /users/{userId}/interests/refresh [post]
func (c *InterestsController) RefreshInterests(ctx *gin.Context) {
	userID := ctx.Param("userId")

	if err := c.service.RefreshInterests(ctx.Request.Context(), userID); err != nil {
		utils.Error(ctx, http.StatusInternalServerError, "Failed to refresh interests: "+err.Error())
		return
	}

	utils.Success(ctx, gin.H{"message": "Interests refreshed successfully"})
}
