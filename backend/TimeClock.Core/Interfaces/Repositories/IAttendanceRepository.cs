using TimeClock.Core.Entities;

namespace TimeClock.Core.Interfaces.Repositories;

public interface IAttendanceRepository
{
    /// <summary>Returns the most recent log entry for a given user, regardless of type.</summary>
    Task<AttendanceLog?> GetLastLogForUserAsync(Guid userId);

    /// <summary>Returns all log entries for a given user, ordered by timestamp descending.</summary>
    Task<IEnumerable<AttendanceLog>> GetLogsForUserAsync(Guid userId);

    /// <summary>
    /// Returns the last log entry per user where the last event was a ClockIn
    /// (i.e. currently clocked-in employees). Used for the Admin live status view.
    /// </summary>
    Task<IEnumerable<AttendanceLog>> GetActiveShiftsAsync();

    /// <summary>
    /// Returns all open ClockIn entries where OfficialTimestamp is older than
    /// the given duration. Used for orphan-shift alert detection (12h threshold).
    /// </summary>
    Task<IEnumerable<AttendanceLog>> GetOpenShiftsOlderThanAsync(TimeSpan duration);

    Task AddAsync(AttendanceLog log);
    Task UpdateAsync(AttendanceLog log);
}
