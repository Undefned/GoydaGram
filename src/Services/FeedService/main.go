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

	"feed-service/clients"
	"feed-service/config"
	"feed-service/controllers"
	"feed-service/middleware"
	"feed-service/services"

	// Swagger imports
	_ "feed-service/docs"

	swaggerFiles "github.com/swaggo/files"
	ginSwagger "github.com/swaggo/gin-swagger"
)

// @title Feed Service API
// @version 1.0
// @description Feed generation service for GoydaGram. Provides personalized and trending feeds.
// @termsOfService http://swagger.io/terms/

// @contact.name API Support
// @contact.email support@goydagram.com
// @contact.url http://goydagram.com/support

// @license.name MIT
// @license.url https://opensource.org/licenses/MIT

// @host localhost:5004
// @BasePath /api
// @schemes http https

// @securityDefinitions.apikey BearerAuth
// @in header
// @name Authorization
// @description Type "Bearer" followed by a space and JWT token.

func main() {
	if err := godotenv.Load(); err != nil {
		log.Println("No .env file found, using system env")
	}

	cfg := config.LoadConfig()
	timeout := time.Duration(cfg.TimeoutSec) * time.Second

	// Initialize HTTP clients
	userClient := clients.NewUserClient(cfg.UserService, timeout)
	contentClient := clients.NewContentClient(cfg.ContentService, timeout)
	socialClient := clients.NewSocialClient(cfg.SocialService, timeout)
	searchClient := clients.NewSearchClient(cfg.SearchService, timeout)

	// Initialize services
	feedService := services.NewFeedService(
		userClient,
		contentClient,
		socialClient,
		searchClient,
		cfg.FeedBatchSize,
	)

	prefetchService := services.NewPrefetchService(feedService, 5)

	// Initialize Gin
	gin.SetMode(gin.ReleaseMode)
	r := gin.New()

	// Middleware
	r.Use(middleware.CorrelationID())
	r.Use(middleware.Logging())
	r.Use(gin.Recovery())

	// Health check
	r.GET("/health", func(c *gin.Context) {
		c.JSON(http.StatusOK, gin.H{
			"status":  "ok",
			"service": "feed-service",
			"version": "1.0",
		})
	})

	// Swagger endpoint
	r.GET("/swagger/*any", ginSwagger.WrapHandler(swaggerFiles.Handler))
	r.GET("/metrics", gin.WrapH(promhttp.Handler()))

	// Routes
	feedCtrl := controllers.NewFeedController(feedService, prefetchService)

	api := r.Group("/api")
	{
		api.GET("/feed", feedCtrl.GetFeed)
		api.GET("/feed/trending", feedCtrl.GetTrending)
		api.GET("/feed/prefetch", feedCtrl.Prefetch)
	}

	// Start server
	srv := &http.Server{
		Addr:    ":" + cfg.Port,
		Handler: r,
	}

	go func() {
		log.Printf("Feed Service running on port %s", cfg.Port)
		log.Printf("Swagger UI available at: http://localhost:%s/swagger/index.html", cfg.Port)
		if err := srv.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			log.Fatalf("Failed to start server: %v", err)
		}
	}()

	// Graceful shutdown
	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)
	<-quit

	log.Println("Shutting down gracefully...")

	ctxShutdown, cancelShutdown := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancelShutdown()

	prefetchService.Close()

	if err := srv.Shutdown(ctxShutdown); err != nil {
		log.Fatalf("Server shutdown error: %v", err)
	}

	log.Println("Feed Service stopped")
}
