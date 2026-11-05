namespace cookies.Tests;

public class SetCookieParserConformanceTests
{
    [Test]
    public void ProducerGrammarTokenAndCookieOctetClassesAreAccepted()
    {
        string tokenName = "AZaz09!#$%&'*+-.^_`|~";
        string cookieOctets =
            "!#$%&'()*+-./0123456789:<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`" +
            "abcdefghijklmnopqrstuvwxyz{|}~";

        SetCookieParseResult unquoted = ParseModern(
            tokenName + "=" + cookieOctets);
        SetCookieParseResult quoted = ParseModern("quoted=\"value\"");

        Assert.That(unquoted.Success, Is.True);
        Assert.That(unquoted.Cookie!.Name, Is.EqualTo(tokenName));
        Assert.That(unquoted.Cookie.Value, Is.EqualTo(cookieOctets));
        Assert.That(quoted.Success, Is.True);
        Assert.That(quoted.Cookie!.Value, Is.EqualTo("\"value\""));
        Assert.That(quoted.Cookie.IsValueQuoted, Is.False);
    }

    [Test]
    public void ConsumerAlgorithmAcceptsProducerDeviationsWithoutDecoding()
    {
        SetCookieParseResult result = ParseModern(
            "  bad name = a b,c%3Btail\\quoted  ; Path = /a=b ; Secure = ignored");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.Name, Is.EqualTo("bad name"));
        Assert.That(result.Cookie.Value, Is.EqualTo("a b,c%3Btail\\quoted"));
        Assert.That(result.Cookie.Path, Is.EqualTo("/a=b"));
        Assert.That(result.Cookie.Secure, Is.True);
    }

    [Test]
    public void ConsumerUsesFirstSemicolonAndFirstEqualsWithoutCommaSplitting()
    {
        SetCookieParseResult result = ParseModern(
            "id=one=two,three; Expires=Wed, 09 Jun 2021 10:18:14 GMT; Flag=a=b");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.Value, Is.EqualTo("one=two,three"));
        Assert.That(result.Cookie.HasExpires, Is.True);
        Assert.That(result.Cookie.Expires, Is.EqualTo(
            new DateTimeOffset(2021, 6, 9, 10, 18, 14, TimeSpan.Zero)));
        Assert.That(result.Cookie.Extensions.Single().Name, Is.EqualTo("Flag"));
        Assert.That(result.Cookie.Extensions[0].Value, Is.EqualTo("a=b"));
    }

    [TestCase("payload; Secure; Path=/", "payload")]
    [TestCase("=payload; Secure; Path=/", "payload")]
    [TestCase("\t payload \t; Secure; Path=/", "payload")]
    public void ModernNamelessFormsContinueIntoAttributeConsumption(
        string text,
        string expectedValue)
    {
        SetCookieParseResult result = ParseModern(text);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.Name, Is.Empty);
        Assert.That(result.Cookie.Value, Is.EqualTo(expectedValue));
        Assert.That(result.Cookie.Secure, Is.True);
        Assert.That(result.Cookie.Path, Is.EqualTo("/"));
    }

    [Test]
    public void WhitespaceIsTrimmedAtEveryPairAndAttributeBoundary()
    {
        SetCookieParseResult result = ParseModern(
            "\t id \t=\t value \t;\t Domain \t=\t .EXAMPLE.TEST \t; " +
            "Path = /account \t; SameSite = Lax \t");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.Name, Is.EqualTo("id"));
        Assert.That(result.Cookie.Value, Is.EqualTo("value"));
        Assert.That(result.Cookie.Domain, Is.EqualTo("example.test"));
        Assert.That(result.Cookie.Path, Is.EqualTo("/account"));
        Assert.That(result.Cookie.SameSiteMode, Is.EqualTo(CookieSameSiteMode.Lax));
    }

    [Test]
    public void MissingValuesIgnoreValueAttributesButFlagsStillCollapseToTrue()
    {
        SetCookieParseResult result = ParseModern(
            "id=1; Expires; Max-Age=; Domain; Path=; SameSite; " +
            "Secure=ignored; HttpOnly=; Partitioned=ignored; =orphan");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.HasExpires, Is.False);
        Assert.That(result.Cookie.HasMaxAge, Is.False);
        Assert.That(result.Cookie.HasDomain, Is.False);
        Assert.That(result.Cookie.HasPath, Is.False);
        Assert.That(result.Cookie.HasSameSite, Is.True);
        Assert.That(result.Cookie.SameSiteMode,
            Is.EqualTo(CookieSameSiteMode.Default));
        Assert.That(result.Cookie.Secure, Is.True);
        Assert.That(result.Cookie.HttpOnly, Is.True);
        Assert.That(result.Cookie.Partitioned, Is.True);
        Assert.That(result.Issues.Count(issue =>
            issue.ErrorCode == CookieErrorCode.InvalidAttribute), Is.EqualTo(6));
    }

    [TestCase("1", 1L)]
    [TestCase("01", 1L)]
    [TestCase("0", 0L)]
    [TestCase("-1", -1L)]
    [TestCase("-0", 0L)]
    public void LiberalMaxAgeConsumptionAcceptsDeployedIntegerForms(
        string value,
        long expected)
    {
        SetCookieParseResult result = ParseModern("id=1; Max-Age=" + value);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.HasMaxAge, Is.True);
        Assert.That(result.Cookie.MaxAgeSeconds, Is.EqualTo(expected));
    }

    [TestCase("")]
    [TestCase("+")]
    [TestCase("+1")]
    [TestCase("--1")]
    [TestCase("1x")]
    [TestCase(" 1 2 ")]
    public void LiberalMaxAgeConsumptionIgnoresNonDecimalForms(string value)
    {
        SetCookieParseResult result = ParseModern("id=1; Max-Age=" + value);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.HasMaxAge, Is.False);
        Assert.That(result.Issues.Single().ErrorCode,
            Is.EqualTo(CookieErrorCode.InvalidAttribute));
    }

    [Test]
    public void DuplicateResolutionRetainsPriorValidValuesExceptSameSiteDefault()
    {
        SetCookieParseResult result = ParseModern(
            "id=1; Expires=Wed, 09 Jun 2021 10:18:14 GMT; Expires=bad; " +
            "Max-Age=10; Max-Age=bad; Domain=example.test; Domain=.; " +
            "Path=/good; Path=bad; SameSite=Strict; SameSite=unknown");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.HasExpires, Is.True);
        Assert.That(result.Cookie.Expires.Year, Is.EqualTo(2021));
        Assert.That(result.Cookie.HasMaxAge, Is.True);
        Assert.That(result.Cookie.MaxAgeSeconds, Is.EqualTo(10));
        Assert.That(result.Cookie.HasDomain, Is.True);
        Assert.That(result.Cookie.Domain, Is.EqualTo("example.test"));
        Assert.That(result.Cookie.HasPath, Is.True);
        Assert.That(result.Cookie.Path, Is.EqualTo("/good"));
        Assert.That(result.Cookie.HasSameSite, Is.True);
        Assert.That(result.Cookie.SameSiteMode,
            Is.EqualTo(CookieSameSiteMode.Default));
        Assert.That(result.Issues.Count(issue =>
            issue.ErrorCode == CookieErrorCode.DuplicateAttribute), Is.EqualTo(5));
        Assert.That(result.Issues.Count(issue =>
            issue.ErrorCode == CookieErrorCode.InvalidAttribute), Is.EqualTo(4));
        Assert.That(result.Issues.Count(issue =>
            issue.ErrorCode == CookieErrorCode.InvalidDate), Is.EqualTo(1));
    }

    [Test]
    public void UnknownAttributesPreserveFirstEqualsAndAcceptedAvOctets()
    {
        SetCookieParseResult result = ParseModern(
            "id=1; X=a=b; Flag; Empty=; Commas=a,b; =orphan; Bad Name=value");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Cookie!.Extensions.Select(extension => extension.Name),
            Is.EqualTo(new[] { "X", "Flag", "Empty", "Commas" }));
        Assert.That(result.Cookie.Extensions.Select(extension => extension.Value),
            Is.EqualTo(new[] { "a=b", "", "", "a,b" }));
        Assert.That(result.Cookie.Extensions.Select(extension => extension.HasValue),
            Is.EqualTo(new[] { true, false, true, true }));
        Assert.That(result.Issues.Count(issue =>
            issue.ErrorCode == CookieErrorCode.UnknownAttribute), Is.EqualTo(4));
        Assert.That(result.Issues.Count(issue =>
            issue.ErrorCode == CookieErrorCode.InvalidAttribute), Is.EqualTo(2));
    }

    [Test]
    public void ModernOctetLimitsAcceptExactBoundariesAndRejectOnlyAboveThem()
    {
        CookieParseOptions inputBelow = ModernOptions();
        inputBelow.Limits.MaximumInputLineOctets = 3;
        CookieParseOptions inputExact = ModernOptions();
        inputExact.Limits.MaximumInputLineOctets = 4;
        CookieParseOptions inputAbove = ModernOptions();
        inputAbove.Limits.MaximumInputLineOctets = 5;
        CookieParseOptions pairBelow = ModernOptions();
        pairBelow.Limits.MaximumNameValueOctets = 2;
        CookieParseOptions pairExact = ModernOptions();
        pairExact.Limits.MaximumNameValueOctets = 3;
        CookieParseOptions pairAbove = ModernOptions();
        pairAbove.Limits.MaximumNameValueOctets = 4;
        CookieParseOptions attributeBelow = ModernOptions();
        attributeBelow.Limits.MaximumAttributeValueOctets = 2;
        CookieParseOptions attributeExact = ModernOptions();
        attributeExact.Limits.MaximumAttributeValueOctets = 3;
        CookieParseOptions attributeAbove = ModernOptions();
        attributeAbove.Limits.MaximumAttributeValueOctets = 4;

        Assert.That(Parse("id=1", inputBelow).ErrorCode,
            Is.EqualTo(CookieErrorCode.InputLimitExceeded));
        Assert.That(Parse("id=1", inputExact).Success, Is.True);
        Assert.That(Parse("id=1", inputAbove).Success, Is.True);
        Assert.That(Parse("id=1", pairBelow).ErrorCode,
            Is.EqualTo(CookieErrorCode.NameValueLimitExceeded));
        Assert.That(Parse("id=1", pairExact).Success, Is.True);
        Assert.That(Parse("id=1", pairAbove).Success, Is.True);
        Assert.That(Parse("id=1; X=abc", attributeBelow).Cookie!.Extensions,
            Is.Empty);
        Assert.That(Parse("id=1; X=abc", attributeExact).Cookie!.Extensions,
            Has.Length.EqualTo(1));
        Assert.That(Parse("id=1; X=abc", attributeAbove).Cookie!.Extensions,
            Has.Length.EqualTo(1));
    }

    [Test]
    public void ModernControlBoundariesDistinguishWhitespaceInjectionAndCtlFailure()
    {
        Assert.That(ParseModern("id=a\0b").ErrorCode,
            Is.EqualTo(CookieErrorCode.HeaderInjection));
        Assert.That(ParseModern("id=a\u0001b").ErrorCode,
            Is.EqualTo(CookieErrorCode.MalformedSyntax));
        Assert.That(ParseModern("id=a\u0008b").ErrorCode,
            Is.EqualTo(CookieErrorCode.MalformedSyntax));
        Assert.That(ParseModern("\tid\t=\tvalue\t").Success, Is.True);
        Assert.That(ParseModern("id=a\nb").ErrorCode,
            Is.EqualTo(CookieErrorCode.HeaderInjection));
        Assert.That(ParseModern("id=a\u000bb").ErrorCode,
            Is.EqualTo(CookieErrorCode.MalformedSyntax));
        Assert.That(ParseModern("id=a\rb").ErrorCode,
            Is.EqualTo(CookieErrorCode.HeaderInjection));
        Assert.That(ParseModern("id=a\u001fb").ErrorCode,
            Is.EqualTo(CookieErrorCode.MalformedSyntax));
        Assert.That(ParseModern("id=a b").Success, Is.True);
        Assert.That(ParseModern("id=a~b").Success, Is.True);
        Assert.That(ParseModern("id=a\u007fb").ErrorCode,
            Is.EqualTo(CookieErrorCode.MalformedSyntax));
        Assert.That(ParseModern("id=café").Success, Is.True);
    }

    [Test]
    public void DiagnosticsHaveStableOrderSeverityOffsetsAndSafeFragments()
    {
        CookieParseOptions options = ModernOptions();
        options.Limits.MaximumAttributeValueOctets = 4;
        options.Limits.MaximumLifetime = TimeSpan.FromSeconds(10);

        SetCookieParseResult result = Parse(
            "id=1;; Unknown=x; Path=bad; Path=/ok; X=12345; Max-Age=20",
            options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Issues.Select(issue => issue.ErrorCode), Is.EqualTo(
            new[]
            {
                CookieErrorCode.IgnoredSegment,
                CookieErrorCode.UnknownAttribute,
                CookieErrorCode.InvalidAttribute,
                CookieErrorCode.DuplicateAttribute,
                CookieErrorCode.AttributeLimitExceeded,
                CookieErrorCode.LifetimeLimitExceeded
            }));
        Assert.That(result.Issues.Select(issue => issue.Severity), Is.EqualTo(
            new[]
            {
                CookieParseSeverity.Information,
                CookieParseSeverity.Information,
                CookieParseSeverity.Warning,
                CookieParseSeverity.Warning,
                CookieParseSeverity.Warning,
                CookieParseSeverity.Warning
            }));
        Assert.That(result.Issues.All(issue => issue.HasOffset), Is.True);
        Assert.That(result.Issues.Select(issue => issue.SafeFragment), Is.EqualTo(
            new[] { "", "Unknown", "Path", "Path", "12345", "20" }));
    }

    [TestCase(CookieComplianceMode.Rfc6265, "id=value", "id", "value")]
    [TestCase(CookieComplianceMode.Rfc6265Bis, "payload", "", "payload")]
    [TestCase(CookieComplianceMode.Compatible, "id=\"quoted value\"", "id", "quoted value")]
    public void PublicThrowingAndTryMethodsAgreeForEveryProfile(
        CookieComplianceMode mode,
        string text,
        string expectedName,
        string expectedValue)
    {
        ICookieParser parser = new CookieParser();
        CookieParseOptions options = new CookieParseOptions
        {
            ComplianceMode = mode
        };

        SetCookieParseResult attempted = parser.TryParseSetCookie(text, options);
        SetCookie parsed = parser.ParseSetCookie(text, options);

        Assert.That(attempted.Success, Is.True);
        Assert.That(attempted.Cookie!.Name, Is.EqualTo(expectedName));
        Assert.That(attempted.Cookie.Value, Is.EqualTo(expectedValue));
        Assert.That(parsed.Name, Is.EqualTo(expectedName));
        Assert.That(parsed.Value, Is.EqualTo(expectedValue));
    }

    [Test]
    public void PublicThrowingMethodReturnsWarningsButThrowsForFatalResults()
    {
        ICookieParser parser = new CookieParser();
        CookieParseOptions options = ModernOptions();
        options.Limits.MaximumNameValueOctets = 2;

        SetCookie warning = parser.ParseSetCookie(
            "id=1; Path=relative",
            ModernOptions());
        SetCookieParseResult attempted = parser.TryParseSetCookie("id=1", options);
        CookieParseException failure = Assert.Throws<CookieParseException>(() =>
            parser.ParseSetCookie("id=1", options));

        Assert.That(warning.Name, Is.EqualTo("id"));
        Assert.That(warning.HasPath, Is.False);
        Assert.That(attempted.ErrorCode,
            Is.EqualTo(CookieErrorCode.NameValueLimitExceeded));
        Assert.That(failure.Message,
            Is.EqualTo("Cookie parsing failed with error code 29."));
    }

    static SetCookieParseResult ParseModern(string text)
    {
        return Parse(text, ModernOptions());
    }

    static SetCookieParseResult Parse(
        string text,
        CookieParseOptions options)
    {
        return SetCookieParserCore.Parse(text.AsSpan(), options);
    }

    static CookieParseOptions ModernOptions()
    {
        return new CookieParseOptions
        {
            ComplianceMode = CookieComplianceMode.Rfc6265Bis
        };
    }
}
