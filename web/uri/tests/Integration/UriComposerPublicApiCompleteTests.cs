namespace uri.Tests;

public class UriComposerPublicApiCompleteTests
{
    [TestCase("")]
    [TestCase("?")]
    [TestCase("#")]
    [TestCase("?#")]
    [TestCase("//")]
    [TestCase("//user@host:/a//b/")]
    [TestCase("/a//b/")]
    [TestCase("a/b:c")]
    [TestCase("x:")]
    [TestCase("x:///path?#")]
    [TestCase("HTTP://User%40Name@Example.COM:080/a%2Fb?c%23d#e%3Ff")]
    [TestCase("x://[2001:0DB8::1]:080/")]
    [TestCase("x://[Vf.A:B]:/")]
    public void EveryParsedReferenceFormRoundTripsLosslesslyThroughPublicApis(string text)
    {
        IUriParser parser = new UriParser();
        IUriComposer composer = new UriComposer();
        UriReference reference = parser.ParseReference(text, new UriParseOptions());

        string throwing = composer.ComposeReference(reference, new UriValidationOptions());
        UriTextResult result = composer.TryComposeReference(reference, new UriValidationOptions());
        int measured = composer.MeasureReference(reference, new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(throwing, Is.EqualTo(text), text);
            Assert.That(result.Success, Is.True, text);
            Assert.That(result.Text, Is.EqualTo(text), text);
            Assert.That(result.Length, Is.EqualTo(text.Length), text);
            Assert.That(result.Diagnostic, Is.Null, text);
            Assert.That(measured, Is.EqualTo(text.Length), text);
        });
    }

    [Test]
    public void CallerCreatedValidModelComposesWithoutOriginalText()
    {
        UriReference reference = new UriReference();
        reference.Kind = UriReferenceKind.Uri;
        reference.HasScheme = true;
        reference.Scheme = "x";
        reference.HasAuthority = true;
        reference.Authority.HasUserInfo = true;
        reference.Authority.UserInfo = "user%40name";
        reference.Authority.HostKind = UriHostKind.IpvFuture;
        reference.Authority.Host = "v1.a:b";
        reference.Authority.HasPort = true;
        reference.Authority.Port = "";
        reference.Path = "/a//b/";
        reference.HasQuery = true;
        reference.Query = "q=%23";
        reference.HasFragment = true;
        reference.Fragment = "";
        IUriComposer composer = new UriComposer();

        UriTextResult result = composer.TryComposeReference(
            reference,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(reference.OriginalText, Is.Empty);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo("x://user%40name@[v1.a:b]:/a//b/?q=%23#"));
            Assert.That(result.Length, Is.EqualTo(result.Text.Length));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [TestCase("scheme", UriComponent.Scheme)]
    [TestCase("authority", UriComponent.Authority)]
    [TestCase("userinfo", UriComponent.UserInfo)]
    [TestCase("host", UriComponent.Host)]
    [TestCase("port", UriComponent.Port)]
    [TestCase("path", UriComponent.Path)]
    [TestCase("query", UriComponent.Query)]
    [TestCase("fragment", UriComponent.Fragment)]
    [TestCase("kind", UriComponent.Input)]
    public void CallerCreatedInvalidModelsReturnNoPartialComposition(
        string mutation,
        UriComponent component)
    {
        IUriComposer composer = new UriComposer();
        UriReference reference = CreateInvalidModel(mutation);

        UriTextResult result = composer.TryComposeReference(
            reference,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, mutation);
            Assert.That(result.Text, Is.Empty, mutation);
            Assert.That(result.Length, Is.Zero, mutation);
            Assert.That(result.Diagnostic, Is.Not.Null, mutation);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Uri), mutation);
            Assert.That(result.Diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error), mutation);
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component), mutation);
        });
    }

    [TestCase("scheme", "path", UriComponent.Scheme)]
    [TestCase("authority", "x:path", UriComponent.Authority)]
    [TestCase("host", "x:///", UriComponent.Host)]
    [TestCase("path", "x:", UriComponent.Path)]
    [TestCase("query", "x:path", UriComponent.Query)]
    [TestCase("fragment", "x:path", UriComponent.Fragment)]
    [TestCase("absolute", "/path", UriComponent.Scheme)]
    public void ComposerAppliesEveryValidationRequirement(
        string requirement,
        string text,
        UriComponent component)
    {
        IUriParser parser = new UriParser();
        IUriComposer composer = new UriComposer();
        UriReference reference = parser.ParseReference(text, new UriParseOptions());
        UriValidationOptions options = new UriValidationOptions();
        SetRequirement(options, requirement);

        UriTextResult result = composer.TryComposeReference(reference, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, requirement);
            Assert.That(result.Text, Is.Empty, requirement);
            Assert.That(result.Diagnostic, Is.Not.Null, requirement);
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.MissingRequiredComponent), requirement);
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component), requirement);
        });
    }

    [Test]
    public void OutputLimitExactBoundaryIsSharedByComposeTryComposeAndMeasure()
    {
        const string text = "x://[2001:db8::1]:080/a?b#c";
        UriReference reference = new UriParser().ParseReference(text, new UriParseOptions());
        IUriComposer composer = new UriComposer();
        UriValidationOptions exact = new UriValidationOptions();
        exact.Limits.MaxOutputCharacters = text.Length;
        UriValidationOptions shortLimit = new UriValidationOptions();
        shortLimit.Limits.MaxOutputCharacters = text.Length - 1;

        string composed = composer.ComposeReference(reference, exact);
        int measured = composer.MeasureReference(reference, exact);
        UriTextResult failure = composer.TryComposeReference(reference, shortLimit);

        Assert.Multiple(() =>
        {
            Assert.That(composed, Is.EqualTo(text));
            Assert.That(measured, Is.EqualTo(text.Length));
            Assert.That(failure.Success, Is.False);
            Assert.That(failure.Text, Is.Empty);
            Assert.That(failure.Length, Is.Zero);
            AssertOutputLimit(failure.Diagnostic, text.Length - 1, 1);
        });
    }

    [Test]
    public void ComponentAndPathSegmentLimitsFailBeforeComposition()
    {
        IUriParser parser = new UriParser();
        IUriComposer composer = new UriComposer();
        UriReference componentReference = parser.ParseReference("x:abcdef", new UriParseOptions());
        UriReference segmentReference = parser.ParseReference("a/b/c", new UriParseOptions());
        UriValidationOptions componentOptions = new UriValidationOptions();
        componentOptions.Limits.MaxComponentCharacters = 5;
        UriValidationOptions segmentOptions = new UriValidationOptions();
        segmentOptions.Limits.MaxPathSegments = 2;

        UriTextResult component = composer.TryComposeReference(componentReference, componentOptions);
        UriTextResult segments = composer.TryComposeReference(segmentReference, segmentOptions);

        Assert.Multiple(() =>
        {
            Assert.That(component.Success, Is.False);
            Assert.That(component.Diagnostic, Is.Not.Null);
            Assert.That(component.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.LimitExceeded));
            Assert.That(component.Diagnostic.Component, Is.EqualTo(UriComponent.Path));
            Assert.That(component.Diagnostic.Message, Is.EqualTo(LimitGuard.ComponentLimitMessage));
            Assert.That(segments.Success, Is.False);
            Assert.That(segments.Diagnostic, Is.Not.Null);
            Assert.That(segments.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.LimitExceeded));
            Assert.That(segments.Diagnostic.Component, Is.EqualTo(UriComponent.Path));
            Assert.That(segments.Diagnostic.Message, Is.EqualTo(LimitGuard.CollectionLimitMessage));
        });
    }

    [Test]
    public void ThrowingCompositionAndMeasurementExposeCompleteFailureDiagnostics()
    {
        IUriComposer composer = new UriComposer();
        UriReference invalid = new UriParser().ParseReference("x:path", new UriParseOptions());
        invalid.Path = "bad path";
        UriReference limited = new UriParser().ParseReference("x:abcdef", new UriParseOptions());
        UriValidationOptions limitOptions = new UriValidationOptions();
        limitOptions.Limits.MaxOutputCharacters = 4;

        UriValidationException composition = Assert.Throws<UriValidationException>(() =>
            composer.ComposeReference(invalid, new UriValidationOptions()))!;
        UriValidationException measurement = Assert.Throws<UriValidationException>(() =>
            composer.MeasureReference(limited, limitOptions))!;

        Assert.Multiple(() =>
        {
            AssertDiagnostic(
                composition.Diagnostic,
                UriDiagnosticCode.InvalidPath,
                UriComponent.Path,
                3,
                1,
                DiagnosticFactory.InvalidPathMessage);
            Assert.That(composition.Message, Is.EqualTo(composition.Diagnostic.Message));
            AssertOutputLimit(measurement.Diagnostic, 4, 4);
            Assert.That(measurement.Message, Is.EqualTo(measurement.Diagnostic.Message));
        });
    }

    [Test]
    public void NullReferenceOptionsAndLimitsCoverAllComposerPaths()
    {
        IUriComposer composer = new UriComposer();
        UriReference reference = new UriReference();
        UriValidationOptions noLimits = new UriValidationOptions();
        noLimits.Limits = null!;

        UriTextResult nullReference = composer.TryComposeReference(null!, new UriValidationOptions());
        UriTextResult nullOptions = composer.TryComposeReference(reference, null!);
        UriTextResult nullLimits = composer.TryComposeReference(reference, noLimits);

        Assert.Multiple(() =>
        {
            AssertInvalidInput(nullReference.Diagnostic);
            AssertInvalidInput(nullOptions.Diagnostic);
            AssertInvalidInput(nullLimits.Diagnostic);
        });
        Assert.Throws<UriValidationException>(() =>
            composer.ComposeReference(null!, new UriValidationOptions()));
        Assert.Throws<UriValidationException>(() =>
            composer.ComposeReference(reference, null!));
        Assert.Throws<UriValidationException>(() =>
            composer.MeasureReference(null!, new UriValidationOptions()));
        Assert.Throws<UriValidationException>(() =>
            composer.MeasureReference(reference, noLimits));
    }

    [Test]
    public void NegativeOutputLimitReturnsAStableOutputDiagnostic()
    {
        IUriComposer composer = new UriComposer();
        UriReference reference = new UriParser().ParseReference("", new UriParseOptions());
        UriValidationOptions options = new UriValidationOptions();
        options.Limits.MaxOutputCharacters = -1;

        UriTextResult result = composer.TryComposeReference(reference, options);

        AssertOutputLimit(result.Diagnostic, 0, 0);
    }

    private static UriReference CreateInvalidModel(string mutation)
    {
        UriReference reference = new UriParser().ParseReference(
            "x://user@host:80/path?query#fragment",
            new UriParseOptions());
        switch (mutation)
        {
            case "scheme":
                reference.Scheme = "1bad";
                break;
            case "authority":
                reference.HasAuthority = false;
                break;
            case "userinfo":
                reference.Authority.HasUserInfo = false;
                break;
            case "host":
                reference.Authority.HostKind = UriHostKind.Ipv4;
                break;
            case "port":
                reference.Authority.HasPort = false;
                break;
            case "path":
                reference.Path = "bad path";
                break;
            case "query":
                reference.HasQuery = false;
                break;
            case "fragment":
                reference.HasFragment = false;
                break;
            default:
                reference.Kind = UriReferenceKind.RelativePathReference;
                break;
        }

        return reference;
    }

    private static void SetRequirement(UriValidationOptions options, string requirement)
    {
        switch (requirement)
        {
            case "scheme":
                options.RequireScheme = true;
                break;
            case "authority":
                options.RequireAuthority = true;
                break;
            case "host":
                options.RequireHost = true;
                break;
            case "path":
                options.RequirePath = true;
                break;
            case "query":
                options.RequireQuery = true;
                break;
            case "fragment":
                options.RequireFragment = true;
                break;
            default:
                options.RequireAbsoluteUri = true;
                break;
        }
    }

    private static void AssertOutputLimit(UriDiagnostic? diagnostic, int offset, int length)
    {
        AssertDiagnostic(
            diagnostic,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Output,
            offset,
            length,
            LimitGuard.OutputLimitMessage);
    }

    private static void AssertInvalidInput(UriDiagnostic? diagnostic)
    {
        AssertDiagnostic(
            diagnostic,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input,
            0,
            0,
            DiagnosticFactory.InvalidInputMessage);
    }

    private static void AssertDiagnostic(
        UriDiagnostic? diagnostic,
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        int length,
        string message)
    {
        Assert.That(diagnostic, Is.Not.Null);
        Assert.That(diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Uri));
        Assert.That(diagnostic.Code, Is.EqualTo(code));
        Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
        Assert.That(diagnostic.Component, Is.EqualTo(component));
        Assert.That(diagnostic.Range.Offset, Is.EqualTo(offset));
        Assert.That(diagnostic.Range.Length, Is.EqualTo(length));
        Assert.That(diagnostic.Message, Is.EqualTo(message));
    }
}
