package services

import (
	"context"
	"encoding/json"
	"log"
	"time"

	"social-service/models"
)

// LikeRepository (полный набор методов, включая пагинацию) — используется LikeService.
type LikeRepositoryFull interface {
	LikeRepository
	GetVideoLikes(ctx context.Context, videoID string, limit, offset int64) ([]models.Like, int64, error)
}

type LikeService struct {
	repo  LikeRepositoryFull
	cache LikeCounterCache
	pub   EventPublisher
}

// Конструктор принимает интерфейсы. *repository.MongoRepository, *repository.RedisRepository
// и *messaging.RabbitMQ уже реализуют эти методы — вызовы на стороне main.go не меняются,
// просто передавайте те же указатели сюда.
func NewLikeService(repo LikeRepositoryFull, cache LikeCounterCache, pub EventPublisher) *LikeService {
	return &LikeService{repo: repo, cache: cache, pub: pub}
}

func (s *LikeService) Like(ctx context.Context, videoID, userID string) error {
	exists, err := s.repo.UserLiked(ctx, videoID, userID)
	if err != nil {
		return err
	}
	if exists {
		return nil
	}

	if err := s.repo.Like(ctx, videoID, userID); err != nil {
		return err
	}

	if _, err := s.cache.IncrementVideoLikes(ctx, videoID); err != nil {
		log.Printf("Failed to increment like count in Redis: %v", err)
	}

	event := map[string]interface{}{
		"video_id":  videoID,
		"user_id":   userID,
		"timestamp": time.Now().Format(time.RFC3339),
		"type":      "like",
	}

	body, _ := json.Marshal(event)
	if err := s.pub.Publish("social.events", "social.liked", body); err != nil {
		log.Printf("Failed to publish like event: %v", err)
	}

	return nil
}

func (s *LikeService) Unlike(ctx context.Context, videoID, userID string) error {
	if err := s.repo.Unlike(ctx, videoID, userID); err != nil {
		return err
	}

	if _, err := s.cache.DecrementVideoLikes(ctx, videoID); err != nil {
		log.Printf("Failed to decrement like count in Redis: %v", err)
	}

	return nil
}

func (s *LikeService) GetVideoLikes(ctx context.Context, videoID string, limit, offset int64) ([]models.Like, int64, error) {
	return s.repo.GetVideoLikes(ctx, videoID, limit, offset)
}

func (s *LikeService) GetVideoLikesCount(ctx context.Context, videoID string) (int64, error) {
	count, err := s.cache.GetVideoLikesCount(ctx, videoID)
	if err == nil && count > 0 {
		return count, nil
	}

	count, err = s.repo.GetVideoLikesCount(ctx, videoID)
	if err != nil {
		return 0, err
	}

	if err := s.cache.SetVideoLikesCount(ctx, videoID, count); err != nil {
		log.Printf("Failed to cache like count: %v", err)
	}

	return count, nil
}
