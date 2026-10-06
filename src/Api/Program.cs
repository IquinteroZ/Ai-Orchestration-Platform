using System.ComponentModel.DataAnnotations;
using Api.Contracts;
using Api.Data;
using Api.Models;
using Api.Services;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddSingleton<IConnection>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<Program>>();
    var factory = new ConnectionFactory
    {
        HostName = cfg["RabbitMQ:Host"] ?? "rabbitmq",
        UserName = cfg["RabbitMQ:User"] ?? "guest",
        Password = cfg["RabbitMQ:Password"] ?? "guest",
        DispatchConsumersAsync = true
    };

    for (int attempt = 1; attempt <= 15; attempt++)
    {
        try { return factory.CreateConnection(); }
        catch (Exception ex)
        {
            logger.LogWarning("RabbitMQ not ready ({Attempt}/15): {Msg}", attempt, ex.Message);
            Thread.Sleep(3000);
        }
    }
    return factory.CreateConnection();
});

builder.Services.AddSingleton<EventPublisher>();
builder.Services.AddHostedService<AnalysisResultConsumer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    for (int i = 0; i < 15; i++)
    {
        try { db.Database.EnsureCreated(); break; }
        catch { Thread.Sleep(3000); }
    }
}

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}));

app.MapPost("/api/tickets", async (
    CreateTicketRequest request,
    AppDbContext db,
    EventPublisher publisher) =>
{
    var errors = new List<ValidationResult>();
    if (!Validator.TryValidateObject(request, new ValidationContext(request), errors, true))
        return Results.BadRequest(errors.Select(e => e.ErrorMessage));

    var ticket = new Ticket
    {
        Id = Guid.NewGuid(),
        Title = request.Title.Trim(),
        Description = request.Description.Trim(),
        CustomerEmail = request.CustomerEmail.Trim(),
        Status = "pending",
        CreatedAt = DateTime.UtcNow
    };

    db.Tickets.Add(ticket);
    await db.SaveChangesAsync();
    await publisher.PublishTicketCreatedAsync(ticket);

    return Results.Created($"/api/tickets/{ticket.Id}", ticket);
});

app.MapGet("/api/tickets", async (AppDbContext db) =>
    Results.Ok(await db.Tickets
        .OrderByDescending(t => t.CreatedAt)
        .Take(100)
        .ToListAsync()));

app.MapGet("/api/tickets/{id:guid}", async (Guid id, AppDbContext db) =>
    await db.Tickets.FindAsync(id) is Ticket t
        ? Results.Ok(t)
        : Results.NotFound(new { message = $"Ticket {id} not found" }));

app.Run();