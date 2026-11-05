using NUnit.Framework;

namespace cache.Tests;

[TestFixture]
public class CacheClockTests
{
    [Test]
    public void SystemUtcClock_ReturnsCurrentUtcTicks()
    {
        long before = DateTime.UtcNow.Ticks;
        CacheClock clock = CacheClock.SystemUtc();
        long actual = clock.GetUtcTicks();
        long after = DateTime.UtcNow.Ticks;

        Assert.That(clock.UsesManualTicks, Is.False);
        Assert.That(actual, Is.GreaterThanOrEqualTo(before));
        Assert.That(actual, Is.LessThanOrEqualTo(after));
    }

    [Test]
    public void DefaultConstructor_UsesSystemUtcTicks()
    {
        CacheClock clock = new CacheClock();

        Assert.That(clock.UsesManualTicks, Is.False);
        Assert.That(clock.GetUtcTicks(), Is.GreaterThan(0L));
    }

    [Test]
    public void ManualClock_ReturnsConfiguredTicks()
    {
        CacheClock clock = CacheClock.Manual(1234L);

        Assert.That(clock.UsesManualTicks, Is.True);
        Assert.That(clock.GetUtcTicks(), Is.EqualTo(1234L));
    }

    [Test]
    public void SetUtcTicks_SwitchesToManualTime()
    {
        CacheClock clock = CacheClock.SystemUtc();

        clock.SetUtcTicks(5000L);

        Assert.That(clock.UsesManualTicks, Is.True);
        Assert.That(clock.ManualUtcTicks, Is.EqualTo(5000L));
        Assert.That(clock.GetUtcTicks(), Is.EqualTo(5000L));
    }

    [Test]
    public void AdvanceTicks_AddsToManualTime()
    {
        CacheClock clock = CacheClock.Manual(100L);

        clock.AdvanceTicks(25L);

        Assert.That(clock.GetUtcTicks(), Is.EqualTo(125L));
    }

    [Test]
    public void AdvanceMilliseconds_UsesDotNetTickDuration()
    {
        CacheClock clock = CacheClock.Manual(100L);

        clock.AdvanceMilliseconds(3L);

        Assert.That(clock.GetUtcTicks(), Is.EqualTo(30100L));
    }

    [Test]
    public void AdvanceSeconds_UsesDotNetTickDuration()
    {
        CacheClock clock = CacheClock.Manual(100L);

        clock.AdvanceSeconds(2L);

        Assert.That(clock.GetUtcTicks(), Is.EqualTo(20000100L));
    }

    [Test]
    public void AdvanceTicks_WhenResultExceedsLongRange_Saturates()
    {
        CacheClock upper = CacheClock.Manual(long.MaxValue - 1L);
        CacheClock lower = CacheClock.Manual(long.MinValue + 1L);

        upper.AdvanceTicks(2L);
        lower.AdvanceTicks(-2L);

        Assert.That(upper.GetUtcTicks(), Is.EqualTo(long.MaxValue));
        Assert.That(lower.GetUtcTicks(), Is.EqualTo(long.MinValue));
    }

    [Test]
    public void AdvanceScaledTime_WhenScalingExceedsLongRange_SaturatesFinalResult()
    {
        CacheClock milliseconds = CacheClock.Manual(long.MinValue);
        CacheClock seconds = CacheClock.Manual(long.MaxValue);

        milliseconds.AdvanceMilliseconds(long.MaxValue);
        seconds.AdvanceSeconds(long.MinValue);

        Assert.That(milliseconds.GetUtcTicks(), Is.EqualTo(long.MaxValue));
        Assert.That(seconds.GetUtcTicks(), Is.EqualTo(long.MinValue));
    }

    [Test]
    public void UseSystemUtcTicks_SwitchesBackToSystemTime()
    {
        CacheClock clock = CacheClock.Manual(100L);

        clock.UseSystemUtcTicks();

        Assert.That(clock.UsesManualTicks, Is.False);
        Assert.That(clock.GetUtcTicks(), Is.GreaterThan(100L));
    }
}
