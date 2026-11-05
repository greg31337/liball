namespace uri.Tests;

public class UriParserPublicApiCompleteTests
{
    [TestCase("")]
    [TestCase("?")]
    [TestCase("#")]
    [TestCase("//host/path")]
    [TestCase("/path")]
    [TestCase("path")]
    [TestCase("x:path")]
    [TestCase("x://user@[2001:db8::1]:080/a//b/?q=%23#f")]
    public void ParseReferenceAndTryParseReferenceAgreeOnEveryReferenceForm(string text)
    {
        IUriParser parser = new UriParser();
        UriParseOptions options = new UriParseOptions();

        UriReference throwing = parser.ParseReference(text, options);
        UriParseResult result = parser.TryParseReference(text, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, text);
            Assert.That(result.Diagnostic, Is.Null, text);
            Assert.That(result.Reference.Kind, Is.EqualTo(throwing.Kind), text);
            Assert.That(result.Reference.OriginalText, Is.EqualTo(text), text);
            Assert.That(throwing.OriginalText, Is.EqualTo(text), text);
        });
    }

    [Test]
    public void TryParseReferenceSpanUsesOnlyTheProvidedSlice()
    {
        const string expected = "x://host/path?query#fragment";
        string padded = "before" + expected + "after";
        IUriParser parser = new UriParser();

        UriParseResult result = parser.TryParseReferenceSpan(
            padded.AsSpan(6, expected.Length),
            new UriParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Reference.OriginalText, Is.EqualTo(expected));
            Assert.That(result.Reference.Authority.Host, Is.EqualTo("host"));
            Assert.That(result.Reference.Path, Is.EqualTo("/path"));
            Assert.That(result.Reference.Query, Is.EqualTo("query"));
            Assert.That(result.Reference.Fragment, Is.EqualTo("fragment"));
        });
    }

    [Test]
    public void EveryReferenceResultEntryPointReturnsCompleteSyntaxDiagnosticFields()
    {
        IUriParser parser = new UriParser();
        UriParseOptions options = new UriParseOptions();

        UriParseResult text = parser.TryParseReference("x:path%GG", options);
        UriParseResult span = parser.TryParseReferenceSpan("x:path%GG".AsSpan(), options);
        UriParseResult absolute = parser.TryParseAbsoluteUri("x:path#fragment", options);
        UriParseResult relative = parser.TryParseRelativeReference("x:path", options);

        Assert.Multiple(() =>
        {
            AssertPercentFailure(text.Diagnostic, UriComponent.Path, 7);
            AssertPercentFailure(span.Diagnostic, UriComponent.Path, 7);
            AssertDiagnostic(
                absolute.Diagnostic,
                UriDiagnosticCode.InvalidFragment,
                UriComponent.Fragment,
                6,
                1,
                DiagnosticFactory.InvalidFragmentMessage);
            AssertDiagnostic(
                relative.Diagnostic,
                UriDiagnosticCode.InvalidPath,
                UriComponent.Path,
                1,
                1,
                DiagnosticFactory.InvalidPathMessage);
        });
    }

    [Test]
    public void ThrowingParserMethodsRetainTheExactResultDiagnostic()
    {
        IUriParser parser = new UriParser();
        UriParseOptions options = new UriParseOptions();
        UriParseResult referenceResult = parser.TryParseReference("x:path%GG", options);
        UriParseResult absoluteResult = parser.TryParseAbsoluteUri("x:path#fragment", options);

        UriSyntaxException referenceException = Assert.Throws<UriSyntaxException>(() =>
            parser.ParseReference("x:path%GG", options))!;
        UriSyntaxException absoluteException = Assert.Throws<UriSyntaxException>(() =>
            parser.ParseAbsoluteUri("x:path#fragment", options))!;

        Assert.Multiple(() =>
        {
            Assert.That(referenceException.Diagnostic.Code, Is.EqualTo(referenceResult.Diagnostic!.Code));
            Assert.That(referenceException.Diagnostic.Component, Is.EqualTo(referenceResult.Diagnostic.Component));
            Assert.That(referenceException.Diagnostic.Range.Offset, Is.EqualTo(referenceResult.Diagnostic.Range.Offset));
            Assert.That(referenceException.Diagnostic.Range.Length, Is.EqualTo(referenceResult.Diagnostic.Range.Length));
            Assert.That(referenceException.Message, Is.EqualTo(referenceException.Diagnostic.Message));
            Assert.That(absoluteException.Diagnostic.Code, Is.EqualTo(absoluteResult.Diagnostic!.Code));
            Assert.That(absoluteException.Diagnostic.Component, Is.EqualTo(absoluteResult.Diagnostic.Component));
            Assert.That(absoluteException.Message, Is.EqualTo(absoluteException.Diagnostic.Message));
        });
    }

    [Test]
    public void InputLimitExactBoundaryAppliesToEveryParserEntryPoint()
    {
        IUriParser parser = new UriParser();
        UriParseOptions exact = new UriParseOptions();
        exact.Limits.MaxInputCharacters = 4;
        UriParseOptions shortLimit = new UriParseOptions();
        shortLimit.Limits.MaxInputCharacters = 3;

        UriReference throwingReference = parser.ParseReference("x:ab", exact);
        UriParseResult reference = parser.TryParseReference("x:ab", shortLimit);
        UriParseResult span = parser.TryParseReferenceSpan("x:ab".AsSpan(), shortLimit);
        UriReference throwingAbsolute = parser.ParseAbsoluteUri("x:ab", exact);
        UriParseResult absolute = parser.TryParseAbsoluteUri("x:ab", shortLimit);
        UriParseResult relative = parser.TryParseRelativeReference("path", shortLimit);

        Assert.Multiple(() =>
        {
            Assert.That(throwingReference.OriginalText, Is.EqualTo("x:ab"));
            Assert.That(throwingAbsolute.OriginalText, Is.EqualTo("x:ab"));
            AssertInputLimit(reference.Diagnostic, 3, 1);
            AssertInputLimit(span.Diagnostic, 3, 1);
            AssertInputLimit(absolute.Diagnostic, 3, 1);
            AssertInputLimit(relative.Diagnostic, 3, 1);
        });
    }

    [Test]
    public void NullOptionsAndLimitsFailEveryParserResultEntryPoint()
    {
        IUriParser parser = new UriParser();
        UriParseOptions noLimits = new UriParseOptions();
        noLimits.Limits = null!;

        UriParseResult referenceOptions = parser.TryParseReference("x:a", null!);
        UriParseResult referenceLimits = parser.TryParseReference("x:a", noLimits);
        UriParseResult spanOptions = parser.TryParseReferenceSpan("x:a".AsSpan(), null!);
        UriParseResult spanLimits = parser.TryParseReferenceSpan("x:a".AsSpan(), noLimits);
        UriParseResult absoluteOptions = parser.TryParseAbsoluteUri("x:a", null!);
        UriParseResult absoluteLimits = parser.TryParseAbsoluteUri("x:a", noLimits);
        UriParseResult relativeOptions = parser.TryParseRelativeReference("path", null!);
        UriParseResult relativeLimits = parser.TryParseRelativeReference("path", noLimits);

        Assert.Multiple(() =>
        {
            AssertInvalidInput(referenceOptions.Diagnostic);
            AssertInvalidInput(referenceLimits.Diagnostic);
            AssertInvalidInput(spanOptions.Diagnostic);
            AssertInvalidInput(spanLimits.Diagnostic);
            AssertInvalidInput(absoluteOptions.Diagnostic);
            AssertInvalidInput(absoluteLimits.Diagnostic);
            AssertInvalidInput(relativeOptions.Diagnostic);
            AssertInvalidInput(relativeLimits.Diagnostic);
        });
    }

    [Test]
    public void NullTextFailsEveryStringParserEntryPoint()
    {
        IUriParser parser = new UriParser();
        UriParseOptions options = new UriParseOptions();

        UriParseResult reference = parser.TryParseReference(null!, options);
        UriParseResult absolute = parser.TryParseAbsoluteUri(null!, options);
        UriParseResult relative = parser.TryParseRelativeReference(null!, options);

        Assert.Multiple(() =>
        {
            AssertInvalidInput(reference.Diagnostic);
            AssertInvalidInput(absolute.Diagnostic);
            AssertInvalidInput(relative.Diagnostic);
        });
        Assert.Throws<UriSyntaxException>(() => parser.ParseReference(null!, options));
        Assert.Throws<UriSyntaxException>(() => parser.ParseAbsoluteUri(null!, options));
    }

    [Test]
    public void NegativeInputLimitProducesDeterministicFailures()
    {
        IUriParser parser = new UriParser();
        UriParseOptions options = new UriParseOptions();
        options.Limits.MaxInputCharacters = -1;

        UriParseResult reference = parser.TryParseReference("", options);
        UriParseResult absolute = parser.TryParseAbsoluteUri("x:", options);
        UriParseResult relative = parser.TryParseRelativeReference("", options);

        Assert.Multiple(() =>
        {
            AssertInputLimit(reference.Diagnostic, 0, 0);
            AssertInputLimit(absolute.Diagnostic, 0, 0);
            AssertInputLimit(relative.Diagnostic, 0, 0);
        });
    }

    private static void AssertPercentFailure(
        UriDiagnostic? diagnostic,
        UriComponent component,
        int offset)
    {
        AssertDiagnostic(
            diagnostic,
            UriDiagnosticCode.InvalidPercentEncoding,
            component,
            offset,
            1,
            DiagnosticFactory.InvalidPercentEncodingMessage);
    }

    private static void AssertInputLimit(UriDiagnostic? diagnostic, int offset, int length)
    {
        AssertDiagnostic(
            diagnostic,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Input,
            offset,
            length,
            LimitGuard.InputLimitMessage);
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
