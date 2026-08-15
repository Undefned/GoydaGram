package models

import "time"

type Video struct {
	ID            string    `json:"id"`
	UserID        string    `json:"user_id"`
	Title         string    `json:"title"`
	Description   string    `json:"description"`
	Duration      int       `json:"duration"`
	URL           string    `json:"url"`
	PreviewURL    string    `json:"preview_url"`
	HlsManifest   string    `json:"hls_manifest,omitempty"`
	Status        string    `json:"status"`
	ViewsCount    int       `json:"views_count"`
	LikesCount    int       `json:"likes_count"`
	CommentsCount int       `json:"comments_count"`
	CreatedAt     time.Time `json:"created_at"`
	User          *User     `json:"user,omitempty"`
	Tags          []string  `json:"tags,omitempty"`
}

type User struct {
	ID         string `json:"id"`
	Username   string `json:"username"`
	Email      string `json:"email"`
	AvatarURL  string `json:"avatar_url"`
	IsVerified bool   `json:"is_verified"`
}

type Interest struct {
	Tag    string  `json:"tag"`
	Weight float64 `json:"weight"`
}
