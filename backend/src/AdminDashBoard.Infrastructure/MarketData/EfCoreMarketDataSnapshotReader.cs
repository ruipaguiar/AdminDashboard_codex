using System.Text.Json;
using AdminDashBoard.Application.MarketData;
using AdminDashBoard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AdminDashBoard.Infrastructure.MarketData;

public sealed class EfCoreMarketDataSnapshotReader : IMarketDataSnapshotReader
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _dbContext;

    public EfCoreMarketDataSnapshotReader(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MarketDataSnapshotListResponse> ListAsync(
        string? coinId,
        string? currency,
        int? days,
        int offset,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.MarketDataSnapshots.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(coinId))
        {
            query = query.Where(snapshot => snapshot.CoinId == coinId);
        }

        if (!string.IsNullOrWhiteSpace(currency))
        {
            query = query.Where(snapshot => snapshot.Currency == currency);
        }

        if (days is not null)
        {
            query = query.Where(snapshot => snapshot.Days == days);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var snapshots = await query
            .OrderByDescending(snapshot => snapshot.RetrievedAtUtc)
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync(cancellationToken);

        var items = snapshots
            .Select(snapshot =>
            {
                var payload = JsonSerializer.Deserialize<CryptoMarketDataResponse>(
                    snapshot.PayloadJson,
                    SerializerOptions);

                return new MarketDataSnapshotListItemResponse(
                    snapshot.Id,
                    snapshot.CoinId,
                    payload?.Symbol ?? "",
                    payload?.Name ?? snapshot.CoinId,
                    snapshot.Currency,
                    snapshot.Days,
                    snapshot.RetrievedAtUtc,
                    payload?.Current.Price,
                    payload?.Current.MarketCap,
                    payload?.Current.TotalVolume,
                    payload?.Current.PriceChangePercentage24h,
                    payload?.History.Count ?? 0);
            })
            .ToArray();

        return new MarketDataSnapshotListResponse(items, totalCount, offset, limit);
    }
}
