using Microsoft.Extensions.Logging;
using TimeClock.Core.DTOs;
using TimeClock.Core.Entities;
using TimeClock.Core.Enums;
using TimeClock.Core.Exceptions;
using TimeClock.Core.Interfaces.Repositories;
using TimeClock.Core.Interfaces.Services;

namespace TimeClock.Services;

public class AttendanceService : IAttendanceService
{
    private static readonly TimeSpan OrphanAlertThreshold = TimeSpan.FromHours(12);

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
            throw new InvalidOperationException(
                "Cannot clock in: the user is already clocked in.");
        }

        var (timestamp, source) = await GetVerifiedTimeAsync(userId);

        var log = new AttendanceLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventType = EventType.ClockIn,
            OfficialTimestamp = timestamp,
            TimeSource = source,
            IsManuallyClosed = false
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
            IsManuallyClosed = false
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

    public async Task CheckOrphanShiftAlertsAsync()
    {
        var orphans = await _attendanceRepository
            .GetOpenShiftsOlderThanAsync(OrphanAlertThreshold);

        foreach (var shift in orphans)
        {
            var userName = shift.User?.FullName ?? shift.UserId.ToString();

            _logger.LogWarning(
                "Orphan shift detected for user {UserId} (opened: {Timestamp}). Creating alert.",
                shift.UserId, shift.OfficialTimestamp);

            await _alertRepository.AddAsync(new SystemAlert
            {
                Id = Guid.NewGuid(),
                Message = $"User {userName} has an open shift for over 12 hours.",
                Severity = AlertSeverity.Warning,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
    }

    public async Task<AttendanceLog> AdminCloseShiftAsync(ManualShiftCloseDto dto)
    {
        var lastLog = await _attendanceRepository.GetLastLogForUserAsync(dto.UserId);

        if (lastLog == null || lastLog.EventType != EventType.ClockIn)
            throw new InvalidOperationException(
                "Cannot close shift: the user does not have an active shift.");

        var closeLog = new AttendanceLog
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            EventType = EventType.ManualClose,
            OfficialTimestamp = dto.ManualEndTime,
            TimeSource = "Admin-Override",
            IsManuallyClosed = true,
            ManualCloseReason = dto.Reason
        };

        await _attendanceRepository.AddAsync(closeLog);

        var userName = lastLog.User?.FullName ?? dto.UserId.ToString();

        await _alertRepository.AddAsync(new SystemAlert
        {
            Id = Guid.NewGuid(),
            Message = $"Shift for user {userName} was manually closed by an administrator. Reason: {dto.Reason}",
            Severity = AlertSeverity.Info,
            CreatedAtUtc = DateTime.UtcNow
        });

        _logger.LogInformation(
            "Admin force-closed shift for user {UserId} with end time {EndTime}. Reason: {Reason}",
            dto.UserId, dto.ManualEndTime, dto.Reason);

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
}
