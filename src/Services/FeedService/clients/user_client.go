package clients

import (
	"encoding/json"
	"fmt"
	"net/http"
	"time"

	"feed-service/models"
)

type UserClient struct {
	baseURL    string
	httpClient *http.Client
}

func NewUserClient(baseURL string, timeout time.Duration) *UserClient {
	return &UserClient{
		baseURL: baseURL,
		httpClient: &http.Client{
			Timeout: timeout,
		},
	}
}

func (c *UserClient) GetSubscriptions(userID string) ([]string, error) {
	url := fmt.Sprintf("%s/api/users/%s/subscriptions", c.baseURL, userID)

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
		return nil, fmt.Errorf("user service returned %d", resp.StatusCode)
	}

	var users []models.User
	if err := json.NewDecoder(resp.Body).Decode(&users); err != nil {
		return nil, err
	}

	ids := make([]string, len(users))
	for i, u := range users {
		ids[i] = u.ID
	}
	return ids, nil
}

func (c *UserClient) GetUser(userID string) (*models.User, error) {
	url := fmt.Sprintf("%s/api/users/%s", c.baseURL, userID)

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
		return nil, fmt.Errorf("user service returned %d", resp.StatusCode)
	}

	var user models.User
	if err := json.NewDecoder(resp.Body).Decode(&user); err != nil {
		return nil, err
	}
	return &user, nil
}

func getCorrelationID() string {
	// In production, this would be passed from context
	return ""
}
