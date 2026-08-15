import httpx
from typing import Optional, Dict
from datetime import datetime

from app.config import settings
from app.models import VideoDocument, UserDocument
from app.services.elasticsearch_service import ElasticsearchService

class IndexingService:
    def __init__(self, es_service: ElasticsearchService):
        self.es_service = es_service
        self.http_client = httpx.Client(timeout=30.0)

    async def index_video(self, video_id: str):
        """Fetch video data from Content Service and index it"""
        try:
            # Get video metadata
            response = self.http_client.get(
                f"{settings.content_service_url}/api/videos/{video_id}"
            )
            
            if response.status_code != 200:
                print(f"Failed to fetch video {video_id}: {response.status_code}")
                return
            
            video_data = response.json()
            video_metadata = video_data.get("data", video_data)
            
            # Get video stats from Social Service
            stats_response = self.http_client.get(
                f"{settings.social_service_url}/api/videos/{video_id}/stats"
            )
            
            stats = {}
            if stats_response.status_code == 200:
                stats_data = stats_response.json().get("data", {})
                stats = {
                    "likes_count": stats_data.get("likes", 0),
                    "comments_count": stats_data.get("comments", 0),
                    "views_count": stats_data.get("views", 0)
                }
            
            # Get user data
            user_id = video_metadata.get("user_id")
            user_response = self.http_client.get(
                f"{settings.user_service_url}/api/users/{user_id}"
            )
            
            user_data = {}
            if user_response.status_code == 200:
                user_data = user_response.json()
                user_data = user_data.get("data", user_data)
            
            # Build video document
            video_doc = VideoDocument(
                id=video_metadata.get("id"),
                user_id=video_metadata.get("user_id"),
                title=video_metadata.get("title", ""),
                description=video_metadata.get("description", ""),
                duration=video_metadata.get("duration", 0),
                url=video_metadata.get("url", ""),
                preview_url=video_metadata.get("preview_url", ""),
                username=user_data.get("username", ""),
                user_avatar_url=user_data.get("avatar_url"),
                tags=video_metadata.get("tags", []),
                likes_count=stats.get("likes_count", 0),
                comments_count=stats.get("comments_count", 0),
                views_count=stats.get("views_count", 0),
                engagement_score=self._calculate_engagement(
                    stats.get("likes_count", 0),
                    stats.get("comments_count", 0),
                    stats.get("views_count", 0)
                ),
                is_verified=user_data.get("is_verified", False),
                created_at=datetime.fromisoformat(video_metadata.get("created_at", datetime.now().isoformat())),
                updated_at=datetime.now()
            )
            
            # Index in Elasticsearch
            self.es_service.index_video(video_doc)
            print(f"Video {video_id} indexed successfully")
            
        except Exception as e:
            print(f"Failed to index video {video_id}: {e}")

    async def index_user(self, user_id: str):
        """Fetch user data from User Service and index it"""
        try:
            response = self.http_client.get(
                f"{settings.user_service_url}/api/users/{user_id}"
            )
            
            if response.status_code != 200:
                print(f"Failed to fetch user {user_id}: {response.status_code}")
                return
            
            user_data = response.json()
            user_data = user_data.get("data", user_data)
            
            user_doc = UserDocument(
                id=user_data.get("id"),
                username=user_data.get("username", ""),
                email=user_data.get("email", ""),
                avatar_url=user_data.get("avatar_url"),
                bio=user_data.get("bio"),
                is_verified=user_data.get("is_verified", False),
                followers_count=user_data.get("followers_count", 0),
                following_count=user_data.get("following_count", 0),
                created_at=datetime.fromisoformat(user_data.get("created_at", datetime.now().isoformat()))
            )
            
            self.es_service.index_user(user_doc)
            print(f"User {user_id} indexed successfully")
            
        except Exception as e:
            print(f"Failed to index user {user_id}: {e}")

    async def delete_video(self, video_id: str):
        """Delete video from index"""
        try:
            self.es_service.delete_video(video_id)
            print(f"Video {video_id} deleted from index")
        except Exception as e:
            print(f"Failed to delete video {video_id}: {e}")

    def _calculate_engagement(self, likes: int, comments: int, views: int) -> float:
        """Calculate engagement score for a video"""
        if views == 0:
            return 0.0
        return round((likes + comments) / views, 4)