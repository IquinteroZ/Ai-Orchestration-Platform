using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ticket>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.CreatedAt);
            e.Property(t => t.Title).HasMaxLength(200).IsRequired();
            e.Property(t => t.CustomerEmail).HasMaxLength(256).IsRequired();
            e.Property(t => t.Status).HasMaxLength(32);
            e.Property(t => t.Sentiment).HasMaxLength(32);
        });
    }
}
