using TimeClock.Core.Entities;
using TimeClock.Core.Enums;

namespace TimeClock.Infrastructure.Data;

public static class DbInitializer
{
    /// <summary>
    /// Seeds the database with default users if the Users table is empty.
    /// Call this once at application startup inside a DI scope.
    /// </summary>
    public static void Initialize(AppDbContext context)
    {
        if (context.Users.Any())
            return;   // DB already has data — nothing to do

        var users = new[]
        {
            new User
            {
                Id           = Guid.NewGuid(),
                Email        = "admin@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                FullName     = "System Admin",
                Role         = UserRole.Admin,
                CreatedAt    = DateTime.UtcNow
            },
            new User
            {
                Id           = Guid.NewGuid(),
                Email        = "user@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
                FullName     = "Test Employee",
                Role         = UserRole.Employee,
                CreatedAt    = DateTime.UtcNow
            },
            new User
            {
                Id           = Guid.NewGuid(),
                Email        = "dina@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("dina123!"),
                FullName     = "Dina",
                Role         = UserRole.Employee,
                CreatedAt    = DateTime.UtcNow
            }
        };

        context.Users.AddRange(users);
        context.SaveChanges();
    }
}
