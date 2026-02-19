namespace TimeClock.Core.DTOs;

/// <summary>
/// Sent by an Admin to manually close an orphan shift.
/// </summary>
public class ManualShiftCloseDto
{
    public Guid LogId { get; set; }
}
