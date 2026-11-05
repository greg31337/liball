using asn1;

namespace asn1.Tests;

public class PerSchemaCodecTests
{
    [Test]
    public void ConstrainedIntegerUsesAlignedBasicPerSchemaDispatch()
    {
        AsnTypeDescriptor integer = Type(AsnTypeKind.Integer, "Number");
        integer.Constraints = [Bounds(AsnTypeKind.Integer, 0, 255, false)];
        PerSchemaCodec codec = Codec(integer);

        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Module.Number",
            Value = Integer(42),
            Options = Options()
        });
        AsnDecodeResult decoded = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.Number",
            Data = encoded.Data,
            Options = Options()
        });

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Data, Is.EqualTo(new byte[] { 0x2a }));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(42)));
            Assert.That(decoded.ConsumedCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void SequenceHonorsAlignmentAndRejectsNonzeroPadding()
    {
        AsnFieldDescriptor flag = Field(
            "flag",
            Type(AsnTypeKind.Boolean, "Flag"),
            0);
        AsnFieldDescriptor payload = Field(
            "payload",
            Type(AsnTypeKind.OctetString, "Payload"),
            1);
        AsnTypeDescriptor sequence = Type(AsnTypeKind.Sequence, "Packet");
        sequence.Fields = [flag, payload];
        sequence.RootFields = [flag, payload];
        PerSchemaCodec codec = Codec(sequence);
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("flag", Boolean(true)),
                Present("payload", Octets(0xaa))
            ]
        };

        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Module.Packet",
            Value = value,
            Options = Options()
        });
        AsnDecodeResult decoded = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.Packet",
            Data = encoded.Data,
            Options = Options()
        });
        byte[] invalid = (byte[])encoded.Data.Clone();
        invalid[0] = 0xc0;
        AsnDecodeResult rejected = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.Packet",
            Data = invalid,
            Options = Options()
        });

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[] { 0x80, 0x01, 0xaa }));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Value?.Fields[1].Value?.Octets,
                Is.EqualTo(new byte[] { 0xaa }));
            Assert.That(rejected.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(rejected.Diagnostics.Single().Code,
                Is.EqualTo("ASNP0007"));
        });
    }

    [Test]
    public void FinalOctetPaddingMustBeZero()
    {
        AsnTypeDescriptor boolean = Type(AsnTypeKind.Boolean, "Flag");
        PerSchemaCodec codec = Codec(boolean);

        AsnDecodeResult result = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.Flag",
            Data = [0xff],
            Options = Options()
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo("ASNP0007"));
        });
    }

    [Test]
    public void EnumeratedRootAndExtensionIndexesRoundTrip()
    {
        AsnTypeDescriptor enumeration = Type(
            AsnTypeKind.Enumerated,
            "Status");
        enumeration.Extensible = true;
        enumeration.RootNamedValues =
        [
            Named("ready", 0),
            Named("busy", 1)
        ];
        enumeration.ExtensionNamedValues = [Named("retired", 2)];
        enumeration.NamedValues =
        [
            enumeration.RootNamedValues[0],
            enumeration.RootNamedValues[1],
            enumeration.ExtensionNamedValues[0]
        ];
        PerSchemaCodec codec = Codec(enumeration);

        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Module.Status",
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Enumerated,
                TextValue = "retired"
            },
            Options = Options()
        });
        AsnDecodeResult decoded = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.Status",
            Data = encoded.Data,
            Options = Options()
        });

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data, Is.EqualTo(new byte[] { 0x80 }));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Value?.TextValue, Is.EqualTo("retired"));
            Assert.That(decoded.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(2)));
        });
    }

    [Test]
    public void ChoicePayloadAlignmentIsRelativeToTheOuterEncoding()
    {
        AsnFieldDescriptor flag = Field(
            "flag",
            Type(AsnTypeKind.Boolean, "Flag"),
            0);
        AsnFieldDescriptor payload = Field(
            "payload",
            Type(AsnTypeKind.OctetString, "Payload"),
            1);
        AsnTypeDescriptor choice = Type(AsnTypeKind.Choice, "Selection");
        choice.Fields = [flag, payload];
        choice.RootFields = [flag, payload];
        PerSchemaCodec codec = Codec(choice);

        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Module.Selection",
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Choice,
                Fields = [Present("payload", Octets(0xaa))]
            },
            Options = Options()
        });
        AsnDecodeResult decoded = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.Selection",
            Data = encoded.Data,
            Options = Options()
        });

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[] { 0x80, 0x01, 0xaa }));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Value?.Fields.Single().Name,
                Is.EqualTo("payload"));
        });
    }

    [Test]
    public void ExtensionAdditionGroupUsesOneOpenType()
    {
        AsnFieldDescriptor root = Field(
            "root",
            Type(AsnTypeKind.Boolean, "Root"),
            0);
        AsnFieldDescriptor first = Field(
            "first",
            Type(AsnTypeKind.Boolean, "First"),
            1);
        first.Extension = true;
        first.ExtensionGroupIndex = 0;
        first.Optional = true;
        AsnFieldDescriptor second = Field(
            "second",
            Type(AsnTypeKind.Boolean, "Second"),
            2);
        second.Extension = true;
        second.ExtensionGroupIndex = 0;
        second.Optional = true;
        AsnTypeDescriptor sequence = Type(AsnTypeKind.Sequence, "Grouped");
        sequence.Extensible = true;
        sequence.Fields = [root, first, second];
        sequence.RootFields = [root];
        sequence.ExtensionFields = [first, second];
        PerSchemaCodec codec = Codec(sequence);

        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Module.Grouped",
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Sequence,
                Fields =
                [
                    Present("root", Boolean(false)),
                    Present("second", Boolean(true))
                ]
            },
            Options = Options()
        });
        AsnDecodeResult decoded = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.Grouped",
            Data = encoded.Data,
            Options = Options()
        });

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Value?.Fields.Count(field => field.Present),
                Is.EqualTo(2));
            Assert.That(decoded.Value?.Fields.Single(
                field => field.Name == "second").Value?.BooleanValue,
                Is.True);
        });
    }

    [Test]
    public void CollectionOfStreamsFragmentedElementCounts()
    {
        AsnTypeDescriptor collection = Type(
            AsnTypeKind.SequenceOf,
            "Nulls");
        collection.ElementType = Type(AsnTypeKind.Null, "Item");
        PerSchemaCodec codec = Codec(collection);
        AsnRuntimeValue[] items = new AsnRuntimeValue[16385];
        int index = 0;
        while (index < items.Length)
        {
            items[index] = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Null
            };
            index = index + 1;
        }

        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Module.Nulls",
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.SequenceOf,
                Items = items
            },
            Options = Options()
        });
        AsnDecodeResult decoded = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.Nulls",
            Data = encoded.Data,
            Options = Options()
        });

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data, Is.EqualTo(new byte[] { 0xc1, 0x01 }));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Value?.Items, Has.Length.EqualTo(16385));
        });
    }

    [Test]
    public void InstanceOfPreservesItsAlignedOpenPayload()
    {
        AsnTypeDescriptor instance = Type(
            AsnTypeKind.InstanceOf,
            "IdentifiedValue");
        PerSchemaCodec codec = Codec(instance);
        AsnCodecOptions options = Options();
        options.UnknownDataPolicy = AsnUnknownDataPolicy.Preserve;
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.InstanceOf,
            Fields =
            [
                Present("type-id", ObjectIdentifier(1, 2, 840, 113549)),
                Present("value", new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.OpenType,
                    OpenTypeValue = new AsnOpenTypeValue
                    {
                        Unresolved = true,
                        HasEncodingRules = true,
                        EncodingRules = AsnEncodingRules.Aper,
                        HasOriginalData = true,
                        OriginalData = [0xa0]
                    }
                })
            ]
        };

        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Module.IdentifiedValue",
            Value = value,
            Options = options
        });
        AsnDecodeResult decoded = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.IdentifiedValue",
            Data = encoded.Data,
            Options = options
        });
        AsnOpenTypeValue? open = decoded.Value?.Fields.Single(
            field => field.Name == "value").Value?.OpenTypeValue;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(open?.OriginalData, Is.EqualTo(new byte[] { 0xa0 }));
            Assert.That(open?.EncodingRules, Is.EqualTo(AsnEncodingRules.Aper));
        });
    }

    private static PerSchemaCodec Codec(AsnTypeDescriptor type)
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
        }, AsnEncodingRules.Aper);
    }

    private static AsnCodecOptions Options()
    {
        return new AsnCodecOptions
        {
            EncodingRules = AsnEncodingRules.Aper,
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
        int upper,
        bool extensible)
    {
        return new AsnConstraintDescriptor
        {
            DomainKind = kind,
            Extensible = extensible,
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

    private static AsnValueDescriptor Named(string name, int value)
    {
        return new AsnValueDescriptor
        {
            Name = name,
            Value = new AsnValue
            {
                Kind = AsnValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = value
            }
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

    private static AsnRuntimeValue Octets(byte value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OctetString,
            Octets = [value]
        };
    }

    private static AsnRuntimeValue ObjectIdentifier(params int[] arcs)
    {
        System.Numerics.BigInteger[] values =
            new System.Numerics.BigInteger[arcs.Length];
        int index = 0;
        while (index < arcs.Length)
        {
            values[index] = new System.Numerics.BigInteger(arcs[index]);
            index = index + 1;
        }

        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.ObjectIdentifier,
            ObjectIdentifierValue = new AsnObjectIdentifierValue
            {
                Arcs = values
            }
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
