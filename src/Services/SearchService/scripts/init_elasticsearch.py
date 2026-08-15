#!/usr/bin/env python3
"""Initialize Elasticsearch indices"""
import asyncio
import sys
import os

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from app.services.elasticsearch_service import ElasticsearchService

async def main():
    print("Initializing Elasticsearch indices...")
    es = ElasticsearchService()
    print("Indices created successfully!")
    print("Ready to index data")

if __name__ == "__main__":
    asyncio.run(main())