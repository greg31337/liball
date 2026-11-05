using asn1;

namespace asn1.Tests;

public class PerIndependentOracleVectorTests
{
    // Independently reproduced with asn1tools 0.167.0 and pycrate 0.8.1.
    // Both implementations used this schema unchanged:
    // Oracle DEFINITIONS AUTOMATIC TAGS ::= BEGIN
    // Packet ::= SEQUENCE {
    //   flag BOOLEAN,
    //   count INTEGER (0..65535),
    //   mode ENUMERATED { idle(0), run(1), stop(2) },
    //   payload OCTET STRING (SIZE(3))
    // }
    // END
    // Abstract value: { flag TRUE, count 4660, mode run,
    //                   payload 'CAFEBA'H }
    // APER: 56 bits, 80 12 34 40 CA FE BA
    // UPER: 43 bits, 89 1A 39 5F D7 40 (last five bits are padding).
    [Test]
    public void PacketMatchesTwoIndependentPerImplementations()
    {
        AsnTypeDescriptor packet = CreatePacketType();
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("flag", Boolean(true)),
                Present("count", Integer(4660)),
                Present("mode", Enumerated("run")),
                Present("payload", Octets(0xca, 0xfe, 0xba))
            ]
        };

        AsnRuntimeValue aligned = AssertExactVector(
            packet,
            value,
            AsnEncodingRules.Aper,
            [0x80, 0x12, 0x34, 0x40, 0xca, 0xfe, 0xba],
            56);
        AsnRuntimeValue unaligned = AssertExactVector(
            packet,
            value,
            AsnEncodingRules.Uper,
            [0x89, 0x1a, 0x39, 0x5f, 0xd7, 0x40],
            43);

        Assert.Multiple(() =>
        {
            Assert.That(Field(aligned, "flag").BooleanValue, Is.True);
            Assert.That(Field(aligned, "count").IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(4660)));
            Assert.That(Field(aligned, "mode").TextValue, Is.EqualTo("run"));
            Assert.That(Field(aligned, "payload").Octets,
                Is.EqualTo(new byte[] { 0xca, 0xfe, 0xba }));
            Assert.That(Field(unaligned, "count").IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(4660)));
            Assert.That(Field(unaligned, "mode").TextValue, Is.EqualTo("run"));
        });
    }

    // Independently reproduced with asn1tools 0.167.0 and pycrate 0.8.1.
    // Both implementations used this schema unchanged:
    // Oracle2 DEFINITIONS AUTOMATIC TAGS ::= BEGIN
    // Envelope ::= SEQUENCE {
    //   enabled BOOLEAN OPTIONAL,
    //   selector CHOICE {
    //     number INTEGER (0..7),
    //     text IA5String (SIZE(2))
    //   },
    //   ...,
    //   extra BOOLEAN OPTIONAL
    // }
    // END
    // Abstract value: { enabled FALSE, selector text:"AZ", extra TRUE }
    // APER: 48 bits, D4 15 A0 10 01 80
    // UPER: 42 bits, D8 36 80 40 60 00 (last six bits are padding).
    [Test]
    public void ExtensionAndChoiceMatchTwoIndependentPerImplementations()
    {
        AsnTypeDescriptor envelope = CreateEnvelopeType();
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("enabled", Boolean(false)),
                Present("selector", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.Choice,
                    Fields =
                    [
                        Present("text", Text("AZ"))
                    ]
                }),
                Present("extra", Boolean(true))
            ]
        };

        AsnRuntimeValue aligned = AssertExactVector(
            envelope,
            value,
            AsnEncodingRules.Aper,
            [0xd4, 0x15, 0xa0, 0x10, 0x01, 0x80],
            48);
        AsnRuntimeValue unaligned = AssertExactVector(
            envelope,
            value,
            AsnEncodingRules.Uper,
            [0xd8, 0x36, 0x80, 0x40, 0x60, 0x00],
            42);

        Assert.Multiple(() =>
        {
            Assert.That(Field(aligned, "enabled").BooleanValue, Is.False);
            Assert.That(Field(Field(aligned, "selector"), "text").TextValue,
                Is.EqualTo("AZ"));
            Assert.That(Field(aligned, "extra").BooleanValue, Is.True);
            Assert.That(Field(unaligned, "enabled").BooleanValue, Is.False);
            Assert.That(Field(Field(unaligned, "selector"), "text").TextValue,
                Is.EqualTo("AZ"));
            Assert.That(Field(unaligned, "extra").BooleanValue, Is.True);
        });
    }

    private static AsnRuntimeValue AssertExactVector(
        AsnTypeDescriptor type,
        AsnRuntimeValue value,
        AsnEncodingRules rules,
        byte[] expected,
        int expectedBitCount)
    {
        PerSchemaCodec codec = new PerSchemaCodec(Catalog(type), rules);
        AsnCodecOptions options = Options(rules);
        PerBitWriter writer = new PerBitWriter(new AsnResourceLimits());
        PerSchemaResult encoded = codec.EncodeType(
            type,
            value,
            writer,
            options,
            0);
        PerBitWriteResult output = writer.GetOutput();
        PerBitReader reader = new PerBitReader(new AsnResourceLimits());
        reader.AppendInputBits(expected, 0, expectedBitCount, true);
        PerSchemaResult decoded = codec.DecodeType(
            type,
            reader,
            options,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(output.WrittenBitCount, Is.EqualTo(expectedBitCount));
            Assert.That(output.Data, Is.EqualTo(expected));
            Assert.That(decoded.Success, Is.True);
            Assert.That(reader.GetAvailableBitCount(), Is.Zero);
        });

        return decoded.Value ?? new AsnRuntimeValue();
    }

    private static AsnTypeDescriptor CreatePacketType()
    {
        AsnTypeDescriptor count = Type(AsnTypeKind.Integer, "Count");
        count.Constraints = [Bounds(AsnTypeKind.Integer, 0, 65535)];
        AsnTypeDescriptor mode = Type(AsnTypeKind.Enumerated, "Mode");
        mode.RootNamedValues =
        [
            Named("idle", 0),
            Named("run", 1),
            Named("stop", 2)
        ];
        mode.NamedValues = mode.RootNamedValues;
        AsnTypeDescriptor payload = Type(
            AsnTypeKind.OctetString,
            "Payload");
        payload.Constraints = [Bounds(AsnTypeKind.OctetString, 3, 3)];
        AsnFieldDescriptor flagField = Descriptor(
            "flag",
            Type(AsnTypeKind.Boolean, "Flag"),
            0);
        AsnFieldDescriptor countField = Descriptor("count", count, 1);
        AsnFieldDescriptor modeField = Descriptor("mode", mode, 2);
        AsnFieldDescriptor payloadField = Descriptor("payload", payload, 3);
        AsnTypeDescriptor packet = Type(AsnTypeKind.Sequence, "Packet");
        packet.Fields = [flagField, countField, modeField, payloadField];
        packet.RootFields = packet.Fields;
        return packet;
    }

    private static AsnTypeDescriptor CreateEnvelopeType()
    {
        AsnTypeDescriptor number = Type(AsnTypeKind.Integer, "Number");
        number.Constraints = [Bounds(AsnTypeKind.Integer, 0, 7)];
        AsnTypeDescriptor text = Type(AsnTypeKind.Ia5String, "Text");
        text.Constraints = [Bounds(AsnTypeKind.Ia5String, 2, 2)];
        AsnFieldDescriptor numberField = Descriptor("number", number, 0);
        AsnFieldDescriptor textField = Descriptor("text", text, 1);
        AsnTypeDescriptor selector = Type(
            AsnTypeKind.Choice,
            "Selector");
        selector.Fields = [numberField, textField];
        selector.RootFields = selector.Fields;

        AsnFieldDescriptor enabled = Descriptor(
            "enabled",
            Type(AsnTypeKind.Boolean, "Enabled"),
            0);
        enabled.Optional = true;
        AsnFieldDescriptor selectorField = Descriptor(
            "selector",
            selector,
            1);
        AsnFieldDescriptor extra = Descriptor(
            "extra",
            Type(AsnTypeKind.Boolean, "Extra"),
            2);
        extra.Optional = true;
        extra.Extension = true;
        AsnTypeDescriptor envelope = Type(
            AsnTypeKind.Sequence,
            "Envelope");
        envelope.Extensible = true;
        envelope.Fields = [enabled, selectorField, extra];
        envelope.RootFields = [enabled, selectorField];
        envelope.ExtensionFields = [extra];
        return envelope;
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

    private static AsnConstraintDescriptor Bounds(
        AsnTypeKind kind,
        int lower,
        int upper)
    {
        return new AsnConstraintDescriptor
        {
            DomainKind = kind,
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

    private static AsnValueDescriptor Named(string name, int value)
    {
        return new AsnValueDescriptor
        {
            Name = name,
            Kind = AsnValueKind.Integer,
            Value = new AsnValue
            {
                Kind = AsnValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = value
            }
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
        AsnRuntimeFieldValue? field = value.Fields.FirstOrDefault(
            item => item.Name == name);
        return field?.Value ?? new AsnRuntimeValue();
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

    private static AsnRuntimeValue Enumerated(string value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Enumerated,
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

    private static AsnRuntimeValue Text(string value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.CharacterString,
            TextValue = value
        };
    }
}
