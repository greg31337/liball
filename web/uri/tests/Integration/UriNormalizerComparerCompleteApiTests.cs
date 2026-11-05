namespace uri.Tests;

public class UriNormalizerComparerCompleteApiTests
{
    [Test]
    public void PublicAnalysisCanReportEveryNormalizationChange()
    {
        IUriNormalizer normalizer = new UriNormalizer();
        UriNormalizationChange[] syntax = normalizer.AnalyzeNormalization(
            "HTTP://User%7e@%45XAMPLE.%43OM/a/%7e/../b?Q=%7e#F=%7e",
            new UriNormalizationOptions());
        UriNormalizationChange[] address = normalizer.AnalyzeNormalization(
            "x://[2001:0DB8:0:0:0:0:0:1]/",
            new UriNormalizationOptions());
        UriNormalizationOptions profileOptions = CreateFullHttpOptions();
        UriNormalizationChange[] profile = normalizer.AnalyzeNormalization(
            "HTTP://EXAMPLE.COM:080?#",
            profileOptions);
        UriNormalizationChange[] emptyPort = normalizer.AnalyzeNormalization(
            "http://example.com:",
            profileOptions);
        UriNormalizationChange[] none = normalizer.AnalyzeNormalization(
            "x:a",
            new UriNormalizationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(syntax, Is.EqualTo(new UriNormalizationChange[]
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
            Assert.That(address, Is.EqualTo(new UriNormalizationChange[]
            {
                UriNormalizationChange.HostAddress
            }));
            Assert.That(profile, Is.EqualTo(new UriNormalizationChange[]
            {
                UriNormalizationChange.SchemeCase,
                UriNormalizationChange.HostCase,
                UriNormalizationChange.AuthorityPath,
                UriNormalizationChange.DefaultPort,
                UriNormalizationChange.EmptyQuery,
                UriNormalizationChange.EmptyFragment
            }));
            Assert.That(emptyPort, Is.EqualTo(new UriNormalizationChange[]
            {
                UriNormalizationChange.AuthorityPath,
                UriNormalizationChange.EmptyPort
            }));
            Assert.That(none, Is.Empty);
        });

        HashSet<UriNormalizationChange> observed = new HashSet<UriNormalizationChange>();
        AddChanges(observed, syntax);
        AddChanges(observed, address);
        AddChanges(observed, profile);
        AddChanges(observed, emptyPort);
        observed.Add(UriNormalizationChange.None);
        UriNormalizationChange[] allValues = Enum.GetValues<UriNormalizationChange>();
        Assert.That(observed.Count, Is.EqualTo(allValues.Length));
        foreach (UriNormalizationChange value in allValues)
        {
            Assert.That(observed, Does.Contain(value), value.ToString());
        }
    }

    [Test]
    public void ProfileAnalysisIsExplicitAndPredictsProfileNormalization()
    {
        IUriNormalizer normalizer = new UriNormalizer();
        const string input = "http://example.com:80";
        UriNormalizationChange[] generic = normalizer.AnalyzeNormalization(
            input,
            new UriNormalizationOptions());
        UriNormalizationOptions profileOptions = CreateFullHttpOptions();
        UriNormalizationChange[] profiled = normalizer.AnalyzeNormalization(
            input,
            profileOptions);

        string syntax = normalizer.NormalizeSyntax(
            input,
            profileOptions);
        string withProfile = normalizer.NormalizeWithProfile(
            input,
            profileOptions);

        Assert.Multiple(() =>
        {
            Assert.That(generic, Is.Empty);
            Assert.That(profiled, Is.EqualTo(new UriNormalizationChange[]
            {
                UriNormalizationChange.AuthorityPath,
                UriNormalizationChange.DefaultPort
            }));
            Assert.That(syntax, Is.EqualTo(input));
            Assert.That(withProfile, Is.EqualTo("http://example.com/"));
        });
    }

    [Test]
    public void AllComparisonLevelsApplyTheirDeclaredRules()
    {
        IUriComparer comparer = new UriComparer();
        const string left = "HTTP://EXAMPLE.COM:80";
        const string right = "http://example.com/";

        UriComparisonResult exact = comparer.CompareExact(
            left,
            right,
            new UriComparisonOptions());
        UriComparisonResult syntax = comparer.CompareSyntax(
            left,
            right,
            new UriComparisonOptions());
        UriComparisonResult profile = comparer.CompareWithProfile(
            left,
            right,
            new UriComparisonOptions());

        Assert.Multiple(() =>
        {
            AssertDifference(exact, UriComparisonLevel.Exact, UriComponent.Scheme);
            AssertDifference(syntax, UriComparisonLevel.Syntax, UriComponent.Port);
            AssertEquality(profile, UriComparisonLevel.SchemeProfile);
        });
    }

    [TestCase("x:a", "a", UriComponent.Scheme)]
    [TestCase("//host/a", "/a", UriComponent.Authority)]
    [TestCase("//user@host/", "//host/", UriComponent.UserInfo)]
    [TestCase("//left/", "//right/", UriComponent.Host)]
    [TestCase("//host:80/", "//host:81/", UriComponent.Port)]
    [TestCase("/left", "/right", UriComponent.Path)]
    [TestCase("/path?left", "/path?right", UriComponent.Query)]
    [TestCase("/path#left", "/path#right", UriComponent.Fragment)]
    public void ExactComparisonReportsEveryPublicDifferenceComponent(
        string left,
        string right,
        UriComponent component)
    {
        IUriComparer comparer = new UriComparer();

        UriComparisonResult forward = comparer.CompareExact(
            left,
            right,
            new UriComparisonOptions());
        UriComparisonResult reverse = comparer.CompareExact(
            right,
            left,
            new UriComparisonOptions());

        Assert.Multiple(() =>
        {
            AssertDifference(forward, UriComparisonLevel.Exact, component);
            AssertDifference(reverse, UriComparisonLevel.Exact, component);
        });
    }

    [Test]
    public void InvalidProfilesAffectOnlyOperationsThatConsumeThem()
    {
        UriSchemeProfile invalidProfile = new UriSchemeProfile();
        UriNormalizationOptions normalizationOptions = new UriNormalizationOptions();
        normalizationOptions.Profile = invalidProfile;
        UriComparisonOptions comparisonOptions = new UriComparisonOptions();
        comparisonOptions.Profile = invalidProfile;
        IUriNormalizer normalizer = new UriNormalizer();
        IUriComparer comparer = new UriComparer();

        string syntax = normalizer.NormalizeSyntax("x:a", normalizationOptions);
        UriNormalizationException analysisFailure =
            Assert.Throws<UriNormalizationException>(() =>
                normalizer.AnalyzeNormalization("x:a", normalizationOptions))!;
        UriNormalizationException profileFailure =
            Assert.Throws<UriNormalizationException>(() =>
                normalizer.NormalizeWithProfile("x:a", normalizationOptions))!;
        UriComparisonResult exact = comparer.CompareExact(
            "x:a",
            "x:a",
            comparisonOptions);
        UriComparisonResult comparedSyntax = comparer.CompareSyntax(
            "x:a",
            "x:a",
            comparisonOptions);
        UriComparisonResult comparedProfile = comparer.CompareWithProfile(
            "x:a",
            "x:a",
            comparisonOptions);

        Assert.Multiple(() =>
        {
            Assert.That(syntax, Is.EqualTo("x:a"));
            Assert.That(analysisFailure.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidInput));
            Assert.That(profileFailure.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidInput));
            AssertEquality(exact, UriComparisonLevel.Exact);
            AssertEquality(comparedSyntax, UriComparisonLevel.Syntax);
            AssertFailure(comparedProfile, UriComparisonLevel.SchemeProfile, UriDiagnosticCode.InvalidInput, UriComponent.Input);
        });
    }

    [Test]
    public void EveryNormalizerMethodThrowsADiagnosticExceptionOnFailure()
    {
        IUriNormalizer normalizer = new UriNormalizer();
        UriNormalizationOptions invalidProfile = CreateFullHttpOptions();
        invalidProfile.Profile!.Scheme = "";

        UriNormalizationException analysis =
            Assert.Throws<UriNormalizationException>(() =>
                normalizer.AnalyzeNormalization("x:%GG", new UriNormalizationOptions()))!;
        UriNormalizationException syntax =
            Assert.Throws<UriNormalizationException>(() =>
                normalizer.NormalizeSyntax("x://[bad]/", new UriNormalizationOptions()))!;
        UriNormalizationException profile =
            Assert.Throws<UriNormalizationException>(() =>
                normalizer.NormalizeWithProfile("http://example.com", invalidProfile))!;

        Assert.Multiple(() =>
        {
            AssertNormalizationFailure(
                analysis,
                UriDiagnosticCode.InvalidPercentEncoding,
                UriComponent.Path);
            AssertNormalizationFailure(
                syntax,
                UriDiagnosticCode.InvalidIpv6Address,
                UriComponent.Host);
            AssertNormalizationFailure(
                profile,
                UriDiagnosticCode.InvalidInput,
                UriComponent.Input);
        });
    }

    [Test]
    public void MissingArgumentsFailConsistentlyAcrossNormalizerMethods()
    {
        IUriNormalizer normalizer = new UriNormalizer();
        UriNormalizationOptions missingLimits = new UriNormalizationOptions();
        missingLimits.Limits = null!;

        TestDelegate[] operations = new TestDelegate[]
        {
            () => normalizer.AnalyzeNormalization(null!, new UriNormalizationOptions()),
            () => normalizer.AnalyzeNormalization("x:a", null!),
            () => normalizer.AnalyzeNormalization("x:a", missingLimits),
            () => normalizer.NormalizeSyntax(null!, new UriNormalizationOptions()),
            () => normalizer.NormalizeSyntax("x:a", null!),
            () => normalizer.NormalizeSyntax("x:a", missingLimits),
            () => normalizer.NormalizeWithProfile(null!, new UriNormalizationOptions()),
            () => normalizer.NormalizeWithProfile("x:a", null!),
            () => normalizer.NormalizeWithProfile("x:a", missingLimits)
        };

        foreach (TestDelegate operation in operations)
        {
            UriNormalizationException failure =
                Assert.Throws<UriNormalizationException>(operation)!;
            AssertNormalizationFailure(
                failure,
                UriDiagnosticCode.InvalidInput,
                UriComponent.Input);
        }
    }

    [Test]
    public void NormalizerMethodsApplyRelevantConfiguredLimits()
    {
        IUriNormalizer normalizer = new UriNormalizer();
        UriNormalizationOptions input = new UriNormalizationOptions();
        input.Limits.MaxInputCharacters = 2;
        UriNormalizationOptions component = new UriNormalizationOptions();
        component.Limits.MaxComponentCharacters = 1;
        UriNormalizationOptions segments = new UriNormalizationOptions();
        segments.Limits.MaxPathSegments = 1;
        UriNormalizationOptions output = new UriNormalizationOptions();
        output.Limits.MaxOutputCharacters = 4;

        UriNormalizationException inputFailure =
            Assert.Throws<UriNormalizationException>(() =>
                normalizer.AnalyzeNormalization("x:abc", input))!;
        UriNormalizationException componentFailure =
            Assert.Throws<UriNormalizationException>(() =>
                normalizer.NormalizeSyntax("x:abc", component))!;
        UriNormalizationException segmentFailure =
            Assert.Throws<UriNormalizationException>(() =>
                normalizer.NormalizeSyntax("x:/a/b", segments))!;
        UriNormalizationException outputFailure =
            Assert.Throws<UriNormalizationException>(() =>
                normalizer.NormalizeWithProfile("http://host", output))!;

        Assert.Multiple(() =>
        {
            AssertNormalizationFailure(inputFailure, UriDiagnosticCode.LimitExceeded, UriComponent.Input);
            AssertNormalizationFailure(componentFailure, UriDiagnosticCode.LimitExceeded, UriComponent.Path);
            AssertNormalizationFailure(segmentFailure, UriDiagnosticCode.LimitExceeded, UriComponent.Path);
            AssertNormalizationFailure(outputFailure, UriDiagnosticCode.LimitExceeded, UriComponent.Output);
        });
    }

    [Test]
    public void EveryComparisonLevelReturnsFailuresWithoutThrowing()
    {
        IUriComparer comparer = new UriComparer();

        UriComparisonResult exact = comparer.CompareExact(
            "x:%GG",
            "x:a",
            new UriComparisonOptions());
        UriComparisonResult syntax = comparer.CompareSyntax(
            "x:a",
            "x://[bad]/",
            new UriComparisonOptions());
        UriComparisonResult profile = comparer.CompareWithProfile(
            null!,
            "x:a",
            new UriComparisonOptions());

        Assert.Multiple(() =>
        {
            AssertFailure(exact, UriComparisonLevel.Exact, UriDiagnosticCode.InvalidPercentEncoding, UriComponent.Path);
            AssertFailure(syntax, UriComparisonLevel.Syntax, UriDiagnosticCode.InvalidIpv6Address, UriComponent.Host);
            AssertFailure(profile, UriComparisonLevel.SchemeProfile, UriDiagnosticCode.InvalidInput, UriComponent.Input);
        });
    }

    [Test]
    public void MissingArgumentsFailConsistentlyAcrossComparerMethods()
    {
        IUriComparer comparer = new UriComparer();
        UriComparisonOptions missingLimits = new UriComparisonOptions();
        missingLimits.Limits = null!;
        UriComparisonResult[] results = new UriComparisonResult[]
        {
            comparer.CompareExact(null!, "x:a", new UriComparisonOptions()),
            comparer.CompareExact("x:a", null!, new UriComparisonOptions()),
            comparer.CompareExact("x:a", "x:a", null!),
            comparer.CompareSyntax(null!, "x:a", new UriComparisonOptions()),
            comparer.CompareSyntax("x:a", null!, new UriComparisonOptions()),
            comparer.CompareSyntax("x:a", "x:a", missingLimits),
            comparer.CompareWithProfile(null!, "x:a", new UriComparisonOptions()),
            comparer.CompareWithProfile("x:a", null!, new UriComparisonOptions()),
            comparer.CompareWithProfile("x:a", "x:a", null!)
        };
        UriComparisonLevel[] levels = new UriComparisonLevel[]
        {
            UriComparisonLevel.Exact,
            UriComparisonLevel.Exact,
            UriComparisonLevel.Exact,
            UriComparisonLevel.Syntax,
            UriComparisonLevel.Syntax,
            UriComparisonLevel.Syntax,
            UriComparisonLevel.SchemeProfile,
            UriComparisonLevel.SchemeProfile,
            UriComparisonLevel.SchemeProfile
        };

        for (int index = 0; index < results.Length; index += 1)
        {
            AssertFailure(
                results[index],
                levels[index],
                UriDiagnosticCode.InvalidInput,
                UriComponent.Input);
        }
    }

    [Test]
    public void ComparisonLevelsApplyRelevantConfiguredLimits()
    {
        IUriComparer comparer = new UriComparer();
        UriComparisonOptions input = new UriComparisonOptions();
        input.Limits.MaxInputCharacters = 2;
        UriComparisonOptions component = new UriComparisonOptions();
        component.Limits.MaxComponentCharacters = 1;
        UriComparisonOptions segments = new UriComparisonOptions();
        segments.Limits.MaxPathSegments = 1;
        UriComparisonOptions output = new UriComparisonOptions();
        output.Limits.MaxOutputCharacters = 4;

        UriComparisonResult inputFailure = comparer.CompareExact(
            "x:abc",
            "x:abc",
            input);
        UriComparisonResult componentFailure = comparer.CompareSyntax(
            "x:abc",
            "x:abc",
            component);
        UriComparisonResult segmentFailure = comparer.CompareSyntax(
            "x:/a/b",
            "x:/a/b",
            segments);
        UriComparisonResult outputFailure = comparer.CompareWithProfile(
            "http://host",
            "http://host/",
            output);

        Assert.Multiple(() =>
        {
            AssertFailure(inputFailure, UriComparisonLevel.Exact, UriDiagnosticCode.LimitExceeded, UriComponent.Input);
            AssertFailure(componentFailure, UriComparisonLevel.Syntax, UriDiagnosticCode.LimitExceeded, UriComponent.Path);
            AssertFailure(segmentFailure, UriComparisonLevel.Syntax, UriDiagnosticCode.LimitExceeded, UriComponent.Path);
            AssertFailure(outputFailure, UriComparisonLevel.SchemeProfile, UriDiagnosticCode.LimitExceeded, UriComponent.Output);
        });
    }

    [Test]
    public void OptionAndResultObjectsRetainIndependentConfiguredState()
    {
        UriNormalizationOptions firstNormalization = new UriNormalizationOptions();
        UriNormalizationOptions secondNormalization = new UriNormalizationOptions();
        UriComparisonOptions firstComparison = new UriComparisonOptions();
        UriComparisonOptions secondComparison = new UriComparisonOptions();
        UriSchemeProfile profile = new UriSchemeProfile();
        firstNormalization.Profile = profile;
        firstComparison.Profile = profile;
        firstNormalization.Limits.MaxInputCharacters = 1;
        firstComparison.Limits.MaxOutputCharacters = 2;
        UriDiagnostic diagnostic = new UriDiagnostic(
            DiagnosticDomain.Uri,
            UriDiagnosticCode.InvalidInput,
            DiagnosticSeverity.Error,
            UriComponent.Input,
            new TextRange(3, 1),
            "failure");
        UriComparisonResult result = new UriComparisonResult(
            false,
            true,
            UriComparisonLevel.Syntax,
            UriComponent.Query,
            diagnostic);

        Assert.Multiple(() =>
        {
            Assert.That(firstNormalization.Profile, Is.SameAs(profile));
            Assert.That(firstComparison.Profile, Is.SameAs(profile));
            Assert.That(secondNormalization.Profile, Is.Null);
            Assert.That(secondComparison.Profile, Is.Null);
            Assert.That(firstNormalization.Limits, Is.Not.SameAs(secondNormalization.Limits));
            Assert.That(firstComparison.Limits, Is.Not.SameAs(secondComparison.Limits));
            Assert.That(secondNormalization.Limits.MaxInputCharacters, Is.EqualTo(1048576));
            Assert.That(secondComparison.Limits.MaxOutputCharacters, Is.EqualTo(4194304));
            Assert.That(result.Success, Is.False);
            Assert.That(result.Equal, Is.True);
            Assert.That(result.Level, Is.EqualTo(UriComparisonLevel.Syntax));
            Assert.That(result.DifferentComponent, Is.EqualTo(UriComponent.Query));
            Assert.That(result.Diagnostic, Is.SameAs(diagnostic));
        });
    }

    [Test]
    public void NormalizationExceptionRetainsItsDiagnosticAndMessage()
    {
        UriDiagnostic diagnostic = new UriDiagnostic(
            DiagnosticDomain.Uri,
            UriDiagnosticCode.InvalidPath,
            DiagnosticSeverity.Error,
            UriComponent.Path,
            new TextRange(4, 2),
            "normalization failed");

        UriNormalizationException exception = new UriNormalizationException(diagnostic);

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic, Is.SameAs(diagnostic));
            Assert.That(exception.Message, Is.EqualTo(diagnostic.Message));
        });
    }

    private static UriNormalizationOptions CreateFullHttpOptions()
    {
        UriSchemeProfile profile = new UriSchemeProfile();
        profile.Scheme = "http";
        profile.NormalizeEmptyAuthorityPath = true;
        profile.RemoveEmptyPort = true;
        profile.HasDefaultPort = true;
        profile.DefaultPort = 80;
        profile.RemoveEmptyQuery = true;
        profile.RemoveEmptyFragment = true;
        UriNormalizationOptions options = new UriNormalizationOptions();
        options.Profile = profile;
        return options;
    }

    private static void AddChanges(
        HashSet<UriNormalizationChange> destination,
        UriNormalizationChange[] changes)
    {
        foreach (UriNormalizationChange change in changes)
        {
            destination.Add(change);
        }
    }

    private static void AssertEquality(
        UriComparisonResult result,
        UriComparisonLevel level)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Equal, Is.True);
        Assert.That(result.Level, Is.EqualTo(level));
        Assert.That(result.DifferentComponent, Is.EqualTo(UriComponent.None));
        Assert.That(result.Diagnostic, Is.Null);
    }

    private static void AssertDifference(
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

    private static void AssertFailure(
        UriComparisonResult result,
        UriComparisonLevel level,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Equal, Is.False);
        Assert.That(result.Level, Is.EqualTo(level));
        Assert.That(result.DifferentComponent, Is.EqualTo(UriComponent.None));
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Uri));
        Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
        Assert.That(result.Diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
        Assert.That(result.Diagnostic.Message, Is.Not.Empty);
    }

    private static void AssertNormalizationFailure(
        UriNormalizationException exception,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(exception.Diagnostic.Domain, Is.EqualTo(DiagnosticDomain.Uri));
        Assert.That(exception.Diagnostic.Code, Is.EqualTo(code));
        Assert.That(exception.Diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
        Assert.That(exception.Diagnostic.Component, Is.EqualTo(component));
        Assert.That(exception.Diagnostic.Message, Is.Not.Empty);
        Assert.That(exception.Message, Is.EqualTo(exception.Diagnostic.Message));
    }
}
