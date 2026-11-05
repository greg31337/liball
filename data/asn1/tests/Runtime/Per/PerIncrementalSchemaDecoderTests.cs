using asn1;

namespace asn1.Tests;

public class PerIncrementalSchemaDecoderTests
{
    [TestCase(AsnEncodingRules.Aper)]
    [TestCase(AsnEncodingRules.Uper)]
    public void OneBitChunksCrossPreamblesAlignmentOpenTypesAndNestedValues(
        AsnEncodingRules rules)
    {
        AsnTypeDescriptor packet = ComplexPacketType();
        AsnCodecOptions options = Options(rules);
        AsnEncodeResult encoded = new PerSchemaCodec(
            Catalog(packet),
            rules).EncodeRequest(new AsnEncodeRequest
            {
                TypeName = "Module.Packet",
                Value = ComplexPacketValue(),
                Options = options
            });
        PerIncrementalSchemaDecoder decoder =
            new PerIncrementalSchemaDecoder(
                Catalog(packet),
                "Module.Packet",
                options);

        AsnIncrementalResult result = new AsnIncrementalResult();
        int bit = 0;
        while (bit < encoded.Data.Length * 8)
        {
            result = decoder.ConsumeBits(
                Bit(encoded.Data, bit),
                0,
                1,
                bit == encoded.Data.Length * 8 - 1);
            if (bit < encoded.Data.Length * 8 - 1)
            {
                Assert.That(result.Status,
                    Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            }

            bit = bit + 1;
        }

        AsnRuntimeValue? decoded = result.Value;
        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Data, Is.EqualTo(ExpectedPacket(rules)));
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded?.Fields.Single(
                field => field.Name == "header").Value?.Fields.Single(
                    field => field.Name == "payload").Value?.Octets,
                Is.EqualTo(new byte[] { 0x12, 0x34 }));
            Assert.That(decoded?.Fields.Single(
                field => field.Name == "extra").Value?.Fields.Single().Name,
                Is.EqualTo("text"));
            Assert.That(decoded?.Fields.Single(
                field => field.Name == "extra").Value?.Fields.Single()
                    .Value?.Octets,
                Is.EqualTo(new byte[] { 0xab }));
        });
    }

    [TestCase(AsnEncodingRules.Aper)]
    [TestCase(AsnEncodingRules.Uper)]
    public void BitExactPacketResumesAtEveryInputBitSplit(
        AsnEncodingRules rules)
    {
        AsnTypeDescriptor packet = ComplexPacketType();
        AsnCodecOptions options = Options(rules);
        AsnReflectionCatalog catalog = Catalog(packet);
        AsnEncodeResult encoded = new PerSchemaCodec(
            catalog,
            rules).EncodeRequest(new AsnEncodeRequest
            {
                TypeName = "Module.Packet",
                Value = ComplexPacketValue(),
                Options = options
            });
        int bitCount = encoded.Data.Length * 8;
        int split = 0;
        while (split <= bitCount)
        {
            PerIncrementalSchemaDecoder decoder =
                new PerIncrementalSchemaDecoder(
                    catalog,
                    "Module.Packet",
                    options);
            AsnIncrementalResult first = decoder.ConsumeBits(
                SliceBits(encoded.Data, 0, split),
                0,
                split,
                false);
            AsnIncrementalResult second = decoder.ConsumeBits(
                SliceBits(encoded.Data, split, bitCount - split),
                0,
                bitCount - split,
                true);

            Assert.Multiple(() =>
            {
                Assert.That(encoded.Data, Is.EqualTo(ExpectedPacket(rules)));
                Assert.That(first.Status,
                    Is.EqualTo(AsnDecodeStatus.NeedMoreData),
                    "first chunk at split " + split);
                Assert.That(second.Status,
                    Is.EqualTo(AsnDecodeStatus.Done),
                    "final chunk at split " + split);
                Assert.That(second.Value?.Fields.Single(
                    field => field.Name == "header").Value?.Fields.Single(
                        field => field.Name == "payload").Value?.Octets,
                    Is.EqualTo(new byte[] { 0x12, 0x34 }),
                    "payload at split " + split);
            });
            split = split + 1;
        }
    }

    [Test]
    public void FragmentedDeterminantsResumeBeforeAndAfterFragmentBoundaries()
    {
        AsnTypeDescriptor collection = Type(
            AsnTypeKind.SequenceOf,
            "Nulls");
        collection.ElementType = Type(AsnTypeKind.Null, "Item");
        AsnRuntimeValue[] items = new AsnRuntimeValue[16385];
        int item = 0;
        while (item < items.Length)
        {
            items[item] = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Null
            };
            item = item + 1;
        }

        AsnCodecOptions options = Options(AsnEncodingRules.Uper);
        AsnReflectionCatalog catalog = Catalog(collection);
        AsnEncodeResult encoded = new PerSchemaCodec(
            catalog,
            AsnEncodingRules.Uper).EncodeRequest(new AsnEncodeRequest
            {
                TypeName = "Module.Nulls",
                Value = new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.SequenceOf,
                    Items = items
                },
                Options = options
            });
        PerIncrementalSchemaDecoder decoder =
            new PerIncrementalSchemaDecoder(
                catalog,
                "Module.Nulls",
                options);

        AsnIncrementalResult determinantPrefix = decoder.ConsumeBits(
            [0xc0],
            0,
            7,
            false);
        AsnIncrementalResult firstFragment = decoder.ConsumeBits(
            [0x80],
            0,
            1,
            false);
        AsnIncrementalResult finalFragment = decoder.ConsumeBits(
            [0x01],
            0,
            8,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[] { 0xc1, 0x01 }));
            Assert.That(determinantPrefix.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(firstFragment.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(finalFragment.Status,
                Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(finalFragment.Value?.Items,
                Has.Length.EqualTo(16385));
        });
    }

    [Test]
    public void CompletionResetAndTerminalStatesAreStable()
    {
        AsnTypeDescriptor flag = Type(AsnTypeKind.Boolean, "Flag");
        AsnCodecOptions options = Options(AsnEncodingRules.Aper);
        PerIncrementalSchemaDecoder decoder =
            new PerIncrementalSchemaDecoder(
                Catalog(flag),
                "Module.Flag",
                options);

        AsnIncrementalResult candidate = decoder.Consume(
            new AsnIncrementalInput { Data = [0x80] });
        AsnIncrementalResult completed = decoder.Complete();
        AsnIncrementalResult repeated = decoder.Complete();
        AsnIncrementalResult terminal = decoder.Consume(
            new AsnIncrementalInput
            {
                Data = [0x00],
                IsFinalBlock = true
            });
        decoder.Reset();
        AsnIncrementalResult reset = decoder.Consume(
            new AsnIncrementalInput
            {
                Data = [0x00],
                IsFinalBlock = true
            });

        Assert.Multiple(() =>
        {
            Assert.That(candidate.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(completed.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(completed.Value?.BooleanValue, Is.True);
            Assert.That(repeated.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(terminal.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(terminal.Diagnostics.Single().Code,
                Is.EqualTo("ASNP0097"));
            Assert.That(reset.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(reset.Value?.BooleanValue, Is.False);
        });
    }

    [Test]
    public void FinalTruncationInsideNestedPayloadIsAnError()
    {
        AsnTypeDescriptor payload = Type(
            AsnTypeKind.OctetString,
            "Payload");
        AsnCodecOptions options = Options(AsnEncodingRules.Aper);
        PerIncrementalSchemaDecoder decoder =
            new PerIncrementalSchemaDecoder(
                Catalog(payload),
                "Module.Payload",
                options);

        AsnIncrementalResult waiting = decoder.Consume(
            new AsnIncrementalInput { Data = [0x02] });
        AsnIncrementalResult truncated = decoder.Complete();

        Assert.Multiple(() =>
        {
            Assert.That(waiting.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(truncated.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(truncated.Diagnostics.Single().Code,
                Is.EqualTo("ASNP0004"));
        });
    }

    private static AsnTypeDescriptor ComplexPacketType()
    {
        AsnFieldDescriptor payload = Field(
            "payload",
            Type(AsnTypeKind.OctetString, "Payload"),
            1);
        AsnFieldDescriptor enabled = Field(
            "enabled",
            Type(AsnTypeKind.Boolean, "Enabled"),
            0);
        enabled.Optional = true;
        AsnTypeDescriptor header = Type(AsnTypeKind.Sequence, "Header");
        header.Fields = [enabled, payload];
        header.RootFields = [enabled, payload];

        AsnFieldDescriptor number = Field(
            "number",
            Type(AsnTypeKind.Integer, "Number"),
            0);
        number.Type!.Constraints =
        [
            Bounds(AsnTypeKind.Integer, 0, 255)
        ];
        AsnFieldDescriptor text = Field(
            "text",
            Type(AsnTypeKind.OctetString, "Text"),
            1);
        AsnTypeDescriptor extra = Type(AsnTypeKind.Choice, "Extra");
        extra.Fields = [number, text];
        extra.RootFields = [number, text];

        AsnFieldDescriptor rootFlag = Field(
            "rootFlag",
            Type(AsnTypeKind.Boolean, "RootFlag"),
            0);
        rootFlag.Optional = true;
        AsnFieldDescriptor headerField = Field("header", header, 1);
        AsnFieldDescriptor extraField = Field("extra", extra, 2);
        extraField.Extension = true;
        extraField.Optional = true;
        AsnTypeDescriptor packet = Type(AsnTypeKind.Sequence, "Packet");
        packet.Extensible = true;
        packet.Fields = [rootFlag, headerField, extraField];
        packet.RootFields = [rootFlag, headerField];
        packet.ExtensionFields = [extraField];
        return packet;
    }

    private static AsnRuntimeValue ComplexPacketValue()
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("rootFlag", Boolean(true)),
                Present("header", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.Sequence,
                    Fields =
                    [
                        Present("enabled", Boolean(false)),
                        Present("payload", Octets(0x12, 0x34))
                    ]
                }),
                Present("extra", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.Choice,
                    Fields = [Present("text", Octets(0xab))]
                })
            ]
        };
    }

    private static byte[] Bit(byte[] data, int bitOffset)
    {
        int value = data[bitOffset / 8] >> (7 - bitOffset % 8);
        return [(byte)((value & 1) << 7)];
    }

    private static byte[] SliceBits(
        byte[] data,
        int bitOffset,
        int bitCount)
    {
        byte[] result = new byte[(bitCount + 7) / 8];
        int bit = 0;
        while (bit < bitCount)
        {
            int source = bitOffset + bit;
            int value = data[source / 8] >> (7 - source % 8);
            if ((value & 1) != 0)
            {
                result[bit / 8] = (byte)(result[bit / 8]
                    | 1 << (7 - bit % 8));
            }

            bit = bit + 1;
        }

        return result;
    }

    private static byte[] ExpectedPacket(AsnEncodingRules rules)
    {
        return rules == AsnEncodingRules.Aper
            ? [0xf0, 0x02, 0x12, 0x34, 0x01, 0x03, 0x80, 0x01, 0xab]
            : [0xf0, 0x10, 0x91, 0xa0, 0x08, 0x1c, 0x06, 0xac, 0x00];
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

    private static AsnFieldDescriptor Field(
        string name,
        AsnTypeDescriptor type,
        int index)
    {
        return new AsnFieldDescriptor
        {
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

    private static AsnRuntimeValue Boolean(bool value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Boolean,
            HasBooleanValue = true,
            BooleanValue = value
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
