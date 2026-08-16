package services

import "context"

// Интерфейсы под LikeService — вынесены отдельно, чтобы сервис зависел от
// поведения, а не от конкретных *repository.MongoRepository/*messaging.RabbitMQ.
// *repository.MongoRepository, *repository.RedisRepository и *messaging.RabbitMQ
// уже реализуют эти методы — реализацию менять не нужно, только конструктор ниже.

type LikeRepository interface {
	Like(ctx context.Context, videoID, userID string) error
	Unlike(ctx context.Context, videoID, userID string) error
	GetVideoLikesCount(ctx context.Context, videoID string) (int64, error)
	UserLiked(ctx context.Context, videoID, userID string) (bool, error)
}

type LikeCounterCache interface {
	IncrementVideoLikes(ctx context.Context, videoID string) (int64, error)
	DecrementVideoLikes(ctx context.Context, videoID string) (int64, error)
	GetVideoLikesCount(ctx context.Context, videoID string) (int64, error)
	SetVideoLikesCount(ctx context.Context, videoID string, count int64) error
}

type EventPublisher interface {
	Publish(exchange, routingKey string, body []byte) error
}
