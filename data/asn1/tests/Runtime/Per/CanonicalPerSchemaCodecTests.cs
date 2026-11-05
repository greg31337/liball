using asn1;

namespace asn1.Tests;

public class CanonicalPerSchemaCodecTests
{
    [TestCase(AsnEncodingRules.CanonicalAlignedPer)]
    [TestCase(AsnEncodingRules.CanonicalUnalignedPer)]
    public void SetOfUsesAscendingCompleteEncodingOrder(
        AsnEncodingRules rules)
    {
        AsnTypeDescriptor integer = Type(AsnTypeKind.Integer, "Item");
        integer.Constraints = [Bounds(AsnTypeKind.Integer, 0, 3)];
        AsnTypeDescriptor set = Type(AsnTypeKind.SetOf, "Numbers");
        set.ElementType = integer;
        set.Constraints = [Bounds(AsnTypeKind.SetOf, 3, 3)];
        PerSchemaCodec codec = Codec(set, rules);

        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Module.Numbers",
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.SetOf,
                Items = [Integer(2), Integer(0), Integer(1)]
            },
            Options = Options(rules)
        });
        AsnDecodeResult decoded = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.Numbers",
            Data = encoded.Data,
            Options = Options(rules)
        });

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Data, Is.EqualTo(new byte[] { 0x18 }));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Value?.Items.Select(item => item.IntegerValue),
                Is.EqualTo(new System.Numerics.BigInteger[] { 0, 1, 2 }));
        });
    }

    [Test]
    public void DecoderRejectsNoncanonicalSetOfOrder()
    {
        AsnTypeDescriptor integer = Type(AsnTypeKind.Integer, "Item");
        integer.Constraints = [Bounds(AsnTypeKind.Integer, 0, 3)];
        AsnTypeDescriptor set = Type(AsnTypeKind.SetOf, "UnsortedNumbers");
        set.ElementType = integer;
        set.Constraints = [Bounds(AsnTypeKind.SetOf, 3, 3)];
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.SetOf,
            Items = [Integer(2), Integer(0), Integer(1)]
        };
        AsnEncodeResult basic = Codec(
            set,
            AsnEncodingRules.Uper).EncodeRequest(new AsnEncodeRequest
            {
                TypeName = "Module.UnsortedNumbers",
                Value = value,
                Options = Options(AsnEncodingRules.Uper)
            });

        AsnDecodeResult result = Codec(
            set,
            AsnEncodingRules.CanonicalUnalignedPer).DecodeRequest(
                new AsnDecodeRequest
                {
                    TypeName = "Module.UnsortedNumbers",
                    Data = basic.Data,
                    Options = Options(
                        AsnEncodingRules.CanonicalUnalignedPer)
                });

        Assert.Multiple(() =>
        {
            Assert.That(basic.Data, Is.EqualTo(new byte[] { 0x84 }));
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo("ASNP0070"));
        });
    }

    [Test]
    public void SetRootComponentsUseCanonicalTagOrder()
    {
        AsnFieldDescriptor later = Field(
            "later",
            Type(AsnTypeKind.Boolean, "Later"),
            0);
        later.Tags = [Tag(1)];
        AsnFieldDescriptor earlier = Field(
            "earlier",
            Type(AsnTypeKind.Boolean, "Earlier"),
            1);
        earlier.Tags = [Tag(0)];
        AsnTypeDescriptor set = Type(AsnTypeKind.Set, "TaggedSet");
        set.Fields = [later, earlier];
        set.RootFields = [later, earlier];
        PerSchemaCodec codec = Codec(
            set,
            AsnEncodingRules.CanonicalUnalignedPer);

        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Module.TaggedSet",
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Set,
                Fields =
                [
                    Present("later", Boolean(false)),
                    Present("earlier", Boolean(true))
                ]
            },
            Options = Options(AsnEncodingRules.CanonicalUnalignedPer)
        });
        AsnDecodeResult decoded = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.TaggedSet",
            Data = encoded.Data,
            Options = Options(AsnEncodingRules.CanonicalUnalignedPer)
        });

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data, Is.EqualTo(new byte[] { 0x80 }));
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.Value?.Fields.Select(field => field.Name),
                Is.EqualTo(new[] { "earlier", "later" }));
        });
    }

    [Test]
    public void DefaultComponentsAreOmittedAndPresentDefaultsAreRejected()
    {
        AsnFieldDescriptor enabled = Field(
            "enabled",
            Type(AsnTypeKind.Boolean, "Enabled"),
            0);
        enabled.HasDefaultValue = true;
        enabled.DefaultValue = new AsnValueDescriptor
        {
            Value = new AsnValue
            {
                Kind = AsnValueKind.Boolean,
                TextValue = "TRUE"
            }
        };
        AsnTypeDescriptor settings = Type(
            AsnTypeKind.Sequence,
            "Settings");
        settings.Fields = [enabled];
        settings.RootFields = [enabled];
        PerSchemaCodec codec = Codec(
            settings,
            AsnEncodingRules.CanonicalAlignedPer);

        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Module.Settings",
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Sequence,
                Fields = [Present("enabled", Boolean(true))]
            },
            Options = Options(AsnEncodingRules.CanonicalAlignedPer)
        });
        AsnDecodeResult rejected = codec.DecodeRequest(new AsnDecodeRequest
        {
            TypeName = "Module.Settings",
            Data = [0xc0],
            Options = Options(AsnEncodingRules.CanonicalAlignedPer)
        });

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data, Is.EqualTo(new byte[] { 0x00 }));
            Assert.That(rejected.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(rejected.Diagnostics.Single().Code,
                Is.EqualTo("ASNP0093"));
        });
    }

    [Test]
    public void ExtensionBitmapEndsAtTheLastPresentAddition()
    {
        AsnFieldDescriptor first = Field(
            "first",
            Type(AsnTypeKind.Boolean, "First"),
            0);
        first.Extension = true;
        first.Optional = true;
        AsnFieldDescriptor second = Field(
            "second",
            Type(AsnTypeKind.Boolean, "Second"),
            1);
        second.Extension = true;
        second.Optional = true;
        AsnTypeDescriptor sequence = Type(
            AsnTypeKind.Sequence,
            "Extensions");
        sequence.Extensible = true;
        sequence.Fields = [first, second];
        sequence.ExtensionFields = [first, second];
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields = [Present("first", Boolean(true))]
        };
        AsnEncodeResult canonical = Codec(
            sequence,
            AsnEncodingRules.CanonicalAlignedPer).EncodeRequest(
                new AsnEncodeRequest
                {
                    TypeName = "Module.Extensions",
                    Value = value,
                    Options = Options(
                        AsnEncodingRules.CanonicalAlignedPer)
                });
        AsnEncodeResult basic = Codec(
            sequence,
            AsnEncodingRules.Aper).EncodeRequest(new AsnEncodeRequest
            {
                TypeName = "Module.Extensions",
                Value = value,
                Options = Options(AsnEncodingRules.Aper)
            });
        AsnDecodeResult rejected = Codec(
            sequence,
            AsnEncodingRules.CanonicalAlignedPer).DecodeRequest(
                new AsnDecodeRequest
                {
                    TypeName = "Module.Extensions",
                    Data = basic.Data,
                    Options = Options(
                        AsnEncodingRules.CanonicalAlignedPer)
                });

        Assert.Multiple(() =>
        {
            Assert.That(canonical.Data,
                Is.EqualTo(new byte[] { 0x80, 0x80, 0x01, 0x80 }));
            Assert.That(basic.Data,
                Is.EqualTo(new byte[] { 0x81, 0x80, 0x01, 0x80 }));
            Assert.That(rejected.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(rejected.Diagnostics.Single().Code,
                Is.EqualTo("ASNP0094"));
        });
    }

    [Test]
    public void DecoderRejectsANonminimalLengthDeterminant()
    {
        AsnTypeDescriptor octets = Type(
            AsnTypeKind.OctetString,
            "Octets");

        AsnDecodeResult result = Codec(
            octets,
            AsnEncodingRules.CanonicalAlignedPer).DecodeRequest(
                new AsnDecodeRequest
                {
                    TypeName = "Module.Octets",
                    Data = [0x80, 0x01, 0xaa],
                    Options = Options(
                        AsnEncodingRules.CanonicalAlignedPer)
                });

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo("ASNP0036"));
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

    private static AsnTagDescriptor Tag(int number)
    {
        return new AsnTagDescriptor
        {
            Class = AsnTagClass.ContextSpecific,
            Number = number
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
