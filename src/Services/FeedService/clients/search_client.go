package clients

import (
	"bytes"
	"encoding/json"
	"fmt"
	"net/http"
	"time"

	"feed-service/models"
)

type SearchClient struct {
	baseURL    string
	httpClient *http.Client
}

func NewSearchClient(baseURL string, timeout time.Duration) *SearchClient {
	return &SearchClient{
		baseURL: baseURL,
		httpClient: &http.Client{
			Timeout: timeout,
		},
	}
}

func (c *SearchClient) GetRecommendations(interests []models.Interest, limit int) ([]string, error) {
	if len(interests) == 0 {
		return []string{}, nil
	}

	url := fmt.Sprintf("%s/api/search/recommendations", c.baseURL)

	body := map[string]interface{}{
		"interests": interests,
		"limit":     limit,
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
		return nil, fmt.Errorf("search service returned %d", resp.StatusCode)
	}

	var result struct {
		VideoIDs []string `json:"video_ids"`
	}
	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return nil, err
	}
	return result.VideoIDs, nil
}
