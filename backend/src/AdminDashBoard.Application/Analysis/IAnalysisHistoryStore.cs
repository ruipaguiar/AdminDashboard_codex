namespace AdminDashBoard.Application.Analysis;

public interface IAnalysisHistoryStore
{
    Task SaveAsync(
        string coinId,
        string currency,
        int days,
        string model,
        AnalysisResponse analysis,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AnalysisHistoryItemResponse>> ListAsync(
        string coinId,
        string currency,
        int days,
        int limit,
        CancellationToken cancellationToken);

    Task<AnalysisHistoryListResponse> ListAsync(
        string? coinId,
        string? currency,
        int? days,
        string? riskLevel,
        int offset,
        int limit,
        CancellationToken cancellationToken);
}
