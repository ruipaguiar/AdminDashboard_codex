namespace AdminDashBoard.Application.MarketData;

public sealed class MarketDataNotFoundException : Exception
{
    public MarketDataNotFoundException(string coinId)
        : base($"Market data was not found for coin '{coinId}'.")
    {
        CoinId = coinId;
    }

    public string CoinId { get; }
}

public sealed class MarketDataProviderException : Exception
{
    public MarketDataProviderException(string message)
        : base(message)
    {
    }
}
