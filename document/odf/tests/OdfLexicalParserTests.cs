namespace odf.Tests;

public class OdfLexicalParserTests
{
    [TestCase("true", true, "true")]
    [TestCase("1", true, "true")]
    [TestCase("false", false, "false")]
    [TestCase("0", false, "false")]
    public void BooleanValuesParseAndFormatCanonically(string lexical, bool expected, string canonical)
    {
        OdfLexicalValue result = OdfLexicalParser.ParseBoolean(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.BooleanValue, Is.EqualTo(expected));
            Assert.That(result.LexicalValue, Is.EqualTo(lexical));
            Assert.That(OdfLexicalFormatter.FormatBoolean(result), Is.EqualTo(canonical));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("TRUE")]
    [TestCase(" true")]
    [TestCase("yes")]
    public void InvalidBooleanValuesArePreserved(string? lexical)
    {
        OdfLexicalValue result = OdfLexicalParser.ParseBoolean(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.LexicalValue, Is.EqualTo(lexical ?? ""));
            Assert.That(OdfLexicalFormatter.FormatBoolean(result), Is.EqualTo(lexical ?? ""));
        });
    }

    [TestCase("0", 0L, "0")]
    [TestCase("+00042", 42L, "42")]
    [TestCase("-00042", -42L, "-42")]
    [TestCase("9223372036854775807", long.MaxValue, "9223372036854775807")]
    [TestCase("-9223372036854775808", long.MinValue, "-9223372036854775808")]
    public void IntegerValuesParseAndFormatCanonically(string lexical, long expected, string canonical)
    {
        OdfLexicalValue result = OdfLexicalParser.ParseInteger(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IntegerValue, Is.EqualTo(expected));
            Assert.That(OdfLexicalFormatter.FormatInteger(result), Is.EqualTo(canonical));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("+")]
    [TestCase("1.0")]
    [TestCase(" 1")]
    [TestCase("9223372036854775808")]
    [TestCase("-9223372036854775809")]
    public void InvalidIntegerValuesArePreserved(string? lexical)
    {
        OdfLexicalValue result = OdfLexicalParser.ParseInteger(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.LexicalValue, Is.EqualTo(lexical ?? ""));
            Assert.That(OdfLexicalFormatter.FormatInteger(result), Is.EqualTo(lexical ?? ""));
        });
    }

    [TestCase("0", "0")]
    [TestCase("+001.2300", "1.23")]
    [TestCase("-.5", "-0.5")]
    [TestCase("5.", "5")]
    [TestCase("79228162514264337593543950335", "79228162514264337593543950335")]
    [TestCase("7.9228162514264337593543950335", "7.9228162514264337593543950335")]
    public void DecimalValuesParseAndFormatCanonically(string lexical, string canonical)
    {
        OdfLexicalValue result = OdfLexicalParser.ParseDecimal(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.LexicalValue, Is.EqualTo(lexical));
            Assert.That(OdfLexicalFormatter.FormatDecimal(result), Is.EqualTo(canonical));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(".")]
    [TestCase("1.2.3")]
    [TestCase("1e2")]
    [TestCase(" 1.2")]
    [TestCase("79228162514264337593543950336")]
    [TestCase("0.00000000000000000000000000001")]
    public void InvalidDecimalValuesArePreserved(string? lexical)
    {
        OdfLexicalValue result = OdfLexicalParser.ParseDecimal(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.LexicalValue, Is.EqualTo(lexical ?? ""));
            Assert.That(OdfLexicalFormatter.FormatDecimal(result), Is.EqualTo(lexical ?? ""));
        });
    }

    [TestCase("0", 0.0)]
    [TestCase("+1.25", 1.25)]
    [TestCase("-.5", -0.5)]
    [TestCase("5.", 5.0)]
    [TestCase("1.25E2", 125.0)]
    [TestCase("-2.5e-1", -0.25)]
    public void FiniteFloatValuesParseInvariantly(string lexical, double expected)
    {
        OdfLexicalValue result = OdfLexicalParser.ParseFloat(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.FloatValue, Is.EqualTo(expected));
            Assert.That(OdfLexicalParser.ParseFloat(OdfLexicalFormatter.FormatFloat(result)).FloatValue, Is.EqualTo(expected));
        });
    }

    [Test]
    public void SpecialAndOverflowFloatValuesUseXmlSchemaTokens()
    {
        OdfLexicalValue positiveInfinity = OdfLexicalParser.ParseFloat("INF");
        OdfLexicalValue negativeInfinity = OdfLexicalParser.ParseFloat("-INF");
        OdfLexicalValue notANumber = OdfLexicalParser.ParseFloat("NaN");
        OdfLexicalValue overflow = OdfLexicalParser.ParseFloat("1e309");

        Assert.Multiple(() =>
        {
            Assert.That(positiveInfinity.IsValid, Is.True);
            Assert.That(double.IsPositiveInfinity(positiveInfinity.FloatValue), Is.True);
            Assert.That(OdfLexicalFormatter.FormatFloat(positiveInfinity), Is.EqualTo("INF"));
            Assert.That(double.IsNegativeInfinity(negativeInfinity.FloatValue), Is.True);
            Assert.That(OdfLexicalFormatter.FormatFloat(negativeInfinity), Is.EqualTo("-INF"));
            Assert.That(double.IsNaN(notANumber.FloatValue), Is.True);
            Assert.That(OdfLexicalFormatter.FormatFloat(notANumber), Is.EqualTo("NaN"));
            Assert.That(double.IsPositiveInfinity(overflow.FloatValue), Is.True);
            Assert.That(OdfLexicalFormatter.FormatFloat(overflow), Is.EqualTo("INF"));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("inf")]
    [TestCase("+INF")]
    [TestCase("nan")]
    [TestCase("1e")]
    [TestCase("1.2.3")]
    [TestCase(" 1.0")]
    public void InvalidFloatValuesArePreserved(string? lexical)
    {
        OdfLexicalValue result = OdfLexicalParser.ParseFloat(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.LexicalValue, Is.EqualTo(lexical ?? ""));
            Assert.That(OdfLexicalFormatter.FormatFloat(result), Is.EqualTo(lexical ?? ""));
        });
    }

    [TestCase("0%", "0%")]
    [TestCase("+001.2500%", "1.25%")]
    [TestCase("-.5%", "-0.5%")]
    [TestCase("100.%", "100%")]
    public void PercentageValuesParseAndFormatCanonically(string lexical, string canonical)
    {
        OdfLexicalValue result = OdfLexicalParser.ParsePercentage(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(OdfLexicalFormatter.FormatPercentage(result), Is.EqualTo(canonical));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("12")]
    [TestCase("12e2%")]
    [TestCase("12 %")]
    [TestCase("%")]
    public void InvalidPercentageValuesArePreserved(string? lexical)
    {
        OdfLexicalValue result = OdfLexicalParser.ParsePercentage(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(OdfLexicalFormatter.FormatPercentage(result), Is.EqualTo(lexical ?? ""));
        });
    }

    [TestCase("", "", "")]
    [TestCase("TQ==", "M", "TQ==")]
    [TestCase("TWE=", "Ma", "TWE=")]
    [TestCase("TWFu", "Man", "TWFu")]
    [TestCase(" T W\tF\r\nu ", "Man", "TWFu")]
    public void Base64ValuesParseXmlWhitespaceAndFormatCanonically(string lexical, string text, string canonical)
    {
        OdfLexicalValue result = OdfLexicalParser.ParseBase64(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(global::System.Text.Encoding.ASCII.GetString(result.BinaryValue), Is.EqualTo(text));
            Assert.That(OdfLexicalFormatter.FormatBase64(result), Is.EqualTo(canonical));
        });
    }

    [TestCase(null)]
    [TestCase("A")]
    [TestCase("A===")]
    [TestCase("T=Fu")]
    [TestCase("TQ=A")]
    [TestCase("TR==")]
    [TestCase("TWF=")]
    [TestCase("TWFu!")]
    public void InvalidBase64ValuesArePreserved(string? lexical)
    {
        OdfLexicalValue result = OdfLexicalParser.ParseBase64(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(OdfLexicalFormatter.FormatBase64(result), Is.EqualTo(lexical ?? ""));
        });
    }

    [TestCase("#000000", 0U, "#000000")]
    [TestCase("#abcdef", 0xABCDEFU, "#ABCDEF")]
    [TestCase("#FFFFFF", 0xFFFFFFU, "#FFFFFF")]
    public void ColorValuesParseAndFormatCanonically(string lexical, uint expected, string canonical)
    {
        OdfLexicalValue result = OdfLexicalParser.ParseColor(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.ColorValue, Is.EqualTo(expected));
            Assert.That(OdfLexicalFormatter.FormatColor(result), Is.EqualTo(canonical));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("000000")]
    [TestCase("#fff")]
    [TestCase("#1234567")]
    [TestCase("#GG0000")]
    [TestCase("transparent")]
    public void InvalidColorValuesArePreserved(string? lexical)
    {
        OdfLexicalValue result = OdfLexicalParser.ParseColor(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(OdfLexicalFormatter.FormatColor(result), Is.EqualTo(lexical ?? ""));
        });
    }

    [Test]
    public void NullFormatInputsProduceEmptyText()
    {
        Assert.Multiple(() =>
        {
            Assert.That(OdfLexicalFormatter.FormatBoolean(null), Is.Empty);
            Assert.That(OdfLexicalFormatter.FormatInteger(null), Is.Empty);
            Assert.That(OdfLexicalFormatter.FormatDecimal(null), Is.Empty);
            Assert.That(OdfLexicalFormatter.FormatFloat(null), Is.Empty);
            Assert.That(OdfLexicalFormatter.FormatPercentage(null), Is.Empty);
            Assert.That(OdfLexicalFormatter.FormatBase64(null), Is.Empty);
            Assert.That(OdfLexicalFormatter.FormatColor(null), Is.Empty);
        });
    }
}
