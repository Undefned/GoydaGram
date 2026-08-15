package models

import (
	"time"

	"go.mongodb.org/mongo-driver/bson/primitive"
)

type Interest struct {
	Tag    string  `bson:"tag" json:"tag"`
	Weight float64 `bson:"weight" json:"weight"`
}

type UserInterest struct {
	ID        primitive.ObjectID `bson:"_id,omitempty" json:"id"`
	UserID    string             `bson:"user_id" json:"user_id"`
	Interests []Interest         `bson:"interests" json:"interests"`
	UpdatedAt time.Time          `bson:"updated_at" json:"updated_at"`
}

type InterestWeight struct {
	Tag    string  `json:"tag"`
	Weight float64 `json:"weight"`
}