using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AdminDasboard.Application.MarketData;
using Microsoft.Extensions.Options;

namespace AdminDasboard.Infrastructure.MarketData;

public sealed class CoinGeckoMarketDataService : ICryptoMarketDataService
{
    private static readonly TimeSpan CacheMaxAge = TimeSpan.FromMinutes(5);

    private readonly HttpClient _httpClient;
    private readonly CoinGeckoOptions _options;
    private readonly IMarketDataSnapshotStore _snapshotStore;

    public CoinGeckoMarketDataService(
        HttpClient httpClient,
        IOptions<CoinGeckoOptions> options,
        IMarketDataSnapshotStore snapshotStore)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _snapshotStore = snapshotStore;
    }

    public async Task<CryptoMarketDataResponse> GetAsync(
        string coinId,
        string currency,
        int days,
        CancellationToken cancellationToken)
    {
        var cached = await _snapshotStore.GetFreshAsync(
            coinId,
            currency,
            days,
            CacheMaxAge,
            cancellationToken);

        if (cached is not null)
        {
            return cached;
        }

        var current = await GetCurrentAsync(coinId, currency, cancellationToken);
        var chart = await GetChartAsync(coinId, currency, days, cancellationToken);

        var marketData = new CryptoMarketDataResponse(
            current.Id,
            current.Symbol,
            current.Name,
            currency,
            days,
            new CurrentMarketDataResponse(
                current.CurrentPrice,
                current.MarketCap,
                current.MarketCapRank,
                current.TotalVolume,
                current.High24h,
                current.Low24h,
                current.PriceChange24h,
                current.PriceChangePercentage24h,
                current.LastUpdated),
            BuildHistory(chart));

        await _snapshotStore.SaveAsync(marketData, cancellationToken);

        return marketData;
    }

    private async Task<CoinGeckoMarketDto> GetCurrentAsync(
        string coinId,
        string currency,
        CancellationToken cancellationToken)
    {
        var path = $"coins/markets?vs_currency={currency}&ids={Uri.EscapeDataString(coinId)}&sparkline=false&price_change_percentage=24h";
        var data = await GetFromCoinGeckoAsync<List<CoinGeckoMarketDto>>(path, cancellationToken);
        var current = data.FirstOrDefault();

        return current ?? throw new MarketDataNotFoundException(coinId);
    }

    private Task<CoinGeckoChartDto> GetChartAsync(
        string coinId,
        string currency,
        int days,
        CancellationToken cancellationToken)
    {
        var path = $"coins/{Uri.EscapeDataString(coinId)}/market_chart?vs_currency={currency}&days={days}";
        return GetFromCoinGeckoAsync<CoinGeckoChartDto>(path, cancellationToken);
    }

    private async Task<T> GetFromCoinGeckoAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Add("x-cg-demo-api-key", _options.ApiKey);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new MarketDataNotFoundException(path);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new MarketDataProviderException("Market data provider is temporarily unavailable.");
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new MarketDataProviderException("Market data provider returned an empty response.");
    }

    private static IReadOnlyList<HistoricalMarketPointResponse> BuildHistory(CoinGeckoChartDto chart)
    {
        return chart.Prices
            .Select((pricePoint, index) =>
            {
                var timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)pricePoint[0]);
                var marketCap = GetOptionalValue(chart.MarketCaps, index);
                var volume = GetOptionalValue(chart.TotalVolumes, index);

                return new HistoricalMarketPointResponse(
                    timestamp,
                    ToDecimal(pricePoint[1]),
                    marketCap,
                    volume);
            })
            .ToArray();
    }

    private static decimal? GetOptionalValue(IReadOnlyList<double[]> values, int index)
    {
        return index < values.Count && values[index].Length > 1
            ? ToDecimal(values[index][1])
            : null;
    }

    private static decimal ToDecimal(double value)
    {
        return decimal.Parse(value.ToString("G17", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }

    private sealed record CoinGeckoMarketDto(
        string Id,
        string Symbol,
        string Name,
        [property: JsonPropertyName("current_price")] decimal? CurrentPrice,
        [property: JsonPropertyName("market_cap")] decimal? MarketCap,
        [property: JsonPropertyName("market_cap_rank")] int? MarketCapRank,
        [property: JsonPropertyName("total_volume")] decimal? TotalVolume,
        [property: JsonPropertyName("high_24h")] decimal? High24h,
        [property: JsonPropertyName("low_24h")] decimal? Low24h,
        [property: JsonPropertyName("price_change_24h")] decimal? PriceChange24h,
        [property: JsonPropertyName("price_change_percentage_24h")] decimal? PriceChangePercentage24h,
        [property: JsonPropertyName("last_updated")] DateTimeOffset? LastUpdated);

    private sealed record CoinGeckoChartDto(
        IReadOnlyList<double[]> Prices,
        [property: JsonPropertyName("market_caps")] IReadOnlyList<double[]> MarketCaps,
        [property: JsonPropertyName("total_volumes")] IReadOnlyList<double[]> TotalVolumes);
}
