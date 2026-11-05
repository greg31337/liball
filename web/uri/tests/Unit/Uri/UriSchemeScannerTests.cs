namespace uri.Tests;

public class UriSchemeScannerTests
{
    [Test]
    public void SchemeGrammarMatchesExactAsciiProductions()
    {
        for (int value = 0; value <= 127; value++)
        {
            char character = (char)value;
            bool expectedStart = character >= 'A' && character <= 'Z' ||
                character >= 'a' && character <= 'z';
            bool expectedContinuation = expectedStart ||
                character >= '0' && character <= '9' ||
                character == '+' ||
                character == '-' ||
                character == '.';

            Assert.That(
                UriGrammar.IsSchemeStartCharacter(character),
                Is.EqualTo(expectedStart),
                $"scheme start ASCII {value}");
            Assert.That(
                UriGrammar.IsSchemeCharacter(character),
                Is.EqualTo(expectedContinuation),
                $"scheme continuation ASCII {value}");
        }

        Assert.Multiple(() =>
        {
            Assert.That(UriGrammar.IsSchemeStartCharacter('\u00e4'), Is.False);
            Assert.That(UriGrammar.IsSchemeCharacter('\u00e4'), Is.False);
        });
    }

    [TestCase("a:", 1, 2)]
    [TestCase("HTTP:", 4, 5)]
    [TestCase("x+1.2-3:", 7, 8)]
    [TestCase("z:remaining:text", 1, 2)]
    [TestCase("file:/path", 4, 5)]
    public void OptionalScannerFindsAValidLeadingScheme(
        string text,
        int expectedLength,
        int expectedNextOffset)
    {
        SchemeScanResult result = UriParserCore.ScanOptionalScheme(text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.HasScheme, Is.True);
            Assert.That(result.SchemeLength, Is.EqualTo(expectedLength));
            Assert.That(result.NextOffset, Is.EqualTo(expectedNextOffset));
            Assert.That(text.Substring(0, result.SchemeLength), Does.Not.Contain(":"));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [TestCase("")]
    [TestCase(":x")]
    [TestCase("1abc:x")]
    [TestCase("+abc:x")]
    [TestCase("a_b:x")]
    [TestCase("a/b:c")]
    [TestCase("a?b:c")]
    [TestCase("a#b:c")]
    [TestCase("a%3Ab")]
    [TestCase("scheme")]
    [TestCase("\u00e4:x")]
    public void OptionalScannerLeavesNonSchemePrefixesForRelativeReferenceParsing(string text)
    {
        SchemeScanResult result = UriParserCore.ScanOptionalScheme(text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.HasScheme, Is.False);
            Assert.That(result.SchemeLength, Is.Zero);
            Assert.That(result.NextOffset, Is.Zero);
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [TestCase("a:", 1, 2)]
    [TestCase("HTTP:", 4, 5)]
    [TestCase("x+1.2-3:/path", 7, 8)]
    public void RequiredScannerReturnsTheSameSuccessfulBoundary(
        string text,
        int expectedLength,
        int expectedNextOffset)
    {
        SchemeScanResult result = UriParserCore.ScanRequiredScheme(text.AsSpan(), 20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.HasScheme, Is.True);
            Assert.That(result.SchemeLength, Is.EqualTo(expectedLength));
            Assert.That(result.NextOffset, Is.EqualTo(expectedNextOffset));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [TestCase("", 30, 0)]
    [TestCase("1abc:", 30, 1)]
    [TestCase(":", 30, 1)]
    [TestCase("ab_cd:", 32, 1)]
    [TestCase("ab/cd", 32, 1)]
    [TestCase("scheme", 36, 0)]
    public void RequiredScannerReportsTheExactInvalidCharacterOrMissingDelimiter(
        string text,
        int expectedOffset,
        int expectedLength)
    {
        SchemeScanResult result = UriParserCore.ScanRequiredScheme(text.AsSpan(), 30);

        Assert.That(result.Success, Is.False);
        Assert.That(result.HasScheme, Is.False);
        Assert.That(result.SchemeLength, Is.Zero);
        Assert.That(result.NextOffset, Is.Zero);
        AssertInvalidScheme(result.Diagnostic, expectedOffset, expectedLength);
    }

    [Test]
    public void RequiredScannerUsesRelativeOffsetsForNegativeBaseAndSaturatesOverflow()
    {
        SchemeScanResult relative = UriParserCore.ScanRequiredScheme("a_b:".AsSpan(), -1);
        SchemeScanResult saturated = UriParserCore.ScanRequiredScheme(
            "ab_cd:".AsSpan(),
            int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(relative.Diagnostic!.Range.Offset, Is.EqualTo(1));
            Assert.That(saturated.Diagnostic!.Range.Offset, Is.EqualTo(int.MaxValue));
        });
    }

    [TestCase("a:")]
    [TestCase("a:b")]
    [TestCase("a:/path")]
    [TestCase("a://host")]
    [TestCase("a:?query")]
    [TestCase("a:#fragment")]
    [TestCase("a:%GG")]
    [TestCase("C:/windows-like")]
    public void ValidSchemePrefixCommitsToUriBeforeTheRemainderIsParsed(string text)
    {
        UriReferenceKind kind = UriParserCore.ClassifyReference(text.AsSpan());

        Assert.That(kind, Is.EqualTo(UriReferenceKind.Uri));
    }

    [TestCase("", UriReferenceKind.EmptyReference)]
    [TestCase("#fragment", UriReferenceKind.SameDocumentReference)]
    [TestCase("//host/path", UriReferenceKind.NetworkPathReference)]
    [TestCase("/path", UriReferenceKind.AbsolutePathReference)]
    [TestCase("?query", UriReferenceKind.RelativeReference)]
    [TestCase("path", UriReferenceKind.RelativePathReference)]
    [TestCase("a/b:c", UriReferenceKind.RelativePathReference)]
    [TestCase("a%3Ab", UriReferenceKind.RelativePathReference)]
    [TestCase("1abc:x", UriReferenceKind.RelativePathReference)]
    public void NonSchemeInputsReceiveTheMatchingRelativeReferenceClassification(
        string text,
        UriReferenceKind expected)
    {
        UriReferenceKind kind = UriParserCore.ClassifyReference(text.AsSpan());

        Assert.That(kind, Is.EqualTo(expected));
    }

    private static void AssertInvalidScheme(
        UriDiagnostic? diagnostic,
        int expectedOffset,
        int expectedLength)
    {
        Assert.That(diagnostic, Is.Not.Null);
        Assert.That(diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Uri));
        Assert.That(diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidScheme));
        Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
        Assert.That(diagnostic.Component, Is.EqualTo(UriComponent.Scheme));
        Assert.That(diagnostic.Range.Offset, Is.EqualTo(expectedOffset));
        Assert.That(diagnostic.Range.Length, Is.EqualTo(expectedLength));
        Assert.That(diagnostic.Message, Is.EqualTo(DiagnosticFactory.InvalidSchemeMessage));
    }
}
