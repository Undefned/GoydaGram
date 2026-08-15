import logging
import asyncio
from contextlib import asynccontextmanager

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.config import settings
from app.controllers import search
from app.middleware.correlation import CorrelationIDMiddleware
from app.services.elasticsearch_service import ElasticsearchService
from app.services.indexing_service import IndexingService
from app.consumers.rabbitmq_consumer import RabbitMQConsumer

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='{"timestamp":"%(asctime)s","level":"%(levelname)s","service":"search-service","message":"%(message)s"}',
    datefmt='%Y-%m-%dT%H:%M:%S.%fZ'
)
logger = logging.getLogger(__name__)

# Global variables
rabbitmq_consumer = None

@asynccontextmanager
async def lifespan(app: FastAPI):
    """Lifespan manager for startup/shutdown"""
    global rabbitmq_consumer
    
    # Startup
    logger.info("Starting Search Service...")
    
    # Initialize Elasticsearch
    es_service = ElasticsearchService()
    indexing_service = IndexingService(es_service)
    
    # Start RabbitMQ consumer
    rabbitmq_consumer = RabbitMQConsumer(indexing_service)
    rabbitmq_consumer.start()
    
    logger.info("Search Service started successfully")
    yield
    
    # Shutdown
    logger.info("Shutting down Search Service...")
    if rabbitmq_consumer:
        rabbitmq_consumer.stop()
    logger.info("Search Service stopped")

# Create FastAPI app
app = FastAPI(
    title="Search Service",
    description="Full-text search and recommendations for GoydaGram",
    version="1.0.0",
    lifespan=lifespan
)

# Middleware
app.add_middleware(CorrelationIDMiddleware)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Health check
@app.get("/health")
async def health_check():
    return {"status": "ok", "service": "search-service"}

@app.get("/healthz")
async def healthz():
    return {"status": "ok"}

# Include routers
app.include_router(search.router)