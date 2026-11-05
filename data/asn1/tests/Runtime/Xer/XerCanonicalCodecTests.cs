using asn1;

namespace asn1.Tests;

public class XerCanonicalCodecTests
{
    [Test]
    public void CanonicalPrimitiveEmissionNormalizesRealNamedBitsOidAndTime()
    {
        AsnTypeDescriptor flags = NamedBitsType();
        AsnRuntimeValue bits = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.BitString,
            BitStringValue = new AsnBitStringValue
            {
                Bytes = [0xA0],
                BitLength = 4
            }
        };
        AsnRuntimeValue real = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Real,
            RealValue = new AsnRealValue
            {
                Kind = AsnRealKind.FiniteDecimal,
                HasDecimalValue = true,
                DecimalText = "12.500e+2"
            }
        };
        AsnRuntimeValue time = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Time,
            TimeValue = new AsnTimeValue
            {
                Kind = AsnTimeKind.GeneralizedTime,
                Text = "20240101120000,5200+0100"
            }
        };
        AsnRuntimeValue duration = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Time,
            TimeValue = new AsnTimeValue
            {
                Kind = AsnTimeKind.Duration,
                Text = "P0Y2M0DT0H3M"
            }
        };

        Assert.Multiple(() =>
        {
            Assert.That(EncodeXml(Type(AsnTypeKind.Real), real),
                Is.EqualTo("<REAL>1.25E3</REAL>"));
            Assert.That(EncodeXml(flags, bits),
                Is.EqualTo("<Flags><urgent/><audit/></Flags>"));
            Assert.That(EncodeXml(
                Type(AsnTypeKind.ObjectIdentifier),
                ObjectIdentifier(2, 999, 3)), Is.EqualTo(
                    "<OBJECT_IDENTIFIER>2.999.3</OBJECT_IDENTIFIER>"));
            Assert.That(EncodeXml(
                Type(AsnTypeKind.GeneralizedTime),
                time), Is.EqualTo(
                    "<GeneralizedTime>20240101110000.52Z</GeneralizedTime>"));
            Assert.That(EncodeXml(
                Type(AsnTypeKind.Duration),
                duration), Is.EqualTo("<DURATION>P2MT3M</DURATION>"));
        });
    }

    [Test]
    public void CanonicalSetUsesTagOrderAndMaterializesDefaultComponents()
    {
        AsnFieldDescriptor title = Field(
            "title",
            StringType(),
            0,
            Tag(AsnTagClass.ContextSpecific, 0));
        AsnFieldDescriptor number = Field(
            "number",
            IntegerType(),
            1,
            Tag(AsnTagClass.Application, 2));
        AsnFieldDescriptor enabled = Field(
            "enabled",
            Type(AsnTypeKind.Boolean),
            2,
            Tag(AsnTagClass.ContextSpecific, 1));
        enabled.HasDefaultValue = true;
        enabled.DefaultValue = new AsnValueDescriptor
        {
            Value = new AsnValue
            {
                Kind = AsnValueKind.Boolean,
                TextValue = "TRUE"
            }
        };
        AsnFieldDescriptor[] fields = [title, number, enabled];
        AsnTypeDescriptor type = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Set,
            Name = "Employee",
            Fields = fields,
            RootFields = fields
        };
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Set,
            Fields =
            [
                Present("title", Text("Director")),
                Present("number", Integer(51)),
                new AsnRuntimeFieldValue
                {
                    Name = "enabled",
                    Present = false
                }
            ]
        };

        Assert.That(EncodeXml(type, value), Is.EqualTo(
            "<Employee><number>51</number><title>Director</title>"
                + "<enabled><true/></enabled></Employee>"));
    }

    [Test]
    public void CanonicalSetOfSortsCompleteElementEncodings()
    {
        AsnTypeDescriptor type = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.SetOf,
            Name = "Values",
            ElementType = new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Utf8String,
                Name = "Item"
            }
        };
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.SetOf,
            Items = [Text("é"), Text("😀"), Text("z")]
        };

        Assert.That(EncodeXml(type, value), Is.EqualTo(
            "<Values><Item>z</Item><Item>é</Item><Item>😀</Item></Values>"));
    }

    [TestCase("<?xml version=\"1.0\" encoding=\"UTF-8\"?><INTEGER>7</INTEGER>")]
    [TestCase("<INTEGER> 7 </INTEGER>")]
    public void StrictDecoderRejectsPrologAndLexicalWhitespace(string xml)
    {
        XerValueResult result = DecodeXml(Type(AsnTypeKind.Integer), xml);

        Assert.That(result.Success, Is.False);
    }

    [TestCase("<OCTET_STRING>a5 ff</OCTET_STRING>")]
    [TestCase("<OBJECT_IDENTIFIER>iso.standard(0).8571</OBJECT_IDENTIFIER>")]
    [TestCase("<REAL>12.5</REAL>")]
    [TestCase("<NULL></NULL>")]
    public void StrictDecoderRejectsNoncanonicalBasicLexicalAlternatives(
        string xml)
    {
        AsnTypeKind kind = xml.StartsWith(
            "<OCTET",
            StringComparison.Ordinal)
                ? AsnTypeKind.OctetString
                : xml.StartsWith(
                    "<OBJECT",
                    StringComparison.Ordinal)
                        ? AsnTypeKind.ObjectIdentifier
                        : xml.StartsWith(
                            "<REAL",
                            StringComparison.Ordinal)
                                ? AsnTypeKind.Real
                                : AsnTypeKind.Null;
        XerValueResult result = DecodeXml(Type(kind), xml);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("ASNXER0200"));
        });
    }

    [Test]
    public void StrictDecoderRequiresCanonicalNamedBitsAndEscaping()
    {
        XerValueResult bitText = DecodeXml(
            NamedBitsType(),
            "<Flags>101</Flags>");
        XerValueResult bitOrder = DecodeXml(
            NamedBitsType(),
            "<Flags><audit/><urgent/></Flags>");
        XerValueResult numericEscape = DecodeXml(
            Type(AsnTypeKind.Utf8String),
            "<UTF8String>&#60;&amp;&gt;</UTF8String>");
        XerValueResult canonicalEscape = DecodeXml(
            Type(AsnTypeKind.Utf8String),
            "<UTF8String>&lt;&amp;&gt;</UTF8String>");

        Assert.Multiple(() =>
        {
            Assert.That(bitText.ErrorCode, Is.EqualTo("ASNXER0200"));
            Assert.That(bitOrder.ErrorCode, Is.EqualTo("ASNXER0200"));
            Assert.That(numericEscape.ErrorCode, Is.EqualTo("ASNXER0200"));
            Assert.That(canonicalEscape.Success, Is.True,
                canonicalEscape.ErrorMessage);
            Assert.That(canonicalEscape.Value?.TextValue, Is.EqualTo("<&>"));
        });
    }

    [Test]
    public void StrictDecoderRejectsNamespacePrefixAttributesAndComments()
    {
        XerValueResult prefix = DecodeXml(
            Type(AsnTypeKind.Integer),
            "<p:INTEGER xmlns:p=\"urn:test\">7</p:INTEGER>");
        XerValueResult attribute = DecodeXml(
            Type(AsnTypeKind.Integer),
            "<INTEGER a=\"1\" b=\"2\">7</INTEGER>");
        AsnTypeDescriptor sequence = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Name = "Record",
            Fields = [Field("id", IntegerType(), 0, null)]
        };
        XerValueResult comment = DecodeXml(
            sequence,
            "<Record><!--note--><id>7</id></Record>");

        Assert.Multiple(() =>
        {
            Assert.That(prefix.Success, Is.False);
            Assert.That(attribute.Success, Is.False);
            Assert.That(comment.ErrorCode, Is.EqualTo("ASNXER0200"));
        });
    }

    [Test]
    public void StrictDecoderEnforcesSetSetOfAndDefaultOrderingRules()
    {
        AsnTypeDescriptor set = CanonicalSetType();
        string canonical = "<Employee><number>51</number>"
            + "<title>Director</title><enabled><true/></enabled></Employee>";
        XerValueResult accepted = DecodeXml(set, canonical);
        XerValueResult setOrder = DecodeXml(
            set,
            "<Employee><title>Director</title><number>51</number>"
                + "<enabled><true/></enabled></Employee>");
        XerValueResult missingDefault = DecodeXml(
            set,
            "<Employee><number>51</number><title>Director</title></Employee>");
        AsnTypeDescriptor setOf = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.SetOf,
            Name = "Values",
            ElementType = new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Utf8String,
                Name = "Item"
            }
        };
        XerValueResult setOfOrder = DecodeXml(
            setOf,
            "<Values><Item>é</Item><Item>z</Item></Values>");

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Success, Is.True, accepted.ErrorMessage);
            Assert.That(setOrder.ErrorCode, Is.EqualTo("ASNXER0200"));
            Assert.That(missingDefault.ErrorCode, Is.EqualTo("ASNXER0200"));
            Assert.That(setOfOrder.ErrorCode, Is.EqualTo("ASNXER0200"));
        });
    }

    [Test]
    public void StrictDecoderEnforcesCanonicalTimeAndTypedOpenValues()
    {
        XerValueResult time = DecodeXml(
            Type(AsnTypeKind.GeneralizedTime),
            "<GeneralizedTime>20240101120000,5200+0100</GeneralizedTime>");
        AsnTypeDescriptor integer = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Integer,
            Name = "Payload",
            StableId = "types.payload"
        };
        AsnTypeDescriptor open = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.OpenType,
            Name = "Open",
            ObjectTables =
            [
                new AsnObjectTableDescriptor
                {
                    Entries =
                    [
                        new AsnObjectTableEntryDescriptor { Type = integer }
                    ]
                }
            ]
        };
        XerValueResult typed = DecodeXml(
            open,
            "<Open><Payload>7</Payload></Open>");
        XerValueResult unresolved = new XerCanonicalCodec(Limits())
            .EncodeCanonical(
                Type(AsnTypeKind.OpenType),
                new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.OpenType,
                    OpenTypeValue = new AsnOpenTypeValue
                    {
                        Unresolved = true,
                        HasEncodingRules = true,
                        EncodingRules = AsnEncodingRules.BasicXer,
                        HasOriginalData = true,
                        OriginalData = System.Text.Encoding.UTF8.GetBytes(
                            "<Future/>")
                    }
                });

        Assert.Multiple(() =>
        {
            Assert.That(time.ErrorCode, Is.EqualTo("ASNXER0200"));
            Assert.That(typed.Success, Is.True, typed.ErrorMessage);
            Assert.That(typed.Value?.OpenTypeValue?.Resolved, Is.True);
            Assert.That(unresolved.Success, Is.False);
            Assert.That(unresolved.ErrorCode, Is.EqualTo("ASNXER0201"));
        });
    }

    private static string EncodeXml(
        AsnTypeDescriptor type,
        AsnRuntimeValue value)
    {
        AsnResourceLimits limits = Limits();
        XerCanonicalCodec codec = new XerCanonicalCodec(limits);
        XerValueResult encoded = codec.EncodeCanonical(type, value);
        Assert.That(encoded.Success, Is.True, encoded.ErrorMessage);
        return System.Text.Encoding.UTF8.GetString(
            codec.RenderTokens(encoded.Tokens));
    }

    private static XerValueResult DecodeXml(
        AsnTypeDescriptor type,
        string xml)
    {
        byte[] data = System.Text.Encoding.UTF8.GetBytes(xml);
        return new XerCanonicalCodec(Limits()).DecodeCanonical(
            type,
            data,
            0,
            data.Length);
    }

    private static AsnTypeDescriptor CanonicalSetType()
    {
        AsnFieldDescriptor title = Field(
            "title",
            StringType(),
            0,
            Tag(AsnTagClass.ContextSpecific, 0));
        AsnFieldDescriptor number = Field(
            "number",
            IntegerType(),
            1,
            Tag(AsnTagClass.Application, 2));
        AsnFieldDescriptor enabled = Field(
            "enabled",
            Type(AsnTypeKind.Boolean),
            2,
            Tag(AsnTagClass.ContextSpecific, 1));
        enabled.HasDefaultValue = true;
        enabled.DefaultValue = new AsnValueDescriptor
        {
            Value = new AsnValue
            {
                Kind = AsnValueKind.Boolean,
                TextValue = "TRUE"
            }
        };
        AsnFieldDescriptor[] fields = [title, number, enabled];
        return new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Set,
            Name = "Employee",
            Fields = fields,
            RootFields = fields
        };
    }

    private static AsnTypeDescriptor NamedBitsType()
    {
        return new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.BitString,
            Name = "Flags",
            NamedValues =
            [
                NamedBit("urgent", 0),
                NamedBit("audit", 2)
            ]
        };
    }

    private static AsnValueDescriptor NamedBit(string name, int index)
    {
        return new AsnValueDescriptor
        {
            Name = name,
            Value = new AsnValue
            {
                Kind = AsnValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = new System.Numerics.BigInteger(index)
            }
        };
    }

    private static AsnTypeDescriptor Type(AsnTypeKind kind)
    {
        return new AsnTypeDescriptor { Kind = kind };
    }

    private static AsnTypeDescriptor IntegerType()
    {
        return Type(AsnTypeKind.Integer);
    }

    private static AsnTypeDescriptor StringType()
    {
        return Type(AsnTypeKind.VisibleString);
    }

    private static AsnFieldDescriptor Field(
        string name,
        AsnTypeDescriptor type,
        int index,
        AsnTagDescriptor? tag)
    {
        return new AsnFieldDescriptor
        {
            Name = name,
            Type = type,
            Index = index,
            Tags = tag == null ? [] : [tag]
        };
    }

    private static AsnTagDescriptor Tag(AsnTagClass tagClass, int number)
    {
        return new AsnTagDescriptor
        {
            Class = tagClass,
            Number = new System.Numerics.BigInteger(number)
        };
    }

    private static AsnRuntimeFieldValue Present(
        string name,
        AsnRuntimeValue value)
    {
        return new AsnRuntimeFieldValue
        {
            Name = name,
            Present = true,
            Value = value
        };
    }

    private static AsnRuntimeValue Integer(int value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = new System.Numerics.BigInteger(value)
        };
    }

    private static AsnRuntimeValue Text(string value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.CharacterString,
            TextValue = value
        };
    }

    private static AsnRuntimeValue ObjectIdentifier(params int[] arcs)
    {
        System.Numerics.BigInteger[] values =
            new System.Numerics.BigInteger[arcs.Length];
        int index = 0;
        while (index < arcs.Length)
        {
            values[index] = new System.Numerics.BigInteger(arcs[index]);
            index = index + 1;
        }

        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.ObjectIdentifier,
            ObjectIdentifierValue = new AsnObjectIdentifierValue
            {
                Arcs = values
            }
        };
    }

    private static AsnResourceLimits Limits()
    {
        return new AsnResourceLimits
        {
            MaximumNestingDepth = 64,
            MaximumCollectionElements = 1024,
            MaximumTextCharacters = 65536,
            MaximumOpenTypeBytes = 65536,
            MaximumTotalAllocationBytes = 1048576
        };
    }
}
