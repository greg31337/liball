namespace uri.Tests;

public class UriTemplateUriWorkflowIntegrationTests
{
    [Test]
    public void ExpandedTemplateParsesAsUriAndIriThenResolvesAndNormalizes()
    {
        const string expectedExpanded =
            "../%E9%A0%85%E7%9B%AE/caf%C3%A9" +
            "?query=%E6%9D%B1%E4%BA%AC#%E7%89%87";
        const string expectedIri =
            "../項目/café?query=東京#片";
        const string expectedResolved =
            "HTTP://EXAMPLE.COM/root/%E9%A0%85%E7%9B%AE/caf%C3%A9" +
            "?query=%E6%9D%B1%E4%BA%AC#%E7%89%87";
        const string expectedNormalized =
            "http://example.com/root/%E9%A0%85%E7%9B%AE/caf%C3%A9" +
            "?query=%E6%9D%B1%E4%BA%AC#%E7%89%87";
        UriTemplate template = ParseTemplate(
            "../項目/{segment}{?query}{#fragment}");
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            ScalarBinding("segment", "café"),
            ScalarBinding("query", "東京"),
            ScalarBinding("fragment", "片")
        };
        IUriTemplateExpander expander = new UriTemplateExpander();
        IUriParser uriParser = new UriParser();
        IIriConverter iriConverter = new IriConverter();
        IIriParser iriParser = new IriParser();
        IUriTemplateProcessor processor = new UriTemplateProcessor();
        IUriNormalizer normalizer = new UriNormalizer();

        string expanded = expander.ExpandTemplate(
            template,
            bindings,
            new UriTemplateExpansionOptions());
        UriReference uri = uriParser.ParseReference(
            expanded,
            new UriParseOptions());
        string displayIri = iriConverter.ConvertUriToIri(
            expanded,
            new IriConversionOptions());
        IriReference iri = iriParser.ParseIriReference(
            displayIri,
            new IriParseOptions());
        string resolved = processor.ExpandAndResolve(
            template,
            bindings,
            "HTTP://EXAMPLE.COM/root/section/index",
            new UriTemplateExpansionOptions(),
            new UriResolutionOptions());
        string normalized = normalizer.NormalizeSyntax(
            resolved,
            new UriNormalizationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(expanded, Is.EqualTo(expectedExpanded));
            Assert.That(uri.Kind, Is.EqualTo(
                UriReferenceKind.RelativePathReference));
            Assert.That(uri.Path, Is.EqualTo(
                "../%E9%A0%85%E7%9B%AE/caf%C3%A9"));
            Assert.That(uri.Query, Is.EqualTo(
                "query=%E6%9D%B1%E4%BA%AC"));
            Assert.That(uri.Fragment, Is.EqualTo("%E7%89%87"));
            Assert.That(displayIri, Is.EqualTo(expectedIri));
            Assert.That(iri.Path, Is.EqualTo("../項目/café"));
            Assert.That(iri.Query, Is.EqualTo("query=東京"));
            Assert.That(iri.Fragment, Is.EqualTo("片"));
            Assert.That(resolved, Is.EqualTo(expectedResolved));
            Assert.That(normalized, Is.EqualTo(expectedNormalized));
        });
    }

    [Test]
    public void ProcessorNormalizesTheExpandedReferenceWithItsSelectedProfile()
    {
        UriTemplate template = ParseTemplate(
            "HTTP://EXAMPLE.COM/a/./{segment}%7e{?query}");
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            ScalarBinding("segment", "café"),
            ScalarBinding("query", "東京")
        };
        IUriTemplateProcessor processor = new UriTemplateProcessor();

        string normalized = processor.ExpandAndNormalize(
            template,
            bindings,
            new UriTemplateExpansionOptions(),
            new UriNormalizationOptions());
        UriReference parsed = processor.ExpandToUri(
            ParseTemplate(normalized),
            new UriTemplateBinding[0],
            new UriTemplateExpansionOptions(),
            new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(normalized, Is.EqualTo(
                "http://example.com/a/caf%C3%A9~" +
                "?query=%E6%9D%B1%E4%BA%AC"));
            Assert.That(parsed.Kind, Is.EqualTo(UriReferenceKind.Uri));
            Assert.That(parsed.Scheme, Is.EqualTo("http"));
            Assert.That(parsed.Authority.Host, Is.EqualTo("example.com"));
            Assert.That(parsed.Path, Is.EqualTo("/a/caf%C3%A9~"));
        });
    }

    [Test]
    public void ExpansionFailuresRetainTheirDiagnosticAcrossEveryProcessorPath()
    {
        string malformed = new string('\ud800', 1);
        UriTemplate template = ParseTemplate("{value}");
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            ScalarBinding("value", malformed)
        };
        IUriTemplateExpander expander = new UriTemplateExpander();
        IUriTemplateProcessor processor = new UriTemplateProcessor();
        UriTemplateExpansionResult expected = expander.TryExpandTemplate(
            template,
            bindings,
            new UriTemplateExpansionOptions());
        UriNormalizationOptions invalidNormalization =
            new UriNormalizationOptions();
        invalidNormalization.Profile = new UriSchemeProfile();

        UriTemplateExpansionException parse = Assert.Throws<
            UriTemplateExpansionException>(() => processor.ExpandToUri(
                template,
                bindings,
                new UriTemplateExpansionOptions(),
                new UriParseOptions()))!;
        UriTemplateExpansionException resolve = Assert.Throws<
            UriTemplateExpansionException>(() => processor.ExpandAndResolve(
                template,
                bindings,
                "relative/base",
                new UriTemplateExpansionOptions(),
                new UriResolutionOptions()))!;
        UriTemplateExpansionException normalize = Assert.Throws<
            UriTemplateExpansionException>(() =>
                processor.ExpandAndNormalize(
                    template,
                    bindings,
                    new UriTemplateExpansionOptions(),
                    invalidNormalization))!;

        Assert.That(expected.Success, Is.False);
        Assert.That(expected.Diagnostic, Is.Not.Null);
        AssertSameDiagnostic(expected.Diagnostic!, parse.Diagnostic);
        AssertSameDiagnostic(expected.Diagnostic!, resolve.Diagnostic);
        AssertSameDiagnostic(expected.Diagnostic!, normalize.Diagnostic);
    }

    [Test]
    public void DownstreamFailuresKeepTheOwningOperationDiagnosticDomain()
    {
        UriTemplate template = ParseTemplate("{+value}");
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            ScalarBinding("value", "http://[bad]")
        };
        IUriTemplateExpander expander = new UriTemplateExpander();
        IUriParser uriParser = new UriParser();
        IIriParser iriParser = new IriParser();
        IUriTemplateProcessor processor = new UriTemplateProcessor();
        string expanded = expander.ExpandTemplate(
            template,
            bindings,
            new UriTemplateExpansionOptions());

        UriSyntaxException uri = Assert.Throws<UriSyntaxException>(() =>
            uriParser.ParseReference(expanded, new UriParseOptions()))!;
        IriSyntaxException iri = Assert.Throws<IriSyntaxException>(() =>
            iriParser.ParseIriReference(expanded, new IriParseOptions()))!;
        UriSyntaxException parse = Assert.Throws<UriSyntaxException>(() =>
            processor.ExpandToUri(
                template,
                bindings,
                new UriTemplateExpansionOptions(),
                new UriParseOptions()))!;
        UriResolutionException resolve = Assert.Throws<
            UriResolutionException>(() => processor.ExpandAndResolve(
                template,
                bindings,
                "http://example.com/base",
                new UriTemplateExpansionOptions(),
                new UriResolutionOptions()))!;
        UriNormalizationException normalize = Assert.Throws<
            UriNormalizationException>(() =>
                processor.ExpandAndNormalize(
                    template,
                    bindings,
                    new UriTemplateExpansionOptions(),
                    new UriNormalizationOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(expanded, Is.EqualTo("http://[bad]"));
            AssertUriFailure(uri.Diagnostic);
            Assert.That(iri.Diagnostic.Domain, Is.EqualTo(
                DiagnosticDomain.Iri));
            Assert.That(iri.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidIpv6Address));
            Assert.That(iri.Diagnostic.Component, Is.EqualTo(
                UriComponent.Host));
            AssertUriFailure(parse.Diagnostic);
            AssertUriFailure(resolve.Diagnostic);
            AssertUriFailure(normalize.Diagnostic);
        });
    }

    private static UriTemplate ParseTemplate(string text)
    {
        IUriTemplateParser parser = new UriTemplateParser();
        return parser.ParseTemplate(text, new UriTemplateParseOptions());
    }

    private static UriTemplateBinding ScalarBinding(
        string name,
        string value)
    {
        UriTemplateValue scalar = new UriTemplateValue();
        scalar.Kind = UriTemplateValueKind.Scalar;
        scalar.Scalar = value;
        return new UriTemplateBinding(name, scalar);
    }

    private static void AssertSameDiagnostic(
        UriDiagnostic expected,
        UriDiagnostic actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Domain, Is.EqualTo(expected.Domain));
            Assert.That(actual.Code, Is.EqualTo(expected.Code));
            Assert.That(actual.Severity, Is.EqualTo(expected.Severity));
            Assert.That(actual.Component, Is.EqualTo(expected.Component));
            Assert.That(actual.Range.Offset, Is.EqualTo(
                expected.Range.Offset));
            Assert.That(actual.Range.Length, Is.EqualTo(
                expected.Range.Length));
            Assert.That(actual.Message, Is.EqualTo(expected.Message));
        });
    }

    private static void AssertUriFailure(UriDiagnostic diagnostic)
    {
        Assert.That(diagnostic.Domain, Is.EqualTo(DiagnosticDomain.Uri));
        Assert.That(diagnostic.Code, Is.EqualTo(
            UriDiagnosticCode.InvalidIpv6Address));
        Assert.That(diagnostic.Component, Is.EqualTo(UriComponent.Host));
    }
}
