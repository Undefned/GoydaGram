package clients

import (
	"bytes"
	"encoding/json"
	"fmt"
	"net/http"
	"time"

	"feed-service/models"
)

type ContentClient struct {
	baseURL    string
	httpClient *http.Client
}

func NewContentClient(baseURL string, timeout time.Duration) *ContentClient {
	return &ContentClient{
		baseURL: baseURL,
		httpClient: &http.Client{
			Timeout: timeout,
		},
	}
}

func (c *ContentClient) GetVideoBatch(videoIDs []string) ([]models.Video, error) {
	if len(videoIDs) == 0 {
		return []models.Video{}, nil
	}

	url := fmt.Sprintf("%s/api/videos/batch", c.baseURL)

	body := map[string]interface{}{
		"video_ids": videoIDs,
	}
	jsonBody, _ := json.Marshal(body)

	req, err := http.NewRequest("POST", url, bytes.NewBuffer(jsonBody))
	if err != nil {
		return nil, err
	}
	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("X-Correlation-ID", getCorrelationID())

	resp, err := c.httpClient.Do(req)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("content service returned %d", resp.StatusCode)
	}

	var videos []models.Video
	if err := json.NewDecoder(resp.Body).Decode(&videos); err != nil {
		return nil, err
	}
	return videos, nil
}

func (c *ContentClient) GetVideo(videoID string) (*models.Video, error) {
	url := fmt.Sprintf("%s/api/videos/%s", c.baseURL, videoID)

	req, err := http.NewRequest("GET", url, nil)
	if err != nil {
		return nil, err
	}
	req.Header.Set("X-Correlation-ID", getCorrelationID())

	resp, err := c.httpClient.Do(req)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("content service returned %d", resp.StatusCode)
	}

	var video models.Video
	if err := json.NewDecoder(resp.Body).Decode(&video); err != nil {
		return nil, err
	}
	return &video, nil
}

func (c *ContentClient) GetTrending(limit int) ([]models.Video, error) {
	url := fmt.Sprintf("%s/api/videos/trending?limit=%d", c.baseURL, limit)

	req, err := http.NewRequest("GET", url, nil)
	if err != nil {
		return nil, err
	}
	req.Header.Set("X-Correlation-ID", getCorrelationID())

	resp, err := c.httpClient.Do(req)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("content service returned %d", resp.StatusCode)
	}

	var videos []models.Video
	if err := json.NewDecoder(resp.Body).Decode(&videos); err != nil {
		return nil, err
	}
	return videos, nil
}
