using asn1;

namespace asn1.Tests;

public class OerIncrementalSchemaDecoderTests
{
    [TestCase(AsnEncodingRules.Oer)]
    [TestCase(AsnEncodingRules.Coer)]
    public void OneByteChunksCrossEveryOerValueBoundary(
        AsnEncodingRules rules)
    {
        AsnTypeDescriptor packet = ComplexPacketType();
        AsnCodecOptions options = Options(rules);
        AsnReflectionCatalog catalog = Catalog(packet);
        AsnEncodeResult encoded = new OerSchemaCodec(
            catalog,
            rules).EncodeRequest(new AsnEncodeRequest
            {
                TypeName = "Module.Packet",
                Value = ComplexPacketValue(),
                Options = options
            });
        OerIncrementalSchemaDecoder decoder =
            new OerIncrementalSchemaDecoder(
                catalog,
                "Module.Packet",
                options);

        AsnIncrementalResult result = new AsnIncrementalResult();
        int index = 0;
        while (index < encoded.Data.Length)
        {
            result = decoder.Consume(new AsnIncrementalInput
            {
                Data = encoded.Data,
                Offset = index,
                HasCount = true,
                Count = 1,
                IsFinalBlock = index == encoded.Data.Length - 1
            });
            if (index < encoded.Data.Length - 1)
            {
                Assert.That(result.Status,
                    Is.EqualTo(AsnDecodeStatus.NeedMoreData));
                Assert.That(result.ConsumedCount, Is.EqualTo(1));
            }

            index = index + 1;
        }

        AsnRuntimeValue? value = result.Value;
        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(value?.Fields.Single(
                field => field.Name == "payload").Value?.Octets,
                Has.Length.EqualTo(128));
            Assert.That(value?.Fields.Single(
                field => field.Name == "number").Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(0x1234)));
            Assert.That(value?.Fields.Single(
                field => field.Name == "signed").Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(-129)));
            Assert.That(value?.Fields.Single(
                field => field.Name == "items").Value?.Items,
                Has.Length.EqualTo(2));
            Assert.That(value?.Fields.Single(
                field => field.Name == "extra").Value?.Octets,
                Is.EqualTo(new byte[] { 0xab }));
        });
    }

    [Test]
    public void CompletionResetAndTerminalStatesAreStable()
    {
        AsnTypeDescriptor payload = Type(
            AsnTypeKind.OctetString,
            "Payload");
        AsnCodecOptions options = Options(AsnEncodingRules.Oer);
        OerIncrementalSchemaDecoder decoder =
            new OerIncrementalSchemaDecoder(
                Catalog(payload),
                "Payload",
                options);

        AsnIncrementalResult waiting = decoder.Consume(
            new AsnIncrementalInput { Data = [0x02, 0xaa] });
        AsnIncrementalResult truncated = decoder.Complete();
        AsnIncrementalResult terminal = decoder.Consume(
            new AsnIncrementalInput
            {
                Data = [0xbb],
                IsFinalBlock = true
            });
        decoder.Reset();
        AsnIncrementalResult reset = decoder.Consume(
            new AsnIncrementalInput
            {
                Data = [0x01, 0xbb],
                IsFinalBlock = true
            });
        AsnIncrementalResult repeated = decoder.Complete();

        Assert.Multiple(() =>
        {
            Assert.That(waiting.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(truncated.Diagnostics.Single().Code,
                Is.EqualTo("ASNO0105"));
            Assert.That(terminal.Diagnostics.Single().Code,
                Is.EqualTo("ASNO0104"));
            Assert.That(reset.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(reset.Value?.Octets,
                Is.EqualTo(new byte[] { 0xbb }));
            Assert.That(repeated.Status, Is.EqualTo(AsnDecodeStatus.Done));
        });
    }

    [Test]
    public void IncrementalBufferHonorsAllocationLimit()
    {
        AsnTypeDescriptor payload = Type(
            AsnTypeKind.OctetString,
            "Payload");
        AsnCodecOptions options = Options(AsnEncodingRules.Oer);
        options.ResourceLimits = new AsnResourceLimits
        {
            MaximumTotalAllocationBytes = 2
        };
        OerIncrementalSchemaDecoder decoder =
            new OerIncrementalSchemaDecoder(
                Catalog(payload),
                "Payload",
                options);

        AsnIncrementalResult result = decoder.Consume(
            new AsnIncrementalInput { Data = [0x02, 0xaa, 0xbb] });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo("ASNR0005"));
            Assert.That(result.Diagnostics.Single().Category,
                Is.EqualTo(AsnDiagnosticCategory.Resource));
        });
    }

    private static AsnTypeDescriptor ComplexPacketType()
    {
        AsnFieldDescriptor flag = Field(
            "flag",
            Type(AsnTypeKind.Boolean, "Flag"),
            0);
        flag.Optional = true;
        AsnFieldDescriptor payload = Field(
            "payload",
            Type(AsnTypeKind.OctetString, "Payload"),
            1);
        AsnTypeDescriptor integer = Type(
            AsnTypeKind.Integer,
            "Number");
        integer.Constraints = [Bounds(0, 65535)];
        AsnFieldDescriptor number = Field("number", integer, 2);
        AsnFieldDescriptor signed = Field(
            "signed",
            Type(AsnTypeKind.Integer, "Signed"),
            3);
        AsnTypeDescriptor list = Type(
            AsnTypeKind.SequenceOf,
            "Items");
        list.ElementType = Type(AsnTypeKind.Boolean, "Item");
        AsnFieldDescriptor items = Field("items", list, 4);
        AsnFieldDescriptor extra = Field(
            "extra",
            Type(AsnTypeKind.OctetString, "Extra"),
            5);
        extra.Extension = true;
        extra.Optional = true;
        AsnTypeDescriptor packet = Type(
            AsnTypeKind.Sequence,
            "Packet");
        packet.Extensible = true;
        packet.Fields = [flag, payload, number, signed, items, extra];
        packet.RootFields = [flag, payload, number, signed, items];
        packet.ExtensionFields = [extra];
        return packet;
    }

    private static AsnRuntimeValue ComplexPacketValue()
    {
        byte[] payload = new byte[128];
        int index = 0;
        while (index < payload.Length)
        {
            payload[index] = (byte)index;
            index = index + 1;
        }

        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("flag", Boolean(true)),
                Present("payload", Octets(payload)),
                Present("number", Integer(0x1234)),
                Present("signed", Integer(-129)),
                Present("items", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.SequenceOf,
                    Items = [Boolean(false), Boolean(true)]
                }),
                Present("extra", Octets(0xab))
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

    private static AsnConstraintDescriptor Bounds(int lower, int upper)
    {
        return new AsnConstraintDescriptor
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
        };
    }

    private static AsnFieldDescriptor Field(
        string name,
        AsnTypeDescriptor type,
        int index)
    {
        return new AsnFieldDescriptor
        {
            Name = name,
            Type = type,
            Index = index
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
}
