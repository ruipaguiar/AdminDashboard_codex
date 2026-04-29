using AdminDasboard.Application.MarketData;

namespace AdminDasboard.Infrastructure.MarketData;

public sealed class NoOpMarketDataSnapshotReader : IMarketDataSnapshotReader
{
    public Task<MarketDataSnapshotListResponse> ListAsync(
        string? coinId,
        string? currency,
        int? days,
        int offset,
        int limit,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new MarketDataSnapshotListResponse([], 0, offset, limit));
    }
}
