using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TimeClock.Core.Entities;
using TimeClock.Core.Enums;
using TimeClock.Core.Exceptions;
using TimeClock.Core.Interfaces.Repositories;
using TimeClock.Core.Interfaces.Services;
using TimeClock.Services;

namespace TimeClock.Tests;

/// <summary>
/// Unit tests for <see cref="AttendanceService"/>.
///
/// Strategy
/// --------
/// • IAttendanceRepository  → mocked with Moq (no real DB)
/// • ISystemAlertRepository → mocked with Moq (side-effect capture where needed)
/// • ITimeProviderService   → mocked with Moq (returns a deterministic timestamp)
/// • ILogger                → NullLogger (swallows all log calls, no noise in output)
///
/// Pattern: Arrange → Act → Assert (AAA) throughout.
/// </summary>
public class AttendanceServiceTests
{
    // ── Shared helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a fully-wired <see cref="AttendanceService"/> with the given mocks.
    /// All mocks that are not explicitly configured still satisfy the interface
    /// contract (Moq returns Task.CompletedTask / null by default for async methods).
    /// </summary>
    private static AttendanceService BuildSut(
        Mock<IAttendanceRepository>  repoMock,
        Mock<ISystemAlertRepository> alertMock,
        Mock<ITimeProviderService>   timeMock)
    {
        return new AttendanceService(
            repoMock.Object,
            alertMock.Object,
            timeMock.Object,
            NullLogger<AttendanceService>.Instance);
    }

    // ── Test 1: Double-clock-in is rejected ───────────────────────────────────

    /// <summary>
    /// Given the user's last log is a <see cref="EventType.ClockIn"/>,
    /// a second ClockIn attempt must throw <see cref="InvalidOperationException"/>
    /// and must NOT call <see cref="IAttendanceRepository.AddAsync"/>.
    /// </summary>
    [Fact]
    public async Task ClockInAsync_WhenUserAlreadyClockedIn_ShouldThrowInvalidOperationException()
    {
        // ── Arrange ──────────────────────────────────────────────────────────
        var userId = Guid.NewGuid();

        // The repository reports that the user is already clocked in.
        var existingClockIn = new AttendanceLog
        {
            Id                = Guid.NewGuid(),
            UserId            = userId,
            EventType         = EventType.ClockIn,
            OfficialTimestamp = DateTimeOffset.UtcNow.AddHours(-2),
            TimeSource        = "worldtimeapi.org"
        };

        var repoMock  = new Mock<IAttendanceRepository>();
        var alertMock = new Mock<ISystemAlertRepository>();
        var timeMock  = new Mock<ITimeProviderService>();

        repoMock
            .Setup(r => r.GetLastLogForUserAsync(userId))
            .ReturnsAsync(existingClockIn);

        var sut = BuildSut(repoMock, alertMock, timeMock);

        // ── Act ───────────────────────────────────────────────────────────────
        Func<Task> act = () => sut.ClockInAsync(userId);

        // ── Assert ────────────────────────────────────────────────────────────

        // Must throw with the correct message.
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*already clocked in*");

        // AddAsync must never be called — no phantom log written to the DB.
        repoMock.Verify(r => r.AddAsync(It.IsAny<AttendanceLog>()), Times.Never);

        // The time provider must never be called — a DB round-trip is pointless
        // if validation already failed.
        timeMock.Verify(t => t.GetCurrentZurichTimeAsync(), Times.Never);
    }

    // ── Test 2: Successful clock-out persists the mocked timestamp ────────────

    /// <summary>
    /// Given the user is currently clocked in, a ClockOut must:
    /// 1. Retrieve the verified timestamp from <see cref="ITimeProviderService"/>.
    /// 2. Pass a log with <see cref="EventType.ClockOut"/> and the exact timestamp
    ///    returned by the mock to <see cref="IAttendanceRepository.AddAsync"/>.
    /// 3. Return that same log to the caller.
    /// </summary>
    [Fact]
    public async Task ClockOutAsync_WhenValid_ShouldSaveLogWithTimeFromMockedTimeProvider()
    {
        // ── Arrange ──────────────────────────────────────────────────────────
        var userId = Guid.NewGuid();

        // Current verified time as supplied by the (mocked) external API.
        var expectedTimestamp = new DateTimeOffset(2026, 2, 24, 9, 0, 0, TimeSpan.FromHours(1));
        const string expectedSource = "worldtimeapi.org";

        // Repository: user IS currently clocked in.
        var openClockIn = new AttendanceLog
        {
            Id                = Guid.NewGuid(),
            UserId            = userId,
            EventType         = EventType.ClockIn,
            OfficialTimestamp = expectedTimestamp.AddHours(-4),
            TimeSource        = expectedSource
        };

        var repoMock  = new Mock<IAttendanceRepository>();
        var alertMock = new Mock<ISystemAlertRepository>();
        var timeMock  = new Mock<ITimeProviderService>();

        repoMock
            .Setup(r => r.GetLastLogForUserAsync(userId))
            .ReturnsAsync(openClockIn);

        // AddAsync must complete without error (default Moq behaviour for Task,
        // but we make it explicit for clarity).
        repoMock
            .Setup(r => r.AddAsync(It.IsAny<AttendanceLog>()))
            .Returns(Task.CompletedTask);

        // Time provider returns our deterministic test value.
        timeMock
            .Setup(t => t.GetCurrentZurichTimeAsync())
            .ReturnsAsync((expectedTimestamp, expectedSource));

        var sut = BuildSut(repoMock, alertMock, timeMock);

        // ── Act ───────────────────────────────────────────────────────────────
        var result = await sut.ClockOutAsync(userId);

        // ── Assert ────────────────────────────────────────────────────────────

        // The returned log must carry ClockOut semantics.
        result.EventType.Should().Be(EventType.ClockOut);

        // The timestamp in the saved log must be exactly what the time provider returned,
        // not "DateTime.Now" or any local clock value.
        result.OfficialTimestamp.Should().Be(expectedTimestamp);

        // The source audit field must also be propagated.
        result.TimeSource.Should().Be(expectedSource);

        // Belongs to the correct user.
        result.UserId.Should().Be(userId);

        // Must not be flagged as a manually-closed shift.
        result.IsManuallyClosed.Should().BeFalse();

        // AddAsync must have been called exactly once with a ClockOut log.
        repoMock.Verify(
            r => r.AddAsync(It.Is<AttendanceLog>(l =>
                l.EventType         == EventType.ClockOut &&
                l.OfficialTimestamp == expectedTimestamp  &&
                l.UserId            == userId)),
            Times.Once);
    }

    // ── Test 3: Time-provider failure triggers Critical alert and re-throws ────

    /// <summary>
    /// When both external time APIs are unavailable, <see cref="ITimeProviderService"/>
    /// throws <see cref="TimeProviderUnavailableException"/>.
    ///
    /// The service must:
    /// 1. Re-throw <see cref="TimeProviderUnavailableException"/> so the controller
    ///    can return HTTP 503.
    /// 2. Persist exactly one <see cref="SystemAlert"/> with
    ///    <see cref="AlertSeverity.Critical"/> severity (the "hard block" audit record).
    /// 3. NOT write any <see cref="AttendanceLog"/> row — the clock-in is fully aborted.
    /// </summary>
    [Fact]
    public async Task ClockInAsync_WhenTimeProviderFails_ShouldThrowAndCreateCriticalAlert()
    {
        // ── Arrange ──────────────────────────────────────────────────────────
        var userId = Guid.NewGuid();

        var repoMock  = new Mock<IAttendanceRepository>();
        var alertMock = new Mock<ISystemAlertRepository>();
        var timeMock  = new Mock<ITimeProviderService>();

        // User is not currently clocked in — so validation passes; the failure
        // happens later, when the service tries to get a verified timestamp.
        repoMock
            .Setup(r => r.GetLastLogForUserAsync(userId))
            .ReturnsAsync((AttendanceLog?)null);

        // The time provider is completely unavailable.
        timeMock
            .Setup(t => t.GetCurrentZurichTimeAsync())
            .ThrowsAsync(new TimeProviderUnavailableException());

        // Capture every SystemAlert that is saved so we can inspect its severity.
        var savedAlerts = new List<SystemAlert>();
        alertMock
            .Setup(a => a.AddAsync(It.IsAny<SystemAlert>()))
            .Callback<SystemAlert>(alert => savedAlerts.Add(alert))
            .Returns(Task.CompletedTask);

        var sut = BuildSut(repoMock, alertMock, timeMock);

        // ── Act ───────────────────────────────────────────────────────────────
        Func<Task> act = () => sut.ClockInAsync(userId);

        // ── Assert ────────────────────────────────────────────────────────────

        // 1. The exception must propagate unchanged to the caller.
        await act.Should()
            .ThrowAsync<TimeProviderUnavailableException>();

        // 2. Exactly one Critical alert must have been written.
        savedAlerts.Should().HaveCount(1);
        savedAlerts[0].Severity.Should().Be(AlertSeverity.Critical);
        savedAlerts[0].Message.Should().Contain(userId.ToString());

        // 3. No attendance log may be written when the operation is aborted.
        repoMock.Verify(r => r.AddAsync(It.IsAny<AttendanceLog>()), Times.Never);
    }
}
