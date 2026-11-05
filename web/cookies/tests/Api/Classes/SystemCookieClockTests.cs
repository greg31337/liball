namespace cookies.Tests;

public class SystemCookieClockTests
{
    [Test]
    public void SystemClockReturnsTheCurrentUtcInstant()
    {
        ICookieClock clock = new SystemCookieClock();
        DateTimeOffset before = DateTimeOffset.UtcNow;

        DateTimeOffset value = clock.GetUtcNow();

        DateTimeOffset after = DateTimeOffset.UtcNow;
        Assert.That(value.Offset, Is.EqualTo(TimeSpan.Zero));
        Assert.That(value, Is.GreaterThanOrEqualTo(before.AddSeconds(-1)));
        Assert.That(value, Is.LessThanOrEqualTo(after.AddSeconds(1)));
    }
}
