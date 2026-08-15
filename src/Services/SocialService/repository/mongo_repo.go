package repository

import (
	"context"
	"time"

	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/bson/primitive"
	"go.mongodb.org/mongo-driver/mongo"
	"go.mongodb.org/mongo-driver/mongo/options"

	"social-service/models"
)

type MongoRepository struct {
	db *mongo.Database
}

func NewMongoRepository(db *mongo.Database) *MongoRepository {
	return &MongoRepository{db: db}
}

// ============ LIKES ============

func (r *MongoRepository) Like(ctx context.Context, videoID, userID string) error {
	like := models.Like{
		VideoID:   videoID,
		UserID:    userID,
		CreatedAt: time.Now(),
	}

	_, err := r.db.Collection("likes").InsertOne(ctx, like)
	return err
}

func (r *MongoRepository) Unlike(ctx context.Context, videoID, userID string) error {
	filter := bson.M{"video_id": videoID, "user_id": userID}
	_, err := r.db.Collection("likes").DeleteOne(ctx, filter)
	return err
}

func (r *MongoRepository) GetVideoLikes(ctx context.Context, videoID string, limit, offset int64) ([]models.Like, int64, error) {
	filter := bson.M{"video_id": videoID}

	total, err := r.db.Collection("likes").CountDocuments(ctx, filter)
	if err != nil {
		return nil, 0, err
	}

	opts := options.Find().
		SetSort(bson.D{{Key: "created_at", Value: -1}}).
		SetLimit(limit).
		SetSkip(offset)

	cursor, err := r.db.Collection("likes").Find(ctx, filter, opts)
	if err != nil {
		return nil, 0, err
	}
	defer cursor.Close(ctx)

	var likes []models.Like
	if err := cursor.All(ctx, &likes); err != nil {
		return nil, 0, err
	}

	return likes, total, nil
}

func (r *MongoRepository) GetVideoLikesCount(ctx context.Context, videoID string) (int64, error) {
	filter := bson.M{"video_id": videoID}
	return r.db.Collection("likes").CountDocuments(ctx, filter)
}

func (r *MongoRepository) UserLiked(ctx context.Context, videoID, userID string) (bool, error) {
	filter := bson.M{"video_id": videoID, "user_id": userID}
	count, err := r.db.Collection("likes").CountDocuments(ctx, filter)
	return count > 0, err
}

// ============ COMMENTS ============

func (r *MongoRepository) Comment(ctx context.Context, comment models.Comment) (string, error) {
	comment.ID = primitive.NewObjectID()
	comment.CreatedAt = time.Now()
	comment.UpdatedAt = time.Now()

	_, err := r.db.Collection("comments").InsertOne(ctx, comment)
	return comment.ID.Hex(), err
}

func (r *MongoRepository) DeleteComment(ctx context.Context, commentID string) error {
	id, err := primitive.ObjectIDFromHex(commentID)
	if err != nil {
		return err
	}

	filter := bson.M{"_id": id}
	_, err = r.db.Collection("comments").DeleteOne(ctx, filter)
	return err
}

func (r *MongoRepository) GetVideoComments(ctx context.Context, videoID string, limit, offset int64) ([]models.Comment, int64, error) {
	filter := bson.M{"video_id": videoID}

	total, err := r.db.Collection("comments").CountDocuments(ctx, filter)
	if err != nil {
		return nil, 0, err
	}

	opts := options.Find().
		SetSort(bson.D{{Key: "created_at", Value: -1}}).
		SetLimit(limit).
		SetSkip(offset)

	cursor, err := r.db.Collection("comments").Find(ctx, filter, opts)
	if err != nil {
		return nil, 0, err
	}
	defer cursor.Close(ctx)

	var comments []models.Comment
	if err := cursor.All(ctx, &comments); err != nil {
		return nil, 0, err
	}

	return comments, total, nil
}

func (r *MongoRepository) GetVideoCommentsCount(ctx context.Context, videoID string) (int64, error) {
	filter := bson.M{"video_id": videoID}
	return r.db.Collection("comments").CountDocuments(ctx, filter)
}

func (r *MongoRepository) GetCommentsByParent(ctx context.Context, parentID string, limit, offset int64) ([]models.Comment, int64, error) {
	filter := bson.M{"parent_id": parentID}

	total, err := r.db.Collection("comments").CountDocuments(ctx, filter)
	if err != nil {
		return nil, 0, err
	}

	opts := options.Find().
		SetSort(bson.D{{Key: "created_at", Value: -1}}).
		SetLimit(limit).
		SetSkip(offset)

	cursor, err := r.db.Collection("comments").Find(ctx, filter, opts)
	if err != nil {
		return nil, 0, err
	}
	defer cursor.Close(ctx)

	var comments []models.Comment
	if err := cursor.All(ctx, &comments); err != nil {
		return nil, 0, err
	}

	return comments, total, nil
}

// ============ VIEWS ============

func (r *MongoRepository) View(ctx context.Context, videoID, userID string) error {
	filter := bson.M{"video_id": videoID, "user_id": userID}
	count, err := r.db.Collection("views").CountDocuments(ctx, filter)
	if err != nil {
		return err
	}

	if count > 0 {
		return nil
	}

	view := models.View{
		VideoID:   videoID,
		UserID:    userID,
		CreatedAt: time.Now(),
	}

	_, err = r.db.Collection("views").InsertOne(ctx, view)
	return err
}

func (r *MongoRepository) GetVideoViews(ctx context.Context, videoID string, limit, offset int64) ([]models.View, int64, error) {
	filter := bson.M{"video_id": videoID}

	total, err := r.db.Collection("views").CountDocuments(ctx, filter)
	if err != nil {
		return nil, 0, err
	}

	opts := options.Find().
		SetSort(bson.D{{Key: "created_at", Value: -1}}).
		SetLimit(limit).
		SetSkip(offset)

	cursor, err := r.db.Collection("views").Find(ctx, filter, opts)
	if err != nil {
		return nil, 0, err
	}
	defer cursor.Close(ctx)

	var views []models.View
	if err := cursor.All(ctx, &views); err != nil {
		return nil, 0, err
	}

	return views, total, nil
}

func (r *MongoRepository) GetVideoViewsCount(ctx context.Context, videoID string) (int64, error) {
	filter := bson.M{"video_id": videoID}
	return r.db.Collection("views").CountDocuments(ctx, filter)
}

// ============ INTERESTS ============

func (r *MongoRepository) GetUserInterests(ctx context.Context, userID string) (*models.UserInterest, error) {
	filter := bson.M{"user_id": userID}

	var interest models.UserInterest
	err := r.db.Collection("user_interests").FindOne(ctx, filter).Decode(&interest)
	if err != nil {
		if err == mongo.ErrNoDocuments {
			return &models.UserInterest{
				UserID:    userID,
				Interests: []models.Interest{},
			}, nil
		}
		return nil, err
	}

	return &interest, nil
}

func (r *MongoRepository) UpdateUserInterests(ctx context.Context, userID string, interests []models.Interest) error {
	filter := bson.M{"user_id": userID}
	update := bson.M{
		"$set": bson.M{
			"interests":  interests,
			"updated_at": time.Now(),
		},
	}

	opts := options.Update().SetUpsert(true)
	_, err := r.db.Collection("user_interests").UpdateOne(ctx, filter, update, opts)
	return err
}

func (r *MongoRepository) GetUsersWithInterests(ctx context.Context, limit int64) ([]models.UserInterest, error) {
	opts := options.Find().
		SetSort(bson.D{{Key: "updated_at", Value: -1}}).
		SetLimit(limit)

	cursor, err := r.db.Collection("user_interests").Find(ctx, bson.M{}, opts)
	if err != nil {
		return nil, err
	}
	defer cursor.Close(ctx)

	var interests []models.UserInterest
	if err := cursor.All(ctx, &interests); err != nil {
		return nil, err
	}

	return interests, nil
}
