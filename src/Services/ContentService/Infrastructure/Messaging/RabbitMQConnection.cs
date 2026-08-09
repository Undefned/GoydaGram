using RabbitMQ.Client;

namespace ContentService.Infrastructure.Messaging;

public class RabbitMQConnection : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMQConnection(IConfiguration config)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(config["RabbitMQ:ConnectionString"]!),
            DispatchConsumersAsync = true
        };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare("video.events", ExchangeType.Topic, durable: true);
        _channel.ExchangeDeclare("social.events", ExchangeType.Topic, durable: true);
    }

    // Публичный IConnection — нужен консьюмеру и паблишеру, чтобы каждый открывал
    // СВОЙ канал. IModel не потокобезопасен, шарить один channel между несколькими
    // одновременными операциями нельзя (тот же баг чинили в UserService).
    public IConnection Connection => _connection;

    // Общий channel используется только для деклараций exchange при старте.
    public IModel Channel => _channel;

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}