namespace TimeClock.Core.Interfaces.Services;

/// <summary>
/// Provides the authoritative current time from an external, tamper-proof source.
/// Implements a primary/fallback strategy with Polly resilience policies.
/// </summary>
public interface ITimeProviderService
{
    /// <summary>
    /// Returns the current Zurich time together with the name of the API that provided it.
    /// Primary source: worldtimeapi.org — Fallback: timeapi.io.
    /// Throws <see cref="TimeProviderUnavailableException"/> if both sources fail.
    /// The caller is responsible for persisting the Source value in AttendanceLog.TimeSource.
    /// </summary>
    Task<(DateTimeOffset Timestamp, string Source)> GetCurrentZurichTimeAsync();
}
