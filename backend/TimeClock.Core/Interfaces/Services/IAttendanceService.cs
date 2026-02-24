using TimeClock.Core.DTOs;
using TimeClock.Core.Entities;

namespace TimeClock.Core.Interfaces.Services;

public interface IAttendanceService
{
    /// <summary>
    /// Records a Clock In event for the given user.
    /// Validates that the user is not already clocked in.
    /// </summary>
    Task<AttendanceLog> ClockInAsync(Guid userId);

    /// <summary>
    /// Records a Clock Out event for the given user.
    /// Validates that the user is currently clocked in.
    /// </summary>
    Task<AttendanceLog> ClockOutAsync(Guid userId);

    /// <summary>Returns the most recent attendance log entry for a user.</summary>
    Task<AttendanceLog?> GetLastLogForUserAsync(Guid userId);

    /// <summary>Returns the full attendance history for a user, newest first.</summary>
    Task<IEnumerable<AttendanceLog>> GetUserHistoryAsync(Guid userId);

    /// <summary>
    /// Returns all users who are currently clocked in, for the Admin live status dashboard.
    /// </summary>
    Task<IEnumerable<AttendanceLog>> GetActiveShiftsAsync();

    /// <summary>
    /// Scans for open ClockIn entries older than 12 hours and creates a Warning
    /// SystemAlert for each one. Shifts remain open — only admins can close them.
    /// </summary>
    Task CheckOrphanShiftAlertsAsync();

    /// <summary>
    /// Admin override: force-closes the currently active shift for the given user.
    /// Uses the admin-supplied ManualEndTime, records the reason, and sets
    /// TimeSource to "Admin-Override".
    /// </summary>
    Task<AttendanceLog> AdminCloseShiftAsync(ManualShiftCloseDto dto);
}
