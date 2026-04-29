using AdminDashBoard.Infrastructure.Analysis;
using AdminDashBoard.Infrastructure.MarketData;
using AdminDashBoard.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AdminDashBoard.Api.Controllers;

[ApiController]
[Route("api/system")]
public sealed class SystemController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly IConfiguration _configuration;
    private readonly IOptions<CoinGeckoOptions> _coinGeckoOptions;
    private readonly IOptions<OpenAiOptions> _openAiOptions;
    private readonly IOptions<CorsOptions> _corsOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly IWebHostEnvironment _environment;

    public SystemController(
        HealthCheckService healthCheckService,
        IConfiguration configuration,
        IOptions<CoinGeckoOptions> coinGeckoOptions,
        IOptions<OpenAiOptions> openAiOptions,
        IOptions<CorsOptions> corsOptions,
        IServiceProvider serviceProvider,
        IWebHostEnvironment environment)
    {
        _healthCheckService = healthCheckService;
        _configuration = configuration;
        _coinGeckoOptions = coinGeckoOptions;
        _openAiOptions = openAiOptions;
        _corsOptions = corsOptions;
        _serviceProvider = serviceProvider;
        _environment = environment;
    }

    [HttpGet("status", Name = "GetSystemStatus")]
    public async Task<ActionResult<SystemStatusResponse>> GetStatus(CancellationToken cancellationToken)
    {
        var postgresConnectionString = _configuration.GetConnectionString("Postgres");
        var databaseConfigured = !string.IsNullOrWhiteSpace(postgresConnectionString);
        var healthReport = await _healthCheckService.CheckHealthAsync(cancellationToken);
        var appliedMigrations = Array.Empty<string>();

        if (databaseConfigured)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();
        }

        return Ok(new SystemStatusResponse(
            "ok",
            _environment.EnvironmentName,
            DateTimeOffset.UtcNow,
            new DatabaseStatusResponse(
                databaseConfigured,
                healthReport.Status.ToString(),
                appliedMigrations,
                true),
            new ExternalProviderStatusResponse(
                _coinGeckoOptions.Value.BaseUrl,
                !string.IsNullOrWhiteSpace(_coinGeckoOptions.Value.ApiKey)),
            new AiStatusResponse(
                "OpenAI",
                !string.IsNullOrWhiteSpace(_openAiOptions.Value.ApiKey),
                _openAiOptions.Value.Model,
                _openAiOptions.Value.BaseUrl),
            new CorsStatusResponse(
                _corsOptions.Value.AllowedOrigins.Distinct(StringComparer.OrdinalIgnoreCase).ToArray())));
    }
}
