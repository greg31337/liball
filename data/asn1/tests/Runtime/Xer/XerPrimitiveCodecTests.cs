using asn1;

namespace asn1.Tests;

public class XerPrimitiveCodecTests
{
    [Test]
    public void DecimalIntegerDigitsAreBoundedBeforeBigIntegerConstruction()
    {
        XerPrimitiveCodec codec = new XerPrimitiveCodec(
            new AsnResourceLimits { MaximumIntegerBits = 8 });

        Assert.That(codec.ParseSignedInteger("255", false).Success, Is.True);
        Assert.That(codec.ParseSignedInteger("256", false).Success, Is.False);
        Assert.That(codec.ParseSignedInteger(new string('9', 10000), false).Success,
            Is.False);
    }

    [Test]
    public void NameResolverUsesAssignedAndStandardBuiltinNames()
    {
        XerNameResolver resolver = new XerNameResolver();

        Assert.Multiple(() =>
        {
            Assert.That(resolver.GetElementName(
                Type(AsnTypeKind.BitString)).Name,
                Is.EqualTo("BIT_STRING"));
            Assert.That(resolver.GetElementName(
                Type(AsnTypeKind.ObjectIdentifier)).Name,
                Is.EqualTo("OBJECT_IDENTIFIER"));
            Assert.That(resolver.GetElementName(
                Type(AsnTypeKind.RelativeObjectIdentifierIri)).Name,
                Is.EqualTo("RELATIVE_OID_IRI"));
            Assert.That(resolver.GetElementName(
                Type(AsnTypeKind.DateTime)).Name,
                Is.EqualTo("DATE_TIME"));
            Assert.That(resolver.GetElementName(new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Integer,
                Name = "Employee-number"
            }).Name, Is.EqualTo("Employee-number"));
        });
    }

    [Test]
    public void BooleanIntegerEnumeratedAndNullUseBasicXerWrappers()
    {
        AsnTypeDescriptor enumerated = EnumeratedType();
        AsnRuntimeValue integer = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = System.Numerics.BigInteger.Parse(
                "-340282366920938463463374607431768211507")
        };
        AsnRuntimeValue green = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Enumerated,
            TextValue = "green"
        };

        Assert.Multiple(() =>
        {
            Assert.That(EncodeXml(
                Type(AsnTypeKind.Boolean),
                Boolean(true)),
                Is.EqualTo("<BOOLEAN><true/></BOOLEAN>"));
            Assert.That(EncodeXml(
                new AsnTypeDescriptor
                {
                    Kind = AsnTypeKind.Integer,
                    Name = "Huge-number"
                },
                integer),
                Is.EqualTo(
                    "<Huge-number>-340282366920938463463374607431768211507</Huge-number>"));
            Assert.That(EncodeXml(enumerated, green),
                Is.EqualTo("<Color><green/></Color>"));
            Assert.That(EncodeXml(
                Type(AsnTypeKind.Null),
                new AsnRuntimeValue { Kind = AsnRuntimeValueKind.Null }),
                Is.EqualTo("<NULL/>"));
        });

        XerPrimitiveResult decodedBoolean = DecodeXml(
            Type(AsnTypeKind.Boolean),
            "<BOOLEAN> \n <false/> \t </BOOLEAN>");
        XerPrimitiveResult decodedInteger = DecodeXml(
            Type(AsnTypeKind.Integer),
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><INTEGER>-19</INTEGER>");
        XerPrimitiveResult decodedEnumerated = DecodeXml(
            enumerated,
            "<Color> <red/> </Color>");

        Assert.Multiple(() =>
        {
            Assert.That(decodedBoolean.Success, Is.True);
            Assert.That(decodedBoolean.Value?.BooleanValue, Is.False);
            Assert.That(decodedInteger.Success, Is.True);
            Assert.That(decodedInteger.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(-19)));
            Assert.That(decodedInteger.ConsumedCount, Is.EqualTo(4));
            Assert.That(decodedEnumerated.Success, Is.True);
            Assert.That(decodedEnumerated.Value?.TextValue,
                Is.EqualTo("red"));
            Assert.That(decodedEnumerated.Value?.IntegerValue,
                Is.EqualTo(System.Numerics.BigInteger.Zero));
        });
    }

    [Test]
    public void RealEncodingsCoverDecimalBinaryZeroAndSpecialValues()
    {
        AsnTypeDescriptor type = Type(AsnTypeKind.Real);
        AsnRuntimeValue decimalValue = Real(new AsnRealValue
        {
            Kind = AsnRealKind.FiniteDecimal,
            HasDecimalValue = true,
            DecimalText = "3.14159"
        });
        AsnRuntimeValue binaryValue = Real(new AsnRealValue
        {
            Kind = AsnRealKind.FiniteBinary,
            HasBinaryValue = true,
            Mantissa = new System.Numerics.BigInteger(3),
            Radix = 2,
            Exponent = new System.Numerics.BigInteger(-1)
        });

        Assert.Multiple(() =>
        {
            Assert.That(EncodeXml(type, decimalValue),
                Is.EqualTo("<REAL>3.14159</REAL>"));
            Assert.That(EncodeXml(type, binaryValue),
                Is.EqualTo("<REAL>1.5</REAL>"));
            Assert.That(EncodeXml(type, RealKind(AsnRealKind.MinusZero)),
                Is.EqualTo("<REAL>-0</REAL>"));
            Assert.That(EncodeXml(
                type,
                RealKind(AsnRealKind.PositiveInfinity)),
                Is.EqualTo("<REAL><PLUS-INFINITY/></REAL>"));
            Assert.That(EncodeXml(
                type,
                RealKind(AsnRealKind.NegativeInfinity)),
                Is.EqualTo("<REAL><MINUS-INFINITY/></REAL>"));
            Assert.That(EncodeXml(
                type,
                RealKind(AsnRealKind.NotANumber)),
                Is.EqualTo("<REAL><NOT-A-NUMBER/></REAL>"));
        });

        XerPrimitiveResult finite = DecodeXml(type, "<REAL>-12.50E2</REAL>");
        XerPrimitiveResult minusZero = DecodeXml(type, "<REAL>-0</REAL>");
        XerPrimitiveResult infinity = DecodeXml(
            type,
            "<REAL><PLUS-INFINITY/></REAL>");
        XerPrimitiveResult alternateZero = DecodeXml(
            type,
            "<REAL>0E10</REAL>");

        Assert.Multiple(() =>
        {
            Assert.That(finite.Success, Is.True);
            Assert.That(finite.Value?.RealValue?.DecimalCoefficient,
                Is.EqualTo(new System.Numerics.BigInteger(-125)));
            Assert.That(finite.Value?.RealValue?.DecimalExponent,
                Is.EqualTo(System.Numerics.BigInteger.One));
            Assert.That(minusZero.Value?.RealValue?.Kind,
                Is.EqualTo(AsnRealKind.MinusZero));
            Assert.That(infinity.Value?.RealValue?.Kind,
                Is.EqualTo(AsnRealKind.PositiveInfinity));
            Assert.That(alternateZero.Value?.RealValue?.Kind,
                Is.EqualTo(AsnRealKind.Zero));
        });
    }

    [Test]
    public void BitOctetStringOidIriAndCharacterStringsRoundTrip()
    {
        AsnRuntimeValue bits = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.BitString,
            BitStringValue = new AsnBitStringValue
            {
                Bytes = [0xB0],
                BitLength = 4
            }
        };
        AsnRuntimeValue octets = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OctetString,
            Octets = [0x00, 0xA5, 0xFF]
        };
        AsnRuntimeValue oid = ObjectIdentifier(false, 2, 999, 3, 0);
        AsnRuntimeValue relative = ObjectIdentifier(true, 8571, 1);
        AsnRuntimeValue iri = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.ObjectIdentifierIri,
            TextValue = "/iso/standard/8571"
        };
        AsnRuntimeValue text = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.CharacterString,
            TextValue = "A<&\U0001F600"
        };

        Assert.Multiple(() =>
        {
            Assert.That(EncodeXml(Type(AsnTypeKind.BitString), bits),
                Is.EqualTo("<BIT_STRING>1011</BIT_STRING>"));
            Assert.That(EncodeXml(Type(AsnTypeKind.OctetString), octets),
                Is.EqualTo("<OCTET_STRING>00A5FF</OCTET_STRING>"));
            Assert.That(EncodeXml(Type(AsnTypeKind.ObjectIdentifier), oid),
                Is.EqualTo("<OBJECT_IDENTIFIER>2.999.3.0</OBJECT_IDENTIFIER>"));
            Assert.That(EncodeXml(
                Type(AsnTypeKind.RelativeObjectIdentifier),
                relative),
                Is.EqualTo("<RELATIVE_OID>8571.1</RELATIVE_OID>"));
            Assert.That(EncodeXml(Type(AsnTypeKind.ObjectIdentifierIri), iri),
                Is.EqualTo("<OID_IRI>/iso/standard/8571</OID_IRI>"));
            Assert.That(EncodeXml(Type(AsnTypeKind.Utf8String), text),
                Is.EqualTo("<UTF8String>A&lt;&amp;\U0001F600</UTF8String>"));
        });

        XerPrimitiveResult decodedBits = DecodeXml(
            Type(AsnTypeKind.BitString),
            "<BIT_STRING>10 11</BIT_STRING>");
        XerPrimitiveResult decodedOctets = DecodeXml(
            Type(AsnTypeKind.OctetString),
            "<OCTET_STRING>00 a5 FF</OCTET_STRING>");
        XerPrimitiveResult decodedOid = DecodeXml(
            Type(AsnTypeKind.ObjectIdentifier),
            "<OBJECT_IDENTIFIER>iso.standard(0).8571.1</OBJECT_IDENTIFIER>");
        XerPrimitiveResult emptyText = DecodeXml(
            Type(AsnTypeKind.Utf8String),
            "<UTF8String></UTF8String>");

        Assert.Multiple(() =>
        {
            Assert.That(decodedBits.Value?.BitStringValue?.BitLength,
                Is.EqualTo(4));
            Assert.That(decodedBits.Value?.BitStringValue?.Bytes,
                Is.EqualTo(new byte[] { 0xB0 }));
            Assert.That(decodedOctets.Value?.Octets,
                Is.EqualTo(new byte[] { 0x00, 0xA5, 0xFF }));
            Assert.That(decodedOid.Value?.ObjectIdentifierValue?.Arcs,
                Is.EqualTo(new System.Numerics.BigInteger[] { 1, 0, 8571, 1 }));
            Assert.That(emptyText.Success, Is.True);
            Assert.That(emptyText.Value?.TextValue, Is.Empty);
        });
    }

    [TestCase(AsnTypeKind.UtcTime, AsnTimeKind.UtcTime, "0101021200Z", "UTCTime")]
    [TestCase(AsnTypeKind.GeneralizedTime, AsnTimeKind.GeneralizedTime, "20010102120000Z", "GeneralizedTime")]
    [TestCase(AsnTypeKind.Date, AsnTimeKind.Date, "2024-02-29", "DATE")]
    [TestCase(AsnTypeKind.TimeOfDay, AsnTimeKind.TimeOfDay, "12:34:56Z", "TIME_OF_DAY")]
    [TestCase(AsnTypeKind.DateTime, AsnTimeKind.DateTime, "2024-02-29T12:34:56Z", "DATE_TIME")]
    [TestCase(AsnTypeKind.Duration, AsnTimeKind.Duration, "P1Y2M3DT4H", "DURATION")]
    public void UsefulAndTimeValuesUseTheirXmlTimeText(
        AsnTypeKind typeKind,
        AsnTimeKind timeKind,
        string text,
        string elementName)
    {
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Time,
            TimeValue = new AsnTimeValue
            {
                Kind = timeKind,
                Text = text
            }
        };
        string xml = EncodeXml(Type(typeKind), value);
        XerPrimitiveResult decoded = DecodeXml(Type(typeKind), xml);

        Assert.Multiple(() =>
        {
            Assert.That(xml, Is.EqualTo(
                "<" + elementName + ">" + text
                    + "</" + elementName + ">"));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Value?.TimeValue?.Kind, Is.EqualTo(timeKind));
            Assert.That(decoded.Value?.TimeValue?.Text, Is.EqualTo(text));
        });
    }

    [TestCase("<BOOLEAN>true</BOOLEAN>")]
    [TestCase("<INTEGER>01</INTEGER>")]
    [TestCase("<REAL>INF</REAL>")]
    [TestCase("<OCTET_STRING>ABC</OCTET_STRING>")]
    public void DecoderRejectsNonBasicOrMalformedScalarLexicalForms(string xml)
    {
        AsnTypeKind kind = xml.StartsWith("<BOOLEAN", StringComparison.Ordinal)
            ? AsnTypeKind.Boolean
            : xml.StartsWith("<INTEGER", StringComparison.Ordinal)
                ? AsnTypeKind.Integer
                : xml.StartsWith("<REAL", StringComparison.Ordinal)
                    ? AsnTypeKind.Real
                    : AsnTypeKind.OctetString;
        XerPrimitiveResult result = DecodeXml(Type(kind), xml);

        Assert.That(result.ErrorCode, Is.EqualTo("ASNXER0108"));
    }

    private static string EncodeXml(
        AsnTypeDescriptor type,
        AsnRuntimeValue value)
    {
        AsnResourceLimits limits = Limits();
        XerPrimitiveResult encoded = new XerPrimitiveCodec(limits)
            .EncodePrimitive(type, value);
        Assert.That(encoded.Success, Is.True, encoded.ErrorMessage);
        System.IO.MemoryStream output = new System.IO.MemoryStream();
        XerXmlTokenWriter writer = new XerXmlTokenWriter(output, limits);
        foreach (XerXmlToken token in encoded.Tokens)
        {
            XerXmlWriteResult written = writer.WriteToken(token);
            Assert.That(written.Success, Is.True, written.ErrorMessage);
        }

        XerXmlWriteResult complete = writer.CompleteWriter();
        Assert.That(complete.Success, Is.True, complete.ErrorMessage);
        return System.Text.Encoding.UTF8.GetString(output.ToArray());
    }

    private static XerPrimitiveResult DecodeXml(
        AsnTypeDescriptor type,
        string xml)
    {
        AsnResourceLimits limits = Limits();
        XerXmlReadResult read = new XerXmlTokenReader(limits).ConsumeXml(
            new AsnIncrementalInput
            {
                Data = System.Text.Encoding.UTF8.GetBytes(xml),
                IsFinalBlock = true
            });
        Assert.That(read.Status, Is.EqualTo(AsnDecodeStatus.Done),
            read.ErrorMessage);
        return new XerPrimitiveCodec(limits).DecodePrimitive(
            type,
            read.Tokens,
            0,
            read.Tokens.Length);
    }

    private static AsnTypeDescriptor Type(AsnTypeKind kind)
    {
        return new AsnTypeDescriptor { Kind = kind };
    }

    private static AsnTypeDescriptor EnumeratedType()
    {
        return new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Enumerated,
            Name = "Color",
            NamedValues =
            [
                Descriptor("red", 0),
                Descriptor("green", 1)
            ]
        };
    }

    private static AsnValueDescriptor Descriptor(string name, int value)
    {
        return new AsnValueDescriptor
        {
            Name = name,
            Value = new AsnValue
            {
                Kind = AsnValueKind.Enumerated,
                HasIntegerValue = true,
                IntegerValue = new System.Numerics.BigInteger(value)
            }
        };
    }

    private static AsnRuntimeValue Boolean(bool value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Boolean,
            HasBooleanValue = true,
            BooleanValue = value
        };
    }

    private static AsnRuntimeValue Real(AsnRealValue value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Real,
            RealValue = value
        };
    }

    private static AsnRuntimeValue RealKind(AsnRealKind kind)
    {
        return Real(new AsnRealValue
        {
            Normalized = true,
            Kind = kind
        });
    }

    private static AsnRuntimeValue ObjectIdentifier(
        bool relative,
        params int[] arcs)
    {
        return new AsnRuntimeValue
        {
            Kind = relative
                ? AsnRuntimeValueKind.RelativeObjectIdentifier
                : AsnRuntimeValueKind.ObjectIdentifier,
            ObjectIdentifierValue = new AsnObjectIdentifierValue
            {
                Relative = relative,
                Arcs = arcs.Select(item =>
                    new System.Numerics.BigInteger(item)).ToArray()
            }
        };
    }

    private static AsnResourceLimits Limits()
    {
        return new AsnResourceLimits
        {
            MaximumNestingDepth = 16,
            MaximumCollectionElements = 128,
            MaximumIntegerBits = 4096,
            MaximumTextCharacters = 4096,
            MaximumTotalAllocationBytes = 65536
        };
    }
}
