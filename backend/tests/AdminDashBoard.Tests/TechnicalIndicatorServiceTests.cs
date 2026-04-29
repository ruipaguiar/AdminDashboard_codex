using AdminDashBoard.Application.MarketData;
using AdminDashBoard.Application.TechnicalIndicators;

namespace AdminDashBoard.Tests;

public sealed class TechnicalIndicatorServiceTests
{
    [Fact]
    public void Calculate_WithRisingPrices_ReturnsMovingAveragesAndOverboughtRsi()
    {
        var service = new TechnicalIndicatorService();
        var marketData = CreateMarketData(Enumerable.Range(1, 25).Select(value => (decimal)value));

        var result = service.Calculate(marketData);

        Assert.Equal(6, result.Sma20.Count);
        Assert.Equal(6, result.Ema20.Count);
        Assert.Equal(11, result.Rsi14.Count);
        Assert.Equal(15.5m, result.Summary.LatestSma20);
        Assert.Equal(100m, result.Summary.LatestRsi14);
        Assert.Equal("overbought", result.Summary.RsiSignal);
    }

    private static CryptoMarketDataResponse CreateMarketData(IEnumerable<decimal> prices)
    {
        var history = prices
            .Select((price, index) => new HistoricalMarketPointResponse(
                DateTimeOffset.Parse("2026-04-01T00:00:00Z").AddHours(index),
                price,
                null,
                null))
            .ToArray();

        return new CryptoMarketDataResponse(
            "bitcoin",
            "btc",
            "Bitcoin",
            "eur",
            30,
            new CurrentMarketDataResponse(null, null, null, null, null, null, null, null, null),
            history);
    }
}
