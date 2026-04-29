using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AdminDashBoard.Application.Assets;
using AdminDashBoard.Application.MarketData;
using Microsoft.Extensions.Options;

namespace AdminDashBoard.Infrastructure.MarketData;

public sealed class CoinGeckoAssetSearchService : IAssetSearchService
{
    private readonly HttpClient _httpClient;
    private readonly CoinGeckoOptions _options;

    public CoinGeckoAssetSearchService(
        HttpClient httpClient,
        IOptions<CoinGeckoOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<AssetSearchResultResponse>> SearchAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return [];
        }

        var path = $"search?query={Uri.EscapeDataString(query.Trim())}";
        var data = await GetFromCoinGeckoAsync<CoinGeckoSearchDto>(path, cancellationToken);

        return data.Coins
            .Where(coin => !string.IsNullOrWhiteSpace(coin.Id))
            .OrderBy(coin => coin.MarketCapRank is null)
            .ThenBy(coin => coin.MarketCapRank)
            .ThenBy(coin => coin.Name)
            .Take(limit)
            .Select(coin => new AssetSearchResultResponse(
                coin.Id,
                coin.Symbol,
                coin.Name,
                coin.MarketCapRank,
                coin.Thumb))
            .ToArray();
    }

    private async Task<T> GetFromCoinGeckoAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Add("x-cg-demo-api-key", _options.ApiKey);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new MarketDataProviderException("Asset search provider is temporarily unavailable.");
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new MarketDataProviderException("Asset search provider returned an empty response.");
    }

    private sealed record CoinGeckoSearchDto(
        IReadOnlyList<CoinGeckoSearchCoinDto> Coins);

    private sealed record CoinGeckoSearchCoinDto(
        string Id,
        string Symbol,
        string Name,
        [property: JsonPropertyName("market_cap_rank")] int? MarketCapRank,
        string? Thumb);
}
