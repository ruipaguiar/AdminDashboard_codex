using AdminDasboard.Api.Analysis;
using AdminDasboard.Api.MarketData;
using AdminDasboard.Application.Analysis;
using AdminDasboard.Application.MarketData;
using Microsoft.AspNetCore.Mvc;

namespace AdminDasboard.Api.Controllers;

[ApiController]
[Route("api/analysis")]
public sealed class AnalysisController : ControllerBase
{
    private readonly IAnalysisService _analysisService;
    private readonly IAnalysisHistoryStore _analysisHistoryStore;

    public AnalysisController(
        IAnalysisService analysisService,
        IAnalysisHistoryStore analysisHistoryStore)
    {
        _analysisService = analysisService;
        _analysisHistoryStore = analysisHistoryStore;
    }

    [HttpPost(Name = "CreateAnalysis")]
    public async Task<IActionResult> Create(
        [FromBody] AnalysisRequest request,
        CancellationToken cancellationToken)
    {
        var requestedDays = request.Days ?? 30;

        if (!MarketDataEndpointValidation.TryValidate(
            request.CoinId,
            request.Currency,
            requestedDays,
            out var normalizedCurrency,
            out var errors))
        {
            return BadRequest(new ValidationProblemDetails(errors));
        }

        try
        {
            var analysis = await _analysisService.AnalyzeAsync(
                request.CoinId,
                normalizedCurrency,
                requestedDays,
                cancellationToken);

            return Ok(analysis);
        }
        catch (AnalysisConfigurationException)
        {
            return Problem(
                title: "AI analysis is not configured",
                detail: "Configure the OpenAI API key on the backend before requesting AI analysis.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
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
        catch (AnalysisProviderException)
        {
            return Problem(
                title: "AI analysis provider unavailable",
                detail: "Could not retrieve AI analysis right now.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    [HttpGet("history", Name = "ListAnalysisHistory")]
    public async Task<IActionResult> ListHistory(
        [FromQuery] string? coinId,
        [FromQuery] string? currency,
        [FromQuery] int? days,
        [FromQuery] string? riskLevel,
        [FromQuery] int? offset,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var normalizedCoinId = string.IsNullOrWhiteSpace(coinId) ? null : coinId.Trim();
        var normalizedCurrency = string.IsNullOrWhiteSpace(currency) ? null : currency.Trim().ToLowerInvariant();
        var normalizedRiskLevel = string.IsNullOrWhiteSpace(riskLevel) ? null : riskLevel.Trim().ToLowerInvariant();
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

        if (normalizedRiskLevel is not null &&
            !AnalysisEndpointValidation.IsAllowedRiskLevel(normalizedRiskLevel))
        {
            errors["riskLevel"] = ["Allowed values: low, medium, high."];
        }

        if (errors.Count > 0)
        {
            return BadRequest(new ValidationProblemDetails(errors));
        }

        var history = await _analysisHistoryStore.ListAsync(
            normalizedCoinId,
            normalizedCurrency,
            days,
            normalizedRiskLevel,
            requestedOffset,
            requestedLimit,
            cancellationToken);

        return Ok(history);
    }

    [HttpGet("history/{coinId}", Name = "GetAnalysisHistory")]
    public async Task<IActionResult> GetHistory(
        string coinId,
        [FromQuery] string? currency,
        [FromQuery] int? days,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var requestedDays = days ?? 30;
        var requestedLimit = Math.Clamp(limit ?? 10, 1, 50);

        if (!MarketDataEndpointValidation.TryValidate(
            coinId,
            currency,
            requestedDays,
            out var normalizedCurrency,
            out var errors))
        {
            return BadRequest(new ValidationProblemDetails(errors));
        }

        var history = await _analysisHistoryStore.ListAsync(
            coinId,
            normalizedCurrency,
            requestedDays,
            requestedLimit,
            cancellationToken);

        return Ok(history);
    }
}
