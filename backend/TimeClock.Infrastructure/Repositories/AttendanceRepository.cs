using Microsoft.EntityFrameworkCore;
using TimeClock.Core.Entities;
using TimeClock.Core.Enums;
using TimeClock.Core.Interfaces.Repositories;
using TimeClock.Infrastructure.Data;

namespace TimeClock.Infrastructure.Repositories;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly AppDbContext _context;

    public AttendanceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AttendanceLog?> GetLastLogForUserAsync(Guid userId) =>
        await _context.AttendanceLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.OfficialTimestamp)
            .FirstOrDefaultAsync();

    public async Task<IEnumerable<AttendanceLog>> GetLogsForUserAsync(Guid userId) =>
        await _context.AttendanceLogs
            .Include(l => l.User)
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.OfficialTimestamp)
            .ToListAsync();

    public async Task<IEnumerable<AttendanceLog>> GetActiveShiftsAsync() =>
        // An active shift is a ClockIn log for which no later log exists for that user.
        // Translates to a SQL NOT EXISTS subquery.
        await _context.AttendanceLogs
            .Include(l => l.User)
            .Where(l => l.EventType == EventType.ClockIn
                     && !_context.AttendanceLogs.Any(later =>
                            later.UserId == l.UserId
                         && later.OfficialTimestamp > l.OfficialTimestamp))
            .ToListAsync();

    public async Task<IEnumerable<AttendanceLog>> GetOpenShiftsOlderThanAsync(TimeSpan duration)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(duration);

        // Open ClockIn entries older than cutoff with no subsequent log (orphan shifts).
        return await _context.AttendanceLogs
            .Include(l => l.User)
            .Where(l => l.EventType == EventType.ClockIn
                     && l.OfficialTimestamp < cutoff
                     && !_context.AttendanceLogs.Any(later =>
                            later.UserId == l.UserId
                         && later.OfficialTimestamp > l.OfficialTimestamp))
            .ToListAsync();
    }

    public async Task AddAsync(AttendanceLog log)
    {
        await _context.AttendanceLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(AttendanceLog log)
    {
        _context.AttendanceLogs.Update(log);
        await _context.SaveChangesAsync();
    }
}
