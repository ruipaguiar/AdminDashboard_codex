namespace AdminDasboard.Application.MarketData;

public interface IMarketDataSnapshotStore
{
    Task<CryptoMarketDataResponse?> GetFreshAsync(
        string coinId,
        string currency,
        int days,
        TimeSpan maxAge,
        CancellationToken cancellationToken);

    Task SaveAsync(
        CryptoMarketDataResponse marketData,
        CancellationToken cancellationToken);
}
