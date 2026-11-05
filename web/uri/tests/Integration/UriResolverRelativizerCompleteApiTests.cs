namespace uri.Tests;

public class UriResolverRelativizerCompleteApiTests
{
    [TestCase("relative/base", "g", UriDiagnosticCode.UnresolvableReference, UriComponent.Scheme)]
    [TestCase("http://a/b#fragment", "g", UriDiagnosticCode.UnresolvableReference, UriComponent.Fragment)]
    [TestCase("http://[bad]/", "g", UriDiagnosticCode.InvalidIpv6Address, UriComponent.Host)]
    [TestCase("http://a/b", "x:%GG", UriDiagnosticCode.InvalidPercentEncoding, UriComponent.Path)]
    public void ResolverReportsEveryBaseAndReferenceFailureThroughBothPaths(
        string baseUri,
        string reference,
        UriDiagnosticCode code,
        UriComponent component)
    {
        AssertResolverFailure(
            baseUri,
            reference,
            new UriResolutionOptions(),
            code,
            component);
    }

    [TestCase("relative/base", "http://a/g", UriDiagnosticCode.UnresolvableReference, UriComponent.Scheme)]
    [TestCase("http://a/b#fragment", "http://a/g", UriDiagnosticCode.UnresolvableReference, UriComponent.Fragment)]
    [TestCase("http://[bad]/", "http://a/g", UriDiagnosticCode.InvalidIpv6Address, UriComponent.Host)]
    [TestCase("http://a/b", "relative/target", UriDiagnosticCode.UnresolvableReference, UriComponent.Scheme)]
    [TestCase("http://a/b", "http://a/%GG", UriDiagnosticCode.InvalidPercentEncoding, UriComponent.Path)]
    public void RelativizerReportsEveryBaseAndTargetFailureThroughBothPaths(
        string baseUri,
        string targetUri,
        UriDiagnosticCode code,
        UriComponent component)
    {
        AssertRelativizerFailure(
            baseUri,
            targetUri,
            new UriRelativizationOptions(),
            code,
            component);
    }

    [Test]
    public void MissingResolverArgumentsAndInvalidOptionsAreConsistent()
    {
        UriResolutionOptions missingLimits = new UriResolutionOptions();
        missingLimits.Limits = null!;
        UriResolutionOptions invalidMode = new UriResolutionOptions();
        invalidMode.Mode = (UriResolutionMode)99;

        AssertResolverFailure(
            null!,
            "g",
            new UriResolutionOptions(),
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertResolverFailure(
            "http://a/b",
            null!,
            new UriResolutionOptions(),
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertResolverFailure(
            "http://a/b",
            "g",
            null!,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertResolverFailure(
            "http://a/b",
            "g",
            missingLimits,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertResolverFailure(
            "http://a/b",
            "g",
            invalidMode,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
    }

    [Test]
    public void MissingRelativizerArgumentsAndInvalidOptionsAreConsistent()
    {
        UriRelativizationOptions missingLimits = new UriRelativizationOptions();
        missingLimits.Limits = null!;
        UriRelativizationOptions invalidMode = new UriRelativizationOptions();
        invalidMode.Mode = (UriRelativizationMode)99;
        UriRelativizationOptions invalidLevel = new UriRelativizationOptions();
        invalidLevel.ComparisonLevel = (UriComparisonLevel)99;
        UriRelativizationOptions invalidProfile = new UriRelativizationOptions();
        invalidProfile.ComparisonLevel = UriComparisonLevel.SchemeProfile;
        invalidProfile.Profile = new UriSchemeProfile();

        AssertRelativizerFailure(
            null!,
            "http://a/g",
            new UriRelativizationOptions(),
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertRelativizerFailure(
            "http://a/b",
            null!,
            new UriRelativizationOptions(),
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertRelativizerFailure(
            "http://a/b",
            "http://a/g",
            null!,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertRelativizerFailure(
            "http://a/b",
            "http://a/g",
            missingLimits,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertRelativizerFailure(
            "http://a/b",
            "http://a/g",
            invalidMode,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertRelativizerFailure(
            "http://a/b",
            "http://a/g",
            invalidLevel,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
        AssertRelativizerFailure(
            "http://a/b",
            "http://a/g",
            invalidProfile,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input);
    }

    [Test]
    public void ResolverAppliesEveryRelevantPublicLimit()
    {
        UriResolutionOptions baseInput = new UriResolutionOptions();
        baseInput.Limits.MaxInputCharacters = 2;
        UriResolutionOptions referenceInput = new UriResolutionOptions();
        referenceInput.Limits.MaxInputCharacters = 2;
        UriResolutionOptions component = new UriResolutionOptions();
        component.Limits.MaxComponentCharacters = 1;
        UriResolutionOptions segments = new UriResolutionOptions();
        segments.Limits.MaxPathSegments = 1;
        UriResolutionOptions output = new UriResolutionOptions();
        output.Limits.MaxOutputCharacters = 4;

        AssertResolverFailure("x:abc", "a", baseInput, UriDiagnosticCode.LimitExceeded, UriComponent.Input);
        AssertResolverFailure("x:", "abc", referenceInput, UriDiagnosticCode.LimitExceeded, UriComponent.Input);
        AssertResolverFailure("x:a", "bb", component, UriDiagnosticCode.LimitExceeded, UriComponent.Path);
        AssertResolverFailure("x:/a", "b/c", segments, UriDiagnosticCode.LimitExceeded, UriComponent.Path);
        AssertResolverFailure("x:/a", "bbbb", output, UriDiagnosticCode.LimitExceeded, UriComponent.Output);
    }

    [Test]
    public void RelativizerAppliesEveryRelevantPublicLimit()
    {
        UriRelativizationOptions baseInput = new UriRelativizationOptions();
        baseInput.Limits.MaxInputCharacters = 2;
        UriRelativizationOptions targetInput = new UriRelativizationOptions();
        targetInput.Limits.MaxInputCharacters = 2;
        UriRelativizationOptions component = new UriRelativizationOptions();
        component.Limits.MaxComponentCharacters = 1;
        UriRelativizationOptions segments = new UriRelativizationOptions();
        segments.Limits.MaxPathSegments = 1;
        UriRelativizationOptions output = new UriRelativizationOptions();
        output.Limits.MaxOutputCharacters = 2;

        AssertRelativizerFailure("x:abc", "x:a", baseInput, UriDiagnosticCode.LimitExceeded, UriComponent.Input);
        AssertRelativizerFailure("x:", "x:abc", targetInput, UriDiagnosticCode.LimitExceeded, UriComponent.Input);
        AssertRelativizerFailure("x:a", "x:bb", component, UriDiagnosticCode.LimitExceeded, UriComponent.Path);
        AssertRelativizerFailure("x:/a", "x:/b/c", segments, UriDiagnosticCode.LimitExceeded, UriComponent.Path);
        AssertRelativizerFailure("http://a/base", "https://b/target", output, UriDiagnosticCode.LimitExceeded, UriComponent.Output);
    }

    [TestCase(UriRelativizationMode.RelativePath, "../../../x")]
    [TestCase(UriRelativizationMode.Shortest, "/x")]
    [TestCase(UriRelativizationMode.AbsolutePath, "/x")]
    [TestCase(UriRelativizationMode.NetworkPath, "//example.com/x")]
    [TestCase(UriRelativizationMode.AbsoluteUri, "http://example.com/x")]
    public void EveryRelativizationModeIsAvailableThroughThePublicApi(
        UriRelativizationMode mode,
        string expected)
    {
        IUriRelativizer relativizer = new UriRelativizer();
        UriRelativizationOptions options = new UriRelativizationOptions();
        options.Mode = mode;

        UriTextResult result = relativizer.TryRelativizeReference(
            "http://example.com/a/very/deep/base",
            "http://example.com/x",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
            Assert.That(result.Text, Is.EqualTo(expected));
            Assert.That(result.Length, Is.EqualTo(expected.Length));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void RelativizerConsumesProfilesOnlyForProfileComparison()
    {
        IUriRelativizer relativizer = new UriRelativizer();
        UriSchemeProfile invalidProfile = new UriSchemeProfile();
        UriRelativizationOptions exactOptions = new UriRelativizationOptions();
        exactOptions.Profile = invalidProfile;
        UriRelativizationOptions syntaxOptions = new UriRelativizationOptions();
        syntaxOptions.ComparisonLevel = UriComparisonLevel.Syntax;
        syntaxOptions.Profile = invalidProfile;
        UriSchemeProfile validProfile = new UriSchemeProfile();
        validProfile.Scheme = "x";
        validProfile.HasDefaultPort = true;
        validProfile.DefaultPort = 9;
        UriRelativizationOptions profileOptions = new UriRelativizationOptions();
        profileOptions.ComparisonLevel = UriComparisonLevel.SchemeProfile;
        profileOptions.Profile = validProfile;

        string exact = relativizer.RelativizeReference(
            "x://a/path",
            "x://a/path",
            exactOptions);
        string syntax = relativizer.RelativizeReference(
            "X://A/path",
            "x://a/path",
            syntaxOptions);
        string profile = relativizer.RelativizeReference(
            "x://a/path",
            "x://a:9/path",
            profileOptions);

        Assert.Multiple(() =>
        {
            Assert.That(exact, Is.Empty);
            Assert.That(syntax, Is.Empty);
            Assert.That(profile, Is.Empty);
        });
    }

    [Test]
    public void UnverifiedExactRelativizationReportsTheDifferingComponent()
    {
        IUriRelativizer relativizer = new UriRelativizer();

        UriTextResult result = relativizer.TryRelativizeReference(
            "http://a/a/b",
            "http://a/a/./c",
            new UriRelativizationOptions());

        AssertFailure(
            result,
            UriDiagnosticCode.UnresolvableReference,
            UriComponent.Path);
    }

    [Test]
    public void PublicResolutionExceptionRetainsDiagnosticIdentityAndMessage()
    {
        UriDiagnostic diagnostic = new UriDiagnostic(
            DiagnosticDomain.Uri,
            UriDiagnosticCode.UnresolvableReference,
            DiagnosticSeverity.Error,
            UriComponent.Path,
            new TextRange(4, 2),
            "cannot resolve");

        UriResolutionException exception = new UriResolutionException(diagnostic);

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic, Is.SameAs(diagnostic));
            Assert.That(exception.Message, Is.EqualTo(diagnostic.Message));
        });
    }

    [Test]
    public void PublicOptionEnumsExposeEverySupportedValueInDeclarationOrder()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Enum.GetValues<UriResolutionMode>(), Is.EqualTo(new UriResolutionMode[]
            {
                UriResolutionMode.Strict,
                UriResolutionMode.Compatible
            }));
            Assert.That(Enum.GetValues<UriRelativizationMode>(), Is.EqualTo(new UriRelativizationMode[]
            {
                UriRelativizationMode.RelativePath,
                UriRelativizationMode.Shortest,
                UriRelativizationMode.AbsolutePath,
                UriRelativizationMode.NetworkPath,
                UriRelativizationMode.AbsoluteUri
            }));
        });
    }

    private static void AssertResolverFailure(
        string baseUri,
        string reference,
        UriResolutionOptions options,
        UriDiagnosticCode code,
        UriComponent component)
    {
        IUriResolver resolver = new UriResolver();
        UriTextResult result = resolver.TryResolveReference(
            baseUri,
            reference,
            options);
        UriResolutionException exception = Assert.Throws<UriResolutionException>(() =>
            resolver.ResolveReference(baseUri, reference, options))!;

        AssertFailure(result, code, component);
        AssertMatchingException(result, exception);
    }

    private static void AssertRelativizerFailure(
        string baseUri,
        string targetUri,
        UriRelativizationOptions options,
        UriDiagnosticCode code,
        UriComponent component)
    {
        IUriRelativizer relativizer = new UriRelativizer();
        UriTextResult result = relativizer.TryRelativizeReference(
            baseUri,
            targetUri,
            options);
        UriResolutionException exception = Assert.Throws<UriResolutionException>(() =>
            relativizer.RelativizeReference(baseUri, targetUri, options))!;

        AssertFailure(result, code, component);
        AssertMatchingException(result, exception);
    }

    private static void AssertFailure(
        UriTextResult result,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Length, Is.Zero);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Uri));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
            Assert.That(result.Diagnostic.Range.Offset, Is.GreaterThanOrEqualTo(0));
            Assert.That(result.Diagnostic.Range.Length, Is.GreaterThanOrEqualTo(0));
            Assert.That(result.Diagnostic.Message, Is.Not.Empty);
        });
    }

    private static void AssertMatchingException(
        UriTextResult result,
        UriResolutionException exception)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Domain, Is.EqualTo(result.Diagnostic!.Domain));
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(result.Diagnostic.Code));
            Assert.That(exception.Diagnostic.Severity, Is.EqualTo(result.Diagnostic.Severity));
            Assert.That(exception.Diagnostic.Component, Is.EqualTo(result.Diagnostic.Component));
            Assert.That(exception.Diagnostic.Range.Offset, Is.EqualTo(result.Diagnostic.Range.Offset));
            Assert.That(exception.Diagnostic.Range.Length, Is.EqualTo(result.Diagnostic.Range.Length));
            Assert.That(exception.Diagnostic.Message, Is.EqualTo(result.Diagnostic.Message));
            Assert.That(exception.Message, Is.EqualTo(result.Diagnostic.Message));
        });
    }
}
