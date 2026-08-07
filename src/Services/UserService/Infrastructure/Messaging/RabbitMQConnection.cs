using RabbitMQ.Client;

namespace UserService.Infrastructure.Messaging;

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
        _channel.ExchangeDeclare("user.events", ExchangeType.Topic, durable: true);
    }

    public IModel Channel => _channel;

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}