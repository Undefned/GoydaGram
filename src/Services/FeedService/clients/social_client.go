package clients

import (
	"encoding/json"
	"fmt"
	"net/http"
	"time"

	"feed-service/models"
)

type SocialClient struct {
	baseURL    string
	httpClient *http.Client
}

func NewSocialClient(baseURL string, timeout time.Duration) *SocialClient {
	return &SocialClient{
		baseURL: baseURL,
		httpClient: &http.Client{
			Timeout: timeout,
		},
	}
}

func (c *SocialClient) GetUserInterests(userID string) ([]models.Interest, error) {
	url := fmt.Sprintf("%s/api/users/%s/interests", c.baseURL, userID)

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
		return nil, fmt.Errorf("social service returned %d", resp.StatusCode)
	}

	var result struct {
		Data struct {
			Interests []models.Interest `json:"interests"`
		} `json:"data"`
	}
	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return nil, err
	}
	return result.Data.Interests, nil
}

func (c *SocialClient) GetVideoLikesCount(videoID string) (int, error) {
	url := fmt.Sprintf("%s/api/videos/%s/likes/count", c.baseURL, videoID)

	req, err := http.NewRequest("GET", url, nil)
	if err != nil {
		return 0, err
	}
	req.Header.Set("X-Correlation-ID", getCorrelationID())

	resp, err := c.httpClient.Do(req)
	if err != nil {
		return 0, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return 0, fmt.Errorf("social service returned %d", resp.StatusCode)
	}

	var result struct {
		Data struct {
			Likes int `json:"likes"`
		} `json:"data"`
	}
	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return 0, err
	}
	return result.Data.Likes, nil
}

func (c *SocialClient) GetVideoViewsCount(videoID string) (int, error) {
	url := fmt.Sprintf("%s/api/videos/%s/views/count", c.baseURL, videoID)

	req, err := http.NewRequest("GET", url, nil)
	if err != nil {
		return 0, err
	}
	req.Header.Set("X-Correlation-ID", getCorrelationID())

	resp, err := c.httpClient.Do(req)
	if err != nil {
		return 0, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return 0, fmt.Errorf("social service returned %d", resp.StatusCode)
	}

	var result struct {
		Data struct {
			Views int `json:"views"`
		} `json:"data"`
	}
	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return 0, err
	}
	return result.Data.Views, nil
}
