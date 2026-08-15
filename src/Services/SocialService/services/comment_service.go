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

type CommentService struct {
	mongoRepo *repository.MongoRepository
	redisRepo *repository.RedisRepository
	rabbit    *messaging.RabbitMQ
}

func NewCommentService(mongoRepo *repository.MongoRepository, redisRepo *repository.RedisRepository, rabbit *messaging.RabbitMQ) *CommentService {
	return &CommentService{
		mongoRepo: mongoRepo,
		redisRepo: redisRepo,
		rabbit:    rabbit,
	}
}

func (s *CommentService) Comment(ctx context.Context, req models.CommentRequest) (string, error) {
	comment := models.Comment{
		VideoID:  req.VideoID,
		UserID:   req.UserID,
		Text:     req.Text,
		ParentID: req.ParentID,
	}

	id, err := s.mongoRepo.Comment(ctx, comment)
	if err != nil {
		return "", err
	}

	if _, err := s.redisRepo.IncrementVideoComments(ctx, req.VideoID); err != nil {
		log.Printf("Failed to increment comment count in Redis: %v", err)
	}

	event := map[string]interface{}{
		"video_id":   req.VideoID,
		"user_id":    req.UserID,
		"comment_id": id,
		"timestamp":  time.Now().Format(time.RFC3339),
		"type":       "comment",
	}

	body, _ := json.Marshal(event)
	if err := s.rabbit.Publish("social.events", "social.commented", body); err != nil {
		log.Printf("Failed to publish comment event: %v", err)
	}

	return id, nil
}

func (s *CommentService) DeleteComment(ctx context.Context, commentID string) error {
	return s.mongoRepo.DeleteComment(ctx, commentID)
}

func (s *CommentService) GetVideoComments(ctx context.Context, videoID string, limit, offset int64) ([]models.Comment, int64, error) {
	return s.mongoRepo.GetVideoComments(ctx, videoID, limit, offset)
}

func (s *CommentService) GetVideoCommentsCount(ctx context.Context, videoID string) (int64, error) {
	count, err := s.redisRepo.GetVideoCommentsCount(ctx, videoID)
	if err == nil && count > 0 {
		return count, nil
	}

	count, err = s.mongoRepo.GetVideoCommentsCount(ctx, videoID)
	if err != nil {
		return 0, err
	}

	if err := s.redisRepo.SetVideoCommentsCount(ctx, videoID, count); err != nil {
		log.Printf("Failed to cache comment count: %v", err)
	}

	return count, nil
}
