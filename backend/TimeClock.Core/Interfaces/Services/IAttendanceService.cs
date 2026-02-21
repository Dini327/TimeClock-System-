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
    /// Scans for open ClockIn entries older than 16 hours and closes them automatically
    /// with an AutoClose event. Called by a background job or on each clock-in attempt.
    /// </summary>
    Task CloseOrphanShiftsAsync();

    /// <summary>
    /// Admin override: force-closes the currently active shift for the given user,
    /// regardless of its age. Creates an AutoClose log using the current verified time.
    /// </summary>
    Task<AttendanceLog> AdminCloseShiftAsync(Guid userId);
}
