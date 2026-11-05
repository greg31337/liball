using asn1;

namespace asn1.Tests;

public class XerStreamingSchemaCodecTests
{
    [Test]
    public void OneByteChunksResumeAcrossEveryExtendedXerBoundary()
    {
        AsnTypeDescriptor envelope = EnvelopeType();
        AsnReflectionCatalog catalog = Catalog(envelope);
        AsnCodecOptions options = Options(AsnEncodingRules.ExtendedXer);
        AsnEncodeResult encoded = new XerSchemaCodec(
            catalog,
            AsnEncodingRules.ExtendedXer).EncodeRequest(
                new AsnEncodeRequest
                {
                    TypeName = "Module.Envelope",
                    Value = EnvelopeValue(),
                    Options = options
                });
        XerIncrementalSchemaDecoder decoder =
            new XerIncrementalSchemaDecoder(
                catalog,
                "Module.Envelope",
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
                    Is.EqualTo(AsnDecodeStatus.NeedMoreData),
                    "byte " + index + ": "
                        + (result.Diagnostics.Length == 0
                            ? "no diagnostic"
                            : result.Diagnostics[0].Code + " "
                                + result.Diagnostics[0].Message)
                        + " in " + Text(encoded.Data));
                Assert.That(result.ConsumedCount, Is.EqualTo(1));
            }

            index = index + 1;
        }

        AsnRuntimeValue? value = result.Value;
        string xml = Text(encoded.Data);
        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(xml, Does.Contain("xmlns:p=\"urn:envelope\""));
            Assert.That(xml, Does.Contain("xmlns:m=\"urn:meta\""));
            Assert.That(xml, Does.Contain("m:code=\"A&amp;B\""));
            Assert.That(xml, Does.Contain("Grüße &lt;&amp; 😀"));
            Assert.That(xml, Does.Contain("<numbers>1 20 -3</numbers>"));
            Assert.That(xml, Does.Contain(
                "<w:item xmlns:w=\"urn:wild\" label=\"A&amp;B\">"));
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(result.ConsumedCount, Is.EqualTo(1));
            Assert.That(value?.Fields.Single(field => field.Name == "code")
                .Value?.TextValue, Is.EqualTo("A&B"));
            Assert.That(value?.Fields.Single(field => field.Name == "text")
                .Value?.TextValue, Is.EqualTo("Grüße <& 😀"));
            Assert.That(value?.Fields.Single(field => field.Name == "numbers")
                .Value?.Items.Select(item => item.IntegerValue),
                Is.EqualTo(new System.Numerics.BigInteger[] { 1, 20, -3 }));
            Assert.That(value?.Fields.Single(field => field.Name == "wildcard")
                .Value?.TextValue, Does.Contain("<w:child>é</w:child>"));
            Assert.That(value?.Fields.Single(field => field.Name == "nested")
                .Value?.Fields[0].Value?.BooleanValue, Is.True);
        });
    }

    [Test]
    public void CompleteTerminalAndResetStatesAreStable()
    {
        AsnTypeDescriptor text = new AsnTypeDescriptor
        {
            StableId = "type.Text",
            Name = "Text",
            Kind = AsnTypeKind.Utf8String
        };
        AsnCodecOptions options = Options(AsnEncodingRules.BasicXer);
        XerIncrementalSchemaDecoder decoder =
            new XerIncrementalSchemaDecoder(
                Catalog(text),
                "Text",
                options);

        AsnIncrementalResult waiting = decoder.Consume(
            new AsnIncrementalInput { Data = Bytes("<Text>A&amp") });
        AsnIncrementalResult truncated = decoder.Complete();
        AsnIncrementalResult terminal = decoder.Consume(
            new AsnIncrementalInput
            {
                Data = Bytes(";</Text>"),
                IsFinalBlock = true
            });
        decoder.Reset();
        AsnIncrementalResult assembled = decoder.Consume(
            new AsnIncrementalInput { Data = Bytes("<Text>ok") });
        AsnIncrementalResult completed = decoder.Consume(
            new AsnIncrementalInput { Data = Bytes("</Text>") });
        AsnIncrementalResult done = decoder.Complete();
        AsnIncrementalResult repeated = decoder.Complete();

        Assert.Multiple(() =>
        {
            Assert.That(waiting.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(truncated.Diagnostics[0].Code,
                Is.EqualTo("ASNXER0022"));
            Assert.That(terminal.Diagnostics[0].Code,
                Is.EqualTo("ASNXER0404"));
            Assert.That(assembled.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(completed.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(done.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(done.ConsumedCount, Is.Zero);
            Assert.That(done.Value?.TextValue, Is.EqualTo("ok"));
            Assert.That(repeated.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(repeated.Value?.TextValue, Is.EqualTo("ok"));
        });
    }

    [Test]
    public void IncrementalCanonicalModeValidatesAfterAssembly()
    {
        AsnTypeDescriptor number = new AsnTypeDescriptor
        {
            StableId = "type.Number",
            Name = "Number",
            Kind = AsnTypeKind.Integer
        };
        AsnReflectionCatalog catalog = Catalog(number);
        AsnCodecOptions options = Options(AsnEncodingRules.CanonicalXer);
        XerIncrementalSchemaDecoder accepted =
            new XerIncrementalSchemaDecoder(catalog, "Number", options);
        XerIncrementalSchemaDecoder rejected =
            new XerIncrementalSchemaDecoder(catalog, "Number", options);

        AsnIncrementalResult valid = accepted.Consume(
            new AsnIncrementalInput
            {
                Data = Bytes("<Number>7</Number>"),
                IsFinalBlock = true
            });
        AsnIncrementalResult invalid = rejected.Consume(
            new AsnIncrementalInput
            {
                Data = Bytes("<Number> 7 </Number>"),
                IsFinalBlock = true
            });

        Assert.Multiple(() =>
        {
            Assert.That(valid.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(valid.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(7)));
            Assert.That(invalid.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(invalid.Diagnostics[0].Code,
                Is.EqualTo("ASNXER0200"));
        });
    }

    [TestCase(AsnEncodingRules.BasicXer)]
    [TestCase(AsnEncodingRules.CanonicalXer)]
    [TestCase(AsnEncodingRules.ExtendedXer)]
    public void StreamEncodingMatchesBufferedOutput(
        AsnEncodingRules rules)
    {
        AsnTypeDescriptor envelope = EnvelopeType();
        AsnReflectionCatalog catalog = Catalog(envelope);
        AsnCodecOptions options = Options(rules);
        AsnEncodeRequest request = new AsnEncodeRequest
        {
            TypeName = "Module.Envelope",
            Value = EnvelopeValue(),
            Options = options
        };
        XerSchemaCodec codec = new XerSchemaCodec(catalog, rules);
        AsnEncodeResult buffered = codec.EncodeRequest(request);
        WriteOnlyStream output = new WriteOnlyStream();

        AsnEncodeResult streamed = codec.EncodeStream(request, output);

        Assert.Multiple(() =>
        {
            Assert.That(buffered.Success, Is.True,
                buffered.Diagnostics.Length == 0
                    ? "Buffered encoding failed."
                    : buffered.Diagnostics[0].Message);
            Assert.That(streamed.Success, Is.True,
                streamed.Diagnostics.Length == 0
                    ? "Stream encoding failed."
                    : streamed.Diagnostics[0].Message);
            Assert.That(streamed.Data, Is.Empty);
            Assert.That(streamed.WrittenCount,
                Is.EqualTo(buffered.WrittenCount));
            Assert.That(streamed.StreamProgress?.WrittenCount,
                Is.EqualTo(buffered.WrittenCount));
            Assert.That(streamed.StreamProgress?.ExpectedCount,
                Is.EqualTo(buffered.WrittenCount));
            Assert.That(streamed.StreamProgress?.HasExpectedCount, Is.True);
            Assert.That(streamed.StreamProgress?.Complete, Is.True);
            Assert.That(output.MaximumWriteCount, Is.EqualTo(1));
            Assert.That(output.ToArray(), Is.EqualTo(buffered.Data));
        });
    }

    private static AsnTypeDescriptor EnvelopeType()
    {
        AsnTypeDescriptor code = Instructions(
            Type(AsnTypeKind.Utf8String),
            Instruction(AsnInstructionKind.Attribute, []),
            Instruction(AsnInstructionKind.Name, ["AS", "\"code\""]),
            Instruction(
                AsnInstructionKind.Namespace,
                ["AS", "\"urn:meta\"", "PREFIX", "\"m\""]));
        AsnTypeDescriptor numbers = Instructions(
            new AsnTypeDescriptor
            {
                Kind = AsnTypeKind.SequenceOf,
                ElementType = Type(AsnTypeKind.Integer)
            },
            Instruction(AsnInstructionKind.List, []));
        AsnTypeDescriptor wildcard = Instructions(
            Type(AsnTypeKind.Utf8String),
            Instruction(
                AsnInstructionKind.AnyElement,
                ["FROM", "\"urn:wild\""]));
        AsnTypeDescriptor nested = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Fields =
            [
                new AsnFieldDescriptor
                {
                    Name = "flag",
                    Type = Type(AsnTypeKind.Boolean)
                },
                new AsnFieldDescriptor
                {
                    Name = "note",
                    Type = Type(AsnTypeKind.Utf8String)
                }
            ]
        };
        return Instructions(
            new AsnTypeDescriptor
            {
                StableId = "type.Envelope",
                Name = "Envelope",
                Kind = AsnTypeKind.Sequence,
                Fields =
                [
                    Field("code", code),
                    Field("text", Type(AsnTypeKind.Utf8String)),
                    Field("numbers", numbers),
                    Field("wildcard", wildcard),
                    Field("nested", nested)
                ]
            },
            Instruction(AsnInstructionKind.Name, ["AS", "LOWERCASED"]),
            Instruction(
                AsnInstructionKind.Namespace,
                ["AS", "\"urn:envelope\"", "PREFIX", "\"p\""]));
    }

    private static AsnRuntimeValue EnvelopeValue()
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("code", TextValue("A&B")),
                Present("text", TextValue("Grüße <& 😀")),
                Present("numbers", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.SequenceOf,
                    Items = [Integer(1), Integer(20), Integer(-3)]
                }),
                Present("wildcard", TextValue(
                    "<w:item xmlns:w=\"urn:wild\" label=\"A&amp;B\">"
                        + "<w:child>é</w:child></w:item>")),
                Present("nested", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.Sequence,
                    Fields =
                    [
                        Present("flag", Boolean(true)),
                        Present("note", TextValue("done"))
                    ]
                })
            ]
        };
    }

    private static AsnTypeDescriptor Instructions(
        AsnTypeDescriptor type,
        params AsnEncodingInstruction[] instructions)
    {
        type.Encodings =
        [
            new AsnEncodingDescriptor
            {
                EncodingRules = AsnEncodingRules.ExtendedXer,
                EncodingReference = "XER",
                Instructions = instructions
            }
        ];
        return type;
    }

    private static AsnEncodingInstruction Instruction(
        AsnInstructionKind kind,
        string[] arguments)
    {
        return new AsnEncodingInstruction
        {
            Kind = kind,
            Name = kind.ToString().ToUpperInvariant(),
            Arguments = arguments
        };
    }

    private static AsnTypeDescriptor Type(AsnTypeKind kind)
    {
        return new AsnTypeDescriptor { Kind = kind };
    }

    private static AsnFieldDescriptor Field(
        string name,
        AsnTypeDescriptor type)
    {
        return new AsnFieldDescriptor { Name = name, Type = type };
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

    private static AsnRuntimeValue TextValue(string value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.CharacterString,
            TextValue = value
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

    private static AsnRuntimeValue Boolean(bool value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Boolean,
            HasBooleanValue = true,
            BooleanValue = value
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
                    QualifiedName = "Module." + type.Name,
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
            UnknownDataPolicy = AsnUnknownDataPolicy.Preserve,
            ValidateValues = false,
            ResourceLimits = new AsnResourceLimits
            {
                MaximumNestingDepth = 64,
                MaximumCollectionElements = 4096,
                MaximumTextCharacters = 1048576,
                MaximumOpenTypeBytes = 1048576,
                MaximumTotalAllocationBytes = 16777216
            }
        };
    }

    private static byte[] Bytes(string value)
    {
        return System.Text.Encoding.UTF8.GetBytes(value);
    }

    private static string Text(byte[] value)
    {
        return System.Text.Encoding.UTF8.GetString(value);
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
