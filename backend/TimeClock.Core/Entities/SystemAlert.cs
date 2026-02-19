using TimeClock.Core.Enums;

namespace TimeClock.Core.Entities;

public class SystemAlert
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// Always stored in UTC. Used for audit and technical investigation.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }
}
