namespace svg.Tests;

public class SvgNumberParserTests
{
    [TestCase("0", 0.0)]
    [TestCase("-0", 0.0)]
    [TestCase("+12", 12.0)]
    [TestCase(".5", 0.5)]
    [TestCase("-.25", -0.25)]
    [TestCase("5.", 5.0)]
    [TestCase("1.25", 1.25)]
    [TestCase("6.02e23", 6.02e23)]
    [TestCase("1E-3", 0.001)]
    [TestCase("-.25e+2", -25.0)]
    public void SvgNumbersParseInvariantSignsDecimalsAndExponents(string input, double expected)
    {
        SvgNumberParseResult result = SvgNumberParser.ParseNumber(input, 64);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgNumberParseError.None));
            Assert.That(result.Value, Is.EqualTo(expected));
            Assert.That(double.IsFinite(result.Value), Is.True);
            Assert.That(result.Start, Is.Zero);
            Assert.That(result.NextOffset, Is.EqualTo(input.Length));
            Assert.That(result.ErrorOffset, Is.EqualTo(-1));
        });
    }

    [Test]
    public void ParsedNegativeZeroIsNormalizedToPositiveZero()
    {
        SvgNumberParseResult number = SvgNumberParser.ParseNumber("-0.0e20", 32);
        SvgIntegerParseResult integer = SvgNumberParser.ParseInteger("-0", 32);

        Assert.Multiple(() =>
        {
            Assert.That(number.Success, Is.True);
            Assert.That(BitConverter.DoubleToInt64Bits(number.Value), Is.Zero);
            Assert.That(integer.Success, Is.True);
            Assert.That(integer.Value, Is.Zero);
        });
    }

    [Test]
    public void SvgAndCssModesRespectTheirDecimalAndExponentBoundaries()
    {
        SvgSpanReader svgReader = new SvgSpanReader("1.e2-3", 32);
        SvgSpanReader cssReader = new SvgSpanReader("1.e2-3", 32);
        SvgSpanReader cssExponentReader = new SvgSpanReader("1e+", 32);

        SvgNumberParseResult svg = SvgNumberParser.ReadNumber(svgReader);
        SvgNumberParseResult css = SvgNumberParser.ReadCssNumber(cssReader);
        SvgNumberParseResult cssExponent = SvgNumberParser.ReadCssNumber(cssExponentReader);
        SvgNumberParseResult completeCss = SvgNumberParser.ParseCssNumber("1e+", 32);

        Assert.Multiple(() =>
        {
            Assert.That(svg.Success, Is.True);
            Assert.That(svg.Value, Is.EqualTo(100));
            Assert.That(svg.NextOffset, Is.EqualTo(4));
            Assert.That(svgReader.PeekCharacter().Value, Is.EqualTo('-'));
            Assert.That(css.Success, Is.True);
            Assert.That(css.Value, Is.EqualTo(1));
            Assert.That(css.NextOffset, Is.EqualTo(1));
            Assert.That(cssReader.PeekCharacter().Value, Is.EqualTo('.'));
            Assert.That(cssExponent.Success, Is.True);
            Assert.That(cssExponent.Value, Is.EqualTo(1));
            Assert.That(cssExponent.NextOffset, Is.EqualTo(1));
            Assert.That(completeCss.Success, Is.False);
            Assert.That(completeCss.Error, Is.EqualTo(SvgNumberParseError.TrailingCharacters));
            Assert.That(completeCss.ErrorOffset, Is.EqualTo(1));
        });
    }

    [Test]
    public void SequentialReadsSupportCompactSignAndDecimalSeparators()
    {
        SvgSpanReader reader = new SvgSpanReader("10-20.5.25e-1", 32);

        SvgNumberParseResult first = SvgNumberParser.ReadNumber(reader);
        SvgNumberParseResult second = SvgNumberParser.ReadNumber(reader);
        SvgNumberParseResult third = SvgNumberParser.ReadNumber(reader);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Value, Is.EqualTo(10));
            Assert.That(first.NextOffset, Is.EqualTo(2));
            Assert.That(second.Success, Is.True);
            Assert.That(second.Value, Is.EqualTo(-20.5));
            Assert.That(second.Start, Is.EqualTo(2));
            Assert.That(second.NextOffset, Is.EqualTo(7));
            Assert.That(third.Success, Is.True);
            Assert.That(third.Value, Is.EqualTo(0.025));
            Assert.That(third.Start, Is.EqualTo(7));
            Assert.That(third.NextOffset, Is.EqualTo(13));
            Assert.That(reader.IsEnd(), Is.True);
        });
    }

    [Test]
    [NonParallelizable]
    public void StandaloneParsingIsCultureInvariantAndHandlesSurroundingWhitespace()
    {
        System.Globalization.CultureInfo originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo originalUiCulture = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("de-DE");

            SvgNumberParseResult svg = SvgNumberParser.ParseNumber(" \t1.5\n", 32);
            SvgNumberParseResult css = SvgNumberParser.ParseCssNumber("\f-2.5E1 ", 32);
            SvgNumberParseResult commaDecimal = SvgNumberParser.ParseNumber("1,5", 32);

            Assert.Multiple(() =>
            {
                Assert.That(svg.Success, Is.True);
                Assert.That(svg.Value, Is.EqualTo(1.5));
                Assert.That(svg.Start, Is.EqualTo(2));
                Assert.That(svg.NextOffset, Is.EqualTo(6));
                Assert.That(css.Success, Is.True);
                Assert.That(css.Value, Is.EqualTo(-25));
                Assert.That(commaDecimal.Success, Is.False);
                Assert.That(commaDecimal.Error, Is.EqualTo(SvgNumberParseError.TrailingCharacters));
                Assert.That(commaDecimal.ErrorOffset, Is.EqualTo(1));
            });
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
            System.Globalization.CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [TestCase("", 1, 0)]
    [TestCase("+", 1, 1)]
    [TestCase(".", 1, 1)]
    [TestCase("-.e", 1, 2)]
    [TestCase("1e", 3, 2)]
    [TestCase("1e+", 3, 3)]
    [TestCase("1e-x", 3, 3)]
    public void MalformedSvgNumbersReportExactOffsetsAndRollBack(string input, int errorValue, int errorOffset)
    {
        SvgSpanReader reader = new SvgSpanReader(input, 32);

        SvgNumberParseResult result = SvgNumberParser.ReadNumber(reader);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That((int)result.Error, Is.EqualTo(errorValue));
            Assert.That(result.Value, Is.Zero);
            Assert.That(result.Start, Is.Zero);
            Assert.That(result.NextOffset, Is.Zero);
            Assert.That(result.ErrorOffset, Is.EqualTo(errorOffset));
            Assert.That(reader.GetPosition(), Is.Zero);
        });
    }

    [Test]
    public void ErrorsUseAbsoluteOffsetsAtNonzeroReaderPositions()
    {
        SvgSpanReader reader = new SvgSpanReader("xx1e+z", 32);
        reader.SetPosition(2);

        SvgNumberParseResult result = SvgNumberParser.ReadNumber(reader);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SvgNumberParseError.InvalidExponent));
            Assert.That(result.Start, Is.EqualTo(2));
            Assert.That(result.NextOffset, Is.EqualTo(2));
            Assert.That(result.ErrorOffset, Is.EqualTo(5));
            Assert.That(reader.GetPosition(), Is.EqualTo(2));
        });
    }

    [Test]
    public void NumberOverflowIsRejectedWhileFiniteUnderflowIsAccepted()
    {
        SvgNumberParseResult positiveOverflow = SvgNumberParser.ParseNumber("1e309", 32);
        SvgNumberParseResult negativeOverflow = SvgNumberParser.ParseNumber("-1e309", 32);
        SvgNumberParseResult underflow = SvgNumberParser.ParseNumber("1e-9999", 32);
        SvgNumberParseResult smallest = SvgNumberParser.ParseNumber("4.9406564584124654e-324", 32);

        Assert.Multiple(() =>
        {
            Assert.That(positiveOverflow.Success, Is.False);
            Assert.That(positiveOverflow.Error, Is.EqualTo(SvgNumberParseError.Overflow));
            Assert.That(positiveOverflow.ErrorOffset, Is.EqualTo(4));
            Assert.That(positiveOverflow.Value, Is.Zero);
            Assert.That(negativeOverflow.Success, Is.False);
            Assert.That(negativeOverflow.ErrorOffset, Is.EqualTo(5));
            Assert.That(underflow.Success, Is.True);
            Assert.That(underflow.Value, Is.Zero);
            Assert.That(double.IsFinite(underflow.Value), Is.True);
            Assert.That(smallest.Success, Is.True);
            Assert.That(smallest.Value, Is.EqualTo(double.Epsilon));
        });
    }

    [Test]
    public void NumericTokenBoundsApplyToSignsFractionsAndExponents()
    {
        SvgSpanReader ordinaryReader = new SvgSpanReader("1234", 3);
        SvgSpanReader exponentReader = new SvgSpanReader("1e10", 3);
        SvgSpanReader exactReader = new SvgSpanReader("1e2!", 3);
        SvgSpanReader invalidReader = new SvgSpanReader("1", 0);

        SvgNumberParseResult ordinary = SvgNumberParser.ReadNumber(ordinaryReader);
        SvgNumberParseResult exponent = SvgNumberParser.ReadNumber(exponentReader);
        SvgNumberParseResult exact = SvgNumberParser.ReadNumber(exactReader);
        SvgNumberParseResult invalid = SvgNumberParser.ReadNumber(invalidReader);

        Assert.Multiple(() =>
        {
            Assert.That(ordinary.Success, Is.False);
            Assert.That(ordinary.Error, Is.EqualTo(SvgNumberParseError.TokenTooLong));
            Assert.That(ordinary.ErrorOffset, Is.EqualTo(3));
            Assert.That(ordinaryReader.GetPosition(), Is.Zero);
            Assert.That(exponent.Success, Is.False);
            Assert.That(exponent.Error, Is.EqualTo(SvgNumberParseError.TokenTooLong));
            Assert.That(exponent.ErrorOffset, Is.EqualTo(3));
            Assert.That(exponentReader.GetPosition(), Is.Zero);
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.Value, Is.EqualTo(100));
            Assert.That(exact.NextOffset, Is.EqualTo(3));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Error, Is.EqualTo(SvgNumberParseError.InvalidArgument));
        });
    }

    [TestCase("0", 0)]
    [TestCase("-0", 0)]
    [TestCase("+17", 17)]
    [TestCase("2147483647", int.MaxValue)]
    [TestCase("-2147483648", int.MinValue)]
    public void SignedIntegersParseAcrossTheirEntireRange(string input, int expected)
    {
        SvgIntegerParseResult result = SvgNumberParser.ParseInteger(input, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgNumberParseError.None));
            Assert.That(result.Value, Is.EqualTo(expected));
            Assert.That(result.NextOffset, Is.EqualTo(input.Length));
            Assert.That(result.ErrorOffset, Is.EqualTo(-1));
        });
    }

    [TestCase("2147483648", 9)]
    [TestCase("-2147483649", 10)]
    [TestCase("99999999999", 9)]
    public void IntegerOverflowReportsTheFirstOutOfRangeDigit(string input, int errorOffset)
    {
        SvgSpanReader reader = new SvgSpanReader(input, 32);

        SvgIntegerParseResult result = SvgNumberParser.ReadInteger(reader);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SvgNumberParseError.Overflow));
            Assert.That(result.Value, Is.Zero);
            Assert.That(result.ErrorOffset, Is.EqualTo(errorOffset));
            Assert.That(reader.GetPosition(), Is.Zero);
        });
    }

    [Test]
    public void IntegerReadsStopAtDecimalExponentAndCompactSignBoundaries()
    {
        SvgSpanReader reader = new SvgSpanReader("12-3.5", 32);

        SvgIntegerParseResult first = SvgNumberParser.ReadInteger(reader);
        SvgIntegerParseResult second = SvgNumberParser.ReadInteger(reader);
        SvgIntegerParseResult decimalValue = SvgNumberParser.ParseInteger("12.0", 32);
        SvgIntegerParseResult exponentValue = SvgNumberParser.ParseInteger("1e2", 32);
        SvgIntegerParseResult signOnly = SvgNumberParser.ParseInteger("-", 32);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Value, Is.EqualTo(12));
            Assert.That(first.NextOffset, Is.EqualTo(2));
            Assert.That(second.Success, Is.True);
            Assert.That(second.Value, Is.EqualTo(-3));
            Assert.That(second.NextOffset, Is.EqualTo(4));
            Assert.That(reader.PeekCharacter().Value, Is.EqualTo('.'));
            Assert.That(decimalValue.Success, Is.False);
            Assert.That(decimalValue.Error, Is.EqualTo(SvgNumberParseError.TrailingCharacters));
            Assert.That(decimalValue.ErrorOffset, Is.EqualTo(2));
            Assert.That(exponentValue.Success, Is.False);
            Assert.That(exponentValue.ErrorOffset, Is.EqualTo(1));
            Assert.That(signOnly.Success, Is.False);
            Assert.That(signOnly.Error, Is.EqualTo(SvgNumberParseError.ExpectedInteger));
            Assert.That(signOnly.ErrorOffset, Is.EqualTo(1));
        });
    }

    [Test]
    public void IntegerTokenBoundsAndNullInputsReturnFailuresWithoutThrowing()
    {
        SvgSpanReader boundedReader = new SvgSpanReader("+123", 3);

        SvgIntegerParseResult bounded = SvgNumberParser.ReadInteger(boundedReader);
        SvgNumberParseResult nullNumber = SvgNumberParser.ParseNumber(null, 32);
        SvgNumberParseResult nullCssNumber = SvgNumberParser.ParseCssNumber(null, 32);
        SvgIntegerParseResult nullInteger = SvgNumberParser.ParseInteger(null, 32);
        SvgNumberParseResult nullReader = SvgNumberParser.ReadNumber(null);

        Assert.Multiple(() =>
        {
            Assert.That(bounded.Success, Is.False);
            Assert.That(bounded.Error, Is.EqualTo(SvgNumberParseError.TokenTooLong));
            Assert.That(bounded.ErrorOffset, Is.EqualTo(3));
            Assert.That(boundedReader.GetPosition(), Is.Zero);
            Assert.That(nullNumber.Error, Is.EqualTo(SvgNumberParseError.InvalidArgument));
            Assert.That(nullCssNumber.Error, Is.EqualTo(SvgNumberParseError.InvalidArgument));
            Assert.That(nullInteger.Error, Is.EqualTo(SvgNumberParseError.InvalidArgument));
            Assert.That(nullReader.Error, Is.EqualTo(SvgNumberParseError.InvalidArgument));
        });
    }
}
