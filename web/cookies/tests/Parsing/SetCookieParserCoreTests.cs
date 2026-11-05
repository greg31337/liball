namespace cookies.Tests;

public class SetCookieParserCoreTests
{
    [Test]
    public void ParserConsumesNameValueWhitespaceAndOnlyTheFirstEqualsSign()
    {
        SetCookieParseResult result = Parse("  session = a=b=c  ");

        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Cookie, Is.Not.Null);
        Assert.That(result.Cookie!.Name, Is.EqualTo("session"));
        Assert.That(result.Cookie.Value, Is.EqualTo("a=b=c"));
    }

    [Test]
    public void ParserAcceptsAnEmptyValue()
    {
        SetCookieParseResult result = Parse("empty=   ");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.Name, Is.EqualTo("empty"));
        Assert.That(result.Cookie.Value, Is.Empty);
    }

    [TestCase("name-without-equals", CookieErrorCode.MalformedSyntax)]
    [TestCase("=value", CookieErrorCode.InvalidName)]
    [TestCase("  = value", CookieErrorCode.InvalidName)]
    public void ParserRejectsMissingOrEmptyNames(string text, CookieErrorCode expected)
    {
        SetCookieParseResult result = Parse(text);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Cookie, Is.Null);
        Assert.That(result.ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(CookieParseSeverity.Error));
    }

    [TestCase("id=1\0x", 4)]
    [TestCase("id=1\rX-Test: x", 4)]
    [TestCase("id=1\nX-Test: x", 4)]
    public void ParserRejectsHeaderInjectionWithoutReflectingControls(string text, int offset)
    {
        SetCookieParseResult result = Parse(text);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.HeaderInjection));
        Assert.That(result.Issues[0].Offset, Is.EqualTo(offset));
        Assert.That(result.Issues[0].SafeFragment, Is.EqualTo("?"));
    }

    [Test]
    public void ParserRecognizesBaselineAndObservableModernAttributesCaseInsensitively()
    {
        SetCookieParseResult result = Parse(
            "id=1; eXpIrEs=Wed, 09 Jun 2021 10:18:14 GMT; MAX-age=60; " +
            "dOmAiN=.Example.TEST; pAtH=/account; SeCuRe=x; HTTPONLY=y; " +
            "sAmEsItE=sTrIcT; PaRtItIoNeD=value");
        SetCookie cookie = result.Cookie!;

        Assert.That(result.Success, Is.True);
        Assert.That(cookie.HasExpires, Is.True);
        Assert.That(cookie.Expires, Is.EqualTo(new DateTimeOffset(2021, 6, 9, 10, 18, 14, TimeSpan.Zero)));
        Assert.That(cookie.HasMaxAge, Is.True);
        Assert.That(cookie.MaxAgeSeconds, Is.EqualTo(60));
        Assert.That(cookie.HasDomain, Is.True);
        Assert.That(cookie.Domain, Is.EqualTo("example.test"));
        Assert.That(cookie.HasPath, Is.True);
        Assert.That(cookie.Path, Is.EqualTo("/account"));
        Assert.That(cookie.Secure, Is.True);
        Assert.That(cookie.HttpOnly, Is.True);
        Assert.That(cookie.HasSameSite, Is.True);
        Assert.That(cookie.SameSiteMode, Is.EqualTo(CookieSameSiteMode.Strict));
        Assert.That(cookie.Partitioned, Is.True);
    }

    [Test]
    public void ParserIgnoresMalformedRecognizedAttributesWithoutRejectingCookie()
    {
        SetCookieParseResult result = Parse(
            "id=1; Expires=not-a-date; Max-Age=12x; Domain=.; Path=relative; SameSite=unexpected");
        SetCookie cookie = result.Cookie!;

        Assert.That(result.Success, Is.True);
        Assert.That(cookie.HasExpires, Is.False);
        Assert.That(cookie.HasMaxAge, Is.False);
        Assert.That(cookie.HasDomain, Is.False);
        Assert.That(cookie.HasPath, Is.False);
        Assert.That(cookie.HasSameSite, Is.True);
        Assert.That(cookie.SameSiteMode, Is.EqualTo(CookieSameSiteMode.Default));
        Assert.That(result.Issues.Count(issue => issue.ErrorCode == CookieErrorCode.InvalidDate), Is.EqualTo(1));
        Assert.That(result.Issues.Count(issue => issue.ErrorCode == CookieErrorCode.InvalidAttribute), Is.EqualTo(4));
        CookieIssue dateIssue = result.Issues.Single(issue => issue.ErrorCode == CookieErrorCode.InvalidDate);
        Assert.That(dateIssue.SafeFragment, Is.EqualTo("not-a-date"));
    }

    [Test]
    public void ParserDoesNotNormalizeMultipleLeadingDomainDotsIntoAValidDomain()
    {
        SetCookieParseResult result = Parse("id=1; Domain=..example.test");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.HasDomain, Is.False);
        Assert.That(result.Issues.Select(issue => issue.ErrorCode), Does.Contain(CookieErrorCode.InvalidAttribute));
    }

    [Test]
    public void LastValidValueAttributeWinsAndDuplicatesAreReported()
    {
        SetCookieParseResult result = Parse(
            "id=1; Domain=first.test; domain=.; DOMAIN=.LAST.TEST; " +
            "Path=/first; Path=relative; Path=/last; Max-Age=10; Max-Age=x; Max-Age=-2; " +
            "Expires=Wed, 09 Jun 2021 10:18:14 GMT; Expires=bad");
        SetCookie cookie = result.Cookie!;

        Assert.That(cookie.Domain, Is.EqualTo("last.test"));
        Assert.That(cookie.Path, Is.EqualTo("/last"));
        Assert.That(cookie.MaxAgeSeconds, Is.EqualTo(-2));
        Assert.That(cookie.Expires, Is.EqualTo(new DateTimeOffset(2021, 6, 9, 10, 18, 14, TimeSpan.Zero)));
        Assert.That(result.Issues.Count(issue => issue.ErrorCode == CookieErrorCode.DuplicateAttribute), Is.EqualTo(7));
    }

    [Test]
    public void RepeatedFlagsCollapseAndSameSiteUsesTheLastOccurrence()
    {
        SetCookieParseResult result = Parse(
            "id=1; Secure; secure=x; HttpOnly; HTTPONLY=y; Partitioned; partitioned=x; " +
            "SameSite=Strict; SameSite=Lax; SameSite=None");
        SetCookie cookie = result.Cookie!;

        Assert.That(cookie.Secure, Is.True);
        Assert.That(cookie.HttpOnly, Is.True);
        Assert.That(cookie.Partitioned, Is.True);
        Assert.That(cookie.SameSiteMode, Is.EqualTo(CookieSameSiteMode.None));
        Assert.That(result.Issues.Count(issue => issue.ErrorCode == CookieErrorCode.DuplicateAttribute), Is.EqualTo(5));
    }

    [Test]
    public void UnknownSafeExtensionsPreserveSpellingValuePresenceAndOrder()
    {
        SetCookieParseResult result = Parse("id=1; Priority=High; Flag; Empty=");
        CookieAttribute[] extensions = result.Cookie!.Extensions;

        Assert.That(extensions, Has.Length.EqualTo(3));
        Assert.That(extensions[0].Name, Is.EqualTo("Priority"));
        Assert.That(extensions[0].HasValue, Is.True);
        Assert.That(extensions[0].Value, Is.EqualTo("High"));
        Assert.That(extensions[1].Name, Is.EqualTo("Flag"));
        Assert.That(extensions[1].HasValue, Is.False);
        Assert.That(extensions[2].Name, Is.EqualTo("Empty"));
        Assert.That(extensions[2].HasValue, Is.True);
        Assert.That(extensions[2].Value, Is.Empty);
        Assert.That(result.Issues.Count(issue => issue.ErrorCode == CookieErrorCode.UnknownAttribute), Is.EqualTo(3));
    }

    [Test]
    public void ParserCanDiscardUnknownExtensionsAndTheirInformationIssues()
    {
        CookieParseOptions options = new CookieParseOptions
        {
            PreserveUnknownAttributes = false,
            ReportUnknownAttributes = false
        };

        SetCookieParseResult result = SetCookieParserCore.Parse("id=1; Priority=High".AsSpan(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.Extensions, Is.Empty);
        Assert.That(result.Issues, Is.Empty);
    }

    [Test]
    public void ParserCanSuppressDuplicateAttributeIssuesWithoutChangingResolution()
    {
        CookieParseOptions options = new CookieParseOptions
        {
            ReportDuplicateAttributes = false
        };

        SetCookieParseResult result = SetCookieParserCore.Parse(
            "id=1; Path=/first; Path=/last".AsSpan(),
            options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.Path, Is.EqualTo("/last"));
        Assert.That(result.Issues.Any(issue => issue.ErrorCode == CookieErrorCode.DuplicateAttribute), Is.False);
    }

    [Test]
    public void UnsafeUnknownExtensionsAndEmptySegmentsAreIgnoredWithIssues()
    {
        SetCookieParseResult result = Parse("id=1;; Bad Name=value; Safe=ok");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.Extensions, Has.Length.EqualTo(1));
        Assert.That(result.Cookie.Extensions[0].Name, Is.EqualTo("Safe"));
        Assert.That(result.Issues.Any(issue => issue.ErrorCode == CookieErrorCode.IgnoredSegment), Is.True);
        Assert.That(result.Issues.Any(issue => issue.ErrorCode == CookieErrorCode.InvalidAttribute), Is.True);
    }

    [Test]
    public void MaxAgeOverflowUsesSaturatedSignedSeconds()
    {
        SetCookieParseResult positive = Parse("id=1; Max-Age=999999999999999999999999");
        SetCookieParseResult negative = Parse("id=1; Max-Age=-999999999999999999999999");

        Assert.That(positive.Cookie!.MaxAgeSeconds, Is.EqualTo(long.MaxValue));
        Assert.That(negative.Cookie!.MaxAgeSeconds, Is.EqualTo(long.MinValue));
    }

    [Test]
    public void DiagnosticFragmentsAreBounded()
    {
        string longName = new string('A', 40);

        SetCookieParseResult result = Parse("id=1; " + longName + "=value");

        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].SafeFragment, Is.EqualTo(new string('A', 32)));
    }

    [TestCase("payload")]
    [TestCase("=payload")]
    [TestCase("  payload  ")]
    public void ModernParserAcceptsTheNonemptyNamelessCookieForm(string text)
    {
        SetCookieParseResult result = ParseModern(text);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.Name, Is.Empty);
        Assert.That(result.Cookie.Value, Is.EqualTo("payload"));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(" = ")]
    public void ModernParserRejectsCookiesWithAnEmptyNameAndValue(string text)
    {
        SetCookieParseResult result = ParseModern(text);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.InvalidName));
    }

    [TestCase("__SeCuRe-session")]
    [TestCase("=__hOsT-session")]
    public void ModernParserRejectsCaseInsensitivePrefixesHiddenInNamelessCookies(string text)
    {
        SetCookieParseResult result = ParseModern(text);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.PrefixViolation));
    }

    [TestCase('\u0001')]
    [TestCase('\u0008')]
    [TestCase('\u000b')]
    [TestCase('\u001f')]
    [TestCase('\u007f')]
    public void ModernParserRejectsControlOctetsOtherThanHorizontalTab(char control)
    {
        SetCookieParseResult result = ParseModern("id=a" + control + "b");

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.MalformedSyntax));
        Assert.That(result.Issues[0].SafeFragment, Is.EqualTo("?"));
    }

    [Test]
    public void HorizontalTabRemainsPermittedWhitespaceInModernParsing()
    {
        SetCookieParseResult result = ParseModern("\tid\t=\tvalue\t");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.Name, Is.EqualTo("id"));
        Assert.That(result.Cookie.Value, Is.EqualTo("value"));
    }

    [Test]
    public void BaselineProfileDoesNotApplyTheModernControlRule()
    {
        SetCookieParseResult result = Parse("id=a\u0001b");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.Value, Is.EqualTo("a\u0001b"));
    }

    [Test]
    public void ModernLimitsCountUtf8OctetsForTheInputAndNameValuePair()
    {
        CookieParseOptions acceptedOptions = ModernOptions();
        acceptedOptions.Limits.MaximumInputLineOctets = 4;
        acceptedOptions.Limits.MaximumNameValueOctets = 3;
        SetCookieParseResult accepted = SetCookieParserCore.Parse("é=x".AsSpan(), acceptedOptions);

        CookieParseOptions nameValueOptions = ModernOptions();
        nameValueOptions.Limits.MaximumNameValueOctets = 2;
        SetCookieParseResult nameValueRejected = SetCookieParserCore.Parse("é=x".AsSpan(), nameValueOptions);

        CookieParseOptions inputOptions = ModernOptions();
        inputOptions.Limits.MaximumInputLineOctets = 3;
        SetCookieParseResult inputRejected = SetCookieParserCore.Parse("é=x".AsSpan(), inputOptions);

        Assert.That(accepted.Success, Is.True);
        Assert.That(nameValueRejected.ErrorCode, Is.EqualTo(CookieErrorCode.NameValueLimitExceeded));
        Assert.That(inputRejected.ErrorCode, Is.EqualTo(CookieErrorCode.InputLimitExceeded));
    }

    [Test]
    public void ModernParserIgnoresOversizedAttributesBeforeDuplicateResolution()
    {
        CookieParseOptions options = ModernOptions();
        options.Limits.MaximumAttributeValueOctets = 4;

        SetCookieParseResult result = SetCookieParserCore.Parse("id=1; Path=/éé; Path=/ok".AsSpan(), options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.Path, Is.EqualTo("/ok"));
        Assert.That(result.Issues.Count(issue => issue.ErrorCode == CookieErrorCode.AttributeLimitExceeded), Is.EqualTo(1));
        Assert.That(result.Issues.Any(issue => issue.ErrorCode == CookieErrorCode.DuplicateAttribute), Is.False);
    }

    [Test]
    public void ModernParserCapsPositiveMaxAgeAtTheConfiguredLifetime()
    {
        CookieParseOptions modernOptions = ModernOptions();
        modernOptions.Limits.MaximumLifetime = TimeSpan.FromSeconds(10);
        SetCookieParseResult modern = SetCookieParserCore.Parse("id=1; Max-Age=11".AsSpan(), modernOptions);

        CookieParseOptions baselineOptions = new CookieParseOptions();
        baselineOptions.Limits.MaximumLifetime = TimeSpan.FromSeconds(10);
        SetCookieParseResult baseline = SetCookieParserCore.Parse("id=1; Max-Age=11".AsSpan(), baselineOptions);

        Assert.That(modern.Success, Is.True);
        Assert.That(modern.Cookie!.MaxAgeSeconds, Is.EqualTo(10));
        Assert.That(modern.Issues.Any(issue => issue.ErrorCode == CookieErrorCode.LifetimeLimitExceeded), Is.True);
        Assert.That(baseline.Cookie!.MaxAgeSeconds, Is.EqualTo(11));
        Assert.That(baseline.Issues.Any(issue => issue.ErrorCode == CookieErrorCode.LifetimeLimitExceeded), Is.False);
    }

    [Test]
    public void ModernParserUsesTheDefaultFourHundredDayLifetimeCap()
    {
        SetCookieParseResult result = ParseModern("id=1; Max-Age=34560001");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.MaxAgeSeconds, Is.EqualTo(34560000));
        Assert.That(result.Issues.Any(issue => issue.ErrorCode == CookieErrorCode.LifetimeLimitExceeded), Is.True);
    }

    [TestCase("sTrIcT", CookieSameSiteMode.Strict)]
    [TestCase("lAx", CookieSameSiteMode.Lax)]
    [TestCase("nOnE", CookieSameSiteMode.None)]
    [TestCase("unknown", CookieSameSiteMode.Default)]
    public void ModernSameSiteRecognitionIsCaseInsensitiveAndDefaultsUnknownValues(
        string value,
        CookieSameSiteMode expected)
    {
        SetCookieParseResult result = ParseModern("id=1; sAmEsItE=" + value);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.HasSameSite, Is.True);
        Assert.That(result.Cookie.SameSiteMode, Is.EqualTo(expected));
    }

    [Test]
    public void CompatibleProfileUsesTheModernSafetyFloor()
    {
        CookieParseOptions options = new CookieParseOptions
        {
            ComplianceMode = CookieComplianceMode.Compatible
        };

        SetCookieParseResult result = SetCookieParserCore.Parse("id=a\u007fb".AsSpan(), options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.MalformedSyntax));
    }

    static SetCookieParseResult Parse(string text)
    {
        return SetCookieParserCore.Parse(text.AsSpan(), new CookieParseOptions());
    }

    static SetCookieParseResult ParseModern(string text)
    {
        return SetCookieParserCore.Parse(text.AsSpan(), ModernOptions());
    }

    static CookieParseOptions ModernOptions()
    {
        return new CookieParseOptions
        {
            ComplianceMode = CookieComplianceMode.Rfc6265Bis
        };
    }
}
