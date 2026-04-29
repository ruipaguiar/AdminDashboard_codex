namespace AdminDashBoard.Application.MarketData;

public sealed record CryptoMarketDataResponse(
    string CoinId,
    string Symbol,
    string Name,
    string Currency,
    int Days,
    CurrentMarketDataResponse Current,
    IReadOnlyList<HistoricalMarketPointResponse> History);

public sealed record CurrentMarketDataResponse(
    decimal? Price,
    decimal? MarketCap,
    int? MarketCapRank,
    decimal? TotalVolume,
    decimal? High24h,
    decimal? Low24h,
    decimal? PriceChange24h,
    decimal? PriceChangePercentage24h,
    DateTimeOffset? LastUpdated);

public sealed record HistoricalMarketPointResponse(
    DateTimeOffset Timestamp,
    decimal Price,
    decimal? MarketCap,
    decimal? TotalVolume);
