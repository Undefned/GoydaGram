using System.Text;
using System.Text.Json;
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
        // connection._connection было private — не компилировалось. Используем публичное свойство.
        _connection = connection.Connection;
        _channel = _connection.CreateModel();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel.QueueDeclare("content.social.likes", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare("content.social.views", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare("content.social.comments", durable: true, exclusive: false, autoDelete: false);

        _channel.QueueBind("content.social.likes", "social.events", "social.liked");
        _channel.QueueBind("content.social.views", "social.events", "social.viewed");
        _channel.QueueBind("content.social.comments", "social.events", "social.commented");

        ConsumeQueue("content.social.likes", ProcessLikeEvent);
        ConsumeQueue("content.social.views", ProcessViewEvent);
        ConsumeQueue("content.social.comments", ProcessCommentEvent);

        return Task.CompletedTask;
    }

    private void ConsumeQueue(string queueName, Func<string, Task> processor)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                await processor(body);
                _channel.BasicAck(ea.DeliveryTag, false);
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

    private async Task ProcessLikeEvent(string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IVideoRepository>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var evt = JsonSerializer.Deserialize<SocialEvent>(message);
        if (evt == null) return;

        var video = await repository.GetByIdAsync(evt.VideoId);
        if (video == null) return;

        video.IncrementLikes();
        await repository.UpdateAsync(video);
        await repository.UnitOfWork.SaveChangesAsync();

        await cache.RemoveAsync($"video:{evt.VideoId}");
        await cache.RemoveAsync("trending:videos");
    }

    private async Task ProcessViewEvent(string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IVideoRepository>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var evt = JsonSerializer.Deserialize<SocialEvent>(message);
        if (evt == null) return;

        var video = await repository.GetByIdAsync(evt.VideoId);
        if (video == null) return;

        video.IncrementViews();
        await repository.UpdateAsync(video);
        await repository.UnitOfWork.SaveChangesAsync();

        await cache.RemoveAsync($"video:{evt.VideoId}");
        await cache.RemoveAsync("trending:videos");
    }

    private async Task ProcessCommentEvent(string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IVideoRepository>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var evt = JsonSerializer.Deserialize<SocialEvent>(message);
        if (evt == null) return;

        var video = await repository.GetByIdAsync(evt.VideoId);
        if (video == null) return;

        video.IncrementComments();
        await repository.UpdateAsync(video);
        await repository.UnitOfWork.SaveChangesAsync();

        await cache.RemoveAsync($"video:{evt.VideoId}");
        await cache.RemoveAsync("trending:videos");
    }

    public override void Dispose()
    {
        _channel?.Close();
        base.Dispose();
    }

    private class SocialEvent
    {
        public Guid VideoId { get; set; }
        public Guid UserId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}