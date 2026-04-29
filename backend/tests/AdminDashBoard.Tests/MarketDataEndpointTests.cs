using System.Net;
using System.Net.Http.Json;
using AdminDashBoard.Application.MarketData;
using AdminDashBoard.Application.TechnicalIndicators;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace AdminDashBoard.Tests;

public sealed class MarketDataEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MarketDataEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMarketData_WithInvalidCurrency_ReturnsValidationProblem()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/market-data/bitcoin?currency=gbp&days=30");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMarketData_WithValidRequest_ReturnsNormalizedPayload()
    {
        using var client = _factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped<ICryptoMarketDataService, FakeMarketDataService>();
                });
            })
            .CreateClient();

        var response = await client.GetAsync("/api/market-data/bitcoin?currency=usd&days=7");
        var payload = await response.Content.ReadFromJsonAsync<CryptoMarketDataResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("bitcoin", payload.CoinId);
        Assert.Equal("usd", payload.Currency);
        Assert.Equal(7, payload.Days);
        Assert.Equal(25, payload.History.Count);
    }

    [Fact]
    public async Task GetMarketDataIndicators_WithValidRequest_ReturnsTechnicalIndicators()
    {
        using var client = _factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped<ICryptoMarketDataService, FakeMarketDataService>();
                });
            })
            .CreateClient();

        var response = await client.GetAsync("/api/market-data/bitcoin/indicators?currency=usd&days=7");
        var payload = await response.Content.ReadFromJsonAsync<TechnicalIndicatorsResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("bitcoin", payload.CoinId);
        Assert.Equal("usd", payload.Currency);
        Assert.NotEmpty(payload.Sma20);
        Assert.NotEmpty(payload.Ema20);
        Assert.NotEmpty(payload.Rsi14);
    }

    [Fact]
    public async Task ListMarketDataSnapshots_WithValidFilters_ReturnsSnapshots()
    {
        using var client = _factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped<IMarketDataSnapshotReader, FakeSnapshotReader>();
                });
            })
            .CreateClient();

        var response = await client.GetAsync("/api/market-data/snapshots?coinId=bitcoin&currency=eur&days=30&limit=10");
        var payload = await response.Content.ReadFromJsonAsync<MarketDataSnapshotListResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(1, payload.TotalCount);
        var snapshot = Assert.Single(payload.Items);
        Assert.Equal("bitcoin", snapshot.CoinId);
        Assert.Equal("eur", snapshot.Currency);
        Assert.Equal(30, snapshot.Days);
        Assert.Equal(100000m, snapshot.Price);
    }

    [Fact]
    public async Task ListMarketDataSnapshots_WithInvalidCurrency_ReturnsValidationProblem()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/market-data/snapshots?currency=gbp");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed class FakeMarketDataService : ICryptoMarketDataService
    {
        public Task<CryptoMarketDataResponse> GetAsync(
            string coinId,
            string currency,
            int days,
            CancellationToken cancellationToken)
        {
            var response = new CryptoMarketDataResponse(
                coinId,
                "btc",
                "Bitcoin",
                currency,
                days,
                new CurrentMarketDataResponse(
                    100000m,
                    2000000000000m,
                    1,
                    30000000000m,
                    101000m,
                    99000m,
                    1200m,
                    1.2m,
                    DateTimeOffset.Parse("2026-04-28T09:00:00Z")),
                Enumerable.Range(1, 25)
                    .Select(index => new HistoricalMarketPointResponse(
                        DateTimeOffset.Parse("2026-04-28T00:00:00Z").AddHours(index),
                        100000m + index,
                        2000000000000m,
                        30000000000m))
                    .ToArray());

            return Task.FromResult(response);
        }
    }

    private sealed class FakeSnapshotReader : IMarketDataSnapshotReader
    {
        public Task<MarketDataSnapshotListResponse> ListAsync(
            string? coinId,
            string? currency,
            int? days,
            int offset,
            int limit,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<MarketDataSnapshotListItemResponse> snapshots =
            [
                new(
                    Guid.Parse("6b52cf04-9467-4488-aa06-238c039df890"),
                    coinId ?? "bitcoin",
                    "btc",
                    "Bitcoin",
                    currency ?? "eur",
                    days ?? 30,
                    DateTimeOffset.Parse("2026-04-29T10:00:00Z"),
                    100000m,
                    2000000000000m,
                    30000000000m,
                    1.2m,
                    250)
            ];

            return Task.FromResult(new MarketDataSnapshotListResponse(snapshots, snapshots.Count, offset, limit));
        }
    }
}
