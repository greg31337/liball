using System.Globalization;

namespace cookies.Tests;

public class SetCookieSerializerCoreTests
{
    [Test]
    public void SerializerEmitsEveryRecognizedAttributeInCanonicalOrder()
    {
        SetCookie cookie = CompleteCookie();

        CookieStringSerializationResult result = SetCookieSerializerCore.Serialize(
            cookie,
            new CookieSerializationOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Issues, Is.Empty);
        Assert.That(
            result.Value,
            Is.EqualTo(
                "id=a=b; Expires=Wed, 09 Jun 2021 10:18:14 GMT; Max-Age=-1; " +
                "Domain=Example.TEST; Path=/account; Secure; HttpOnly; SameSite=Strict; " +
                "Partitioned; Priority=High; Flag; Empty="));
    }

    [Test]
    public void SerializerOmitsAbsentAttributes()
    {
        SetCookie cookie = new SetCookie
        {
            Name = "session",
            Value = ""
        };

        CookieStringSerializationResult result = SetCookieSerializerCore.Serialize(
            cookie,
            new CookieSerializationOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.EqualTo("session="));
    }

    [TestCase(CookieSameSiteMode.Lax, "Lax")]
    [TestCase(CookieSameSiteMode.Strict, "Strict")]
    [TestCase(CookieSameSiteMode.None, "None")]
    public void SerializerUsesCanonicalSameSiteSpelling(CookieSameSiteMode mode, string expected)
    {
        SetCookie cookie = new SetCookie
        {
            Name = "id",
            Value = "1",
            HasSameSite = true,
            SameSiteMode = mode
        };

        CookieStringSerializationResult result = SetCookieSerializerCore.Serialize(
            cookie,
            new CookieSerializationOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.EqualTo("id=1; SameSite=" + expected));
    }

    [Test]
    public void CompatibleSerializationPreservesRequestedSafeQuotes()
    {
        SetCookie cookie = new SetCookie
        {
            Name = "id",
            Value = "quoted value",
            IsValueQuoted = true
        };
        CookieSerializationOptions options = new CookieSerializationOptions
        {
            ComplianceMode = CookieComplianceMode.Compatible,
            PreserveQuotedValues = true
        };

        CookieStringSerializationResult result = SetCookieSerializerCore.Serialize(cookie, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.EqualTo("id=\"quoted value\""));
    }

    [Test]
    public void CanonicalSerializationRemovesAnUnneededQuoteHint()
    {
        SetCookie cookie = new SetCookie
        {
            Name = "id",
            Value = "quoted-value",
            IsValueQuoted = true
        };
        CookieSerializationOptions options = new CookieSerializationOptions
        {
            ComplianceMode = CookieComplianceMode.Compatible,
            PreserveQuotedValues = false
        };

        CookieStringSerializationResult result = SetCookieSerializerCore.Serialize(cookie, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.EqualTo("id=quoted-value"));
    }

    [Test]
    public void QuotePreservationIsLimitedToCompatibleMode()
    {
        SetCookie cookie = new SetCookie
        {
            Name = "id",
            Value = "quoted-value",
            IsValueQuoted = true
        };
        CookieSerializationOptions options = new CookieSerializationOptions
        {
            ComplianceMode = CookieComplianceMode.Rfc6265Bis,
            PreserveQuotedValues = true
        };

        CookieStringSerializationResult result = SetCookieSerializerCore.Serialize(cookie, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.EqualTo("id=quoted-value"));
    }

    [Test]
    public void ExtensionsCanBeExcludedWithoutChangingRecognizedAttributes()
    {
        SetCookie cookie = CompleteCookie();
        CookieSerializationOptions options = new CookieSerializationOptions
        {
            IncludeExtensions = false
        };

        CookieStringSerializationResult result = SetCookieSerializerCore.Serialize(cookie, options);

        Assert.That(result.Success, Is.True);
        Assert.That(
            result.Value,
            Is.EqualTo(
                "id=a=b; Expires=Wed, 09 Jun 2021 10:18:14 GMT; Max-Age=-1; " +
                "Domain=Example.TEST; Path=/account; Secure; HttpOnly; SameSite=Strict; Partitioned"));
    }

    [TestCase(long.MinValue, "-9223372036854775808")]
    [TestCase(long.MaxValue, "9223372036854775807")]
    [TestCase(0L, "0")]
    public void MaxAgeUsesInvariantSignedDecimal(long maxAge, string expected)
    {
        SetCookie cookie = new SetCookie
        {
            Name = "id",
            Value = "1",
            HasMaxAge = true,
            MaxAgeSeconds = maxAge
        };

        CookieStringSerializationResult result = SetCookieSerializerCore.Serialize(
            cookie,
            new CookieSerializationOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.EqualTo("id=1; Max-Age=" + expected));
    }

    [Test]
    [NonParallelizable]
    public void SerializerOutputDoesNotDependOnCurrentCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            SetCookie cookie = new SetCookie
            {
                Name = "id",
                Value = "1",
                HasExpires = true,
                Expires = new DateTimeOffset(2024, 6, 9, 12, 18, 14, TimeSpan.FromHours(2)),
                HasMaxAge = true,
                MaxAgeSeconds = 1234567890
            };

            CookieStringSerializationResult result = SetCookieSerializerCore.Serialize(
                cookie,
                new CookieSerializationOptions());

            Assert.That(result.Success, Is.True);
            Assert.That(
                result.Value,
                Is.EqualTo("id=1; Expires=Sun, 09 Jun 2024 10:18:14 GMT; Max-Age=1234567890"));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Test]
    public void InvalidModelsReturnTheValidationFailureWithoutOutput()
    {
        SetCookie cookie = new SetCookie
        {
            Name = "bad name",
            Value = "1"
        };

        CookieStringSerializationResult result = SetCookieSerializerCore.Serialize(
            cookie,
            new CookieSerializationOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.InvalidName));
        Assert.That(result.Value, Is.Empty);
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].ErrorCode, Is.EqualTo(CookieErrorCode.InvalidName));
    }

    [Test]
    public void OutputLimitIsEnforcedBeforeSerializationSucceeds()
    {
        SetCookie cookie = new SetCookie
        {
            Name = "id",
            Value = "1",
            Secure = true
        };
        CookieSerializationOptions options = new CookieSerializationOptions();
        options.Limits.MaximumInputLineOctets = 11;

        CookieStringSerializationResult result = SetCookieSerializerCore.Serialize(cookie, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.InputLimitExceeded));
        Assert.That(result.Value, Is.Empty);
    }

    static SetCookie CompleteCookie()
    {
        return new SetCookie
        {
            Name = "id",
            Value = "a=b",
            HasExpires = true,
            Expires = new DateTimeOffset(2021, 6, 9, 10, 18, 14, TimeSpan.Zero),
            HasMaxAge = true,
            MaxAgeSeconds = -1,
            HasDomain = true,
            Domain = "Example.TEST",
            HasPath = true,
            Path = "/account",
            Secure = true,
            HttpOnly = true,
            HasSameSite = true,
            SameSiteMode = CookieSameSiteMode.Strict,
            Partitioned = true,
            Extensions =
            [
                new CookieAttribute { Name = "Priority", HasValue = true, Value = "High" },
                new CookieAttribute { Name = "Flag" },
                new CookieAttribute { Name = "Empty", HasValue = true, Value = "" }
            ]
        };
    }
}
