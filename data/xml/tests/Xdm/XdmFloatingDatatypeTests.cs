using xml;

namespace xml.Tests;

[TestFixture]
public class XdmFloatingDatatypeTests
{
    [Test]
    public void FormatsSmallFloatStringValuesFromTheirShortestRoundTripForm()
    {
        XdmAtomicValue value = new XdmFloatingDatatype().ParseXdmFloating("1e-5", XdmAtomicType.Float);

        Assert.That(value.StringValue, Is.EqualTo("0.00001"));
    }

    [TestCase(XdmAtomicType.Double, "1", "1.0E0")]
    [TestCase(XdmAtomicType.Double, "+001.2500", "1.25E0")]
    [TestCase(XdmAtomicType.Double, ".5", "5.0E-1")]
    [TestCase(XdmAtomicType.Double, "5.", "5.0E0")]
    [TestCase(XdmAtomicType.Double, " 1e2\n", "1.0E2")]
    [TestCase(XdmAtomicType.Float, "1.25E-2", "1.25E-2")]
    [TestCase(XdmAtomicType.Float, "16777217", "1.6777216E7")]
    [TestCase(XdmAtomicType.Double, "INF", "INF")]
    [TestCase(XdmAtomicType.Float, "+INF", "INF")]
    [TestCase(XdmAtomicType.Double, "-INF", "-INF")]
    [TestCase(XdmAtomicType.Float, "NaN", "NaN")]
    public void ParsesFloatingLexicalFormsAndProducesCanonicalOutput(XdmAtomicType type, string lexical, string canonical)
    {
        XdmAtomicValue value = new XdmFloatingDatatype().ParseXdmFloating(lexical, type);

        Assert.Multiple(() =>
        {
            Assert.That(value.Type, Is.EqualTo(type));
            Assert.That(value.LexicalValue, Is.EqualTo(lexical));
            Assert.That(value.CanonicalValue, Is.EqualTo(canonical));
            Assert.That(value.StringValue, Is.EqualTo(new XdmFloatingDatatype().FormatXdmFloatingStringValue(value.FloatingValue, type)));
            Assert.That(value.IsImmutable, Is.True);
        });
    }

    [TestCase(XdmAtomicType.Float, "3.3", "3.3")]
    [TestCase(XdmAtomicType.Double, "1", "1")]
    [TestCase(XdmAtomicType.Double, "0", "0")]
    [TestCase(XdmAtomicType.Double, "-0", "-0")]
    [TestCase(XdmAtomicType.Double, "0.000001", "0.000001")]
    [TestCase(XdmAtomicType.Double, "0.0000001", "1.0E-7")]
    [TestCase(XdmAtomicType.Double, "1000000", "1.0E6")]
    public void ProducesXPathStringRepresentationsSeparatelyFromCanonicalLexicalForms(XdmAtomicType type, string lexical, string expected)
    {
        XdmAtomicValue value = new XdmFloatingDatatype().ParseXdmFloating(lexical, type);

        Assert.That(value.StringValue, Is.EqualTo(expected));
    }

    [TestCase("")]
    [TestCase(".")]
    [TestCase("e1")]
    [TestCase("1e")]
    [TestCase("1e+")]
    [TestCase("Infinity")]
    [TestCase("nan")]
    [TestCase("+NaN")]
    [TestCase("1 2")]
    [TestCase("0x1")]
    public void RejectsValuesOutsideTheFloatingLexicalSpace(string lexical)
    {
        XdmFloatingDatatype datatype = new XdmFloatingDatatype();
        XdmNumericParseResult result = datatype.TryParseXdmFloating(lexical, XdmAtomicType.Double);
        XPathDynamicException exception = Assert.Throws<XPathDynamicException>(() => datatype.ParseXdmFloating(lexical, XdmAtomicType.Double))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(XPathErrorCodes.FORG0001));
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0001));
        });
    }

    [Test]
    public void PreservesSignedZeroAndMapsOverflowAndUnderflowToIeeeValues()
    {
        XdmFloatingDatatype datatype = new XdmFloatingDatatype();
        XdmAtomicValue positiveZero = datatype.ParseXdmFloating("0", XdmAtomicType.Double);
        XdmAtomicValue negativeZero = datatype.ParseXdmFloating("-0", XdmAtomicType.Double);
        XdmAtomicValue negativeFloatZero = datatype.ParseXdmFloating("-1e-999", XdmAtomicType.Float);
        XdmAtomicValue overflow = datatype.ParseXdmFloating("1e999", XdmAtomicType.Double);
        XdmAtomicValue extremeOverflow = datatype.ParseXdmFloating("1e" + new string('9', 10000), XdmAtomicType.Double);
        XdmAtomicValue extremeUnderflow = datatype.ParseXdmFloating("-1e-" + new string('9', 10000), XdmAtomicType.Double);

        Assert.Multiple(() =>
        {
            Assert.That(BitConverter.DoubleToInt64Bits(positiveZero.FloatingValue), Is.EqualTo(0));
            Assert.That(BitConverter.DoubleToInt64Bits(negativeZero.FloatingValue), Is.EqualTo(long.MinValue));
            Assert.That(negativeZero.CanonicalValue, Is.EqualTo("-0.0E0"));
            Assert.That(BitConverter.SingleToInt32Bits((float)negativeFloatZero.FloatingValue), Is.EqualTo(int.MinValue));
            Assert.That(negativeFloatZero.CanonicalValue, Is.EqualTo("-0.0E0"));
            Assert.That(double.IsPositiveInfinity(overflow.FloatingValue), Is.True);
            Assert.That(overflow.CanonicalValue, Is.EqualTo("INF"));
            Assert.That(extremeOverflow.CanonicalValue, Is.EqualTo("INF"));
            Assert.That(extremeUnderflow.CanonicalValue, Is.EqualTo("-0.0E0"));
        });
    }

    [Test]
    public void CanonicalRepresentationsRoundTripExtremeFiniteValues()
    {
        XdmFloatingDatatype datatype = new XdmFloatingDatatype();
        double[] doubles = [1d, -123.45d, 0.00123d, 1e20d, double.Epsilon, double.MaxValue];
        float[] floats = [1f, -123.45f, 0.00123f, 1e20f, float.Epsilon, float.MaxValue];

        foreach (double number in doubles)
        {
            XdmAtomicValue value = datatype.CreateXdmFloatingValue(number, XdmAtomicType.Double, null);
            XdmAtomicValue reparsed = datatype.ParseXdmFloating(value.CanonicalValue!, XdmAtomicType.Double);
            Assert.That(BitConverter.DoubleToInt64Bits(reparsed.FloatingValue), Is.EqualTo(BitConverter.DoubleToInt64Bits(number)), value.CanonicalValue);
        }

        foreach (float number in floats)
        {
            XdmAtomicValue value = datatype.CreateXdmFloatingValue(number, XdmAtomicType.Float, null);
            XdmAtomicValue reparsed = datatype.ParseXdmFloating(value.CanonicalValue!, XdmAtomicType.Float);
            Assert.That(BitConverter.SingleToInt32Bits((float)reparsed.FloatingValue), Is.EqualTo(BitConverter.SingleToInt32Bits(number)), value.CanonicalValue);
        }
    }

    [Test]
    public void ImplementsUnorderedNanAndSignedZeroComparison()
    {
        XdmFloatingDatatype datatype = new XdmFloatingDatatype();
        XdmNumericDatatype numeric = new XdmNumericDatatype();
        XdmAtomicValue nan = datatype.ParseXdmFloating("NaN", XdmAtomicType.Double);
        XdmAtomicValue negativeZero = datatype.ParseXdmFloating("-0", XdmAtomicType.Float);
        XdmAtomicValue positiveZero = datatype.ParseXdmFloating("0", XdmAtomicType.Double);
        XdmAtomicValue negativeInfinity = datatype.ParseXdmFloating("-INF", XdmAtomicType.Double);
        XdmAtomicValue positiveInfinity = datatype.ParseXdmFloating("INF", XdmAtomicType.Float);

        Assert.Multiple(() =>
        {
            Assert.That(datatype.CompareXdmFloating(nan, nan), Is.EqualTo(XdmFloatingDatatype.UnorderedComparison));
            Assert.That(datatype.EqualXdmFloating(nan, nan), Is.False);
            Assert.That(datatype.LessThanXdmFloating(nan, positiveZero), Is.False);
            Assert.That(datatype.GreaterThanXdmFloating(nan, positiveZero), Is.False);
            Assert.That(datatype.EqualXdmFloating(negativeZero, positiveZero), Is.True);
            Assert.That(datatype.CompareXdmFloating(negativeInfinity, positiveInfinity), Is.LessThan(0));
            Assert.That(numeric.EqualXdmNumeric(nan, nan), Is.False);
            Assert.That(numeric.LessThanXdmNumeric(nan, positiveZero), Is.False);
            Assert.That(numeric.GreaterThanXdmNumeric(nan, positiveZero), Is.False);
            Assert.That(numeric.EqualXdmNumeric(negativeZero, positiveZero), Is.True);
        });
    }

    [Test]
    public void PromotesNumericValuesWithoutAnIntermediateFloatConversion()
    {
        XdmNumericDatatype numeric = new XdmNumericDatatype();
        XdmFloatingDatatype floating = new XdmFloatingDatatype();
        XdmAtomicValue decimalValue = numeric.ParseXdmDecimal("0.1");
        XdmAtomicValue floatValue = floating.ParseXdmFloating("0.1", XdmAtomicType.Float);
        XdmAtomicValue promotedFloat = numeric.PromoteXdmNumeric(decimalValue, XdmAtomicType.Float);
        XdmAtomicValue promotedDouble = numeric.PromoteXdmNumeric(decimalValue, XdmAtomicType.Double);
        XdmAtomicValue widenedFloat = numeric.PromoteXdmNumeric(floatValue, XdmAtomicType.Double);

        Assert.Multiple(() =>
        {
            Assert.That(promotedFloat.Type, Is.EqualTo(XdmAtomicType.Float));
            Assert.That(promotedFloat.CanonicalValue, Is.EqualTo("1.0E-1"));
            Assert.That(promotedDouble.Type, Is.EqualTo(XdmAtomicType.Double));
            Assert.That(promotedDouble.CanonicalValue, Is.EqualTo("1.0E-1"));
            Assert.That(widenedFloat.FloatingValue, Is.EqualTo((double)(float)0.1));
            Assert.That(widenedFloat.CanonicalValue, Is.EqualTo("1.0000000149011612E-1"));
            Assert.That(Assert.Throws<ArgumentException>(() => numeric.PromoteXdmNumeric(promotedDouble, XdmAtomicType.Float)), Is.Not.Null);
        });
    }

    [Test]
    public void AppliesIeeeArithmeticAndNumericPromotion()
    {
        XdmNumericDatatype numeric = new XdmNumericDatatype();
        XdmFloatingDatatype floating = new XdmFloatingDatatype();
        XdmAtomicValue oneFloat = floating.ParseXdmFloating("1", XdmAtomicType.Float);
        XdmAtomicValue twoDouble = floating.ParseXdmFloating("2", XdmAtomicType.Double);
        XdmAtomicValue positiveZero = floating.ParseXdmFloating("0", XdmAtomicType.Float);
        XdmAtomicValue negativeZero = floating.ParseXdmFloating("-0", XdmAtomicType.Double);
        XdmAtomicValue positiveInfinity = floating.ParseXdmFloating("INF", XdmAtomicType.Float);
        XdmAtomicValue negativeInfinity = floating.ParseXdmFloating("-INF", XdmAtomicType.Double);

        XdmAtomicValue mixed = numeric.AddXdmNumeric(oneFloat, twoDouble);
        XdmAtomicValue indeterminateSum = numeric.AddXdmNumeric(positiveInfinity, negativeInfinity);
        XdmAtomicValue indeterminateProduct = numeric.MultiplyXdmNumeric(positiveZero, positiveInfinity);
        XdmAtomicValue positiveQuotient = numeric.DivideXdmNumeric(numeric.NegateXdmNumeric(oneFloat), negativeZero);
        XdmAtomicValue negativeQuotient = numeric.DivideXdmNumeric(oneFloat, negativeZero);
        XdmAtomicValue zeroQuotient = numeric.DivideXdmNumeric(positiveZero, positiveZero);

        Assert.Multiple(() =>
        {
            Assert.That(mixed.Type, Is.EqualTo(XdmAtomicType.Double));
            Assert.That(mixed.FloatingValue, Is.EqualTo(3d));
            Assert.That(double.IsNaN(indeterminateSum.FloatingValue), Is.True);
            Assert.That(double.IsNaN(indeterminateProduct.FloatingValue), Is.True);
            Assert.That(double.IsPositiveInfinity(positiveQuotient.FloatingValue), Is.True);
            Assert.That(double.IsNegativeInfinity(negativeQuotient.FloatingValue), Is.True);
            Assert.That(double.IsNaN(zeroQuotient.FloatingValue), Is.True);
        });
    }

    [Test]
    public void ImplementsFloatingModuloAndIntegerDivisionSpecialCases()
    {
        XdmNumericDatatype numeric = new XdmNumericDatatype();
        XdmFloatingDatatype floating = new XdmFloatingDatatype();
        XdmAtomicValue fivePointFive = floating.ParseXdmFloating("5.5", XdmAtomicType.Double);
        XdmAtomicValue two = floating.ParseXdmFloating("2", XdmAtomicType.Float);
        XdmAtomicValue zero = floating.ParseXdmFloating("0", XdmAtomicType.Double);
        XdmAtomicValue negativeZero = floating.ParseXdmFloating("-0", XdmAtomicType.Double);
        XdmAtomicValue infinity = floating.ParseXdmFloating("INF", XdmAtomicType.Double);
        XdmAtomicValue nan = floating.ParseXdmFloating("NaN", XdmAtomicType.Double);

        Assert.Multiple(() =>
        {
            Assert.That(numeric.ModuloXdmNumeric(fivePointFive, two).FloatingValue, Is.EqualTo(1.5d));
            Assert.That(double.IsNaN(numeric.ModuloXdmNumeric(infinity, two).FloatingValue), Is.True);
            Assert.That(double.IsNaN(numeric.ModuloXdmNumeric(fivePointFive, zero).FloatingValue), Is.True);
            Assert.That(numeric.ModuloXdmNumeric(fivePointFive, infinity).FloatingValue, Is.EqualTo(5.5d));
            Assert.That(numeric.ModuloXdmNumeric(negativeZero, two).CanonicalValue, Is.EqualTo("-0.0E0"));
            Assert.That(numeric.IntegerDivideXdmNumeric(fivePointFive, two).CanonicalValue, Is.EqualTo("2"));
            Assert.That(numeric.IntegerDivideXdmNumeric(fivePointFive, infinity).CanonicalValue, Is.EqualTo("0"));
            Assert.That(Assert.Throws<XPathDynamicException>(() => numeric.IntegerDivideXdmNumeric(fivePointFive, zero))!.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAR0001));
            Assert.That(Assert.Throws<XPathDynamicException>(() => numeric.IntegerDivideXdmNumeric(nan, two))!.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAR0002));
            Assert.That(Assert.Throws<XPathDynamicException>(() => numeric.IntegerDivideXdmNumeric(infinity, two))!.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAR0002));
        });
    }

    [Test]
    public void PreservesIeeeValuesAcrossUnaryAndRoundingFunctions()
    {
        XdmNumericDatatype numeric = new XdmNumericDatatype();
        XdmFloatingDatatype floating = new XdmFloatingDatatype();
        XdmAtomicValue negativeZero = floating.ParseXdmFloating("-0", XdmAtomicType.Double);
        XdmAtomicValue positiveZero = numeric.NegateXdmNumeric(negativeZero);
        XdmAtomicValue roundedNegative = numeric.RoundXdmNumeric(floating.ParseXdmFloating("-0.1", XdmAtomicType.Double), 0);

        Assert.Multiple(() =>
        {
            Assert.That(positiveZero.CanonicalValue, Is.EqualTo("0.0E0"));
            Assert.That(numeric.PositiveXdmNumeric(negativeZero).CanonicalValue, Is.EqualTo("-0.0E0"));
            Assert.That(numeric.AbsoluteXdmNumeric(negativeZero).CanonicalValue, Is.EqualTo("0.0E0"));
            Assert.That(numeric.CeilingXdmNumeric(floating.ParseXdmFloating("-0.1", XdmAtomicType.Float)).CanonicalValue, Is.EqualTo("-0.0E0"));
            Assert.That(numeric.FloorXdmNumeric(floating.ParseXdmFloating("-0.1", XdmAtomicType.Float)).CanonicalValue, Is.EqualTo("-1.0E0"));
            Assert.That(roundedNegative.CanonicalValue, Is.EqualTo("-0.0E0"));
            Assert.That(numeric.RoundXdmNumeric(floating.ParseXdmFloating("2.5", XdmAtomicType.Double), 0).CanonicalValue, Is.EqualTo("3.0E0"));
            Assert.That(numeric.RoundXdmNumeric(floating.ParseXdmFloating("-2.5", XdmAtomicType.Double), 0).CanonicalValue, Is.EqualTo("-2.0E0"));
            Assert.That(numeric.RoundHalfToEvenXdmNumeric(floating.ParseXdmFloating("2.5", XdmAtomicType.Double), 0).CanonicalValue, Is.EqualTo("2.0E0"));
            Assert.That(numeric.RoundXdmNumeric(floating.ParseXdmFloating("35.425", XdmAtomicType.Double), 2).CanonicalValue, Is.EqualTo("3.542E1"));
            Assert.That(numeric.RoundHalfToEvenXdmNumeric(floating.ParseXdmFloating("150.015", XdmAtomicType.Float), 2).CanonicalValue, Is.EqualTo("1.5001E2"));
            Assert.That(numeric.RoundXdmNumeric(floating.ParseXdmFloating("INF", XdmAtomicType.Double), 2).CanonicalValue, Is.EqualTo("INF"));
            Assert.That(numeric.RoundXdmNumeric(floating.ParseXdmFloating("NaN", XdmAtomicType.Float), 2).CanonicalValue, Is.EqualTo("NaN"));
        });
    }

    [Test]
    public void FactoryAndSequenceSerializationUseXPathFloatingStringValues()
    {
        XdmAtomicValue value = new XdmFactory().CreateXdmFloatingAtomicValue("001.25", XdmAtomicType.Double);
        XdmSequence sequence = new XdmSequence
        {
            Count = 1,
            ItemKinds = [XdmItemKind.Atomic],
            ItemIndexes = [0],
            AtomicValues = [value]
        };
        XmlSerializationOptions options = new XmlSerializationOptions { Method = XmlSerializationMethod.Text };
        StringWriter writer = new StringWriter();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer };
        XmlSerializationOptions jsonOptions = new XmlSerializationOptions { Method = XmlSerializationMethod.Json };
        StringWriter jsonWriter = new StringWriter();
        XmlOutput jsonOutput = new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = jsonWriter };

        new XmlSerializer().SerializeXdmSequence(sequence, output, options);
        new XmlSerializer().SerializeXdmSequence(sequence, jsonOutput, jsonOptions);

        Assert.Multiple(() =>
        {
            Assert.That(writer.ToString(), Is.EqualTo("1.25"));
            Assert.That(jsonWriter.ToString(), Is.EqualTo("1.25"));
        });
    }
}
