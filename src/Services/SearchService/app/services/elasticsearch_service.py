import json
from typing import List, Optional, Dict, Any
from datetime import datetime

from elasticsearch import Elasticsearch, exceptions
from tenacity import retry, stop_after_attempt, wait_exponential

from app.config import settings
from app.models import VideoDocument, UserDocument, SearchResponse
from app.schemas import VideoMetadata, UserMetadata, VideoStats

class ElasticsearchService:
    def __init__(self):
        self.client = Elasticsearch(
            [settings.elasticsearch_url],
            request_timeout=30,
            retry_on_timeout=True,
            max_retries=3
        )
        self._ensure_indices()

    def _ensure_indices(self):
        """Create indices if they don't exist"""
        # Videos index
        if not self.client.indices.exists(index=settings.elasticsearch_index_videos):
            self._create_video_index()
        
        # Users index
        if not self.client.indices.exists(index=settings.elasticsearch_index_users):
            self._create_user_index()

    def _create_video_index(self):
        """Create videos index with mappings"""
        index_settings = {
            "settings": {
                "number_of_shards": 1,
                "number_of_replicas": 0,
                "analysis": {
                    "analyzer": {
                        "english_analyzer": {
                            "type": "custom",
                            "tokenizer": "standard",
                            "filter": ["lowercase", "english_stemmer"]
                        }
                    },
                    "filter": {
                        "english_stemmer": {
                            "type": "stemmer",
                            "language": "english"
                        }
                    }
                }
            },
            "mappings": {
                "properties": {
                    "id": {"type": "keyword"},
                    "user_id": {"type": "keyword"},
                    "title": {
                        "type": "text",
                        "analyzer": "english_analyzer",
                        "fields": {
                            "keyword": {"type": "keyword"},
                            "ngram": {
                                "type": "text",
                                "analyzer": "english_analyzer"
                            }
                        }
                    },
                    "description": {
                        "type": "text",
                        "analyzer": "english_analyzer"
                    },
                    "username": {
                        "type": "text",
                        "analyzer": "english_analyzer",
                        "fields": {
                            "keyword": {"type": "keyword"}
                        }
                    },
                    "tags": {
                        "type": "text",
                        "fields": {
                            "keyword": {"type": "keyword"}
                        }
                    },
                    "duration": {"type": "integer"},
                    "url": {"type": "keyword"},
                    "preview_url": {"type": "keyword"},
                    "thumbnail_url": {"type": "keyword"},
                    "likes_count": {"type": "integer"},
                    "comments_count": {"type": "integer"},
                    "views_count": {"type": "integer"},
                    "engagement_score": {"type": "float"},
                    "is_verified": {"type": "boolean"},
                    "created_at": {"type": "date"},
                    "updated_at": {"type": "date"}
                }
            }
        }
        
        self.client.indices.create(
            index=settings.elasticsearch_index_videos,
            body=index_settings
        )

    def _create_user_index(self):
        """Create users index with mappings"""
        index_settings = {
            "settings": {
                "number_of_shards": 1,
                "number_of_replicas": 0,
                "analysis": {
                    "analyzer": {
                        "english_analyzer": {
                            "type": "custom",
                            "tokenizer": "standard",
                            "filter": ["lowercase", "english_stemmer"]
                        }
                    },
                    "filter": {
                        "english_stemmer": {
                            "type": "stemmer",
                            "language": "english"
                        }
                    }
                }
            },
            "mappings": {
                "properties": {
                    "id": {"type": "keyword"},
                    "username": {
                        "type": "text",
                        "analyzer": "english_analyzer",
                        "fields": {
                            "keyword": {"type": "keyword"}
                        }
                    },
                    "email": {"type": "keyword"},
                    "avatar_url": {"type": "keyword"},
                    "bio": {"type": "text", "analyzer": "english_analyzer"},
                    "is_verified": {"type": "boolean"},
                    "followers_count": {"type": "integer"},
                    "following_count": {"type": "integer"},
                    "created_at": {"type": "date"}
                }
            }
        }
        
        self.client.indices.create(
            index=settings.elasticsearch_index_users,
            body=index_settings
        )

    # ============ VIDEO OPERATIONS ============

    @retry(stop=stop_after_attempt(3), wait=wait_exponential(multiplier=1, min=2, max=10))
    def index_video(self, video: VideoDocument):
        """Index a video document"""
        try:
            self.client.index(
                index=settings.elasticsearch_index_videos,
                id=video.id,
                document=video.dict(),
                refresh=True
            )
        except exceptions.ElasticsearchException as e:
            print(f"Failed to index video {video.id}: {e}")
            raise

    @retry(stop=stop_after_attempt(3), wait=wait_exponential(multiplier=1, min=2, max=10))
    def delete_video(self, video_id: str):
        """Delete a video from index"""
        try:
            self.client.delete(
                index=settings.elasticsearch_index_videos,
                id=video_id,
                refresh=True,
                ignore=[404]
            )
        except exceptions.ElasticsearchException as e:
            print(f"Failed to delete video {video_id}: {e}")
            raise

    @retry(stop=stop_after_attempt(3), wait=wait_exponential(multiplier=1, min=2, max=10))
    def update_video_stats(self, video_id: str, stats: Dict[str, int]):
        """Update video stats (likes, views, comments)"""
        try:
            self.client.update(
                index=settings.elasticsearch_index_videos,
                id=video_id,
                body={"doc": stats},
                refresh=True
            )
        except exceptions.ElasticsearchException as e:
            print(f"Failed to update video {video_id}: {e}")
            raise

    @retry(stop=stop_after_attempt(3), wait=wait_exponential(multiplier=1, min=2, max=10))
    def search_videos(self, query: str, limit: int = 30, offset: int = 0, filters: Optional[dict] = None) -> SearchResponse:
        """Search videos by query"""
        must_queries = []
        
        if query and query.strip():
            must_queries.append({
                "multi_match": {
                    "query": query.strip(),
                    "fields": ["title^3", "description", "username^2", "tags^2"],
                    "fuzziness": settings.fuzziness
                }
            })
        
        # Add filters
        filter_queries = []
        if filters:
            if filters.get("user_id"):
                filter_queries.append({"term": {"user_id": filters["user_id"]}})
            if filters.get("tags"):
                filter_queries.append({"terms": {"tags": filters["tags"]}})
            if filters.get("min_views"):
                filter_queries.append({"range": {"views_count": {"gte": filters["min_views"]}}})
            if filters.get("min_likes"):
                filter_queries.append({"range": {"likes_count": {"gte": filters["min_likes"]}}})
            if filters.get("created_after"):
                filter_queries.append({"range": {"created_at": {"gte": filters["created_after"]}}})

        # Build query body
        body = {
            "from": offset,
            "size": min(limit, settings.max_results),
            "sort": [
                {"engagement_score": {"order": "desc"}},
                {"created_at": {"order": "desc"}}
            ]
        }
        
        if must_queries or filter_queries:
            body["query"] = {
                "bool": {}
            }
            if must_queries:
                body["query"]["bool"]["must"] = must_queries
            if filter_queries:
                body["query"]["bool"]["filter"] = filter_queries
        else:
            # No query - return latest videos
            body["query"] = {"match_all": {}}
            body["sort"] = [{"created_at": {"order": "desc"}}]

        try:
            response = self.client.search(
                index=settings.elasticsearch_index_videos,
                body=body
            )
            
            hits = response.get("hits", {})
            videos = []
            for hit in hits.get("hits", []):
                video = hit.get("_source", {})
                video["id"] = hit.get("_id")
                videos.append(VideoDocument(**video))
            
            return SearchResponse(
                videos=videos,
                total=hits.get("total", {}).get("value", 0),
                offset=offset,
                limit=limit
            )
        except exceptions.ElasticsearchException as e:
            print(f"Search failed: {e}")
            raise

    @retry(stop=stop_after_attempt(3), wait=wait_exponential(multiplier=1, min=2, max=10))
    def get_recommendations(self, interests: List[Dict], limit: int = 30) -> List[str]:
        """Get video recommendations based on user interests"""
        if not interests:
            return []
        
        # Extract tag weights
        tag_weights = {item["tag"]: item.get("weight", 1.0) for item in interests}
        tags = list(tag_weights.keys())
        
        if not tags:
            return []
        
        # Build query
        # Boosting by engagement score and tag relevance
        should_queries = []
        for tag, weight in tag_weights.items():
            should_queries.append({
                "match": {
                    "tags": {
                        "query": tag,
                        "boost": weight * 2.0
                    }
                }
            })
            should_queries.append({
                "match": {
                    "title": {
                        "query": tag,
                        "boost": weight * 1.5
                    }
                }
            })
        
        body = {
            "size": min(limit * 2, 100),
            "query": {
                "bool": {
                    "should": should_queries,
                    "minimum_should_match": 1,
                    "filter": [
                        {"term": {"is_verified": True}}  # Prefer verified content
                    ]
                }
            },
            "sort": [
                {"_score": {"order": "desc"}},
                {"engagement_score": {"order": "desc"}},
                {"created_at": {"order": "desc"}}
            ]
        }
        
        try:
            response = self.client.search(
                index=settings.elasticsearch_index_videos,
                body=body
            )
            
            hits = response.get("hits", {})
            video_ids = []
            for hit in hits.get("hits", [])[:limit]:
                video_ids.append(hit.get("_id"))
            
            return video_ids
        except exceptions.ElasticsearchException as e:
            print(f"Recommendations failed: {e}")
            return []

    # ============ USER OPERATIONS ============

    @retry(stop=stop_after_attempt(3), wait=wait_exponential(multiplier=1, min=2, max=10))
    def index_user(self, user: UserDocument):
        """Index a user document"""
        try:
            self.client.index(
                index=settings.elasticsearch_index_users,
                id=user.id,
                document=user.dict(),
                refresh=True
            )
        except exceptions.ElasticsearchException as e:
            print(f"Failed to index user {user.id}: {e}")
            raise

    @retry(stop=stop_after_attempt(3), wait=wait_exponential(multiplier=1, min=2, max=10))
    def delete_user(self, user_id: str):
        """Delete a user from index"""
        try:
            self.client.delete(
                index=settings.elasticsearch_index_users,
                id=user_id,
                refresh=True,
                ignore=[404]
            )
        except exceptions.ElasticsearchException as e:
            print(f"Failed to delete user {user_id}: {e}")
            raise

    @retry(stop=stop_after_attempt(3), wait=wait_exponential(multiplier=1, min=2, max=10))
    def search_users(self, query: str, limit: int = 30, offset: int = 0) -> List[UserDocument]:
        """Search users by query"""
        body = {
            "from": offset,
            "size": min(limit, settings.max_results),
            "query": {
                "multi_match": {
                    "query": query.strip(),
                    "fields": ["username^3", "bio^2"],
                    "fuzziness": settings.fuzziness
                }
            }
        }
        
        try:
            response = self.client.search(
                index=settings.elasticsearch_index_users,
                body=body
            )
            
            hits = response.get("hits", {})
            users = []
            for hit in hits.get("hits", []):
                user = hit.get("_source", {})
                user["id"] = hit.get("_id")
                users.append(UserDocument(**user))
            
            return users
        except exceptions.ElasticsearchException as e:
            print(f"Search users failed: {e}")
            raise