package config

import "os"

type Config struct {
	Port           string
	UserService    string
	ContentService string
	SocialService  string
	SearchService  string
	RabbitMQURL    string
	FeedBatchSize  int
	PrefetchCount  int
	TimeoutSec     int
}

func LoadConfig() *Config {
	return &Config{
		Port:           getEnv("PORT", "8080"),
		UserService:    getEnv("USER_SERVICE", "http://user-service:8080"),
		ContentService: getEnv("CONTENT_SERVICE", "http://content-service:8080"),
		SocialService:  getEnv("SOCIAL_SERVICE", "http://social-service:8080"),
		SearchService:  getEnv("SEARCH_SERVICE", "http://search-service:8000"),
		RabbitMQURL:    getEnv("RABBITMQ_URL", "amqp://guest:guest@rabbitmq:5672"),
		FeedBatchSize:  30,
		PrefetchCount:  3,
		TimeoutSec:     5,
	}
}

func getEnv(key, defaultValue string) string {
	if value := os.Getenv(key); value != "" {
		return value
	}
	return defaultValue
}
