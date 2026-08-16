package clients

import (
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"testing"
	"time"

	"feed-service/models"
)

func TestContentClient_GetVideoBatch(t *testing.T) {
	// Arrange
	expectedVideos := []models.Video{
		{ID: "video1", UserID: "user1", Title: "Video 1"},
		{ID: "video2", UserID: "user2", Title: "Video 2"},
	}

	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path != "/api/videos/batch" {
			t.Errorf("Expected path /api/videos/batch, got %s", r.URL.Path)
		}
		if r.Method != "POST" {
			t.Errorf("Expected POST method, got %s", r.Method)
		}

		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(expectedVideos)
	}))
	defer server.Close()

	client := NewContentClient(server.URL, 5*time.Second)

	// Act
	result, err := client.GetVideoBatch([]string{"video1", "video2"})

	// Assert
	if err != nil {
		t.Errorf("Expected no error, got: %v", err)
	}
	if len(result) != 2 {
		t.Errorf("Expected 2 videos, got %d", len(result))
	}
	if result[0].ID != "video1" {
		t.Errorf("Expected video1, got %s", result[0].ID)
	}
}

func TestContentClient_GetVideoBatch_EmptyList(t *testing.T) {
	client := NewContentClient("http://test.com", 5*time.Second)

	result, err := client.GetVideoBatch([]string{})

	if err != nil {
		t.Errorf("Expected no error, got: %v", err)
	}
	if len(result) != 0 {
		t.Errorf("Expected empty result, got %d videos", len(result))
	}
}

func TestContentClient_GetVideo(t *testing.T) {
	expectedVideo := models.Video{ID: "video1", UserID: "user1", Title: "Video 1"}

	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path != "/api/videos/video1" {
			t.Errorf("Expected path /api/videos/video1, got %s", r.URL.Path)
		}

		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(expectedVideo)
	}))
	defer server.Close()

	client := NewContentClient(server.URL, 5*time.Second)

	result, err := client.GetVideo("video1")

	if err != nil {
		t.Errorf("Expected no error, got: %v", err)
	}
	if result == nil {
		t.Error("Expected video, got nil")
	}
	if result.ID != "video1" {
		t.Errorf("Expected video1, got %s", result.ID)
	}
}

func TestContentClient_GetTrending(t *testing.T) {
	expectedVideos := []models.Video{
		{ID: "trending1", UserID: "user1", Title: "Trending 1"},
	}

	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path != "/api/videos/trending" {
			t.Errorf("Expected path /api/videos/trending, got %s", r.URL.Path)
		}

		limit := r.URL.Query().Get("limit")
		if limit != "10" {
			t.Errorf("Expected limit=10, got %s", limit)
		}

		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(expectedVideos)
	}))
	defer server.Close()

	client := NewContentClient(server.URL, 5*time.Second)

	result, err := client.GetTrending(10)

	if err != nil {
		t.Errorf("Expected no error, got: %v", err)
	}
	if len(result) != 1 {
		t.Errorf("Expected 1 video, got %d", len(result))
	}
}
