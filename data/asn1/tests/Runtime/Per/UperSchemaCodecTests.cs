using asn1;

namespace asn1.Tests;

public class UperSchemaCodecTests
{
    [Test]
    public void StructuredFieldsRemainBitContiguous()
    {
        AsnTypeDescriptor integer = Type(AsnTypeKind.Integer, "Number");
        integer.Constraints = [Bounds(0, 65535)];
        AsnFieldDescriptor flag = Field(
            "flag",
            Type(AsnTypeKind.Boolean, "Flag"),
            0);
        AsnFieldDescriptor number = Field("number", integer, 1);
        AsnTypeDescriptor packet = Type(AsnTypeKind.Sequence, "Packet");
        packet.Fields = [flag, number];
        packet.RootFields = [flag, number];
        PerSchemaCodec codec = Codec(packet, AsnEncodingRules.Uper);

        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Module.Packet",
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Sequence,
                Fields =
                [
                    Present("flag", Boolean(true)),
                    Present("number", Integer(0x1234))
                ]
            },
            Options = Options(AsnEncodingRules.Uper)
        });
        AsnDecodeResult decoded = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.Packet",
            Data = encoded.Data,
            Options = Options(AsnEncodingRules.Uper)
        });

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[] { 0x89, 0x1a, 0x00 }));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Value?.Fields[1].Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(0x1234)));
        });
    }

    [Test]
    public void LengthAndPayloadFollowThePrecedingBitWithoutAlignment()
    {
        AsnFieldDescriptor flag = Field(
            "flag",
            Type(AsnTypeKind.Boolean, "Flag"),
            0);
        AsnFieldDescriptor payload = Field(
            "payload",
            Type(AsnTypeKind.OctetString, "Payload"),
            1);
        AsnTypeDescriptor packet = Type(AsnTypeKind.Sequence, "PayloadPacket");
        packet.Fields = [flag, payload];
        packet.RootFields = [flag, payload];
        PerSchemaCodec codec = Codec(packet, AsnEncodingRules.Uper);

        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Module.PayloadPacket",
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Sequence,
                Fields =
                [
                    Present("flag", Boolean(true)),
                    Present("payload", Octets(0xaa))
                ]
            },
            Options = Options(AsnEncodingRules.Uper)
        });
        AsnDecodeResult decoded = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.PayloadPacket",
            Data = encoded.Data,
            Options = Options(AsnEncodingRules.Uper)
        });

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[] { 0x80, 0xd5, 0x00 }));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Value?.Fields[1].Value?.Octets,
                Is.EqualTo(new byte[] { 0xaa }));
        });
    }

    [Test]
    public void FinalTransportPaddingMustStillBeZero()
    {
        AsnTypeDescriptor integer = Type(AsnTypeKind.Integer, "Number");
        integer.Constraints = [Bounds(0, 65535)];
        AsnFieldDescriptor flag = Field(
            "flag",
            Type(AsnTypeKind.Boolean, "Flag"),
            0);
        AsnFieldDescriptor number = Field("number", integer, 1);
        AsnTypeDescriptor packet = Type(AsnTypeKind.Sequence, "Padded");
        packet.Fields = [flag, number];
        packet.RootFields = [flag, number];
        PerSchemaCodec codec = Codec(packet, AsnEncodingRules.Uper);

        AsnDecodeResult result = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.Padded",
            Data = [0x89, 0x1a, 0x01],
            Options = Options(AsnEncodingRules.Uper)
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo("ASNP0007"));
        });
    }

    [TestCase(AsnEncodingRules.Uper)]
    [TestCase(AsnEncodingRules.CanonicalUnalignedPer)]
    public void UnalignedPerInvokesRegisteredPerInstructionHooks(
        AsnEncodingRules rules)
    {
        AsnTypeDescriptor boolean = Type(AsnTypeKind.Boolean, "Directed");
        boolean.Encodings = [Encoding("CUSTOM")];
        TestInstructionHandler handler = new TestInstructionHandler("CUSTOM");
        AsnCodecOptions options = Options(rules);
        options.PerInstructionRegistry = handler;
        PerSchemaCodec codec = Codec(boolean, rules);

        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Module.Directed",
            Value = Boolean(true),
            Options = options
        });
        AsnDecodeResult decoded = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.Directed",
            Data = encoded.Data,
            Options = options
        });

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(handler.ValidationCount, Is.EqualTo(2));
            Assert.That(handler.ApplicationCount, Is.EqualTo(2));
            Assert.That(handler.LastRules, Is.EqualTo(rules));
        });
    }

    [TestCase(AsnEncodingRules.Aper)]
    [TestCase(AsnEncodingRules.CanonicalAlignedPer)]
    public void AlignedPerSilentlyIgnoresUnknownPerInstructions(
        AsnEncodingRules rules)
    {
        AsnTypeDescriptor boolean = Type(AsnTypeKind.Boolean, "Aligned");
        boolean.Encodings = [Encoding("UNKNOWN")];
        AsnCodecOptions options = Options(rules);

        AsnEncodeResult encoded = Codec(
            boolean,
            rules).EncodeRequest(new AsnEncodeRequest
            {
                TypeName = "Module.Aligned",
                Value = Boolean(true),
                Options = options
            });
        AsnDecodeResult decoded = Codec(
            boolean,
            rules).DecodeRequest(new AsnDecodeRequest
            {
                TypeName = "Module.Aligned",
                Data = encoded.Data,
                Options = options
            });

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Diagnostics, Is.Empty);
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Diagnostics, Is.Empty);
            Assert.That(decoded.Value?.BooleanValue, Is.True);
        });
    }

    [TestCase(AsnEncodingRules.Uper)]
    [TestCase(AsnEncodingRules.CanonicalUnalignedPer)]
    public void UnalignedPerRejectsAnUnknownInstruction(
        AsnEncodingRules rules)
    {
        AsnTypeDescriptor boolean = Type(AsnTypeKind.Boolean, "UnknownHook");
        boolean.Encodings = [Encoding("UNKNOWN")];

        AsnEncodeResult result = Codec(
            boolean,
            rules).EncodeRequest(new AsnEncodeRequest
            {
                TypeName = "Module.UnknownHook",
                Value = Boolean(true),
                Options = Options(rules)
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo("ASNP0091"));
            Assert.That(result.Diagnostics.Single().Message,
                Does.Contain("not supported"));
        });
    }

    [Test]
    public void ComponentOccurrenceInstructionReplacesReferencedTypeInstruction()
    {
        AsnTypeDescriptor integer = Type(AsnTypeKind.Integer, "Original");
        integer.Encodings = [Encoding("ORIGINAL")];
        AsnFieldDescriptor field = Field("value", integer, 0);
        field.StableId = "field.value";
        field.Encodings = [Encoding("CUSTOM")];
        AsnTypeDescriptor packet = Type(AsnTypeKind.Sequence, "Occurrence");
        packet.Fields = [field];
        packet.RootFields = [field];
        TestInstructionHandler handler = new TestInstructionHandler("CUSTOM");
        AsnCodecOptions options = Options(AsnEncodingRules.Uper);
        options.PerInstructionRegistry = handler;
        PerSchemaCodec codec = Codec(packet, AsnEncodingRules.Uper);

        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Module.Occurrence",
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Sequence,
                Fields = [Present("value", Integer(7))]
            },
            Options = options
        });
        AsnDecodeResult decoded = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.Occurrence",
            Data = encoded.Data,
            Options = options
        });

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(handler.ValidationCount, Is.EqualTo(2));
            Assert.That(handler.ApplicationCount, Is.EqualTo(2));
        });
    }

    private static PerSchemaCodec Codec(
        AsnTypeDescriptor type,
        AsnEncodingRules rules)
    {
        return new PerSchemaCodec(new AsnReflectionCatalog
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
        }, rules);
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
            Type = type,
            Index = index
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

    private static AsnEncodingDescriptor Encoding(string name)
    {
        AsnEncodingInstruction instruction = new AsnEncodingInstruction
        {
            Name = name,
            Kind = AsnInstructionKind.PerApplication
        };
        return new AsnEncodingDescriptor
        {
            StableId = "encoding." + name,
            EncodingRules = AsnEncodingRules.Uper,
            Instructions = [instruction],
            Plan = new AsnEncodingPlan
            {
                StableId = "plan." + name,
                EncodingRules = AsnEncodingRules.Uper,
                Instructions = [instruction]
            }
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

    private static AsnRuntimeValue Octets(byte value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OctetString,
            Octets = [value]
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

    private class TestInstructionHandler :
        IPerInstructionRegistry,
        IPerInstructionHandler
    {
        public string Keyword;
        public int ValidationCount;
        public int ApplicationCount;
        public AsnEncodingRules LastRules;

        public TestInstructionHandler(string keyword)
        {
            this.Keyword = keyword;
        }

        public void RegisterHandler(
            AsnPerInstructionRegistration registration,
            IPerInstructionHandler handler)
        {
            throw new NotSupportedException();
        }

        public IPerInstructionHandler? ResolveHandler(string keyword)
        {
            return keyword == this.Keyword ? this : null;
        }

        public AsnPerInstructionRegistration[] GetRegistrations()
        {
            return
            [
                new AsnPerInstructionRegistration
                {
                    Keyword = this.Keyword,
                    HandlerId = "test.handler",
                    SupportsUnaligned = true
                }
            ];
        }

        public AsnDiagnostic[] ValidateInstruction(
            AsnEncodingInstruction instruction,
            AsnType type)
        {
            this.ValidationCount = this.ValidationCount + 1;
            return [];
        }

        public AsnEncodingPlanStep[] ApplyInstruction(
            AsnEncodingInstruction instruction,
            AsnType type,
            AsnEncodingPlan plan)
        {
            this.ApplicationCount = this.ApplicationCount + 1;
            this.LastRules = plan.EncodingRules;
            return [];
        }
    }
}
