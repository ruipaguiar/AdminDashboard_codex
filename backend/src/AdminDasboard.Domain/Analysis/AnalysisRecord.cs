namespace AdminDasboard.Domain.Analysis;

public sealed class AnalysisRecord
{
    public Guid Id { get; set; }

    public required string CoinId { get; set; }

    public required string Currency { get; set; }

    public int Days { get; set; }

    public required string Model { get; set; }

    public required string RiskLevel { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public required string ResponseJson { get; set; }
}
