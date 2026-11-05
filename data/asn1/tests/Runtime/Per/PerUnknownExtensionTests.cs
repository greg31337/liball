using asn1;

namespace asn1.Tests;

public class PerUnknownExtensionTests
{
    [TestCase(AsnEncodingRules.Aper)]
    [TestCase(AsnEncodingRules.Uper)]
    [TestCase(AsnEncodingRules.CanonicalAlignedPer)]
    [TestCase(AsnEncodingRules.CanonicalUnalignedPer)]
    public void UnknownExtensionGroupRoundTripsExactOpenTypeOctets(
        AsnEncodingRules rules)
    {
        AsnTypeDescriptor newer = NewerType();
        AsnTypeDescriptor older = OlderType();
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("root", Boolean(true)),
                Present("second", Boolean(true))
            ]
        };
        AsnEncodeResult encoded = Codec(newer, rules).EncodeRequest(
            new AsnEncodeRequest
            {
                TypeName = "Module.Message",
                Value = value,
                Options = Options(rules, AsnUnknownDataPolicy.Preserve)
            });
        AsnDecodeResult decoded = Codec(older, rules).DecodeRequest(
            new AsnDecodeRequest
            {
                TypeName = "Module.Message",
                Data = encoded.Data,
                Options = Options(rules, AsnUnknownDataPolicy.Preserve)
            });
        AsnUnknownExtension unknown = decoded.Value?.UnknownExtensions.Single()
            ?? throw new InvalidOperationException();
        AsnEncodeResult reencoded = Codec(older, rules).EncodeRequest(
            new AsnEncodeRequest
            {
                TypeName = "Module.Message",
                Value = decoded.Value,
                Options = Options(rules, AsnUnknownDataPolicy.Preserve)
            });

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(unknown.Index, Is.EqualTo(1));
            Assert.That(unknown.Present, Is.True);
            Assert.That(unknown.HasEncodedIndex, Is.True);
            Assert.That(unknown.EncodedIndex, Is.EqualTo(1));
            Assert.That(unknown.HasBinaryData, Is.True);
            Assert.That(unknown.BinaryData, Is.EqualTo(new byte[] { 0x60 }));
            Assert.That(unknown.EncodingRules, Is.EqualTo(rules));
            Assert.That(unknown.HasEncodedBitLocation, Is.True);
            Assert.That(unknown.EncodedBitOffset, Is.GreaterThan(0));
            Assert.That(unknown.EncodedBitLength, Is.GreaterThan(0));
            Assert.That(unknown.HasAlignmentPaddingBitCount, Is.True);
            Assert.That(unknown.AlignmentPaddingBitCount,
                Is.EqualTo(IsAligned(rules) ? 5 : 0));
            Assert.That(unknown.IsValid(), Is.True);
            Assert.That(reencoded.Success, Is.True);
            Assert.That(reencoded.Data, Is.EqualTo(encoded.Data));
        });
    }

    [Test]
    public void UnknownExtensionPolicyCanRejectOrSkip()
    {
        AsnEncodingRules rules = AsnEncodingRules.Uper;
        AsnEncodeResult encoded = Codec(NewerType(), rules).EncodeRequest(
            new AsnEncodeRequest
            {
                TypeName = "Module.Message",
                Value = new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.Sequence,
                    Fields =
                    [
                        Present("root", Boolean(false)),
                        Present("second", Boolean(true))
                    ]
                },
                Options = Options(rules, AsnUnknownDataPolicy.Preserve)
            });

        AsnDecodeResult rejected = Codec(OlderType(), rules).DecodeRequest(
            new AsnDecodeRequest
            {
                TypeName = "Module.Message",
                Data = encoded.Data,
                Options = Options(rules, AsnUnknownDataPolicy.Reject)
            });
        AsnDecodeResult skipped = Codec(OlderType(), rules).DecodeRequest(
            new AsnDecodeRequest
            {
                TypeName = "Module.Message",
                Data = encoded.Data,
                Options = Options(rules, AsnUnknownDataPolicy.Skip)
            });

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(rejected.Diagnostics.Single().Code,
                Is.EqualTo("ASNP0084"));
            Assert.That(skipped.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(skipped.Value?.UnknownExtensions, Is.Empty);
        });
    }

    [TestCase(AsnEncodingRules.Aper)]
    [TestCase(AsnEncodingRules.Uper)]
    public void BasicPerPreservesAbsentUnknownBitmapSlots(
        AsnEncodingRules rules)
    {
        AsnTypeDescriptor newer = NewerTypeWithTail();
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("root", Boolean(true)),
                Present("second", Boolean(true))
            ]
        };
        AsnEncodeResult encoded = Codec(newer, rules).EncodeRequest(
            new AsnEncodeRequest
            {
                TypeName = "Module.Message",
                Value = value,
                Options = Options(rules, AsnUnknownDataPolicy.Preserve)
            });
        AsnDecodeResult decoded = Codec(OlderType(), rules).DecodeRequest(
            new AsnDecodeRequest
            {
                TypeName = "Module.Message",
                Data = encoded.Data,
                Options = Options(rules, AsnUnknownDataPolicy.Preserve)
            });
        AsnUnknownExtension absent = decoded.Value?.UnknownExtensions.Single(
            extension => extension.Index == 2)
            ?? throw new InvalidOperationException();
        AsnEncodeResult reencoded = Codec(
            OlderType(),
            rules).EncodeRequest(new AsnEncodeRequest
            {
                TypeName = "Module.Message",
                Value = decoded.Value,
                Options = Options(rules, AsnUnknownDataPolicy.Preserve)
            });

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Value?.UnknownExtensions, Has.Length.EqualTo(2));
            Assert.That(absent.Present, Is.False);
            Assert.That(absent.HasEncodedIndex, Is.True);
            Assert.That(absent.HasBinaryData, Is.False);
            Assert.That(absent.IsValid(), Is.True);
            Assert.That(reencoded.Data, Is.EqualTo(encoded.Data));
        });
    }

    [Test]
    public void PreservedExtensionRequiresMatchingPerRules()
    {
        AsnTypeDescriptor older = OlderType();
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields = [Present("root", Boolean(true))],
            UnknownExtensions =
            [
                new AsnUnknownExtension
                {
                    Index = 1,
                    Present = true,
                    EncodingRules = AsnEncodingRules.Aper,
                    HasEncodingRules = true,
                    HasBinaryData = true,
                    BinaryData = [0x60]
                }
            ]
        };

        AsnEncodeResult result = Codec(
            older,
            AsnEncodingRules.Uper).EncodeRequest(new AsnEncodeRequest
            {
                TypeName = "Module.Message",
                Value = value,
                Options = Options(
                    AsnEncodingRules.Uper,
                    AsnUnknownDataPolicy.Preserve)
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo("ASNP0096"));
        });
    }

    private static AsnTypeDescriptor NewerType()
    {
        AsnFieldDescriptor root = Field(
            "root",
            Type(AsnTypeKind.Boolean, "Root"),
            0);
        AsnFieldDescriptor known = Field(
            "known",
            Type(AsnTypeKind.Boolean, "Known"),
            1);
        known.Extension = true;
        known.Optional = true;
        AsnFieldDescriptor first = Field(
            "first",
            Type(AsnTypeKind.Boolean, "First"),
            2);
        first.Extension = true;
        first.ExtensionGroupIndex = 0;
        first.Optional = true;
        AsnFieldDescriptor second = Field(
            "second",
            Type(AsnTypeKind.Boolean, "Second"),
            3);
        second.Extension = true;
        second.ExtensionGroupIndex = 0;
        second.Optional = true;
        AsnTypeDescriptor result = Type(AsnTypeKind.Sequence, "Message");
        result.Extensible = true;
        result.Fields = [root, known, first, second];
        result.RootFields = [root];
        result.ExtensionFields = [known, first, second];
        return result;
    }

    private static AsnTypeDescriptor OlderType()
    {
        AsnFieldDescriptor root = Field(
            "root",
            Type(AsnTypeKind.Boolean, "Root"),
            0);
        AsnFieldDescriptor known = Field(
            "known",
            Type(AsnTypeKind.Boolean, "Known"),
            1);
        known.Extension = true;
        known.Optional = true;
        AsnTypeDescriptor result = Type(AsnTypeKind.Sequence, "Message");
        result.Extensible = true;
        result.Fields = [root, known];
        result.RootFields = [root];
        result.ExtensionFields = [known];
        return result;
    }

    private static AsnTypeDescriptor NewerTypeWithTail()
    {
        AsnTypeDescriptor result = NewerType();
        AsnFieldDescriptor tail = Field(
            "tail",
            Type(AsnTypeKind.Boolean, "Tail"),
            4);
        tail.Extension = true;
        tail.Optional = true;
        result.Fields =
        [
            result.Fields[0],
            result.Fields[1],
            result.Fields[2],
            result.Fields[3],
            tail
        ];
        result.ExtensionFields =
        [
            result.ExtensionFields[0],
            result.ExtensionFields[1],
            result.ExtensionFields[2],
            tail
        ];
        return result;
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

    private static AsnCodecOptions Options(
        AsnEncodingRules rules,
        AsnUnknownDataPolicy policy)
    {
        return new AsnCodecOptions
        {
            EncodingRules = rules,
            UnknownDataPolicy = policy,
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

    private static bool IsAligned(AsnEncodingRules rules)
    {
        return rules == AsnEncodingRules.Aper
            || rules == AsnEncodingRules.CanonicalAlignedPer;
    }
}
