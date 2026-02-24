using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeClock.Core.DTOs;
using TimeClock.Core.Entities;
using TimeClock.Core.Enums;
using TimeClock.Core.Exceptions;
using TimeClock.Core.Interfaces.Services;

namespace TimeClock.API.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    /// <summary>Records a Clock In event for the authenticated user.</summary>
    [HttpPost("clock-in")]
    public async Task<ActionResult<AttendanceLogDto>> ClockIn()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var log = await _attendanceService.ClockInAsync(userId.Value);
            return Ok(ToDto(log, GetCurrentUserFullName()));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (TimeProviderUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }

    /// <summary>Records a Clock Out event for the authenticated user.</summary>
    [HttpPost("clock-out")]
    public async Task<ActionResult<AttendanceLogDto>> ClockOut()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var log = await _attendanceService.ClockOutAsync(userId.Value);
            return Ok(ToDto(log, GetCurrentUserFullName()));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (TimeProviderUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }

    /// <summary>Returns the current clock-in/out status for the authenticated user.</summary>
    [HttpGet("status")]
    public async Task<ActionResult<UserStatusDto>> GetStatus()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var lastLog = await _attendanceService.GetLastLogForUserAsync(userId.Value);
        var fullName = GetCurrentUserFullName();

        return Ok(new UserStatusDto
        {
            Status    = lastLog?.EventType == EventType.ClockIn ? "ClockedIn" : "ClockedOut",
            LastEvent = lastLog == null ? null : ToDto(lastLog, fullName)
        });
    }

    /// <summary>Returns the full attendance history for the authenticated user, newest first.</summary>
    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<AttendanceLogDto>>> GetHistory()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var logs = await _attendanceService.GetUserHistoryAsync(userId.Value);
        return Ok(logs.Select(l => ToDto(l, l.User?.FullName ?? string.Empty)));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private string GetCurrentUserFullName() =>
        User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    private static AttendanceLogDto ToDto(AttendanceLog log, string fullName) => new()
    {
        Id                = log.Id,
        UserId            = log.UserId,
        FullName          = fullName,
        Email             = log.User?.Email ?? string.Empty,
        EventType         = log.EventType,
        OfficialTimestamp = log.OfficialTimestamp,
        TimeSource        = log.TimeSource,
        IsManuallyClosed  = log.IsManuallyClosed,
        ManualCloseReason = log.ManualCloseReason
    };
}
