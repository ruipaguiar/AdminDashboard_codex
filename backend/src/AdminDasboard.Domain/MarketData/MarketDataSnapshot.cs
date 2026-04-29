namespace AdminDasboard.Domain.MarketData;

public sealed class MarketDataSnapshot
{
    public Guid Id { get; set; }

    public required string CoinId { get; set; }

    public required string Currency { get; set; }

    public int Days { get; set; }

    public DateTimeOffset RetrievedAtUtc { get; set; }

    public required string PayloadJson { get; set; }
}
