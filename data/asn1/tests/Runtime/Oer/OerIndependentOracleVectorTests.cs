using asn1;

namespace asn1.Tests;

public class OerIndependentOracleVectorTests
{
    // Independently reproduced with asn1tools 0.167.0 and pycrate 0.8.1.
    // Reproduction APIs:
    //   asn1tools.compile_string(schema, "oer").encode("Packet", value)
    //   pycrate Packet.to_oer(value) and Packet.to_coer(value)
    // Both implementations used this schema unchanged:
    // Oracle DEFINITIONS AUTOMATIC TAGS ::= BEGIN
    // Packet ::= SEQUENCE {
    //   flag BOOLEAN,
    //   count INTEGER (0..65535),
    //   label IA5String,
    //   items SEQUENCE OF INTEGER (0..255)
    // }
    // END
    // Abstract value: { flag TRUE, count 4660, label "AZ",
    //                   items { 1, 255 } }
    // OER and COER: FF 12 34 02 41 5A 01 02 01 FF
    [Test]
    public void StructuredFixtureMatchesIndependentOerAndCoerEncoders()
    {
        AsnTypeDescriptor packet = PacketType();
        AsnRuntimeValue value = PacketValue();
        byte[] expected =
        [
            0xff, 0x12, 0x34, 0x02, 0x41,
            0x5a, 0x01, 0x02, 0x01, 0xff
        ];

        AsnRuntimeValue basic = AssertExactRoundTrip(
            packet,
            value,
            AsnEncodingRules.Oer,
            expected);
        AsnRuntimeValue canonical = AssertExactRoundTrip(
            packet,
            value,
            AsnEncodingRules.Coer,
            expected);

        AssertPacketValue(basic);
        AssertPacketValue(canonical);
    }

    // Both independent OER decoders map these inputs to the same abstract value:
    //   canonical:     FF 12 34 02 41 5A 01 02 01 FF
    //   non-canonical: 01 12 34 02 41 5A 01 02 01 FF
    // BASIC-OER permits any nonzero BOOLEAN TRUE octet. COER requires FF.
    [Test]
    public void PairedInputsHaveEqualValuesAndDistinctCanonicalityResults()
    {
        AsnTypeDescriptor packet = PacketType();
        byte[] canonicalData =
        [
            0xff, 0x12, 0x34, 0x02, 0x41,
            0x5a, 0x01, 0x02, 0x01, 0xff
        ];
        byte[] noncanonicalData =
        [
            0x01, 0x12, 0x34, 0x02, 0x41,
            0x5a, 0x01, 0x02, 0x01, 0xff
        ];
        OerSchemaCodec basicCodec = new OerSchemaCodec(
            Catalog(packet),
            AsnEncodingRules.Oer);
        AsnDecodeResult canonicalValue = basicCodec.DecodeRequest(
            DecodeRequest(AsnEncodingRules.Oer, canonicalData));
        AsnDecodeResult noncanonicalValue = basicCodec.DecodeRequest(
            DecodeRequest(AsnEncodingRules.Oer, noncanonicalData));
        IAsnCodec canonicalCodec = new CodecProviderImplementation()
            .CreateCodec(
                Catalog(packet),
                Options(AsnEncodingRules.Coer));
        AsnCanonicalityResult canonical = canonicalCodec.CheckCanonicality(
            DecodeRequest(AsnEncodingRules.Coer, canonicalData));
        AsnCanonicalityResult noncanonical = canonicalCodec.CheckCanonicality(
            DecodeRequest(AsnEncodingRules.Coer, noncanonicalData));
        AsnEncodeResult repaired = canonicalCodec.Encode(
            new AsnEncodeRequest
            {
                TypeName = "Module.Packet",
                Value = noncanonicalValue.Value,
                Options = Options(AsnEncodingRules.Coer)
            });

        Assert.Multiple(() =>
        {
            Assert.That(canonicalValue.Status,
                Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(noncanonicalValue.Status,
                Is.EqualTo(AsnDecodeStatus.Done));
            AssertEquivalentPacketValues(
                canonicalValue.Value ?? new AsnRuntimeValue(),
                noncanonicalValue.Value ?? new AsnRuntimeValue());
            Assert.That(canonical.Status,
                Is.EqualTo(AsnCanonicalityStatus.Canonical));
            Assert.That(canonical.Diagnostics, Is.Empty);
            Assert.That(noncanonical.Status,
                Is.EqualTo(AsnCanonicalityStatus.NonCanonical));
            Assert.That(noncanonical.Diagnostics.Single().Code,
                Is.EqualTo("ASNO0023"));
            Assert.That(noncanonical.Diagnostics.Single().Category,
                Is.EqualTo(AsnDiagnosticCategory.Canonicality));
            Assert.That(noncanonical.HasViolationOffset, Is.True);
            Assert.That(noncanonical.ViolationOffset, Is.Zero);
            Assert.That(noncanonical.Repairable, Is.True);
            Assert.That(repaired.Success, Is.True);
            Assert.That(repaired.Data, Is.EqualTo(canonicalData));
        });
    }

    private static AsnRuntimeValue AssertExactRoundTrip(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        AsnEncodingRules rules,
        byte[] expected)
    {
        OerSchemaCodec codec = new OerSchemaCodec(Catalog(type), rules);
        AsnCodecOptions options = Options(rules);
        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Module.Packet",
            Value = value,
            Options = options
        });
        AsnDecodeResult decoded = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.Packet",
            Data = expected,
            Options = options
        });

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True, rules.ToString());
            Assert.That(encoded.Data, Is.EqualTo(expected), rules.ToString());
            Assert.That(decoded.Status,
                Is.EqualTo(AsnDecodeStatus.Done),
                rules.ToString());
            Assert.That(decoded.ConsumedCount,
                Is.EqualTo(expected.Length),
                rules.ToString());
        });
        return decoded.Value ?? new AsnRuntimeValue();
    }

    private static void AssertPacketValue(AsnRuntimeValue value)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Field(value, "flag").BooleanValue, Is.True);
            Assert.That(Field(value, "count").IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(4660)));
            Assert.That(Field(value, "label").TextValue, Is.EqualTo("AZ"));
            Assert.That(Field(value, "items").Items.Select(
                item => item.IntegerValue),
                Is.EqualTo(new System.Numerics.BigInteger[] { 1, 255 }));
        });
    }

    private static void AssertEquivalentPacketValues(
        AsnRuntimeValue expected,
        AsnRuntimeValue actual)
    {
        Assert.That(Field(actual, "flag").BooleanValue,
            Is.EqualTo(Field(expected, "flag").BooleanValue));
        Assert.That(Field(actual, "count").IntegerValue,
            Is.EqualTo(Field(expected, "count").IntegerValue));
        Assert.That(Field(actual, "label").TextValue,
            Is.EqualTo(Field(expected, "label").TextValue));
        Assert.That(Field(actual, "items").Items.Select(
            item => item.IntegerValue),
            Is.EqualTo(Field(expected, "items").Items.Select(
                item => item.IntegerValue)));
    }

    private static AsnTypeDescriptor PacketType()
    {
        AsnTypeDescriptor count = Type(AsnTypeKind.Integer, "Count");
        count.Constraints = [Bounds(0, 65535)];
        AsnTypeDescriptor label = Type(AsnTypeKind.Ia5String, "Label");
        AsnTypeDescriptor item = Type(AsnTypeKind.Integer, "Item");
        item.Constraints = [Bounds(0, 255)];
        AsnTypeDescriptor items = Type(
            AsnTypeKind.SequenceOf,
            "Items");
        items.ElementType = item;
        AsnFieldDescriptor flagField = Descriptor(
            "flag",
            Type(AsnTypeKind.Boolean, "Flag"),
            0);
        AsnFieldDescriptor countField = Descriptor("count", count, 1);
        AsnFieldDescriptor labelField = Descriptor("label", label, 2);
        AsnFieldDescriptor itemsField = Descriptor("items", items, 3);
        AsnTypeDescriptor packet = Type(AsnTypeKind.Sequence, "Packet");
        packet.Fields = [flagField, countField, labelField, itemsField];
        packet.RootFields = packet.Fields;
        return packet;
    }

    private static AsnRuntimeValue PacketValue()
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("flag", Boolean(true)),
                Present("count", Integer(4660)),
                Present("label", Text("AZ")),
                Present("items", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.SequenceOf,
                    Items = [Integer(1), Integer(255)]
                })
            ]
        };
    }

    private static AsnDecodeRequest DecodeRequest(
        AsnEncodingRules rules,
        byte[] data)
    {
        return new AsnDecodeRequest
        {
            TypeName = "Module.Packet",
            Data = data,
            Options = Options(rules)
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
                    StableId = "assignment.Packet",
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
            UnknownDataPolicy = AsnUnknownDataPolicy.Reject,
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

    private static AsnConstraintDescriptor Bounds(int lower, int upper)
    {
        return new AsnConstraintDescriptor
        {
            Kind = AsnConstraintKind.ValueRange,
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

    private static AsnRuntimeValue Text(string value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.CharacterString,
            TextValue = value
        };
    }
}
