using System.Text.Json;
using AdminDasboard.Application.MarketData;
using AdminDasboard.Domain.MarketData;
using AdminDasboard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AdminDasboard.Infrastructure.MarketData;

public sealed class EfCoreMarketDataSnapshotStore : IMarketDataSnapshotStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _dbContext;

    public EfCoreMarketDataSnapshotStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CryptoMarketDataResponse?> GetFreshAsync(
        string coinId,
        string currency,
        int days,
        TimeSpan maxAge,
        CancellationToken cancellationToken)
    {
        var minimumRetrievedAt = DateTimeOffset.UtcNow.Subtract(maxAge);

        var snapshot = await _dbContext.MarketDataSnapshots
            .AsNoTracking()
            .Where(item =>
                item.CoinId == coinId &&
                item.Currency == currency &&
                item.Days == days &&
                item.RetrievedAtUtc >= minimumRetrievedAt)
            .OrderByDescending(item => item.RetrievedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return snapshot is null
            ? null
            : JsonSerializer.Deserialize<CryptoMarketDataResponse>(
                snapshot.PayloadJson,
                SerializerOptions);
    }

    public async Task SaveAsync(CryptoMarketDataResponse marketData, CancellationToken cancellationToken)
    {
        var snapshot = new MarketDataSnapshot
        {
            Id = Guid.NewGuid(),
            CoinId = marketData.CoinId,
            Currency = marketData.Currency,
            Days = marketData.Days,
            RetrievedAtUtc = DateTimeOffset.UtcNow,
            PayloadJson = JsonSerializer.Serialize(marketData, SerializerOptions)
        };

        _dbContext.MarketDataSnapshots.Add(snapshot);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
