package models

import (
	"time"

	"go.mongodb.org/mongo-driver/bson/primitive"
)

type View struct {
	ID        primitive.ObjectID `bson:"_id,omitempty" json:"id"`
	VideoID   string             `bson:"video_id" json:"video_id"`
	UserID    string             `bson:"user_id" json:"user_id"`
	CreatedAt time.Time          `bson:"created_at" json:"created_at"`
}

type ViewRequest struct {
	VideoID string `json:"video_id" binding:"required"`
	UserID  string `json:"user_id" binding:"required"`
}