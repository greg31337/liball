namespace uri.Tests;

public class WebLinkScannerTests
{
    [Test]
    public void OptionalWhitespaceMatchesOnlySpaceAndHorizontalTab()
    {
        for (int value = 0; value <= 255; value++)
        {
            char character = (char)value;
            bool expected = character == ' ' || character == '\t';

            Assert.That(
                WebLinkGrammar.IsOptionalWhitespaceCharacter(character),
                Is.EqualTo(expected),
                $"HTTP OWS character {value}");
        }

        Assert.That(
            WebLinkGrammar.IsOptionalWhitespaceCharacter('\u0100'),
            Is.False);
    }

    [Test]
    public void OptionalWhitespaceScannerConsumesZeroOrMoreAtAnyBoundary()
    {
        WebLinkScanResult middle =
            WebLinkGrammar.ScanOptionalWhitespace(
                "x \t y".AsSpan(),
                1,
                50);
        WebLinkScanResult none =
            WebLinkGrammar.ScanOptionalWhitespace(
                "value".AsSpan(),
                0,
                20);
        WebLinkScanResult end =
            WebLinkGrammar.ScanOptionalWhitespace(
                "value".AsSpan(),
                5,
                20);

        AssertSuccess(middle, " \t ", 1, 4, 51, 3);
        AssertSuccess(none, "", 0, 0, 20, 0);
        AssertSuccess(end, "", 5, 5, 25, 0);
    }

    [Test]
    public void TokenGrammarMatchesTheCompleteHttpTcharSet()
    {
        const string punctuation = "!#$%&'*+-.^_`|~";
        for (int value = 0; value <= 255; value++)
        {
            char character = (char)value;
            bool expected = character >= 'A' && character <= 'Z' ||
                character >= 'a' && character <= 'z' ||
                character >= '0' && character <= '9' ||
                punctuation.IndexOf(character) >= 0;

            Assert.That(
                WebLinkGrammar.IsTokenCharacter(character),
                Is.EqualTo(expected),
                $"HTTP tchar {value}");
        }

        Assert.That(WebLinkGrammar.IsTokenCharacter('\u0100'), Is.False);
    }

    [Test]
    public void TokenScannerReturnsTheMaximalTokenWithoutConsumingDelimiters()
    {
        const string allTokenCharacters =
            "!#$%&'*+-.^_`|~AZaz09";
        string text = "x" + allTokenCharacters + "=remaining";

        WebLinkScanResult result = WebLinkGrammar.ScanToken(
            text.AsSpan(),
            1,
            100);

        AssertSuccess(
            result,
            allTokenCharacters,
            1,
            1 + allTokenCharacters.Length,
            101,
            allTokenCharacters.Length);
        Assert.That(text[result.NextOffset], Is.EqualTo('='));
    }

    [TestCase("", 0, 10, 10, 0)]
    [TestCase(" name", 0, 10, 10, 1)]
    [TestCase("=value", 0, 10, 10, 1)]
    [TestCase("name", 4, 10, 14, 0)]
    public void TokenScannerReportsTheExactMissingOrInvalidFirstCharacter(
        string text,
        int offset,
        int baseOffset,
        int expectedErrorOffset,
        int expectedErrorLength)
    {
        WebLinkScanResult result = WebLinkGrammar.ScanToken(
            text.AsSpan(),
            offset,
            baseOffset);

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidLinkParameter,
            UriComponent.LinkParameter,
            expectedErrorOffset,
            expectedErrorLength,
            WebLinkGrammar.InvalidTokenMessage);
    }

    [Test]
    public void QuotedTextAndQuotedPairPredicatesMatchHttpFieldByteRanges()
    {
        for (int value = 0; value <= 256; value++)
        {
            char character = (char)value;
            bool expectedText = character == '\t' ||
                character == ' ' ||
                character == '!' ||
                character >= '#' && character <= '[' ||
                character >= ']' && character <= '~' ||
                character >= '\u0080' && character <= '\u00ff';
            bool expectedPair = character == '\t' ||
                character >= ' ' && character <= '~' ||
                character >= '\u0080' && character <= '\u00ff';

            Assert.That(
                HttpQuotedString.IsQuotedTextCharacter(character),
                Is.EqualTo(expectedText),
                $"HTTP qdtext {value}");
            Assert.That(
                HttpQuotedString.IsQuotedPairCharacter(character),
                Is.EqualTo(expectedPair),
                $"HTTP quoted-pair {value}");
        }
    }

    [TestCase("x\\ y", 1, " ", 3)]
    [TestCase("x\\\"y", 1, "\"", 3)]
    [TestCase("x\\\\y", 1, "\\", 3)]
    [TestCase("x\\\ty", 1, "\t", 3)]
    [TestCase("x\\\u0080y", 1, "\u0080", 3)]
    public void QuotedPairScannerUnescapesEveryAllowedCharacterClass(
        string text,
        int offset,
        string expectedValue,
        int expectedNextOffset)
    {
        WebLinkScanResult result = HttpQuotedString.ScanQuotedPair(
            text.AsSpan(),
            offset,
            30);

        AssertSuccess(
            result,
            expectedValue,
            offset,
            expectedNextOffset,
            30 + offset,
            2);
    }

    [TestCase("x", 0, 40, 40, 1)]
    [TestCase("\\", 0, 40, 41, 0)]
    [TestCase("\\\r", 0, 40, 41, 1)]
    [TestCase("\\\u0100", 0, 40, 41, 1)]
    public void QuotedPairScannerReportsMarkerEndAndEscapedCharacterFailures(
        string text,
        int offset,
        int baseOffset,
        int expectedErrorOffset,
        int expectedErrorLength)
    {
        WebLinkScanResult result = HttpQuotedString.ScanQuotedPair(
            text.AsSpan(),
            offset,
            baseOffset);

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidLinkParameter,
            UriComponent.LinkParameter,
            expectedErrorOffset,
            expectedErrorLength,
            HttpQuotedString.InvalidQuotedPairMessage);
    }

    [Test]
    public void QuotedStringScannerUnescapesPairsAndStopsAtClosingQuote()
    {
        const string text = "x\"a\\b\\\"c\\\t\u0080\"tail";

        WebLinkScanResult result = HttpQuotedString.ScanQuotedString(
            text.AsSpan(),
            1,
            70);

        AssertSuccess(
            result,
            "ab\"c\t\u0080",
            1,
            12,
            71,
            11);
        Assert.That(text.Substring(result.NextOffset), Is.EqualTo("tail"));
    }

    [Test]
    public void QuotedStringScannerAcceptsEmptyAndAllUnescapedBoundaries()
    {
        WebLinkScanResult empty = HttpQuotedString.ScanQuotedString(
            "\"\"".AsSpan(),
            0,
            5);
        WebLinkScanResult boundaries = HttpQuotedString.ScanQuotedString(
            "\"\t !#[]~\u0080\u00ff\"".AsSpan(),
            0,
            5);

        AssertSuccess(empty, "", 0, 2, 5, 2);
        AssertSuccess(
            boundaries,
            "\t !#[]~\u0080\u00ff",
            0,
            11,
            5,
            11);
    }

    [TestCase("plain", 0, 80, 80, 1)]
    [TestCase("\"unterminated", 0, 80, 93, 0)]
    [TestCase("\"bad\rvalue\"", 0, 80, 84, 1)]
    [TestCase("\"bad\u0100value\"", 0, 80, 84, 1)]
    [TestCase("\"bad\\\"", 0, 80, 86, 0)]
    public void QuotedStringScannerReportsExactOpeningContentOrEndFailure(
        string text,
        int offset,
        int baseOffset,
        int expectedErrorOffset,
        int expectedErrorLength)
    {
        WebLinkScanResult result = HttpQuotedString.ScanQuotedString(
            text.AsSpan(),
            offset,
            baseOffset);

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidLinkParameter,
            UriComponent.LinkParameter,
            expectedErrorOffset,
            expectedErrorLength,
            result.Diagnostic!.Message);
        Assert.That(
            result.Diagnostic.Message,
            Is.EqualTo(
                text == "\"bad\\" ?
                HttpQuotedString.InvalidQuotedPairMessage :
                HttpQuotedString.InvalidQuotedStringMessage));
    }

    [Test]
    public void AngleTargetScannerPreservesCommasSemicolonsAndEmptyTargets()
    {
        const string text =
            "x<https://example.test/a,b;c?x=\"y\">; rel=next";
        int closingOffset = text.IndexOf('>');
        WebLinkScanResult target = WebLinkGrammar.ScanAngleTarget(
            text.AsSpan(),
            1,
            200);
        WebLinkScanResult empty = WebLinkGrammar.ScanAngleTarget(
            "<>tail".AsSpan(),
            0,
            9);

        AssertSuccess(
            target,
            "https://example.test/a,b;c?x=\"y\"",
            1,
            closingOffset + 1,
            201,
            closingOffset);
        Assert.That(text[target.NextOffset], Is.EqualTo(';'));
        AssertSuccess(empty, "", 0, 2, 9, 2);
    }

    [TestCase("target", 0, 15, 15, 1)]
    [TestCase("", 0, 15, 15, 0)]
    [TestCase("<target", 0, 15, 22, 0)]
    [TestCase("x<target", 1, 15, 23, 0)]
    public void AngleTargetScannerReportsMissingOpeningOrClosingDelimiter(
        string text,
        int offset,
        int baseOffset,
        int expectedErrorOffset,
        int expectedErrorLength)
    {
        WebLinkScanResult result = WebLinkGrammar.ScanAngleTarget(
            text.AsSpan(),
            offset,
            baseOffset);

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidLinkTarget,
            UriComponent.LinkTarget,
            expectedErrorOffset,
            expectedErrorLength,
            WebLinkGrammar.InvalidAngleTargetMessage);
    }

    [Test]
    public void SemicolonAndCommaScannersConsumeExactlyOneDelimiter()
    {
        WebLinkScanResult semicolon = WebLinkGrammar.ScanSemicolon(
            "x; next".AsSpan(),
            1,
            20);
        WebLinkScanResult comma = WebLinkGrammar.ScanComma(
            "x, next".AsSpan(),
            1,
            20);

        AssertSuccess(semicolon, ";", 1, 2, 21, 1);
        AssertSuccess(comma, ",", 1, 2, 21, 1);
    }

    [Test]
    public void DelimiterScannersReportWrongCharacterAndEndSeparately()
    {
        WebLinkScanResult semicolon = WebLinkGrammar.ScanSemicolon(
            ",".AsSpan(),
            0,
            90);
        WebLinkScanResult comma = WebLinkGrammar.ScanComma(
            "".AsSpan(),
            0,
            90);

        AssertFailure(
            semicolon,
            UriDiagnosticCode.InvalidWebLink,
            UriComponent.WebLink,
            90,
            1,
            WebLinkGrammar.ExpectedSemicolonMessage);
        AssertFailure(
            comma,
            UriDiagnosticCode.InvalidWebLink,
            UriComponent.WebLink,
            90,
            0,
            WebLinkGrammar.ExpectedCommaMessage);
    }

    [Test]
    public void EveryScannerRejectsOutOfRangeRelativeOffsets()
    {
        WebLinkScanResult whitespace =
            WebLinkGrammar.ScanOptionalWhitespace("x".AsSpan(), -1, 10);
        WebLinkScanResult token =
            WebLinkGrammar.ScanToken("x".AsSpan(), 2, 10);
        WebLinkScanResult target =
            WebLinkGrammar.ScanAngleTarget("<>".AsSpan(), 3, 10);
        WebLinkScanResult semicolon =
            WebLinkGrammar.ScanSemicolon(";".AsSpan(), -1, 10);
        WebLinkScanResult comma =
            WebLinkGrammar.ScanComma(",".AsSpan(), 2, 10);
        WebLinkScanResult pair =
            HttpQuotedString.ScanQuotedPair("\\x".AsSpan(), -1, 10);
        WebLinkScanResult quoted =
            HttpQuotedString.ScanQuotedString("\"x\"".AsSpan(), 4, 10);

        AssertInvalidInput(whitespace);
        AssertInvalidInput(token);
        AssertInvalidInput(target);
        AssertInvalidInput(semicolon);
        AssertInvalidInput(comma);
        AssertInvalidInput(pair);
        AssertInvalidInput(quoted);
    }

    [Test]
    public void AbsoluteOffsetsUseRelativeFallbackAndSaturateOverflow()
    {
        WebLinkScanResult relative = WebLinkGrammar.ScanToken(
            "x name".AsSpan(),
            2,
            -1);
        WebLinkScanResult saturatedSuccess = WebLinkGrammar.ScanToken(
            "x name".AsSpan(),
            2,
            int.MaxValue);
        WebLinkScanResult saturatedFailure = WebLinkGrammar.ScanToken(
            "x =".AsSpan(),
            2,
            int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(relative.Range.Offset, Is.EqualTo(2));
            Assert.That(saturatedSuccess.Range.Offset,
                Is.EqualTo(int.MaxValue));
            Assert.That(saturatedFailure.Diagnostic!.Range.Offset,
                Is.EqualTo(int.MaxValue));
        });
    }

    private static void AssertSuccess(
        WebLinkScanResult result,
        string value,
        int startOffset,
        int nextOffset,
        int absoluteOffset,
        int length)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Value, Is.EqualTo(value));
            Assert.That(result.StartOffset, Is.EqualTo(startOffset));
            Assert.That(result.NextOffset, Is.EqualTo(nextOffset));
            Assert.That(result.Range.Offset, Is.EqualTo(absoluteOffset));
            Assert.That(result.Range.Length, Is.EqualTo(length));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    private static void AssertFailure(
        WebLinkScanResult result,
        UriDiagnosticCode code,
        UriComponent component,
        int errorOffset,
        int errorLength,
        string message)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Value, Is.Empty);
            Assert.That(result.NextOffset, Is.EqualTo(result.StartOffset));
            Assert.That(result.Diagnostic!.Domain,
                Is.EqualTo(DiagnosticDomain.WebLink));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Severity,
                Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(result.Diagnostic.Component,
                Is.EqualTo(component));
            Assert.That(result.Diagnostic.Range.Offset,
                Is.EqualTo(errorOffset));
            Assert.That(result.Diagnostic.Range.Length,
                Is.EqualTo(errorLength));
            Assert.That(result.Diagnostic.Message, Is.EqualTo(message));
        });
    }

    private static void AssertInvalidInput(WebLinkScanResult result)
    {
        AssertFailure(
            result,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input,
            0,
            0,
            DiagnosticFactory.InvalidInputMessage);
    }
}
