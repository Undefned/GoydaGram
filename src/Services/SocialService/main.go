package main

import (
	"context"
	"log"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/gin-gonic/gin"
	"github.com/joho/godotenv"
	"github.com/prometheus/client_golang/prometheus/promhttp"
	"go.mongodb.org/mongo-driver/mongo"
	"go.mongodb.org/mongo-driver/mongo/options"

	"social-service/cache"
	"social-service/config"
	"social-service/controllers"
	"social-service/messaging"
	"social-service/middleware"
	"social-service/repository"
	"social-service/services"

	// Swagger imports
	swaggerFiles "github.com/swaggo/files"
	ginSwagger "github.com/swaggo/gin-swagger"

	// Путь к сгенерированной документации
	_ "social-service/docs"
)

// @title Social Service API
// @version 1.0
// @description Social interaction service for GoydaGram (likes, comments, views, interests)
// @termsOfService http://swagger.io/terms/

// @contact.name API Support
// @contact.email support@goydagram.com

// @license.name MIT
// @license.url https://opensource.org/licenses/MIT

// @host localhost:5003
// @BasePath /api
// @schemes http https

func main() {
	if err := godotenv.Load(); err != nil {
		log.Println("No .env file found, using system env")
	}

	cfg := config.LoadConfig()

	// Connect to MongoDB
	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	client, err := mongo.Connect(ctx, options.Client().ApplyURI(cfg.MongoURI))
	if err != nil {
		log.Fatalf("Failed to connect to MongoDB: %v", err)
	}

	if err := client.Ping(ctx, nil); err != nil {
		log.Fatalf("MongoDB ping failed: %v", err)
	}
	log.Println("Connected to MongoDB")

	db := client.Database(cfg.MongoDB)

	// Connect to Redis
	redisCache, err := cache.NewRedisCache(cfg.RedisURL, cfg.RedisPassword)
	if err != nil {
		log.Printf("Failed to connect to Redis: %v (continuing without cache)", err)
		redisCache = nil
	} else {
		log.Println("Connected to Redis")
	}

	// Initialize repositories
	mongoRepo := repository.NewMongoRepository(db)
	redisRepo := repository.NewRedisRepository(redisCache)

	// Initialize RabbitMQ
	rabbitMQ, err := messaging.NewRabbitMQ(cfg.RabbitMQURL)
	if err != nil {
		log.Fatalf("Failed to connect to RabbitMQ: %v", err)
	}
	defer rabbitMQ.Close()
	log.Println("Connected to RabbitMQ")

	// Initialize services
	likeService := services.NewLikeService(mongoRepo, redisRepo, rabbitMQ)
	commentService := services.NewCommentService(mongoRepo, redisRepo, rabbitMQ)
	viewService := services.NewViewService(mongoRepo, redisRepo, rabbitMQ)
	interestService := services.NewInterestService(mongoRepo, redisRepo)

	// Initialize Gin
	gin.SetMode(gin.ReleaseMode)
	r := gin.New()

	r.Use(middleware.CorrelationID())
	r.Use(middleware.Logging())
	r.Use(gin.Recovery())

	// Health check
	r.GET("/health", func(c *gin.Context) {
		c.JSON(http.StatusOK, gin.H{"status": "ok", "service": "social-service"})
	})

	// Swagger endpoint
	r.GET("/swagger/*any", ginSwagger.WrapHandler(swaggerFiles.Handler))
	r.GET("/metrics", gin.WrapH(promhttp.Handler()))

	api := r.Group("/api")
	{
		likesCtrl := controllers.NewLikesController(likeService)
		api.POST("/likes", likesCtrl.Like)
		api.DELETE("/likes", likesCtrl.Unlike)
		api.GET("/videos/:videoId/likes", likesCtrl.GetVideoLikes)
		api.GET("/videos/:videoId/likes/count", likesCtrl.GetVideoLikesCount)

		commentsCtrl := controllers.NewCommentsController(commentService)
		api.POST("/comments", commentsCtrl.Comment)
		api.DELETE("/comments/:commentId", commentsCtrl.DeleteComment)
		api.GET("/videos/:videoId/comments", commentsCtrl.GetVideoComments)
		api.GET("/videos/:videoId/comments/count", commentsCtrl.GetVideoCommentsCount)

		viewsCtrl := controllers.NewViewsController(viewService)
		api.POST("/views", viewsCtrl.View)
		api.GET("/videos/:videoId/views", viewsCtrl.GetVideoViews)
		api.GET("/videos/:videoId/views/count", viewsCtrl.GetVideoViewsCount)

		interestsCtrl := controllers.NewInterestsController(interestService)
		api.GET("/users/:userId/interests", interestsCtrl.GetUserInterests)
		api.POST("/users/:userId/interests/refresh", interestsCtrl.RefreshInterests)
	}

	srv := &http.Server{
		Addr:    ":" + cfg.Port,
		Handler: r,
	}

	go func() {
		log.Printf("Social Service running on port %s", cfg.Port)
		if err := srv.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			log.Fatalf("Failed to start server: %v", err)
		}
	}()

	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)
	<-quit

	log.Println("Shutting down gracefully...")

	ctxShutdown, cancelShutdown := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancelShutdown()

	if err := srv.Shutdown(ctxShutdown); err != nil {
		log.Fatalf("Server shutdown error: %v", err)
	}

	if err := client.Disconnect(ctxShutdown); err != nil {
		log.Printf("MongoDB disconnect error: %v", err)
	}

	if redisCache != nil {
		if err := redisCache.Close(); err != nil {
			log.Printf("Redis close error: %v", err)
		}
	}

	log.Println("Social Service stopped")
}
