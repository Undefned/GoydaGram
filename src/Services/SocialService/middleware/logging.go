package middleware

import (
	"log"
	"time"

	"github.com/gin-gonic/gin"
)

func Logging() gin.HandlerFunc {
	return func(c *gin.Context) {
		start := time.Now()
		correlationID := c.GetString("correlation_id")

		c.Next()

		latency := time.Since(start)
		status := c.Writer.Status()

		log.Printf(
			`{"timestamp":"%s","level":"info","service":"social-service","correlation_id":"%s","method":"%s","path":"%s","status":%d,"latency":"%s"}`,
			time.Now().Format(time.RFC3339),
			correlationID,
			c.Request.Method,
			c.Request.URL.Path,
			status,
			latency,
		)
	}
}