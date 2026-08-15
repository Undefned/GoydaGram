package services

import (
    "context"
    "log"
    "sort"
    "time"

    "social-service/models"
    "social-service/repository"
)

type InterestService struct {
    mongoRepo *repository.MongoRepository
    redisRepo *repository.RedisRepository
}

func NewInterestService(mongoRepo *repository.MongoRepository, redisRepo *repository.RedisRepository) *InterestService {
    return &InterestService{
        mongoRepo: mongoRepo,
        redisRepo: redisRepo,
    }
}

func (s *InterestService) GetUserInterests(ctx context.Context, userID string) ([]models.Interest, error) {
    // Try Redis first
    var interests []models.Interest
    err := s.redisRepo.GetUserInterests(ctx, userID, &interests)
    if err == nil && len(interests) > 0 {
        return interests, nil
    }

    // Fallback to MongoDB
    userInterests, err := s.mongoRepo.GetUserInterests(ctx, userID)
    if err != nil {
        return nil, err
    }

    if len(userInterests.Interests) == 0 {
        // Return default interests
        return []models.Interest{
            {Tag: "funny", Weight: 0.8},
            {Tag: "music", Weight: 0.6},
            {Tag: "dance", Weight: 0.5},
        }, nil
    }

    // Cache in Redis
    if err := s.redisRepo.SetUserInterests(ctx, userID, userInterests.Interests, 10*time.Minute); err != nil {
        log.Printf("Failed to cache user interests: %v", err)
    }

    return userInterests.Interests, nil
}

func (s *InterestService) RefreshInterests(ctx context.Context, userID string) error {
    log.Printf("Refreshing interests for user %s", userID)

    // 1. Get user's likes
    likes, _, err := s.mongoRepo.GetVideoLikes(ctx, "", 1000, 0)
    if err != nil {
        return err
    }
    likedVideoIDs := []string{}
    for _, l := range likes {
        if l.UserID == userID {
            likedVideoIDs = append(likedVideoIDs, l.VideoID)
        }
    }

    // 2. Get user's views
    views, _, err := s.mongoRepo.GetVideoViews(ctx, "", 1000, 0)
    if err != nil {
        return err
    }
    viewedVideoIDs := []string{}
    for _, v := range views {
        if v.UserID == userID {
            viewedVideoIDs = append(viewedVideoIDs, v.VideoID)
        }
    }

    // 3. Get tags for videos (from Content Service via HTTP)
    // For simplicity, we'll use dummy tags here
    // In production, you'd call Content Service API
    tagsMap := map[string][]string{}
    for _, id := range likedVideoIDs {
        tagsMap[id] = []string{"funny", "music", "dance"}
    }
    for _, id := range viewedVideoIDs {
        if _, ok := tagsMap[id]; !ok {
            tagsMap[id] = []string{"funny", "music"}
        }
    }

    // 4. Calculate interest weights
    weights := map[string]float64{}
    for _, id := range likedVideoIDs {
        for _, tag := range tagsMap[id] {
            weights[tag] += 3.0 // Likes have higher weight
        }
    }
    for _, id := range viewedVideoIDs {
        for _, tag := range tagsMap[id] {
            weights[tag] += 1.0 // Views have lower weight
        }
    }

    // 5. Normalize
    var interests []models.Interest
    if len(weights) > 0 {
        maxWeight := 0.0
        for _, w := range weights {
            if w > maxWeight {
                maxWeight = w
            }
        }
        for tag, weight := range weights {
            interests = append(interests, models.Interest{
                Tag:    tag,
                Weight: weight / maxWeight,
            })
        }
        sort.Slice(interests, func(i, j int) bool {
            return interests[i].Weight > interests[j].Weight
        })
    } else {
        interests = []models.Interest{
            {Tag: "funny", Weight: 0.8},
            {Tag: "music", Weight: 0.6},
        }
    }

    // 6. Save to MongoDB
    if err := s.mongoRepo.UpdateUserInterests(ctx, userID, interests); err != nil {
        return err
    }

    // 7. Cache in Redis
    if err := s.redisRepo.SetUserInterests(ctx, userID, interests, 10*time.Minute); err != nil {
        log.Printf("Failed to cache user interests: %v", err)
    }

    return nil
}