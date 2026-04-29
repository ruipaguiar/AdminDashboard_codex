using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AdminDasboard.Application.Assets;
using AdminDasboard.Application.Analysis;
using AdminDasboard.Application.MarketData;
using AdminDasboard.Infrastructure.Analysis;
using AdminDasboard.Infrastructure.MarketData;
using AdminDasboard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AdminDasboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CoinGeckoOptions>(configuration.GetSection(CoinGeckoOptions.SectionName));
        services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));

        var coinGeckoOptions = configuration
            .GetSection(CoinGeckoOptions.SectionName)
            .Get<CoinGeckoOptions>() ?? new CoinGeckoOptions();
        var openAiOptions = configuration
            .GetSection(OpenAiOptions.SectionName)
            .Get<OpenAiOptions>() ?? new OpenAiOptions();

        services.AddHttpClient<ICryptoMarketDataService, CoinGeckoMarketDataService>(client =>
        {
            client.BaseAddress = new Uri(coinGeckoOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AdminDasboard/0.1");
        });
        services.AddHttpClient<IAssetSearchService, CoinGeckoAssetSearchService>(client =>
        {
            client.BaseAddress = new Uri(coinGeckoOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AdminDasboard/0.1");
        });
        services.AddHttpClient<IAnalysisService, OpenAiAnalysisService>(client =>
        {
            client.BaseAddress = new Uri(openAiOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(45);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AdminDasboard/0.1");
        });

        var postgresConnectionString = configuration.GetConnectionString("Postgres");

        if (!string.IsNullOrWhiteSpace(postgresConnectionString))
        {
            services.AddDbContext<AppDbContext>(dbOptions =>
            {
                dbOptions.UseNpgsql(
                    postgresConnectionString,
                    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(
                        "__EFMigrationsHistory",
                        "admin_dashboard"));
            });

            services.AddHealthChecks()
                .AddCheck<PostgresHealthCheck>("postgres");

            services.AddHostedService<DatabaseMigrationHostedService>();
            services.AddScoped<IMarketDataSnapshotStore, EfCoreMarketDataSnapshotStore>();
            services.AddScoped<IMarketDataSnapshotReader, EfCoreMarketDataSnapshotReader>();
            services.AddScoped<IAnalysisHistoryStore, EfCoreAnalysisHistoryStore>();
        }
        else
        {
            services.AddHealthChecks();
            services.AddScoped<IMarketDataSnapshotStore, NoOpMarketDataSnapshotStore>();
            services.AddScoped<IMarketDataSnapshotReader, NoOpMarketDataSnapshotReader>();
            services.AddScoped<IAnalysisHistoryStore, NoOpAnalysisHistoryStore>();
        }

        return services;
    }
}
