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

type LikeService struct {
	mongoRepo *repository.MongoRepository
	redisRepo *repository.RedisRepository
	rabbit    *messaging.RabbitMQ
}

func NewLikeService(mongoRepo *repository.MongoRepository, redisRepo *repository.RedisRepository, rabbit *messaging.RabbitMQ) *LikeService {
	return &LikeService{
		mongoRepo: mongoRepo,
		redisRepo: redisRepo,
		rabbit:    rabbit,
	}
}

func (s *LikeService) Like(ctx context.Context, videoID, userID string) error {
	exists, err := s.mongoRepo.UserLiked(ctx, videoID, userID)
	if err != nil {
		return err
	}
	if exists {
		return nil
	}

	if err := s.mongoRepo.Like(ctx, videoID, userID); err != nil {
		return err
	}

	if _, err := s.redisRepo.IncrementVideoLikes(ctx, videoID); err != nil {
		log.Printf("Failed to increment like count in Redis: %v", err)
	}

	event := map[string]interface{}{
		"video_id":  videoID,
		"user_id":   userID,
		"timestamp": time.Now().Format(time.RFC3339),
		"type":      "like",
	}

	body, _ := json.Marshal(event)
	if err := s.rabbit.Publish("social.events", "social.liked", body); err != nil {
		log.Printf("Failed to publish like event: %v", err)
	}

	return nil
}

func (s *LikeService) Unlike(ctx context.Context, videoID, userID string) error {
	if err := s.mongoRepo.Unlike(ctx, videoID, userID); err != nil {
		return err
	}

	if _, err := s.redisRepo.DecrementVideoLikes(ctx, videoID); err != nil {
		log.Printf("Failed to decrement like count in Redis: %v", err)
	}

	return nil
}

func (s *LikeService) GetVideoLikes(ctx context.Context, videoID string, limit, offset int64) ([]models.Like, int64, error) {
	return s.mongoRepo.GetVideoLikes(ctx, videoID, limit, offset)
}

func (s *LikeService) GetVideoLikesCount(ctx context.Context, videoID string) (int64, error) {
	count, err := s.redisRepo.GetVideoLikesCount(ctx, videoID)
	if err == nil && count > 0 {
		return count, nil
	}

	count, err = s.mongoRepo.GetVideoLikesCount(ctx, videoID)
	if err != nil {
		return 0, err
	}

	if err := s.redisRepo.SetVideoLikesCount(ctx, videoID, count); err != nil {
		log.Printf("Failed to cache like count: %v", err)
	}

	return count, nil
}
