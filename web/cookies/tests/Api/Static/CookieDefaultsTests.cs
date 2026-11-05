namespace cookies.Tests;

public class CookieDefaultsTests
{
    [Test]
    public void SizeAndCountDefaultsArePortable()
    {
        Assert.That(CookieDefaults.MaximumNameValueOctets, Is.EqualTo(4096));
        Assert.That(CookieDefaults.MaximumAttributeValueOctets, Is.EqualTo(1024));
        Assert.That(CookieDefaults.MaximumInputLineOctets, Is.EqualTo(65536));
        Assert.That(CookieDefaults.MaximumCookiesPerDomain, Is.EqualTo(50));
        Assert.That(CookieDefaults.MaximumCookies, Is.EqualTo(3000));
        Assert.That(CookieDefaults.MaximumRequestHeaderOctets, Is.Zero);
    }

    [Test]
    public void TimeAndPersistenceDefaultsMatchTheModernProfile()
    {
        Assert.That(CookieDefaults.MaximumLifetime, Is.EqualTo(TimeSpan.FromDays(400)));
        Assert.That(CookieDefaults.LaxAllowingUnsafeGracePeriod, Is.EqualTo(TimeSpan.FromMinutes(2)));
        Assert.That(CookieDefaults.SnapshotVersion, Is.EqualTo(1));
    }
}
