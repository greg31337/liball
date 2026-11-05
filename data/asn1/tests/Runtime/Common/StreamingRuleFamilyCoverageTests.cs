using asn1;

namespace asn1.Tests;

public class StreamingRuleFamilyCoverageTests
{
    [TestCase(AsnEncodingRules.Ber)]
    [TestCase(AsnEncodingRules.Uper)]
    [TestCase(AsnEncodingRules.Oer)]
    [TestCase(AsnEncodingRules.BasicXer)]
    [TestCase(AsnEncodingRules.Ecn)]
    public void IncrementalDecoderAcceptsDeterministicRandomAndOneByteChunks(
        AsnEncodingRules rules)
    {
        RuleFixture fixture = CreateFixture(rules, 42);
        System.Random random = new System.Random(232 + (int)rules);

        int pass = 0;
        while (pass < 16)
        {
            IAsnIncrementalDecoder decoder = fixture.Codec.CreateIncrementalDecoder(
                fixture.TypeName,
                fixture.Options);
            int offset = 0;
            AsnIncrementalResult result = new AsnIncrementalResult();
            while (offset < fixture.Encoded.Length)
            {
                int maximum = System.Math.Min(5, fixture.Encoded.Length - offset);
                int count = random.Next(1, maximum + 1);
                bool final = offset + count == fixture.Encoded.Length;
                result = decoder.Consume(new AsnIncrementalInput
                {
                    Data = fixture.Encoded,
                    Offset = offset,
                    HasCount = true,
                    Count = count,
                    IsFinalBlock = final
                });
                Assert.That(result.ConsumedCount, Is.EqualTo(count),
                    $"rule={rules}, pass={pass}, offset={offset}");
                Assert.That(result.Status, final
                    ? Is.EqualTo(AsnDecodeStatus.Done)
                    : Is.EqualTo(AsnDecodeStatus.NeedMoreData),
                    $"rule={rules}, pass={pass}, offset={offset}");
                offset = offset + count;
            }

            Assert.That(result.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(42)));
            Assert.That(decoder.Complete().Status, Is.EqualTo(AsnDecodeStatus.Done));

            decoder.Reset();
            int index = 0;
            while (index < fixture.Encoded.Length)
            {
                result = decoder.Consume(new AsnIncrementalInput
                {
                    Data = fixture.Encoded,
                    Offset = index,
                    HasCount = true,
                    Count = 1,
                    IsFinalBlock = index + 1 == fixture.Encoded.Length
                });
                index = index + 1;
            }

            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(result.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(42)));
            pass = pass + 1;
        }
    }

    [TestCase(AsnEncodingRules.Ber)]
    [TestCase(AsnEncodingRules.Uper)]
    [TestCase(AsnEncodingRules.Oer)]
    [TestCase(AsnEncodingRules.BasicXer)]
    [TestCase(AsnEncodingRules.Ecn)]
    public void EveryTruncatedByteBoundaryIsRejectedByIncrementalAndStreamDecoders(
        AsnEncodingRules rules)
    {
        RuleFixture fixture = CreateFixture(rules, 42);

        int boundary = 0;
        while (boundary < fixture.Encoded.Length)
        {
            byte[] prefix = new byte[boundary];
            System.Buffer.BlockCopy(fixture.Encoded, 0, prefix, 0, boundary);
            IAsnIncrementalDecoder incremental = fixture.Codec
                .CreateIncrementalDecoder(fixture.TypeName, fixture.Options);
            AsnIncrementalResult incrementalResult = incremental.Consume(
                new AsnIncrementalInput
                {
                    Data = prefix,
                    IsFinalBlock = true
                });
            CountingReadStream input = new CountingReadStream(prefix);
            AsnDecodeResult streamResult = fixture.Codec.CreateStreamingCodec()
                .DecodeStream(input, fixture.TypeName, fixture.Options);

            Assert.Multiple(() =>
            {
                Assert.That(incrementalResult.Status,
                    Is.EqualTo(AsnDecodeStatus.Error),
                    $"incremental rule={rules}, boundary={boundary}");
                Assert.That(incrementalResult.Diagnostics.Single().Code,
                    Is.EqualTo("ASNCODEC0008"),
                    $"incremental rule={rules}, boundary={boundary}");
                Assert.That(streamResult.Status,
                    Is.EqualTo(AsnDecodeStatus.Error),
                    $"stream rule={rules}, boundary={boundary}");
                Assert.That(streamResult.Diagnostics.Single().Code,
                    Is.EqualTo("ASNCODEC0007"),
                    $"stream rule={rules}, boundary={boundary}");
                Assert.That(streamResult.ConsumedCount, Is.EqualTo(boundary));
                Assert.That(input.ReadByteCount, Is.EqualTo(boundary));
                Assert.That(input.ZeroReadCount, Is.EqualTo(1));
            });
            boundary = boundary + 1;
        }
    }

    [TestCase(AsnEncodingRules.Ber)]
    [TestCase(AsnEncodingRules.Uper)]
    [TestCase(AsnEncodingRules.Oer)]
    [TestCase(AsnEncodingRules.BasicXer)]
    [TestCase(AsnEncodingRules.Ecn)]
    public void StreamFailuresAfterPartialIoPropagateForEveryRuleFamily(
        AsnEncodingRules rules)
    {
        RuleFixture fixture = CreateFixture(rules, 42);
        System.IO.IOException writeFailure = new System.IO.IOException(
            rules + " write failure");
        System.IO.IOException readFailure = new System.IO.IOException(
            rules + " read failure");
        FailingWriteStream output = new FailingWriteStream(writeFailure, 1);
        FailingReadStream input = new FailingReadStream(
            fixture.Encoded,
            readFailure,
            1);
        IAsnStreamingCodec streaming = fixture.Codec.CreateStreamingCodec();

        System.IO.IOException? written = Assert.Throws<System.IO.IOException>(
            () => streaming.EncodeStream(
                new AsnEncodeRequest
                {
                    TypeName = fixture.TypeName,
                    Value = Integer(42)
                },
                output));
        System.IO.IOException? read = Assert.Throws<System.IO.IOException>(
            () => streaming.DecodeStream(
                input,
                fixture.TypeName,
                fixture.Options));

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Encoded.Length, Is.GreaterThan(1));
            Assert.That(written, Is.SameAs(writeFailure));
            Assert.That(read, Is.SameAs(readFailure));
            Assert.That(output.WrittenCount, Is.EqualTo(1));
            Assert.That(input.ReadByteCount, Is.EqualTo(1));
        });
    }

    private static RuleFixture CreateFixture(
        AsnEncodingRules rules,
        int value)
    {
        AsnCodecOptions options = new AsnCodecOptions
        {
            EncodingRules = rules,
            UnknownDataPolicy = AsnUnknownDataPolicy.Reject,
            ValidateValues = false
        };
        AsnReflectionCatalog catalog;
        string typeName;
        if (rules == AsnEncodingRules.Ecn)
        {
            AsnCompilationResult compiled = new EcnCompilerImplementation()
                .CompileEcn(
                    new AsnSourceUnit
                    {
                        Name = "stream-family.edm",
                        Text = "StreamingFamilies ENCODING-DEFINITIONS ::= BEGIN\n"
                            + "scalar #INT ::= { ENCODING-SPACE, SIZE 16 }\n"
                            + "END"
                    },
                    [IntegerModule()]);
            Assert.That(compiled.Success, Is.True, Format(compiled));
            catalog = compiled.ReflectionCatalog
                ?? throw new AssertionException("ECN reflection was not built.");
            typeName = "Schema.scalar";
        }
        else
        {
            catalog = IntegerCatalog();
            typeName = "Schema.Number";
        }

        ConfiguredCodec codec = (ConfiguredCodec)new CodecProviderImplementation()
            .CreateCodec(catalog, options);
        AsnEncodeResult encoded = codec.Encode(new AsnEncodeRequest
        {
            TypeName = typeName,
            Value = Integer(value)
        });
        Assert.That(encoded.Success, Is.True);
        return new RuleFixture
        {
            Codec = codec,
            Encoded = encoded.Data,
            Options = options,
            TypeName = typeName
        };
    }

    private static AsnReflectionCatalog IntegerCatalog()
    {
        AsnTypeDescriptor type = new AsnTypeDescriptor
        {
            StableId = "type.Number",
            Name = "Number",
            GeneratedName = "Number",
            Kind = AsnTypeKind.Integer
        };
        return new AsnReflectionCatalog
        {
            Assignments =
            [
                new AsnAssignmentDescriptor
                {
                    StableId = "assignment.Number",
                    Name = "Number",
                    QualifiedName = "Schema.Number",
                    GeneratedName = "Number",
                    Kind = AsnAssignmentKind.Type,
                    Type = type
                }
            ]
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

    private static AsnRuntimeValue Integer(int value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = value
        };
    }

    private static string Format(AsnCompilationResult result)
    {
        return string.Join(
            " | ",
            result.Diagnostics.Select(item => item.Code + ":" + item.Message));
    }

    private class RuleFixture
    {
        internal ConfiguredCodec Codec = null!;
        internal byte[] Encoded = [];
        internal AsnCodecOptions Options = null!;
        internal string TypeName = "";
    }

    private class CountingReadStream : System.IO.Stream
    {
        private readonly byte[] data;
        private int position;

        internal CountingReadStream(byte[] data)
        {
            this.data = data;
        }

        internal int ReadByteCount;
        internal int ZeroReadCount;

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
                this.ZeroReadCount = this.ZeroReadCount + 1;
                return 0;
            }

            buffer[offset] = this.data[this.position];
            this.position = this.position + 1;
            this.ReadByteCount = this.ReadByteCount + 1;
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

    private class FailingReadStream : System.IO.Stream
    {
        private readonly byte[] data;
        private readonly System.IO.IOException failure;
        private readonly int failAfter;
        private int position;

        internal FailingReadStream(
            byte[] data,
            System.IO.IOException failure,
            int failAfter)
        {
            this.data = data;
            this.failure = failure;
            this.failAfter = failAfter;
        }

        internal int ReadByteCount;

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
            if (this.ReadByteCount == this.failAfter)
            {
                throw this.failure;
            }

            buffer[offset] = this.data[this.position];
            this.position = this.position + 1;
            this.ReadByteCount = this.ReadByteCount + 1;
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

    private class FailingWriteStream : System.IO.Stream
    {
        private readonly System.IO.IOException failure;
        private readonly int failAfter;

        internal FailingWriteStream(
            System.IO.IOException failure,
            int failAfter)
        {
            this.failure = failure;
            this.failAfter = failAfter;
        }

        internal int WrittenCount;

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
            if (this.WrittenCount == this.failAfter)
            {
                throw this.failure;
            }

            if (count != 1)
            {
                throw new System.IO.IOException(
                    "The stream accepts only one-byte writes.");
            }

            this.WrittenCount = this.WrittenCount + count;
        }
    }
}
