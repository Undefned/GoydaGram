package services

import (
	"testing"
)

func TestPrefetchService_NewPrefetchService(t *testing.T) {
	// Arrange
	feedService := &FeedService{batchSize: 30}

	// Act
	prefetchService := NewPrefetchService(feedService, 3)
	defer prefetchService.Close()

	// Assert
	if prefetchService == nil {
		t.Error("Expected PrefetchService, got nil")
	}
}

func TestPrefetchService_Close(t *testing.T) {
	// Arrange
	feedService := &FeedService{batchSize: 30}
	prefetchService := NewPrefetchService(feedService, 2)

	// Act
	prefetchService.Close()

	// Assert - should not panic
	// Channel should be closed
	_, ok := <-prefetchService.prefetchChan
	if ok {
		t.Error("Expected prefetchChan to be closed")
	}
}
