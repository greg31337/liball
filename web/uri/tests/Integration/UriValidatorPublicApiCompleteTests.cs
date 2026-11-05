namespace uri.Tests;

public class UriValidatorPublicApiCompleteTests
{
    [TestCase("scheme", "x:path", UriComponent.Scheme)]
    [TestCase("authority", "x://", UriComponent.Authority)]
    [TestCase("host", "x://host", UriComponent.Host)]
    [TestCase("path", "x:path", UriComponent.Path)]
    [TestCase("query", "x:?", UriComponent.Query)]
    [TestCase("fragment", "x:#", UriComponent.Fragment)]
    [TestCase("absolute", "x:path", UriComponent.Scheme)]
    public void EveryValidationRequirementAcceptsItsPresentComponent(
        string requirement,
        string text,
        UriComponent component)
    {
        IUriValidator validator = new UriValidator();
        UriValidationOptions options = new UriValidationOptions();
        SetRequirement(options, requirement);

        UriValidationResult result = validator.ValidateReference(text, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, requirement);
            Assert.That(result.Reference.OriginalText, Is.EqualTo(text), requirement);
            Assert.That(result.Diagnostic, Is.Null, component.ToString());
            Assert.That(result.Diagnostics, Is.Empty, component.ToString());
        });
    }

    [TestCase("scheme", "path", UriComponent.Scheme)]
    [TestCase("authority", "x:path", UriComponent.Authority)]
    [TestCase("host", "x:///", UriComponent.Host)]
    [TestCase("path", "x:", UriComponent.Path)]
    [TestCase("query", "x:path", UriComponent.Query)]
    [TestCase("fragment", "x:path", UriComponent.Fragment)]
    [TestCase("absolute", "/path", UriComponent.Scheme)]
    public void EveryValidationRequirementReportsItsMissingComponent(
        string requirement,
        string text,
        UriComponent component)
    {
        IUriValidator validator = new UriValidator();
        UriValidationOptions options = new UriValidationOptions();
        SetRequirement(options, requirement);

        UriValidationResult result = validator.ValidateReference(text, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, requirement);
            Assert.That(result.Reference.OriginalText, Is.EqualTo(text), requirement);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1), requirement);
            Assert.That(result.Diagnostics[0], Is.SameAs(result.Diagnostic), requirement);
            AssertDiagnostic(
                result.Diagnostic,
                UriDiagnosticCode.MissingRequiredComponent,
                component,
                0,
                0,
                DiagnosticFactory.MissingRequiredComponentMessage);
        });
    }

    [Test]
    public void AbsoluteUriRequirementRejectsFragmentsWithCompleteDiagnostic()
    {
        IUriValidator validator = new UriValidator();
        UriValidationOptions options = new UriValidationOptions();
        options.RequireAbsoluteUri = true;

        UriValidationResult result = validator.ValidateReference("x:path#fragment", options);

        AssertDiagnostic(
            result.Diagnostic,
            UriDiagnosticCode.InvalidComponent,
            UriComponent.Fragment,
            0,
            0,
            DiagnosticFactory.InvalidComponentMessage);
    }

    [Test]
    public void PublicReferenceValidationAppliesInputComponentAndPathSegmentLimits()
    {
        IUriValidator validator = new UriValidator();
        UriValidationOptions inputOptions = new UriValidationOptions();
        inputOptions.Limits.MaxInputCharacters = 4;
        UriValidationOptions componentOptions = new UriValidationOptions();
        componentOptions.Limits.MaxComponentCharacters = 3;
        UriValidationOptions segmentOptions = new UriValidationOptions();
        segmentOptions.Limits.MaxPathSegments = 2;

        UriValidationResult input = validator.ValidateReference("x:path", inputOptions);
        UriValidationResult component = validator.ValidateReference("http:path", componentOptions);
        UriValidationResult segments = validator.ValidateReference("a/b/c", segmentOptions);

        Assert.Multiple(() =>
        {
            AssertDiagnostic(
                input.Diagnostic,
                UriDiagnosticCode.LimitExceeded,
                UriComponent.Input,
                4,
                2,
                LimitGuard.InputLimitMessage);
            AssertDiagnostic(
                component.Diagnostic,
                UriDiagnosticCode.LimitExceeded,
                UriComponent.Scheme,
                3,
                1,
                LimitGuard.ComponentLimitMessage);
            Assert.That(segments.Diagnostic, Is.Not.Null);
            Assert.That(segments.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.LimitExceeded));
            Assert.That(segments.Diagnostic.Component, Is.EqualTo(UriComponent.Path));
            Assert.That(segments.Diagnostic.Message, Is.EqualTo(LimitGuard.CollectionLimitMessage));
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
    public void CallerCreatedInvalidModelsFailInTheirOwningComponent(
        string mutation,
        UriComponent component)
    {
        IUriValidator validator = new UriValidator();
        UriReference reference = CreateInvalidModel(mutation);

        UriValidationResult result = validator.ValidateComponents(
            reference,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, mutation);
            Assert.That(result.Reference, Is.SameAs(reference), mutation);
            Assert.That(result.Diagnostic, Is.Not.Null, mutation);
            Assert.That(result.Diagnostic!.Component, Is.EqualTo(component), mutation);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1), mutation);
            Assert.That(result.Diagnostics[0], Is.SameAs(result.Diagnostic), mutation);
        });
    }

    [Test]
    public void ValidateAllDiagnosticsReturnsEveryCallerModelFailureInOrder()
    {
        IUriValidator validator = new UriValidator();
        UriReference reference = new UriReference();
        reference.Kind = UriReferenceKind.Uri;
        reference.HasScheme = true;
        reference.Scheme = "1bad";
        reference.HasAuthority = true;
        reference.Authority.HasUserInfo = true;
        reference.Authority.UserInfo = "bad[";
        reference.Authority.HostKind = UriHostKind.RegisteredName;
        reference.Authority.Host = "bad[";
        reference.Authority.HasPort = true;
        reference.Authority.Port = "x";
        reference.Path = "bad path";
        reference.HasQuery = true;
        reference.Query = "bad[";
        reference.HasFragment = true;
        reference.Fragment = "bad[";

        UriDiagnostic[] diagnostics = validator.ValidateAllDiagnostics(
            reference,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics, Has.Length.EqualTo(7));
            Assert.That(diagnostics[0].Component, Is.EqualTo(UriComponent.Scheme));
            Assert.That(diagnostics[1].Component, Is.EqualTo(UriComponent.UserInfo));
            Assert.That(diagnostics[2].Component, Is.EqualTo(UriComponent.Host));
            Assert.That(diagnostics[3].Component, Is.EqualTo(UriComponent.Port));
            Assert.That(diagnostics[4].Component, Is.EqualTo(UriComponent.Path));
            Assert.That(diagnostics[5].Component, Is.EqualTo(UriComponent.Query));
            Assert.That(diagnostics[6].Component, Is.EqualTo(UriComponent.Fragment));
        });
    }

    [Test]
    public void NullValuesAndOptionsFailEveryValidatorMethodDeterministically()
    {
        IUriValidator validator = new UriValidator();
        UriReference reference = new UriReference();
        UriValidationOptions noLimits = new UriValidationOptions();
        noLimits.Limits = null!;

        UriValidationResult nullText = validator.ValidateReference(null!, new UriValidationOptions());
        UriValidationResult nullTextOptions = validator.ValidateReference("x:a", null!);
        UriValidationResult nullReference = validator.ValidateComponents(null!, new UriValidationOptions());
        UriValidationResult nullComponentOptions = validator.ValidateComponents(reference, null!);
        UriValidationResult nullLimits = validator.ValidateComponents(reference, noLimits);
        UriDiagnostic[] allNullReference = validator.ValidateAllDiagnostics(null!, new UriValidationOptions());
        UriDiagnostic[] allNullOptions = validator.ValidateAllDiagnostics(reference, null!);
        UriDiagnostic[] allNullLimits = validator.ValidateAllDiagnostics(reference, noLimits);

        Assert.Multiple(() =>
        {
            AssertInvalidInputResult(nullText);
            AssertInvalidInputResult(nullTextOptions);
            AssertInvalidInputResult(nullReference);
            AssertInvalidInputResult(nullComponentOptions);
            AssertInvalidInputResult(nullLimits);
            AssertInvalidInputArray(allNullReference);
            AssertInvalidInputArray(allNullOptions);
            AssertInvalidInputArray(allNullLimits);
        });
    }

    [Test]
    public void PresentEmptyQueryAndFragmentSatisfyPublicPresenceRequirements()
    {
        IUriValidator validator = new UriValidator();
        UriValidationOptions options = new UriValidationOptions();
        options.RequireQuery = true;
        options.RequireFragment = true;

        UriValidationResult result = validator.ValidateReference("?#", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Reference.HasQuery, Is.True);
            Assert.That(result.Reference.Query, Is.Empty);
            Assert.That(result.Reference.HasFragment, Is.True);
            Assert.That(result.Reference.Fragment, Is.Empty);
        });
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

    private static UriReference CreateInvalidModel(string mutation)
    {
        IUriParser parser = new UriParser();
        UriReference reference = parser.ParseReference(
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

    private static void AssertInvalidInputResult(UriValidationResult result)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
        Assert.That(result.Diagnostics[0], Is.SameAs(result.Diagnostic));
        AssertDiagnostic(
            result.Diagnostic,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input,
            0,
            0,
            DiagnosticFactory.InvalidInputMessage);
    }

    private static void AssertInvalidInputArray(UriDiagnostic[] diagnostics)
    {
        Assert.That(diagnostics, Has.Length.EqualTo(1));
        AssertDiagnostic(
            diagnostics[0],
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
