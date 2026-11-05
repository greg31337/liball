namespace svg.Tests;

public class SvgNumberFormatterTests
{
    [TestCase(0.1, "0.1")]
    [TestCase(1e-7, "1e-7")]
    [TestCase(1e20, "1e20")]
    [TestCase(double.Epsilon, "5e-324")]
    [TestCase(double.MaxValue, "1.7976931348623157e308")]
    public void ShortestFormattingUsesNormalizedRoundTripText(double value, string expected)
    {
        SvgNumberFormatResult result = SvgNumberFormatter.FormatShortest(value);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgNumberFormatError.None));
            Assert.That(result.Text, Is.EqualTo(expected));
            Assert.That(result.Text, Does.Not.Contain("E"));
            Assert.That(result.Text, Does.Not.Contain("e+"));
        });
    }

    [TestCase(0.1)]
    [TestCase(Math.PI)]
    [TestCase(-123456789.12345679)]
    [TestCase(1e-7)]
    [TestCase(1e20)]
    [TestCase(double.Epsilon)]
    [TestCase(double.MaxValue)]
    [TestCase(double.MinValue)]
    public void ExactModesRoundTripToTheSameDoubleBits(double value)
    {
        SvgNumberFormatResult shortest = SvgNumberFormatter.FormatShortest(value);
        SvgNumberFormatResult canonical = SvgNumberFormatter.FormatCanonical(value);
        SvgNumberFormatResult compact = SvgNumberFormatter.FormatCompact(value);

        SvgNumberParseResult shortestParsed = SvgNumberParser.ParseNumber(shortest.Text, 512);
        SvgNumberParseResult canonicalParsed = SvgNumberParser.ParseNumber(canonical.Text, 512);
        SvgNumberParseResult compactParsed = SvgNumberParser.ParseNumber(compact.Text, 512);
        long expectedBits = BitConverter.DoubleToInt64Bits(value);

        Assert.Multiple(() =>
        {
            Assert.That(shortestParsed.Success, Is.True);
            Assert.That(canonicalParsed.Success, Is.True);
            Assert.That(compactParsed.Success, Is.True);
            Assert.That(BitConverter.DoubleToInt64Bits(shortestParsed.Value), Is.EqualTo(expectedBits));
            Assert.That(BitConverter.DoubleToInt64Bits(canonicalParsed.Value), Is.EqualTo(expectedBits));
            Assert.That(BitConverter.DoubleToInt64Bits(compactParsed.Value), Is.EqualTo(expectedBits));
        });
    }

    [TestCase(0.000001, "0.000001")]
    [TestCase(0.0000001, "1e-7")]
    [TestCase(1e20, "100000000000000000000")]
    [TestCase(1e21, "1e21")]
    [TestCase(123.5, "123.5")]
    [TestCase(-1e21, "-1e21")]
    public void CanonicalFormattingUsesStablePlainDecimalThresholds(double value, string expected)
    {
        SvgNumberFormatResult result = SvgNumberFormatter.FormatCanonical(value);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(expected));
            Assert.That(result.Text, Does.Not.Contain("E"));
            Assert.That(result.Text, Does.Not.Contain("e+"));
        });
    }

    [TestCase(0.5, ".5")]
    [TestCase(-0.5, "-.5")]
    [TestCase(0.0001, "1e-4")]
    [TestCase(100000.0, "1e5")]
    [TestCase(120000.0, "12e4")]
    [TestCase(-120000.0, "-12e4")]
    [TestCase(0.00012, "12e-5")]
    [TestCase(123.45, "123.45")]
    public void CompactFormattingChoosesTheShorterValidSvgToken(double value, string expected)
    {
        SvgNumberFormatResult result = SvgNumberFormatter.FormatCompact(value);
        SvgNumberParseResult parsed = SvgNumberParser.ParseNumber(result.Text, 64);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(expected));
            Assert.That(parsed.Success, Is.True);
            Assert.That(BitConverter.DoubleToInt64Bits(parsed.Value), Is.EqualTo(BitConverter.DoubleToInt64Bits(value)));
        });
    }

    [Test]
    public void FixedFormattingUsesExactFractionalPrecisionAndInvariantRounding()
    {
        SvgNumberFormatResult padded = SvgNumberFormatter.FormatFixed(1.25, 3);
        SvgNumberFormatResult integer = SvgNumberFormatter.FormatFixed(12.6, 0);
        SvgNumberFormatResult roundedZero = SvgNumberFormatter.FormatFixed(-0.004, 2);
        SvgNumberFormatResult maximumPrecision = SvgNumberFormatter.FormatFixed(1, SvgNumberFormatter.MaximumFixedPrecision);

        Assert.Multiple(() =>
        {
            Assert.That(padded.Success, Is.True);
            Assert.That(padded.Text, Is.EqualTo("1.250"));
            Assert.That(integer.Text, Is.EqualTo("13"));
            Assert.That(roundedZero.Text, Is.EqualTo("0.00"));
            Assert.That(maximumPrecision.Success, Is.True);
            Assert.That(maximumPrecision.Text.Length, Is.EqualTo(101));
            Assert.That(maximumPrecision.Text, Does.StartWith("1."));
        });
    }

    [Test]
    public void TrimmedFixedFormattingRemovesOnlyInsignificantFractionDigits()
    {
        SvgNumberFormatResult fraction = SvgNumberFormatter.FormatFixedTrimmed(1.25, 4);
        SvgNumberFormatResult integer = SvgNumberFormatter.FormatFixedTrimmed(12, 3);
        SvgNumberFormatResult roundedZero = SvgNumberFormatter.FormatFixedTrimmed(-0.004, 2);

        Assert.Multiple(() =>
        {
            Assert.That(fraction.Text, Is.EqualTo("1.25"));
            Assert.That(integer.Text, Is.EqualTo("12"));
            Assert.That(roundedZero.Text, Is.EqualTo("0"));
        });
    }

    [TestCase(0.0)]
    [TestCase(-0.0)]
    public void EveryModeNormalizesNegativeZero(double value)
    {
        SvgNumberFormatResult shortest = SvgNumberFormatter.FormatShortest(value);
        SvgNumberFormatResult canonical = SvgNumberFormatter.FormatCanonical(value);
        SvgNumberFormatResult fixedResult = SvgNumberFormatter.FormatFixed(value, 2);
        SvgNumberFormatResult compact = SvgNumberFormatter.FormatCompact(value);

        Assert.Multiple(() =>
        {
            Assert.That(shortest.Text, Is.EqualTo("0"));
            Assert.That(canonical.Text, Is.EqualTo("0"));
            Assert.That(fixedResult.Text, Is.EqualTo("0.00"));
            Assert.That(compact.Text, Is.EqualTo("0"));
        });
    }

    [Test]
    public void NonFiniteValuesAndInvalidPrecisionReturnStableFailures()
    {
        SvgNumberFormatResult nan = SvgNumberFormatter.FormatShortest(double.NaN);
        SvgNumberFormatResult positiveInfinity = SvgNumberFormatter.FormatCanonical(double.PositiveInfinity);
        SvgNumberFormatResult negativeInfinity = SvgNumberFormatter.FormatCompact(double.NegativeInfinity);
        SvgNumberFormatResult lowPrecision = SvgNumberFormatter.FormatFixed(1, -1);
        SvgNumberFormatResult highPrecision = SvgNumberFormatter.FormatFixed(1, SvgNumberFormatter.MaximumFixedPrecision + 1);
        SvgNumberFormatResult invalidMode = SvgNumberFormatter.Format(1, (SvgNumberFormatMode)999, 0);

        Assert.Multiple(() =>
        {
            Assert.That(nan.Success, Is.False);
            Assert.That(nan.Error, Is.EqualTo(SvgNumberFormatError.NonFinite));
            Assert.That(nan.Text, Is.Empty);
            Assert.That(positiveInfinity.Error, Is.EqualTo(SvgNumberFormatError.NonFinite));
            Assert.That(negativeInfinity.Error, Is.EqualTo(SvgNumberFormatError.NonFinite));
            Assert.That(lowPrecision.Error, Is.EqualTo(SvgNumberFormatError.InvalidPrecision));
            Assert.That(highPrecision.Error, Is.EqualTo(SvgNumberFormatError.InvalidPrecision));
            Assert.That(invalidMode.Error, Is.EqualTo(SvgNumberFormatError.InvalidMode));
        });
    }

    [Test]
    public void GeneralFormatDispatchesToEveryMode()
    {
        SvgNumberFormatResult shortest = SvgNumberFormatter.Format(0.5, SvgNumberFormatMode.Shortest, 9);
        SvgNumberFormatResult canonical = SvgNumberFormatter.Format(1e-6, SvgNumberFormatMode.Canonical, 9);
        SvgNumberFormatResult fixedResult = SvgNumberFormatter.Format(1.25, SvgNumberFormatMode.Fixed, 2);
        SvgNumberFormatResult compact = SvgNumberFormatter.Format(0.5, SvgNumberFormatMode.Compact, 9);

        Assert.Multiple(() =>
        {
            Assert.That(shortest.Text, Is.EqualTo("0.5"));
            Assert.That(canonical.Text, Is.EqualTo("0.000001"));
            Assert.That(fixedResult.Text, Is.EqualTo("1.25"));
            Assert.That(compact.Text, Is.EqualTo(".5"));
        });
    }

    [Test]
    [NonParallelizable]
    public void FormattingIsCultureInvariant()
    {
        System.Globalization.CultureInfo originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo originalUiCulture = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("de-DE");

            SvgNumberFormatResult shortest = SvgNumberFormatter.FormatShortest(1234.5);
            SvgNumberFormatResult canonical = SvgNumberFormatter.FormatCanonical(0.000001);
            SvgNumberFormatResult fixedResult = SvgNumberFormatter.FormatFixed(1234.5, 2);
            SvgNumberFormatResult compact = SvgNumberFormatter.FormatCompact(0.5);

            Assert.Multiple(() =>
            {
                Assert.That(shortest.Text, Is.EqualTo("1234.5"));
                Assert.That(canonical.Text, Is.EqualTo("0.000001"));
                Assert.That(fixedResult.Text, Is.EqualTo("1234.50"));
                Assert.That(compact.Text, Is.EqualTo(".5"));
            });
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
            System.Globalization.CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
