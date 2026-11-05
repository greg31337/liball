using asn1;

namespace asn1.Tests;

public class OerStreamingSchemaEncoderTests
{
    [TestCase(AsnEncodingRules.Oer)]
    [TestCase(AsnEncodingRules.Coer)]
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
        AsnRuntimeValue value = Octets(payload);
        AsnEncodeResult expected = new OerSchemaCodec(
            catalog,
            rules).EncodeRequest(new AsnEncodeRequest
            {
                TypeName = "Module.Payload",
                Value = value,
                Options = Options(rules, 30000)
            });
        AsnEncodeResult bounded = new OerSchemaCodec(
            catalog,
            rules).EncodeRequest(new AsnEncodeRequest
            {
                TypeName = "Module.Payload",
                Value = value,
                Options = Options(rules, 32)
            });
        WriteOnlyStream output = new WriteOnlyStream();
        AsnEncodeResult streamed = new OerSchemaCodec(
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
                Is.EqualTo("ASNR0020"));
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

    [TestCase(AsnEncodingRules.Oer)]
    [TestCase(AsnEncodingRules.Coer)]
    public void OpenTypePayloadRemainsBoundedBeforeLengthEmission(
        AsnEncodingRules rules)
    {
        AsnTypeDescriptor open = Type(AsnTypeKind.OpenType, "Open");
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OpenType,
            OpenTypeValue = new AsnOpenTypeValue
            {
                Unresolved = true,
                HasEncodingRules = true,
                EncodingRules = rules,
                HasOriginalData = true,
                OriginalData = new byte[8]
            }
        };
        WriteOnlyStream output = new WriteOnlyStream();

        AsnEncodeResult result = new OerSchemaCodec(
            Catalog(open),
            rules).EncodeStream(new AsnEncodeRequest
            {
                TypeName = "Module.Open",
                Value = value,
                Options = Options(rules, 4)
            }, output);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo("ASNR0020"));
            Assert.That(result.WrittenCount, Is.Zero);
            Assert.That(result.StreamProgress?.Complete, Is.False);
            Assert.That(output.ToArray(), Is.Empty);
        });
    }

    [Test]
    public void ConstructedCanonicalEncodingMatchesBufferedOutput()
    {
        AsnTypeDescriptor item = Type(
            AsnTypeKind.OctetString,
            "Item");
        AsnTypeDescriptor set = Type(AsnTypeKind.SetOf, "Items");
        set.ElementType = item;
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.SetOf,
            Items = [Octets(0x02), Octets(0x01)]
        };
        AsnCodecOptions options = Options(AsnEncodingRules.Coer, 32);
        AsnReflectionCatalog catalog = Catalog(set);
        AsnEncodeRequest request = new AsnEncodeRequest
        {
            TypeName = "Module.Items",
            Value = value,
            Options = options
        };
        AsnEncodeResult expected = new OerSchemaCodec(
            catalog,
            AsnEncodingRules.Coer).EncodeRequest(request);
        WriteOnlyStream output = new WriteOnlyStream();

        AsnEncodeResult streamed = new OerSchemaCodec(
            catalog,
            AsnEncodingRules.Coer).EncodeStream(request, output);

        Assert.Multiple(() =>
        {
            Assert.That(expected.Success, Is.True);
            Assert.That(streamed.Success, Is.True);
            Assert.That(streamed.Data, Is.Empty);
            Assert.That(streamed.WrittenCount,
                Is.EqualTo(expected.WrittenCount));
            Assert.That(streamed.StreamProgress?.Complete, Is.True);
            Assert.That(output.MaximumWriteCount, Is.EqualTo(1));
            Assert.That(output.ToArray(), Is.EqualTo(expected.Data));
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
            UnknownDataPolicy = AsnUnknownDataPolicy.Preserve,
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

    private static AsnRuntimeValue Octets(params byte[] value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OctetString,
            Octets = value
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
