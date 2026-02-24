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
    /// Used for audit purposes. Set to "Admin-Override" when an admin manually closes.
    /// </summary>
    public string TimeSource { get; set; } = string.Empty;

    /// <summary>
    /// True when this ClockOut was force-closed by an administrator rather than
    /// by an explicit Clock Out from the employee.
    /// </summary>
    public bool IsManuallyClosed { get; set; }

    /// <summary>
    /// The reason provided by the administrator when manually closing a shift.
    /// Null for normal clock-out events.
    /// </summary>
    public string? ManualCloseReason { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}
