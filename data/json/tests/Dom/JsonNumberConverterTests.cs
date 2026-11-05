namespace json.Tests;

public class JsonNumberConverterTests
{
    [Test]
    public void ThrowingIntegerConversionsAcceptTheirExactBoundaries()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        IJsonNumberConverter converter = new JsonNumberConverter();

        long minimum = converter.ToInt64(factory.CreateNumber("-9223372036854775808").Number!);
        long maximum = converter.ToInt64(factory.CreateNumber("9223372036854775807").Number!);
        ulong unsignedMaximum = converter.ToUInt64(
            factory.CreateNumber("18446744073709551615").Number!);
        ulong negativeZero = converter.ToUInt64(factory.CreateNumber("-0").Number!);

        Assert.Multiple(() =>
        {
            Assert.That(minimum, Is.EqualTo(long.MinValue));
            Assert.That(maximum, Is.EqualTo(long.MaxValue));
            Assert.That(unsignedMaximum, Is.EqualTo(ulong.MaxValue));
            Assert.That(negativeZero, Is.Zero);
        });
    }

    [Test]
    [NonParallelizable]
    public void DecimalAndDoubleConversionsUseInvariantFullLexemes()
    {
        global::System.Globalization.CultureInfo original =
            global::System.Globalization.CultureInfo.CurrentCulture;
        global::System.Globalization.CultureInfo.CurrentCulture =
            new global::System.Globalization.CultureInfo("fr-FR");
        try
        {
            IJsonValueFactory factory = new JsonValueFactory();
            IJsonNumberConverter converter = new JsonNumberConverter();

            decimal decimalValue = converter.ToDecimal(
                factory.CreateNumber("1234.50e-1").Number!);
            double doubleValue = converter.ToDouble(
                factory.CreateNumber("6.25E+2").Number!);

            Assert.Multiple(() =>
            {
                Assert.That(decimalValue, Is.EqualTo(123.450m));
                Assert.That(doubleValue, Is.EqualTo(625.0d));
            });
        }
        finally
        {
            global::System.Globalization.CultureInfo.CurrentCulture = original;
        }
    }

    [Test]
    public void DoubleConversionPreservesNegativeZeroAndRejectsInfinity()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        IJsonNumberConverter converter = new JsonNumberConverter();

        double negativeZero = converter.ToDouble(factory.CreateNumber("-0.0e+20").Number!);
        JsonParseException? overflow = Assert.Throws<JsonParseException>(() =>
            converter.ToDouble(factory.CreateNumber("1e400").Number!));

        Assert.Multiple(() =>
        {
            Assert.That(negativeZero, Is.Zero);
            Assert.That(global::System.BitConverter.DoubleToInt64Bits(negativeZero),
                Is.EqualTo(long.MinValue));
            Assert.That(overflow?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.NumericConversionFailed));
        });
    }

    [Test]
    public void ThrowingConversionsReportLexicalFormSignAndRangeFailures()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        IJsonNumberConverter converter = new JsonNumberConverter();

        JsonParseException? fractionalInteger = Assert.Throws<JsonParseException>(() =>
            converter.ToInt64(factory.CreateNumber("1.0").Number!));
        JsonParseException? signedOverflow = Assert.Throws<JsonParseException>(() =>
            converter.ToInt64(factory.CreateNumber("9223372036854775808").Number!));
        JsonParseException? negativeUnsigned = Assert.Throws<JsonParseException>(() =>
            converter.ToUInt64(factory.CreateNumber("-1").Number!));
        JsonParseException? decimalOverflow = Assert.Throws<JsonParseException>(() =>
            converter.ToDecimal(
                factory.CreateNumber("79228162514264337593543950336").Number!));

        Assert.Multiple(() =>
        {
            AssertConversionFailure(fractionalInteger);
            AssertConversionFailure(signedOverflow);
            AssertConversionFailure(negativeUnsigned);
            AssertConversionFailure(decimalOverflow);
        });
    }

    [Test]
    public void ThrowingConversionsRejectMalformedMutableNumberModels()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonNumber invalidLexeme = factory.CreateNumber("1").Number!;
        invalidLexeme.Lexeme = "1x";
        JsonNumber invalidClassification = factory.CreateNumber("1e2").Number!;
        invalidClassification.HasExponent = false;
        IJsonNumberConverter converter = new JsonNumberConverter();

        JsonParseException? nullNumber = Assert.Throws<JsonParseException>(() =>
            converter.ToInt64(null!));
        JsonParseException? badLexeme = Assert.Throws<JsonParseException>(() =>
            converter.ToDecimal(invalidLexeme));
        JsonParseException? badClassification = Assert.Throws<JsonParseException>(() =>
            converter.ToDouble(invalidClassification));

        Assert.Multiple(() =>
        {
            Assert.That(nullNumber?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(badLexeme?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(badClassification?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
        });
    }

    [Test]
    public void ExceptionLessConversionsReturnExactValuesWithoutDiagnostics()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        IJsonNumberConverter converter = new JsonNumberConverter();

        JsonInt64Result signed = converter.TryInt64(factory.CreateNumber("-42").Number!);
        JsonUInt64Result unsigned = converter.TryUInt64(
            factory.CreateNumber("18446744073709551615").Number!);
        JsonDecimalResult decimalResult = converter.TryDecimal(
            factory.CreateNumber("123.450e-1").Number!);
        JsonDoubleResult doubleResult = converter.TryDouble(
            factory.CreateNumber("-0.0e+20").Number!);

        Assert.Multiple(() =>
        {
            Assert.That(signed.Success, Is.True);
            Assert.That(signed.Value, Is.EqualTo(-42));
            Assert.That(signed.Diagnostic, Is.Null);
            Assert.That(unsigned.Success, Is.True);
            Assert.That(unsigned.Value, Is.EqualTo(ulong.MaxValue));
            Assert.That(unsigned.Diagnostic, Is.Null);
            Assert.That(decimalResult.Success, Is.True);
            Assert.That(decimalResult.Value, Is.EqualTo(12.3450m));
            Assert.That(decimalResult.Diagnostic, Is.Null);
            Assert.That(doubleResult.Success, Is.True);
            Assert.That(doubleResult.Value, Is.Zero);
            Assert.That(global::System.BitConverter.DoubleToInt64Bits(doubleResult.Value),
                Is.EqualTo(long.MinValue));
            Assert.That(doubleResult.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void ExceptionLessFailuresHaveZeroValuesAndThrowingDiagnosticParity()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonNumber signedNumber = factory.CreateNumber("9223372036854775808").Number!;
        JsonNumber unsignedNumber = factory.CreateNumber("-1").Number!;
        JsonNumber decimalNumber = factory.CreateNumber(
            "79228162514264337593543950336").Number!;
        JsonNumber doubleNumber = factory.CreateNumber("1e400").Number!;
        IJsonNumberConverter converter = new JsonNumberConverter();

        JsonInt64Result signed = converter.TryInt64(signedNumber);
        JsonParseException? signedFailure = Assert.Throws<JsonParseException>(() =>
            converter.ToInt64(signedNumber));
        JsonUInt64Result unsigned = converter.TryUInt64(unsignedNumber);
        JsonParseException? unsignedFailure = Assert.Throws<JsonParseException>(() =>
            converter.ToUInt64(unsignedNumber));
        JsonDecimalResult decimalResult = converter.TryDecimal(decimalNumber);
        JsonParseException? decimalFailure = Assert.Throws<JsonParseException>(() =>
            converter.ToDecimal(decimalNumber));
        JsonDoubleResult doubleResult = converter.TryDouble(doubleNumber);
        JsonParseException? doubleFailure = Assert.Throws<JsonParseException>(() =>
            converter.ToDouble(doubleNumber));

        Assert.Multiple(() =>
        {
            Assert.That(signed.Success, Is.False);
            Assert.That(signed.Value, Is.Zero);
            AssertDiagnosticParity(signed.Diagnostic, signedFailure);
            Assert.That(unsigned.Success, Is.False);
            Assert.That(unsigned.Value, Is.Zero);
            AssertDiagnosticParity(unsigned.Diagnostic, unsignedFailure);
            Assert.That(decimalResult.Success, Is.False);
            Assert.That(decimalResult.Value, Is.Zero);
            AssertDiagnosticParity(decimalResult.Diagnostic, decimalFailure);
            Assert.That(doubleResult.Success, Is.False);
            Assert.That(doubleResult.Value, Is.Zero);
            AssertDiagnosticParity(doubleResult.Diagnostic, doubleFailure);
        });
    }

    [Test]
    public void ExceptionLessMalformedModelFailureMatchesTheThrowingDiagnostic()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonNumber number = factory.CreateNumber("1").Number!;
        number.Lexeme = "1 trailing";
        IJsonNumberConverter converter = new JsonNumberConverter();

        JsonInt64Result result = converter.TryInt64(number);
        JsonParseException? failure = Assert.Throws<JsonParseException>(() =>
            converter.ToInt64(number));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Value, Is.Zero);
            Assert.That(result.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            AssertDiagnosticParity(result.Diagnostic, failure);
        });
    }

    [Test]
    public void IntegerConversionsRejectEveryOneStepOverflowBoundary()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        IJsonNumberConverter converter = new JsonNumberConverter();

        JsonParseException? belowSignedMinimum = Assert.Throws<JsonParseException>(() =>
            converter.ToInt64(
                factory.CreateNumber("-9223372036854775809").Number!));
        JsonParseException? aboveSignedMaximum = Assert.Throws<JsonParseException>(() =>
            converter.ToInt64(
                factory.CreateNumber("9223372036854775808").Number!));
        JsonParseException? aboveUnsignedMaximum = Assert.Throws<JsonParseException>(() =>
            converter.ToUInt64(
                factory.CreateNumber("18446744073709551616").Number!));
        JsonParseException? negativeUnsigned = Assert.Throws<JsonParseException>(() =>
            converter.ToUInt64(factory.CreateNumber("-1").Number!));

        Assert.Multiple(() =>
        {
            AssertConversionFailure(belowSignedMinimum);
            AssertConversionFailure(aboveSignedMaximum);
            AssertConversionFailure(aboveUnsignedMaximum);
            AssertConversionFailure(negativeUnsigned);
        });
    }

    [Test]
    public void DecimalConversionCoversRangePrecisionAndScaleBoundaries()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        IJsonNumberConverter converter = new JsonNumberConverter();

        decimal minimum = converter.ToDecimal(
            factory.CreateNumber("-79228162514264337593543950335").Number!);
        decimal maximum = converter.ToDecimal(
            factory.CreateNumber("79228162514264337593543950335").Number!);
        decimal rounded = converter.ToDecimal(factory.CreateNumber(
            "0.123456789012345678901234567890").Number!);
        decimal scaled = converter.ToDecimal(factory.CreateNumber("1.2300").Number!);
        int[] scaledBits = decimal.GetBits(scaled);
        int scale = (scaledBits[3] >> 16) & 0x7F;
        JsonParseException? belowMinimum = Assert.Throws<JsonParseException>(() =>
            converter.ToDecimal(factory.CreateNumber(
                "-79228162514264337593543950336").Number!));
        JsonParseException? aboveMaximum = Assert.Throws<JsonParseException>(() =>
            converter.ToDecimal(factory.CreateNumber(
                "79228162514264337593543950336").Number!));

        Assert.Multiple(() =>
        {
            Assert.That(minimum, Is.EqualTo(decimal.MinValue));
            Assert.That(maximum, Is.EqualTo(decimal.MaxValue));
            Assert.That(rounded,
                Is.EqualTo(0.1234567890123456789012345679m));
            Assert.That(scaled, Is.EqualTo(1.2300m));
            Assert.That(scale, Is.EqualTo(4));
            AssertConversionFailure(belowMinimum);
            AssertConversionFailure(aboveMaximum);
        });
    }

    [Test]
    public void DoubleConversionCoversFiniteOverflowAndUnderflowBoundaries()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        IJsonNumberConverter converter = new JsonNumberConverter();

        double maximum = converter.ToDouble(
            factory.CreateNumber("1.7976931348623157e308").Number!);
        double minimumSubnormal = converter.ToDouble(
            factory.CreateNumber("5e-324").Number!);
        double positiveUnderflow = converter.ToDouble(
            factory.CreateNumber("1e-324").Number!);
        double negativeUnderflow = converter.ToDouble(
            factory.CreateNumber("-1e-400").Number!);
        JsonParseException? positiveOverflow = Assert.Throws<JsonParseException>(() =>
            converter.ToDouble(
                factory.CreateNumber("1.7976931348623159e308").Number!));
        JsonParseException? negativeOverflow = Assert.Throws<JsonParseException>(() =>
            converter.ToDouble(
                factory.CreateNumber("-1.7976931348623159e308").Number!));

        Assert.Multiple(() =>
        {
            Assert.That(maximum, Is.EqualTo(double.MaxValue));
            Assert.That(minimumSubnormal, Is.EqualTo(double.Epsilon));
            Assert.That(global::System.BitConverter.DoubleToInt64Bits(positiveUnderflow),
                Is.Zero);
            Assert.That(global::System.BitConverter.DoubleToInt64Bits(negativeUnderflow),
                Is.EqualTo(long.MinValue));
            AssertConversionFailure(positiveOverflow);
            AssertConversionFailure(negativeOverflow);
        });
    }

    [Test]
    public void NegativeZeroIsPreservedWhereTheTargetTypeSupportsIt()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        IJsonNumberConverter converter = new JsonNumberConverter();

        long signed = converter.ToInt64(factory.CreateNumber("-0").Number!);
        ulong unsigned = converter.ToUInt64(factory.CreateNumber("-0").Number!);
        decimal decimalValue = converter.ToDecimal(factory.CreateNumber("-0.00").Number!);
        double doubleValue = converter.ToDouble(factory.CreateNumber("-0.0").Number!);
        int[] decimalBits = decimal.GetBits(decimalValue);
        int decimalScale = (decimalBits[3] >> 16) & 0x7F;

        Assert.Multiple(() =>
        {
            Assert.That(signed, Is.Zero);
            Assert.That(unsigned, Is.Zero);
            Assert.That(decimalValue, Is.Zero);
            Assert.That(decimalBits[3] < 0, Is.True);
            Assert.That(decimalScale, Is.EqualTo(2));
            Assert.That(doubleValue, Is.Zero);
            Assert.That(global::System.BitConverter.DoubleToInt64Bits(doubleValue),
                Is.EqualTo(long.MinValue));
        });
    }

    [Test]
    public void ConversionsConsumeTheCompleteValidatedLexeme()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonNumber exponent = factory.CreateNumber("1e2").Number!;
        JsonNumber trailing = factory.CreateNumber("42").Number!;
        trailing.Lexeme = "42 trailing";
        IJsonNumberConverter converter = new JsonNumberConverter();

        JsonParseException? signedExponent = Assert.Throws<JsonParseException>(() =>
            converter.ToInt64(exponent));
        JsonParseException? unsignedExponent = Assert.Throws<JsonParseException>(() =>
            converter.ToUInt64(exponent));
        decimal decimalExponent = converter.ToDecimal(exponent);
        double doubleExponent = converter.ToDouble(exponent);
        JsonInt64Result signedTrailing = converter.TryInt64(trailing);
        JsonUInt64Result unsignedTrailing = converter.TryUInt64(trailing);
        JsonDecimalResult decimalTrailing = converter.TryDecimal(trailing);
        JsonDoubleResult doubleTrailing = converter.TryDouble(trailing);

        Assert.Multiple(() =>
        {
            AssertConversionFailure(signedExponent);
            AssertConversionFailure(unsignedExponent);
            Assert.That(decimalExponent, Is.EqualTo(100m));
            Assert.That(doubleExponent, Is.EqualTo(100d));
            Assert.That(signedTrailing.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(unsignedTrailing.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(decimalTrailing.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(doubleTrailing.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
        });
    }

    static void AssertDiagnosticParity(
        JsonDiagnostic? diagnostic,
        JsonParseException? failure)
    {
        Assert.That(diagnostic, Is.Not.Null);
        Assert.That(failure, Is.Not.Null);
        Assert.That(diagnostic?.Code, Is.EqualTo(failure?.Diagnostic.Code));
        Assert.That(diagnostic?.Message, Is.EqualTo(failure?.Diagnostic.Message));
        Assert.That(diagnostic?.Range, Is.EqualTo(failure?.Diagnostic.Range));
        Assert.That(diagnostic?.Pointer, Is.EqualTo(failure?.Diagnostic.Pointer));
        Assert.That(diagnostic?.Depth, Is.EqualTo(failure?.Diagnostic.Depth));
        Assert.That(diagnostic?.ExpectedTokens,
            Is.EqualTo(failure?.Diagnostic.ExpectedTokens));
        Assert.That(diagnostic?.ActualToken, Is.EqualTo(failure?.Diagnostic.ActualToken));
        Assert.That(diagnostic?.SourceExcerpt, Is.EqualTo(failure?.Diagnostic.SourceExcerpt));
    }

    static void AssertConversionFailure(JsonParseException? failure)
    {
        Assert.That(failure, Is.Not.Null);
        Assert.That(failure?.Diagnostic.Code,
            Is.EqualTo(JsonDiagnosticCode.NumericConversionFailed));
        Assert.That(failure?.Diagnostic.Message, Is.Not.Empty);
        Assert.That(failure?.Diagnostic.ActualToken, Is.EqualTo(JsonValueKind.Number.ToString()));
    }
}
