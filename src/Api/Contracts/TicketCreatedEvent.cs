namespace Api.Contracts;

public record TicketCreatedEvent(
    Guid Id,
    string Title,
    string Description,
    string CustomerEmail,
    DateTime CreatedAt);