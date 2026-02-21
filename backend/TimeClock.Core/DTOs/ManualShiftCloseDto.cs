namespace TimeClock.Core.DTOs;

/// <summary>
/// Sent by an Admin to force-close the currently active shift for a specific user.
/// </summary>
public class ManualShiftCloseDto
{
    public Guid UserId { get; set; }
}
