namespace AdminDashBoard.Application.Analysis;

public interface IAnalysisService
{
    Task<AnalysisResponse> AnalyzeAsync(
        string coinId,
        string currency,
        int days,
        CancellationToken cancellationToken);
}
