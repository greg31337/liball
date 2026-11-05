namespace cookies.Tests;

public class CookieLowLevelModelTests
{
    [Test]
    public void CookiePairRetainsOrderedPairDataAndQuotedHint()
    {
        CookiePair pair = new CookiePair
        {
            Name = "session",
            Value = "a=b",
            IsQuoted = true
        };

        Assert.That(pair.Name, Is.EqualTo("session"));
        Assert.That(pair.Value, Is.EqualTo("a=b"));
        Assert.That(pair.IsQuoted, Is.True);
    }

    [Test]
    public void CookieAttributeDistinguishesFlagFromEmptyValue()
    {
        CookieAttribute flag = new CookieAttribute
        {
            Name = "Flag",
            HasValue = false,
            Value = ""
        };
        CookieAttribute emptyValue = new CookieAttribute
        {
            Name = "Empty",
            HasValue = true,
            Value = ""
        };

        Assert.That(flag.HasValue, Is.False);
        Assert.That(emptyValue.HasValue, Is.True);
        Assert.That(emptyValue.Value, Is.Empty);
    }

    [Test]
    public void SetCookieRetainsRecognizedAttributesAndExtensionOrder()
    {
        DateTimeOffset expires = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);
        CookieAttribute first = new CookieAttribute { Name = "Priority", HasValue = true, Value = "High" };
        CookieAttribute second = new CookieAttribute { Name = "Flag", HasValue = false };
        SetCookie cookie = new SetCookie
        {
            Name = "session",
            Value = "abc",
            IsValueQuoted = true,
            HasExpires = true,
            Expires = expires,
            HasMaxAge = true,
            MaxAgeSeconds = 0,
            HasDomain = true,
            Domain = "example.test",
            HasPath = true,
            Path = "/account",
            Secure = true,
            HttpOnly = true,
            HasSameSite = true,
            SameSiteMode = CookieSameSiteMode.Default,
            Partitioned = true,
            Extensions = [first, second]
        };

        Assert.That(cookie.Name, Is.EqualTo("session"));
        Assert.That(cookie.Value, Is.EqualTo("abc"));
        Assert.That(cookie.IsValueQuoted, Is.True);
        Assert.That(cookie.HasExpires, Is.True);
        Assert.That(cookie.Expires, Is.EqualTo(expires));
        Assert.That(cookie.HasMaxAge, Is.True);
        Assert.That(cookie.MaxAgeSeconds, Is.Zero);
        Assert.That(cookie.HasDomain, Is.True);
        Assert.That(cookie.Domain, Is.EqualTo("example.test"));
        Assert.That(cookie.HasPath, Is.True);
        Assert.That(cookie.Path, Is.EqualTo("/account"));
        Assert.That(cookie.Secure, Is.True);
        Assert.That(cookie.HttpOnly, Is.True);
        Assert.That(cookie.HasSameSite, Is.True);
        Assert.That(cookie.SameSiteMode, Is.EqualTo(CookieSameSiteMode.Default));
        Assert.That(cookie.Partitioned, Is.True);
        Assert.That(cookie.Extensions, Is.EqualTo(new[] { first, second }));
    }

    [Test]
    public void SetCookiePresenceFlagsDistinguishAbsentFromDefaultValues()
    {
        SetCookie absent = new SetCookie();
        SetCookie present = new SetCookie
        {
            HasExpires = true,
            Expires = default,
            HasMaxAge = true,
            MaxAgeSeconds = 0,
            HasDomain = true,
            Domain = "",
            HasPath = true,
            Path = "",
            HasSameSite = true,
            SameSiteMode = CookieSameSiteMode.Default
        };

        Assert.That(absent.HasExpires, Is.False);
        Assert.That(absent.HasMaxAge, Is.False);
        Assert.That(absent.HasDomain, Is.False);
        Assert.That(absent.HasPath, Is.False);
        Assert.That(absent.HasSameSite, Is.False);
        Assert.That(present.HasExpires, Is.True);
        Assert.That(present.HasMaxAge, Is.True);
        Assert.That(present.HasDomain, Is.True);
        Assert.That(present.HasPath, Is.True);
        Assert.That(present.HasSameSite, Is.True);
    }

    [Test]
    public void CookieIdentityDistinguishesPartitionPresenceFromItsValue()
    {
        CookieIdentity unpartitioned = new CookieIdentity
        {
            Name = "id",
            Domain = "example.test",
            Path = "/"
        };
        CookieIdentity partitioned = new CookieIdentity
        {
            Name = "id",
            Domain = "example.test",
            Path = "/",
            HasPartitionKey = true,
            PartitionKey = ""
        };

        Assert.That(unpartitioned.HasPartitionKey, Is.False);
        Assert.That(partitioned.HasPartitionKey, Is.True);
        Assert.That(partitioned.PartitionKey, Is.Empty);

        CookieStoreQuery query = new CookieStoreQuery
        {
            HasIdentity = true,
            Identity = partitioned
        };
        Assert.That(query.Identity, Is.SameAs(partitioned));
    }

    [Test]
    public void CookieIssueDistinguishesMissingAndPresentSourceLocations()
    {
        CookieIssue withoutLocation = new CookieIssue
        {
            ErrorCode = CookieErrorCode.PolicyRejection,
            Severity = CookieParseSeverity.Error,
            SafeFragment = ""
        };
        CookieIssue withLocation = new CookieIssue
        {
            ErrorCode = CookieErrorCode.InvalidAttribute,
            Severity = CookieParseSeverity.Warning,
            HasOffset = true,
            Offset = 0,
            Length = 4,
            SafeFragment = "Path"
        };

        Assert.That(withoutLocation.HasOffset, Is.False);
        Assert.That(withLocation.HasOffset, Is.True);
        Assert.That(withLocation.Offset, Is.Zero);
        Assert.That(withLocation.Length, Is.EqualTo(4));
        Assert.That(withLocation.SafeFragment, Is.EqualTo("Path"));
    }

    [Test]
    public void MatchResultCanCarryTheIncludedRequestPair()
    {
        CookiePair pair = new CookiePair { Name = "id", Value = "42" };
        CookieMatchResult result = new CookieMatchResult
        {
            Pair = pair,
            Status = CookieMatchStatus.Included
        };

        Assert.That(result.Pair, Is.SameAs(pair));
        Assert.That(result.Status, Is.EqualTo(CookieMatchStatus.Included));
    }
}
