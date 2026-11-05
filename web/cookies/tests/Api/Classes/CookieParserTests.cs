namespace cookies.Tests;

public class CookieParserTests
{
    [Test]
    public void ParserImplementsTheContractAndExposesBothSetCookieStyles()
    {
        CookieParser implementation = new CookieParser();
        ICookieParser parser = implementation;
        CookieParseOptions options = new CookieParseOptions();

        SetCookieParseResult attempted = implementation.TryParseSetCookie(
            "id=1; Path=/account; HttpOnly",
            options);
        SetCookie parsed = parser.ParseSetCookie("id=2; Secure", options);

        Assert.That(attempted.Success, Is.True);
        Assert.That(attempted.Cookie!.Name, Is.EqualTo("id"));
        Assert.That(attempted.Cookie.Path, Is.EqualTo("/account"));
        Assert.That(attempted.Cookie.HttpOnly, Is.True);
        Assert.That(parsed.Value, Is.EqualTo("2"));
        Assert.That(parsed.Secure, Is.True);
    }

    [Test]
    public void SetCookieTryMethodReturnsMalformedInputWithoutThrowing()
    {
        ICookieParser parser = new CookieParser();

        SetCookieParseResult result = parser.TryParseSetCookie(
            "secret=abc\rInjected: value",
            new CookieParseOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Cookie, Is.Null);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.HeaderInjection));
        Assert.That(result.Issues, Has.Length.EqualTo(1));
    }

    [Test]
    public void SetCookieThrowingMethodUsesASafeCodeOnlyMessage()
    {
        ICookieParser parser = new CookieParser();

        CookieParseException? exception = Assert.Throws<CookieParseException>(() =>
            parser.ParseSetCookie("secret=abc\rInjected: value", new CookieParseOptions()));

        Assert.That(exception!.Message, Is.EqualTo("Cookie parsing failed with error code 2."));
        Assert.That(exception.Message, Does.Not.Contain("secret"));
        Assert.That(exception.Message, Does.Not.Contain("Injected"));
    }

    [Test]
    public void ParserHonorsCompatibleOptionsThroughThePublicBoundary()
    {
        ICookieParser parser = new CookieParser();
        CookieParseOptions options = new CookieParseOptions
        {
            ComplianceMode = CookieComplianceMode.Compatible
        };

        SetCookieParseResult result = parser.TryParseSetCookie("id=\"quoted value\"", options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.Value, Is.EqualTo("quoted value"));
        Assert.That(result.Cookie.IsValueQuoted, Is.True);
        Assert.That(result.Issues.Any(issue => issue.ErrorCode == CookieErrorCode.CompatibilityRecovery), Is.True);
    }

    [Test]
    public void ParserExposesBothOrderedCookieFieldStyles()
    {
        CookieParser implementation = new CookieParser();
        ICookieParser parser = implementation;
        string[] fields = new string[] { "a=1; a=2", "empty=; b=3" };

        CookieHeaderParseResult attempted = implementation.TryParseCookieFields(
            fields,
            new CookieParseOptions());
        CookiePair[] parsed = parser.ParseCookieFields(fields, new CookieParseOptions());

        Assert.That(attempted.Success, Is.True);
        Assert.That(attempted.Pairs.Select(pair => pair.Name), Is.EqualTo(
            new string[] { "a", "a", "empty", "b" }));
        Assert.That(parsed.Select(pair => pair.Value), Is.EqualTo(
            new string[] { "1", "2", "", "3" }));
    }

    [Test]
    public void SegmentWarningsRemainNonfatalForTheThrowingCookieFieldMethod()
    {
        ICookieParser parser = new CookieParser();

        CookiePair[] pairs = parser.ParseCookieFields(
            new string[] { "good=1; missing; =bad; final=2" },
            new CookieParseOptions());

        Assert.That(pairs.Select(pair => pair.Name), Is.EqualTo(new string[] { "good", "final" }));
    }

    [Test]
    public void FatalCookieFieldFailureIsReturnedOrThrownAsRequested()
    {
        ICookieParser parser = new CookieParser();
        string[] fields = new string[] { "good=1", "bad=2\nInjected: value" };

        CookieHeaderParseResult attempted = parser.TryParseCookieFields(fields, new CookieParseOptions());
        CookieParseException? exception = Assert.Throws<CookieParseException>(() =>
            parser.ParseCookieFields(fields, new CookieParseOptions()));

        Assert.That(attempted.Success, Is.False);
        Assert.That(attempted.Pairs, Is.Empty);
        Assert.That(attempted.ErrorCode, Is.EqualTo(CookieErrorCode.HeaderInjection));
        Assert.That(exception!.Message, Is.EqualTo("Cookie parsing failed with error code 2."));
    }

    [Test]
    public void InvalidOptionGraphsReturnStableConfigurationFailures()
    {
        ICookieParser parser = new CookieParser();
        CookieParseOptions[] invalidOptions = CreateInvalidOptions();

        foreach (CookieParseOptions options in invalidOptions)
        {
            SetCookieParseResult setCookie = parser.TryParseSetCookie("id=1", options);
            CookieHeaderParseResult cookieFields = parser.TryParseCookieFields(
                new string[] { "id=1" },
                options);

            Assert.That(setCookie.Success, Is.False);
            Assert.That(setCookie.ErrorCode, Is.EqualTo(CookieErrorCode.InvalidConfiguration));
            Assert.That(setCookie.Issues, Has.Length.EqualTo(1));
            Assert.That(setCookie.Issues[0].HasOffset, Is.False);
            Assert.That(cookieFields.Success, Is.False);
            Assert.That(cookieFields.ErrorCode, Is.EqualTo(CookieErrorCode.InvalidConfiguration));
            Assert.That(cookieFields.Issues[0].HasOffset, Is.False);
        }
    }

    [Test]
    public void NullOptionsReturnOrThrowInvalidConfiguration()
    {
        ICookieParser parser = new CookieParser();

        SetCookieParseResult attempted = parser.TryParseSetCookie("id=1", null!);
        CookieParseException? exception = Assert.Throws<CookieParseException>(() =>
            parser.ParseSetCookie("id=1", null!));

        Assert.That(attempted.ErrorCode, Is.EqualTo(CookieErrorCode.InvalidConfiguration));
        Assert.That(exception!.Message, Is.EqualTo("Cookie parsing failed with error code 43."));
    }

    [Test]
    public void NullInputsReturnOrThrowMalformedSyntax()
    {
        ICookieParser parser = new CookieParser();

        SetCookieParseResult nullSetCookie = parser.TryParseSetCookie(null!, new CookieParseOptions());
        CookieHeaderParseResult nullFields = parser.TryParseCookieFields(null!, new CookieParseOptions());
        CookieHeaderParseResult nullField = parser.TryParseCookieFields(
            new string[] { null! },
            new CookieParseOptions());

        Assert.That(nullSetCookie.ErrorCode, Is.EqualTo(CookieErrorCode.MalformedSyntax));
        Assert.That(nullSetCookie.Issues[0].HasOffset, Is.True);
        Assert.That(nullFields.ErrorCode, Is.EqualTo(CookieErrorCode.MalformedSyntax));
        Assert.That(nullField.ErrorCode, Is.EqualTo(CookieErrorCode.MalformedSyntax));
        Assert.Throws<CookieParseException>(() =>
            parser.ParseCookieFields(null!, new CookieParseOptions()));
    }

    [Test]
    public void SetCookieResultsAreIndependentMutableGraphs()
    {
        ICookieParser parser = new CookieParser();
        CookieParseOptions options = new CookieParseOptions();

        SetCookieParseResult first = parser.TryParseSetCookie("id=1; Priority=High", options);
        SetCookieParseResult second = parser.TryParseSetCookie("id=1; Priority=High", options);
        first.Cookie!.Name = "changed";
        first.Cookie.Extensions[0].Name = "changed";
        first.Issues[0].SafeFragment = "changed";

        Assert.That(second.Cookie!.Name, Is.EqualTo("id"));
        Assert.That(second.Cookie.Extensions[0].Name, Is.EqualTo("Priority"));
        Assert.That(second.Issues[0].SafeFragment, Is.EqualTo("Priority"));
    }

    [Test]
    public void CookieFieldResultsAreIndependentFromInputsAndOtherCalls()
    {
        ICookieParser parser = new CookieParser();
        string[] fields = new string[] { "a=1; missing" };

        CookieHeaderParseResult first = parser.TryParseCookieFields(fields, new CookieParseOptions());
        fields[0] = "changed=2";
        CookieHeaderParseResult second = parser.TryParseCookieFields(
            new string[] { "a=1; missing" },
            new CookieParseOptions());
        first.Pairs[0].Name = "changed";
        first.Issues[0].SafeFragment = "changed";

        Assert.That(second.Pairs[0].Name, Is.EqualTo("a"));
        Assert.That(second.Pairs[0].Value, Is.EqualTo("1"));
        Assert.That(second.Issues[0].SafeFragment, Is.EqualTo("missing"));
    }

    static CookieParseOptions[] CreateInvalidOptions()
    {
        CookieParseOptions nullLimits = new CookieParseOptions
        {
            Limits = null!
        };
        CookieParseOptions invalidMode = new CookieParseOptions
        {
            ComplianceMode = (CookieComplianceMode)99
        };
        CookieParseOptions negativeNameValue = new CookieParseOptions();
        negativeNameValue.Limits.MaximumNameValueOctets = -1;
        CookieParseOptions negativeAttribute = new CookieParseOptions();
        negativeAttribute.Limits.MaximumAttributeValueOctets = -1;
        CookieParseOptions negativeInput = new CookieParseOptions();
        negativeInput.Limits.MaximumInputLineOctets = -1;
        CookieParseOptions negativePerDomain = new CookieParseOptions();
        negativePerDomain.Limits.MaximumCookiesPerDomain = -1;
        CookieParseOptions negativeTotal = new CookieParseOptions();
        negativeTotal.Limits.MaximumCookies = -1;
        CookieParseOptions negativeHeader = new CookieParseOptions();
        negativeHeader.Limits.MaximumRequestHeaderOctets = -1;
        CookieParseOptions negativeLifetime = new CookieParseOptions();
        negativeLifetime.Limits.MaximumLifetime = TimeSpan.FromTicks(-1);
        return
        [
            nullLimits,
            invalidMode,
            negativeNameValue,
            negativeAttribute,
            negativeInput,
            negativePerDomain,
            negativeTotal,
            negativeHeader,
            negativeLifetime
        ];
    }
}
