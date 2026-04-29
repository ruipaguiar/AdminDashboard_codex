namespace AdminDasboard.Infrastructure.MarketData;

public sealed class CoinGeckoOptions
{
    public const string SectionName = "CoinGecko";

    public string BaseUrl { get; init; } = "https://api.coingecko.com/api/v3/";

    public string? ApiKey { get; init; }
}
