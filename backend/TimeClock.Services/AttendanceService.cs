using Microsoft.Extensions.Logging;
using TimeClock.Core.Entities;
using TimeClock.Core.Enums;
using TimeClock.Core.Exceptions;
using TimeClock.Core.Interfaces.Repositories;
using TimeClock.Core.Interfaces.Services;

namespace TimeClock.Services;

public class AttendanceService : IAttendanceService
{
    private static readonly TimeSpan OrphanShiftThreshold = TimeSpan.FromHours(16);

    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ISystemAlertRepository _alertRepository;
    private readonly ITimeProviderService _timeProviderService;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        ISystemAlertRepository alertRepository,
        ITimeProviderService timeProviderService,
        ILogger<AttendanceService> logger)
    {
        _attendanceRepository = attendanceRepository;
        _alertRepository = alertRepository;
        _timeProviderService = timeProviderService;
        _logger = logger;
    }

    public async Task<AttendanceLog> ClockInAsync(Guid userId)
    {
        var lastLog = await _attendanceRepository.GetLastLogForUserAsync(userId);

        if (lastLog?.EventType == EventType.ClockIn)
        {
            var shiftAge = DateTimeOffset.UtcNow - lastLog.OfficialTimestamp;

            if (shiftAge >= OrphanShiftThreshold)
            {
                // Orphan shift: auto-close before allowing the new clock-in
                _logger.LogInformation(
                    "Orphan shift detected for user {UserId} ({Hours}h old). Auto-closing.",
                    userId, (int)shiftAge.TotalHours);

                await AutoCloseShiftAsync(lastLog);
            }
            else
            {
                throw new InvalidOperationException(
                    "Cannot clock in: the user is already clocked in.");
            }
        }

        var (timestamp, source) = await GetVerifiedTimeAsync(userId);

        var log = new AttendanceLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventType = EventType.ClockIn,
            OfficialTimestamp = timestamp,
            TimeSource = source,
            IsAutoClosed = false
        };

        await _attendanceRepository.AddAsync(log);

        _logger.LogInformation(
            "User {UserId} clocked IN at {Timestamp} via {Source}.",
            userId, timestamp, source);

        return log;
    }

    public async Task<AttendanceLog> ClockOutAsync(Guid userId)
    {
        var lastLog = await _attendanceRepository.GetLastLogForUserAsync(userId);

        if (lastLog == null || lastLog.EventType != EventType.ClockIn)
            throw new InvalidOperationException(
                "Cannot clock out: the user is not currently clocked in.");

        var (timestamp, source) = await GetVerifiedTimeAsync(userId);

        var log = new AttendanceLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventType = EventType.ClockOut,
            OfficialTimestamp = timestamp,
            TimeSource = source,
            IsAutoClosed = false
        };

        await _attendanceRepository.AddAsync(log);

        _logger.LogInformation(
            "User {UserId} clocked OUT at {Timestamp} via {Source}.",
            userId, timestamp, source);

        return log;
    }

    public async Task<AttendanceLog?> GetLastLogForUserAsync(Guid userId) =>
        await _attendanceRepository.GetLastLogForUserAsync(userId);

    public async Task<IEnumerable<AttendanceLog>> GetUserHistoryAsync(Guid userId) =>
        await _attendanceRepository.GetLogsForUserAsync(userId);

    public async Task<IEnumerable<AttendanceLog>> GetActiveShiftsAsync() =>
        await _attendanceRepository.GetActiveShiftsAsync();

    public async Task CloseOrphanShiftsAsync()
    {
        var orphans = await _attendanceRepository
            .GetOpenShiftsOlderThanAsync(OrphanShiftThreshold);

        foreach (var shift in orphans)
        {
            _logger.LogInformation(
                "Auto-closing orphan shift for user {UserId} (opened: {Timestamp}).",
                shift.UserId, shift.OfficialTimestamp);

            await AutoCloseShiftAsync(shift);
        }
    }

    public async Task<AttendanceLog> AdminCloseShiftAsync(Guid userId)
    {
        var lastLog = await _attendanceRepository.GetLastLogForUserAsync(userId);

        if (lastLog == null || lastLog.EventType != EventType.ClockIn)
            throw new InvalidOperationException(
                "Cannot close shift: the user does not have an active shift.");

        var (timestamp, source) = await GetVerifiedTimeAsync(userId);

        var closeLog = new AttendanceLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventType = EventType.AutoClose,
            OfficialTimestamp = timestamp,
            TimeSource = $"Admin Override via {source}",
            IsAutoClosed = true
        };

        await _attendanceRepository.AddAsync(closeLog);

        await _alertRepository.AddAsync(new SystemAlert
        {
            Id = Guid.NewGuid(),
            Message = $"Shift for user {userId} was manually closed by an administrator.",
            Severity = AlertSeverity.Info,
            CreatedAtUtc = DateTime.UtcNow
        });

        _logger.LogInformation(
            "Admin force-closed shift for user {UserId} at {Timestamp}.",
            userId, timestamp);

        return closeLog;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Calls ITimeProviderService. On failure, creates a Critical SystemAlert
    /// (as required by the spec) and re-throws so the controller can return 503.
    /// </summary>
    private async Task<(DateTimeOffset Timestamp, string Source)> GetVerifiedTimeAsync(Guid userId)
    {
        try
        {
            return await _timeProviderService.GetCurrentZurichTimeAsync();
        }
        catch (TimeProviderUnavailableException)
        {
            await _alertRepository.AddAsync(new SystemAlert
            {
                Id = Guid.NewGuid(),
                Message =
                    $"All time APIs failed. Clock In/Out blocked for user {userId}. " +
                    "Manual review required.",
                Severity = AlertSeverity.Critical,
                CreatedAtUtc = DateTime.UtcNow
            });

            _logger.LogCritical(
                "TimeProviderUnavailableException for user {UserId}. Critical alert created.",
                userId);

            throw;
        }
    }

    /// <summary>
    /// Creates an AutoClose log entry and a Warning alert for the given open ClockIn.
    /// The OfficialTimestamp is set to ClockIn + 16 h so payroll is deterministic
    /// and does not depend on when the system discovers the orphan.
    /// </summary>
    private async Task AutoCloseShiftAsync(AttendanceLog openClockIn)
    {
        var closeTimestamp = openClockIn.OfficialTimestamp.Add(OrphanShiftThreshold);

        await _attendanceRepository.AddAsync(new AttendanceLog
        {
            Id = Guid.NewGuid(),
            UserId = openClockIn.UserId,
            EventType = EventType.AutoClose,
            OfficialTimestamp = closeTimestamp,
            TimeSource = "System Auto-Close",
            IsAutoClosed = true
        });

        await _alertRepository.AddAsync(new SystemAlert
        {
            Id = Guid.NewGuid(),
            Message =
                $"Shift for user {openClockIn.UserId} was automatically closed after " +
                $"{OrphanShiftThreshold.TotalHours}h. " +
                $"Original ClockIn: {openClockIn.OfficialTimestamp:O}.",
            Severity = AlertSeverity.Warning,
            CreatedAtUtc = DateTime.UtcNow
        });
    }
}
