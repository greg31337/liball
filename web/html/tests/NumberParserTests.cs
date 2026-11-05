using System.Globalization;

namespace UniversalHtml.Tests;

[TestFixture]
public class NumberParserTests
{
    [TestCase("0", 0)]
    [TestCase("42", 42)]
    [TestCase("-42", -42)]
    [TestCase("-0", 0)]
    [TestCase("2147483647", int.MaxValue)]
    [TestCase("-2147483648", int.MinValue)]
    public void SignedIntegerParsesExactHtmlGrammar(string input, int expected)
    {
        IntegerScanResult result = NumberParser.ParseSignedInteger(input.AsSpan());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.WasOverflow(), Is.False);
        Assert.That(result.GetValue(), Is.EqualTo(expected));
        Assert.That(result.GetConsumedLength(), Is.EqualTo(input.Length));
    }

    [TestCase("")]
    [TestCase("-")]
    [TestCase("+1")]
    [TestCase(" 1")]
    [TestCase("1 ")]
    [TestCase("1.0")]
    [TestCase("١")]
    public void SignedIntegerRejectsNonGrammarInput(string input)
    {
        Assert.That(NumberParser.ParseSignedInteger(input.AsSpan()).IsSuccess(), Is.False);
    }

    [TestCase("2147483648")]
    [TestCase("-2147483649")]
    [TestCase("999999999999999999999999")]
    public void SignedIntegerReportsOverflow(string input)
    {
        IntegerScanResult result = NumberParser.ParseSignedInteger(input.AsSpan());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.WasOverflow(), Is.True);
        Assert.That(result.GetConsumedLength(), Is.EqualTo(input.Length));
    }

    [Test]
    public void DecimalScannerSupportsConfiguredSignsAndPartialTokens()
    {
        IntegerScanResult positive = NumberParser.ScanDecimal("x+123px".AsSpan(), 1, true, true);
        IntegerScanResult negative = NumberParser.ScanDecimal("-9!".AsSpan(), 0, false, true);

        Assert.That(positive.IsSuccess(), Is.True);
        Assert.That(positive.GetValue(), Is.EqualTo(123));
        Assert.That(positive.GetConsumedLength(), Is.EqualTo(4));
        Assert.That(negative.GetValue(), Is.EqualTo(-9));
        Assert.That(negative.GetConsumedLength(), Is.EqualTo(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => NumberParser.ScanDecimal("1".AsSpan(), 2, false, false));
    }

    [Test]
    public void DecimalParsingIsExhaustiveAcrossFocusedIntegerRange()
    {
        for (int value = -10000; value <= 10000; value++)
        {
            string text = value.ToString(CultureInfo.InvariantCulture);
            IntegerScanResult result = NumberParser.ParseSignedInteger(text.AsSpan());
            Assert.That(result.IsSuccess(), Is.True, text);
            Assert.That(result.GetValue(), Is.EqualTo(value), text);
        }
    }

    [Test]
    public void NonNegativeIntegerRejectsSignsAndTrailingData()
    {
        Assert.That(NumberParser.ParseNonNegativeInteger("0".AsSpan()).GetValue(), Is.EqualTo(0));
        Assert.That(NumberParser.ParseNonNegativeInteger("123".AsSpan()).GetValue(), Is.EqualTo(123));
        Assert.That(NumberParser.ParseNonNegativeInteger("-1".AsSpan()).IsSuccess(), Is.False);
        Assert.That(NumberParser.ParseNonNegativeInteger("+1".AsSpan()).IsSuccess(), Is.False);
        Assert.That(NumberParser.ParseNonNegativeInteger("1x".AsSpan()).IsSuccess(), Is.False);
    }

    [TestCase("0", 0)]
    [TestCase("f", 15)]
    [TestCase("Ff", 255)]
    [TestCase("10", 16)]
    [TestCase("7fffffff", int.MaxValue)]
    public void HexadecimalParsesExactAsciiDigits(string input, int expected)
    {
        IntegerScanResult result = NumberParser.ParseHexadecimal(input.AsSpan());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetValue(), Is.EqualTo(expected));
    }

    [Test]
    public void HexadecimalScannerStopsAndReportsOverflow()
    {
        IntegerScanResult partial = NumberParser.ScanHexadecimal("0x10".AsSpan(), 0);
        IntegerScanResult overflow = NumberParser.ParseHexadecimal("80000000".AsSpan());

        Assert.That(partial.GetValue(), Is.EqualTo(0));
        Assert.That(partial.GetConsumedLength(), Is.EqualTo(1));
        Assert.That(NumberParser.ParseHexadecimal("0x10".AsSpan()).IsSuccess(), Is.False);
        Assert.That(overflow.IsSuccess(), Is.False);
        Assert.That(overflow.WasOverflow(), Is.True);
        Assert.That(NumberParser.ParseHexadecimal("".AsSpan()).IsSuccess(), Is.False);
    }

    [Test]
    public void HexadecimalParsingIsExhaustiveThroughFourDigits()
    {
        for (int value = 0; value <= 0xFFFF; value++)
        {
            string text = value.ToString("x", CultureInfo.InvariantCulture);
            IntegerScanResult result = NumberParser.ParseHexadecimal(text.AsSpan());
            Assert.That(result.IsSuccess(), Is.True, text);
            Assert.That(result.GetValue(), Is.EqualTo(value), text);
        }
    }

    [TestCase("0", 0.0)]
    [TestCase("-0", -0.0)]
    [TestCase("1", 1.0)]
    [TestCase(".5", 0.5)]
    [TestCase("-.5", -0.5)]
    [TestCase("1.25", 1.25)]
    [TestCase("1e2", 100.0)]
    [TestCase("1E-2", 0.01)]
    [TestCase("1e+2", 100.0)]
    public void FloatingPointParsesValidHtmlGrammar(string input, double expected)
    {
        FloatingPointScanResult result = NumberParser.ParseFloatingPoint(input.AsSpan());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetValue(), Is.EqualTo(expected).Within(1e-14));
        Assert.That(result.GetConsumedLength(), Is.EqualTo(input.Length));
    }

    [TestCase("")]
    [TestCase("+")]
    [TestCase("+1")]
    [TestCase(".")]
    [TestCase("1.")]
    [TestCase("1e")]
    [TestCase("1e+")]
    [TestCase(" 1")]
    [TestCase("1 ")]
    [TestCase("NaN")]
    [TestCase("Infinity")]
    [TestCase("1,5")]
    public void FloatingPointRejectsInvalidHtmlGrammar(string input)
    {
        Assert.That(NumberParser.ParseFloatingPoint(input.AsSpan()).IsSuccess(), Is.False);
    }

    [Test]
    public void FloatingPointScannerRollsBackIncompleteSuffixes()
    {
        FloatingPointScanResult decimalPoint = NumberParser.ScanFloatingPoint("1.px".AsSpan(), 0);
        FloatingPointScanResult exponent = NumberParser.ScanFloatingPoint("1e+px".AsSpan(), 0);

        Assert.That(decimalPoint.IsSuccess(), Is.True);
        Assert.That(decimalPoint.GetValue(), Is.EqualTo(1));
        Assert.That(decimalPoint.GetConsumedLength(), Is.EqualTo(1));
        Assert.That(exponent.IsSuccess(), Is.True);
        Assert.That(exponent.GetConsumedLength(), Is.EqualTo(1));
    }

    [TestCase("1e400")]
    [TestCase("1e-400")]
    public void FloatingPointReportsUnrepresentableValues(string input)
    {
        FloatingPointScanResult result = NumberParser.ParseFloatingPoint(input.AsSpan());

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.WasOutOfRange(), Is.True);
    }

    [Test]
    public void FloatingPointHandlesFiniteAndZeroBoundaries()
    {
        string maximum = double.MaxValue.ToString("R", CultureInfo.InvariantCulture);
        string epsilon = double.Epsilon.ToString("R", CultureInfo.InvariantCulture);

        Assert.That(NumberParser.ParseFloatingPoint(maximum.AsSpan()).GetValue(), Is.EqualTo(double.MaxValue));
        Assert.That(NumberParser.ParseFloatingPoint(epsilon.AsSpan()).GetValue(), Is.EqualTo(double.Epsilon));
        Assert.That(NumberParser.ParseFloatingPoint("0e999".AsSpan()).GetValue(), Is.EqualTo(0));
        Assert.That(NumberParser.ParseFloatingPoint("-0e-999".AsSpan()).IsSuccess(), Is.True);
        Assert.That(NumberParser.ParseFloatingPoint("1e-324".AsSpan()).WasOutOfRange(), Is.True);
    }

    [Test]
    public void FloatingPointHandlesVeryLongValidTokensWithoutExceptions()
    {
        string hugeInteger = new string('9', 10000);
        string hugeExponent = "1e" + new string('9', 10000);
        string tinyExponent = "1e-" + new string('9', 10000);
        string zeroExponent = "0e" + new string('9', 10000);

        Assert.That(NumberParser.ParseFloatingPoint(hugeInteger.AsSpan()).WasOutOfRange(), Is.True);
        Assert.That(NumberParser.ParseFloatingPoint(hugeExponent.AsSpan()).WasOutOfRange(), Is.True);
        Assert.That(NumberParser.ParseFloatingPoint(tinyExponent.AsSpan()).WasOutOfRange(), Is.True);
        Assert.That(NumberParser.ParseFloatingPoint(zeroExponent.AsSpan()).IsSuccess(), Is.True);
    }

    [TestCase("1x", 1.0)]
    [TestCase("1.5x", 1.5)]
    [TestCase(".5x", 0.5)]
    [TestCase("1e2x", 100.0)]
    public void FloatingPointDescriptorRequiresPositiveValueAndLowercaseSuffix(string input, double expected)
    {
        FloatingPointScanResult result = NumberParser.ParseFloatingPointDescriptor(input.AsSpan());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetValue(), Is.EqualTo(expected));
        Assert.That(result.GetConsumedLength(), Is.EqualTo(input.Length));
    }

    [TestCase("0x")]
    [TestCase("-1x")]
    [TestCase("+1x")]
    [TestCase("1X")]
    [TestCase("1.x")]
    [TestCase("x")]
    public void FloatingPointDescriptorRejectsInvalidForms(string input)
    {
        Assert.That(NumberParser.ParseFloatingPointDescriptor(input.AsSpan()).IsSuccess(), Is.False);
    }

    [Test]
    [NonParallelizable]
    public void FloatingPointIsCultureIndependent()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            Assert.That(NumberParser.ParseFloatingPoint("1.5".AsSpan()).GetValue(), Is.EqualTo(1.5));
            Assert.That(NumberParser.ParseFloatingPoint("1,5".AsSpan()).IsSuccess(), Is.False);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [TestCase("odd", 2, 1)]
    [TestCase("ODD", 2, 1)]
    [TestCase("even", 2, 0)]
    [TestCase("n", 1, 0)]
    [TestCase("N", 1, 0)]
    [TestCase("+n", 1, 0)]
    [TestCase("-n", -1, 0)]
    [TestCase("2n", 2, 0)]
    [TestCase("-2n", -2, 0)]
    [TestCase("0n+5", 0, 5)]
    [TestCase("5", 0, 5)]
    [TestCase("+6", 0, 6)]
    [TestCase("-6", 0, -6)]
    [TestCase("3n-6", 3, -6)]
    [TestCase("3n + 1", 3, 1)]
    [TestCase("+3n - 2", 3, -2)]
    [TestCase("-n+ 6", -1, 6)]
    [TestCase("n- 6", 1, -6)]
    [TestCase("n +2", 1, 2)]
    [TestCase("2N-1", 2, -1)]
    [TestCase("  2n + 3\t", 2, 3)]
    [TestCase("-2147483648n+2147483647", int.MinValue, int.MaxValue)]
    [TestCase("n-2147483648", 1, int.MinValue)]
    public void AnPlusBParsesCssForms(string input, int step, int offset)
    {
        AnPlusBScanResult result = NumberParser.ParseAnPlusB(input.AsSpan());

        Assert.That(result.IsSuccess(), Is.True, input);
        Assert.That(result.WasOverflow(), Is.False, input);
        Assert.That(result.GetStep(), Is.EqualTo(step), input);
        Assert.That(result.GetOffset(), Is.EqualTo(offset), input);
        Assert.That(result.GetConsumedLength(), Is.EqualTo(input.Length), input);
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("3 n")]
    [TestCase("+ 2n")]
    [TestCase("+ 2")]
    [TestCase("3n + -6")]
    [TestCase("n+")]
    [TestCase("n 3")]
    [TestCase("oddly")]
    [TestCase("2n+1x")]
    [TestCase("2n\u00A0+1")]
    public void AnPlusBRejectsInvalidForms(string input)
    {
        Assert.That(NumberParser.ParseAnPlusB(input.AsSpan()).IsSuccess(), Is.False, input);
    }

    [TestCase("2147483648n")]
    [TestCase("-2147483649n")]
    [TestCase("n+2147483648")]
    [TestCase("n-2147483649")]
    [TestCase("2147483648")]
    public void AnPlusBReportsOverflow(string input)
    {
        AnPlusBScanResult result = NumberParser.ParseAnPlusB(input.AsSpan());

        Assert.That(result.IsSuccess(), Is.False, input);
        Assert.That(result.WasOverflow(), Is.True, input);
    }
}
