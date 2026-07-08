using Microsoft.Extensions.DependencyInjection;
using i4Twins.Application.Interfaces;
using i4Twins.Application.Services;
using i4Twins.Infrastructure.Data;

namespace i4Twins.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IReadingRepository>(sp =>
            new SqliteReadingRepository(connectionString));

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IReadingService, ReadingService>();
        return services;
    }
}