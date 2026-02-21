using Microsoft.Extensions.Logging;
using TimeClock.Core.Exceptions;
using TimeClock.Core.Interfaces.Services;

namespace TimeClock.Services;

/// <summary>
/// Implements the No-Trust Local Time strategy defined in the spec.
/// Iterates registered IExternalTimeClient implementations in DI order
/// (primary first, fallback second). Retry logic is applied to the primary
/// client's HttpClient via Polly at registration time (see InfrastructureServiceExtensions).
/// </summary>
public class TimeProviderService : ITimeProviderService
{
    private readonly IReadOnlyList<IExternalTimeClient> _clients;
    private readonly ILogger<TimeProviderService> _logger;

    // clients is resolved as IEnumerable<IExternalTimeClient> from DI.
    // Registration order = priority order: WorldTimeApiClient → TimeApiIoClient.
    public TimeProviderService(
        IEnumerable<IExternalTimeClient> clients,
        ILogger<TimeProviderService> logger)
    {
        _clients = clients.ToList();
        _logger = logger;
    }

    public async Task<(DateTimeOffset Timestamp, string Source)> GetCurrentZurichTimeAsync()
    {
        for (int i = 0; i < _clients.Count; i++)
        {
            var client = _clients[i];
            try
            {
                var time = await client.GetCurrentZurichTimeAsync();

                if (i > 0)
                    _logger.LogInformation(
                        "Fallback time source '{Source}' succeeded after primary failure.",
                        client.SourceName);

                return (time, client.SourceName);
            }
            catch (Exception ex)
            {
                bool isLast = i == _clients.Count - 1;

                _logger.LogWarning(ex,
                    "Time source '{Source}' failed ({Attempt}/{Total}). {NextAction}",
                    client.SourceName,
                    i + 1,
                    _clients.Count,
                    isLast ? "No more sources available." : "Trying next source.");
            }
        }

        // All sources exhausted.
        // Alert creation is handled by the caller (AttendanceService) so it can include
        // user context in the alert message.
        throw new TimeProviderUnavailableException();
    }
}
