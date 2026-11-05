using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CddlNumberParserTests
{
    [Test]
    public void ParsesSignedAndUnsignedIntegersInEveryRfcRadix()
    {
        BigInteger hugeDecimal = BigInteger.Parse(
            "12345678901234567890123456789012345678901234567890");
        CddlToken zero = Read("0");
        CddlToken negativeZero = Read("-0");
        CddlToken decimalValue = Read(hugeDecimal.ToString());
        CddlToken negativeDecimal = Read("-42");
        CddlToken hexadecimal = Read("0XDEADBEEF");
        CddlToken negativeHexadecimal = Read("-0x10000000000000000");
        CddlToken binary = Read("0b101010");
        CddlToken negativeBinary = Read("-0B11111111");

        Assert.Multiple(() =>
        {
            AssertInteger(zero, CddlLiteralKind.UnsignedInteger, BigInteger.Zero);
            AssertInteger(negativeZero, CddlLiteralKind.NegativeInteger, BigInteger.Zero);
            AssertInteger(decimalValue, CddlLiteralKind.UnsignedInteger, hugeDecimal);
            AssertInteger(negativeDecimal, CddlLiteralKind.NegativeInteger, -42);
            AssertInteger(hexadecimal, CddlLiteralKind.UnsignedInteger, 0xdeadbeef);
            AssertInteger(
                negativeHexadecimal,
                CddlLiteralKind.NegativeInteger,
                -(BigInteger.One << 64));
            AssertInteger(binary, CddlLiteralKind.UnsignedInteger, 42);
            AssertInteger(negativeBinary, CddlLiteralKind.NegativeInteger, -255);
        });
    }

    [Test]
    public void ParsesDecimalFractionsExponentsUnderflowAndNegativeZero()
    {
        CddlToken fraction = Read("12.5");
        CddlToken exponent = Read("1E3");
        CddlToken combined = Read("-12.5e+2");
        CddlToken underflow = Read("1e-999999");
        CddlToken negativeZero = Read("-0.0e999999");

        Assert.Multiple(() =>
        {
            AssertFloat(fraction, CddlLiteralKind.DecimalFloat, 12.5);
            AssertFloat(exponent, CddlLiteralKind.DecimalFloat, 1000.0);
            AssertFloat(combined, CddlLiteralKind.DecimalFloat, -1250.0);
            AssertFloat(underflow, CddlLiteralKind.DecimalFloat, 0.0);
            Assert.That(
                BitConverter.DoubleToInt64Bits(negativeZero.FloatValue),
                Is.EqualTo(long.MinValue));
        });
    }

    [Test]
    public void ParsesHexadecimalFloatsAndCaseInsensitiveMarkers()
    {
        CddlToken one = Read("0x1p0");
        CddlToken three = Read("0x1.8p1");
        CddlToken negative = Read("-0X1.Ap+2");
        CddlToken zero = Read("0x0p999999999999999999999");
        CddlToken negativeZero = Read("-0x0.0P-999999999999999999999");

        Assert.Multiple(() =>
        {
            AssertFloat(one, CddlLiteralKind.HexadecimalFloat, 1.0);
            AssertFloat(three, CddlLiteralKind.HexadecimalFloat, 3.0);
            AssertFloat(negative, CddlLiteralKind.HexadecimalFloat, -6.5);
            AssertFloat(zero, CddlLiteralKind.HexadecimalFloat, 0.0);
            Assert.That(
                BitConverter.DoubleToInt64Bits(negativeZero.FloatValue),
                Is.EqualTo(long.MinValue));
        });
    }

    [TestCase("1", CddlLiteralKind.UnsignedInteger, 1.0)]
    [TestCase("0", CddlLiteralKind.UnsignedInteger, 0.0)]
    [TestCase("0xFf", CddlLiteralKind.UnsignedInteger, 255.0)]
    [TestCase("0b01", CddlLiteralKind.UnsignedInteger, 1.0)]
    [TestCase("-9", CddlLiteralKind.NegativeInteger, -9.0)]
    [TestCase("1.25", CddlLiteralKind.DecimalFloat, 1.25)]
    [TestCase("1e-2", CddlLiteralKind.DecimalFloat, 0.01)]
    [TestCase("0x1p+4", CddlLiteralKind.HexadecimalFloat, 16.0)]
    [TestCase("0x1.fp-1", CddlLiteralKind.HexadecimalFloat, 0.96875)]
    public void CoversEveryNumericGrammarAlternative(
        string source,
        CddlLiteralKind literalKind,
        double expected)
    {
        CddlToken token = Read(source);

        Assert.Multiple(() =>
        {
            Assert.That(token.LiteralKind, Is.EqualTo(literalKind));
            if (token.HasIntegerValue)
            {
                Assert.That((double)token.IntegerValue, Is.EqualTo(expected));
            }
            else
            {
                Assert.That(token.FloatValue, Is.EqualTo(expected));
            }
        });
    }

    [Test]
    public void HexadecimalConversionCoversNormalAndSubnormalBoundaries()
    {
        CddlToken maximum = Read("0x1.fffffffffffffp1023");
        CddlToken minimumNormal = Read("0x1p-1022");
        CddlToken minimumSubnormal = Read("0x1p-1074");
        CddlToken halfwayToZero = Read("0x1p-1075");
        CddlToken aboveHalfway = Read("0x1.8p-1075");

        Assert.Multiple(() =>
        {
            Assert.That(
                BitConverter.DoubleToInt64Bits(maximum.FloatValue),
                Is.EqualTo(0x7fefffffffffffffL));
            Assert.That(
                BitConverter.DoubleToInt64Bits(minimumNormal.FloatValue),
                Is.EqualTo(0x0010000000000000L));
            Assert.That(
                BitConverter.DoubleToInt64Bits(minimumSubnormal.FloatValue),
                Is.EqualTo(1L));
            Assert.That(
                BitConverter.DoubleToInt64Bits(halfwayToZero.FloatValue),
                Is.EqualTo(0L));
            Assert.That(
                BitConverter.DoubleToInt64Bits(aboveHalfway.FloatValue),
                Is.EqualTo(1L));
        });
    }

    [Test]
    public void HexadecimalConversionRoundsHalfwayValuesToEven()
    {
        CddlToken evenLower = Read("0x1.00000000000008p0");
        CddlToken evenUpper = Read("0x1.00000000000018p0");

        Assert.Multiple(() =>
        {
            Assert.That(
                BitConverter.DoubleToInt64Bits(evenLower.FloatValue),
                Is.EqualTo(0x3ff0000000000000L));
            Assert.That(
                BitConverter.DoubleToInt64Bits(evenUpper.FloatValue),
                Is.EqualTo(0x3ff0000000000002L));
        });
    }

    [TestCase("1e309")]
    [TestCase("-1e999999999999999999999999")]
    [TestCase("0x1p1024")]
    [TestCase("-0x1p999999999999999999999999")]
    [TestCase("0x1.fffffffffffff8p1023")]
    public void RejectsDecimalAndHexadecimalOverflow(string source)
    {
        AssertInvalid(Read(source), "CDDL_NUMBER_OVERFLOW");
    }

    [TestCase("00", "CDDL_NUMBER_DECIMAL_LEADING_ZERO")]
    [TestCase("01", "CDDL_NUMBER_DECIMAL_LEADING_ZERO")]
    [TestCase("-00", "CDDL_NUMBER_DECIMAL_LEADING_ZERO")]
    [TestCase("0x", "CDDL_NUMBER_DIGIT_REQUIRED")]
    [TestCase("0b", "CDDL_NUMBER_DIGIT_REQUIRED")]
    [TestCase("0b102", "CDDL_NUMBER_CHARACTER_INVALID")]
    [TestCase("1.", "CDDL_NUMBER_FRACTION_REQUIRED")]
    [TestCase("1e", "CDDL_NUMBER_EXPONENT_REQUIRED")]
    [TestCase("1e+", "CDDL_NUMBER_EXPONENT_REQUIRED")]
    [TestCase("1.2.3", "CDDL_NUMBER_CHARACTER_INVALID")]
    [TestCase("123abc", "CDDL_NUMBER_CHARACTER_INVALID")]
    [TestCase("0x1.", "CDDL_NUMBER_FRACTION_REQUIRED")]
    [TestCase("0x1.2", "CDDL_NUMBER_HEX_EXPONENT_REQUIRED")]
    [TestCase("0x1p", "CDDL_NUMBER_EXPONENT_REQUIRED")]
    [TestCase("0x1p+", "CDDL_NUMBER_EXPONENT_REQUIRED")]
    [TestCase("0x1g", "CDDL_NUMBER_CHARACTER_INVALID")]
    [TestCase("0o7", "CDDL_NUMBER_CHARACTER_INVALID")]
    public void RejectsMalformedNumericGrammar(string source, string errorCode)
    {
        CddlNumberResult result = ParseRaw(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(result.ErrorMessage, Is.Not.Empty);
        });
    }

    [Test]
    public void LexerReportsMalformedNumericSubspansPrecisely()
    {
        CddlParseOptions options = new CddlParseOptions
        {
            SourceName = "numbers.cddl"
        };
        CddlLexer lexer = new CddlLexer("  00", options);
        CddlToken leadingZero = lexer.ReadToken();
        CddlDiagnostic diagnostic = lexer.Diagnostics[0];
        CddlLexer exponentLexer = new CddlLexer("\n 0x1p+", options);
        CddlToken exponent = exponentLexer.ReadToken();
        CddlDiagnostic exponentDiagnostic = exponentLexer.Diagnostics[0];

        Assert.Multiple(() =>
        {
            AssertInvalid(leadingZero, "CDDL_NUMBER_DECIMAL_LEADING_ZERO");
            Assert.That(leadingZero.Span.Start, Is.EqualTo(2));
            Assert.That(leadingZero.Span.Length, Is.EqualTo(2));
            Assert.That(diagnostic.Span.SourceName, Is.EqualTo("numbers.cddl"));
            Assert.That(diagnostic.Span.Start, Is.EqualTo(3));
            Assert.That(diagnostic.Span.StartLine, Is.EqualTo(1));
            Assert.That(diagnostic.Span.StartColumn, Is.EqualTo(4));
            Assert.That(diagnostic.Span.Length, Is.EqualTo(1));
            AssertInvalid(exponent, "CDDL_NUMBER_EXPONENT_REQUIRED");
            Assert.That(exponentDiagnostic.Span.StartLine, Is.EqualTo(2));
            Assert.That(exponentDiagnostic.Span.StartColumn, Is.EqualTo(7));
            Assert.That(exponentDiagnostic.Span.Length, Is.EqualTo(0));
        });
    }

    [Test]
    public void LexerKeepsMalformedAlphanumericNumbersTogetherForOneDiagnostic()
    {
        CddlLexer hexadecimal = new CddlLexer(
            "0xG",
            new CddlParseOptions());
        CddlLexer unsupportedPrefix = new CddlLexer(
            "0o7",
            new CddlParseOptions());
        CddlLexer adjacentName = new CddlLexer(
            "1name",
            new CddlParseOptions());

        CddlToken badHexadecimal = hexadecimal.ReadToken();
        CddlToken badPrefix = unsupportedPrefix.ReadToken();
        CddlToken badAdjacentName = adjacentName.ReadToken();

        Assert.Multiple(() =>
        {
            AssertInvalid(badHexadecimal, "CDDL_NUMBER_DIGIT_REQUIRED");
            Assert.That(badHexadecimal.Text, Is.EqualTo("0xG"));
            AssertInvalid(badPrefix, "CDDL_NUMBER_CHARACTER_INVALID");
            Assert.That(badPrefix.Text, Is.EqualTo("0o7"));
            AssertInvalid(badAdjacentName, "CDDL_NUMBER_CHARACTER_INVALID");
            Assert.That(badAdjacentName.Text, Is.EqualTo("1name"));
        });
    }

    private static CddlToken Read(string source)
    {
        CddlLexer lexer = new CddlLexer(source, new CddlParseOptions
        {
            SourceName = "numbers.cddl"
        });
        return lexer.ReadToken();
    }

    private static CddlNumberResult ParseRaw(string source)
    {
        CddlToken token = new CddlToken
        {
            Kind = CddlTokenKind.Number,
            Text = source,
            Span = new CddlSourceSpan
            {
                SourceName = "numbers.cddl",
                Length = source.Length
            }
        };
        return new CddlNumberParser(token).ParseNumber();
    }

    private static void AssertInteger(
        CddlToken token,
        CddlLiteralKind literalKind,
        BigInteger expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(token.Kind, Is.EqualTo(CddlTokenKind.Number));
            Assert.That(token.LiteralKind, Is.EqualTo(literalKind));
            Assert.That(token.HasIntegerValue, Is.True);
            Assert.That(token.IntegerValue, Is.EqualTo(expected));
            Assert.That(token.HasFloatValue, Is.False);
        });
    }

    private static void AssertFloat(
        CddlToken token,
        CddlLiteralKind literalKind,
        double expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(token.Kind, Is.EqualTo(CddlTokenKind.Number));
            Assert.That(token.LiteralKind, Is.EqualTo(literalKind));
            Assert.That(token.HasFloatValue, Is.True);
            Assert.That(token.FloatValue, Is.EqualTo(expected));
            Assert.That(token.HasIntegerValue, Is.False);
        });
    }

    private static void AssertInvalid(CddlToken token, string errorCode)
    {
        Assert.Multiple(() =>
        {
            Assert.That(token.Kind, Is.EqualTo(CddlTokenKind.Invalid));
            Assert.That(token.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(token.ErrorMessage, Is.Not.Empty);
        });
    }
}
