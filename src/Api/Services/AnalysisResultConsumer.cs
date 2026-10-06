using System.Text;
using System.Text.Json;
using Api.Contracts;
using Api.Data;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Api.Services;

public class AnalysisResultConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnalysisResultConsumer> _logger;
    private IModel? _channel;

    public AnalysisResultConsumer(
        IConnection connection,
        IServiceScopeFactory scopeFactory,
        ILogger<AnalysisResultConsumer> logger)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(EventPublisher.Exchange, ExchangeType.Topic, durable: true);
        _channel.QueueDeclare(EventPublisher.AnalyzedQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(EventPublisher.AnalyzedQueue, EventPublisher.Exchange, EventPublisher.AnalyzedRoutingKey);
        _channel.BasicQos(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var result = JsonSerializer.Deserialize<AnalysisResultEvent>(
                    json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result is not null)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var ticket = await db.Tickets.FindAsync(result.TicketId);
                    if (ticket is not null)
                    {
                        ticket.Sentiment = result.Sentiment;
                        ticket.SentimentScore = result.Score;
                        ticket.AnalyzedAt = DateTime.UtcNow;
                        ticket.Status = "analyzed";
                        await db.SaveChangesAsync();
                        _logger.LogInformation("ticket {TicketId} analyzed: {Sentiment} ({Score:F2})",
                            ticket.Id, result.Sentiment, result.Score);
                    }
                }
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing analysis result");
                _channel.BasicNack(ea.DeliveryTag, false, requeue: false);
            }
        };

        _channel.BasicConsume(EventPublisher.AnalyzedQueue, autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}
