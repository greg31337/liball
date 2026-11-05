using System.Numerics;
using xml;

namespace xml.Tests;

[TestFixture]
public class XdmNumericDatatypeTests
{
    [Test]
    public void ParsesAndCanonicalizesBooleanValues()
    {
        XdmNumericDatatype datatype = new XdmNumericDatatype();
        XdmAtomicValue trueWord = datatype.ParseXdmBoolean("\ttrue\r\n");
        XdmAtomicValue trueDigit = datatype.ParseXdmBoolean("1");
        XdmAtomicValue falseWord = datatype.ParseXdmBoolean("false");
        XdmAtomicValue falseDigit = datatype.ParseXdmBoolean(" 0 ");

        Assert.Multiple(() =>
        {
            Assert.That(trueWord.Type, Is.EqualTo(XdmAtomicType.Boolean));
            Assert.That(trueWord.BooleanValue, Is.True);
            Assert.That(trueWord.CanonicalValue, Is.EqualTo("true"));
            Assert.That(trueWord.LexicalValue, Is.EqualTo("\ttrue\r\n"));
            Assert.That(trueWord.IsImmutable, Is.True);
            Assert.That(trueDigit.CanonicalValue, Is.EqualTo("true"));
            Assert.That(falseWord.BooleanValue, Is.False);
            Assert.That(falseWord.CanonicalValue, Is.EqualTo("false"));
            Assert.That(falseDigit.CanonicalValue, Is.EqualTo("false"));
            Assert.That(datatype.CompareXdmBoolean(falseDigit, trueDigit), Is.LessThan(0));
            Assert.That(datatype.CompareXdmBoolean(trueWord, trueDigit), Is.Zero);
        });
    }

    [TestCase("")]
    [TestCase("TRUE")]
    [TestCase("yes")]
    [TestCase("true false")]
    public void RejectsInvalidBooleanLexicalValues(string lexical)
    {
        XdmNumericDatatype datatype = new XdmNumericDatatype();
        XdmNumericParseResult result = datatype.TryParseXdmBoolean(lexical);
        XPathDynamicException exception = Assert.Throws<XPathDynamicException>(() => datatype.ParseXdmBoolean(lexical))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Value, Is.Null);
            Assert.That(result.ErrorCode, Is.EqualTo(XPathErrorCodes.FORG0001));
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0001));
        });
    }

    [TestCase("+001.2300", "1.23")]
    [TestCase(".5", "0.5")]
    [TestCase("5.", "5")]
    [TestCase("-0.000", "0")]
    [TestCase("  -00012.34000\n", "-12.34")]
    public void ParsesExactDecimalsAndProducesCanonicalForms(string lexical, string canonical)
    {
        XdmAtomicValue value = new XdmNumericDatatype().ParseXdmDecimal(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(value.Type, Is.EqualTo(XdmAtomicType.Decimal));
            Assert.That(value.LexicalValue, Is.EqualTo(lexical));
            Assert.That(value.CanonicalValue, Is.EqualTo(canonical));
            Assert.That(value.StringValue, Is.EqualTo(canonical));
            Assert.That(value.ExtendedValue, Is.TypeOf<XdmDecimalStorage>());
            Assert.That(value.IsImmutable, Is.True);
        });
    }

    [TestCase("")]
    [TestCase(".")]
    [TestCase("+")]
    [TestCase("1e2")]
    [TestCase("1.2.3")]
    [TestCase("1 2")]
    public void RejectsInvalidDecimalLexicalValues(string lexical)
    {
        XdmNumericDatatype datatype = new XdmNumericDatatype();
        XdmNumericParseResult result = datatype.TryParseXdmDecimal(lexical);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(XPathErrorCodes.FORG0001));
            Assert.That(Assert.Throws<XPathDynamicException>(() => datatype.ParseXdmDecimal(lexical))!.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FORG0001));
        });
    }

    [Test]
    public void ParsesArbitraryPrecisionIntegersAndUsesTheFactoryEntryPoints()
    {
        XdmNumericDatatype datatype = new XdmNumericDatatype();
        string huge = "12345678901234567890123456789012345678901234567890";
        XdmAtomicValue integer = datatype.ParseXdmInteger(" +000" + huge + " ", XdmAtomicType.Integer);
        XdmAtomicValue negativeZero = datatype.ParseXdmInteger("-000", XdmAtomicType.Integer);
        XdmFactory factory = new XdmFactory();

        Assert.Multiple(() =>
        {
            Assert.That(integer.CanonicalValue, Is.EqualTo(huge));
            Assert.That(((XdmIntegerStorage)integer.ExtendedValue!).Value, Is.EqualTo(BigInteger.Parse(huge)));
            Assert.That(negativeZero.CanonicalValue, Is.EqualTo("0"));
            Assert.That(factory.CreateXdmBooleanAtomicValue("1").CanonicalValue, Is.EqualTo("true"));
            Assert.That(factory.CreateXdmDecimalAtomicValue("01.50").CanonicalValue, Is.EqualTo("1.5"));
            Assert.That(factory.CreateXdmIntegerAtomicValue("0007", XdmAtomicType.UnsignedByte).CanonicalValue, Is.EqualTo("7"));
        });
    }

    [TestCase(XdmAtomicType.Long, "-9223372036854775808", "9223372036854775807")]
    [TestCase(XdmAtomicType.Int, "-2147483648", "2147483647")]
    [TestCase(XdmAtomicType.Short, "-32768", "32767")]
    [TestCase(XdmAtomicType.Byte, "-128", "127")]
    [TestCase(XdmAtomicType.UnsignedLong, "0", "18446744073709551615")]
    [TestCase(XdmAtomicType.UnsignedInt, "0", "4294967295")]
    [TestCase(XdmAtomicType.UnsignedShort, "0", "65535")]
    [TestCase(XdmAtomicType.UnsignedByte, "0", "255")]
    public void AcceptsBothBoundariesOfBoundedIntegerTypes(XdmAtomicType type, string minimum, string maximum)
    {
        XdmNumericDatatype datatype = new XdmNumericDatatype();

        Assert.Multiple(() =>
        {
            Assert.That(datatype.ParseXdmInteger(minimum, type).CanonicalValue, Is.EqualTo(minimum));
            Assert.That(datatype.ParseXdmInteger(maximum, type).CanonicalValue, Is.EqualTo(maximum));
        });
    }

    [TestCase(XdmAtomicType.NonPositiveInteger, "1")]
    [TestCase(XdmAtomicType.NegativeInteger, "0")]
    [TestCase(XdmAtomicType.Long, "9223372036854775808")]
    [TestCase(XdmAtomicType.Int, "-2147483649")]
    [TestCase(XdmAtomicType.Short, "32768")]
    [TestCase(XdmAtomicType.Byte, "-129")]
    [TestCase(XdmAtomicType.NonNegativeInteger, "-1")]
    [TestCase(XdmAtomicType.UnsignedLong, "18446744073709551616")]
    [TestCase(XdmAtomicType.UnsignedInt, "4294967296")]
    [TestCase(XdmAtomicType.UnsignedShort, "65536")]
    [TestCase(XdmAtomicType.UnsignedByte, "256")]
    [TestCase(XdmAtomicType.PositiveInteger, "0")]
    public void RejectsValuesOutsideIntegerTypeBounds(XdmAtomicType type, string lexical)
    {
        XdmNumericDatatype datatype = new XdmNumericDatatype();
        XdmNumericParseResult result = datatype.TryParseXdmInteger(lexical, type);
        XPathDynamicException exception = Assert.Throws<XPathDynamicException>(() => datatype.ParseXdmInteger(lexical, type))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(XPathErrorCodes.FOCA0002));
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOCA0002));
        });
    }

    [Test]
    public void ComparesAndPromotesIntegersAndDecimalsExactly()
    {
        XdmNumericDatatype datatype = new XdmNumericDatatype();
        XdmAtomicValue huge = datatype.ParseXdmInteger("900719925474099300000000000000000001", XdmAtomicType.Integer);
        XdmAtomicValue next = datatype.ParseXdmInteger("900719925474099300000000000000000002", XdmAtomicType.Integer);
        XdmAtomicValue oneDecimal = datatype.ParseXdmDecimal("1.000");
        XdmAtomicValue oneInteger = datatype.ParseXdmInteger("1", XdmAtomicType.UnsignedByte);
        XdmAtomicValue promotedDecimal = datatype.PromoteXdmNumeric(oneInteger, XdmAtomicType.Decimal);
        XdmAtomicValue promotedInteger = datatype.PromoteXdmNumeric(oneInteger, XdmAtomicType.Integer);

        Assert.Multiple(() =>
        {
            Assert.That(datatype.CompareXdmNumeric(huge, next), Is.LessThan(0));
            Assert.That(datatype.CompareXdmNumeric(oneDecimal, oneInteger), Is.Zero);
            Assert.That(promotedDecimal.Type, Is.EqualTo(XdmAtomicType.Decimal));
            Assert.That(promotedDecimal.CanonicalValue, Is.EqualTo("1"));
            Assert.That(promotedInteger.Type, Is.EqualTo(XdmAtomicType.Integer));
            Assert.That(promotedInteger.CanonicalValue, Is.EqualTo("1"));
        });
    }

    [Test]
    public void PerformsExactIntegerDecimalAndMixedArithmetic()
    {
        XdmNumericDatatype datatype = new XdmNumericDatatype();
        XdmAtomicValue huge = datatype.ParseXdmInteger("999999999999999999999999999999", XdmAtomicType.Integer);
        XdmAtomicValue one = datatype.ParseXdmInteger("1", XdmAtomicType.Byte);
        XdmAtomicValue decimalValue = datatype.ParseXdmDecimal("1.20");
        XdmAtomicValue three = datatype.ParseXdmInteger("3", XdmAtomicType.Integer);

        Assert.Multiple(() =>
        {
            Assert.That(datatype.AddXdmNumeric(huge, one).CanonicalValue, Is.EqualTo("1000000000000000000000000000000"));
            Assert.That(datatype.SubtractXdmNumeric(one, three).CanonicalValue, Is.EqualTo("-2"));
            Assert.That(datatype.MultiplyXdmNumeric(decimalValue, three).CanonicalValue, Is.EqualTo("3.6"));
            Assert.That(datatype.AddXdmNumeric(decimalValue, one).CanonicalValue, Is.EqualTo("2.2"));
            Assert.That(datatype.NegateXdmNumeric(decimalValue).CanonicalValue, Is.EqualTo("-1.2"));
            Assert.That(datatype.AddXdmNumeric(one, three).Type, Is.EqualTo(XdmAtomicType.Integer));
            Assert.That(datatype.AddXdmNumeric(decimalValue, one).Type, Is.EqualTo(XdmAtomicType.Decimal));
        });
    }

    [Test]
    public void ImplementsDecimalDivisionIntegerDivisionAndModulo()
    {
        XdmNumericDatatype datatype = new XdmNumericDatatype();
        XdmAtomicValue one = datatype.ParseXdmInteger("1", XdmAtomicType.Integer);
        XdmAtomicValue two = datatype.ParseXdmInteger("2", XdmAtomicType.Integer);
        XdmAtomicValue three = datatype.ParseXdmInteger("3", XdmAtomicType.Integer);
        XdmAtomicValue eight = datatype.ParseXdmInteger("8", XdmAtomicType.Integer);
        XdmAtomicValue negativeSeven = datatype.ParseXdmInteger("-7", XdmAtomicType.Integer);
        XdmAtomicValue fivePointFive = datatype.ParseXdmDecimal("5.5");

        Assert.Multiple(() =>
        {
            Assert.That(datatype.DivideXdmNumeric(one, eight).CanonicalValue, Is.EqualTo("0.125"));
            Assert.That(datatype.DivideXdmNumeric(one, three).CanonicalValue, Is.EqualTo("0.333333333333333333"));
            Assert.That(datatype.DivideXdmNumeric(two, three).CanonicalValue, Is.EqualTo("0.666666666666666667"));
            Assert.That(datatype.DivideXdmNumeric(one, eight).Type, Is.EqualTo(XdmAtomicType.Decimal));
            Assert.That(datatype.IntegerDivideXdmNumeric(negativeSeven, three).CanonicalValue, Is.EqualTo("-2"));
            Assert.That(datatype.ModuloXdmNumeric(negativeSeven, three).CanonicalValue, Is.EqualTo("-1"));
            Assert.That(datatype.ModuloXdmNumeric(fivePointFive, two).CanonicalValue, Is.EqualTo("1.5"));
        });
    }

    [Test]
    public void DividesValuesAtTheSupportedDecimalScaleWithoutIntermediateOverflow()
    {
        XdmNumericDatatype datatype = new XdmNumericDatatype();
        XdmAtomicValue one = datatype.ParseXdmInteger("1", XdmAtomicType.Integer);
        XdmAtomicValue repeatingNinth = datatype.ParseXdmDecimal("0." + new string('1', 9999));

        XdmAtomicValue result = datatype.DivideXdmNumeric(one, repeatingNinth);

        Assert.That(result.CanonicalValue, Is.EqualTo("9"));
    }

    [Test]
    public void AppliesXPathRoundingRulesAtPositiveAndNegativePrecision()
    {
        XdmNumericDatatype datatype = new XdmNumericDatatype();

        Assert.Multiple(() =>
        {
            Assert.That(datatype.FloorXdmNumeric(datatype.ParseXdmDecimal("-2.1")).CanonicalValue, Is.EqualTo("-3"));
            Assert.That(datatype.CeilingXdmNumeric(datatype.ParseXdmDecimal("-2.1")).CanonicalValue, Is.EqualTo("-2"));
            Assert.That(datatype.RoundXdmNumeric(datatype.ParseXdmDecimal("2.5"), 0).CanonicalValue, Is.EqualTo("3"));
            Assert.That(datatype.RoundXdmNumeric(datatype.ParseXdmDecimal("-2.5"), 0).CanonicalValue, Is.EqualTo("-2"));
            Assert.That(datatype.RoundHalfToEvenXdmNumeric(datatype.ParseXdmDecimal("2.5"), 0).CanonicalValue, Is.EqualTo("2"));
            Assert.That(datatype.RoundHalfToEvenXdmNumeric(datatype.ParseXdmDecimal("3.5"), 0).CanonicalValue, Is.EqualTo("4"));
            Assert.That(datatype.RoundHalfToEvenXdmNumeric(datatype.ParseXdmDecimal("-3.5"), 0).CanonicalValue, Is.EqualTo("-4"));
            Assert.That(datatype.RoundXdmNumeric(datatype.ParseXdmDecimal("12.345"), 2).CanonicalValue, Is.EqualTo("12.35"));
            Assert.That(datatype.RoundHalfToEvenXdmNumeric(datatype.ParseXdmDecimal("12.345"), 2).CanonicalValue, Is.EqualTo("12.34"));
            Assert.That(datatype.RoundXdmNumeric(datatype.ParseXdmInteger("125", XdmAtomicType.Integer), -1).CanonicalValue, Is.EqualTo("130"));
            Assert.That(datatype.RoundHalfToEvenXdmNumeric(datatype.ParseXdmInteger("125", XdmAtomicType.Integer), -1).CanonicalValue, Is.EqualTo("120"));
        });
    }

    [Test]
    public void ReportsStandardDivisionAndOverflowErrors()
    {
        XdmNumericDatatype datatype = new XdmNumericDatatype();
        XdmAtomicValue one = datatype.ParseXdmInteger("1", XdmAtomicType.Integer);
        XdmAtomicValue zero = datatype.ParseXdmInteger("0", XdmAtomicType.Integer);
        string wide = new string('9', 6000);
        XdmAtomicValue wideInteger = datatype.ParseXdmInteger(wide, XdmAtomicType.Integer);

        XPathDynamicException divide = Assert.Throws<XPathDynamicException>(() => datatype.DivideXdmNumeric(one, zero))!;
        XPathDynamicException integerDivide = Assert.Throws<XPathDynamicException>(() => datatype.IntegerDivideXdmNumeric(one, zero))!;
        XPathDynamicException modulo = Assert.Throws<XPathDynamicException>(() => datatype.ModuloXdmNumeric(one, zero))!;
        XPathDynamicException overflow = Assert.Throws<XPathDynamicException>(() => datatype.MultiplyXdmNumeric(wideInteger, wideInteger))!;

        Assert.Multiple(() =>
        {
            Assert.That(divide.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAR0001));
            Assert.That(integerDivide.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAR0001));
            Assert.That(modulo.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAR0001));
            Assert.That(overflow.Diagnostic!.Code, Is.EqualTo(XPathErrorCodes.FOAR0002));
        });
    }

    [Test]
    public void SerializesExactDecimalStorageWithoutClrDecimalRounding()
    {
        string exact = "123456789012345678901234567890.1234567890123456789";
        XdmAtomicValue value = new XdmNumericDatatype().ParseXdmDecimal(exact);
        XdmSequence sequence = new XdmSequence
        {
            Count = 1,
            ItemKinds = [XdmItemKind.Atomic],
            ItemIndexes = [0],
            AtomicValues = [value]
        };
        XmlSerializationOptions options = new XmlSerializationOptions { Method = XmlSerializationMethod.Json };
        StringWriter writer = new StringWriter();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer };

        new XmlSerializer().SerializeXdmSequence(sequence, output, options);

        Assert.That(writer.ToString(), Is.EqualTo(exact));
    }
}
