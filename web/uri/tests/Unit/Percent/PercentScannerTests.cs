namespace uri.Tests;

public class PercentScannerTests
{
    [Test]
    public void ScanTripletDecodesEveryByteInUpperAndLowerHex()
    {
        for (int value = 0; value <= 255; value++)
        {
            char upperHigh = Ascii.GetUpperHexCharacter(value >> 4);
            char upperLow = Ascii.GetUpperHexCharacter(value & 15);
            char lowerHigh = AsciiCase.FoldToLower(upperHigh);
            char lowerLow = AsciiCase.FoldToLower(upperLow);
            string upperText = new string(new char[] { '%', upperHigh, upperLow });
            string lowerText = new string(new char[] { '%', lowerHigh, lowerLow });

            PercentTripletResult upper = PercentScanner.ScanTriplet(
                upperText.AsSpan(),
                0,
                0,
                UriComponent.Path);
            PercentTripletResult lower = PercentScanner.ScanTriplet(
                lowerText.AsSpan(),
                0,
                0,
                UriComponent.Path);

            Assert.That(upper.Success, Is.True, $"upper byte {value}");
            Assert.That(upper.Value, Is.EqualTo(value));
            Assert.That(upper.StartOffset, Is.Zero);
            Assert.That(upper.NextOffset, Is.EqualTo(3));
            Assert.That(upper.Diagnostic, Is.Null);
            Assert.That(lower.Success, Is.True, $"lower byte {value}");
            Assert.That(lower.Value, Is.EqualTo(value));
        }
    }

    [Test]
    public void ScanTripletUsesRelativeCursorAndAbsoluteDiagnosticBaseIndependently()
    {
        PercentTripletResult result = PercentScanner.ScanTriplet(
            "x%2fy".AsSpan(),
            1,
            100,
            UriComponent.Query);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Value, Is.EqualTo(47));
            Assert.That(result.StartOffset, Is.EqualTo(1));
            Assert.That(result.NextOffset, Is.EqualTo(4));
        });
    }

    [TestCase("%", 0, 10, 1)]
    [TestCase("%0", 0, 10, 1)]
    [TestCase("%G0", 0, 11, 1)]
    [TestCase("%0G", 0, 12, 1)]
    [TestCase("x", 0, 10, 1)]
    [TestCase("", 0, 10, 0)]
    [TestCase("x", 1, 11, 0)]
    [TestCase("x", -1, 10, 0)]
    public void ScanTripletReportsExactInvalidMarkerDigitOrBoundary(
        string text,
        int tripletOffset,
        int expectedOffset,
        int expectedLength)
    {
        PercentTripletResult result = PercentScanner.ScanTriplet(
            text.AsSpan(),
            tripletOffset,
            10,
            UriComponent.Fragment);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Value, Is.Zero);
            Assert.That(result.NextOffset, Is.EqualTo(tripletOffset));
            AssertInvalidPercentDiagnostic(
                result.Diagnostic,
                UriComponent.Fragment,
                expectedOffset,
                expectedLength);
        });
    }

    [Test]
    public void FirstErrorModeSkipsValidTripletsAndReturnsFirstMalformedDigit()
    {
        UriDiagnostic? valid = PercentScanner.ValidateFirstError(
            "plain%20text%2f".AsSpan(),
            20,
            UriComponent.Path);
        UriDiagnostic? invalid = PercentScanner.ValidateFirstError(
            "plain%20text%G0then%0G".AsSpan(),
            20,
            UriComponent.Path);

        Assert.That(valid, Is.Null);
        AssertInvalidPercentDiagnostic(invalid, UriComponent.Path, 33, 1);
    }

    [Test]
    public void AllErrorModeReportsEveryInvalidDigitAndIncompleteMarker()
    {
        UriDiagnostic[] diagnostics = PercentScanner.ValidateAllErrors(
            "a%20b%GZc%".AsSpan(),
            10,
            UriComponent.Query);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics, Has.Length.EqualTo(3));
            AssertInvalidPercentDiagnostic(diagnostics[0], UriComponent.Query, 16, 1);
            AssertInvalidPercentDiagnostic(diagnostics[1], UriComponent.Query, 17, 1);
            AssertInvalidPercentDiagnostic(diagnostics[2], UriComponent.Query, 19, 1);
        });
    }

    [Test]
    public void AllErrorModeTreatsEachFullTripletCandidateAsOneLexicalUnit()
    {
        UriDiagnostic[] diagnostics = PercentScanner.ValidateAllErrors(
            "%GG%0Z".AsSpan(),
            0,
            UriComponent.UserInfo);
        UriDiagnostic[] overlapping = PercentScanner.ValidateAllErrors(
            "%G%20".AsSpan(),
            0,
            UriComponent.UserInfo);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics.Select(item => item.Range.Offset), Is.EqualTo(new int[] { 1, 2, 5 }));
            Assert.That(overlapping.Select(item => item.Range.Offset), Is.EqualTo(new int[] { 1, 2 }));
        });
    }

    [Test]
    public void IncompleteTripletReportsMarkerInsteadOfPartialDigits()
    {
        UriDiagnostic? first = PercentScanner.ValidateFirstError(
            "%G".AsSpan(),
            7,
            UriComponent.Host);
        UriDiagnostic[] all = PercentScanner.ValidateAllErrors(
            "%G".AsSpan(),
            7,
            UriComponent.Host);

        AssertInvalidPercentDiagnostic(first, UriComponent.Host, 7, 1);
        Assert.That(all, Has.Length.EqualTo(1));
        AssertInvalidPercentDiagnostic(all[0], UriComponent.Host, 7, 1);
    }

    [Test]
    public void ValidationWithoutPercentMarkersReturnsNoDiagnostics()
    {
        UriDiagnostic? first = PercentScanner.ValidateFirstError(
            "plain text \u00e4".AsSpan(),
            0,
            UriComponent.Input);
        UriDiagnostic[] all = PercentScanner.ValidateAllErrors(
            "plain text \u00e4".AsSpan(),
            0,
            UriComponent.Input);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Null);
            Assert.That(all, Is.Empty);
        });
    }

    [Test]
    public void AbsoluteOffsetCalculationSaturatesInsteadOfOverflowing()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PercentScanner.GetAbsoluteOffset(10, 5), Is.EqualTo(15));
            Assert.That(PercentScanner.GetAbsoluteOffset(-1, 5), Is.EqualTo(5));
            Assert.That(PercentScanner.GetAbsoluteOffset(int.MaxValue, 1), Is.EqualTo(int.MaxValue));
        });
    }

    private static void AssertInvalidPercentDiagnostic(
        UriDiagnostic? diagnostic,
        UriComponent component,
        int offset,
        int length)
    {
        Assert.That(diagnostic, Is.Not.Null);
        Assert.That(diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.PercentEncoding));
        Assert.That(diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidPercentEncoding));
        Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
        Assert.That(diagnostic.Component, Is.EqualTo(component));
        Assert.That(diagnostic.Range.Offset, Is.EqualTo(offset));
        Assert.That(diagnostic.Range.Length, Is.EqualTo(length));
        Assert.That(diagnostic.Message, Is.EqualTo(DiagnosticFactory.InvalidPercentEncodingMessage));
    }
}
