namespace TimeClock.Core.DTOs;

public class UserStatusDto
{
    /// <summary>"ClockedIn" or "ClockedOut"</summary>
    public string Status { get; set; } = "ClockedOut";
    public AttendanceLogDto? LastEvent { get; set; }
}
