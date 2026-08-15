from pydantic import BaseModel
from typing import List, Optional

class VideoMetadata(BaseModel):
    id: str
    user_id: str
    title: str
    description: str
    duration: int
    url: str
    preview_url: str
    tags: List[str] = []
    created_at: str
    status: str

class UserMetadata(BaseModel):
    id: str
    username: str
    email: str
    avatar_url: Optional[str] = None
    bio: Optional[str] = None
    is_verified: bool = False
    created_at: str

class VideoStats(BaseModel):
    video_id: str
    likes_count: int = 0
    comments_count: int = 0
    views_count: int = 0