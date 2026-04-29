namespace AdminDashBoard.Api.Analysis;

public static class AnalysisEndpointValidation
{
    private static readonly string[] AllowedRiskLevels = ["low", "medium", "high"];

    public static bool IsAllowedRiskLevel(string riskLevel)
    {
        return AllowedRiskLevels.Contains(riskLevel, StringComparer.Ordinal);
    }
}
