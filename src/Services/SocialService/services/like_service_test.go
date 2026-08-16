package services

import (
	"context"
	"errors"
	"testing"

	"social-service/models"

	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"
)

// ---- fakes implementing LikeRepositoryFull / LikeCounterCache / EventPublisher ----

type fakeLikeRepo struct {
	liked         map[string]bool // key: videoID+userID
	likeErr       error
	unlikeErr     error
	likesCount    int64
	likesCountErr error
}

func newFakeLikeRepo() *fakeLikeRepo {
	return &fakeLikeRepo{liked: make(map[string]bool)}
}

func key(videoID, userID string) string { return videoID + "|" + userID }

func (f *fakeLikeRepo) Like(ctx context.Context, videoID, userID string) error {
	if f.likeErr != nil {
		return f.likeErr
	}
	f.liked[key(videoID, userID)] = true
	return nil
}

func (f *fakeLikeRepo) Unlike(ctx context.Context, videoID, userID string) error {
	if f.unlikeErr != nil {
		return f.unlikeErr
	}
	delete(f.liked, key(videoID, userID))
	return nil
}

func (f *fakeLikeRepo) GetVideoLikesCount(ctx context.Context, videoID string) (int64, error) {
	return f.likesCount, f.likesCountErr
}

func (f *fakeLikeRepo) UserLiked(ctx context.Context, videoID, userID string) (bool, error) {
	return f.liked[key(videoID, userID)], nil
}

func (f *fakeLikeRepo) GetVideoLikes(ctx context.Context, videoID string, limit, offset int64) ([]models.Like, int64, error) {
	return nil, 0, nil
}

type fakeCounterCache struct {
	incrementCalls int
	decrementCalls int
	cachedCount    int64
}

func (f *fakeCounterCache) IncrementVideoLikes(ctx context.Context, videoID string) (int64, error) {
	f.incrementCalls++
	return 1, nil
}

func (f *fakeCounterCache) DecrementVideoLikes(ctx context.Context, videoID string) (int64, error) {
	f.decrementCalls++
	return 0, nil
}

func (f *fakeCounterCache) GetVideoLikesCount(ctx context.Context, videoID string) (int64, error) {
	return f.cachedCount, nil
}

func (f *fakeCounterCache) SetVideoLikesCount(ctx context.Context, videoID string, count int64) error {
	f.cachedCount = count
	return nil
}

type fakePublisher struct {
	published []string // routing keys
	err       error
}

func (f *fakePublisher) Publish(exchange, routingKey string, body []byte) error {
	if f.err != nil {
		return f.err
	}
	f.published = append(f.published, routingKey)
	return nil
}

// ---- tests ----

func TestLikeService_Like_NewLike_PersistsAndPublishesEvent(t *testing.T) {
	repo := newFakeLikeRepo()
	cache := &fakeCounterCache{}
	pub := &fakePublisher{}
	svc := NewLikeService(repo, cache, pub)

	err := svc.Like(context.Background(), "video-1", "user-1")

	require.NoError(t, err)
	assert.True(t, repo.liked[key("video-1", "user-1")])
	assert.Equal(t, 1, cache.incrementCalls)
	assert.Equal(t, []string{"social.liked"}, pub.published)
}

func TestLikeService_Like_AlreadyLiked_IsIdempotent(t *testing.T) {
	repo := newFakeLikeRepo()
	repo.liked[key("video-1", "user-1")] = true
	cache := &fakeCounterCache{}
	pub := &fakePublisher{}
	svc := NewLikeService(repo, cache, pub)

	err := svc.Like(context.Background(), "video-1", "user-1")

	require.NoError(t, err)
	// Already liked — must not increment counter or publish a duplicate event
	assert.Equal(t, 0, cache.incrementCalls)
	assert.Empty(t, pub.published)
}

func TestLikeService_Like_RepoFailure_DoesNotIncrementOrPublish(t *testing.T) {
	repo := newFakeLikeRepo()
	repo.likeErr = errors.New("mongo down")
	cache := &fakeCounterCache{}
	pub := &fakePublisher{}
	svc := NewLikeService(repo, cache, pub)

	err := svc.Like(context.Background(), "video-1", "user-1")

	require.Error(t, err)
	assert.Equal(t, 0, cache.incrementCalls)
	assert.Empty(t, pub.published)
}

func TestLikeService_Unlike_RemovesLikeAndDecrementsCounter(t *testing.T) {
	repo := newFakeLikeRepo()
	repo.liked[key("video-1", "user-1")] = true
	cache := &fakeCounterCache{}
	pub := &fakePublisher{}
	svc := NewLikeService(repo, cache, pub)

	err := svc.Unlike(context.Background(), "video-1", "user-1")

	require.NoError(t, err)
	assert.False(t, repo.liked[key("video-1", "user-1")])
	assert.Equal(t, 1, cache.decrementCalls)
}

func TestLikeService_GetVideoLikesCount_PrefersCacheWhenPositive(t *testing.T) {
	repo := newFakeLikeRepo()
	repo.likesCount = 999 // should NOT be used if cache has a positive value
	cache := &fakeCounterCache{cachedCount: 5}
	pub := &fakePublisher{}
	svc := NewLikeService(repo, cache, pub)

	count, err := svc.GetVideoLikesCount(context.Background(), "video-1")

	require.NoError(t, err)
	assert.Equal(t, int64(5), count)
}

func TestLikeService_GetVideoLikesCount_FallsBackToRepoWhenCacheEmpty(t *testing.T) {
	repo := newFakeLikeRepo()
	repo.likesCount = 42
	cache := &fakeCounterCache{cachedCount: 0} // cache miss
	pub := &fakePublisher{}
	svc := NewLikeService(repo, cache, pub)

	count, err := svc.GetVideoLikesCount(context.Background(), "video-1")

	require.NoError(t, err)
	assert.Equal(t, int64(42), count)
	assert.Equal(t, int64(42), cache.cachedCount, "should backfill cache after DB fallback")
}
