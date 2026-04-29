namespace AdminDasboard.Application.MarketData;

public sealed record MarketDataSnapshotListItemResponse(
    Guid Id,
    string CoinId,
    string Symbol,
    string Name,
    string Currency,
    int Days,
    DateTimeOffset RetrievedAtUtc,
    decimal? Price,
    decimal? MarketCap,
    decimal? TotalVolume,
    decimal? PriceChangePercentage24h,
    int HistoryPoints);
