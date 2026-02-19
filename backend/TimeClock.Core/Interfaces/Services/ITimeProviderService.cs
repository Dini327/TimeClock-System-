namespace TimeClock.Core.Interfaces.Services;

/// <summary>
/// Provides the authoritative current time from an external, tamper-proof source.
/// Implements a primary/fallback strategy with Polly resilience policies.
/// </summary>
public interface ITimeProviderService
{
    /// <summary>
    /// Returns the current time in the Europe/Zurich timezone as a DateTimeOffset.
    /// Primary source: worldtimeapi.org — Fallback: timeapi.io.
    /// Throws <see cref="TimeProviderUnavailableException"/> if both sources fail.
    /// </summary>
    Task<DateTimeOffset> GetCurrentZurichTimeAsync();
}
