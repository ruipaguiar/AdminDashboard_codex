namespace AdminDasboard.Application.TechnicalIndicators;

public sealed record TechnicalIndicatorsResponse(
    string CoinId,
    string Currency,
    int Days,
    IReadOnlyList<MovingAveragePointResponse> Sma20,
    IReadOnlyList<MovingAveragePointResponse> Ema20,
    IReadOnlyList<RsiPointResponse> Rsi14,
    TechnicalIndicatorSummaryResponse Summary);

public sealed record MovingAveragePointResponse(
    DateTimeOffset Timestamp,
    decimal Value);

public sealed record RsiPointResponse(
    DateTimeOffset Timestamp,
    decimal Value);

public sealed record TechnicalIndicatorSummaryResponse(
    decimal? LatestSma20,
    decimal? LatestEma20,
    decimal? LatestRsi14,
    string? RsiSignal);
