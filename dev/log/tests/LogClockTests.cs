using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogClockTests
{
    [Test]
    public void SystemClock_ReturnsCurrentUtcTime()
    {
        LogClock clock = LogClock.SystemClock();
        DateTimeOffset before = DateTimeOffset.UtcNow;

        DateTimeOffset now = clock.UtcNow();

        DateTimeOffset after = DateTimeOffset.UtcNow;
        Assert.That(clock.IsFixed, Is.False);
        Assert.That(now, Is.GreaterThanOrEqualTo(before));
        Assert.That(now, Is.LessThanOrEqualTo(after));
    }

    [Test]
    public void Fixed_ReturnsControlledUtcTime()
    {
        DateTimeOffset fixedUtcNow = new DateTimeOffset(2026, 8, 2, 16, 0, 0, TimeSpan.Zero);
        LogClock clock = LogClock.Fixed(fixedUtcNow);

        Assert.That(clock.IsFixed, Is.True);
        Assert.That(clock.UtcNow(), Is.EqualTo(fixedUtcNow));
    }

    [Test]
    public void SetFixedUtcNow_ChangesControlledTime()
    {
        DateTimeOffset first = new DateTimeOffset(2026, 8, 2, 16, 0, 0, TimeSpan.Zero);
        DateTimeOffset second = new DateTimeOffset(2026, 8, 2, 16, 1, 0, TimeSpan.Zero);
        LogClock clock = LogClock.Fixed(first);

        clock.SetFixedUtcNow(second);

        Assert.That(clock.IsFixed, Is.True);
        Assert.That(clock.UtcNow(), Is.EqualTo(second));
    }

    [Test]
    public void UseSystemUtcNow_ReturnsClockToSystemTime()
    {
        DateTimeOffset fixedUtcNow = new DateTimeOffset(2026, 8, 2, 16, 0, 0, TimeSpan.Zero);
        LogClock clock = LogClock.Fixed(fixedUtcNow);

        clock.UseSystemUtcNow();
        DateTimeOffset now = clock.UtcNow();

        Assert.That(clock.IsFixed, Is.False);
        Assert.That(now, Is.Not.EqualTo(fixedUtcNow));
    }

    [Test]
    public void LogEventCreateWithClock_UsesControlledTimestamp()
    {
        DateTimeOffset fixedUtcNow = new DateTimeOffset(2026, 8, 2, 16, 0, 0, TimeSpan.Zero);
        LogClock clock = LogClock.Fixed(fixedUtcNow);

        LogEvent logEvent = LogEvent.CreateWithClock(clock, LogLevel.Info, "Application.Worker", "template", "rendered", new LogField[0]);

        Assert.That(logEvent.Timestamp, Is.EqualTo(fixedUtcNow));
        Assert.That(logEvent.MessageTemplate, Is.EqualTo("template"));
        Assert.That(logEvent.RenderedMessage, Is.EqualTo("rendered"));
    }
}
