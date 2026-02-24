namespace TimeClock.Core.DTOs;

/// <summary>
/// Sent by an Admin to force-close the currently active shift for a specific user.
/// Requires an explicit end time and a documented reason for the audit trail.
/// </summary>
public class ManualShiftCloseDto
{
    public Guid UserId { get; set; }

    /// <summary>
    /// The exact end time chosen by the admin for the forced clock-out.
    /// </summary>
    public DateTimeOffset ManualEndTime { get; set; }

    /// <summary>
    /// Mandatory reason for closing the shift manually. Saved to the audit trail.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
