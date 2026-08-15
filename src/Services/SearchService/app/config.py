import os
from pydantic_settings import BaseSettings
from dotenv import load_dotenv

load_dotenv()

class Settings(BaseSettings):
    # App
    app_name: str = "Search Service"
    debug: bool = False
    host: str = "0.0.0.0"
    port: int = 8000

    # Elasticsearch
    elasticsearch_url: str = os.getenv("ELASTICSEARCH_URL", "http://localhost:9200")
    elasticsearch_index_videos: str = "videos"
    elasticsearch_index_users: str = "users"
    
    # RabbitMQ
    rabbitmq_url: str = os.getenv("RABBITMQ_URL", "amqp://guest:guest@localhost:5672")
    
    # Service URLs
    content_service_url: str = os.getenv("CONTENT_SERVICE", "http://content-service:8080")
    user_service_url: str = os.getenv("USER_SERVICE", "http://user-service:8080")
    social_service_url: str = os.getenv("SOCIAL_SERVICE", "http://social-service:8080")
    
    # Search settings
    max_results: int = 100
    default_limit: int = 30
    fuzziness: str = "AUTO"
    
    class Config:
        env_file = ".env"

settings = Settings()