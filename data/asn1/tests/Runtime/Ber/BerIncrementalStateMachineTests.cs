using asn1;

namespace asn1.Tests;

public class BerIncrementalStateMachineTests
{
    [Test]
    public void IncrementalTlvDecoderResumesAtEveryCerByteBoundary()
    {
        byte[] encoded = ComplexCerValue();
        int split = 0;
        while (split <= encoded.Length)
        {
            BerIncrementalTlvDecoder decoder = new BerIncrementalTlvDecoder(
                AsnEncodingRules.Cer,
                true,
                new AsnResourceLimits());
            BerReadResult first = decoder.ConsumeChunk(
                new AsnIncrementalInput
                {
                    Data = encoded,
                    HasCount = true,
                    Count = split
                });
            if (split == encoded.Length)
            {
                Assert.That(first.Status, Is.EqualTo(AsnDecodeStatus.Done));
                Assert.That(first.ConsumedCount, Is.EqualTo(split));
            }
            else
            {
                Assert.That(first.Status,
                    Is.EqualTo(AsnDecodeStatus.NeedMoreData));
                Assert.That(first.ConsumedCount, Is.EqualTo(split));
                BerReadResult second = decoder.ConsumeChunk(
                    new AsnIncrementalInput
                    {
                        Data = encoded,
                        Offset = split,
                        HasCount = true,
                        Count = encoded.Length - split,
                        IsFinalBlock = true
                    });
                Assert.That(second.Status,
                    Is.EqualTo(AsnDecodeStatus.Done));
                Assert.That(second.ConsumedCount,
                    Is.EqualTo(encoded.Length - split));
                Assert.That(second.Tlv?.Children.Length, Is.EqualTo(2));
            }

            split = split + 1;
        }
    }

    [Test]
    public void IncrementalTlvDecoderTracksChunkConsumptionAndTerminalStates()
    {
        BerIncrementalTlvDecoder decoder = new BerIncrementalTlvDecoder(
            AsnEncodingRules.Der,
            true,
            new AsnResourceLimits());
        BerReadResult complete = decoder.ConsumeChunk(
            new AsnIncrementalInput
            {
                Data = [0x02, 0x01, 0x2a, 0x05, 0x00],
                IsFinalBlock = true
            });
        BerReadResult terminal = decoder.ConsumeChunk(
            new AsnIncrementalInput { Data = [] });
        decoder.ResetDecoder();
        BerReadResult partial = decoder.ConsumeChunk(
            new AsnIncrementalInput { Data = [0x02, 0x02, 0x01] });
        BerReadResult truncated = decoder.CompleteInput();

        Assert.Multiple(() =>
        {
            Assert.That(complete.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(complete.ConsumedCount, Is.EqualTo(3));
            Assert.That(terminal.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(terminal.Diagnostics.Single().Code,
                Is.EqualTo("ASND0110"));
            Assert.That(partial.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(partial.ConsumedCount, Is.EqualTo(3));
            Assert.That(truncated.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(truncated.Diagnostics.Single().Code,
                Is.EqualTo("ASND0111"));
        });
    }

    [Test]
    public void IncrementalStrictRulesRejectCanonicalViolationsAfterAssembly()
    {
        BerIncrementalTlvDecoder der = new BerIncrementalTlvDecoder(
            AsnEncodingRules.Der,
            false,
            new AsnResourceLimits());
        byte[] unordered =
            [0x31, 0x06, 0x02, 0x01, 0x02, 0x02, 0x01, 0x01];
        BerReadResult first = der.ConsumeChunk(new AsnIncrementalInput
        {
            Data = unordered,
            HasCount = true,
            Count = 5
        });
        BerReadResult invalid = der.ConsumeChunk(new AsnIncrementalInput
        {
            Data = unordered,
            Offset = 5,
            HasCount = true,
            Count = 3,
            IsFinalBlock = true
        });

        BerIncrementalTlvDecoder cer = new BerIncrementalTlvDecoder(
            AsnEncodingRules.Cer,
            false,
            new AsnResourceLimits());
        BerReadResult definite = cer.ConsumeChunk(new AsnIncrementalInput
        {
            Data = [0x30, 0x03, 0x02, 0x01, 0x01],
            IsFinalBlock = true
        });

        Assert.Multiple(() =>
        {
            Assert.That(first.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(invalid.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(invalid.Diagnostics.Single().Code,
                Is.EqualTo("ASNCAN0008"));
            Assert.That(definite.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(definite.Diagnostics.Single().Code,
                Is.EqualTo("ASNCAN0003"));
        });
    }

    [Test]
    public void IncrementalSchemaDecoderProducesValuesAndCanBeReset()
    {
        AsnTypeDescriptor integer = new AsnTypeDescriptor
        {
            StableId = "type.number",
            Name = "Number",
            Kind = AsnTypeKind.Integer
        };
        AsnReflectionCatalog catalog = Catalog("Module.Number", integer);
        BerIncrementalSchemaDecoder decoder =
            new BerIncrementalSchemaDecoder(
                catalog,
                "Module.Number",
                new AsnCodecOptions
                {
                    EncodingRules = AsnEncodingRules.Der,
                    ValidateValues = false
                });
        AsnIncrementalResult identifier = decoder.Consume(
            new AsnIncrementalInput { Data = [0x02] });
        AsnIncrementalResult length = decoder.Consume(
            new AsnIncrementalInput { Data = [0x01] });
        AsnIncrementalResult value = decoder.Consume(
            new AsnIncrementalInput
            {
                Data = [0x2a],
                IsFinalBlock = true
            });
        decoder.Reset();
        AsnIncrementalResult resetValue = decoder.Consume(
            new AsnIncrementalInput
            {
                Data = [0x02, 0x01, 0x07],
                IsFinalBlock = true
            });

        Assert.Multiple(() =>
        {
            Assert.That(identifier.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(length.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(value.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(value.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(42)));
            Assert.That(resetValue.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(resetValue.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(7)));
        });
    }

    [Test]
    public void IncrementalSchemaDecoderChecksCanonicalImplicitStringSegments()
    {
        AsnFieldDescriptor data = new AsnFieldDescriptor
        {
            Name = "data",
            Type = new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.OctetString
            },
            Tags =
            [
                new AsnTagDescriptor
                {
                    Class = AsnTagClass.ContextSpecific,
                    Number = 0,
                    Mode = AsnTaggingMode.Implicit
                }
            ]
        };
        AsnTypeDescriptor sequence = new AsnTypeDescriptor
        {
            StableId = "type.payload",
            Name = "Payload",
            Kind = AsnTypeKind.Sequence,
            Fields = [data]
        };
        BerTlv implicitString = new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.ContextSpecific,
                Number = 0
            },
            Constructed = true,
            IndefiniteLength = true,
            Children =
            [
                new BerTlv
                {
                    Tag = UniversalTag(AsnUniversalTags.OctetString),
                    Content = new byte[999]
                },
                new BerTlv
                {
                    Tag = UniversalTag(AsnUniversalTags.OctetString),
                    Content = new byte[2]
                }
            ]
        };
        BerTlv root = new BerTlv
        {
            Tag = UniversalTag(AsnUniversalTags.Sequence),
            Constructed = true,
            IndefiniteLength = true,
            Children = [implicitString]
        };
        byte[] nonCanonical = Write(root, AsnEncodingRules.Cer);
        BerIncrementalSchemaDecoder decoder =
            new BerIncrementalSchemaDecoder(
                Catalog("Module.Payload", sequence),
                "Module.Payload",
                new AsnCodecOptions
                {
                    EncodingRules = AsnEncodingRules.Cer,
                    ValidateValues = false
                });
        AsnIncrementalResult result = decoder.Consume(
            new AsnIncrementalInput
            {
                Data = nonCanonical,
                IsFinalBlock = true
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo("ASNCAN0011"));
            Assert.That(result.Diagnostics.Single().Category,
                Is.EqualTo(AsnDiagnosticCategory.Canonicality));
        });
    }

    [Test]
    public void IncrementalTlvEncoderEmitsCerHeadersContentsChildrenAndEoc()
    {
        BerPrimitiveEncodeResult longString = new BerPrimitiveCodec(
            new AsnResourceLimits()).EncodePrimitive(
                AsnTypeKind.OctetString,
                new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.OctetString,
                    Octets = new byte[1001]
                },
                AsnEncodingRules.Cer);
        BerConstructedResult root = new BerConstructedCodec(
            new AsnResourceLimits()).EncodeSequenceOfValue(
                [longString.Tlv ?? throw new InvalidOperationException()],
                AsnEncodingRules.Cer,
                false);
        BerTlv tlv = root.Tlv ?? throw new InvalidOperationException();
        byte[] expected = Write(tlv, AsnEncodingRules.Cer);
        BerIncrementalTlvEncoder encoder = new BerIncrementalTlvEncoder(
            null,
            new AsnResourceLimits());
        BerIncrementalWriteResult started = encoder.StartTlv(
            new BerWriteRequest
            {
                Tlv = tlv,
                EncodingRules = AsnEncodingRules.Cer
            });
        BerIncrementalWriteResult active = encoder.StartTlv(
            new BerWriteRequest
            {
                Tlv = new BerTlv
                {
                    Tag = UniversalTag(AsnUniversalTags.Null)
                },
                EncodingRules = AsnEncodingRules.Cer
            });
        BerIncrementalWriteResult empty = encoder.WriteChunk([]);
        byte[] actual = EmitOneByteAtATime(encoder);

        Assert.Multiple(() =>
        {
            Assert.That(started.Success, Is.True);
            Assert.That(started.ExpectedCount, Is.EqualTo(expected.Length));
            Assert.That(active.Success, Is.False);
            Assert.That(active.Diagnostics.Single().Code,
                Is.EqualTo("ASNE0105"));
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.Done, Is.False);
            Assert.That(empty.WrittenCount, Is.Zero);
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual.TakeLast(4),
                Is.EqualTo(new byte[] { 0x00, 0x00, 0x00, 0x00 }));
        });
    }

    [Test]
    public void IncrementalSchemaEncoderCompletesCanonicalSortBeforeEmission()
    {
        AsnTypeDescriptor integer = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Integer
        };
        AsnTypeDescriptor setOf = new AsnTypeDescriptor
        {
            StableId = "type.numbers",
            Name = "Numbers",
            Kind = AsnTypeKind.SetOf,
            ElementType = integer
        };
        AsnReflectionCatalog catalog = Catalog("Module.Numbers", setOf);
        BerIncrementalTlvEncoder encoder = new BerIncrementalTlvEncoder(
            catalog,
            new AsnResourceLimits());
        BerIncrementalWriteResult started = encoder.StartSchema(
            new AsnEncodeRequest
            {
                TypeName = "Module.Numbers",
                Value = new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.SetOf,
                    Items = [IntegerValue(2), IntegerValue(1)]
                },
                Options = new AsnCodecOptions
                {
                    EncodingRules = AsnEncodingRules.Der,
                    ValidateValues = false
                }
            });
        byte[] actual = EmitOneByteAtATime(encoder);

        Assert.Multiple(() =>
        {
            Assert.That(started.Success, Is.True);
            Assert.That(actual, Is.EqualTo(new byte[]
            {
                0x31, 0x06, 0x02, 0x01, 0x01, 0x02, 0x01, 0x02
            }));
            Assert.That(new BerCanonicalValidator(
                new AsnResourceLimits()).ValidateInput(
                    actual,
                    0,
                    actual.Length,
                    AsnEncodingRules.Der).Status,
                Is.EqualTo(AsnCanonicalityStatus.Canonical));
        });
    }

    private static byte[] ComplexCerValue()
    {
        BerTlv root = new BerTlv
        {
            Tag = UniversalTag(AsnUniversalTags.Sequence),
            Constructed = true,
            IndefiniteLength = true,
            Children =
            [
                new BerTlv
                {
                    Tag = new AsnTag
                    {
                        Class = AsnTagClass.Application,
                        Number = 128
                    },
                    Content = new byte[130]
                },
                new BerTlv
                {
                    Tag = UniversalTag(AsnUniversalTags.Sequence),
                    Constructed = true,
                    IndefiniteLength = true,
                    Children =
                    [
                        new BerTlv
                        {
                            Tag = UniversalTag(AsnUniversalTags.OctetString),
                            Content = [0x2a]
                        }
                    ]
                }
            ]
        };
        return Write(root, AsnEncodingRules.Cer);
    }

    private static byte[] EmitOneByteAtATime(
        BerIncrementalTlvEncoder encoder)
    {
        ByteAccumulator bytes = new ByteAccumulator(0);
        bool done = false;
        while (!done)
        {
            byte[] destination = new byte[1];
            BerIncrementalWriteResult result = encoder.WriteChunk(destination);
            Assert.That(result.Success, Is.True);
            Assert.That(result.WrittenCount, Is.LessThanOrEqualTo(1));
            if (result.WrittenCount == 1)
            {
                bytes.AppendByte(destination[0]);
            }

            done = result.Done;
        }

        return bytes.ToArray();
    }

    private static AsnRuntimeValue IntegerValue(int value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = value
        };
    }

    private static AsnReflectionCatalog Catalog(
        string qualifiedName,
        AsnTypeDescriptor type)
    {
        return new AsnReflectionCatalog
        {
            Assignments =
            [
                new AsnAssignmentDescriptor
                {
                    Kind = AsnAssignmentKind.Type,
                    Name = type.Name,
                    QualifiedName = qualifiedName,
                    StableId = type.StableId,
                    Type = type
                }
            ]
        };
    }

    private static AsnTag UniversalTag(int number)
    {
        return new AsnTag
        {
            Class = AsnTagClass.Universal,
            Number = number
        };
    }

    private static byte[] Write(BerTlv tlv, AsnEncodingRules rules)
    {
        BerWriteResult written = new BerTlvWriterImplementation(
            new AsnResourceLimits()).Write(new BerWriteRequest
            {
                Tlv = tlv,
                EncodingRules = rules
            });
        Assert.That(written.Success, Is.True);
        return written.Data;
    }
}
