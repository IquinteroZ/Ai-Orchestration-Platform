namespace Api.Contracts;

public record AnalysisResultEvent(
    Guid TicketId,
    string Sentiment,
    double Score,
    DateTime AnalyzedAt);
    