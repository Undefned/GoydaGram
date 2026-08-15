package config

import "os"

type Config struct {
    Port         string
    MongoURI     string
    MongoDB      string
    RedisURL     string
    RedisPassword string
    RabbitMQURL  string
    ContentAPI   string
    UserAPI      string
}

func LoadConfig() *Config {
    return &Config{
        Port:          getEnv("PORT", "8080"),
        MongoURI:      getEnv("MONGO_URI", "mongodb://localhost:27017"),
        MongoDB:       getEnv("MONGO_DB", "socialdb"),
        RedisURL:      getEnv("REDIS_URL", "localhost:6379"),
        RedisPassword: getEnv("REDIS_PASSWORD", ""),
        RabbitMQURL:   getEnv("RABBITMQ_URL", "amqp://guest:guest@localhost:5672"),
        ContentAPI:    getEnv("CONTENT_API", "http://content-service:8080"),
        UserAPI:       getEnv("USER_API", "http://user-service:8080"),
    }
}

func getEnv(key, defaultValue string) string {
    if value := os.Getenv(key); value != "" {
        return value
    }
    return defaultValue
}