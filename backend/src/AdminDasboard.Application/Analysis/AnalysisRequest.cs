namespace AdminDasboard.Application.Analysis;

public sealed record AnalysisRequest(
    string CoinId,
    string? Currency,
    int? Days);
