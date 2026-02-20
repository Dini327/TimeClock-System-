namespace TimeClock.Core.Interfaces.Services;

/// <summary>
/// Abstraction for a single external time source.
/// Implemented by WorldTimeApiClient and TimeApiIoClient in the Infrastructure layer.
/// The retry / failover orchestration lives in TimeProviderService (Services layer).
/// </summary>
public interface IExternalTimeClient
{
    /// <summary>Human-readable name stored in AttendanceLog.TimeSource for audit.</summary>
    string SourceName { get; }

    /// <summary>Fetches the current time for the Europe/Zurich timezone.</summary>
    Task<DateTimeOffset> GetCurrentZurichTimeAsync();
}
