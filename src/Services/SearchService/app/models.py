from datetime import datetime
from typing import List, Optional
from pydantic import BaseModel

class VideoDocument(BaseModel):
    id: str
    user_id: str
    title: str
    description: str
    duration: int
    url: str
    preview_url: str
    thumbnail_url: Optional[str] = None
    username: str
    user_avatar_url: Optional[str] = None
    tags: List[str] = []
    likes_count: int = 0
    comments_count: int = 0
    views_count: int = 0
    engagement_score: float = 0.0
    is_verified: bool = False
    created_at: datetime
    updated_at: datetime

class UserDocument(BaseModel):
    id: str
    username: str
    email: str
    avatar_url: Optional[str] = None
    bio: Optional[str] = None
    is_verified: bool = False
    followers_count: int = 0
    following_count: int = 0
    created_at: datetime

class SearchRequest(BaseModel):
    query: str
    limit: int = 30
    offset: int = 0
    filters: Optional[dict] = None

class SearchResponse(BaseModel):
    videos: List[VideoDocument]
    total: int
    offset: int
    limit: int

class IndexVideoRequest(BaseModel):
    video_id: str

class IndexUserRequest(BaseModel):
    user_id: str

class RecommendationRequest(BaseModel):
    interests: List[dict]  # [{"tag": "funny", "weight": 0.8}]
    limit: int = 30