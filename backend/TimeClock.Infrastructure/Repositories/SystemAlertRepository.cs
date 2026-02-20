using Microsoft.EntityFrameworkCore;
using TimeClock.Core.Entities;
using TimeClock.Core.Enums;
using TimeClock.Core.Interfaces.Repositories;
using TimeClock.Infrastructure.Data;

namespace TimeClock.Infrastructure.Repositories;

public class SystemAlertRepository : ISystemAlertRepository
{
    private readonly AppDbContext _context;

    public SystemAlertRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SystemAlert alert)
    {
        await _context.SystemAlerts.AddAsync(alert);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<SystemAlert>> GetRecentAsync(
        int withinMinutes = 60,
        AlertSeverity? severity = null)
    {
        var since = DateTime.UtcNow.AddMinutes(-withinMinutes);

        var query = _context.SystemAlerts
            .Where(a => a.CreatedAtUtc >= since);

        if (severity.HasValue)
            query = query.Where(a => a.Severity == severity.Value);

        return await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<IEnumerable<SystemAlert>> GetAllAsync() =>
        await _context.SystemAlerts
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync();
}
