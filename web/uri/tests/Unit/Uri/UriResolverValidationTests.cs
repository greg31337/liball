namespace uri.Tests;

public class UriResolverValidationTests
{
    [TestCase("x:", "x:")]
    [TestCase("x:path", "x:path")]
    [TestCase("x:/path", "x:/path")]
    [TestCase("x://host", "x://host")]
    [TestCase("x://host/path?query", "x://host/path?query")]
    public void AbsoluteBaseFormsAreAccepted(
        string baseText,
        string expected)
    {
        UriResolutionCoreResult result = Resolve(
            baseText,
            "",
            new UriResolutionOptions());

        AssertSuccess(result, expected);
    }

    [TestCase("", UriComponent.Scheme)]
    [TestCase("relative/path", UriComponent.Scheme)]
    [TestCase("//host/path", UriComponent.Scheme)]
    [TestCase("/absolute/path", UriComponent.Scheme)]
    [TestCase("http://host/path#fragment", UriComponent.Fragment)]
    public void BaseMustBeAbsoluteAndMustNotContainAFragment(
        string baseText,
        UriComponent component)
    {
        UriResolutionCoreResult result = Resolve(
            baseText,
            "reference",
            new UriResolutionOptions());

        AssertFailure(
            result,
            UriDiagnosticCode.UnresolvableReference,
            component);
        Assert.That(
            result.Diagnostic!.Message,
            Is.EqualTo(DiagnosticFactory.UnresolvableReferenceMessage));
    }

    [TestCase("http://[bad]/", UriDiagnosticCode.InvalidIpv6Address, UriComponent.Host)]
    [TestCase("x:%GG", UriDiagnosticCode.InvalidPercentEncoding, UriComponent.Path)]
    [TestCase("1bad:path", UriDiagnosticCode.InvalidPath, UriComponent.Path)]
    public void MalformedBasePreservesItsSyntaxDiagnostic(
        string baseText,
        UriDiagnosticCode code,
        UriComponent component)
    {
        UriResolutionCoreResult result = Resolve(
            baseText,
            "reference",
            new UriResolutionOptions());

        AssertFailure(result, code, component);
    }

    [TestCase("x:%GG", UriDiagnosticCode.InvalidPercentEncoding, UriComponent.Path)]
    [TestCase("//[bad]/", UriDiagnosticCode.InvalidIpv6Address, UriComponent.Host)]
    [TestCase("//host:bad/", UriDiagnosticCode.InvalidPort, UriComponent.Port)]
    public void MalformedReferencePreservesItsSyntaxDiagnostic(
        string referenceText,
        UriDiagnosticCode code,
        UriComponent component)
    {
        UriResolutionCoreResult result = Resolve(
            "http://base/path",
            referenceText,
            new UriResolutionOptions());

        AssertFailure(result, code, component);
    }

    [Test]
    public void BaseValidationTakesPrecedenceOverReferenceValidation()
    {
        UriResolutionCoreResult result = Resolve(
            "relative%GG",
            "x:%GG",
            new UriResolutionOptions());

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidPercentEncoding,
            UriComponent.Path);
        Assert.That(result.Diagnostic!.Range.Offset, Is.EqualTo(9));
    }

    [Test]
    public void MissingArgumentsLimitsAndUndefinedModesReturnInvalidInput()
    {
        UriResolutionOptions missingLimits = new UriResolutionOptions();
        missingLimits.Limits = null!;
        UriResolutionOptions invalidMode = new UriResolutionOptions();
        invalidMode.Mode = (UriResolutionMode)99;
        UriResolutionCoreResult[] results = new UriResolutionCoreResult[]
        {
            UriResolverCore.ResolveReferenceText(null, "a", new UriResolutionOptions()),
            UriResolverCore.ResolveReferenceText("x:", null, new UriResolutionOptions()),
            UriResolverCore.ResolveReferenceText("x:", "a", null),
            UriResolverCore.ResolveReferenceText("x:", "a", missingLimits),
            UriResolverCore.ResolveReferenceText("x:", "a", invalidMode)
        };

        foreach (UriResolutionCoreResult result in results)
        {
            AssertFailure(result, UriDiagnosticCode.InvalidInput, UriComponent.Input);
        }
    }

    [TestCase("", "http://user@base:80/a/b?base", true, "base", false, "")]
    [TestCase("#fragment", "http://user@base:80/a/b?base#fragment", true, "base", true, "fragment")]
    [TestCase("#", "http://user@base:80/a/b?base#", true, "base", true, "")]
    [TestCase("?reference", "http://user@base:80/a/b?reference", true, "reference", false, "")]
    [TestCase("?", "http://user@base:80/a/b?", true, "", false, "")]
    [TestCase("relative", "http://user@base:80/a/relative", false, "", false, "")]
    public void EmptyAndRelativeReferencesApplyQueryAndFragmentInheritance(
        string referenceText,
        string expectedText,
        bool hasQuery,
        string query,
        bool hasFragment,
        string fragment)
    {
        UriResolutionCoreResult result = Resolve(
            "http://user@base:80/a/b?base",
            referenceText,
            new UriResolutionOptions());

        AssertSuccess(result, expectedText);
        Assert.Multiple(() =>
        {
            Assert.That(result.Reference.HasAuthority, Is.True);
            Assert.That(result.Reference.Authority.HasUserInfo, Is.True);
            Assert.That(result.Reference.Authority.UserInfo, Is.EqualTo("user"));
            Assert.That(result.Reference.Authority.Host, Is.EqualTo("base"));
            Assert.That(result.Reference.Authority.HasPort, Is.True);
            Assert.That(result.Reference.Authority.Port, Is.EqualTo("80"));
            Assert.That(result.Reference.HasQuery, Is.EqualTo(hasQuery));
            Assert.That(result.Reference.Query, Is.EqualTo(query));
            Assert.That(result.Reference.HasFragment, Is.EqualTo(hasFragment));
            Assert.That(result.Reference.Fragment, Is.EqualTo(fragment));
        });
    }

    [Test]
    public void PresentEmptyBaseQueryIsInheritedWithoutInventingAFragment()
    {
        UriResolutionCoreResult result = Resolve(
            "http://base/path?",
            "",
            new UriResolutionOptions());

        AssertSuccess(result, "http://base/path?");
        Assert.Multiple(() =>
        {
            Assert.That(result.Reference.HasQuery, Is.True);
            Assert.That(result.Reference.Query, Is.Empty);
            Assert.That(result.Reference.HasFragment, Is.False);
            Assert.That(result.Reference.Fragment, Is.Empty);
        });
    }

    [Test]
    public void NetworkPathReferenceReplacesEveryAuthoritySubcomponent()
    {
        UriResolutionCoreResult result = Resolve(
            "http://base-user@base:80/a/b?base",
            "//new-user@[2001:db8::1]:/x/../y?#",
            new UriResolutionOptions());

        AssertSuccess(result, "http://new-user@[2001:db8::1]:/y?#");
        Assert.Multiple(() =>
        {
            Assert.That(result.Reference.Scheme, Is.EqualTo("http"));
            Assert.That(result.Reference.Authority.UserInfo, Is.EqualTo("new-user"));
            Assert.That(result.Reference.Authority.HostKind, Is.EqualTo(UriHostKind.Ipv6));
            Assert.That(result.Reference.Authority.Host, Is.EqualTo("2001:db8::1"));
            Assert.That(result.Reference.Authority.HasPort, Is.True);
            Assert.That(result.Reference.Authority.Port, Is.Empty);
            Assert.That(result.Reference.Path, Is.EqualTo("/y"));
            Assert.That(result.Reference.HasQuery, Is.True);
            Assert.That(result.Reference.Query, Is.Empty);
            Assert.That(result.Reference.HasFragment, Is.True);
            Assert.That(result.Reference.Fragment, Is.Empty);
        });
    }

    [Test]
    public void AbsoluteReferenceReplacesSchemeAuthorityPathAndQuery()
    {
        UriResolutionCoreResult result = Resolve(
            "http://base/a/b?base",
            "HTTPS:/x/../y?reference#fragment",
            new UriResolutionOptions());

        AssertSuccess(result, "HTTPS:/y?reference#fragment");
        Assert.Multiple(() =>
        {
            Assert.That(result.Reference.Scheme, Is.EqualTo("HTTPS"));
            Assert.That(result.Reference.HasAuthority, Is.False);
            Assert.That(result.Reference.Authority.HostKind, Is.EqualTo(UriHostKind.Absent));
            Assert.That(result.Reference.Path, Is.EqualTo("/y"));
            Assert.That(result.Reference.Query, Is.EqualTo("reference"));
            Assert.That(result.Reference.Fragment, Is.EqualTo("fragment"));
        });
    }

    [Test]
    public void ConfiguredModeSelectsStrictOrCompatibleResolution()
    {
        UriResolutionOptions strictOptions = new UriResolutionOptions();
        UriResolutionOptions compatibleOptions = new UriResolutionOptions();
        compatibleOptions.Mode = UriResolutionMode.Compatible;

        UriResolutionCoreResult strict = Resolve(
            "http://a/b/c/d;p?q",
            "HTTP:g",
            strictOptions);
        UriResolutionCoreResult compatible = Resolve(
            "http://a/b/c/d;p?q",
            "HTTP:g",
            compatibleOptions);

        Assert.Multiple(() =>
        {
            AssertSuccess(strict, "HTTP:g");
            AssertSuccess(compatible, "http://a/b/c/g");
        });
    }

    [Test]
    public void ResolutionAppliesInputComponentSegmentAndOutputLimits()
    {
        UriResolutionOptions baseInput = new UriResolutionOptions();
        baseInput.Limits.MaxInputCharacters = 2;
        UriResolutionOptions referenceInput = new UriResolutionOptions();
        referenceInput.Limits.MaxInputCharacters = 2;
        UriResolutionOptions component = new UriResolutionOptions();
        component.Limits.MaxComponentCharacters = 3;
        UriResolutionOptions segments = new UriResolutionOptions();
        segments.Limits.MaxPathSegments = 2;
        UriResolutionOptions output = new UriResolutionOptions();
        output.Limits.MaxOutputCharacters = 6;

        UriResolutionCoreResult baseInputFailure = Resolve("x:abc", "a", baseInput);
        UriResolutionCoreResult referenceInputFailure = Resolve("x:", "abc", referenceInput);
        UriResolutionCoreResult componentFailure = Resolve("x:/a/", "bb", component);
        UriResolutionCoreResult segmentFailure = Resolve("x:/a/", "b/c", segments);
        UriResolutionCoreResult outputFailure = Resolve("x:/a/", "bbbb", output);

        Assert.Multiple(() =>
        {
            AssertFailure(baseInputFailure, UriDiagnosticCode.LimitExceeded, UriComponent.Input);
            AssertFailure(referenceInputFailure, UriDiagnosticCode.LimitExceeded, UriComponent.Input);
            AssertFailure(componentFailure, UriDiagnosticCode.LimitExceeded, UriComponent.Path);
            AssertFailure(segmentFailure, UriDiagnosticCode.LimitExceeded, UriComponent.Path);
            AssertFailure(outputFailure, UriDiagnosticCode.LimitExceeded, UriComponent.Output);
        });
    }

    [Test]
    public void ResolutionResultRetainsSuccessAndFailureState()
    {
        UriDiagnostic diagnostic = DiagnosticFactory.CreateUnresolvableReference(
            UriComponent.Scheme);
        UriReference reference = new UriReference();
        reference.Kind = UriReferenceKind.Uri;
        reference.HasScheme = true;
        reference.Scheme = "x";
        UriResolutionCoreResult success = new UriResolutionCoreResult(
            true,
            reference,
            "x:",
            2,
            null);
        UriResolutionCoreResult failure = new UriResolutionCoreResult(
            false,
            new UriReference(),
            "",
            0,
            diagnostic);

        Assert.Multiple(() =>
        {
            Assert.That(success.Success, Is.True);
            Assert.That(success.Reference, Is.SameAs(reference));
            Assert.That(success.Text, Is.EqualTo("x:"));
            Assert.That(success.Length, Is.EqualTo(2));
            Assert.That(success.Diagnostic, Is.Null);
            Assert.That(failure.Success, Is.False);
            Assert.That(failure.Reference.Kind, Is.EqualTo(UriReferenceKind.EmptyReference));
            Assert.That(failure.Text, Is.Empty);
            Assert.That(failure.Length, Is.Zero);
            Assert.That(failure.Diagnostic, Is.SameAs(diagnostic));
        });
    }

    private static UriResolutionCoreResult Resolve(
        string baseText,
        string referenceText,
        UriResolutionOptions options)
    {
        return UriResolverCore.ResolveReferenceText(
            baseText,
            referenceText,
            options);
    }

    private static void AssertSuccess(
        UriResolutionCoreResult result,
        string expectedText)
    {
        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
        Assert.That(result.Text, Is.EqualTo(expectedText));
        Assert.That(result.Length, Is.EqualTo(expectedText.Length));
        Assert.That(result.Reference.Kind, Is.EqualTo(UriReferenceKind.Uri));
        Assert.That(result.Diagnostic, Is.Null);
    }

    private static void AssertFailure(
        UriResolutionCoreResult result,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reference.Kind, Is.EqualTo(UriReferenceKind.EmptyReference));
        Assert.That(result.Text, Is.Empty);
        Assert.That(result.Length, Is.Zero);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Uri));
        Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
        Assert.That(result.Diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
        Assert.That(result.Diagnostic.Message, Is.Not.Empty);
    }
}
