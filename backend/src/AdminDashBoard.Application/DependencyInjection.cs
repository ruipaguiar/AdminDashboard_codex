using Microsoft.Extensions.DependencyInjection;
using AdminDashBoard.Application.TechnicalIndicators;

namespace AdminDashBoard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<ITechnicalIndicatorService, TechnicalIndicatorService>();

        return services;
    }
}
