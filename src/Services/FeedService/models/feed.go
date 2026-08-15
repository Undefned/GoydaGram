package models

type FeedRequest struct {
	UserID string   `json:"user_id"`
	Offset int      `json:"offset"`
	Limit  int      `json:"limit"`
	Seen   []string `json:"seen"`
}

type FeedResponse struct {
	Videos     []Video `json:"videos"`
	NextOffset int     `json:"next_offset"`
	HasMore    bool    `json:"has_more"`
	TotalCount int     `json:"total_count,omitempty"`
}
