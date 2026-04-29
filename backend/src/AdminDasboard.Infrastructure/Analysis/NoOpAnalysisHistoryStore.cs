using AdminDasboard.Application.Analysis;

namespace AdminDasboard.Infrastructure.Analysis;

public sealed class NoOpAnalysisHistoryStore : IAnalysisHistoryStore
{
    public Task SaveAsync(
        string coinId,
        string currency,
        int days,
        string model,
        AnalysisResponse analysis,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AnalysisHistoryItemResponse>> ListAsync(
        string coinId,
        string currency,
        int days,
        int limit,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<AnalysisHistoryItemResponse>>([]);
    }

    public Task<AnalysisHistoryListResponse> ListAsync(
        string? coinId,
        string? currency,
        int? days,
        string? riskLevel,
        int offset,
        int limit,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new AnalysisHistoryListResponse([], 0, offset, limit));
    }
}
