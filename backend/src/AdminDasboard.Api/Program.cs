using AdminDasboard.Api.MarketData;
using AdminDasboard.Application;
using AdminDasboard.Application.Analysis;
using AdminDasboard.Application.Assets;
using AdminDasboard.Application.MarketData;
using AdminDasboard.Application.TechnicalIndicators;
using AdminDasboard.Infrastructure;
using AdminDasboard.Infrastructure.Analysis;
using AdminDasboard.Infrastructure.MarketData;
using AdminDasboard.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "local",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));

var corsOptions = builder.Configuration
    .GetSection(CorsOptions.SectionName)
    .Get<CorsOptions>() ?? new CorsOptions();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsOptions.PolicyName, policy =>
    {
        policy
            .WithOrigins(corsOptions.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(CorsOptions.PolicyName);
app.UseRateLimiter();
app.UseExceptionHandler();

app.MapGet("/health", () => Results.Ok(new HealthResponse("ok", DateTimeOffset.UtcNow)))
    .WithName("GetHealth");
app.MapHealthChecks("/health/db", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                error = entry.Value.Exception?.Message
            })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
});

app.MapGet("/api/system/status", async (
    HealthCheckService healthCheckService,
    IConfiguration configuration,
    IOptions<CoinGeckoOptions> coinGeckoOptions,
    IOptions<OpenAiOptions> openAiOptions,
    IServiceProvider serviceProvider,
    IWebHostEnvironment environment,
    CancellationToken cancellationToken) =>
{
    var postgresConnectionString = configuration.GetConnectionString("Postgres");
    var databaseConfigured = !string.IsNullOrWhiteSpace(postgresConnectionString);
    var healthReport = await healthCheckService.CheckHealthAsync(cancellationToken);
    var appliedMigrations = Array.Empty<string>();

    if (databaseConfigured)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();
    }

    return Results.Ok(new SystemStatusResponse(
        "ok",
        environment.EnvironmentName,
        DateTimeOffset.UtcNow,
        new DatabaseStatusResponse(
            databaseConfigured,
            healthReport.Status.ToString(),
            appliedMigrations,
            true),
        new ExternalProviderStatusResponse(
            coinGeckoOptions.Value.BaseUrl,
            !string.IsNullOrWhiteSpace(coinGeckoOptions.Value.ApiKey)),
        new AiStatusResponse(
            "OpenAI",
            !string.IsNullOrWhiteSpace(openAiOptions.Value.ApiKey),
            openAiOptions.Value.Model,
            openAiOptions.Value.BaseUrl),
        new CorsStatusResponse(corsOptions.AllowedOrigins.Distinct(StringComparer.OrdinalIgnoreCase).ToArray())));
})
    .WithName("GetSystemStatus");

app.MapGet("/api/assets/search", async (
    string? query,
    int? limit,
    IAssetSearchService assetSearchService,
    CancellationToken cancellationToken) =>
{
    var trimmedQuery = query?.Trim();
    var requestedLimit = Math.Clamp(limit ?? 10, 1, 20);

    if (string.IsNullOrWhiteSpace(trimmedQuery) || trimmedQuery.Length < 2 || trimmedQuery.Length > 80)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["query"] = ["Use a search term between 2 and 80 characters."]
        });
    }

    try
    {
        var assets = await assetSearchService.SearchAsync(
            trimmedQuery,
            requestedLimit,
            cancellationToken);

        return Results.Ok(assets);
    }
    catch (MarketDataProviderException)
    {
        return Results.Problem(
            title: "Asset search provider unavailable",
            detail: "Could not search assets right now.",
            statusCode: StatusCodes.Status502BadGateway);
    }
})
    .WithName("SearchAssets");

app.MapGet("/api/market-data/{coinId}", async (
    string coinId,
    string? currency,
    int? days,
    ICryptoMarketDataService marketDataService,
    CancellationToken cancellationToken) =>
{
    var requestedDays = days ?? 30;

    if (!MarketDataEndpointValidation.TryValidate(
        coinId,
        currency,
        requestedDays,
        out var normalizedCurrency,
        out var errors))
    {
        return Results.ValidationProblem(errors);
    }

    try
    {
        var marketData = await marketDataService.GetAsync(
            coinId,
            normalizedCurrency,
            requestedDays,
            cancellationToken);

        return Results.Ok(marketData);
    }
    catch (MarketDataNotFoundException)
    {
        return Results.Problem(
            title: "Market data not found",
            detail: "The requested coin was not found.",
            statusCode: StatusCodes.Status404NotFound);
    }
    catch (MarketDataProviderException)
    {
        return Results.Problem(
            title: "Market data provider unavailable",
            detail: "Could not retrieve market data right now.",
            statusCode: StatusCodes.Status502BadGateway);
    }
})
    .WithName("GetMarketData");

app.MapGet("/api/market-data/{coinId}/indicators", async (
    string coinId,
    string? currency,
    int? days,
    ICryptoMarketDataService marketDataService,
    ITechnicalIndicatorService technicalIndicatorService,
    CancellationToken cancellationToken) =>
{
    var requestedDays = days ?? 30;

    if (!MarketDataEndpointValidation.TryValidate(
        coinId,
        currency,
        requestedDays,
        out var normalizedCurrency,
        out var errors))
    {
        return Results.ValidationProblem(errors);
    }

    try
    {
        var marketData = await marketDataService.GetAsync(
            coinId,
            normalizedCurrency,
            requestedDays,
            cancellationToken);

        return Results.Ok(technicalIndicatorService.Calculate(marketData));
    }
    catch (MarketDataNotFoundException)
    {
        return Results.Problem(
            title: "Market data not found",
            detail: "The requested coin was not found.",
            statusCode: StatusCodes.Status404NotFound);
    }
    catch (MarketDataProviderException)
    {
        return Results.Problem(
            title: "Market data provider unavailable",
            detail: "Could not retrieve market data right now.",
            statusCode: StatusCodes.Status502BadGateway);
    }
})
    .WithName("GetMarketDataIndicators");

app.MapGet("/api/market-data/snapshots", async (
    string? coinId,
    string? currency,
    int? days,
    int? offset,
    int? limit,
    IMarketDataSnapshotReader snapshotReader,
    CancellationToken cancellationToken) =>
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
        return Results.ValidationProblem(errors);
    }

    var snapshots = await snapshotReader.ListAsync(
        normalizedCoinId,
        normalizedCurrency,
        days,
        requestedOffset,
        requestedLimit,
        cancellationToken);

    return Results.Ok(snapshots);
})
    .WithName("ListMarketDataSnapshots");

app.MapPost("/api/analysis", async (
    AnalysisRequest request,
    IAnalysisService analysisService,
    CancellationToken cancellationToken) =>
{
    var requestedDays = request.Days ?? 30;

    if (!MarketDataEndpointValidation.TryValidate(
        request.CoinId,
        request.Currency,
        requestedDays,
        out var normalizedCurrency,
        out var errors))
    {
        return Results.ValidationProblem(errors);
    }

    try
    {
        var analysis = await analysisService.AnalyzeAsync(
            request.CoinId,
            normalizedCurrency,
            requestedDays,
            cancellationToken);

        return Results.Ok(analysis);
    }
    catch (AnalysisConfigurationException)
    {
        return Results.Problem(
            title: "AI analysis is not configured",
            detail: "Configure the OpenAI API key on the backend before requesting AI analysis.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (MarketDataNotFoundException)
    {
        return Results.Problem(
            title: "Market data not found",
            detail: "The requested coin was not found.",
            statusCode: StatusCodes.Status404NotFound);
    }
    catch (MarketDataProviderException)
    {
        return Results.Problem(
            title: "Market data provider unavailable",
            detail: "Could not retrieve market data right now.",
            statusCode: StatusCodes.Status502BadGateway);
    }
    catch (AnalysisProviderException)
    {
        return Results.Problem(
            title: "AI analysis provider unavailable",
            detail: "Could not retrieve AI analysis right now.",
            statusCode: StatusCodes.Status502BadGateway);
    }
})
    .WithName("CreateAnalysis");

app.MapGet("/api/analysis/history", async (
    string? coinId,
    string? currency,
    int? days,
    string? riskLevel,
    int? offset,
    int? limit,
    IAnalysisHistoryStore analysisHistoryStore,
    CancellationToken cancellationToken) =>
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
        return Results.ValidationProblem(errors);
    }

    var history = await analysisHistoryStore.ListAsync(
        normalizedCoinId,
        normalizedCurrency,
        days,
        normalizedRiskLevel,
        requestedOffset,
        requestedLimit,
        cancellationToken);

    return Results.Ok(history);
})
    .WithName("ListAnalysisHistory");

app.MapGet("/api/analysis/history/{coinId}", async (
    string coinId,
    string? currency,
    int? days,
    int? limit,
    IAnalysisHistoryStore analysisHistoryStore,
    CancellationToken cancellationToken) =>
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
        return Results.ValidationProblem(errors);
    }

    var history = await analysisHistoryStore.ListAsync(
        coinId,
        normalizedCurrency,
        requestedDays,
        requestedLimit,
        cancellationToken);

    return Results.Ok(history);
})
    .WithName("GetAnalysisHistory");

app.Run();

public sealed record HealthResponse(string Status, DateTimeOffset TimestampUtc);

public sealed record SystemStatusResponse(
    string Status,
    string Environment,
    DateTimeOffset TimestampUtc,
    DatabaseStatusResponse Database,
    ExternalProviderStatusResponse CoinGecko,
    AiStatusResponse Ai,
    CorsStatusResponse Cors);

public sealed record DatabaseStatusResponse(
    bool Configured,
    string Health,
    IReadOnlyList<string> AppliedMigrations,
    bool AutomaticMigrationsEnabled);

public sealed record ExternalProviderStatusResponse(
    string BaseUrl,
    bool ApiKeyConfigured);

public sealed record AiStatusResponse(
    string Provider,
    bool Configured,
    string Model,
    string BaseUrl);

public sealed record CorsStatusResponse(IReadOnlyList<string> AllowedOrigins);

public sealed class CorsOptions
{
    public const string SectionName = "Cors";
    public const string PolicyName = "LocalFrontend";

    public string[] AllowedOrigins { get; init; } = ["http://localhost:6001"];
}

public static class AnalysisEndpointValidation
{
    private static readonly string[] AllowedRiskLevels = ["low", "medium", "high"];

    public static bool IsAllowedRiskLevel(string riskLevel)
    {
        return AllowedRiskLevels.Contains(riskLevel, StringComparer.Ordinal);
    }
}

public partial class Program;
