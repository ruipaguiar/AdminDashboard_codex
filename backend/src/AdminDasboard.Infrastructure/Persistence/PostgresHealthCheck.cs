using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AdminDasboard.Infrastructure.Persistence;

public sealed class PostgresHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PostgresHealthCheck(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
            await dbContext.Database.CloseConnectionAsync();

            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL connection failed.", exception);
        }
    }
}
