using System.Net;
using System.Net.Http.Json;
using AdminDashBoard.Application.Analysis;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace AdminDashBoard.Tests;

public sealed class AnalysisEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Disclaimer =
        "Isto \u00e9 uma an\u00e1lise automatizada com base em dados hist\u00f3ricos e indicadores t\u00e9cnicos. N\u00e3o constitui aconselhamento financeiro.";

    private readonly WebApplicationFactory<Program> _factory;

    public AnalysisEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateAnalysis_WithInvalidRange_ReturnsValidationProblem()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/analysis",
            new AnalysisRequest("bitcoin", "eur", 2));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAnalysis_WithoutOpenAiKey_ReturnsConfigurationProblem()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/analysis",
            new AnalysisRequest("bitcoin", "eur", 30));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task CreateAnalysis_WithValidRequest_ReturnsStructuredAnalysis()
    {
        using var client = await _factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped<IAnalysisService, FakeAnalysisService>();
                });
            })
            .CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/analysis",
            new AnalysisRequest("bitcoin", "eur", 30));
        var payload = await response.Content.ReadFromJsonAsync<AnalysisResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("medium", payload.RiskLevel);
        Assert.Equal(Disclaimer, payload.Disclaimer);
    }

    [Fact]
    public async Task GetAnalysisHistory_WithValidRequest_ReturnsSavedItems()
    {
        using var client = await _factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped<IAnalysisHistoryStore, FakeAnalysisHistoryStore>();
                });
            })
            .CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/analysis/history/bitcoin?currency=eur&days=30&limit=5");
        var payload = await response.Content.ReadFromJsonAsync<AnalysisHistoryItemResponse[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        var item = Assert.Single(payload);
        Assert.Equal("bitcoin", item.CoinId);
        Assert.Equal("eur", item.Currency);
        Assert.Equal(30, item.Days);
        Assert.Equal("medium", item.RiskLevel);
    }

    [Fact]
    public async Task ListAnalysisHistory_WithRiskFilter_ReturnsSavedItems()
    {
        using var client = await _factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped<IAnalysisHistoryStore, FakeAnalysisHistoryStore>();
                });
            })
            .CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/analysis/history?riskLevel=medium&limit=25");
        var payload = await response.Content.ReadFromJsonAsync<AnalysisHistoryListResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(1, payload.TotalCount);
        var item = Assert.Single(payload.Items);
        Assert.Equal("medium", item.RiskLevel);
    }

    [Fact]
    public async Task ListAnalysisHistory_WithInvalidRiskFilter_ReturnsValidationProblem()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/analysis/history?riskLevel=extreme");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed class FakeAnalysisService : IAnalysisService
    {
        public Task<AnalysisResponse> AnalyzeAsync(
            string coinId,
            string currency,
            int days,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new AnalysisResponse(
                "Momentum mixed.",
                "Sideways",
                "RSI is neutral.",
                [90000m],
                [95000m],
                "Wait for confirmation near support.",
                "Below recent support.",
                [97000m, 99000m],
                "medium",
                Disclaimer));
        }
    }

    private sealed class FakeAnalysisHistoryStore : IAnalysisHistoryStore
    {
        public Task SaveAsync(
            string coinId,
            string currency,
            int days,
            string model,
            AnalysisResponse analysis,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AnalysisHistoryItemResponse>> ListAsync(
            string coinId,
            string currency,
            int days,
            int limit,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<AnalysisHistoryItemResponse> items =
            [
                new AnalysisHistoryItemResponse(
                    Guid.Parse("0a838671-0881-4bd1-9f33-4e1b870ac501"),
                    coinId,
                    currency,
                    days,
                    "medium",
                    DateTimeOffset.Parse("2026-04-29T09:00:00Z"),
                    new AnalysisResponse(
                        "Momentum mixed.",
                        "Sideways",
                        "RSI is neutral.",
                        [90000m],
                        [95000m],
                        "Wait for confirmation near support.",
                        "Below recent support.",
                        [97000m],
                        "medium",
                        Disclaimer))
            ];

            return Task.FromResult(items);
        }

        public Task<AnalysisHistoryListResponse> ListAsync(
            string? coinId,
            string? currency,
            int? days,
            string? riskLevel,
            int offset,
            int limit,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<AnalysisHistoryItemResponse> items =
            [
                new AnalysisHistoryItemResponse(
                    Guid.Parse("0a838671-0881-4bd1-9f33-4e1b870ac501"),
                    coinId ?? "bitcoin",
                    currency ?? "eur",
                    days ?? 30,
                    riskLevel ?? "medium",
                    DateTimeOffset.Parse("2026-04-29T09:00:00Z"),
                    new AnalysisResponse(
                        "Momentum mixed.",
                        "Sideways",
                        "RSI is neutral.",
                        [90000m],
                        [95000m],
                        "Wait for confirmation near support.",
                        "Below recent support.",
                        [97000m],
                        riskLevel ?? "medium",
                        Disclaimer))
            ];

            return Task.FromResult(new AnalysisHistoryListResponse(items, items.Count, offset, limit));
        }
    }
}
