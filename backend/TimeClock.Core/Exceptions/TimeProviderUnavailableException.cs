namespace TimeClock.Core.Exceptions;

/// <summary>
/// Thrown when both external time providers (worldtimeapi.org and timeapi.io)
/// are unreachable. Triggers a Critical SystemAlert and blocks Clock In/Out.
/// </summary>
public class TimeProviderUnavailableException : Exception
{
    public TimeProviderUnavailableException()
        : base("Service temporarily unavailable due to secure time verification failure.")
    {
    }

    public TimeProviderUnavailableException(string message): base(message)
    {
    }

    public TimeProviderUnavailableException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
