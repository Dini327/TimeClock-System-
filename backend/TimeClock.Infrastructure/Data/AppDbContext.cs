using Microsoft.EntityFrameworkCore;
using TimeClock.Core.Entities;
using TimeClock.Core.Enums;

namespace TimeClock.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<AttendanceLog> AttendanceLogs => Set<AttendanceLog>();
    public DbSet<SystemAlert> SystemAlerts => Set<SystemAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Email)
                  .IsRequired()
                  .HasMaxLength(256);

            entity.HasIndex(u => u.Email)
                  .IsUnique();

            entity.Property(u => u.PasswordHash)
                  .IsRequired();

            entity.Property(u => u.FullName)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(u => u.Role)
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .IsRequired();

            entity.Property(u => u.CreatedAt)
                  .IsRequired();
        });

        modelBuilder.Entity<AttendanceLog>(entity =>
        {
            entity.HasKey(a => a.Id);

            // Stored as SQL Server datetimeoffset — preserves the Zurich UTC offset for audit.
            entity.Property(a => a.OfficialTimestamp)
                  .IsRequired();

            entity.Property(a => a.EventType)
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .IsRequired();

            entity.Property(a => a.TimeSource)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(a => a.IsAutoClosed)
                  .IsRequired()
                  .HasDefaultValue(false);

            entity.HasOne(a => a.User)
                  .WithMany(u => u.AttendanceLogs)
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SystemAlert>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Message)
                  .IsRequired();

            entity.Property(s => s.Severity)
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .IsRequired();

            entity.Property(s => s.CreatedAtUtc)
                  .IsRequired();
        });
    }
}
