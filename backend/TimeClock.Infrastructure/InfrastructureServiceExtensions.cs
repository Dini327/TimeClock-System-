using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using TimeClock.Core.Interfaces.Repositories;
using TimeClock.Core.Interfaces.Services;
using TimeClock.Infrastructure.Data;
using TimeClock.Infrastructure.ExternalServices;
using TimeClock.Infrastructure.Repositories;

namespace TimeClock.Infrastructure;

public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Registers all infrastructure concerns: DbContext, Repositories,
    /// and typed HttpClients for the external time APIs with Polly resilience.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Database ─────────────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));

        // ── Repositories ─────────────────────────────────────────────────────
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<ISystemAlertRepository, SystemAlertRepository>();

        // ── External Time API Clients ────────────────────────────────────────

        // Primary: worldtimeapi.org — 3 retries with exponential backoff (2s, 4s, 8s)
        services.AddHttpClient<WorldTimeApiClient>(client =>
        {
            client.BaseAddress = new Uri(
                configuration["ExternalTimeApis:WorldTimeApi:BaseUrl"]!);
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddPolicyHandler(BuildExponentialRetryPolicy());

        // Fallback: timeapi.io — no retry; fail fast so the caller can surface the error
        services.AddHttpClient<TimeApiIoClient>(client =>
        {
            client.BaseAddress = new Uri(
                configuration["ExternalTimeApis:TimeApiIo:BaseUrl"]!);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // Expose both as IExternalTimeClient in priority order.
        // TimeProviderService receives IEnumerable<IExternalTimeClient> and
        // iterates them: WorldTimeApiClient (index 0) → TimeApiIoClient (index 1).
        services.AddTransient<IExternalTimeClient>(
            sp => sp.GetRequiredService<WorldTimeApiClient>());
        services.AddTransient<IExternalTimeClient>(
            sp => sp.GetRequiredService<TimeApiIoClient>());

        return services;
    }

    // ── Polly policy ─────────────────────────────────────────────────────────

    /// <summary>
    /// Retry policy for transient HTTP errors on the primary time source:
    ///  • Network failures and HTTP 5xx (via HandleTransientHttpError)
    ///  • HTTP 408 Request Timeout
    /// Delays: 2 s → 4 s → 8 s (exponential backoff, 3 retries).
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> BuildExponentialRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => response.StatusCode == HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt)));
}
