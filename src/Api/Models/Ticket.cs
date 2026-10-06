namespace Api.Models;

public class Ticket
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Resultados IA
    public string? Sentiment { get; set; }
    public double? SentimentScore { get; set; }
    public DateTime? AnalyzedAt { get; set; }
}
