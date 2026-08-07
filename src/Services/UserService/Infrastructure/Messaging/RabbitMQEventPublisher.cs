using System.Text;
using System.Text.Json;
using UserService.Application.Interfaces;
using UserService.Application.Events;

namespace UserService.Infrastructure.Messaging;

public class RabbitMQEventPublisher : IEventPublisher
{
    private readonly RabbitMQConnection _connection;

    public RabbitMQEventPublisher(RabbitMQConnection connection)
    {
        _connection = connection;
    }

    public Task PublishAsync<T>(T @event) where T : class
    {
        var channel = _connection.Channel;
        var json = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(json);

        var routingKey = @event.GetType().Name switch
        {
            nameof(UserRegisteredEvent) => "user.registered",
            nameof(UserSubscribedEvent) => "user.subscribed",
            _ => "user.unknown"
        };

        // Persistent = true — сообщение переживёт рестарт брокера (пишется на диск),
        // без этого при падении RabbitMQ несохранённые сообщения из очереди теряются безвозвратно
        var props = channel.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";

        channel.BasicPublish(
            exchange: "user.events",
            routingKey: routingKey,
            mandatory: true,
            basicProperties: props,
            body: body
        );

        return Task.CompletedTask;
    }
}