using AdminDasboard.Api.MarketData;
using AdminDasboard.Application.MarketData;
using AdminDasboard.Application.TechnicalIndicators;
using Microsoft.AspNetCore.Mvc;

namespace AdminDasboard.Api.Controllers;

[ApiController]
[Route("api/market-data")]
public sealed class MarketDataController : ControllerBase
{
    private readonly ICryptoMarketDataService _marketDataService;
    private readonly ITechnicalIndicatorService _technicalIndicatorService;
    private readonly IMarketDataSnapshotReader _snapshotReader;

    public MarketDataController(
        ICryptoMarketDataService marketDataService,
        ITechnicalIndicatorService technicalIndicatorService,
        IMarketDataSnapshotReader snapshotReader)
    {
        _marketDataService = marketDataService;
        _technicalIndicatorService = technicalIndicatorService;
        _snapshotReader = snapshotReader;
    }

    [HttpGet("{coinId}", Name = "GetMarketData")]
    public async Task<IActionResult> Get(
        string coinId,
        [FromQuery] string? currency,
        [FromQuery] int? days,
        CancellationToken cancellationToken)
    {
        var requestedDays = days ?? 30;

        if (!MarketDataEndpointValidation.TryValidate(
            coinId,
            currency,
            requestedDays,
            out var normalizedCurrency,
            out var errors))
        {
            return BadRequest(new ValidationProblemDetails(errors));
        }

        try
        {
            var marketData = await _marketDataService.GetAsync(
                coinId,
                normalizedCurrency,
                requestedDays,
                cancellationToken);

            return Ok(marketData);
        }
        catch (MarketDataNotFoundException)
        {
            return Problem(
                title: "Market data not found",
                detail: "The requested coin was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }
        catch (MarketDataProviderException)
        {
            return Problem(
                title: "Market data provider unavailable",
                detail: "Could not retrieve market data right now.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    [HttpGet("{coinId}/indicators", Name = "GetMarketDataIndicators")]
    public async Task<IActionResult> GetIndicators(
        string coinId,
        [FromQuery] string? currency,
        [FromQuery] int? days,
        CancellationToken cancellationToken)
    {
        var requestedDays = days ?? 30;

        if (!MarketDataEndpointValidation.TryValidate(
            coinId,
            currency,
            requestedDays,
            out var normalizedCurrency,
            out var errors))
        {
            return BadRequest(new ValidationProblemDetails(errors));
        }

        try
        {
            var marketData = await _marketDataService.GetAsync(
                coinId,
                normalizedCurrency,
                requestedDays,
                cancellationToken);

            return Ok(_technicalIndicatorService.Calculate(marketData));
        }
        catch (MarketDataNotFoundException)
        {
            return Problem(
                title: "Market data not found",
                detail: "The requested coin was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }
        catch (MarketDataProviderException)
        {
            return Problem(
                title: "Market data provider unavailable",
                detail: "Could not retrieve market data right now.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    [HttpGet("snapshots", Name = "ListMarketDataSnapshots")]
    public async Task<IActionResult> ListSnapshots(
        [FromQuery] string? coinId,
        [FromQuery] string? currency,
        [FromQuery] int? days,
        [FromQuery] int? offset,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var normalizedCoinId = string.IsNullOrWhiteSpace(coinId) ? null : coinId.Trim();
        var normalizedCurrency = string.IsNullOrWhiteSpace(currency) ? null : currency.Trim().ToLowerInvariant();
        var requestedOffset = Math.Max(offset ?? 0, 0);
        var requestedLimit = Math.Clamp(limit ?? 25, 1, 100);
        var errors = new Dictionary<string, string[]>();

        if (normalizedCoinId is not null &&
            !MarketDataEndpointValidation.IsValidCoinId(normalizedCoinId))
        {
            errors["coinId"] = ["Use a CoinGecko coin id with lowercase letters, numbers, and hyphens."];
        }

        if (normalizedCurrency is not null &&
            !MarketDataEndpointValidation.IsAllowedCurrency(normalizedCurrency))
        {
            errors["currency"] = ["Allowed values: eur, usd."];
        }

        if (days is not null &&
            !MarketDataEndpointValidation.IsAllowedRange(days.Value))
        {
            errors["days"] = ["Allowed values: 1, 7, 30, 90, 365."];
        }

        if (errors.Count > 0)
        {
            return BadRequest(new ValidationProblemDetails(errors));
        }

        var snapshots = await _snapshotReader.ListAsync(
            normalizedCoinId,
            normalizedCurrency,
            days,
            requestedOffset,
            requestedLimit,
            cancellationToken);

        return Ok(snapshots);
    }
}
