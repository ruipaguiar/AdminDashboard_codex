namespace AdminDashBoard.Application.MarketData;

public sealed record MarketDataSnapshotListResponse(
    IReadOnlyList<MarketDataSnapshotListItemResponse> Items,
    int TotalCount,
    int Offset,
    int Limit);
