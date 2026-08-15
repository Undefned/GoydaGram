import json
import threading
import asyncio
from typing import Callable

import pika
from pika.adapters.blocking_connection import BlockingChannel

from app.config import settings
from app.services.indexing_service import IndexingService

class RabbitMQConsumer:
    def __init__(self, indexing_service: IndexingService):
        self.indexing_service = indexing_service
        self._connection = None
        self._channel = None
        self._running = False

    def start(self):
        """Start consuming messages from RabbitMQ"""
        self._running = True
        threading.Thread(target=self._consume, daemon=True).start()
        print("RabbitMQ consumer started")

    def _consume(self):
        """Main consumer loop"""
        while self._running:
            try:
                self._connect()
                self._channel.basic_consume(
                    queue="search.video.uploaded",
                    on_message_callback=self._on_video_uploaded,
                    auto_ack=False
                )
                self._channel.basic_consume(
                    queue="search.video.updated",
                    on_message_callback=self._on_video_updated,
                    auto_ack=False
                )
                self._channel.basic_consume(
                    queue="search.user.registered",
                    on_message_callback=self._on_user_registered,
                    auto_ack=False
                )
                self._channel.start_consuming()
            except Exception as e:
                print(f"RabbitMQ consumer error: {e}")
                import time
                time.sleep(5)

    def _connect(self):
        """Connect to RabbitMQ"""
        params = pika.URLParameters(settings.rabbitmq_url)
        self._connection = pika.BlockingConnection(params)
        self._channel = self._connection.channel()
        
        # Declare exchanges
        self._channel.exchange_declare(
            exchange="video.events",
            exchange_type="topic",
            durable=True
        )
        self._channel.exchange_declare(
            exchange="user.events",
            exchange_type="topic",
            durable=True
        )
        
        # Declare queues
        self._channel.queue_declare(
            queue="search.video.uploaded",
            durable=True
        )
        self._channel.queue_declare(
            queue="search.video.updated",
            durable=True
        )
        self._channel.queue_declare(
            queue="search.user.registered",
            durable=True
        )
        
        # Bind queues
        self._channel.queue_bind(
            queue="search.video.uploaded",
            exchange="video.events",
            routing_key="video.uploaded"
        )
        self._channel.queue_bind(
            queue="search.video.updated",
            exchange="video.events",
            routing_key="video.updated"
        )
        self._channel.queue_bind(
            queue="search.user.registered",
            exchange="user.events",
            routing_key="user.registered"
        )

    def _on_video_uploaded(self, channel: BlockingChannel, method, properties, body):
        """Handle video.uploaded event"""
        try:
            data = json.loads(body)
            video_id = data.get("video_id")
            if video_id:
                # Run async indexing in thread
                threading.Thread(
                    target=lambda: asyncio.run(
                        self.indexing_service.index_video(video_id)
                    )
                ).start()
                channel.basic_ack(delivery_tag=method.delivery_tag)
                print(f"Video uploaded event processed: {video_id}")
        except Exception as e:
            print(f"Error processing video.uploaded event: {e}")
            channel.basic_nack(delivery_tag=method.delivery_tag, requeue=True)

    def _on_video_updated(self, channel: BlockingChannel, method, properties, body):
        """Handle video.updated event"""
        try:
            data = json.loads(body)
            video_id = data.get("video_id")
            if video_id:
                threading.Thread(
                    target=lambda: asyncio.run(
                        self.indexing_service.index_video(video_id)
                    )
                ).start()
                channel.basic_ack(delivery_tag=method.delivery_tag)
                print(f"Video updated event processed: {video_id}")
        except Exception as e:
            print(f"Error processing video.updated event: {e}")
            channel.basic_nack(delivery_tag=method.delivery_tag, requeue=True)

    def _on_user_registered(self, channel: BlockingChannel, method, properties, body):
        """Handle user.registered event"""
        try:
            data = json.loads(body)
            user_id = data.get("UserId") or data.get("user_id")
            if user_id:
                threading.Thread(
                    target=lambda: asyncio.run(
                        self.indexing_service.index_user(user_id)
                    )
                ).start()
                channel.basic_ack(delivery_tag=method.delivery_tag)
                print(f"User registered event processed: {user_id}")
        except Exception as e:
            print(f"Error processing user.registered event: {e}")
            channel.basic_nack(delivery_tag=method.delivery_tag, requeue=True)

    def stop(self):
        """Stop the consumer"""
        self._running = False
        if self._channel:
            self._channel.stop_consuming()
        if self._connection:
            self._connection.close()
        print("RabbitMQ consumer stopped")