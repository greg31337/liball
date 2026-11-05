using System;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class SystemSessionClockTests
{
    [Test]
    public void SystemClockReturnsTheCurrentUtcTimeThroughTheContract()
    {
        ISessionClock clock = new SystemSessionClock();
        DateTime before = DateTime.UtcNow;

        DateTime actual = clock.GetUtcNow();

        DateTime after = DateTime.UtcNow;
        Assert.Multiple(() =>
        {
            Assert.That(actual.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(actual, Is.GreaterThanOrEqualTo(before.AddSeconds(-1)));
            Assert.That(actual, Is.LessThanOrEqualTo(after.AddSeconds(1)));
        });
    }

    [Test]
    public void FakeClockProvidesDeterministicSetAndAdvanceOperations()
    {
        DateTime initial = new DateTime(2026, 8, 30, 12, 0, 0, DateTimeKind.Utc);
        FakeSessionClock clock = new FakeSessionClock(initial);

        DateTime first = clock.GetUtcNow();
        clock.AdvanceClock(TimeSpan.FromSeconds(30));
        DateTime advanced = clock.GetUtcNow();
        DateTime reset = new DateTime(2026, 9, 1, 9, 15, 0, DateTimeKind.Utc);
        clock.SetUtcNow(reset);
        DateTime replaced = clock.GetUtcNow();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(initial));
            Assert.That(advanced, Is.EqualTo(initial.AddSeconds(30)));
            Assert.That(replaced, Is.EqualTo(reset));
            Assert.That(clock.ReadCount, Is.EqualTo(3));
        });
    }
}
