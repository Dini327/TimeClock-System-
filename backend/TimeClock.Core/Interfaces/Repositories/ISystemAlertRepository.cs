using TimeClock.Core.Entities;
using TimeClock.Core.Enums;

namespace TimeClock.Core.Interfaces.Repositories;

public interface ISystemAlertRepository
{
    Task AddAsync(SystemAlert alert);

    /// <summary>
    /// Returns alerts created within the last <paramref name="withinMinutes"/> minutes.
    /// Optionally filter by severity (e.g. Critical only for the admin bell indicator).
    /// </summary>
    Task<IEnumerable<SystemAlert>> GetRecentAsync(int withinMinutes = 60, AlertSeverity? severity = null);

    Task<IEnumerable<SystemAlert>> GetAllAsync();
}
