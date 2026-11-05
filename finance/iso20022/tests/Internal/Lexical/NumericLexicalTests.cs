using System.Globalization;
using iso20022;

namespace iso20022.Tests;

public class NumericLexicalTests
{
    [TestCase("0", 0)]
    [TestCase("+17", 17)]
    [TestCase("-17", -17)]
    [TestCase("00042", 42)]
    [TestCase("2147483647", int.MaxValue)]
    [TestCase("-2147483648", int.MinValue)]
    public void Int32ParserAcceptsInvariantBoundaryForms(string text, int expected)
    {
        IntegerLexicalResult result = IntegerLexical.ParseInt32(text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Value, Is.EqualTo(expected));
            Assert.That(result.LexicalValue, Is.EqualTo(text));
            Assert.That(IntegerLexical.FormatInt32(result.Value), Is.EqualTo(expected.ToString(CultureInfo.InvariantCulture)));
        });
    }

    [TestCase("")]
    [TestCase("+")]
    [TestCase("--1")]
    [TestCase(" 1")]
    [TestCase("1 ")]
    [TestCase("1.0")]
    [TestCase("2147483648")]
    [TestCase("-2147483649")]
    public void Int32ParserRejectsMalformedAndOverflowingForms(string text)
    {
        IntegerLexicalResult result = IntegerLexical.ParseInt32(text.AsSpan());

        Assert.That(result.Success, Is.False);
    }

    [TestCase("9223372036854775807", long.MaxValue)]
    [TestCase("-9223372036854775808", long.MinValue)]
    [TestCase("+0000000000000000001", 1L)]
    public void Int64ParserAcceptsSignedBoundaries(string text, long expected)
    {
        LongIntegerLexicalResult result = IntegerLexical.ParseInt64(text.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Value, Is.EqualTo(expected));
            Assert.That(result.LexicalValue, Is.EqualTo(text));
            Assert.That(IntegerLexical.FormatInt64(result.Value), Is.EqualTo(expected.ToString(CultureInfo.InvariantCulture)));
        });
    }

    [TestCase("9223372036854775808")]
    [TestCase("-9223372036854775809")]
    public void Int64ParserRejectsOverflow(string text)
    {
        Assert.That(IntegerLexical.ParseInt64(text.AsSpan()).Success, Is.False);
    }

    [TestCase("0", "0", 0, false)]
    [TestCase("+12", "12", 0, false)]
    [TestCase("12.3400", "12.3400", 4, false)]
    [TestCase(".5", "0.5", 1, false)]
    [TestCase("1.", "1", 0, false)]
    [TestCase("-0.0000", "-0.0000", 4, true)]
    [TestCase("79228162514264337593543950335", "79228162514264337593543950335", 0, false)]
    public void DecimalParserAndFormatterPreserveRepresentableScale(string text, string formatted, int scale, bool negativeZero)
    {
        IsoDecimalValue? result = DecimalLexical.ParseDecimal(text.AsSpan());

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result?.Scale, Is.EqualTo(scale));
            Assert.That(result?.IsNegativeZero, Is.EqualTo(negativeZero));
            Assert.That(result?.LexicalValue, Is.EqualTo(text));
            Assert.That(DecimalLexical.FormatDecimal(result!), Is.EqualTo(formatted));
        });
    }

    [TestCase("")]
    [TestCase("+")]
    [TestCase(".")]
    [TestCase("1.2.3")]
    [TestCase("1e2")]
    [TestCase(" 1")]
    [TestCase("79228162514264337593543950336")]
    [TestCase("0.00000000000000000000000000001")]
    public void DecimalParserRejectsMalformedOrUnrepresentableForms(string text)
    {
        Assert.That(DecimalLexical.ParseDecimal(text.AsSpan()), Is.Null);
    }

    [TestCase("true", true)]
    [TestCase("1", true)]
    [TestCase("false", false)]
    [TestCase("0", false)]
    public void BooleanParserAcceptsEveryXsdBooleanForm(string text, bool expected)
    {
        IsoBooleanValue? result = BooleanLexical.ParseBoolean(text.AsSpan());

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result?.Value, Is.EqualTo(expected));
            Assert.That(result?.LexicalValue, Is.EqualTo(text));
            Assert.That(BooleanLexical.FormatBoolean(expected), Is.EqualTo(expected ? "true" : "false"));
        });
    }

    [TestCase("True")]
    [TestCase("FALSE")]
    [TestCase("yes")]
    [TestCase("")]
    [TestCase(" true")]
    public void BooleanParserRejectsNonXsdForms(string text)
    {
        Assert.That(BooleanLexical.ParseBoolean(text.AsSpan()), Is.Null);
    }

    [Test]
    [NonParallelizable]
    public void NumericParsingAndFormattingIgnoreCurrentCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            IsoDecimalValue? parsed = DecimalLexical.ParseDecimal("1234.50".AsSpan());

            Assert.That(parsed, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(parsed?.Value, Is.EqualTo(1234.50m));
                Assert.That(DecimalLexical.FormatDecimal(parsed!), Is.EqualTo("1234.50"));
                Assert.That(IntegerLexical.FormatInt32(1234), Is.EqualTo("1234"));
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
