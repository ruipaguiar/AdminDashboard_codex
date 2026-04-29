namespace AdminDashBoard.Application.Analysis;

public sealed record AnalysisHistoryItemResponse(
    Guid Id,
    string CoinId,
    string Currency,
    int Days,
    string RiskLevel,
    DateTimeOffset CreatedAtUtc,
    AnalysisResponse Analysis);
