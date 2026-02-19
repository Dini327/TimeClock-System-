using TimeClock.Core.Enums;

namespace TimeClock.Core.DTOs;

public class AttendanceLogDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public EventType EventType { get; set; }

    /// <summary>
    /// The authoritative Zurich-time timestamp used for payroll calculations.
    /// </summary>
    public DateTimeOffset OfficialTimestamp { get; set; }

    public string TimeSource { get; set; } = string.Empty;
    public bool IsAutoClosed { get; set; }
}
