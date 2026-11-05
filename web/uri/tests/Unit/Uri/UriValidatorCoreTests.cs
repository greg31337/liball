namespace uri.Tests;

public class UriValidatorCoreTests
{
    [TestCase("")]
    [TestCase("path")]
    [TestCase("http://user@example.com:80/a/b?query#fragment")]
    [TestCase("x://[2001:db8::1]/")]
    public void WholeReferenceValidationReturnsTheParsedReference(string text)
    {
        UriValidationResult result = UriValidatorCore.ValidateReference(
            text.AsSpan(),
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, text);
            Assert.That(result.Reference.OriginalText, Is.EqualTo(text));
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void WholeReferenceSyntaxFailureReturnsOnlyTheParserDiagnostic()
    {
        UriValidationResult result = UriValidatorCore.ValidateReference(
            "x:path%GG".AsSpan(),
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Reference.OriginalText, Is.Empty);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidPercentEncoding));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Path));
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0], Is.SameAs(result.Diagnostic));
        });
    }

    [Test]
    public void InputLimitPrecedesParsingAndReportsTheExcessRange()
    {
        UriValidationOptions options = new UriValidationOptions();
        options.Limits.MaxInputCharacters = 4;

        UriValidationResult result = UriValidatorCore.ValidateReference(
            "x:path%GG".AsSpan(),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.LimitExceeded));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(UriComponent.Input));
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(4));
            Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(5));
            Assert.That(result.Diagnostic.Message, Is.EqualTo(LimitGuard.InputLimitMessage));
        });
    }

    [Test]
    public void ComponentAndPathSegmentLimitsApplyAfterParsing()
    {
        UriValidationOptions componentOptions = new UriValidationOptions();
        componentOptions.Limits.MaxComponentCharacters = 3;
        UriValidationOptions segmentOptions = new UriValidationOptions();
        segmentOptions.Limits.MaxPathSegments = 2;

        UriValidationResult component = UriValidatorCore.ValidateReference(
            "http:path".AsSpan(),
            componentOptions);
        UriValidationResult segments = UriValidatorCore.ValidateReference(
            "a/b/c".AsSpan(),
            segmentOptions);

        Assert.Multiple(() =>
        {
            Assert.That(component.Diagnostic, Is.Not.Null);
            Assert.That(component.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.LimitExceeded));
            Assert.That(component.Diagnostic.Component, Is.EqualTo(UriComponent.Scheme));
            Assert.That(component.Diagnostic.Range.Offset, Is.EqualTo(3));
            Assert.That(component.Diagnostic.Range.Length, Is.EqualTo(1));
            Assert.That(segments.Diagnostic, Is.Not.Null);
            Assert.That(segments.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.LimitExceeded));
            Assert.That(segments.Diagnostic.Component, Is.EqualTo(UriComponent.Path));
            Assert.That(segments.Diagnostic.Message, Is.EqualTo(LimitGuard.CollectionLimitMessage));
        });
    }

    [TestCase("scheme", "path", UriComponent.Scheme)]
    [TestCase("authority", "x:path", UriComponent.Authority)]
    [TestCase("host", "x:///", UriComponent.Host)]
    [TestCase("path", "x://host", UriComponent.Path)]
    [TestCase("query", "x:path", UriComponent.Query)]
    [TestCase("fragment", "x:path", UriComponent.Fragment)]
    [TestCase("absolute", "/path", UriComponent.Scheme)]
    public void EachRequirementReportsItsMissingComponent(
        string requirement,
        string text,
        UriComponent component)
    {
        UriValidationOptions options = new UriValidationOptions();
        SetRequirement(options, requirement);

        UriValidationResult result = UriValidatorCore.ValidateReference(
            text.AsSpan(),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Reference.OriginalText, Is.EqualTo(text));
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.MissingRequiredComponent));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
            Assert.That(result.Diagnostic.Range.Offset, Is.Zero);
            Assert.That(result.Diagnostic.Range.Length, Is.Zero);
            Assert.That(result.Diagnostic.Message, Is.EqualTo(DiagnosticFactory.MissingRequiredComponentMessage));
        });
    }

    [Test]
    public void PresentEmptyQueryAndFragmentSatisfyPresenceRequirements()
    {
        UriValidationOptions options = new UriValidationOptions();
        options.RequireQuery = true;
        options.RequireFragment = true;

        UriValidationResult result = UriValidatorCore.ValidateReference("?#".AsSpan(), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Reference.HasQuery, Is.True);
            Assert.That(result.Reference.Query, Is.Empty);
            Assert.That(result.Reference.HasFragment, Is.True);
            Assert.That(result.Reference.Fragment, Is.Empty);
        });
    }

    [Test]
    public void AbsoluteUriRequirementAllowsUriKindButRejectsFragments()
    {
        UriValidationOptions options = new UriValidationOptions();
        options.RequireAbsoluteUri = true;

        UriValidationResult valid = UriValidatorCore.ValidateReference("x:path".AsSpan(), options);
        UriValidationResult fragment = UriValidatorCore.ValidateReference(
            "x:path#fragment".AsSpan(),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.Reference.Kind, Is.EqualTo(UriReferenceKind.Uri));
            Assert.That(fragment.Success, Is.False);
            Assert.That(fragment.Diagnostic, Is.Not.Null);
            Assert.That(fragment.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidComponent));
            Assert.That(fragment.Diagnostic.Component, Is.EqualTo(UriComponent.Fragment));
            Assert.That(fragment.Diagnostic.Message, Is.EqualTo(DiagnosticFactory.InvalidComponentMessage));
        });
    }

    [Test]
    public void ValidCallerCreatedComponentsAreReturnedWithoutReplacement()
    {
        UriReference reference = UriParserCore.ParseReference(
            "http://192.0.2.1/a?b#c".AsSpan()).Reference;

        UriValidationResult result = UriValidatorCore.ValidateComponents(
            reference,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Reference, Is.SameAs(reference));
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void FirstDiagnosticModeAndAllDiagnosticModeHaveDeterministicOrdering()
    {
        UriReference reference = CreateMultiplyInvalidReference();
        UriValidationOptions options = new UriValidationOptions();

        UriValidationResult first = UriValidatorCore.ValidateComponents(reference, options);
        UriDiagnostic[] all = UriValidatorCore.ValidateAllDiagnostics(reference, options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.False);
            Assert.That(first.Reference, Is.SameAs(reference));
            Assert.That(first.Diagnostic, Is.Not.Null);
            Assert.That(first.Diagnostic!.Component, Is.EqualTo(UriComponent.Scheme));
            Assert.That(first.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(all, Has.Length.EqualTo(7));
            Assert.That(all[0].Component, Is.EqualTo(UriComponent.Scheme));
            Assert.That(all[1].Component, Is.EqualTo(UriComponent.UserInfo));
            Assert.That(all[2].Component, Is.EqualTo(UriComponent.Host));
            Assert.That(all[3].Component, Is.EqualTo(UriComponent.Port));
            Assert.That(all[4].Component, Is.EqualTo(UriComponent.Path));
            Assert.That(all[5].Component, Is.EqualTo(UriComponent.Query));
            Assert.That(all[6].Component, Is.EqualTo(UriComponent.Fragment));
        });
    }

    [Test]
    public void AllMissingRequirementsAreReportedOnceInComponentOrder()
    {
        UriValidationOptions options = new UriValidationOptions();
        options.RequireScheme = true;
        options.RequireAuthority = true;
        options.RequireHost = true;
        options.RequirePath = true;
        options.RequireQuery = true;
        options.RequireFragment = true;

        UriDiagnostic[] diagnostics = UriValidatorCore.ValidateAllDiagnostics(
            new UriReference(),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics, Has.Length.EqualTo(6));
            Assert.That(diagnostics[0].Component, Is.EqualTo(UriComponent.Scheme));
            Assert.That(diagnostics[1].Component, Is.EqualTo(UriComponent.Authority));
            Assert.That(diagnostics[2].Component, Is.EqualTo(UriComponent.Host));
            Assert.That(diagnostics[3].Component, Is.EqualTo(UriComponent.Path));
            Assert.That(diagnostics[4].Component, Is.EqualTo(UriComponent.Query));
            Assert.That(diagnostics[5].Component, Is.EqualTo(UriComponent.Fragment));
            Assert.That(
                diagnostics.All(diagnostic => diagnostic.Code == UriDiagnosticCode.MissingRequiredComponent),
                Is.True);
        });
    }

    [Test]
    public void InconsistentPresenceFlagsAndReferenceKindAreDiagnosed()
    {
        UriReference reference = UriParserCore.ParseReference("path".AsSpan()).Reference;
        reference.HasQuery = false;
        reference.Query = "stale";
        reference.Kind = UriReferenceKind.AbsolutePathReference;

        UriDiagnostic[] diagnostics = UriValidatorCore.ValidateAllDiagnostics(
            reference,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics, Has.Length.EqualTo(2));
            Assert.That(diagnostics[0].Code, Is.EqualTo(UriDiagnosticCode.InvalidComponent));
            Assert.That(diagnostics[0].Component, Is.EqualTo(UriComponent.Input));
            Assert.That(diagnostics[1].Code, Is.EqualTo(UriDiagnosticCode.InvalidComponent));
            Assert.That(diagnostics[1].Component, Is.EqualTo(UriComponent.Query));
        });
    }

    [Test]
    public void HostKindAndParsedAddressBytesMustAgreeWithHostText()
    {
        UriReference bytes = UriParserCore.ParseReference("x://192.0.2.1/".AsSpan()).Reference;
        bytes.Authority.AddressBytes[0] = 193;
        UriReference kind = UriParserCore.ParseReference("x://192.0.2.1/".AsSpan()).Reference;
        kind.Authority.HostKind = UriHostKind.RegisteredName;
        kind.Authority.AddressBytes = new byte[0];

        UriDiagnostic[] byteDiagnostics = UriValidatorCore.ValidateAllDiagnostics(
            bytes,
            new UriValidationOptions());
        UriDiagnostic[] kindDiagnostics = UriValidatorCore.ValidateAllDiagnostics(
            kind,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(byteDiagnostics, Has.Length.EqualTo(1));
            Assert.That(byteDiagnostics[0].Code, Is.EqualTo(UriDiagnosticCode.InvalidComponent));
            Assert.That(byteDiagnostics[0].Component, Is.EqualTo(UriComponent.Host));
            Assert.That(kindDiagnostics, Has.Length.EqualTo(1));
            Assert.That(kindDiagnostics[0].Code, Is.EqualTo(UriDiagnosticCode.InvalidComponent));
            Assert.That(kindDiagnostics[0].Component, Is.EqualTo(UriComponent.Host));
        });
    }

    [Test]
    public void ComponentValuesUseTheirOwnGrammarAndOffsets()
    {
        UriReference reference = UriParserCore.ParseReference("x:path?#".AsSpan()).Reference;
        reference.Query = "bad#query";
        reference.Fragment = "%GG";

        UriDiagnostic[] diagnostics = UriValidatorCore.ValidateAllDiagnostics(
            reference,
            new UriValidationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics, Has.Length.EqualTo(2));
            Assert.That(diagnostics[0].Code, Is.EqualTo(UriDiagnosticCode.InvalidQuery));
            Assert.That(diagnostics[0].Component, Is.EqualTo(UriComponent.Query));
            Assert.That(diagnostics[0].Range.Offset, Is.EqualTo(3));
            Assert.That(diagnostics[1].Code, Is.EqualTo(UriDiagnosticCode.InvalidPercentEncoding));
            Assert.That(diagnostics[1].Component, Is.EqualTo(UriComponent.Fragment));
            Assert.That(diagnostics[1].Range.Offset, Is.EqualTo(1));
        });
    }

    [Test]
    public void NullValidationInputsReturnInvalidInputDiagnostics()
    {
        UriValidationOptions invalidOptions = new UriValidationOptions();
        invalidOptions.Limits = null!;

        UriValidationResult noOptions = UriValidatorCore.ValidateReference(
            "path".AsSpan(),
            null);
        UriValidationResult noReference = UriValidatorCore.ValidateComponents(
            null,
            new UriValidationOptions());
        UriDiagnostic[] noLimits = UriValidatorCore.ValidateAllDiagnostics(
            new UriReference(),
            invalidOptions);

        Assert.Multiple(() =>
        {
            Assert.That(noOptions.Diagnostic, Is.Not.Null);
            Assert.That(noOptions.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidInput));
            Assert.That(noReference.Diagnostic, Is.Not.Null);
            Assert.That(noReference.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidInput));
            Assert.That(noLimits, Has.Length.EqualTo(1));
            Assert.That(noLimits[0].Code, Is.EqualTo(UriDiagnosticCode.InvalidInput));
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

    private static UriReference CreateMultiplyInvalidReference()
    {
        UriReference reference = new UriReference();
        reference.Kind = UriReferenceKind.Uri;
        reference.HasScheme = true;
        reference.Scheme = "1bad";
        reference.HasAuthority = true;
        reference.Authority.HasUserInfo = true;
        reference.Authority.UserInfo = "bad@";
        reference.Authority.HostKind = UriHostKind.RegisteredName;
        reference.Authority.Host = "bad[";
        reference.Authority.HasPort = true;
        reference.Authority.Port = "x";
        reference.Path = "/bad[";
        reference.HasQuery = true;
        reference.Query = "bad[";
        reference.HasFragment = true;
        reference.Fragment = "bad[";
        return reference;
    }
}
