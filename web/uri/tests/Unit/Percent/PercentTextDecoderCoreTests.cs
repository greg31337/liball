namespace uri.Tests;

public class PercentTextDecoderCoreTests
{
    [Test]
    public void StrictModeDecodesAsciiAndEveryUtf8ScalarLength()
    {
        PercentTextDecodeResult result = PercentDecoderCore.DecodeTextStrict(
            "A%00%C2%A2%E2%82%AC%F0%9f%98%80",
            20,
            UriComponent.Path,
            7);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo("A\0\u00a2\u20ac\ud83d\ude00"));
            Assert.That(result.UsedReplacement, Is.False);
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void StrictTextAndSpanEntryPointsProduceIdenticalResults()
    {
        string input = "plain%20%C3%A4%2Ftext";

        PercentTextDecodeResult fromText = PercentDecoderCore.DecodeTextStrict(
            input,
            100,
            UriComponent.Query,
            4);
        PercentTextDecodeResult fromSpan = PercentDecoderCore.DecodeTextSpanStrict(
            input.AsSpan(),
            100,
            UriComponent.Query,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(fromText.Success, Is.True);
            Assert.That(fromSpan.Success, Is.True);
            Assert.That(fromText.Text, Is.EqualTo("plain \u00e4/text"));
            Assert.That(fromSpan.Text, Is.EqualTo(fromText.Text));
        });
    }

    [TestCase("x%80", 11, 3)]
    [TestCase("x%C0%AF", 11, 3)]
    [TestCase("x%ED%A0%80", 14, 3)]
    [TestCase("x%F4%90%80%80", 14, 3)]
    [TestCase("x%E2%28", 14, 3)]
    [TestCase("x%E2A", 14, 1)]
    [TestCase("x%E2%82", 17, 0)]
    public void StrictModeReportsExactSourceRangeForInvalidUtf8(
        string input,
        int expectedOffset,
        int expectedLength)
    {
        PercentTextDecodeResult result = PercentDecoderCore.DecodeTextStrict(
            input,
            100,
            UriComponent.Fragment,
            10);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Text, Is.Empty);
        Assert.That(result.UsedReplacement, Is.False);
        AssertInvalidDiagnostic(
            result.Diagnostic,
            UriDiagnosticCode.InvalidUtf8,
            UriComponent.Fragment,
            expectedOffset,
            expectedLength,
            DiagnosticFactory.InvalidUtf8Message);
    }

    [Test]
    public void StrictModeReturnsNoPartialTextAfterValidPrefix()
    {
        PercentTextDecodeResult result = PercentDecoderCore.DecodeTextStrict(
            "valid%20prefix%FFsuffix",
            100,
            UriComponent.Input,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidUtf8));
        });
    }

    [TestCase("%E2%28%A1", "\uFFFD(\uFFFD")]
    [TestCase("%E2%82", "\uFFFD")]
    [TestCase("%E2%82A", "\uFFFDA")]
    [TestCase("%F0%90%80", "\uFFFD")]
    [TestCase("%F0%80%80%80", "\uFFFD\uFFFD\uFFFD\uFFFD")]
    [TestCase("%ED%A0%80", "\uFFFD\uFFFD\uFFFD")]
    [TestCase("%C0%AF", "\uFFFD\uFFFD")]
    public void ReplacementModeConsumesUnicodeMaximalSubparts(string input, string expected)
    {
        PercentTextDecodeResult result = PercentDecoderCore.DecodeTextWithReplacement(
            input,
            100,
            UriComponent.PathSegment,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(expected));
            Assert.That(result.UsedReplacement, Is.True);
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void ReplacementTextAndSpanEntryPointsProduceIdenticalResults()
    {
        string input = "a%FFb%E2%82";

        PercentTextDecodeResult fromText = PercentDecoderCore.DecodeTextWithReplacement(
            input,
            10,
            UriComponent.UserInfo,
            5);
        PercentTextDecodeResult fromSpan = PercentDecoderCore.DecodeTextSpanWithReplacement(
            input.AsSpan(),
            10,
            UriComponent.UserInfo,
            5);

        Assert.Multiple(() =>
        {
            Assert.That(fromText.Success, Is.True);
            Assert.That(fromText.Text, Is.EqualTo("a\uFFFDb\uFFFD"));
            Assert.That(fromText.UsedReplacement, Is.True);
            Assert.That(fromSpan.Text, Is.EqualTo(fromText.Text));
            Assert.That(fromSpan.UsedReplacement, Is.True);
        });
    }

    [Test]
    public void ReplacementModeLeavesLegalUtf8Unchanged()
    {
        PercentTextDecodeResult result = PercentDecoderCore.DecodeTextWithReplacement(
            "plain%20%C2%A2%E2%82%AC%F0%9F%98%80",
            100,
            UriComponent.Input,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo("plain \u00a2\u20ac\ud83d\ude00"));
            Assert.That(result.UsedReplacement, Is.False);
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void ReplacementModeDoesNotRepairMalformedPercentSyntax()
    {
        PercentTextDecodeResult result = PercentDecoderCore.DecodeTextWithReplacement(
            "a%G0%FF",
            100,
            UriComponent.Query,
            20);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Text, Is.Empty);
        Assert.That(result.UsedReplacement, Is.False);
        AssertInvalidDiagnostic(
            result.Diagnostic,
            UriDiagnosticCode.InvalidPercentEncoding,
            UriComponent.Query,
            22,
            1,
            DiagnosticFactory.InvalidPercentEncodingMessage);
    }

    [Test]
    public void TextModesRejectLiteralNonAsciiWithoutAssumingItsEncoding()
    {
        PercentTextDecodeResult strict = PercentDecoderCore.DecodeTextStrict(
            "a\u00e9",
            10,
            UriComponent.Path,
            30);
        PercentTextDecodeResult replacement = PercentDecoderCore.DecodeTextWithReplacement(
            "a\u00e9",
            10,
            UriComponent.Path,
            30);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Success, Is.False);
            Assert.That(strict.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidCharacter));
            Assert.That(strict.Diagnostic.Range.Offset, Is.EqualTo(31));
            Assert.That(replacement.Success, Is.False);
            Assert.That(replacement.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidCharacter));
            Assert.That(replacement.Diagnostic.Range.Offset, Is.EqualTo(31));
        });
    }

    [Test]
    public void StrictOutputLimitCountsUtf16CodeUnitsAndReturnsNoPartialText()
    {
        PercentTextDecodeResult result = PercentDecoderCore.DecodeTextStrict(
            "%F0%9F%98%80",
            1,
            UriComponent.Input,
            0);

        AssertLimitFailure(result, 1, 1, false);
    }

    [Test]
    public void ReplacementOutputLimitCountsReplacementCharacters()
    {
        PercentTextDecodeResult result = PercentDecoderCore.DecodeTextWithReplacement(
            "%FF%FF",
            1,
            UriComponent.Input,
            0);

        AssertLimitFailure(result, 1, 1, true);
    }

    [Test]
    public void ExactAndEmptyOutputLimitsSucceedInBothModes()
    {
        PercentTextDecodeResult strict = PercentDecoderCore.DecodeTextStrict(
            "%F0%9F%98%80",
            2,
            UriComponent.Input,
            0);
        PercentTextDecodeResult replacement = PercentDecoderCore.DecodeTextWithReplacement(
            "%FF",
            1,
            UriComponent.Input,
            0);
        PercentTextDecodeResult empty = PercentDecoderCore.DecodeTextStrict(
            "",
            0,
            UriComponent.Input,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Success, Is.True);
            Assert.That(strict.Text, Is.EqualTo("\ud83d\ude00"));
            Assert.That(replacement.Success, Is.True);
            Assert.That(replacement.Text, Is.EqualTo("\uFFFD"));
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.Text, Is.Empty);
        });
    }

    [Test]
    public void InvalidUtf8TakesPrecedenceOverStrictOutputLimit()
    {
        PercentTextDecodeResult result = PercentDecoderCore.DecodeTextStrict(
            "%FF",
            0,
            UriComponent.Input,
            0);

        Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidUtf8));
    }

    [Test]
    public void NegativeOutputLimitFailsDeterministicallyForEmptyInput()
    {
        PercentTextDecodeResult strict = PercentDecoderCore.DecodeTextStrict(
            "",
            -1,
            UriComponent.Input,
            0);
        PercentTextDecodeResult replacement = PercentDecoderCore.DecodeTextWithReplacement(
            "",
            -1,
            UriComponent.Input,
            0);

        AssertLimitFailure(strict, 0, 0, false);
        AssertLimitFailure(replacement, 0, 0, false);
    }

    private static void AssertLimitFailure(
        PercentTextDecodeResult result,
        int offset,
        int length,
        bool usedReplacement)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Text, Is.Empty);
        Assert.That(result.UsedReplacement, Is.EqualTo(usedReplacement));
        AssertInvalidDiagnostic(
            result.Diagnostic,
            UriDiagnosticCode.LimitExceeded,
            UriComponent.Output,
            offset,
            length,
            LimitGuard.OutputLimitMessage);
    }

    private static void AssertInvalidDiagnostic(
        UriDiagnostic? diagnostic,
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        int length,
        string message)
    {
        Assert.That(diagnostic, Is.Not.Null);
        Assert.That(diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.PercentEncoding));
        Assert.That(diagnostic.Code, Is.EqualTo(code));
        Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
        Assert.That(diagnostic.Component, Is.EqualTo(component));
        Assert.That(diagnostic.Range.Offset, Is.EqualTo(offset));
        Assert.That(diagnostic.Range.Length, Is.EqualTo(length));
        Assert.That(diagnostic.Message, Is.EqualTo(message));
    }
}
