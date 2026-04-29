using Microsoft.EntityFrameworkCore;
using AdminDashBoard.Domain.Analysis;
using AdminDashBoard.Domain.Auth;
using AdminDashBoard.Domain.MarketData;

namespace AdminDashBoard.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<MarketDataSnapshot> MarketDataSnapshots => Set<MarketDataSnapshot>();

    public DbSet<AnalysisRecord> AnalysisRecords => Set<AnalysisRecord>();

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

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

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("user_accounts");

            entity.HasKey(account => account.Id);

            entity.Property(account => account.Email)
                .HasMaxLength(320)
                .IsRequired();

            entity.Property(account => account.PasswordHash)
                .HasMaxLength(512)
                .IsRequired();

            entity.HasIndex(account => account.Email)
                .IsUnique();
        });
    }
}
