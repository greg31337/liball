namespace json.Tests;

public class JsonCanonicalNumberFormatterTests
{
    [TestCase("0000000000000000", "0")]
    [TestCase("8000000000000000", "0")]
    [TestCase("0000000000000001", "5e-324")]
    [TestCase("8000000000000001", "-5e-324")]
    [TestCase("7fefffffffffffff", "1.7976931348623157e+308")]
    [TestCase("ffefffffffffffff", "-1.7976931348623157e+308")]
    [TestCase("4340000000000000", "9007199254740992")]
    [TestCase("c340000000000000", "-9007199254740992")]
    [TestCase("4430000000000000", "295147905179352830000")]
    [TestCase("44b52d02c7e14af5", "9.999999999999997e+22")]
    [TestCase("44b52d02c7e14af6", "1e+23")]
    [TestCase("44b52d02c7e14af7", "1.0000000000000001e+23")]
    [TestCase("444b1ae4d6e2ef4e", "999999999999999700000")]
    [TestCase("444b1ae4d6e2ef4f", "999999999999999900000")]
    [TestCase("444b1ae4d6e2ef50", "1e+21")]
    [TestCase("3eb0c6f7a0b5ed8c", "9.999999999999997e-7")]
    [TestCase("3eb0c6f7a0b5ed8d", "0.000001")]
    [TestCase("41b3de4355555553", "333333333.3333332")]
    [TestCase("41b3de4355555554", "333333333.33333325")]
    [TestCase("41b3de4355555555", "333333333.3333333")]
    [TestCase("41b3de4355555556", "333333333.3333334")]
    [TestCase("41b3de4355555557", "333333333.33333343")]
    [TestCase("becbf647612f3696", "-0.0000033333333333333333")]
    [TestCase("43143ff3c1cb0959", "1424953923781206.2")]
    public void Rfc8785AppendixBFiniteVectorsMatchExactly(
        string hexadecimalBits,
        string expected)
    {
        JsonCanonicalNumberFormatter formatter = CreateFormatter(null);

        string actual = Text(formatter.FormatDouble(
            DoubleFromBits(hexadecimalBits)));

        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase("000fffffffffffff", "2.225073858507201e-308")]
    [TestCase("0010000000000000", "2.2250738585072014e-308")]
    [TestCase("800fffffffffffff", "-2.225073858507201e-308")]
    [TestCase("8010000000000000", "-2.2250738585072014e-308")]
    public void NormalAndSubnormalBoundaryUsesShortestRoundTripSpelling(
        string hexadecimalBits,
        string expected)
    {
        JsonCanonicalNumberFormatter formatter = CreateFormatter(null);

        string actual = Text(formatter.FormatDouble(
            DoubleFromBits(hexadecimalBits)));

        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase("7fffffffffffffff")]
    [TestCase("7ff0000000000000")]
    [TestCase("7ff8000000000000")]
    [TestCase("fff0000000000000")]
    public void AppendixBAndRelatedNonFinitePatternsAreRejected(
        string hexadecimalBits)
    {
        JsonCanonicalNumberFormatter formatter = CreateFormatter(null);

        JsonWriteException failure = Assert.Throws<JsonWriteException>(() =>
            formatter.FormatDouble(DoubleFromBits(hexadecimalBits)))
            ?? throw new InvalidOperationException(
                "Expected JsonWriteException.");

        Assert.That(failure.Diagnostic.Code,
            Is.EqualTo(JsonDiagnosticCode.NonFiniteNumber));
    }

    [Test]
    public void SignedZeroAndFiniteExtremesUseEcmaScriptSpellings()
    {
        JsonCanonicalNumberFormatter formatter = CreateFormatter(null);

        Assert.Multiple(() =>
        {
            Assert.That(Text(formatter.FormatDouble(0.0)), Is.EqualTo("0"));
            Assert.That(Text(formatter.FormatDouble(-0.0)), Is.EqualTo("0"));
            Assert.That(Text(formatter.FormatDouble(double.Epsilon)),
                Is.EqualTo("5e-324"));
            Assert.That(Text(formatter.FormatDouble(-double.Epsilon)),
                Is.EqualTo("-5e-324"));
            Assert.That(Text(formatter.FormatDouble(double.MaxValue)),
                Is.EqualTo("1.7976931348623157e+308"));
            Assert.That(Text(formatter.FormatDouble(double.MinValue)),
                Is.EqualTo("-1.7976931348623157e+308"));
        });
    }

    [Test]
    public void FixedAndScientificWindowsMatchEcmaScriptBoundaries()
    {
        JsonCanonicalNumberFormatter formatter = CreateFormatter(null);

        Assert.Multiple(() =>
        {
            Assert.That(Text(formatter.FormatDouble(1e-7)),
                Is.EqualTo("1e-7"));
            Assert.That(Text(formatter.FormatDouble(1e-6)),
                Is.EqualTo("0.000001"));
            Assert.That(Text(formatter.FormatDouble(1e-5)),
                Is.EqualTo("0.00001"));
            Assert.That(Text(formatter.FormatDouble(1e20)),
                Is.EqualTo("100000000000000000000"));
            Assert.That(Text(formatter.FormatDouble(1e21)),
                Is.EqualTo("1e+21"));
            Assert.That(Text(formatter.FormatDouble(1e30)),
                Is.EqualTo("1e+30"));
        });
    }

    [Test]
    public void ShortestDigitsAreRelayoutedWithoutChangingTheirValue()
    {
        JsonCanonicalNumberFormatter formatter = CreateFormatter(null);
        long[] bitPatterns = new long[]
        {
            0x3eb0c6f7a0b5ed8c,
            0x3eb0c6f7a0b5ed8d,
            0x41b3de4355555553,
            0x41b3de4355555554,
            0x41b3de4355555555,
            0x41b3de4355555556,
            0x41b3de4355555557,
            unchecked((long)0xbecbf647612f3696),
            0x43143ff3c1cb0959,
            0x4430000000000000
        };

        foreach (long bits in bitPatterns)
        {
            double value = BitConverter.Int64BitsToDouble(bits);
            string text = Text(formatter.FormatDouble(value));
            double reparsed = double.Parse(
                text,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture);
            Assert.That(
                BitConverter.DoubleToInt64Bits(reparsed),
                Is.EqualTo(bits),
                text);
        }

        Assert.Multiple(() =>
        {
            Assert.That(
                FormatBits(formatter, 0x4430000000000000),
                Is.EqualTo("295147905179352830000"));
            Assert.That(
                FormatBits(formatter, 0x43143ff3c1cb0959),
                Is.EqualTo("1424953923781206.2"));
            Assert.That(
                FormatBits(formatter, 0x3eb0c6f7a0b5ed8c),
                Is.EqualTo("9.999999999999997e-7"));
            Assert.That(
                FormatBits(formatter, 0x3eb0c6f7a0b5ed8d),
                Is.EqualTo("0.000001"));
        });
    }

    [Test]
    public void NonFiniteValuesAreRejectedAndFormatterRemainsReusable()
    {
        JsonCanonicalNumberFormatter formatter = CreateFormatter(null);

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

        Assert.That(Text(formatter.FormatDouble(4.5)), Is.EqualTo("4.5"));
    }

    [Test]
    [NonParallelizable]
    public void CanonicalFormattingIsCultureIndependent()
    {
        System.Globalization.CultureInfo original =
            System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture =
                System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            JsonCanonicalNumberFormatter formatter = CreateFormatter(null);

            Assert.Multiple(() =>
            {
                Assert.That(Text(formatter.FormatDouble(333333333.3333333)),
                    Is.EqualTo("333333333.3333333"));
                Assert.That(Text(formatter.FormatDouble(0.002)),
                    Is.EqualTo("0.002"));
                Assert.That(Text(formatter.FormatDouble(1e-27)),
                    Is.EqualTo("1e-27"));
            });
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = original;
        }
    }

    [Test]
    public void NumberAndOutputLimitsApplyToCanonicalSpelling()
    {
        JsonLimits numberLimits = new JsonLimits();
        numberLimits.MaximumNumberCharacters = 2;
        JsonCanonicalNumberFormatter numberFormatter =
            CreateFormatter(numberLimits);
        JsonLimits outputLimits = new JsonLimits();
        outputLimits.MaximumOutputBytes = 2;
        JsonCanonicalNumberFormatter outputFormatter =
            CreateFormatter(outputLimits);

        JsonLimitException numberFailure =
            Assert.Throws<JsonLimitException>(() =>
                numberFormatter.FormatDouble(100))
            ?? throw new InvalidOperationException(
                "Expected JsonLimitException.");
        JsonLimitException outputFailure =
            Assert.Throws<JsonLimitException>(() =>
                outputFormatter.FormatDouble(1.5))
            ?? throw new InvalidOperationException(
                "Expected JsonLimitException.");

        Assert.Multiple(() =>
        {
            Assert.That(numberFailure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.NumberCharacterLimitExceeded));
            Assert.That(outputFailure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.OutputByteLimitExceeded));
        });
    }

    static string FormatBits(
        JsonCanonicalNumberFormatter formatter,
        long bits)
    {
        return Text(formatter.FormatDouble(
            BitConverter.Int64BitsToDouble(bits)));
    }

    static double DoubleFromBits(string hexadecimalBits)
    {
        ulong bits = ulong.Parse(
            hexadecimalBits,
            System.Globalization.NumberStyles.HexNumber,
            System.Globalization.CultureInfo.InvariantCulture);
        return BitConverter.Int64BitsToDouble(unchecked((long)bits));
    }

    static JsonCanonicalNumberFormatter CreateFormatter(JsonLimits? limits)
    {
        return new JsonCanonicalNumberFormatter(limits);
    }

    static string Text(byte[] bytes)
    {
        return System.Text.Encoding.ASCII.GetString(bytes);
    }
}
