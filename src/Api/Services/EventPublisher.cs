using System.Text;
using System.Text.Json;
using Api.Contracts;
using Api.Models;
using RabbitMQ.Client;

namespace Api.Services;

public class EventPublisher
{
    public const string Exchange = "tickets.exchange";
    public const string CreatedQueue = "tickets.created";
    public const string AnalyzedQueue = "tickets.analyzed";
    public const string CreatedRoutingKey = "ticket.created";
    public const string AnalyzedRoutingKey = "ticket.analyzed";

    private readonly IConnection _connection;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(IConnection connection, ILogger<EventPublisher> logger)
    {
        _connection = connection;
        _logger = logger;
        using var ch = _connection.CreateModel();
        ch.ExchangeDeclare(Exchange, ExchangeType.Topic, durable: true);
        ch.QueueDeclare(CreatedQueue, durable: true, exclusive: false, autoDelete: false);
        ch.QueueBind(CreatedQueue, Exchange, CreatedRoutingKey);
        ch.QueueDeclare(AnalyzedQueue, durable: true, exclusive: false, autoDelete: false);
        ch.QueueBind(AnalyzedQueue, Exchange, AnalyzedRoutingKey);
    }

    public Task PublishTicketCreatedAsync(Ticket ticket)
    {
        var evt = new TicketCreatedEvent(
            ticket.Id, ticket.Title, ticket.Description,
            ticket.CustomerEmail, ticket.CreatedAt);

        using var ch = _connection.CreateModel();
        var props = ch.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));
        ch.BasicPublish(Exchange, CreatedRoutingKey, props, body);
        _logger.LogInformation("ticket.created published: {TicketId}", ticket.Id);
        return Task.CompletedTask;
    }
}
