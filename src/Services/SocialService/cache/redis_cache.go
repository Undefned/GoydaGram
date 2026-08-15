package cache

import (
    "context"
    "encoding/json"
    "time"

    "github.com/redis/go-redis/v9"
)

type RedisCache struct {
    client *redis.Client
}

func NewRedisCache(addr, password string) (*RedisCache, error) {
    client := redis.NewClient(&redis.Options{
        Addr:     addr,
        Password: password,
        DB:       0,
    })

    ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
    defer cancel()

    if err := client.Ping(ctx).Err(); err != nil {
        return nil, err
    }

    return &RedisCache{client: client}, nil
}

func (r *RedisCache) Get(ctx context.Context, key string) (string, error) {
    return r.client.Get(ctx, key).Result()
}

func (r *RedisCache) GetJSON(ctx context.Context, key string, dest interface{}) error {
    val, err := r.client.Get(ctx, key).Result()
    if err != nil {
        return err
    }
    return json.Unmarshal([]byte(val), dest)
}

func (r *RedisCache) Set(ctx context.Context, key string, value interface{}, ttl time.Duration) error {
    return r.client.Set(ctx, key, value, ttl).Err()
}

func (r *RedisCache) SetJSON(ctx context.Context, key string, value interface{}, ttl time.Duration) error {
    data, err := json.Marshal(value)
    if err != nil {
        return err
    }
    return r.client.Set(ctx, key, data, ttl).Err()
}

func (r *RedisCache) Delete(ctx context.Context, keys ...string) error {
    return r.client.Del(ctx, keys...).Err()
}

func (r *RedisCache) Increment(ctx context.Context, key string) (int64, error) {
    return r.client.Incr(ctx, key).Result()
}

func (r *RedisCache) Decrement(ctx context.Context, key string) (int64, error) {
    return r.client.Decr(ctx, key).Result()
}

func (r *RedisCache) Exists(ctx context.Context, key string) (bool, error) {
    result, err := r.client.Exists(ctx, key).Result()
    return result > 0, err
}

func (r *RedisCache) Close() error {
    return r.client.Close()
}