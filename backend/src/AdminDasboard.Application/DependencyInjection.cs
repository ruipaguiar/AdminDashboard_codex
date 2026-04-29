using Microsoft.Extensions.DependencyInjection;
using AdminDasboard.Application.TechnicalIndicators;

namespace AdminDasboard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<ITechnicalIndicatorService, TechnicalIndicatorService>();

        return services;
    }
}
