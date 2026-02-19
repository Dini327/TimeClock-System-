using TimeClock.Core.Enums;

namespace TimeClock.Core.Entities;

public class AttendanceLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public EventType EventType { get; set; }

    /// <summary>
    /// The authoritative timestamp for payroll, sourced from an external time API
    /// and expressed in the Europe/Zurich timezone.
    /// </summary>
    public DateTimeOffset OfficialTimestamp { get; set; }

    /// <summary>
    /// Name of the external API that provided the timestamp (e.g. "worldtimeapi.org").
    /// Used for audit purposes.
    /// </summary>
    public string TimeSource { get; set; } = string.Empty;

    /// <summary>
    /// True when the shift was closed automatically by the system after 16 hours
    /// of inactivity, rather than by an explicit Clock Out from the employee.
    /// </summary>
    public bool IsAutoClosed { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}
