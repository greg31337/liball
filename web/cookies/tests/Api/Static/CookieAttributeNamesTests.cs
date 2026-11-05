namespace cookies.Tests;

public class CookieAttributeNamesTests
{
    [Test]
    public void RecognizedAttributeNamesUseCanonicalSpelling()
    {
        Assert.That(CookieAttributeNames.Expires, Is.EqualTo("Expires"));
        Assert.That(CookieAttributeNames.MaxAge, Is.EqualTo("Max-Age"));
        Assert.That(CookieAttributeNames.Domain, Is.EqualTo("Domain"));
        Assert.That(CookieAttributeNames.Path, Is.EqualTo("Path"));
        Assert.That(CookieAttributeNames.Secure, Is.EqualTo("Secure"));
        Assert.That(CookieAttributeNames.HttpOnly, Is.EqualTo("HttpOnly"));
        Assert.That(CookieAttributeNames.SameSite, Is.EqualTo("SameSite"));
        Assert.That(CookieAttributeNames.Partitioned, Is.EqualTo("Partitioned"));
    }

    [Test]
    public void CookieNamePrefixesUseCanonicalSpelling()
    {
        Assert.That(CookieAttributeNames.SecurePrefix, Is.EqualTo("__Secure-"));
        Assert.That(CookieAttributeNames.HostPrefix, Is.EqualTo("__Host-"));
    }
}
