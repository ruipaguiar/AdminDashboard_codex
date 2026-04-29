using AdminDasboard.Application.Assets;
using AdminDasboard.Application.MarketData;
using Microsoft.AspNetCore.Mvc;

namespace AdminDasboard.Api.Controllers;

[ApiController]
[Route("api/assets")]
public sealed class AssetsController : ControllerBase
{
    private readonly IAssetSearchService _assetSearchService;

    public AssetsController(IAssetSearchService assetSearchService)
    {
        _assetSearchService = assetSearchService;
    }

    [HttpGet("search", Name = "SearchAssets")]
    public async Task<IActionResult> Search(
        [FromQuery] string? query,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var trimmedQuery = query?.Trim();
        var requestedLimit = Math.Clamp(limit ?? 10, 1, 20);

        if (string.IsNullOrWhiteSpace(trimmedQuery) || trimmedQuery.Length < 2 || trimmedQuery.Length > 80)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["query"] = ["Use a search term between 2 and 80 characters."]
            }));
        }

        try
        {
            var assets = await _assetSearchService.SearchAsync(
                trimmedQuery,
                requestedLimit,
                cancellationToken);

            return Ok(assets);
        }
        catch (MarketDataProviderException)
        {
            return Problem(
                title: "Asset search provider unavailable",
                detail: "Could not search assets right now.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
