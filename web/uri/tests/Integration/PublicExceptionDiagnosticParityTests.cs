namespace uri.Tests;

public class PublicExceptionDiagnosticParityTests
{
    [Test]
    public void IriConversionExceptionMatchesConversionResult()
    {
        IIriConverter converter = new IriConverter();
        IriConversionOptions options = new IriConversionOptions();
        string malformed = new string('\ud800', 1);

        UriTextResult result = converter.TryConvertIriToUri(
            malformed,
            options);
        IriConversionException exception = Assert.Throws<
            IriConversionException>(() => converter.ConvertIriToUri(
                malformed,
                options))!;

        AssertFailure(result.Success, result.Diagnostic);
        AssertDiagnosticParity(
            result.Diagnostic!,
            exception.Diagnostic,
            exception.Message);
    }

    [Test]
    public void IriSyntaxExceptionMatchesParseResult()
    {
        IIriParser parser = new IriParser();
        IriParseOptions options = new IriParseOptions();
        const string text = "https://example.test/%GG";

        IriParseResult result = parser.TryParseIriReference(text, options);
        IriSyntaxException exception = Assert.Throws<IriSyntaxException>(
            () => parser.ParseIriReference(text, options))!;

        AssertFailure(result.Success, result.Diagnostic);
        AssertDiagnosticParity(
            result.Diagnostic!,
            exception.Diagnostic,
            exception.Message);
    }

    [Test]
    public void PercentEncodingExceptionMatchesValidationDiagnostic()
    {
        IPercentCodec codec = new PercentCodec();
        PercentCodecOptions options = new PercentCodecOptions();

        UriDiagnostic? diagnostic = codec.ValidatePercentEncoding(
            "%GG",
            options);
        PercentEncodingException exception = Assert.Throws<
            PercentEncodingException>(() => codec.NormalizePercentEncoding(
                "%GG",
                options))!;

        Assert.That(diagnostic, Is.Not.Null);
        AssertDiagnosticParity(
            diagnostic!,
            exception.Diagnostic,
            exception.Message);
    }

    [Test]
    public void UriNormalizationExceptionMatchesValidationResult()
    {
        IUriValidator validator = new UriValidator();
        IUriNormalizer normalizer = new UriNormalizer();
        const string text = "x:path%GG";

        UriValidationResult result = validator.ValidateReference(
            text,
            new UriValidationOptions());
        UriNormalizationException exception = Assert.Throws<
            UriNormalizationException>(() => normalizer.NormalizeSyntax(
                text,
                new UriNormalizationOptions()))!;

        AssertFailure(result.Success, result.Diagnostic);
        AssertDiagnosticParity(
            result.Diagnostic!,
            exception.Diagnostic,
            exception.Message);
    }

    [Test]
    public void UriResolutionExceptionMatchesResolutionResult()
    {
        IUriResolver resolver = new UriResolver();
        UriResolutionOptions options = new UriResolutionOptions();

        UriTextResult result = resolver.TryResolveReference(
            "relative/base",
            "target",
            options);
        UriResolutionException exception = Assert.Throws<
            UriResolutionException>(() => resolver.ResolveReference(
                "relative/base",
                "target",
                options))!;

        AssertFailure(result.Success, result.Diagnostic);
        AssertDiagnosticParity(
            result.Diagnostic!,
            exception.Diagnostic,
            exception.Message);
    }

    [Test]
    public void UriSyntaxExceptionMatchesParseResult()
    {
        IUriParser parser = new UriParser();
        UriParseOptions options = new UriParseOptions();
        const string text = "x:path%GG";

        UriParseResult result = parser.TryParseReference(text, options);
        UriSyntaxException exception = Assert.Throws<UriSyntaxException>(
            () => parser.ParseReference(text, options))!;

        AssertFailure(result.Success, result.Diagnostic);
        AssertDiagnosticParity(
            result.Diagnostic!,
            exception.Diagnostic,
            exception.Message);
    }

    [Test]
    public void UriTemplateExpansionExceptionMatchesExpansionResult()
    {
        IUriTemplateParser parser = new UriTemplateParser();
        IUriTemplateExpander expander = new UriTemplateExpander();
        UriTemplate template = parser.ParseTemplate(
            "{value}",
            new UriTemplateParseOptions());
        UriTemplateValue value = new UriTemplateValue();
        value.Kind = UriTemplateValueKind.Scalar;
        value.Scalar = new string('\ud800', 1);
        UriTemplateBinding[] bindings = new UriTemplateBinding[]
        {
            new UriTemplateBinding("value", value)
        };
        UriTemplateExpansionOptions options =
            new UriTemplateExpansionOptions();

        UriTemplateExpansionResult result = expander.TryExpandTemplate(
            template,
            bindings,
            options);
        UriTemplateExpansionException exception = Assert.Throws<
            UriTemplateExpansionException>(() => expander.ExpandTemplate(
                template,
                bindings,
                options))!;

        AssertFailure(result.Success, result.Diagnostic);
        AssertDiagnosticParity(
            result.Diagnostic!,
            exception.Diagnostic,
            exception.Message);
    }

    [Test]
    public void UriTemplateSyntaxExceptionMatchesParseResult()
    {
        IUriTemplateParser parser = new UriTemplateParser();
        UriTemplateParseOptions options = new UriTemplateParseOptions();
        const string text = "{value";

        UriTemplateParseResult result = parser.TryParseTemplate(
            text,
            options);
        UriTemplateSyntaxException exception = Assert.Throws<
            UriTemplateSyntaxException>(() => parser.ParseTemplate(
                text,
                options))!;

        AssertFailure(result.Success, result.Diagnostic);
        AssertDiagnosticParity(
            result.Diagnostic!,
            exception.Diagnostic,
            exception.Message);
    }

    [Test]
    public void UriValidationExceptionMatchesCompositionResult()
    {
        IUriParser parser = new UriParser();
        IUriComposer composer = new UriComposer();
        UriReference reference = parser.ParseReference(
            "x:path",
            new UriParseOptions());
        reference.Path = "path%GG";
        UriValidationOptions options = new UriValidationOptions();

        UriTextResult result = composer.TryComposeReference(
            reference,
            options);
        UriValidationException exception = Assert.Throws<
            UriValidationException>(() => composer.ComposeReference(
                reference,
                options))!;

        AssertFailure(result.Success, result.Diagnostic);
        AssertDiagnosticParity(
            result.Diagnostic!,
            exception.Diagnostic,
            exception.Message);
    }

    [Test]
    public void UrnEquivalenceExceptionMatchesParseResult()
    {
        IUrnParser parser = new UrnParser();
        IUrnNormalizer normalizer = new UrnNormalizer();
        UrnParseOptions options = new UrnParseOptions();
        const string text = "not-a-urn";

        UrnParseResult result = parser.TryParseUrn(text, options);
        UrnEquivalenceException exception = Assert.Throws<
            UrnEquivalenceException>(() => normalizer.NormalizeUrn(
                text,
                options))!;

        AssertFailure(result.Success, result.Diagnostic);
        AssertDiagnosticParity(
            result.Diagnostic!,
            exception.Diagnostic,
            exception.Message);
    }

    [Test]
    public void UrnSyntaxExceptionMatchesParseResult()
    {
        IUrnParser parser = new UrnParser();
        UrnParseOptions options = new UrnParseOptions();
        const string text = "urn:bad";

        UrnParseResult result = parser.TryParseUrn(text, options);
        UrnSyntaxException exception = Assert.Throws<UrnSyntaxException>(
            () => parser.ParseUrn(text, options))!;

        AssertFailure(result.Success, result.Diagnostic);
        AssertDiagnosticParity(
            result.Diagnostic!,
            exception.Diagnostic,
            exception.Message);
    }

    [Test]
    public void WebLinkResolutionExceptionMatchesResolutionResult()
    {
        IWebLinkResolver resolver = new WebLinkResolver();
        WebLink link = new WebLink();
        link.Target = "relative";

        WebLinkResolutionResult result = resolver.ResolveWebLink(
            link,
            null,
            new OperationLimits());
        Assert.That(result.Diagnostic, Is.Not.Null);
        WebLinkResolutionException exception =
            new WebLinkResolutionException(result.Diagnostic!);

        AssertFailure(result.Success, result.Diagnostic);
        AssertDiagnosticParity(
            result.Diagnostic!,
            exception.Diagnostic,
            exception.Message);
    }

    [Test]
    public void WebLinkSyntaxExceptionMatchesParseResult()
    {
        IWebLinkParser parser = new WebLinkParser();
        WebLinkParseOptions options = new WebLinkParseOptions();
        string[] fieldValues = new string[] { "<unterminated" };

        WebLinkParseResult result = parser.TryParseLinkFieldValues(
            fieldValues,
            options);
        WebLinkSyntaxException exception = Assert.Throws<
            WebLinkSyntaxException>(() => parser.ParseLinkFieldValues(
                fieldValues,
                options))!;

        AssertFailure(result.Success, result.Diagnostic);
        AssertDiagnosticParity(
            result.Diagnostic!,
            exception.Diagnostic,
            exception.Message);
    }

    private static void AssertFailure(
        bool success,
        UriDiagnostic? diagnostic)
    {
        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(diagnostic, Is.Not.Null);
        });
    }

    private static void AssertDiagnosticParity(
        UriDiagnostic expected,
        UriDiagnostic actual,
        string exceptionMessage)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Domain, Is.EqualTo(expected.Domain));
            Assert.That(actual.Code, Is.EqualTo(expected.Code));
            Assert.That(actual.Severity, Is.EqualTo(expected.Severity));
            Assert.That(actual.Component, Is.EqualTo(expected.Component));
            Assert.That(actual.Range.Offset,
                Is.EqualTo(expected.Range.Offset));
            Assert.That(actual.Range.Length,
                Is.EqualTo(expected.Range.Length));
            Assert.That(actual.Message, Is.EqualTo(expected.Message));
            Assert.That(exceptionMessage, Is.EqualTo(expected.Message));
        });
    }
}
