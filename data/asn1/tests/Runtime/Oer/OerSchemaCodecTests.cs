using asn1;

namespace asn1.Tests;

public class OerSchemaCodecTests
{
    [Test]
    public void ConstrainedIntegerUsesSchemaDirectedFixedWidth()
    {
        AsnTypeDescriptor integer = Type(AsnTypeKind.Integer, "Number");
        integer.Constraints = [Bounds(0, 255)];
        OerSchemaCodec codec = Codec(integer);

        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Module.Number",
            Value = Integer(42),
            Options = Options(false)
        });
        AsnDecodeResult decoded = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.Number",
            Data = encoded.Data,
            Options = Options(false)
        });

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Data, Is.EqualTo(new byte[] { 0x2a }));
            Assert.That(encoded.WrittenCount, Is.EqualTo(1));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(42)));
            Assert.That(decoded.ConsumedCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void SequenceUsesRootPreambleAndExtensionOpenTypes()
    {
        AsnFieldDescriptor required = Field(
            "required",
            0,
            Type(AsnTypeKind.Boolean, "Required"));
        AsnFieldDescriptor optional = Field(
            "optional",
            1,
            Type(AsnTypeKind.Boolean, "Optional"));
        optional.Optional = true;
        AsnFieldDescriptor addition = Field(
            "addition",
            2,
            Type(AsnTypeKind.Boolean, "Addition"));
        addition.Extension = true;
        AsnTypeDescriptor sequence = Type(AsnTypeKind.Sequence, "Packet");
        sequence.Extensible = true;
        sequence.Fields = [required, optional, addition];
        sequence.RootFields = [required, optional];
        sequence.ExtensionFields = [addition];
        OerSchemaCodec codec = Codec(sequence);

        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Packet",
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Sequence,
                Fields =
                [
                    Present("required", Boolean(false)),
                    Present("optional", Boolean(true)),
                    Present("addition", Boolean(true))
                ]
            },
            Options = Options(false)
        });
        AsnDecodeResult decoded = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Packet",
            Data = encoded.Data,
            Options = Options(false)
        });

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data, Is.EqualTo(new byte[]
            {
                0xc0, 0x00, 0xff,
                0x02, 0x07, 0x80,
                0x01, 0xff
            }));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Value?.Fields.Single(
                field => field.Name == "optional").Value?.BooleanValue,
                Is.True);
            Assert.That(decoded.Value?.Fields.Single(
                field => field.Name == "addition").Value?.BooleanValue,
                Is.True);
        });
    }

    [Test]
    public void RequestRangeIsRespectedAndTrailingDataIsRejected()
    {
        OerSchemaCodec codec = Codec(Type(AsnTypeKind.Boolean, "Flag"));
        AsnDecodeResult ranged = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.Flag",
            Data = [0xaa, 0x01, 0xbb],
            Offset = 1,
            HasCount = true,
            Count = 1,
            Options = Options(false)
        });
        AsnDecodeResult trailing = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.Flag",
            Data = [0xff, 0x00],
            Options = Options(false)
        });

        Assert.Multiple(() =>
        {
            Assert.That(ranged.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(ranged.Value?.BooleanValue, Is.True);
            Assert.That(ranged.ConsumedCount, Is.EqualTo(1));
            Assert.That(trailing.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(trailing.ConsumedCount, Is.EqualTo(1));
            Assert.That(trailing.Diagnostics.Single().Code,
                Is.EqualTo("ASNO0103"));
        });
    }

    [Test]
    public void IncompleteValueReportsNeedMoreData()
    {
        AsnTypeDescriptor integer = Type(AsnTypeKind.Integer, "Number");
        integer.Constraints = [Bounds(0, 65535)];

        AsnDecodeResult decoded = Codec(integer).DecodeRequest(
            new AsnDecodeRequest
            {
                TypeName = "Number",
                Data = [0x01],
                Options = Options(false)
            });

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(decoded.Value, Is.Null);
        });
    }

    [Test]
    public void ValueValidationRunsBeforeEncodingAndAfterDecoding()
    {
        AsnTypeDescriptor integer = Type(AsnTypeKind.Integer, "Small");
        integer.Constraints = [Bounds(0, 10)];
        integer.Constraints[0].Kind = AsnConstraintKind.UserDefined;
        integer.Constraints[0].Constraint = new AsnConstraint
        {
            Kind = AsnConstraintKind.UserDefined,
            RequiresApplicationValidation = true
        };
        OerSchemaCodec codec = Codec(integer);
        AsnCodecOptions validated = Options(true);

        AsnEncodeResult encode = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Small",
            Value = Integer(5),
            Options = validated
        });
        AsnDecodeResult decode = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Small",
            Data = [0x05],
            Options = validated
        });

        Assert.Multiple(() =>
        {
            Assert.That(encode.Success, Is.False);
            Assert.That(encode.Diagnostics.Single().Category,
                Is.EqualTo(AsnDiagnosticCategory.Constraint));
            Assert.That(decode.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(decode.Diagnostics.Single().Category,
                Is.EqualTo(AsnDiagnosticCategory.Constraint));
            Assert.That(decode.ConsumedCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void RulesTypeAndResourceFailuresBecomePublicDiagnostics()
    {
        AsnTypeDescriptor text = Type(AsnTypeKind.Utf8String, "Text");
        OerSchemaCodec codec = Codec(text);
        AsnCodecOptions wrongRules = Options(false);
        wrongRules.EncodingRules = AsnEncodingRules.Coer;
        AsnCodecOptions bounded = Options(false);
        bounded.ResourceLimits = new AsnResourceLimits
        {
            MaximumTextCharacters = 1
        };

        AsnEncodeResult wrong = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Text",
            Value = Text("ok"),
            Options = wrongRules
        });
        AsnDecodeResult missing = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Missing",
            Data = [],
            Options = Options(false)
        });
        AsnEncodeResult resource = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Text",
            Value = Text("ok"),
            Options = bounded
        });

        Assert.Multiple(() =>
        {
            Assert.That(wrong.Diagnostics.Single().Code,
                Is.EqualTo("ASNO0101"));
            Assert.That(missing.Diagnostics.Single().Code,
                Is.EqualTo("ASNO0102"));
            Assert.That(resource.Diagnostics.Single().Category,
                Is.EqualTo(AsnDiagnosticCategory.Resource));
        });
    }

    private static OerSchemaCodec Codec(AsnTypeDescriptor type)
    {
        return new OerSchemaCodec(
            new AsnReflectionCatalog
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
            },
            AsnEncodingRules.Oer);
    }

    private static AsnCodecOptions Options(bool validate)
    {
        return new AsnCodecOptions
        {
            EncodingRules = AsnEncodingRules.Oer,
            UnknownDataPolicy = AsnUnknownDataPolicy.Reject,
            ValidateValues = validate
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
        int index,
        AsnTypeDescriptor type)
    {
        return new AsnFieldDescriptor
        {
            Name = name,
            Index = index,
            Type = type
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

    private static AsnRuntimeValue Text(string value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.CharacterString,
            TextValue = value
        };
    }
}
