import json
import threading
import asyncio
import time
from typing import Callable
import logging

import pika
from pika.adapters.blocking_connection import BlockingChannel
from pika.exceptions import AMQPConnectionError, ChannelClosedByBroker

from app.config import settings
from app.services.indexing_service import IndexingService

logger = logging.getLogger(__name__)

class RabbitMQConsumer:
    def __init__(self, indexing_service: IndexingService):
        self.indexing_service = indexing_service
        self._connection = None
        self._channel = None
        self._running = False
        self._reconnect_delay = 5
        self._max_reconnect_delay = 60

    def start(self):
        """Start consuming messages from RabbitMQ"""
        self._running = True
        threading.Thread(target=self._consume, daemon=True).start()
        logger.info("RabbitMQ consumer started")

    def _consume(self):
        """Main consumer loop with reconnect logic"""
        while self._running:
            try:
                self._connect()
                logger.info("Connected to RabbitMQ, starting consumption...")
                
                # Настраиваем QoS (важно для предотвращения переполнения)
                self._channel.basic_qos(prefetch_count=1)
                
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
                
                # Блокирующий вызов
                self._channel.start_consuming()
                
            except (AMQPConnectionError, ChannelClosedByBroker, Exception) as e:
                if not self._running:
                    break
                logger.error(f"RabbitMQ consumer error: {e}")
                self._reconnect_delay = min(self._reconnect_delay * 2, self._max_reconnect_delay)
                logger.info(f"Reconnecting in {self._reconnect_delay}s...")
                time.sleep(self._reconnect_delay)
            finally:
                self._cleanup()

    def _connect(self):
        """Connect to RabbitMQ with timeout"""
        try:
            params = pika.URLParameters(settings.rabbitmq_url)
            # Добавляем таймауты
            params.connection_attempts = 3
            params.retry_delay = 2
            params.socket_timeout = 10
            
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
            
            # Сбрасываем задержку при успешном подключении
            self._reconnect_delay = 5
            
        except Exception as e:
            logger.error(f"Failed to connect to RabbitMQ: {e}")
            raise

    def _cleanup(self):
        """Clean up connection"""
        try:
            if self._channel and self._channel.is_open:
                self._channel.stop_consuming()
                self._channel.close()
        except Exception as e:
            logger.debug(f"Error during channel cleanup: {e}")
        
        try:
            if self._connection and self._connection.is_open:
                self._connection.close()
        except Exception as e:
            logger.debug(f"Error during connection cleanup: {e}")
        
        self._channel = None
        self._connection = None

    def _on_video_uploaded(self, channel: BlockingChannel, method, properties, body):
        """Handle video.uploaded event"""
        try:
            data = json.loads(body)
            video_id = data.get("video_id")
            if video_id:
                logger.info(f"Processing video upload: {video_id}")
                # Запускаем в отдельном потоке
                thread = threading.Thread(
                    target=lambda: asyncio.run(
                        self.indexing_service.index_video(video_id)
                    ),
                    daemon=True
                )
                thread.start()
                # Сразу подтверждаем, чтобы не блокировать очередь
                channel.basic_ack(delivery_tag=method.delivery_tag)
                logger.info(f"Video {video_id} queued for indexing")
        except Exception as e:
            logger.error(f"Error processing video.uploaded event: {e}")
            channel.basic_nack(delivery_tag=method.delivery_tag, requeue=True)

    def _on_video_updated(self, channel: BlockingChannel, method, properties, body):
        """Handle video.updated event"""
        try:
            data = json.loads(body)
            video_id = data.get("video_id")
            if video_id:
                logger.info(f"Processing video update: {video_id}")
                thread = threading.Thread(
                    target=lambda: asyncio.run(
                        self.indexing_service.index_video(video_id)
                    ),
                    daemon=True
                )
                thread.start()
                channel.basic_ack(delivery_tag=method.delivery_tag)
                logger.info(f"Video {video_id} updated in index")
        except Exception as e:
            logger.error(f"Error processing video.updated event: {e}")
            channel.basic_nack(delivery_tag=method.delivery_tag, requeue=True)

    def _on_user_registered(self, channel: BlockingChannel, method, properties, body):
        """Handle user.registered event"""
        try:
            data = json.loads(body)
            user_id = data.get("UserId") or data.get("user_id")
            if user_id:
                logger.info(f"Processing user registration: {user_id}")
                thread = threading.Thread(
                    target=lambda: asyncio.run(
                        self.indexing_service.index_user(user_id)
                    ),
                    daemon=True
                )
                thread.start()
                channel.basic_ack(delivery_tag=method.delivery_tag)
                logger.info(f"User {user_id} indexed")
        except Exception as e:
            logger.error(f"Error processing user.registered event: {e}")
            channel.basic_nack(delivery_tag=method.delivery_tag, requeue=True)

    def stop(self):
        """Stop the consumer"""
        self._running = False
        self._cleanup()
        logger.info("RabbitMQ consumer stopped")