namespace AdminDasboard.Application.MarketData;

public interface IMarketDataSnapshotReader
{
    Task<MarketDataSnapshotListResponse> ListAsync(
        string? coinId,
        string? currency,
        int? days,
        int offset,
        int limit,
        CancellationToken cancellationToken);
}
