using System.Text;
using System.Text.Json;
using ContentService.Application.Events;
using ContentService.Application.Interfaces;

namespace ContentService.Infrastructure.Messaging;

public class RabbitMQEventPublisher : IEventPublisher
{
    private readonly RabbitMQConnection _connection;

    public RabbitMQEventPublisher(RabbitMQConnection connection)
    {
        _connection = connection;
    }

    public Task PublishAsync<T>(T @event) where T : class
    {
        using var channel = _connection.Connection.CreateModel();

        var json = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(json);

        var routingKey = @event.GetType().Name switch
        {
            nameof(VideoUploadedEvent) => "video.uploaded",
            nameof(VideoProcessedEvent) => "video.processed",
            _ => "video.unknown"
        };

        var props = channel.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";

        channel.BasicPublish(
            exchange: "video.events",
            routingKey: routingKey,
            mandatory: true,
            basicProperties: props,
            body: body);

        return Task.CompletedTask;
    }
}