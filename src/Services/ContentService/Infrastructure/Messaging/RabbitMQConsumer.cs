using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ContentService.Domain.Interfaces;
using ContentService.Application.Interfaces;

namespace ContentService.Infrastructure.Messaging;

public class RabbitMQConsumer : BackgroundService
{
    private readonly ILogger<RabbitMQConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMQConsumer(ILogger<RabbitMQConsumer> logger, IServiceProvider serviceProvider, RabbitMQConnection connection)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _connection = connection.Connection;
        _channel = _connection.CreateModel();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting RabbitMQ consumers...");
        
        // Декларируем очереди
        _channel.QueueDeclare("content.social.likes", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare("content.social.views", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare("content.social.comments", durable: true, exclusive: false, autoDelete: false);

        _channel.QueueBind("content.social.likes", "social.events", "social.liked");
        _channel.QueueBind("content.social.views", "social.events", "social.viewed");
        _channel.QueueBind("content.social.comments", "social.events", "social.commented");

        // Запускаем consumers
        ConsumeQueue("content.social.likes", ProcessLikeEvent);
        ConsumeQueue("content.social.views", ProcessViewEvent);
        ConsumeQueue("content.social.comments", ProcessCommentEvent);
        
        _logger.LogInformation("All RabbitMQ consumers started");
        
        // Держим сервис живым
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private void ConsumeQueue(string queueName, Func<string, Task> processor)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                _logger.LogInformation("Received message from {QueueName}: {Message}", queueName, body);
                
                await processor(body);
                _channel.BasicAck(ea.DeliveryTag, false);
                
                _logger.LogInformation("Message processed successfully from {QueueName}", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from {QueueName}", queueName);
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume(queueName, autoAck: false, consumer);
        _logger.LogInformation("Started consuming from {QueueName}", queueName);
    }

    private async Task ProcessViewEvent(string message)
    {
        try
        {
            _logger.LogInformation("Processing view event: {Message}", message);
            
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IVideoRepository>();
            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

            // ✅ Правильная десериализация
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var evt = JsonSerializer.Deserialize<SocialEvent>(message, options);
            
            if (evt == null)
            {
                _logger.LogWarning("Failed to deserialize view event: {Message}", message);
                return;
            }

            _logger.LogInformation("Looking for video: {VideoId}", evt.VideoId);

            var video = await repository.GetByIdAsync(evt.VideoId);
            if (video == null)
            {
                _logger.LogWarning("Video {VideoId} not found", evt.VideoId);
                return;
            }

            // ✅ Увеличиваем счетчик просмотров
            video.IncrementViews();
            await repository.UpdateAsync(video);
            await repository.UnitOfWork.SaveChangesAsync();

            // Инвалидируем кеш
            await cache.RemoveAsync($"video:{evt.VideoId}");
            await cache.RemoveAsync("trending:videos");

            _logger.LogInformation("Video {VideoId} views updated to {ViewsCount}", evt.VideoId, video.ViewsCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process view event");
            throw;
        }
    }

    private async Task ProcessLikeEvent(string message)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IVideoRepository>();
            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var evt = JsonSerializer.Deserialize<SocialEvent>(message, options);
            if (evt == null) return;

            var video = await repository.GetByIdAsync(evt.VideoId);
            if (video == null) return;

            video.IncrementLikes();
            await repository.UpdateAsync(video);
            await repository.UnitOfWork.SaveChangesAsync();

            await cache.RemoveAsync($"video:{evt.VideoId}");
            await cache.RemoveAsync("trending:videos");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process like event");
            throw;
        }
    }

    private async Task ProcessCommentEvent(string message)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IVideoRepository>();
            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var evt = JsonSerializer.Deserialize<SocialEvent>(message, options);
            if (evt == null) return;

            var video = await repository.GetByIdAsync(evt.VideoId);
            if (video == null) return;

            video.IncrementComments();
            await repository.UpdateAsync(video);
            await repository.UnitOfWork.SaveChangesAsync();

            await cache.RemoveAsync($"video:{evt.VideoId}");
            await cache.RemoveAsync("trending:videos");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process comment event");
            throw;
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        base.Dispose();
    }

    // ✅ ИСПРАВЛЕННЫЙ КЛАСС С АТРИБУТАМИ
    private class SocialEvent
    {
        [JsonPropertyName("video_id")]
        public Guid VideoId { get; set; }
        
        [JsonPropertyName("user_id")]
        public Guid UserId { get; set; }
        
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
        
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}