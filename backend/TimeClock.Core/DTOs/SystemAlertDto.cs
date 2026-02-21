using TimeClock.Core.Enums;

namespace TimeClock.Core.DTOs;

public class SystemAlertDto
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
