using asn1;

namespace asn1.Tests;

public class XerComprehensiveVectorTests
{
    [Test]
    public void EveryCharacterAndIriTypeHasExactBasicAndCanonicalVectors()
    {
        AsnTypeKind[] kinds =
        [
            AsnTypeKind.ObjectIdentifierIri,
            AsnTypeKind.RelativeObjectIdentifierIri,
            AsnTypeKind.ObjectDescriptor,
            AsnTypeKind.Utf8String,
            AsnTypeKind.NumericString,
            AsnTypeKind.PrintableString,
            AsnTypeKind.TeletexString,
            AsnTypeKind.VideotexString,
            AsnTypeKind.Ia5String,
            AsnTypeKind.GraphicString,
            AsnTypeKind.VisibleString,
            AsnTypeKind.GeneralString,
            AsnTypeKind.UniversalString,
            AsnTypeKind.BmpString
        ];
        string[] names =
        [
            "OID_IRI",
            "RELATIVE_OID_IRI",
            "ObjectDescriptor",
            "UTF8String",
            "NumericString",
            "PrintableString",
            "TeletexString",
            "VideotexString",
            "IA5String",
            "GraphicString",
            "VisibleString",
            "GeneralString",
            "UniversalString",
            "BMPString"
        ];
        string[] values =
        [
            "/iso/member-body/us",
            "member-body/us",
            "A",
            "A<&é😀",
            "1 ",
            "A+1",
            "A",
            "A",
            "A",
            "A",
            "A",
            "A",
            "😀",
            "é"
        ];
        string[] contents =
        [
            "/iso/member-body/us",
            "member-body/us",
            "A",
            "A&lt;&amp;é😀",
            "1 ",
            "A+1",
            "A",
            "A",
            "A",
            "A",
            "A",
            "A",
            "😀",
            "é"
        ];
        AsnEncodingRules[] rules =
        [
            AsnEncodingRules.BasicXer,
            AsnEncodingRules.CanonicalXer
        ];

        int kindIndex = 0;
        while (kindIndex < kinds.Length)
        {
            AsnTypeDescriptor type = Type(kinds[kindIndex], names[kindIndex]);
            AsnRuntimeValue value = TextValue(kinds[kindIndex], values[kindIndex]);
            string expected = "<" + names[kindIndex] + ">"
                + contents[kindIndex] + "</" + names[kindIndex] + ">";
            int rulesIndex = 0;
            while (rulesIndex < rules.Length)
            {
                AsnRuntimeValue decoded = AssertExactRoundTrip(
                    type,
                    value,
                    rules[rulesIndex],
                    expected);
                Assert.That(decoded.TextValue,
                    Is.EqualTo(values[kindIndex]),
                    kinds[kindIndex] + " " + rules[rulesIndex]);
                rulesIndex = rulesIndex + 1;
            }

            kindIndex = kindIndex + 1;
        }
    }

    [TestCase(AsnEncodingRules.BasicXer)]
    [TestCase(AsnEncodingRules.CanonicalXer)]
    public void TimeTypeHasAnExactLexicalVector(AsnEncodingRules rules)
    {
        AsnRuntimeValue decoded = AssertExactRoundTrip(
            Type(AsnTypeKind.Time, "TIME"),
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Time,
                TimeValue = new AsnTimeValue
                {
                    Kind = AsnTimeKind.Time,
                    Text = "12:34:56Z"
                }
            },
            rules,
            "<TIME>12:34:56Z</TIME>");

        Assert.That(decoded.TimeValue?.Text, Is.EqualTo("12:34:56Z"));
    }

    [Test]
    public void CompositeBasicAndCanonicalVectorsCoverStructuralDifferences()
    {
        AsnTypeDescriptor packet = PacketType();
        AsnRuntimeValue value = PacketValue();
        string basicXml = "<Packet>"
            + "<message>A&lt;&amp;é😀</message>"
            + "<empty/>"
            + "<selection><number>7</number></selection>"
            + "<values><Item>é</Item><Item>z</Item></values>"
            + "<open><Payload>9</Payload></open>"
            + "<extra>A5</extra>"
            + "</Packet>";
        string canonicalXml = "<Packet>"
            + "<message>A&lt;&amp;é😀</message>"
            + "<empty/>"
            + "<enabled><true/></enabled>"
            + "<selection><number>7</number></selection>"
            + "<values><Item>z</Item><Item>é</Item></values>"
            + "<open><Payload>9</Payload></open>"
            + "<extra>A5</extra>"
            + "</Packet>";

        AsnRuntimeValue basic = AssertExactRoundTrip(
            packet,
            value,
            AsnEncodingRules.BasicXer,
            basicXml);
        AsnRuntimeValue canonical = AssertExactRoundTrip(
            packet,
            value,
            AsnEncodingRules.CanonicalXer,
            canonicalXml);

        Assert.Multiple(() =>
        {
            Assert.That(Field(basic, "message").TextValue,
                Is.EqualTo("A<&é😀"));
            Assert.That(Field(basic, "empty").TextValue, Is.Empty);
            Assert.That(Field(Field(basic, "selection"), "number")
                .IntegerValue, Is.EqualTo(new System.Numerics.BigInteger(7)));
            Assert.That(Field(basic, "values").Items.Select(
                item => item.TextValue), Is.EqualTo(new[] { "é", "z" }));
            Assert.That(Field(canonical, "values").Items.Select(
                item => item.TextValue), Is.EqualTo(new[] { "z", "é" }));
            Assert.That(Field(canonical, "enabled").BooleanValue, Is.True);
            Assert.That(Field(basic, "open").OpenTypeValue?.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(9)));
            Assert.That(Field(canonical, "extra").Octets,
                Is.EqualTo(new byte[] { 0xa5 }));
        });
    }

    [TestCase(AsnEncodingRules.BasicXer)]
    [TestCase(AsnEncodingRules.CanonicalXer)]
    public void Utf8EntityAndMarkupDecodeAtEveryByteSplit(
        AsnEncodingRules rules)
    {
        AsnTypeDescriptor type = Type(AsnTypeKind.Utf8String, "Text");
        AsnReflectionCatalog catalog = Catalog(type);
        AsnCodecOptions options = Options(rules);
        byte[] data = System.Text.Encoding.UTF8.GetBytes(
            "<Text>Grüße &lt;&amp; 😀</Text>");

        int split = 0;
        while (split <= data.Length)
        {
            XerIncrementalSchemaDecoder decoder =
                new XerIncrementalSchemaDecoder(catalog, "Module.Text", options);
            AsnIncrementalResult first = decoder.Consume(
                new AsnIncrementalInput
                {
                    Data = data,
                    HasCount = true,
                    Count = split
                });
            AsnIncrementalResult second = decoder.Consume(
                new AsnIncrementalInput
                {
                    Data = data,
                    Offset = split,
                    HasCount = true,
                    Count = data.Length - split,
                    IsFinalBlock = true
                });

            Assert.Multiple(() =>
            {
                Assert.That(first.Status,
                    Is.EqualTo(AsnDecodeStatus.NeedMoreData),
                    "first block at split " + split);
                Assert.That(second.Status,
                    Is.EqualTo(AsnDecodeStatus.Done),
                    "final block at split " + split);
                Assert.That(second.Value?.TextValue,
                    Is.EqualTo("Grüße <& 😀"),
                    "value at split " + split);
            });
            split = split + 1;
        }
    }

    [TestCase("<Text>&bogus;</Text>")]
    [TestCase("<Text><nested/></Text>")]
    [TestCase("<Text>value</Wrong>")]
    [TestCase("<Text a=\"1\" a=\"2\">value</Text>")]
    public void MalformedXmlNeverProducesASchemaValue(string xml)
    {
        AsnTypeDescriptor type = Type(AsnTypeKind.Utf8String, "Text");
        AsnDecodeResult result = new XerSchemaCodec(
            Catalog(type),
            AsnEncodingRules.BasicXer).DecodeRequest(new AsnDecodeRequest
            {
                TypeName = "Module.Text",
                Data = System.Text.Encoding.UTF8.GetBytes(xml),
                Options = Options(AsnEncodingRules.BasicXer)
            });

        Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
    }

    private static AsnRuntimeValue AssertExactRoundTrip(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        AsnEncodingRules rules,
        string expected)
    {
        XerSchemaCodec codec = new XerSchemaCodec(Catalog(type), rules);
        AsnCodecOptions options = Options(rules);
        string typeName = "Module." + type.Name;
        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = typeName,
            Value = value,
            Options = options
        });
        byte[] expectedData = System.Text.Encoding.UTF8.GetBytes(expected);
        AsnDecodeResult decoded = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = typeName,
            Data = expectedData,
            Options = options
        });

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True,
                rules + " " + type.Kind + Diagnostic(encoded.Diagnostics));
            Assert.That(System.Text.Encoding.UTF8.GetString(encoded.Data),
                Is.EqualTo(expected),
                rules + " " + type.Kind);
            Assert.That(decoded.Status,
                Is.EqualTo(AsnDecodeStatus.Done),
                rules + " " + type.Kind + Diagnostic(decoded.Diagnostics));
            Assert.That(decoded.ConsumedCount,
                Is.EqualTo(expectedData.Length),
                rules + " " + type.Kind);
        });
        return decoded.Value ?? new AsnRuntimeValue();
    }

    private static string Diagnostic(AsnDiagnostic[] diagnostics)
    {
        return diagnostics.Length == 0
            ? ""
            : ": " + diagnostics[0].Code + " " + diagnostics[0].Message;
    }

    private static AsnTypeDescriptor PacketType()
    {
        AsnFieldDescriptor message = Descriptor(
            "message",
            Type(AsnTypeKind.Utf8String, "Message"),
            0);
        AsnFieldDescriptor empty = Descriptor(
            "empty",
            Type(AsnTypeKind.Utf8String, "Empty"),
            1);
        AsnFieldDescriptor optional = Descriptor(
            "optional",
            Type(AsnTypeKind.Boolean, "Optional"),
            2);
        optional.Optional = true;
        AsnFieldDescriptor enabled = Descriptor(
            "enabled",
            Type(AsnTypeKind.Boolean, "Enabled"),
            3);
        enabled.HasDefaultValue = true;
        enabled.DefaultValue = new AsnValueDescriptor
        {
            Value = new AsnValue
            {
                Kind = AsnValueKind.Boolean,
                TextValue = "TRUE"
            }
        };

        AsnFieldDescriptor number = Descriptor(
            "number",
            Type(AsnTypeKind.Integer, "Number"),
            0);
        AsnFieldDescriptor text = Descriptor(
            "text",
            Type(AsnTypeKind.Utf8String, "ChoiceText"),
            1);
        AsnTypeDescriptor choice = Type(
            AsnTypeKind.Choice,
            "Selection");
        choice.Fields = [number, text];
        choice.RootFields = choice.Fields;
        AsnFieldDescriptor selection = Descriptor("selection", choice, 4);

        AsnTypeDescriptor item = Type(AsnTypeKind.Utf8String, "Item");
        AsnTypeDescriptor set = Type(AsnTypeKind.SetOf, "Values");
        set.ElementType = item;
        AsnFieldDescriptor values = Descriptor("values", set, 5);

        AsnTypeDescriptor payload = Type(AsnTypeKind.Integer, "Payload");
        payload.StableId = "type.payload";
        AsnTypeDescriptor openType = Type(AsnTypeKind.OpenType, "Open");
        openType.ObjectTables =
        [
            new AsnObjectTableDescriptor
            {
                Entries = [new AsnObjectTableEntryDescriptor { Type = payload }]
            }
        ];
        AsnFieldDescriptor open = Descriptor("open", openType, 6);
        AsnFieldDescriptor extra = Descriptor(
            "extra",
            Type(AsnTypeKind.OctetString, "Extra"),
            7);
        extra.Optional = true;
        extra.Extension = true;

        AsnTypeDescriptor packet = Type(AsnTypeKind.Sequence, "Packet");
        packet.Extensible = true;
        packet.Fields =
            [message, empty, optional, enabled, selection, values, open, extra];
        packet.RootFields =
            [message, empty, optional, enabled, selection, values, open];
        packet.ExtensionFields = [extra];
        return packet;
    }

    private static AsnRuntimeValue PacketValue()
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("message", Text("A<&é😀")),
                Present("empty", Text("")),
                Absent("optional"),
                Absent("enabled"),
                Present("selection", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.Choice,
                    Fields = [Present("number", Integer(7))]
                }),
                Present("values", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.SetOf,
                    Items = [Text("é"), Text("z")]
                }),
                Present("open", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.OpenType,
                    OpenTypeValue = new AsnOpenTypeValue
                    {
                        Resolved = true,
                        ResolvedTypeId = "type.payload",
                        TypeName = "Payload",
                        Value = Integer(9)
                    }
                }),
                Present("extra", Octets(0xa5))
            ]
        };
    }

    private static AsnReflectionCatalog Catalog(AsnTypeDescriptor type)
    {
        return new AsnReflectionCatalog
        {
            Assignments =
            [
                new AsnAssignmentDescriptor
                {
                    StableId = "assignment." + type.Name,
                    Name = type.Name,
                    QualifiedName = "Module." + type.Name,
                    GeneratedName = type.Name,
                    Kind = AsnAssignmentKind.Type,
                    Type = type
                }
            ]
        };
    }

    private static AsnCodecOptions Options(AsnEncodingRules rules)
    {
        return new AsnCodecOptions
        {
            EncodingRules = rules,
            UnknownDataPolicy = AsnUnknownDataPolicy.Preserve,
            ValidateValues = false
        };
    }

    private static AsnTypeDescriptor Type(AsnTypeKind kind, string name)
    {
        return new AsnTypeDescriptor
        {
            StableId = "type." + name,
            Name = name,
            GeneratedName = name,
            Kind = kind
        };
    }

    private static AsnFieldDescriptor Descriptor(
        string name,
        AsnTypeDescriptor type,
        int index)
    {
        return new AsnFieldDescriptor
        {
            StableId = "field." + name,
            Name = name,
            Index = index,
            Type = type
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

    private static AsnRuntimeValue Field(
        AsnRuntimeValue value,
        string name)
    {
        return value.Fields.Single(item => item.Name == name).Value
            ?? new AsnRuntimeValue();
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

    private static AsnRuntimeValue Integer(int value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = value
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

    private static AsnRuntimeValue Text(string value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.CharacterString,
            TextValue = value
        };
    }

    private static AsnRuntimeValue TextValue(
        AsnTypeKind kind,
        string value)
    {
        AsnRuntimeValueKind runtimeKind = kind == AsnTypeKind.ObjectIdentifierIri
            ? AsnRuntimeValueKind.ObjectIdentifierIri
            : kind == AsnTypeKind.RelativeObjectIdentifierIri
                ? AsnRuntimeValueKind.RelativeObjectIdentifierIri
                : AsnRuntimeValueKind.CharacterString;
        return new AsnRuntimeValue
        {
            Kind = runtimeKind,
            TextValue = value
        };
    }
}
