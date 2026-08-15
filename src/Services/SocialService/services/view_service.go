package services

import (
	"context"
	"encoding/json"
	"log"
	"time"

	"social-service/messaging"
	"social-service/models"
	"social-service/repository"
)

type ViewService struct {
	mongoRepo *repository.MongoRepository
	redisRepo *repository.RedisRepository
	rabbit    *messaging.RabbitMQ
}

func NewViewService(mongoRepo *repository.MongoRepository, redisRepo *repository.RedisRepository, rabbit *messaging.RabbitMQ) *ViewService {
	return &ViewService{
		mongoRepo: mongoRepo,
		redisRepo: redisRepo,
		rabbit:    rabbit,
	}
}

func (s *ViewService) View(ctx context.Context, videoID, userID string) error {
	if err := s.mongoRepo.View(ctx, videoID, userID); err != nil {
		return err
	}

	if _, err := s.redisRepo.IncrementVideoViews(ctx, videoID); err != nil {
		log.Printf("Failed to increment view count in Redis: %v", err)
	}

	event := map[string]interface{}{
		"video_id":  videoID,
		"user_id":   userID,
		"timestamp": time.Now().Format(time.RFC3339),
		"type":      "view",
	}

	body, _ := json.Marshal(event)
	if err := s.rabbit.Publish("social.events", "social.viewed", body); err != nil {
		log.Printf("Failed to publish view event: %v", err)
	}

	return nil
}

func (s *ViewService) GetVideoViews(ctx context.Context, videoID string, limit, offset int64) ([]models.View, int64, error) {
	return s.mongoRepo.GetVideoViews(ctx, videoID, limit, offset)
}

func (s *ViewService) GetVideoViewsCount(ctx context.Context, videoID string) (int64, error) {
	count, err := s.redisRepo.GetVideoViewsCount(ctx, videoID)
	if err == nil && count > 0 {
		return count, nil
	}

	count, err = s.mongoRepo.GetVideoViewsCount(ctx, videoID)
	if err != nil {
		return 0, err
	}

	if err := s.redisRepo.SetVideoViewsCount(ctx, videoID, count); err != nil {
		log.Printf("Failed to cache view count: %v", err)
	}

	return count, nil
}
