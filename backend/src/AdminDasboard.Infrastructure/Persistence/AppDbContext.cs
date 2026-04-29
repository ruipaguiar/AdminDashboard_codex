using Microsoft.EntityFrameworkCore;
using AdminDasboard.Domain.Analysis;
using AdminDasboard.Domain.MarketData;

namespace AdminDasboard.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<MarketDataSnapshot> MarketDataSnapshots => Set<MarketDataSnapshot>();

    public DbSet<AnalysisRecord> AnalysisRecords => Set<AnalysisRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("admin_dashboard");

        modelBuilder.Entity<MarketDataSnapshot>(entity =>
        {
            entity.ToTable("market_data_snapshots");

            entity.HasKey(snapshot => snapshot.Id);

            entity.Property(snapshot => snapshot.CoinId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(snapshot => snapshot.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(snapshot => snapshot.PayloadJson)
                .HasColumnType("jsonb")
                .IsRequired();

            entity.HasIndex(snapshot => new
            {
                snapshot.CoinId,
                snapshot.Currency,
                snapshot.Days,
                snapshot.RetrievedAtUtc
            });
        });

        modelBuilder.Entity<AnalysisRecord>(entity =>
        {
            entity.ToTable("analysis_records");

            entity.HasKey(record => record.Id);

            entity.Property(record => record.CoinId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(record => record.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(record => record.Model)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(record => record.RiskLevel)
                .HasMaxLength(16)
                .IsRequired();

            entity.Property(record => record.ResponseJson)
                .HasColumnType("jsonb")
                .IsRequired();

            entity.HasIndex(record => new
            {
                record.CoinId,
                record.Currency,
                record.Days,
                record.CreatedAtUtc
            });
        });
    }
}
