using asn1;

namespace asn1.Tests;

public class OerComprehensiveVectorTests
{
    [Test]
    public void EveryFixedIntegerWidthHasExactEndpointVectors()
    {
        AssertIntegerEndpoints(
            0,
            255,
            [0x00],
            [0xff]);
        AssertIntegerEndpoints(
            0,
            65535,
            [0x00, 0x00],
            [0xff, 0xff]);
        AssertIntegerEndpoints(
            0,
            (System.Numerics.BigInteger.One << 32) - 1,
            [0x00, 0x00, 0x00, 0x00],
            [0xff, 0xff, 0xff, 0xff]);
        AssertIntegerEndpoints(
            0,
            (System.Numerics.BigInteger.One << 64) - 1,
            [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
            [0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff]);

        AssertIntegerEndpoints(
            -128,
            127,
            [0x80],
            [0x7f]);
        AssertIntegerEndpoints(
            -32768,
            32767,
            [0x80, 0x00],
            [0x7f, 0xff]);
        AssertIntegerEndpoints(
            -(System.Numerics.BigInteger.One << 31),
            (System.Numerics.BigInteger.One << 31) - 1,
            [0x80, 0x00, 0x00, 0x00],
            [0x7f, 0xff, 0xff, 0xff]);
        AssertIntegerEndpoints(
            -(System.Numerics.BigInteger.One << 63),
            (System.Numerics.BigInteger.One << 63) - 1,
            [0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
            [0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff]);
    }

    [Test]
    public void EveryCharacterAndIriPrimitiveHasAnExactSchemaVector()
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
        string[] texts =
        [
            "/a",
            "a",
            "A",
            "A",
            "1",
            "A",
            "A",
            "A",
            "A",
            "A",
            "A",
            "A",
            "A",
            "A"
        ];
        byte[][] expected =
        [
            [0x02, 0x2f, 0x61],
            [0x01, 0x61],
            [0x01, 0x41],
            [0x01, 0x41],
            [0x01, 0x31],
            [0x01, 0x41],
            [0x01, 0x41],
            [0x01, 0x41],
            [0x01, 0x41],
            [0x01, 0x41],
            [0x01, 0x41],
            [0x01, 0x41],
            [0x04, 0x00, 0x00, 0x00, 0x41],
            [0x02, 0x00, 0x41]
        ];
        AsnEncodingRules[] rules =
        [
            AsnEncodingRules.Oer,
            AsnEncodingRules.Coer
        ];

        int kindIndex = 0;
        while (kindIndex < kinds.Length)
        {
            AsnRuntimeValue value = TextValue(kinds[kindIndex], texts[kindIndex]);
            int rulesIndex = 0;
            while (rulesIndex < rules.Length)
            {
                AsnRuntimeValue decoded = AssertExactRoundTrip(
                    Type(kinds[kindIndex], "Primitive" + kindIndex),
                    value,
                    rules[rulesIndex],
                    expected[kindIndex]);
                Assert.That(decoded.TextValue,
                    Is.EqualTo(texts[kindIndex]),
                    kinds[kindIndex] + " " + rules[rulesIndex]);
                rulesIndex = rulesIndex + 1;
            }

            kindIndex = kindIndex + 1;
        }
    }

    [TestCase(AsnEncodingRules.Oer)]
    [TestCase(AsnEncodingRules.Coer)]
    public void GeneralizedTimeHasAnExactUsefulTimeVector(
        AsnEncodingRules rules)
    {
        AsnRuntimeValue decoded = AssertExactRoundTrip(
            Type(AsnTypeKind.GeneralizedTime, "Generalized"),
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Time,
                TimeValue = new AsnTimeValue
                {
                    Kind = AsnTimeKind.GeneralizedTime,
                    Text = "20240101000000Z"
                }
            },
            rules,
            [
                0x0f,
                0x32, 0x30, 0x32, 0x34, 0x30, 0x31, 0x30, 0x31,
                0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x5a
            ]);

        Assert.That(decoded.TimeValue?.Text, Is.EqualTo("20240101000000Z"));
    }

    [TestCase(AsnEncodingRules.Oer)]
    [TestCase(AsnEncodingRules.Coer)]
    public void CompositeVectorHasExactConstructedEncoding(
        AsnEncodingRules rules)
    {
        AsnTypeDescriptor packet = CompositeType(true);
        AsnRuntimeValue decoded = AssertExactRoundTrip(
            packet,
            CompositeValue(rules),
            rules,
            [
                0xc0,
                0xff,
                0x7f, 0x81, 0x49, 0x02, 0xca, 0xfe,
                0x01, 0x02, 0xff, 0x00,
                0x02, 0x07, 0x80,
                0x03, 0x02, 0xde, 0xad
            ]);

        Assert.Multiple(() =>
        {
            Assert.That(Field(decoded, "flag").BooleanValue, Is.True);
            Assert.That(decoded.Fields.Single(
                item => item.Name == "enabled").Present, Is.False);
            Assert.That(Field(Field(decoded, "selection"), "payload").Octets,
                Is.EqualTo(new byte[] { 0xca, 0xfe }));
            Assert.That(Field(decoded, "items").Items.Select(
                item => item.BooleanValue),
                Is.EqualTo(new bool[] { true, false }));
            Assert.That(Field(decoded, "extra").OpenTypeValue?.OriginalData,
                Is.EqualTo(new byte[] { 0xde, 0xad }));
        });
    }

    [TestCase(AsnEncodingRules.Oer)]
    [TestCase(AsnEncodingRules.Coer)]
    public void CompositeVectorPreservesUnknownExtensionAndStreamsExactly(
        AsnEncodingRules rules)
    {
        AsnTypeDescriptor newer = CompositeType(true);
        AsnTypeDescriptor older = CompositeType(false);
        AsnCodecOptions options = Options(rules);
        AsnEncodeResult encoded = Codec(newer, rules).EncodeRequest(
            new AsnEncodeRequest
            {
                TypeName = "Module.Packet",
                Value = CompositeValue(rules),
                Options = options
            });
        AsnDecodeResult decoded = Codec(older, rules).DecodeRequest(
            new AsnDecodeRequest
            {
                TypeName = "Module.Packet",
                Data = encoded.Data,
                Options = options
            });
        AsnEncodeRequest request = new AsnEncodeRequest
        {
            TypeName = "Module.Packet",
            Value = decoded.Value,
            Options = options
        };
        AsnEncodeResult reencoded = Codec(older, rules).EncodeRequest(request);
        System.IO.MemoryStream output = new System.IO.MemoryStream();
        AsnEncodeResult streamed = Codec(older, rules).EncodeStream(
            request,
            output);

        AsnUnknownExtension unknown = decoded.Value?.UnknownExtensions.Single()
            ?? throw new System.InvalidOperationException();
        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(unknown.Index, Is.Zero);
            Assert.That(unknown.Present, Is.True);
            Assert.That(unknown.BinaryData,
                Is.EqualTo(new byte[] { 0x02, 0xde, 0xad }));
            Assert.That(unknown.EncodingRules, Is.EqualTo(rules));
            Assert.That(reencoded.Data, Is.EqualTo(encoded.Data));
            Assert.That(streamed.Success, Is.True);
            Assert.That(streamed.WrittenCount, Is.EqualTo(encoded.Data.Length));
            Assert.That(output.ToArray(), Is.EqualTo(encoded.Data));
        });
    }

    [TestCase(AsnEncodingRules.Oer)]
    [TestCase(AsnEncodingRules.Coer)]
    public void CompositeVectorDecodesAtEveryByteSplit(
        AsnEncodingRules rules)
    {
        AsnTypeDescriptor type = CompositeType(true);
        AsnCodecOptions options = Options(rules);
        AsnReflectionCatalog catalog = Catalog(type);
        AsnEncodeResult encoded = new OerSchemaCodec(
            catalog,
            rules).EncodeRequest(new AsnEncodeRequest
            {
                TypeName = "Module.Packet",
                Value = CompositeValue(rules),
                Options = options
            });

        int split = 0;
        while (split <= encoded.Data.Length)
        {
            OerIncrementalSchemaDecoder decoder =
                new OerIncrementalSchemaDecoder(
                    catalog,
                    "Module.Packet",
                    options);
            AsnIncrementalResult first = decoder.Consume(
                new AsnIncrementalInput
                {
                    Data = encoded.Data,
                    HasCount = true,
                    Count = split
                });
            AsnIncrementalResult second = decoder.Consume(
                new AsnIncrementalInput
                {
                    Data = encoded.Data,
                    Offset = split,
                    HasCount = true,
                    Count = encoded.Data.Length - split,
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
                Assert.That(Field(second.Value ?? new AsnRuntimeValue(),
                    "extra").OpenTypeValue?.OriginalData,
                    Is.EqualTo(new byte[] { 0xde, 0xad }),
                    "decoded value at split " + split);
            });
            split = split + 1;
        }
    }

    [Test]
    public void CoerRejectsTheNoncanonicalBasicOerBooleanVector()
    {
        AsnTypeDescriptor type = CompositeType(true);
        AsnEncodeResult encoded = Codec(
            type,
            AsnEncodingRules.Oer).EncodeRequest(new AsnEncodeRequest
            {
                TypeName = "Module.Packet",
                Value = CompositeValue(AsnEncodingRules.Oer),
                Options = Options(AsnEncodingRules.Oer)
            });
        byte[] noncanonical = (byte[])encoded.Data.Clone();
        noncanonical[1] = 0x01;

        AsnDecodeResult basic = Codec(
            type,
            AsnEncodingRules.Oer).DecodeRequest(new AsnDecodeRequest
            {
                TypeName = "Module.Packet",
                Data = noncanonical,
                Options = Options(AsnEncodingRules.Oer)
            });
        AsnDecodeResult canonical = Codec(
            type,
            AsnEncodingRules.Coer).DecodeRequest(new AsnDecodeRequest
            {
                TypeName = "Module.Packet",
                Data = noncanonical,
                Options = Options(AsnEncodingRules.Coer)
            });

        Assert.Multiple(() =>
        {
            Assert.That(basic.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(Field(basic.Value ?? new AsnRuntimeValue(),
                "flag").BooleanValue, Is.True);
            Assert.That(canonical.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(canonical.Diagnostics.Single().Code,
                Is.EqualTo("ASNO0023"));
        });
    }

    private static AsnRuntimeValue AssertExactRoundTrip(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        AsnEncodingRules rules,
        byte[] expected)
    {
        OerSchemaCodec codec = Codec(type, rules);
        AsnCodecOptions options = Options(rules);
        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Module." + type.Name,
            Value = value,
            Options = options
        });
        AsnDecodeResult decoded = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module." + type.Name,
            Data = expected,
            Options = options
        });

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True, type.Kind.ToString());
            Assert.That(encoded.Data, Is.EqualTo(expected), type.Kind.ToString());
            Assert.That(decoded.Status,
                Is.EqualTo(AsnDecodeStatus.Done),
                type.Kind.ToString());
            Assert.That(decoded.ConsumedCount,
                Is.EqualTo(expected.Length),
                type.Kind.ToString());
        });
        return decoded.Value ?? new AsnRuntimeValue();
    }

    private static void AssertIntegerEndpoints(
        System.Numerics.BigInteger lower,
        System.Numerics.BigInteger upper,
        byte[] expectedLower,
        byte[] expectedUpper)
    {
        AsnTypeDescriptor type = Type(AsnTypeKind.Integer, "Integer");
        type.Constraints =
        [
            new AsnConstraintDescriptor
            {
                DomainKind = AsnTypeKind.Integer,
                RootIntervals =
                [
                    new AsnIntervalDescriptor
                    {
                        HasLowerBound = true,
                        LowerBound = lower,
                        HasUpperBound = true,
                        UpperBound = upper
                    }
                ]
            }
        ];
        AsnEncodingRules[] rules =
        [
            AsnEncodingRules.Oer,
            AsnEncodingRules.Coer
        ];

        int index = 0;
        while (index < rules.Length)
        {
            AssertExactRoundTrip(
                type,
                Integer(lower),
                rules[index],
                expectedLower);
            AssertExactRoundTrip(
                type,
                Integer(upper),
                rules[index],
                expectedUpper);
            index = index + 1;
        }
    }

    private static AsnTypeDescriptor CompositeType(bool includeExtension)
    {
        AsnFieldDescriptor flag = Descriptor(
            "flag",
            Type(AsnTypeKind.Boolean, "Flag"),
            0);
        flag.Optional = true;
        AsnFieldDescriptor enabled = Descriptor(
            "enabled",
            Type(AsnTypeKind.Boolean, "Enabled"),
            1);
        enabled.HasDefaultValue = true;
        enabled.DefaultValue = new AsnValueDescriptor
        {
            Value = new AsnValue
            {
                Kind = AsnValueKind.Boolean,
                TextValue = "FALSE"
            }
        };

        AsnFieldDescriptor choiceFlag = Descriptor(
            "choiceFlag",
            Type(AsnTypeKind.Boolean, "ChoiceFlag"),
            0);
        choiceFlag.Tags = [Tag(AsnTagClass.ContextSpecific, 2)];
        AsnFieldDescriptor payload = Descriptor(
            "payload",
            Type(AsnTypeKind.OctetString, "Payload"),
            1);
        payload.Tags = [Tag(AsnTagClass.Application, 201)];
        AsnTypeDescriptor choice = Type(
            AsnTypeKind.Choice,
            "Selection");
        choice.Fields = [choiceFlag, payload];
        choice.RootFields = choice.Fields;
        AsnFieldDescriptor selection = Descriptor(
            "selection",
            choice,
            2);

        AsnTypeDescriptor list = Type(
            AsnTypeKind.SequenceOf,
            "Items");
        list.ElementType = Type(AsnTypeKind.Boolean, "Item");
        AsnFieldDescriptor items = Descriptor("items", list, 3);
        AsnFieldDescriptor extra = Descriptor(
            "extra",
            Type(AsnTypeKind.OpenType, "Extra"),
            4);
        extra.Optional = true;
        extra.Extension = true;

        AsnTypeDescriptor packet = Type(AsnTypeKind.Sequence, "Packet");
        packet.Extensible = true;
        packet.RootFields = [flag, enabled, selection, items];
        packet.Fields = includeExtension
            ? [flag, enabled, selection, items, extra]
            : [flag, enabled, selection, items];
        packet.ExtensionFields = includeExtension ? [extra] : [];
        return packet;
    }

    private static AsnRuntimeValue CompositeValue(AsnEncodingRules rules)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("flag", Boolean(true)),
                Present("selection", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.Choice,
                    Fields = [Present("payload", Octets(0xca, 0xfe))]
                }),
                Present("items", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.SequenceOf,
                    Items = [Boolean(true), Boolean(false)]
                }),
                Present("extra", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.OpenType,
                    OpenTypeValue = new AsnOpenTypeValue
                    {
                        Unresolved = true,
                        HasEncodingRules = true,
                        EncodingRules = rules,
                        HasOriginalData = true,
                        OriginalData = [0xde, 0xad]
                    }
                })
            ]
        };
    }

    private static OerSchemaCodec Codec(
        AsnTypeDescriptor type,
        AsnEncodingRules rules)
    {
        return new OerSchemaCodec(Catalog(type), rules);
    }

    private static AsnReflectionCatalog Catalog(AsnTypeDescriptor type)
    {
        return new AsnReflectionCatalog
        {
            Assignments =
            [
                new AsnAssignmentDescriptor
                {
                    Name = type.Name,
                    QualifiedName = "Module." + type.Name,
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

    private static AsnTagDescriptor Tag(AsnTagClass tagClass, int number)
    {
        return new AsnTagDescriptor
        {
            Class = tagClass,
            Number = number,
            Mode = AsnTaggingMode.Implicit
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

    private static AsnRuntimeValue Integer(
        System.Numerics.BigInteger value)
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

    private static AsnRuntimeValue TextValue(
        AsnTypeKind kind,
        string text)
    {
        AsnRuntimeValueKind runtimeKind = kind == AsnTypeKind.ObjectIdentifierIri
            ? AsnRuntimeValueKind.ObjectIdentifierIri
            : kind == AsnTypeKind.RelativeObjectIdentifierIri
                ? AsnRuntimeValueKind.RelativeObjectIdentifierIri
                : AsnRuntimeValueKind.CharacterString;
        return new AsnRuntimeValue
        {
            Kind = runtimeKind,
            TextValue = text
        };
    }
}
