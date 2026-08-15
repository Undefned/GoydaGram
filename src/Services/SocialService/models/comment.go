package models

import (
	"time"

	"go.mongodb.org/mongo-driver/bson/primitive"
)

type Comment struct {
	ID        primitive.ObjectID `bson:"_id,omitempty" json:"id"`
	VideoID   string             `bson:"video_id" json:"video_id"`
	UserID    string             `bson:"user_id" json:"user_id"`
	Text      string             `bson:"text" json:"text"`
	ParentID  *string            `bson:"parent_id,omitempty" json:"parent_id,omitempty"`
	CreatedAt time.Time          `bson:"created_at" json:"created_at"`
	UpdatedAt time.Time          `bson:"updated_at" json:"updated_at"`
}

type CommentRequest struct {
	VideoID  string  `json:"video_id" binding:"required"`
	UserID   string  `json:"user_id" binding:"required"`
	Text     string  `json:"text" binding:"required,min=1,max=500"`
	ParentID *string `json:"parent_id,omitempty"`
}