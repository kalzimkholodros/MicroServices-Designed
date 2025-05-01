using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Common.Messaging;

public interface IRabbitMQService
{
    void PublishMessage<T>(string queueName, T message);
    void ConsumeMessage<T>(string queueName, Action<T> handler);
}

public class RabbitMQService : IRabbitMQService, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly RabbitMQSettings _settings;

    public RabbitMQService(IOptions<RabbitMQSettings> settings)
    {
        _settings = settings.Value;

        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            UserName = _settings.UserName,
            Password = _settings.Password,
            Port = _settings.Port,
            VirtualHost = _settings.VirtualHost
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public void PublishMessage<T>(string queueName, T message)
    {
        _channel.QueueDeclare(queue: queueName,
                            durable: true,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        _channel.BasicPublish(exchange: "",
                            routingKey: queueName,
                            basicProperties: null,
                            body: body);
    }

    public void ConsumeMessage<T>(string queueName, Action<T> handler)
    {
        _channel.QueueDeclare(queue: queueName,
                            durable: true,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var deserializedMessage = JsonSerializer.Deserialize<T>(message);

            if (deserializedMessage != null)
            {
                handler(deserializedMessage);
            }
        };

        _channel.BasicConsume(queue: queueName,
                            autoAck: true,
                            consumer: consumer);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
} 