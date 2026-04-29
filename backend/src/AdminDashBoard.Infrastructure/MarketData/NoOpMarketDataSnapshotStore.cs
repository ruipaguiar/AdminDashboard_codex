using AdminDashBoard.Application.MarketData;

namespace AdminDashBoard.Infrastructure.MarketData;

public sealed class NoOpMarketDataSnapshotStore : IMarketDataSnapshotStore
{
    public Task<CryptoMarketDataResponse?> GetFreshAsync(
        string coinId,
        string currency,
        int days,
        TimeSpan maxAge,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<CryptoMarketDataResponse?>(null);
    }

    public Task SaveAsync(CryptoMarketDataResponse marketData, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
