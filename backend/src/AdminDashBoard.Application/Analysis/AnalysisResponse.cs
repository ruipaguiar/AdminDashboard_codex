namespace AdminDashBoard.Application.Analysis;

public sealed record AnalysisResponse(
    string Summary,
    string Trend,
    string RsiComment,
    IReadOnlyList<decimal> SupportLevels,
    IReadOnlyList<decimal> ResistanceLevels,
    string PossibleEntryZone,
    string StopLoss,
    IReadOnlyList<decimal> TakeProfitTargets,
    string RiskLevel,
    string Disclaimer);
