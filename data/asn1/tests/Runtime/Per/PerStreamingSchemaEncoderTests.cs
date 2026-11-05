using asn1;

namespace asn1.Tests;

public class PerStreamingSchemaEncoderTests
{
    [TestCase(AsnEncodingRules.Aper)]
    [TestCase(AsnEncodingRules.Uper)]
    public void LargeOrdinaryPayloadStreamsBeyondRetainedOpenTypeLimit(
        AsnEncodingRules rules)
    {
        byte[] payload = new byte[20000];
        int index = 0;
        while (index < payload.Length)
        {
            payload[index] = (byte)(index % 251);
            index = index + 1;
        }

        AsnTypeDescriptor type = Type(
            AsnTypeKind.OctetString,
            "Payload");
        AsnReflectionCatalog catalog = Catalog(type);
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OctetString,
            Octets = payload
        };
        AsnEncodeResult expected = new PerSchemaCodec(
            catalog,
            rules).EncodeRequest(new AsnEncodeRequest
            {
                TypeName = "Module.Payload",
                Value = value,
                Options = Options(rules, 30000)
            });
        AsnEncodeResult bounded = new PerSchemaCodec(
            catalog,
            rules).EncodeRequest(new AsnEncodeRequest
            {
                TypeName = "Module.Payload",
                Value = value,
                Options = Options(rules, 32)
            });
        WriteOnlyStream output = new WriteOnlyStream();
        AsnEncodeResult streamed = new PerSchemaCodec(
            catalog,
            rules).EncodeStream(new AsnEncodeRequest
            {
                TypeName = "Module.Payload",
                Value = value,
                Options = Options(rules, 32)
            }, output);

        Assert.Multiple(() =>
        {
            Assert.That(expected.Success, Is.True);
            Assert.That(bounded.Success, Is.False);
            Assert.That(bounded.Diagnostics.Single().Code,
                Is.EqualTo("ASNR0006"));
            Assert.That(streamed.Success, Is.True);
            Assert.That(streamed.Data, Is.Empty);
            Assert.That(streamed.WrittenCount,
                Is.EqualTo(expected.WrittenCount));
            Assert.That(streamed.StreamProgress?.WrittenCount,
                Is.EqualTo(expected.WrittenCount));
            Assert.That(streamed.StreamProgress?.ExpectedCount,
                Is.EqualTo(expected.WrittenCount));
            Assert.That(streamed.StreamProgress?.HasExpectedCount, Is.True);
            Assert.That(streamed.StreamProgress?.Complete, Is.True);
            Assert.That(output.CanSeek, Is.False);
            Assert.That(output.MaximumWriteCount, Is.EqualTo(1));
            Assert.That(output.ToArray(), Is.EqualTo(expected.Data));
        });
    }

    [Test]
    public void UnalignedPartialOctetsFlushWithFinalZeroPadding()
    {
        AsnTypeDescriptor integer = Type(AsnTypeKind.Integer, "Number");
        integer.Constraints =
        [
            Bounds(AsnTypeKind.Integer, 0, 7)
        ];
        AsnFieldDescriptor flag = Field(
            "flag",
            Type(AsnTypeKind.Boolean, "Flag"),
            0);
        AsnFieldDescriptor number = Field("number", integer, 1);
        AsnTypeDescriptor packet = Type(AsnTypeKind.Sequence, "Packet");
        packet.Fields = [flag, number];
        packet.RootFields = [flag, number];
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("flag", Boolean(true)),
                Present("number", Integer(5))
            ]
        };
        AsnCodecOptions options = Options(AsnEncodingRules.Uper, 8);
        WriteOnlyStream output = new WriteOnlyStream();

        AsnEncodeResult streamed = new PerSchemaCodec(
            Catalog(packet),
            AsnEncodingRules.Uper).EncodeStream(new AsnEncodeRequest
            {
                TypeName = "Module.Packet",
                Value = value,
                Options = options
            }, output);

        Assert.Multiple(() =>
        {
            Assert.That(streamed.Success, Is.True);
            Assert.That(streamed.WrittenCount, Is.EqualTo(1));
            Assert.That(output.ToArray(), Is.EqualTo(new byte[] { 0xd0 }));
        });
    }

    [Test]
    public void OpenTypePayloadRemainsBoundedBeforeLengthEmission()
    {
        AsnTypeDescriptor open = Type(AsnTypeKind.OpenType, "Open");
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OpenType,
            OpenTypeValue = new AsnOpenTypeValue
            {
                Unresolved = true,
                HasEncodingRules = true,
                EncodingRules = AsnEncodingRules.Aper,
                HasOriginalData = true,
                OriginalData = new byte[8]
            }
        };
        WriteOnlyStream output = new WriteOnlyStream();

        AsnEncodeResult result = new PerSchemaCodec(
            Catalog(open),
            AsnEncodingRules.Aper).EncodeStream(new AsnEncodeRequest
            {
                TypeName = "Module.Open",
                Value = value,
                Options = Options(AsnEncodingRules.Aper, 4)
            }, output);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo("ASNR0006"));
            Assert.That(result.WrittenCount, Is.Zero);
            Assert.That(result.StreamProgress?.Complete, Is.False);
            Assert.That(output.ToArray(), Is.Empty);
        });
    }

    [Test]
    public void CanonicalSetElementsRemainBoundedUntilOrderingCompletes()
    {
        AsnTypeDescriptor item = Type(
            AsnTypeKind.OctetString,
            "Item");
        AsnTypeDescriptor set = Type(AsnTypeKind.SetOf, "Items");
        set.ElementType = item;
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.SetOf,
            Items =
            [
                Octets(0x01),
                new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.OctetString,
                    Octets = new byte[8]
                }
            ]
        };
        WriteOnlyStream output = new WriteOnlyStream();

        AsnEncodeResult result = new PerSchemaCodec(
            Catalog(set),
            AsnEncodingRules.CanonicalUnalignedPer).EncodeStream(
                new AsnEncodeRequest
                {
                    TypeName = "Module.Items",
                    Value = value,
                    Options = Options(
                        AsnEncodingRules.CanonicalUnalignedPer,
                        4)
                },
                output);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo("ASNR0006"));
            Assert.That(result.WrittenCount, Is.Zero);
            Assert.That(output.ToArray(), Is.Empty);
        });
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

    private static AsnCodecOptions Options(
        AsnEncodingRules rules,
        int maximumOpenTypeBytes)
    {
        return new AsnCodecOptions
        {
            EncodingRules = rules,
            ValidateValues = false,
            ResourceLimits = new AsnResourceLimits
            {
                MaximumOpenTypeBytes = maximumOpenTypeBytes,
                MaximumTotalAllocationBytes = 1048576
            }
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
            Type = type,
            Index = index
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

    private sealed class WriteOnlyStream : System.IO.Stream
    {
        private readonly System.IO.MemoryStream storage =
            new System.IO.MemoryStream();

        public int MaximumWriteCount;

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new System.NotSupportedException();

        public override long Position
        {
            get => throw new System.NotSupportedException();
            set => throw new System.NotSupportedException();
        }

        public byte[] ToArray()
        {
            return this.storage.ToArray();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new System.NotSupportedException();
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new System.NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new System.NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count > this.MaximumWriteCount)
            {
                this.MaximumWriteCount = count;
            }

            this.storage.Write(buffer, offset, count);
        }
    }
}
