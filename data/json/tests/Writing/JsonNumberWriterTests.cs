namespace json.Tests;

public class JsonNumberWriterTests
{
    [Test]
    public void EveryNumericEntryPointWritesThroughThePublicInterface()
    {
        WriterFixture fixture = new WriterFixture();

        AssertSuccess(fixture.Writer.WriteStartArray());
        AssertSuccess(fixture.Writer.WriteNumberLexeme("-0.00E+02"));
        AssertSuccess(fixture.Writer.WriteInt64(-42));
        AssertSuccess(fixture.Writer.WriteUInt64(42));
        AssertSuccess(fixture.Writer.WriteDecimal(1.2300m));
        AssertSuccess(fixture.Writer.WriteDouble(1.25));
        AssertSuccess(fixture.Writer.WriteEndArray());
        AssertSuccess(fixture.Writer.CompleteWriter());

        Assert.That(
            fixture.Text(),
            Is.EqualTo("[-0.00E+02,-42,42,1.2300,1.25]"));
    }

    [Test]
    public void IntegerEntryPointsWriteEverySignedAndUnsignedBoundary()
    {
        WriterFixture fixture = new WriterFixture();

        AssertSuccess(fixture.Writer.WriteStartArray());
        AssertSuccess(fixture.Writer.WriteInt64(long.MinValue));
        AssertSuccess(fixture.Writer.WriteInt64(-1));
        AssertSuccess(fixture.Writer.WriteInt64(0));
        AssertSuccess(fixture.Writer.WriteInt64(1));
        AssertSuccess(fixture.Writer.WriteInt64(long.MaxValue));
        AssertSuccess(fixture.Writer.WriteUInt64(0));
        AssertSuccess(fixture.Writer.WriteUInt64(1));
        AssertSuccess(fixture.Writer.WriteUInt64(ulong.MaxValue));
        AssertSuccess(fixture.Writer.WriteEndArray());
        AssertSuccess(fixture.Writer.CompleteWriter());

        Assert.That(
            fixture.Text(),
            Is.EqualTo(
                "[-9223372036854775808,-1,0,1,9223372036854775807," +
                "0,1,18446744073709551615]"));
    }

    [Test]
    public void DecimalEntryPointPreservesBoundariesScaleAndNegativeZero()
    {
        WriterFixture fixture = new WriterFixture();
        decimal smallestPositive = new decimal(1, 0, 0, false, 28);
        decimal scaled = new decimal(12300, 0, 0, false, 4);
        decimal positiveZero = new decimal(0, 0, 0, false, 4);
        decimal negativeZero = new decimal(0, 0, 0, true, 4);

        AssertSuccess(fixture.Writer.WriteStartArray());
        AssertSuccess(fixture.Writer.WriteDecimal(decimal.MinValue));
        AssertSuccess(fixture.Writer.WriteDecimal(smallestPositive));
        AssertSuccess(fixture.Writer.WriteDecimal(scaled));
        AssertSuccess(fixture.Writer.WriteDecimal(positiveZero));
        AssertSuccess(fixture.Writer.WriteDecimal(negativeZero));
        AssertSuccess(fixture.Writer.WriteDecimal(decimal.MaxValue));
        AssertSuccess(fixture.Writer.WriteEndArray());
        AssertSuccess(fixture.Writer.CompleteWriter());

        Assert.That(
            fixture.Text(),
            Is.EqualTo(
                "[-79228162514264337593543950335," +
                "0.0000000000000000000000000001,1.2300," +
                "0.0000,-0.0000,79228162514264337593543950335]"));
    }

    [Test]
    public void DoubleEntryPointRoundTripsFiniteBoundaryBitPatterns()
    {
        double[] values = new double[]
        {
            double.MinValue,
            -double.Epsilon,
            -0.0,
            0.0,
            double.Epsilon,
            double.MaxValue
        };

        foreach (double value in values)
        {
            string lexeme = WriteSingleDouble(value);
            double reparsed = double.Parse(
                lexeme,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture);
            Assert.That(
                BitConverter.DoubleToInt64Bits(reparsed),
                Is.EqualTo(BitConverter.DoubleToInt64Bits(value)),
                lexeme);
        }

        Assert.Multiple(() =>
        {
            Assert.That(WriteSingleDouble(double.MinValue),
                Is.EqualTo("-1.7976931348623157E+308"));
            Assert.That(WriteSingleDouble(-double.Epsilon),
                Is.EqualTo("-5E-324"));
            Assert.That(WriteSingleDouble(-0.0), Is.EqualTo("-0"));
            Assert.That(WriteSingleDouble(0.0), Is.EqualTo("0"));
            Assert.That(WriteSingleDouble(double.Epsilon),
                Is.EqualTo("5E-324"));
            Assert.That(WriteSingleDouble(double.MaxValue),
                Is.EqualTo("1.7976931348623157E+308"));
        });
    }

    [Test]
    public void DoubleEntryPointRejectsEveryNonFiniteValueAndRecovers()
    {
        WriterFixture fixture = new WriterFixture();

        AssertSuccess(fixture.Writer.WriteStartArray());
        foreach (double value in new double[]
        {
            double.NaN,
            double.PositiveInfinity,
            double.NegativeInfinity
        })
        {
            JsonOperationResult result = fixture.Writer.WriteDouble(value);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(
                    result.Diagnostic?.Code,
                    Is.EqualTo(JsonDiagnosticCode.NonFiniteNumber));
                Assert.That(result.BytesWritten, Is.Zero);
                Assert.That(fixture.Writer.GetBytesPending(), Is.EqualTo(1));
            });
        }

        AssertSuccess(fixture.Writer.WriteDouble(0.5));
        AssertSuccess(fixture.Writer.WriteEndArray());
        AssertSuccess(fixture.Writer.CompleteWriter());
        Assert.That(fixture.Text(), Is.EqualTo("[0.5]"));
    }

    [Test]
    public void ExactLexemeEntryPointPreservesEveryValidSpelling()
    {
        string[] lexemes = new string[]
        {
            "0",
            "-0",
            "0.0",
            "-0.00E+02",
            "1e-999",
            "1E+999",
            "1234567890123456789012345678901234567890",
            "0.0000000000000000000000000000000000000001"
        };

        foreach (string lexeme in lexemes)
        {
            WriterFixture fixture = new WriterFixture();
            JsonOperationResult result =
                fixture.Writer.WriteNumberLexeme(lexeme);
            AssertSuccess(result);
            Assert.That(result.BytesWritten, Is.EqualTo(lexeme.Length));
            AssertSuccess(fixture.Writer.CompleteWriter());
            Assert.That(fixture.Text(), Is.EqualTo(lexeme));
        }
    }

    [Test]
    public void ExactLexemeEntryPointRejectsEveryInvalidGrammarFamilyAndRecovers()
    {
        string[] lexemes = new string[]
        {
            null!,
            "",
            " ",
            "+1",
            "-",
            "00",
            "01",
            "-01",
            ".1",
            "1.",
            "1e",
            "1e+",
            "1e-",
            "1 2",
            "1,5",
            "NaN",
            "Infinity",
            "１２"
        };

        WriterFixture fixture = new WriterFixture();
        AssertSuccess(fixture.Writer.WriteStartArray());
        foreach (string lexeme in lexemes)
        {
            JsonOperationResult result =
                fixture.Writer.WriteNumberLexeme(lexeme);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False, lexeme);
                Assert.That(
                    result.Diagnostic?.Code,
                    Is.EqualTo(JsonDiagnosticCode.InvalidNumber),
                    lexeme);
                Assert.That(result.BytesWritten, Is.Zero, lexeme);
                Assert.That(fixture.Writer.GetBytesPending(), Is.EqualTo(1));
            });
        }

        AssertSuccess(fixture.Writer.WriteNumberLexeme("1e2"));
        AssertSuccess(fixture.Writer.WriteEndArray());
        AssertSuccess(fixture.Writer.CompleteWriter());
        Assert.That(fixture.Text(), Is.EqualTo("[1e2]"));
    }

    [Test]
    [NonParallelizable]
    public void EveryNumericEntryPointIsIndependentOfCurrentCulture()
    {
        System.Globalization.CultureInfo originalCulture =
            System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo originalUiCulture =
            System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture =
                System.Globalization.CultureInfo.GetCultureInfo("ar-SA");
            System.Globalization.CultureInfo.CurrentUICulture =
                System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            WriterFixture fixture = new WriterFixture();

            AssertSuccess(fixture.Writer.WriteStartArray());
            AssertSuccess(fixture.Writer.WriteNumberLexeme("1234.50e-2"));
            AssertSuccess(fixture.Writer.WriteInt64(-1234));
            AssertSuccess(fixture.Writer.WriteUInt64(1234));
            AssertSuccess(fixture.Writer.WriteDecimal(1234.50m));
            AssertSuccess(fixture.Writer.WriteDouble(1234.5));
            AssertSuccess(fixture.Writer.WriteEndArray());
            AssertSuccess(fixture.Writer.CompleteWriter());

            Assert.That(
                fixture.Text(),
                Is.EqualTo("[1234.50e-2,-1234,1234,1234.50,1234.5]"));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
            System.Globalization.CultureInfo.CurrentUICulture =
                originalUiCulture;
        }
    }

    static string WriteSingleDouble(double value)
    {
        WriterFixture fixture = new WriterFixture();
        AssertSuccess(fixture.Writer.WriteDouble(value));
        AssertSuccess(fixture.Writer.CompleteWriter());
        return fixture.Text();
    }

    static void AssertSuccess(JsonOperationResult result)
    {
        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
    }

    sealed class WriterFixture
    {
        internal System.Buffers.ArrayBufferWriter<byte> Destination;
        internal IJsonTokenWriter Writer;

        internal WriterFixture()
        {
            Destination = new System.Buffers.ArrayBufferWriter<byte>();
            JsonWriterOptions options = new JsonWriterOptions();
            JsonWriterTarget target = new JsonWriterTarget();
            target.BufferWriter = Destination;
            Writer = new JsonUtf8Writer(options, target);
        }

        internal string Text()
        {
            return System.Text.Encoding.UTF8.GetString(
                Destination.WrittenSpan);
        }
    }
}
