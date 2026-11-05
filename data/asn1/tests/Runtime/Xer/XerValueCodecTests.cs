using asn1;

namespace asn1.Tests;

public class XerValueCodecTests
{
    [Test]
    public void SequenceHandlesOptionalDefaultExtensionAndUnknownComponents()
    {
        AsnTypeDescriptor type = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Name = "Record",
            Extensible = true,
            Fields =
            [
                Field("id", IntegerType()),
                Field("nickname", StringType(), optional: true),
                Field("level", IntegerType(), hasDefault: true),
                Field("note", StringType(), extension: true)
            ]
        };
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("id", Integer(7)),
                Absent("nickname"),
                Absent("level"),
                Present("note", Text("ready"))
            ]
        };

        Assert.That(EncodeXml(type, value), Is.EqualTo(
            "<Record><id>7</id><note>ready</note></Record>"));

        XerValueResult decoded = DecodeXml(
            type,
            "<Record><id>7</id><note>ready</note>"
                + "<future><enabled/></future></Record>");

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True, decoded.ErrorMessage);
            Assert.That(decoded.Value?.Fields[0].Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(7)));
            Assert.That(decoded.Value?.Fields[1].Present, Is.False);
            Assert.That(decoded.Value?.Fields[2].Present, Is.False);
            Assert.That(decoded.Value?.Fields[3].Value?.TextValue,
                Is.EqualTo("ready"));
            Assert.That(decoded.Value?.UnknownFields, Has.Length.EqualTo(1));
            Assert.That(decoded.Value?.UnknownFields[0].Name,
                Is.EqualTo("future"));
            Assert.That(decoded.Value?.UnknownFields[0].TextData,
                Is.EqualTo("<future><enabled/></future>"));
        });

        Assert.That(EncodeXml(type, decoded.Value!), Is.EqualTo(
            "<Record><id>7</id><note>ready</note>"
                + "<future><enabled/></future></Record>"));
    }

    [Test]
    public void SetAcceptsComponentOrderIndependentOfSchemaOrder()
    {
        AsnTypeDescriptor type = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Set,
            Name = "Coordinates",
            Fields =
            [
                Field("x", IntegerType()),
                Field("y", IntegerType())
            ]
        };

        XerValueResult decoded = DecodeXml(
            type,
            "<Coordinates><y>2</y><x>1</x></Coordinates>");

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True, decoded.ErrorMessage);
            Assert.That(decoded.Value?.Kind, Is.EqualTo(AsnRuntimeValueKind.Set));
            Assert.That(decoded.Value?.Fields[0].Name, Is.EqualTo("x"));
            Assert.That(decoded.Value?.Fields[0].Value?.IntegerValue,
                Is.EqualTo(System.Numerics.BigInteger.One));
            Assert.That(decoded.Value?.Fields[1].Name, Is.EqualTo("y"));
            Assert.That(decoded.Value?.Fields[1].Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(2)));
        });

        Assert.That(EncodeXml(type, decoded.Value!), Is.EqualTo(
            "<Coordinates><x>1</x><y>2</y></Coordinates>"));
    }

    [Test]
    public void ChoiceAndCollectionsRoundTripTheirStructuredValues()
    {
        AsnTypeDescriptor choice = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Choice,
            Name = "Payload",
            Fields =
            [
                Field("number", IntegerType()),
                Field("text", StringType())
            ]
        };
        AsnRuntimeValue selected = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Choice,
            Fields = [Present("text", Text("hello"))]
        };
        AsnTypeDescriptor sequenceOf = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.SequenceOf,
            Name = "Numbers",
            ElementType = IntegerType()
        };
        AsnRuntimeValue numbers = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.SequenceOf,
            Items = [Integer(3), Integer(1), Integer(4)]
        };

        Assert.Multiple(() =>
        {
            Assert.That(EncodeXml(choice, selected),
                Is.EqualTo("<Payload><text>hello</text></Payload>"));
            Assert.That(EncodeXml(sequenceOf, numbers), Is.EqualTo(
                "<Numbers><INTEGER>3</INTEGER><INTEGER>1</INTEGER>"
                    + "<INTEGER>4</INTEGER></Numbers>"));
        });

        XerValueResult decodedChoice = DecodeXml(
            choice,
            "<Payload><number>99</number></Payload>");
        XerValueResult decodedItems = DecodeXml(
            sequenceOf,
            "<Numbers><INTEGER>8</INTEGER><INTEGER>9</INTEGER></Numbers>");

        Assert.Multiple(() =>
        {
            Assert.That(decodedChoice.Value?.Fields, Has.Length.EqualTo(1));
            Assert.That(decodedChoice.Value?.Fields[0].Name,
                Is.EqualTo("number"));
            Assert.That(decodedChoice.Value?.Fields[0].Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(99)));
            Assert.That(decodedItems.Value?.Items, Has.Length.EqualTo(2));
            Assert.That(decodedItems.Value?.Items[1].IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(9)));
        });
    }

    [TestCase(AsnTypeKind.External, AsnRuntimeValueKind.External, "data-value")]
    [TestCase(AsnTypeKind.EmbeddedPdv, AsnRuntimeValueKind.EmbeddedPdv, "data-value")]
    [TestCase(AsnTypeKind.CharacterString, AsnRuntimeValueKind.CharacterString, "string-value")]
    public void UsefulTypesUseTheirAssociatedStructuredType(
        AsnTypeKind typeKind,
        AsnRuntimeValueKind valueKind,
        string dataName)
    {
        AsnTypeDescriptor type = new AsnTypeDescriptor
        {
            Kind = typeKind,
            Name = "Useful"
        };
        System.Collections.Generic.List<AsnRuntimeFieldValue> fields =
            new System.Collections.Generic.List<AsnRuntimeFieldValue>
            {
                Present("identification", FixedIdentification())
            };
        if (typeKind == AsnTypeKind.External)
        {
            fields.Add(Absent("data-value-descriptor"));
        }

        fields.Add(Present(dataName, Octets(0xA5, 0x00)));
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = valueKind,
            Fields = fields.ToArray()
        };

        string xml = "<Useful><identification><fixed/></identification><"
            + dataName + ">A500</" + dataName + "></Useful>";
        Assert.That(EncodeXml(type, value), Is.EqualTo(xml));

        XerValueResult decoded = DecodeXml(type, xml);
        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True, decoded.ErrorMessage);
            Assert.That(decoded.Value?.Kind, Is.EqualTo(valueKind));
            Assert.That(decoded.Value?.Fields[^1].Value?.Octets,
                Is.EqualTo(new byte[] { 0xA5, 0x00 }));
        });
    }

    [Test]
    public void InstanceOfCarriesTypeIdAndUnresolvedTypedXmlValue()
    {
        AsnTypeDescriptor type = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.InstanceOf,
            Name = "Envelope"
        };
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.InstanceOf,
            Fields =
            [
                Present("type-id", ObjectIdentifier(1, 2, 840, 113549)),
                Present("value", UnresolvedOpen(
                    "<Payload><INTEGER>5</INTEGER></Payload>"))
            ]
        };
        string xml = "<Envelope><type-id>1.2.840.113549</type-id>"
            + "<value><Payload><INTEGER>5</INTEGER></Payload></value>"
            + "</Envelope>";

        Assert.That(EncodeXml(type, value), Is.EqualTo(xml));

        XerValueResult decoded = DecodeXml(type, xml);
        AsnOpenTypeValue? open = decoded.Value?.Fields[1].Value?.OpenTypeValue;
        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True, decoded.ErrorMessage);
            Assert.That(decoded.Value?.Kind,
                Is.EqualTo(AsnRuntimeValueKind.InstanceOf));
            Assert.That(open?.Unresolved, Is.True);
            Assert.That(open?.EncodingRules,
                Is.EqualTo(AsnEncodingRules.BasicXer));
            Assert.That(System.Text.Encoding.UTF8.GetString(
                open?.OriginalData ?? []), Is.EqualTo(
                    "<Payload><INTEGER>5</INTEGER></Payload>"));
        });
    }

    [Test]
    public void OpenTypeResolvesUniqueTypedElementFromObjectTable()
    {
        AsnTypeDescriptor age = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Integer,
            Name = "Age",
            StableId = "types.age"
        };
        AsnTypeDescriptor openType = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.OpenType,
            Name = "OpenValue",
            ObjectTables =
            [
                new AsnObjectTableDescriptor
                {
                    Entries =
                    [
                        new AsnObjectTableEntryDescriptor
                        {
                            StableId = "entry.age",
                            Type = age
                        }
                    ]
                }
            ]
        };
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OpenType,
            OpenTypeValue = new AsnOpenTypeValue
            {
                Resolved = true,
                ResolvedTypeId = "types.age",
                TypeName = "Age",
                Value = Integer(21)
            }
        };

        Assert.That(EncodeXml(openType, value),
            Is.EqualTo("<OpenValue><Age>21</Age></OpenValue>"));

        XerValueResult decoded = DecodeXml(
            openType,
            "<OpenValue><Age>22</Age></OpenValue>");
        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True, decoded.ErrorMessage);
            Assert.That(decoded.Value?.OpenTypeValue?.Resolved, Is.True);
            Assert.That(decoded.Value?.OpenTypeValue?.ResolvedTypeId,
                Is.EqualTo("types.age"));
            Assert.That(decoded.Value?.OpenTypeValue?.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(22)));
            Assert.That(System.Text.Encoding.UTF8.GetString(
                decoded.Value?.OpenTypeValue?.OriginalData ?? []),
                Is.EqualTo("<Age>22</Age>"));
        });
    }

    [Test]
    public void UnresolvedOpenTypeIsPreservedAndCanBeReencoded()
    {
        AsnTypeDescriptor type = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.OpenType
        };
        string xml = "<OPEN_TYPE><Future><flag/></Future></OPEN_TYPE>";

        XerValueResult decoded = DecodeXml(type, xml);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True, decoded.ErrorMessage);
            Assert.That(decoded.Value?.OpenTypeValue?.Unresolved, Is.True);
            Assert.That(System.Text.Encoding.UTF8.GetString(
                decoded.Value?.OpenTypeValue?.OriginalData ?? []),
                Is.EqualTo("<Future><flag/></Future>"));
        });
        Assert.That(EncodeXml(type, decoded.Value!), Is.EqualTo(xml));
    }

    [Test]
    public void CollectionLimitAndMalformedStructuredValuesAreRejected()
    {
        AsnResourceLimits limits = Limits();
        limits.MaximumCollectionElements = 1;
        AsnTypeDescriptor type = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.SequenceOf,
            Name = "Values",
            ElementType = IntegerType()
        };
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.SequenceOf,
            Items = [Integer(1), Integer(2)]
        };

        XerValueResult encoded = new XerValueCodec(limits).EncodeValue(
            type,
            value);
        XerValueResult malformed = DecodeXml(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.Choice,
                Name = "Selection",
                Fields = [Field("one", IntegerType())]
            },
            "<Selection><one>1</one><one>2</one></Selection>");

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.False);
            Assert.That(encoded.LimitExceeded, Is.True);
            Assert.That(encoded.ErrorCode, Is.EqualTo("ASNR0011"));
            Assert.That(malformed.Success, Is.False);
            Assert.That(malformed.ErrorCode, Is.EqualTo("ASNXER0135"));
        });
    }

    private static string EncodeXml(
        AsnTypeDescriptor type,
        AsnRuntimeValue value)
    {
        AsnResourceLimits limits = Limits();
        XerValueResult encoded = new XerValueCodec(limits).EncodeValue(
            type,
            value);
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

    private static XerValueResult DecodeXml(
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
        return new XerValueCodec(limits).DecodeValue(
            type,
            read.Tokens,
            0,
            read.Tokens.Length);
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

    private static AsnTypeDescriptor IntegerType()
    {
        return new AsnTypeDescriptor { Kind = AsnTypeKind.Integer };
    }

    private static AsnTypeDescriptor StringType()
    {
        return new AsnTypeDescriptor { Kind = AsnTypeKind.Utf8String };
    }

    private static AsnFieldDescriptor Field(
        string name,
        AsnTypeDescriptor type,
        bool optional = false,
        bool hasDefault = false,
        bool extension = false)
    {
        return new AsnFieldDescriptor
        {
            Name = name,
            Type = type,
            Optional = optional,
            HasDefaultValue = hasDefault,
            Extension = extension
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

    private static AsnRuntimeFieldValue Absent(string name)
    {
        return new AsnRuntimeFieldValue { Name = name };
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

    private static AsnRuntimeValue Octets(params byte[] value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OctetString,
            Octets = value
        };
    }

    private static AsnRuntimeValue FixedIdentification()
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Choice,
            Fields =
            [
                Present(
                    "fixed",
                    new AsnRuntimeValue { Kind = AsnRuntimeValueKind.Null })
            ]
        };
    }

    private static AsnRuntimeValue ObjectIdentifier(params int[] arcs)
    {
        System.Numerics.BigInteger[] values =
            new System.Numerics.BigInteger[arcs.Length];
        for (int index = 0; index < arcs.Length; index++)
        {
            values[index] = new System.Numerics.BigInteger(arcs[index]);
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

    private static AsnRuntimeValue UnresolvedOpen(string xml)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OpenType,
            OpenTypeValue = new AsnOpenTypeValue
            {
                Unresolved = true,
                HasEncodingRules = true,
                EncodingRules = AsnEncodingRules.BasicXer,
                HasOriginalData = true,
                OriginalData = System.Text.Encoding.UTF8.GetBytes(xml)
            }
        };
    }
}
