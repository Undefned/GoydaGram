package models

import "time"

type Video struct {
	ID            string    `json:"id"`
	UserID        string    `json:"userId"`
	Title         string    `json:"title"`
	Description   string    `json:"description"`
	Duration      int       `json:"duration"`
	URL           string    `json:"originalUrl"`
	PreviewURL    string    `json:"previewUrl"`
	HlsManifest   string    `json:"hlsManifestUrl"`
	Status        string    `json:"status"`
	ViewsCount    int       `json:"viewsCount"`
	LikesCount    int       `json:"likesCount"`
	CommentsCount int       `json:"commentsCount"`
	CreatedAt     time.Time `json:"createdAt"`
	User          *User     `json:"user,omitempty"`
	Tags          []string  `json:"tags,omitempty"`
}

type User struct {
	ID         string `json:"id"`
	Username   string `json:"username"`
	Email      string `json:"email"`
	AvatarURL  string `json:"avatarUrl"`
	IsVerified bool   `json:"isVerified"`
}

type Interest struct {
	Tag    string  `json:"tag"`
	Weight float64 `json:"weight"`
}
