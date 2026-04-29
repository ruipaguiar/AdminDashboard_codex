using AdminDasboard.Application.MarketData;

namespace AdminDasboard.Application.TechnicalIndicators;

public sealed class TechnicalIndicatorService : ITechnicalIndicatorService
{
    private const int SmaPeriod = 20;
    private const int EmaPeriod = 20;
    private const int RsiPeriod = 14;

    public TechnicalIndicatorsResponse Calculate(CryptoMarketDataResponse marketData)
    {
        var orderedHistory = marketData.History
            .OrderBy(point => point.Timestamp)
            .ToArray();

        var sma20 = CalculateSma(orderedHistory, SmaPeriod);
        var ema20 = CalculateEma(orderedHistory, EmaPeriod);
        var rsi14 = CalculateRsi(orderedHistory, RsiPeriod);
        var latestRsi = rsi14.LastOrDefault()?.Value;

        return new TechnicalIndicatorsResponse(
            marketData.CoinId,
            marketData.Currency,
            marketData.Days,
            sma20,
            ema20,
            rsi14,
            new TechnicalIndicatorSummaryResponse(
                sma20.LastOrDefault()?.Value,
                ema20.LastOrDefault()?.Value,
                latestRsi,
                GetRsiSignal(latestRsi)));
    }

    private static IReadOnlyList<MovingAveragePointResponse> CalculateSma(
        IReadOnlyList<HistoricalMarketPointResponse> history,
        int period)
    {
        if (history.Count < period)
        {
            return [];
        }

        var result = new List<MovingAveragePointResponse>(history.Count - period + 1);

        for (var index = period - 1; index < history.Count; index++)
        {
            var average = history
                .Skip(index - period + 1)
                .Take(period)
                .Average(point => point.Price);

            result.Add(new MovingAveragePointResponse(
                history[index].Timestamp,
                Round(average)));
        }

        return result;
    }

    private static IReadOnlyList<MovingAveragePointResponse> CalculateEma(
        IReadOnlyList<HistoricalMarketPointResponse> history,
        int period)
    {
        if (history.Count < period)
        {
            return [];
        }

        var result = new List<MovingAveragePointResponse>(history.Count - period + 1);
        var multiplier = 2m / (period + 1);
        var previousEma = history
            .Take(period)
            .Average(point => point.Price);

        result.Add(new MovingAveragePointResponse(history[period - 1].Timestamp, Round(previousEma)));

        for (var index = period; index < history.Count; index++)
        {
            previousEma = ((history[index].Price - previousEma) * multiplier) + previousEma;

            result.Add(new MovingAveragePointResponse(
                history[index].Timestamp,
                Round(previousEma)));
        }

        return result;
    }

    private static IReadOnlyList<RsiPointResponse> CalculateRsi(
        IReadOnlyList<HistoricalMarketPointResponse> history,
        int period)
    {
        if (history.Count <= period)
        {
            return [];
        }

        var result = new List<RsiPointResponse>(history.Count - period);
        var gains = 0m;
        var losses = 0m;

        for (var index = 1; index <= period; index++)
        {
            var change = history[index].Price - history[index - 1].Price;

            if (change >= 0)
            {
                gains += change;
            }
            else
            {
                losses -= change;
            }
        }

        var averageGain = gains / period;
        var averageLoss = losses / period;
        result.Add(new RsiPointResponse(history[period].Timestamp, CalculateRsiValue(averageGain, averageLoss)));

        for (var index = period + 1; index < history.Count; index++)
        {
            var change = history[index].Price - history[index - 1].Price;
            var gain = change > 0 ? change : 0m;
            var loss = change < 0 ? -change : 0m;

            averageGain = ((averageGain * (period - 1)) + gain) / period;
            averageLoss = ((averageLoss * (period - 1)) + loss) / period;

            result.Add(new RsiPointResponse(
                history[index].Timestamp,
                CalculateRsiValue(averageGain, averageLoss)));
        }

        return result;
    }

    private static decimal CalculateRsiValue(decimal averageGain, decimal averageLoss)
    {
        if (averageLoss == 0)
        {
            return 100m;
        }

        var relativeStrength = averageGain / averageLoss;
        return Round(100m - (100m / (1m + relativeStrength)));
    }

    private static string? GetRsiSignal(decimal? rsi)
    {
        return rsi switch
        {
            null => null,
            >= 70m => "overbought",
            <= 30m => "oversold",
            _ => "neutral"
        };
    }

    private static decimal Round(decimal value)
    {
        return decimal.Round(value, 4, MidpointRounding.AwayFromZero);
    }
}
