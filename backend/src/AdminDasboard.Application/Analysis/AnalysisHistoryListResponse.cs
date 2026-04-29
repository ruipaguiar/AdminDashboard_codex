namespace AdminDasboard.Application.Analysis;

public sealed record AnalysisHistoryListResponse(
    IReadOnlyList<AnalysisHistoryItemResponse> Items,
    int TotalCount,
    int Offset,
    int Limit);
