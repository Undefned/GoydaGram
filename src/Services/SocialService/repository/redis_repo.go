package repository

import (
    "context"
    "fmt"
    "time"

    "social-service/cache"
)

type RedisRepository struct {
    cache *cache.RedisCache
}

func NewRedisRepository(cache *cache.RedisCache) *RedisRepository {
    return &RedisRepository{cache: cache}
}

// ============ COUNTERS ============

func (r *RedisRepository) IncrementCounter(ctx context.Context, key string) (int64, error) {
    return r.cache.Increment(ctx, key)
}

func (r *RedisRepository) GetCounter(ctx context.Context, key string) (int64, error) {
    val, err := r.cache.Get(ctx, key)
    if err != nil {
        if err.Error() == "redis: nil" {
            return 0, nil
        }
        return 0, err
    }
    var result int64
    fmt.Sscanf(val, "%d", &result)
    return result, nil
}

func (r *RedisRepository) SetCounter(ctx context.Context, key string, value int64, ttl time.Duration) error {
    return r.cache.Set(ctx, key, value, ttl)
}

// ============ INTERESTS CACHE ============

func (r *RedisRepository) GetUserInterests(ctx context.Context, userID string, dest interface{}) error {
    key := fmt.Sprintf("interests:%s", userID)
    return r.cache.GetJSON(ctx, key, dest)
}

func (r *RedisRepository) SetUserInterests(ctx context.Context, userID string, interests interface{}, ttl time.Duration) error {
    key := fmt.Sprintf("interests:%s", userID)
    return r.cache.SetJSON(ctx, key, interests, ttl)
}

func (r *RedisRepository) DeleteUserInterests(ctx context.Context, userID string) error {
    key := fmt.Sprintf("interests:%s", userID)
    return r.cache.Delete(ctx, key)
}

// ============ LIKES CACHE ============

func (r *RedisRepository) GetVideoLikesCount(ctx context.Context, videoID string) (int64, error) {
    key := fmt.Sprintf("likes:video:%s", videoID)
    return r.GetCounter(ctx, key)
}

func (r *RedisRepository) SetVideoLikesCount(ctx context.Context, videoID string, count int64) error {
    key := fmt.Sprintf("likes:video:%s", videoID)
    return r.SetCounter(ctx, key, count, 5*time.Minute)
}

func (r *RedisRepository) IncrementVideoLikes(ctx context.Context, videoID string) (int64, error) {
    key := fmt.Sprintf("likes:video:%s", videoID)
    return r.IncrementCounter(ctx, key)
}

func (r *RedisRepository) DecrementVideoLikes(ctx context.Context, videoID string) (int64, error) {
    key := fmt.Sprintf("likes:video:%s", videoID)
    return r.DecrementCounter(ctx, key)
}

// ============ VIEWS CACHE ============

func (r *RedisRepository) GetVideoViewsCount(ctx context.Context, videoID string) (int64, error) {
    key := fmt.Sprintf("views:video:%s", videoID)
    return r.GetCounter(ctx, key)
}

func (r *RedisRepository) SetVideoViewsCount(ctx context.Context, videoID string, count int64) error {
    key := fmt.Sprintf("views:video:%s", videoID)
    return r.SetCounter(ctx, key, count, 5*time.Minute)
}

func (r *RedisRepository) IncrementVideoViews(ctx context.Context, videoID string) (int64, error) {
    key := fmt.Sprintf("views:video:%s", videoID)
    return r.IncrementCounter(ctx, key)
}

// ============ COMMENTS CACHE ============

func (r *RedisRepository) GetVideoCommentsCount(ctx context.Context, videoID string) (int64, error) {
    key := fmt.Sprintf("comments:video:%s", videoID)
    return r.GetCounter(ctx, key)
}

func (r *RedisRepository) SetVideoCommentsCount(ctx context.Context, videoID string, count int64) error {
    key := fmt.Sprintf("comments:video:%s", videoID)
    return r.SetCounter(ctx, key, count, 5*time.Minute)
}

func (r *RedisRepository) IncrementVideoComments(ctx context.Context, videoID string) (int64, error) {
    key := fmt.Sprintf("comments:video:%s", videoID)
    return r.IncrementCounter(ctx, key)
}

// ============ GENERIC ============

func (r *RedisRepository) DecrementCounter(ctx context.Context, key string) (int64, error) {
    return r.cache.Decrement(ctx, key)
}