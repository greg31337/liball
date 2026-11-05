namespace json.Tests;

public class JsonNumberFormatterTests
{
    [Test]
    public void SignedAndUnsignedIntegersUseInvariantBoundaryDigits()
    {
        JsonNumberFormatter formatter = CreateFormatter(null);

        Assert.Multiple(() =>
        {
            Assert.That(Text(formatter.FormatInt64(long.MinValue)),
                Is.EqualTo("-9223372036854775808"));
            Assert.That(Text(formatter.FormatInt64(long.MaxValue)),
                Is.EqualTo("9223372036854775807"));
            Assert.That(Text(formatter.FormatUInt64(0)), Is.EqualTo("0"));
            Assert.That(Text(formatter.FormatUInt64(ulong.MaxValue)),
                Is.EqualTo("18446744073709551615"));
        });
    }

    [Test]
    public void DecimalFormattingPreservesScaleAndSignedZero()
    {
        JsonNumberFormatter formatter = CreateFormatter(null);
        decimal scaled = new decimal(12300, 0, 0, false, 4);
        decimal negativeZero = new decimal(0, 0, 0, true, 2);

        Assert.Multiple(() =>
        {
            Assert.That(Text(formatter.FormatDecimal(scaled)),
                Is.EqualTo("1.2300"));
            Assert.That(Text(formatter.FormatDecimal(negativeZero)),
                Is.EqualTo("-0.00"));
            Assert.That(Text(formatter.FormatDecimal(decimal.MinValue)),
                Is.EqualTo("-79228162514264337593543950335"));
            Assert.That(Text(formatter.FormatDecimal(decimal.MaxValue)),
                Is.EqualTo("79228162514264337593543950335"));
        });
    }

    [Test]
    public void DoubleFormattingRoundTripsFiniteValuesAndPreservesNegativeZero()
    {
        JsonNumberFormatter formatter = CreateFormatter(null);
        string negativeZero = Text(formatter.FormatDouble(-0.0));
        double reparsed = double.Parse(
            negativeZero,
            System.Globalization.CultureInfo.InvariantCulture);

        Assert.Multiple(() =>
        {
            Assert.That(Text(formatter.FormatDouble(1.5)), Is.EqualTo("1.5"));
            Assert.That(negativeZero, Is.EqualTo("-0"));
            Assert.That(BitConverter.DoubleToInt64Bits(reparsed),
                Is.EqualTo(BitConverter.DoubleToInt64Bits(-0.0)));
            Assert.That(
                double.Parse(
                    Text(formatter.FormatDouble(double.Epsilon)),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture),
                Is.EqualTo(double.Epsilon));
            Assert.That(
                double.Parse(
                    Text(formatter.FormatDouble(double.MaxValue)),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture),
                Is.EqualTo(double.MaxValue));
        });
    }

    [Test]
    public void DoubleFormattingRejectsEveryNonFiniteValueAndRemainsReusable()
    {
        JsonNumberFormatter formatter = CreateFormatter(null);

        foreach (double value in new double[]
        {
            double.NaN,
            double.PositiveInfinity,
            double.NegativeInfinity
        })
        {
            JsonWriteException failure = Assert.Throws<JsonWriteException>(() =>
                formatter.FormatDouble(value))
                ?? throw new InvalidOperationException(
                    "Expected JsonWriteException.");
            Assert.That(failure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.NonFiniteNumber));
        }

        Assert.That(Text(formatter.FormatDouble(0.25)), Is.EqualTo("0.25"));
    }

    [Test]
    public void ExactLexemesAreValidatedAndPreservedByteForByte()
    {
        JsonNumberFormatter formatter = CreateFormatter(null);
        string[] valid =
        {
            "0",
            "-0.00E+02",
            "123456789012345678901234567890",
            "1e-999"
        };

        foreach (string lexeme in valid)
        {
            Assert.That(Text(formatter.FormatExactLexeme(lexeme)),
                Is.EqualTo(lexeme));
        }

        foreach (string lexeme in new string[]
        {
            "",
            "+1",
            "01",
            "1.",
            "1e",
            "NaN",
            "1,5",
            "１２"
        })
        {
            JsonWriteException failure = Assert.Throws<JsonWriteException>(() =>
                formatter.FormatExactLexeme(lexeme))
                ?? throw new InvalidOperationException(
                    "Expected JsonWriteException.");
            Assert.That(failure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidNumber), lexeme);
        }
    }

    [Test]
    [NonParallelizable]
    public void NumericFormattingDoesNotDependOnCurrentCulture()
    {
        System.Globalization.CultureInfo original =
            System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture =
                System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            JsonNumberFormatter formatter = CreateFormatter(null);

            Assert.Multiple(() =>
            {
                Assert.That(Text(formatter.FormatDecimal(1234.50m)),
                    Is.EqualTo("1234.50"));
                Assert.That(Text(formatter.FormatDouble(1234.5)),
                    Is.EqualTo("1234.5"));
            });
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = original;
        }
    }

    [Test]
    public void NumberAndOutputLimitsApplyBeforeReturningBytes()
    {
        JsonLimits numberLimits = new JsonLimits();
        numberLimits.MaximumNumberCharacters = 2;
        JsonNumberFormatter numberFormatter = CreateFormatter(numberLimits);
        JsonLimits outputLimits = new JsonLimits();
        outputLimits.MaximumOutputBytes = 2;
        JsonNumberFormatter outputFormatter = CreateFormatter(outputLimits);

        JsonLimitException numberFailure = Assert.Throws<JsonLimitException>(() =>
            numberFormatter.FormatInt64(100))
            ?? throw new InvalidOperationException(
                "Expected JsonLimitException.");
        JsonLimitException exactFailure = Assert.Throws<JsonLimitException>(() =>
            numberFormatter.FormatExactLexeme("1e2"))
            ?? throw new InvalidOperationException(
                "Expected JsonLimitException.");
        JsonLimitException outputFailure = Assert.Throws<JsonLimitException>(() =>
            outputFormatter.FormatDouble(1.5))
            ?? throw new InvalidOperationException(
                "Expected JsonLimitException.");

        Assert.Multiple(() =>
        {
            Assert.That(numberFailure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.NumberCharacterLimitExceeded));
            Assert.That(exactFailure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.NumberCharacterLimitExceeded));
            Assert.That(outputFailure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.OutputByteLimitExceeded));
        });
    }

    static JsonNumberFormatter CreateFormatter(JsonLimits? limits)
    {
        JsonWriterOptions options = new JsonWriterOptions();
        options.Limits = limits;
        return new JsonNumberFormatter(options);
    }

    static string Text(byte[] bytes)
    {
        return System.Text.Encoding.ASCII.GetString(bytes);
    }
}
