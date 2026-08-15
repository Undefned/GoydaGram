from typing import Optional
from fastapi import APIRouter, HTTPException, Depends, Query

from app.models import SearchRequest, RecommendationRequest
from app.schemas import VideoMetadata, UserMetadata
from app.services.elasticsearch_service import ElasticsearchService
from app.services.indexing_service import IndexingService

router = APIRouter(prefix="/api/search", tags=["search"])

# Dependencies
def get_es_service():
    return ElasticsearchService()

def get_indexing_service(es_service: ElasticsearchService = Depends(get_es_service)):
    return IndexingService(es_service)

@router.get("/videos")
async def search_videos(
    q: str = Query(..., min_length=1),
    limit: int = Query(30, ge=1, le=100),
    offset: int = Query(0, ge=0),
    user_id: Optional[str] = None,
    tags: Optional[str] = None,
    es_service: ElasticsearchService = Depends(get_es_service)
):
    """Search videos by query"""
    try:
        # Build filters
        filters = {}
        if user_id:
            filters["user_id"] = user_id
        if tags:
            filters["tags"] = [t.strip() for t in tags.split(",")]
        
        result = es_service.search_videos(q, limit, offset, filters)
        return {
            "success": True,
            "data": {
                "videos": result.videos,
                "total": result.total,
                "offset": result.offset,
                "limit": result.limit
            }
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.get("/users")
async def search_users(
    q: str = Query(..., min_length=1),
    limit: int = Query(30, ge=1, le=100),
    offset: int = Query(0, ge=0),
    es_service: ElasticsearchService = Depends(get_es_service)
):
    """Search users by query"""
    try:
        users = es_service.search_users(q, limit, offset)
        return {
            "success": True,
            "data": {
                "users": users,
                "total": len(users),
                "offset": offset,
                "limit": limit
            }
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.post("/recommendations")
async def get_recommendations(
    request: RecommendationRequest,
    es_service: ElasticsearchService = Depends(get_es_service)
):
    """Get video recommendations based on interests"""
    try:
        interests = [{"tag": i.get("tag"), "weight": i.get("weight", 1.0)} for i in request.interests]
        video_ids = es_service.get_recommendations(interests, request.limit)
        return {
            "success": True,
            "data": {
                "video_ids": video_ids
            }
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.post("/index/video")
async def index_video(
    video_id: str = Query(...),
    indexing_service: IndexingService = Depends(get_indexing_service)
):
    """Index a video manually (for testing)"""
    try:
        await indexing_service.index_video(video_id)
        return {"success": True, "message": f"Video {video_id} indexed"}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.delete("/index/video")
async def delete_video(
    video_id: str = Query(...),
    indexing_service: IndexingService = Depends(get_indexing_service)
):
    """Delete a video from index (for testing)"""
    try:
        await indexing_service.delete_video(video_id)
        return {"success": True, "message": f"Video {video_id} deleted"}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))