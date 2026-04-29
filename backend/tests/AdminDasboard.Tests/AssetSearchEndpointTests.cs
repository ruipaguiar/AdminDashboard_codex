using System.Net;
using System.Net.Http.Json;
using AdminDasboard.Application.Assets;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace AdminDasboard.Tests;

public sealed class AssetSearchEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AssetSearchEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SearchAssets_WithShortQuery_ReturnsValidationProblem()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/assets/search?query=b");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchAssets_WithValidQuery_ReturnsAssets()
    {
        using var client = _factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped<IAssetSearchService, FakeAssetSearchService>();
                });
            })
            .CreateClient();

        var response = await client.GetAsync("/api/assets/search?query=bit&limit=5");
        var payload = await response.Content.ReadFromJsonAsync<AssetSearchResultResponse[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        var asset = Assert.Single(payload);
        Assert.Equal("bitcoin", asset.Id);
        Assert.Equal("btc", asset.Symbol);
        Assert.Equal(1, asset.MarketCapRank);
    }

    private sealed class FakeAssetSearchService : IAssetSearchService
    {
        public Task<IReadOnlyList<AssetSearchResultResponse>> SearchAsync(
            string query,
            int limit,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<AssetSearchResultResponse> assets =
            [
                new("bitcoin", "btc", "Bitcoin", 1, "https://assets.coingecko.com/coins/images/1/thumb/bitcoin.png")
            ];

            return Task.FromResult(assets);
        }
    }
}
