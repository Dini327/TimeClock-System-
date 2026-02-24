using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeClock.Core.DTOs;
using TimeClock.Core.Entities;
using TimeClock.Core.Enums;
using TimeClock.Core.Interfaces.Repositories;
using TimeClock.Core.Interfaces.Services;

namespace TimeClock.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly ISystemAlertRepository _alertRepository;

    public AdminController(
        IAttendanceService attendanceService,
        ISystemAlertRepository alertRepository)
    {
        _attendanceService = attendanceService;
        _alertRepository = alertRepository;
    }

    /// <summary>
    /// Returns all users who are currently clocked in (live status for Admin dashboard).
    /// Also triggers orphan-shift alert generation for shifts open longer than 12 hours.
    /// </summary>
    [HttpGet("live-status")]
    public async Task<ActionResult<IEnumerable<AttendanceLogDto>>> GetLiveStatus()
    {
        await _attendanceService.CheckOrphanShiftAlertsAsync();
        var shifts = await _attendanceService.GetActiveShiftsAsync();
        return Ok(shifts.Select(ToDto));
    }

    /// <summary>
    /// Returns system alerts. Optionally filter by time window and/or severity.
    /// </summary>
    [HttpGet("alerts")]
    public async Task<ActionResult<IEnumerable<SystemAlertDto>>> GetAlerts(
        [FromQuery] int? withinMinutes,
        [FromQuery] AlertSeverity? severity)
    {
        IEnumerable<SystemAlert> alerts;

        if (withinMinutes.HasValue)
        {
            alerts = await _alertRepository.GetRecentAsync(withinMinutes.Value, severity);
        }
        else
        {
            alerts = await _alertRepository.GetAllAsync();
            if (severity.HasValue)
                alerts = alerts.Where(a => a.Severity == severity.Value);
        }

        return Ok(alerts.Select(a => new SystemAlertDto
        {
            Id           = a.Id,
            Message      = a.Message,
            Severity     = a.Severity,
            CreatedAtUtc = a.CreatedAtUtc
        }));
    }

    /// <summary>
    /// Force-closes the currently active shift for the specified user.
    /// Requires an explicit end time and a documented reason for the audit trail.
    /// </summary>
    [HttpPost("close-shift")]
    public async Task<ActionResult<AttendanceLogDto>> CloseShift([FromBody] ManualShiftCloseDto dto)
    {
        try
        {
            var log = await _attendanceService.AdminCloseShiftAsync(dto);
            return Ok(ToDto(log));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private static AttendanceLogDto ToDto(AttendanceLog log) => new()
    {
        Id                = log.Id,
        UserId            = log.UserId,
        FullName          = log.User?.FullName ?? string.Empty,
        Email             = log.User?.Email    ?? string.Empty,
        EventType         = log.EventType,
        OfficialTimestamp = log.OfficialTimestamp,
        TimeSource        = log.TimeSource,
        IsManuallyClosed  = log.IsManuallyClosed,
        ManualCloseReason = log.ManualCloseReason
    };
}
