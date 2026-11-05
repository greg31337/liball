using asn1;

namespace asn1.Tests;

public class StreamingCodecImplementationTests
{
    [TestCase(AsnEncodingRules.Ber)]
    [TestCase(AsnEncodingRules.Der)]
    [TestCase(AsnEncodingRules.Cer)]
    [TestCase(AsnEncodingRules.Aper)]
    [TestCase(AsnEncodingRules.Uper)]
    [TestCase(AsnEncodingRules.CanonicalAlignedPer)]
    [TestCase(AsnEncodingRules.CanonicalUnalignedPer)]
    [TestCase(AsnEncodingRules.Oer)]
    [TestCase(AsnEncodingRules.Coer)]
    [TestCase(AsnEncodingRules.BasicXer)]
    [TestCase(AsnEncodingRules.CanonicalXer)]
    [TestCase(AsnEncodingRules.ExtendedXer)]
    public void RoundTripsWithoutSeekingOrMultiByteWrites(
        AsnEncodingRules rules)
    {
        AsnCodecOptions options = Options(rules);
        ConfiguredCodec configured = Configured(
            Catalog(Type(AsnTypeKind.Boolean, "Flag")),
            options);
        IAsnStreamingCodec streaming = new AsnStreamingCodec(
            new StreamingCodecImplementation(configured));
        AsnEncodeRequest request = new AsnEncodeRequest
        {
            TypeName = "Schema.Flag",
            Value = Boolean(true)
        };
        AsnEncodeResult expected = configured.Encode(request);
        SingleByteWriteStream output = new SingleByteWriteStream();
        AsnEncodeResult encoded = streaming.EncodeStream(request, output);
        byte[] twoValues = Join(expected.Data, expected.Data);
        ShortReadStream input = new ShortReadStream(twoValues);
        AsnDecodeResult decoded = streaming.DecodeStream(
            input,
            "Schema.Flag",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Data, Is.Empty);
            Assert.That(encoded.WrittenCount,
                Is.EqualTo(expected.WrittenCount));
            Assert.That(encoded.StreamProgress?.WrittenCount,
                Is.EqualTo(expected.WrittenCount));
            Assert.That(encoded.StreamProgress?.ExpectedCount,
                Is.EqualTo(expected.WrittenCount));
            Assert.That(encoded.StreamProgress?.Complete, Is.True);
            Assert.That(output.CanSeek, Is.False);
            Assert.That(output.MaximumWriteCount, Is.EqualTo(1));
            Assert.That(output.ToArray(), Is.EqualTo(expected.Data));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Value?.BooleanValue, Is.True);
            Assert.That(decoded.ConsumedCount,
                Is.EqualTo(expected.Data.Length));
            Assert.That(decoded.StreamProgress?.ConsumedCount,
                Is.EqualTo(expected.Data.Length));
            Assert.That(decoded.StreamProgress?.HasExpectedCount, Is.False);
            Assert.That(decoded.StreamProgress?.Complete, Is.True);
            Assert.That(input.CanSeek, Is.False);
            Assert.That(input.ReadCount, Is.EqualTo(expected.Data.Length));
            Assert.That(input.RemainingCount,
                Is.EqualTo(expected.Data.Length));
        });
    }

    [Test]
    public void EcnPlanStreamsWithUnknownInputLength()
    {
        AsnCompilationResult compiled = new EcnCompilerImplementation()
            .CompileEcn(
                new AsnSourceUnit
                {
                    Name = "stream.edm",
                    Text = "Streaming ENCODING-DEFINITIONS ::= BEGIN\n"
                        + "scalar #INT ::= { ENCODING-SPACE, SIZE 8 }\n"
                        + "END"
                },
                [IntegerModule()]);
        AsnReflectionCatalog catalog = compiled.ReflectionCatalog
            ?? throw new AssertionException("ECN reflection was not built.");
        AsnCodecOptions options = Options(AsnEncodingRules.Ecn);
        ConfiguredCodec configured = Configured(catalog, options);
        IAsnStreamingCodec streaming = new StreamingCodecImplementation(
            configured);
        SingleByteWriteStream output = new SingleByteWriteStream();
        AsnEncodeResult encoded = streaming.EncodeStream(
            new AsnEncodeRequest
            {
                TypeName = "Schema.scalar",
                Value = Integer(42)
            },
            output);
        ShortReadStream input = new ShortReadStream([42, 43]);
        AsnDecodeResult decoded = streaming.DecodeStream(
            input,
            "Schema.scalar",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(compiled.Success, Is.True);
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.WrittenCount, Is.EqualTo(1));
            Assert.That(output.ToArray(), Is.EqualTo(new byte[] { 42 }));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(42)));
            Assert.That(decoded.ConsumedCount, Is.EqualTo(1));
            Assert.That(input.ReadCount, Is.EqualTo(1));
            Assert.That(input.RemainingCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void EndOfStreamAndResourceFailuresReportExactProgress()
    {
        AsnCodecOptions options = Options(AsnEncodingRules.Ber);
        ConfiguredCodec configured = Configured(
            Catalog(Type(AsnTypeKind.Boolean, "Flag")),
            options);
        IAsnStreamingCodec streaming = new StreamingCodecImplementation(
            configured);
        AsnDecodeResult incomplete = streaming.DecodeStream(
            new ShortReadStream([0x01, 0x01]),
            "Schema.Flag",
            options);
        AsnCodecOptions limitedOptions = Options(AsnEncodingRules.Ber);
        limitedOptions.ResourceLimits = new AsnResourceLimits
        {
            MaximumTotalAllocationBytes = 2
        };
        IAsnStreamingCodec limited = new StreamingCodecImplementation(
            Configured(
                Catalog(Type(AsnTypeKind.Boolean, "Flag")),
                limitedOptions));
        ShortReadStream limitedInput = new ShortReadStream(
            [0x01, 0x01, 0xff]);
        AsnDecodeResult resource = limited.DecodeStream(
            limitedInput,
            "Schema.Flag",
            limitedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(incomplete.Status,
                Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(incomplete.ConsumedCount, Is.EqualTo(2));
            Assert.That(incomplete.StreamProgress?.ConsumedCount,
                Is.EqualTo(2));
            Assert.That(incomplete.StreamProgress?.Complete, Is.False);
            Assert.That(incomplete.Diagnostics.Single().Code,
                Is.EqualTo("ASNCODEC0007"));
            Assert.That(resource.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(resource.ConsumedCount, Is.EqualTo(2));
            Assert.That(resource.Diagnostics.Single().Code,
                Is.EqualTo("ASNR0005"));
            Assert.That(resource.Diagnostics.Single().Category,
                Is.EqualTo(AsnDiagnosticCategory.Resource));
            Assert.That(limitedInput.ReadCount, Is.EqualTo(2));
            Assert.That(limitedInput.RemainingCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ReadAndWriteExceptionsPropagateWithoutWrapping()
    {
        AsnCodecOptions options = Options(AsnEncodingRules.Ber);
        IAsnStreamingCodec streaming = new StreamingCodecImplementation(
            Configured(
                Catalog(Type(AsnTypeKind.Boolean, "Flag")),
                options));
        System.IO.IOException writeFailure = new System.IO.IOException(
            "write failed");
        System.IO.IOException readFailure = new System.IO.IOException(
            "read failed");

        System.IO.IOException? written = Assert.Throws<System.IO.IOException>(
            () => streaming.EncodeStream(
                new AsnEncodeRequest
                {
                    TypeName = "Schema.Flag",
                    Value = Boolean(true)
                },
                new ThrowingWriteStream(writeFailure)));
        System.IO.IOException? read = Assert.Throws<System.IO.IOException>(
            () => streaming.DecodeStream(
                new ThrowingReadStream(readFailure),
                "Schema.Flag",
                options));

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.SameAs(writeFailure));
            Assert.That(read, Is.SameAs(readFailure));
        });
    }

    [Test]
    public void ZeroLengthValuesAndLookupFailuresDoNotReadAhead()
    {
        AsnCodecOptions perOptions = Options(AsnEncodingRules.Uper);
        IAsnStreamingCodec per = new StreamingCodecImplementation(
            Configured(
                Catalog(Type(AsnTypeKind.Null, "Empty")),
                perOptions));
        ShortReadStream zeroLengthInput = new ShortReadStream([0xaa]);
        AsnDecodeResult empty = per.DecodeStream(
            zeroLengthInput,
            "Schema.Empty",
            perOptions);

        AsnCodecOptions berOptions = Options(AsnEncodingRules.Ber);
        IAsnStreamingCodec ber = new StreamingCodecImplementation(
            Configured(
                Catalog(Type(AsnTypeKind.Boolean, "Flag")),
                berOptions));
        ShortReadStream missingInput = new ShortReadStream(
            [0x01, 0x01, 0xff]);
        AsnDecodeResult missing = ber.DecodeStream(
            missingInput,
            "Schema.Missing",
            berOptions);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(empty.ConsumedCount, Is.Zero);
            Assert.That(empty.StreamProgress?.Complete, Is.True);
            Assert.That(zeroLengthInput.ReadCount, Is.Zero);
            Assert.That(zeroLengthInput.RemainingCount, Is.EqualTo(1));
            Assert.That(missing.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(missing.ConsumedCount, Is.Zero);
            Assert.That(missing.Diagnostics, Is.Not.Empty);
            Assert.That(missingInput.ReadCount, Is.Zero);
            Assert.That(missingInput.RemainingCount, Is.EqualTo(3));
        });
    }

    private static ConfiguredCodec Configured(
        AsnReflectionCatalog catalog,
        AsnCodecOptions options)
    {
        return (ConfiguredCodec)new CodecProviderImplementation()
            .CreateCodec(catalog, options);
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
                    QualifiedName = "Schema." + type.Name,
                    GeneratedName = type.Name,
                    Kind = AsnAssignmentKind.Type,
                    Type = type
                }
            ]
        };
    }

    private static AsnTypeDescriptor Type(
        AsnTypeKind kind,
        string name)
    {
        return new AsnTypeDescriptor
        {
            StableId = "type." + name,
            Name = name,
            GeneratedName = name,
            Kind = kind
        };
    }

    private static AsnModule IntegerModule()
    {
        return new AsnModule
        {
            Name = "Schema",
            Assignments =
            [
                new AsnAssignment
                {
                    Name = "scalar",
                    Kind = AsnAssignmentKind.Type,
                    Type = new AsnType
                    {
                        Name = "scalar",
                        Kind = AsnTypeKind.Integer
                    }
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

    private static byte[] Join(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        System.Buffer.BlockCopy(first, 0, result, 0, first.Length);
        System.Buffer.BlockCopy(
            second,
            0,
            result,
            first.Length,
            second.Length);
        return result;
    }

    private sealed class ShortReadStream : System.IO.Stream
    {
        private readonly byte[] data;
        private int position;

        public ShortReadStream(byte[] data)
        {
            this.data = data;
        }

        public int ReadCount;

        public int RemainingCount => this.data.Length - this.position;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.position >= this.data.Length)
            {
                return 0;
            }
            if (count == 0)
            {
                return 0;
            }

            buffer[offset] = this.data[this.position];
            this.position = this.position + 1;
            this.ReadCount = this.ReadCount + 1;
            return 1;
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class SingleByteWriteStream : System.IO.Stream
    {
        private readonly System.IO.MemoryStream storage =
            new System.IO.MemoryStream();

        public int MaximumWriteCount;

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
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
            throw new NotSupportedException();
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count > 1)
            {
                throw new System.IO.IOException(
                    "The stream accepts only one byte per write.");
            }
            if (count > this.MaximumWriteCount)
            {
                this.MaximumWriteCount = count;
            }
            this.storage.Write(buffer, offset, count);
        }
    }

    private sealed class ThrowingWriteStream : System.IO.Stream
    {
        private readonly System.IO.IOException failure;

        public ThrowingWriteStream(System.IO.IOException failure)
        {
            this.failure = failure;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw this.failure;
        }
    }

    private sealed class ThrowingReadStream : System.IO.Stream
    {
        private readonly System.IO.IOException failure;

        public ThrowingReadStream(System.IO.IOException failure)
        {
            this.failure = failure;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw this.failure;
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
