namespace uri.Tests;

public class UriNormalizerComparerPublicApiTests
{
    [Test]
    public void NormalizerImplementsPublicContractAndAppliesSyntaxNormalization()
    {
        IUriNormalizer normalizer = new UriNormalizer();

        string result = normalizer.NormalizeSyntax(
            "HTTP://User%7e@EXAMPLE%2eCOM:080/a/%7e/../b?Q=%7e%2f#F=%7e%2f",
            new UriNormalizationOptions());

        Assert.That(
            result,
            Is.EqualTo("http://User~@example.com:080/a/b?Q=~%2F#F=~%2F"));
    }

    [Test]
    public void AnalysisReportsEveryIndividualSyntaxTransformationInOrder()
    {
        IUriNormalizer normalizer = new UriNormalizer();

        UriNormalizationChange[] changes = normalizer.AnalyzeNormalization(
            "HTTP://User%7e@%45XAMPLE.%43OM/a/%7e/../b?Q=%7e#F=%7e",
            new UriNormalizationOptions());

        Assert.That(changes, Is.EqualTo(new UriNormalizationChange[]
        {
            UriNormalizationChange.SchemeCase,
            UriNormalizationChange.UserInfoPercentEncoding,
            UriNormalizationChange.HostPercentEncoding,
            UriNormalizationChange.HostCase,
            UriNormalizationChange.PathPercentEncoding,
            UriNormalizationChange.PathDotSegments,
            UriNormalizationChange.QueryPercentEncoding,
            UriNormalizationChange.FragmentPercentEncoding
        }));
    }

    [TestCase("http://example.com/path?query#fragment")]
    [TestCase("x://[2001:db8::1]/")]
    [TestCase("?#")]
    public void AnalysisReturnsNoChangesForCanonicalReferences(string text)
    {
        IUriNormalizer normalizer = new UriNormalizer();

        UriNormalizationChange[] changes = normalizer.AnalyzeNormalization(
            text,
            new UriNormalizationOptions());

        Assert.That(changes, Is.Empty);
    }

    [TestCase("HTTP://EXAMPLE.COM:080?", "http://example.com/?")]
    [TestCase("HTTPS://EXAMPLE.COM:443", "https://example.com/")]
    [TestCase("WS://EXAMPLE.COM:80", "ws://example.com/")]
    [TestCase("WSS://EXAMPLE.COM:443", "wss://example.com/")]
    [TestCase("FTP://EXAMPLE.COM:21", "ftp://example.com/")]
    [TestCase("X://EXAMPLE.COM:80", "x://example.com:80")]
    public void ProfileNormalizationUsesBuiltInsWhenNoExplicitProfileIsSet(
        string input,
        string expected)
    {
        IUriNormalizer normalizer = new UriNormalizer();

        string result = normalizer.NormalizeWithProfile(
            input,
            new UriNormalizationOptions());

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void ProfileNormalizationUsesTheExplicitOptionsProfile()
    {
        UriSchemeProfile profile = new UriSchemeProfile();
        profile.Scheme = "x";
        profile.NormalizeEmptyAuthorityPath = true;
        profile.RemoveEmptyPort = true;
        profile.RemoveEmptyQuery = true;
        profile.RemoveEmptyFragment = true;
        UriNormalizationOptions options = new UriNormalizationOptions();
        options.Profile = profile;
        IUriNormalizer normalizer = new UriNormalizer();

        string result = normalizer.NormalizeWithProfile("X://EXAMPLE.COM:?#", options);

        Assert.That(result, Is.EqualTo("x://example.com/"));
    }

    [TestCase("x:path%GG", UriDiagnosticCode.InvalidPercentEncoding, UriComponent.Path)]
    [TestCase("x://[not-ip]/", UriDiagnosticCode.InvalidIpv6Address, UriComponent.Host)]
    public void NormalizerThrowsDiagnosticExceptionForInvalidText(
        string text,
        UriDiagnosticCode expectedCode,
        UriComponent expectedComponent)
    {
        IUriNormalizer normalizer = new UriNormalizer();

        UriNormalizationException exception = Assert.Throws<UriNormalizationException>(() =>
            normalizer.NormalizeSyntax(text, new UriNormalizationOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(expectedCode));
            Assert.That(exception.Diagnostic.Component, Is.EqualTo(expectedComponent));
            Assert.That(exception.Message, Is.EqualTo(exception.Diagnostic.Message));
        });
    }

    [Test]
    public void NormalizerRejectsMissingOptionsLimitsAndText()
    {
        IUriNormalizer normalizer = new UriNormalizer();
        UriNormalizationOptions missingLimits = new UriNormalizationOptions();
        missingLimits.Limits = null!;

        Assert.Multiple(() =>
        {
            AssertInvalidNormalization(() => normalizer.NormalizeSyntax(null!, new UriNormalizationOptions()));
            AssertInvalidNormalization(() => normalizer.NormalizeSyntax("x:a", null!));
            AssertInvalidNormalization(() => normalizer.AnalyzeNormalization("x:a", missingLimits));
        });
    }

    [TestCase("", 80)]
    [TestCase("bad scheme", 80)]
    [TestCase("http", -1)]
    [TestCase("http", 65536)]
    public void ProfileNormalizationRejectsInvalidExplicitProfiles(
        string scheme,
        int defaultPort)
    {
        UriSchemeProfile profile = new UriSchemeProfile();
        profile.Scheme = scheme;
        profile.HasDefaultPort = true;
        profile.DefaultPort = defaultPort;
        UriNormalizationOptions options = new UriNormalizationOptions();
        options.Profile = profile;
        IUriNormalizer normalizer = new UriNormalizer();

        AssertInvalidNormalization(() =>
            normalizer.NormalizeWithProfile("http://example.com", options));
    }

    [Test]
    public void NormalizerAppliesInputComponentAndOutputLimits()
    {
        IUriNormalizer normalizer = new UriNormalizer();
        UriNormalizationOptions input = new UriNormalizationOptions();
        input.Limits.MaxInputCharacters = 2;
        UriNormalizationOptions component = new UriNormalizationOptions();
        component.Limits.MaxComponentCharacters = 1;
        UriNormalizationOptions output = new UriNormalizationOptions();
        output.Limits.MaxOutputCharacters = 5;

        UriNormalizationException inputFailure = Assert.Throws<UriNormalizationException>(() =>
            normalizer.NormalizeSyntax("x:abc", input))!;
        UriNormalizationException componentFailure = Assert.Throws<UriNormalizationException>(() =>
            normalizer.NormalizeSyntax("x:abc", component))!;
        UriNormalizationException outputFailure = Assert.Throws<UriNormalizationException>(() =>
            normalizer.NormalizeSyntax("HTTP://EXAMPLE.COM", output))!;

        Assert.Multiple(() =>
        {
            Assert.That(inputFailure.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.LimitExceeded));
            Assert.That(inputFailure.Diagnostic.Component, Is.EqualTo(UriComponent.Input));
            Assert.That(componentFailure.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.LimitExceeded));
            Assert.That(componentFailure.Diagnostic.Component, Is.EqualTo(UriComponent.Path));
            Assert.That(outputFailure.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.LimitExceeded));
            Assert.That(outputFailure.Diagnostic.Component, Is.EqualTo(UriComponent.Output));
        });
    }

    [Test]
    public void ComparerImplementsPublicContractAndReportsExactDifference()
    {
        IUriComparer comparer = new UriComparer();

        UriComparisonResult result = comparer.CompareExact(
            "x://host/path?left",
            "x://host/path?right",
            new UriComparisonOptions());

        AssertSuccessfulDifference(
            result,
            UriComparisonLevel.Exact,
            UriComponent.Query);
    }

    [Test]
    public void PublicSyntaxComparisonRecognizesGenericEquivalence()
    {
        IUriComparer comparer = new UriComparer();

        UriComparisonResult result = comparer.CompareSyntax(
            "HTTP://EXAMPLE.COM/a/%7e/../b%2f?x=%41",
            "http://example.com/a/b%2F?x=A",
            new UriComparisonOptions());

        AssertSuccessfulEquality(result, UriComparisonLevel.Syntax);
    }

    [TestCase("http://host:80", "http://host/")]
    [TestCase("https://host:443", "https://host/")]
    [TestCase("ws://host:80", "ws://host/")]
    [TestCase("wss://host:443", "wss://host/")]
    [TestCase("ftp://host:21", "ftp://host/")]
    public void PublicProfileComparisonUsesBuiltInProfiles(
        string left,
        string right)
    {
        IUriComparer comparer = new UriComparer();

        UriComparisonResult result = comparer.CompareWithProfile(
            left,
            right,
            new UriComparisonOptions());

        AssertSuccessfulEquality(result, UriComparisonLevel.SchemeProfile);
    }

    [Test]
    public void PublicProfileComparisonUsesExplicitProfileOptions()
    {
        UriSchemeProfile profile = new UriSchemeProfile();
        profile.Scheme = "x";
        profile.RemoveEmptyQuery = true;
        profile.RemoveEmptyFragment = true;
        UriComparisonOptions options = new UriComparisonOptions();
        options.Profile = profile;
        IUriComparer comparer = new UriComparer();

        UriComparisonResult result = comparer.CompareWithProfile("x:?#", "x:", options);

        AssertSuccessfulEquality(result, UriComparisonLevel.SchemeProfile);
    }

    [Test]
    public void ComparisonFailuresReturnDiagnosticsWithoutThrowing()
    {
        IUriComparer comparer = new UriComparer();
        UriComparisonResult invalidLeft = comparer.CompareSyntax(
            "x:%GG",
            "x:a",
            new UriComparisonOptions());
        UriComparisonResult invalidRight = comparer.CompareExact(
            "x:a",
            "x:%GG",
            new UriComparisonOptions());
        UriComparisonResult missing = comparer.CompareExact(
            null!,
            "x:a",
            new UriComparisonOptions());

        Assert.Multiple(() =>
        {
            AssertComparisonFailure(invalidLeft, UriComparisonLevel.Syntax, UriDiagnosticCode.InvalidPercentEncoding);
            AssertComparisonFailure(invalidRight, UriComparisonLevel.Exact, UriDiagnosticCode.InvalidPercentEncoding);
            AssertComparisonFailure(missing, UriComparisonLevel.Exact, UriDiagnosticCode.InvalidInput);
        });
    }

    [Test]
    public void ComparisonRejectsInvalidOptionsAndProfilesAsResults()
    {
        IUriComparer comparer = new UriComparer();
        UriComparisonOptions missingLimits = new UriComparisonOptions();
        missingLimits.Limits = null!;
        UriSchemeProfile invalidProfile = new UriSchemeProfile();
        invalidProfile.Scheme = "";
        UriComparisonOptions profileOptions = new UriComparisonOptions();
        profileOptions.Profile = invalidProfile;

        UriComparisonResult missingOptions = comparer.CompareExact("x:a", "x:a", null!);
        UriComparisonResult invalidLimits = comparer.CompareSyntax("x:a", "x:a", missingLimits);
        UriComparisonResult invalidProfileResult = comparer.CompareWithProfile(
            "x:a",
            "x:a",
            profileOptions);

        Assert.Multiple(() =>
        {
            AssertComparisonFailure(missingOptions, UriComparisonLevel.Exact, UriDiagnosticCode.InvalidInput);
            AssertComparisonFailure(invalidLimits, UriComparisonLevel.Syntax, UriDiagnosticCode.InvalidInput);
            AssertComparisonFailure(invalidProfileResult, UriComparisonLevel.SchemeProfile, UriDiagnosticCode.InvalidInput);
        });
    }

    [Test]
    public void SyntaxComparisonAppliesInputComponentAndNormalizedOutputLimits()
    {
        IUriComparer comparer = new UriComparer();
        UriComparisonOptions input = new UriComparisonOptions();
        input.Limits.MaxInputCharacters = 2;
        UriComparisonOptions component = new UriComparisonOptions();
        component.Limits.MaxComponentCharacters = 1;
        UriComparisonOptions output = new UriComparisonOptions();
        output.Limits.MaxOutputCharacters = 5;

        UriComparisonResult inputFailure = comparer.CompareSyntax("x:abc", "x:abc", input);
        UriComparisonResult componentFailure = comparer.CompareSyntax("x:abc", "x:abc", component);
        UriComparisonResult outputFailure = comparer.CompareSyntax(
            "HTTP://EXAMPLE.COM",
            "http://example.com",
            output);

        Assert.Multiple(() =>
        {
            Assert.That(inputFailure.Diagnostic!.Component, Is.EqualTo(UriComponent.Input));
            Assert.That(componentFailure.Diagnostic!.Component, Is.EqualTo(UriComponent.Path));
            Assert.That(outputFailure.Diagnostic!.Component, Is.EqualTo(UriComponent.Output));
        });
    }

    [Test]
    public void ComparisonOptionsAndResultDataRetainConfiguredValues()
    {
        UriComparisonOptions options = new UriComparisonOptions();
        UriSchemeProfile profile = new UriSchemeProfile();
        options.Profile = profile;
        UriDiagnostic diagnostic = new UriDiagnostic(
            DiagnosticDomain.Uri,
            UriDiagnosticCode.InvalidInput,
            DiagnosticSeverity.Error,
            UriComponent.Input,
            new TextRange(1, 2),
            "failure");
        UriComparisonResult result = new UriComparisonResult(
            false,
            false,
            UriComparisonLevel.SchemeProfile,
            UriComponent.Host,
            diagnostic);

        Assert.Multiple(() =>
        {
            Assert.That(options.Limits, Is.Not.Null);
            Assert.That(options.Profile, Is.SameAs(profile));
            Assert.That(result.Success, Is.False);
            Assert.That(result.Equal, Is.False);
            Assert.That(result.Level, Is.EqualTo(UriComparisonLevel.SchemeProfile));
            Assert.That(result.DifferentComponent, Is.EqualTo(UriComponent.Host));
            Assert.That(result.Diagnostic, Is.SameAs(diagnostic));
        });
    }

    private static void AssertInvalidNormalization(TestDelegate operation)
    {
        UriNormalizationException exception = Assert.Throws<UriNormalizationException>(operation)!;
        Assert.That(exception.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidInput));
    }

    private static void AssertSuccessfulEquality(
        UriComparisonResult result,
        UriComparisonLevel level)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Equal, Is.True);
        Assert.That(result.Level, Is.EqualTo(level));
        Assert.That(result.DifferentComponent, Is.EqualTo(UriComponent.None));
        Assert.That(result.Diagnostic, Is.Null);
    }

    private static void AssertSuccessfulDifference(
        UriComparisonResult result,
        UriComparisonLevel level,
        UriComponent component)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Equal, Is.False);
        Assert.That(result.Level, Is.EqualTo(level));
        Assert.That(result.DifferentComponent, Is.EqualTo(component));
        Assert.That(result.Diagnostic, Is.Null);
    }

    private static void AssertComparisonFailure(
        UriComparisonResult result,
        UriComparisonLevel level,
        UriDiagnosticCode code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Equal, Is.False);
        Assert.That(result.Level, Is.EqualTo(level));
        Assert.That(result.DifferentComponent, Is.EqualTo(UriComponent.None));
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Code, Is.EqualTo(code));
    }
}
