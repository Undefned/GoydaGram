import pytest
from unittest.mock import MagicMock, patch


class TestEngagementScore:
    """IndexingService._calculate_engagement is pure logic — easiest thing to unit test here."""

    def _make_service(self):
        from app.services.indexing_service import IndexingService
        return IndexingService(es_service=MagicMock())

    def test_zero_views_returns_zero(self):
        svc = self._make_service()
        assert svc._calculate_engagement(likes=10, comments=5, views=0) == 0.0

    def test_typical_engagement_calculation(self):
        svc = self._make_service()
        # (likes + comments) / views, rounded to 4 decimals
        result = svc._calculate_engagement(likes=50, comments=10, views=1000)
        assert result == 0.06

    def test_high_engagement_video(self):
        svc = self._make_service()
        result = svc._calculate_engagement(likes=800, comments=200, views=1000)
        assert result == 1.0


class TestSearchVideosQueryBuilding:
    """ElasticsearchService.search_videos — verify the query body is built correctly
    without hitting a real Elasticsearch cluster (client is mocked)."""

    def _make_service_with_mock_client(self):
        with patch("app.services.elasticsearch_service.Elasticsearch") as MockES:
            mock_client = MagicMock()
            MockES.return_value = mock_client
            # Skip index creation on init
            mock_client.indices.exists.return_value = True

            from app.services.elasticsearch_service import ElasticsearchService
            service = ElasticsearchService()
            return service, mock_client

    def test_empty_query_returns_latest_videos_sorted_by_created_at(self):
        service, mock_client = self._make_service_with_mock_client()
        mock_client.search.return_value = {"hits": {"hits": [], "total": {"value": 0}}}

        service.search_videos(query="", limit=10, offset=0)

        called_body = mock_client.search.call_args.kwargs["body"]
        assert called_body["query"] == {"match_all": {}}
        assert called_body["sort"] == [{"created_at": {"order": "desc"}}]

    def test_query_with_text_uses_multi_match(self):
        service, mock_client = self._make_service_with_mock_client()
        mock_client.search.return_value = {"hits": {"hits": [], "total": {"value": 0}}}

        service.search_videos(query="funny cats", limit=10, offset=0)

        called_body = mock_client.search.call_args.kwargs["body"]
        must = called_body["query"]["bool"]["must"]
        assert must[0]["multi_match"]["query"] == "funny cats"

    def test_filters_are_translated_to_term_and_range_queries(self):
        service, mock_client = self._make_service_with_mock_client()
        mock_client.search.return_value = {"hits": {"hits": [], "total": {"value": 0}}}

        service.search_videos(
            query="cats",
            limit=10,
            offset=0,
            filters={"user_id": "abc-123", "min_views": 100},
        )

        called_body = mock_client.search.call_args.kwargs["body"]
        filters = called_body["query"]["bool"]["filter"]
        assert {"term": {"user_id": "abc-123"}} in filters
        assert {"range": {"views_count": {"gte": 100}}} in filters


class TestGetRecommendations:
    def _make_service_with_mock_client(self):
        with patch("app.services.elasticsearch_service.Elasticsearch") as MockES:
            mock_client = MagicMock()
            MockES.return_value = mock_client
            mock_client.indices.exists.return_value = True

            from app.services.elasticsearch_service import ElasticsearchService
            service = ElasticsearchService()
            return service, mock_client

    def test_no_interests_returns_empty_list_without_calling_elasticsearch(self):
        service, mock_client = self._make_service_with_mock_client()

        result = service.get_recommendations(interests=[], limit=30)

        assert result == []
        mock_client.search.assert_not_called()

    def test_interests_produce_video_id_list_capped_at_limit(self):
        service, mock_client = self._make_service_with_mock_client()
        mock_client.search.return_value = {
            "hits": {"hits": [{"_id": f"video-{i}"} for i in range(5)]}
        }

        result = service.get_recommendations(
            interests=[{"tag": "funny", "weight": 0.9}], limit=3
        )

        assert result == ["video-0", "video-1", "video-2"]
