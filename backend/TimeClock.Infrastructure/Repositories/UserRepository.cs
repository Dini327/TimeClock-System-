using Microsoft.EntityFrameworkCore;
using TimeClock.Core.Entities;
using TimeClock.Core.Interfaces.Repositories;
using TimeClock.Infrastructure.Data;

namespace TimeClock.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id) =>
        await _context.Users.FindAsync(id);

    public async Task<User?> GetByEmailAsync(string email) =>
        await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

    public async Task<IEnumerable<User>> GetAllAsync() =>
        await _context.Users
            .OrderBy(u => u.FullName)
            .ToListAsync();

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}
