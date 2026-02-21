using Microsoft.Extensions.DependencyInjection;
using TimeClock.Core.Interfaces.Services;

namespace TimeClock.Services;

public static class ServiceRegistration
{
    /// <summary>
    /// Registers all application-layer services (business logic).
    /// Call this from Program.cs alongside AddInfrastructure().
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAttendanceService, AttendanceService>();

        // TimeProviderService is Transient: it is stateless and its IExternalTimeClient
        // dependencies are Transient typed HttpClients.
        services.AddTransient<ITimeProviderService, TimeProviderService>();

        return services;
    }
}
