namespace uri.Tests;

public class TemplateProcessorCoreTests
{
    [Test]
    public void ExpandToUriReturnsTheParsedExpandedReference()
    {
        UriTemplate template = Parse(
            "https://{host}{/segments*}{?q}");
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            ScalarBinding("host", "example.com"),
            ListBinding("segments", new string[] { "a b", "c" }),
            ScalarBinding("q", "x/y")
        };

        UriParseResult result = TemplateProcessorCore.ExpandToUri(
            template,
            bindings,
            new UriTemplateExpansionOptions(),
            new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(result.Reference.OriginalText, Is.EqualTo(
                "https://example.com/a%20b/c?q=x%2Fy"));
            Assert.That(result.Reference.HasScheme, Is.True);
            Assert.That(result.Reference.Scheme, Is.EqualTo("https"));
            Assert.That(result.Reference.HasAuthority, Is.True);
            Assert.That(result.Reference.Authority.Host, Is.EqualTo(
                "example.com"));
            Assert.That(result.Reference.Path, Is.EqualTo("/a%20b/c"));
            Assert.That(result.Reference.HasQuery, Is.True);
            Assert.That(result.Reference.Query, Is.EqualTo("q=x%2Fy"));
        });
    }

    [Test]
    public void ExpandToUriPreservesRelativeReferenceClassification()
    {
        UriParseResult result = TemplateProcessorCore.ExpandToUri(
            Parse("../{path}"),
            new UriTemplateBinding[] { ScalarBinding("path", "a b") },
            new UriTemplateExpansionOptions(),
            new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Reference.Kind, Is.EqualTo(
                UriReferenceKind.RelativePathReference));
            Assert.That(result.Reference.Path, Is.EqualTo("../a%20b"));
        });
    }

    [Test]
    public void ExpandToUriReportsExpansionSyntaxAndParseLimitFailures()
    {
        string malformed = "x" + new string('\ud800', 1);
        UriParseOptions shortInput = new UriParseOptions();
        shortInput.Limits.MaxInputCharacters = 2;

        UriParseResult expansion = TemplateProcessorCore.ExpandToUri(
            Parse("{value}"),
            new UriTemplateBinding[] { ScalarBinding("value", malformed) },
            new UriTemplateExpansionOptions(),
            new UriParseOptions());
        UriParseResult syntax = TemplateProcessorCore.ExpandToUri(
            Parse("{+value}"),
            new UriTemplateBinding[]
            {
                ScalarBinding("value", "http://[bad]")
            },
            new UriTemplateExpansionOptions(),
            new UriParseOptions());
        UriParseResult limit = TemplateProcessorCore.ExpandToUri(
            Parse("{value}"),
            new UriTemplateBinding[] { ScalarBinding("value", "abc") },
            new UriTemplateExpansionOptions(),
            shortInput);

        Assert.Multiple(() =>
        {
            AssertParseFailure(
                expansion,
                DiagnosticDomain.UriTemplate,
                UriDiagnosticCode.InvalidUnicode,
                UriComponent.TemplateVariable);
            AssertParseFailure(
                syntax,
                DiagnosticDomain.Uri,
                UriDiagnosticCode.InvalidIpv6Address,
                UriComponent.Host);
            AssertParseFailure(
                limit,
                DiagnosticDomain.Uri,
                UriDiagnosticCode.LimitExceeded,
                UriComponent.Input);
        });
    }

    [Test]
    public void ExpandAndResolveUsesTheExpandedTextAsAReference()
    {
        UriTextResult result = TemplateProcessorCore.ExpandAndResolve(
            Parse("../{segment}{?q}"),
            new UriTemplateBinding[]
            {
                ScalarBinding("segment", "c d"),
                ScalarBinding("q", "x/y")
            },
            new UriTemplateExpansionOptions(),
            "http://example.com/a/b/",
            new UriResolutionOptions());

        AssertTextSuccess(
            result,
            "http://example.com/a/c%20d?q=x%2Fy");
    }

    [Test]
    public void ExpandAndResolveHonorsTheSelectedResolutionMode()
    {
        UriTemplate template = Parse("HTTP:{path}");
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            ScalarBinding("path", "g")
        };
        UriResolutionOptions strictOptions = new UriResolutionOptions();
        UriResolutionOptions compatibleOptions = new UriResolutionOptions();
        compatibleOptions.Mode = UriResolutionMode.Compatible;

        UriTextResult strict = TemplateProcessorCore.ExpandAndResolve(
            template,
            bindings,
            new UriTemplateExpansionOptions(),
            "http://example.com/a/b",
            strictOptions);
        UriTextResult compatible = TemplateProcessorCore.ExpandAndResolve(
            template,
            bindings,
            new UriTemplateExpansionOptions(),
            "http://example.com/a/b",
            compatibleOptions);

        Assert.Multiple(() =>
        {
            AssertTextSuccess(strict, "HTTP:g");
            AssertTextSuccess(
                compatible,
                "http://example.com/a/g");
        });
    }

    [Test]
    public void ExpandAndResolvePropagatesExpansionAndResolutionFailures()
    {
        string malformed = new string('\udc00', 1);
        UriTextResult expansion = TemplateProcessorCore.ExpandAndResolve(
            Parse("{value}"),
            new UriTemplateBinding[] { ScalarBinding("value", malformed) },
            new UriTemplateExpansionOptions(),
            "http://example.com/",
            new UriResolutionOptions());
        UriTextResult resolution = TemplateProcessorCore.ExpandAndResolve(
            Parse("{path}"),
            new UriTemplateBinding[] { ScalarBinding("path", "value") },
            new UriTemplateExpansionOptions(),
            "relative/base",
            new UriResolutionOptions());

        Assert.Multiple(() =>
        {
            AssertTextFailure(
                expansion,
                DiagnosticDomain.UriTemplate,
                UriDiagnosticCode.InvalidUnicode,
                UriComponent.TemplateVariable);
            AssertTextFailure(
                resolution,
                DiagnosticDomain.Uri,
                UriDiagnosticCode.UnresolvableReference,
                UriComponent.Scheme);
        });
    }

    [Test]
    public void ExpandAndNormalizeAppliesGenericSyntaxNormalization()
    {
        UriTextResult result = TemplateProcessorCore.ExpandAndNormalize(
            Parse("HTTP://EXAMPLE.COM:080/a/./{value}%7e"),
            new UriTemplateBinding[] { ScalarBinding("value", "b") },
            new UriTemplateExpansionOptions(),
            new UriNormalizationOptions());

        AssertTextSuccess(result, "http://example.com:080/a/b~");
    }

    [Test]
    public void ExpandAndNormalizeAppliesOnlyAnExplicitSchemeProfile()
    {
        UriTemplate template = Parse("HTTP://EXAMPLE.COM:080");
        UriNormalizationOptions genericOptions =
            new UriNormalizationOptions();
        UriNormalizationOptions profileOptions =
            new UriNormalizationOptions();
        UriSchemeProfile profile = new UriSchemeProfile();
        profile.Scheme = "http";
        profile.NormalizeEmptyAuthorityPath = true;
        profile.HasDefaultPort = true;
        profile.DefaultPort = 80;
        profileOptions.Profile = profile;

        UriTextResult generic = TemplateProcessorCore.ExpandAndNormalize(
            template,
            new UriTemplateBinding[0],
            new UriTemplateExpansionOptions(),
            genericOptions);
        UriTextResult profiled = TemplateProcessorCore.ExpandAndNormalize(
            template,
            new UriTemplateBinding[0],
            new UriTemplateExpansionOptions(),
            profileOptions);

        Assert.Multiple(() =>
        {
            AssertTextSuccess(generic, "http://example.com:080");
            AssertTextSuccess(profiled, "http://example.com/");
        });
    }

    [Test]
    public void ExpandAndNormalizePropagatesEveryStageFailure()
    {
        string malformed = new string('\ud800', 1);
        UriNormalizationOptions invalidProfile =
            new UriNormalizationOptions();
        invalidProfile.Profile = new UriSchemeProfile();
        UriNormalizationOptions shortInput =
            new UriNormalizationOptions();
        shortInput.Limits.MaxInputCharacters = 2;

        UriTextResult expansion = TemplateProcessorCore.ExpandAndNormalize(
            Parse("{value}"),
            new UriTemplateBinding[] { ScalarBinding("value", malformed) },
            new UriTemplateExpansionOptions(),
            new UriNormalizationOptions());
        UriTextResult syntax = TemplateProcessorCore.ExpandAndNormalize(
            Parse("{+value}"),
            new UriTemplateBinding[]
            {
                ScalarBinding("value", "http://[bad]")
            },
            new UriTemplateExpansionOptions(),
            new UriNormalizationOptions());
        UriTextResult profile = TemplateProcessorCore.ExpandAndNormalize(
            Parse("http://example.com"),
            new UriTemplateBinding[0],
            new UriTemplateExpansionOptions(),
            invalidProfile);
        UriTextResult limit = TemplateProcessorCore.ExpandAndNormalize(
            Parse("{value}"),
            new UriTemplateBinding[] { ScalarBinding("value", "abc") },
            new UriTemplateExpansionOptions(),
            shortInput);

        Assert.Multiple(() =>
        {
            AssertTextFailure(
                expansion,
                DiagnosticDomain.UriTemplate,
                UriDiagnosticCode.InvalidUnicode,
                UriComponent.TemplateVariable);
            AssertTextFailure(
                syntax,
                DiagnosticDomain.Uri,
                UriDiagnosticCode.InvalidIpv6Address,
                UriComponent.Host);
            AssertTextFailure(
                profile,
                DiagnosticDomain.Uri,
                UriDiagnosticCode.InvalidInput,
                UriComponent.Input);
            AssertTextFailure(
                limit,
                DiagnosticDomain.Uri,
                UriDiagnosticCode.LimitExceeded,
                UriComponent.Input);
        });
    }

    private static UriTemplate Parse(string text)
    {
        UriTemplateParseResult result = TemplateParserCore.ParseTemplate(
            text.AsSpan());
        Assert.That(result.Success, Is.True);
        return result.Template;
    }

    private static UriTemplateBinding ScalarBinding(string name, string value)
    {
        UriTemplateValue scalar = new UriTemplateValue();
        scalar.Kind = UriTemplateValueKind.Scalar;
        scalar.Scalar = value;
        return new UriTemplateBinding(name, scalar);
    }

    private static UriTemplateBinding ListBinding(
        string name,
        string[] values)
    {
        UriTemplateValue list = new UriTemplateValue();
        list.Kind = UriTemplateValueKind.List;
        list.List = values;
        return new UriTemplateBinding(name, list);
    }

    private static void AssertParseFailure(
        UriParseResult result,
        DiagnosticDomain domain,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reference.OriginalText, Is.Empty);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Domain, Is.EqualTo(domain));
        Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
    }

    private static void AssertTextSuccess(
        UriTextResult result,
        string expected)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Text, Is.EqualTo(expected));
        Assert.That(result.Length, Is.EqualTo(expected.Length));
        Assert.That(result.Diagnostic, Is.Null);
    }

    private static void AssertTextFailure(
        UriTextResult result,
        DiagnosticDomain domain,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Text, Is.Empty);
        Assert.That(result.Length, Is.EqualTo(0));
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Domain, Is.EqualTo(domain));
        Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
        Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
    }
}
