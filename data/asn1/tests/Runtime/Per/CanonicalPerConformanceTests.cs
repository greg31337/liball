using asn1;

namespace asn1.Tests;

public class CanonicalPerConformanceTests
{
    [TestCase(AsnEncodingRules.CanonicalAlignedPer)]
    [TestCase(AsnEncodingRules.CanonicalUnalignedPer)]
    public void SetOfOutputIsIndependentOfInputPermutation(
        AsnEncodingRules rules)
    {
        AsnTypeDescriptor item = Type(AsnTypeKind.Integer, "Item");
        item.Constraints = [Bounds(AsnTypeKind.Integer, 0, 3)];
        AsnTypeDescriptor set = Type(AsnTypeKind.SetOf, "Numbers");
        set.ElementType = item;
        set.Constraints = [Bounds(AsnTypeKind.SetOf, 3, 3)];
        PerSchemaCodec codec = SchemaCodec(set, rules);
        AsnRuntimeValue[] first = [Integer(2), Integer(0), Integer(1)];
        AsnRuntimeValue[] second = [Integer(1), Integer(2), Integer(0)];
        AsnRuntimeValue[] third = [Integer(0), Integer(1), Integer(2)];

        byte[] firstEncoding = EncodeSet(codec, set, first, rules);
        byte[] secondEncoding = EncodeSet(codec, set, second, rules);
        byte[] thirdEncoding = EncodeSet(codec, set, third, rules);

        Assert.Multiple(() =>
        {
            Assert.That(firstEncoding, Is.EqualTo(new byte[] { 0x18 }));
            Assert.That(secondEncoding, Is.EqualTo(firstEncoding));
            Assert.That(thirdEncoding, Is.EqualTo(firstEncoding));
        });
    }

    [TestCase(AsnEncodingRules.CanonicalAlignedPer)]
    [TestCase(AsnEncodingRules.CanonicalUnalignedPer)]
    public void PaddingAndMalformedFragmentsProduceStrictInvalidReports(
        AsnEncodingRules rules)
    {
        IAsnCodec booleanCodec = PublicCodec(
            Type(AsnTypeKind.Boolean, "Flag"),
            rules,
            AsnUnknownDataPolicy.Reject);
        AsnCanonicalityResult padding = booleanCodec.CheckCanonicality(
            new AsnDecodeRequest
            {
                TypeName = "Module.Flag",
                Data = [0xc0]
            });

        byte[] malformed = new byte[32771];
        malformed[0] = 0xc1;
        malformed[16385] = 0xc1;
        malformed[32770] = 0x00;
        IAsnCodec octetCodec = PublicCodec(
            Type(AsnTypeKind.OctetString, "Payload"),
            rules,
            AsnUnknownDataPolicy.Reject);
        AsnCanonicalityResult fragments = octetCodec.CheckCanonicality(
            new AsnDecodeRequest
            {
                TypeName = "Module.Payload",
                Data = malformed
            });

        Assert.Multiple(() =>
        {
            AssertInvalidReport(padding, rules, "ASNP0007", 1);
            AssertInvalidReport(fragments, rules, "ASNP0038", 0);
        });
    }

    [TestCase(AsnEncodingRules.CanonicalAlignedPer)]
    [TestCase(AsnEncodingRules.CanonicalUnalignedPer)]
    public void UnknownExtensionTailIsReportedAndCanonicalEncodingTrimsIt(
        AsnEncodingRules rules)
    {
        AsnEncodingRules basicRules = rules
            == AsnEncodingRules.CanonicalAlignedPer
                ? AsnEncodingRules.Aper
                : AsnEncodingRules.Uper;
        AsnTypeDescriptor newer = NewerMessage();
        AsnTypeDescriptor older = OlderMessage();
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("root", Boolean(true)),
                Present("second", Boolean(true))
            ]
        };
        AsnEncodeResult basic = SchemaCodec(
            newer,
            basicRules).EncodeRequest(new AsnEncodeRequest
            {
                TypeName = "Module.Message",
                Value = value,
                Options = Options(
                    basicRules,
                    AsnUnknownDataPolicy.Preserve)
            });
        IAsnCodec canonicalCodec = PublicCodec(
            older,
            rules,
            AsnUnknownDataPolicy.Preserve);
        AsnCanonicalityResult report = canonicalCodec.CheckCanonicality(
            new AsnDecodeRequest
            {
                TypeName = "Module.Message",
                Data = basic.Data
            });
        AsnDecodeResult preserved = SchemaCodec(
            older,
            basicRules).DecodeRequest(new AsnDecodeRequest
            {
                TypeName = "Module.Message",
                Data = basic.Data,
                Options = Options(
                    basicRules,
                    AsnUnknownDataPolicy.Preserve)
            });
        AsnRuntimeValue decoded = preserved.Value
            ?? throw new InvalidOperationException();
        int extensionIndex = 0;
        while (extensionIndex < decoded.UnknownExtensions.Length)
        {
            decoded.UnknownExtensions[extensionIndex].EncodingRules = rules;
            extensionIndex = extensionIndex + 1;
        }

        AsnEncodeResult canonical = canonicalCodec.Encode(
            new AsnEncodeRequest
            {
                TypeName = "Module.Message",
                Value = decoded
            });
        AsnCanonicalityResult repaired = canonicalCodec.CheckCanonicality(
            new AsnDecodeRequest
            {
                TypeName = "Module.Message",
                Data = canonical.Data
            });

        Assert.Multiple(() =>
        {
            Assert.That(report.Status,
                Is.EqualTo(AsnCanonicalityStatus.NonCanonical));
            Assert.That(report.Diagnostics.Single().Code,
                Is.EqualTo("ASNP0094"));
            Assert.That(report.Diagnostics.Single().Category,
                Is.EqualTo(AsnDiagnosticCategory.Canonicality));
            Assert.That(report.HasViolationOffset, Is.True);
            Assert.That(report.ViolationOffsetIsBit, Is.True);
            Assert.That(report.Repairable, Is.False);
            Assert.That(preserved.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(decoded.UnknownExtensions, Has.Length.EqualTo(2));
            Assert.That(decoded.UnknownExtensions[0].Present, Is.True);
            Assert.That(decoded.UnknownExtensions[1].Present, Is.False);
            Assert.That(canonical.Success, Is.True);
            Assert.That(canonical.Data, Is.Not.EqualTo(basic.Data));
            Assert.That(repaired.Status,
                Is.EqualTo(AsnCanonicalityStatus.Canonical));
        });
    }

    [TestCase(AsnEncodingRules.CanonicalAlignedPer)]
    [TestCase(AsnEncodingRules.CanonicalUnalignedPer)]
    public void ObjectTableDispatchWrapsCanonicalResolvedPayload(
        AsnEncodingRules rules)
    {
        AsnTypeDescriptor payloadType = Type(
            AsnTypeKind.Integer,
            "Payload");
        payloadType.Constraints = [Bounds(AsnTypeKind.Integer, 0, 3)];
        AsnObjectTableDescriptor table = Table(payloadType, 7);
        AsnOpenTypeValue open = new AsnOpenTypeValue
        {
            Resolved = true,
            ResolvedTypeId = payloadType.StableId,
            TypeName = payloadType.Name,
            Value = Integer(1),
            DispatchKey = Integer(7)
        };
        PerTableOpenTypeCodec codec = new PerTableOpenTypeCodec(
            new AsnResourceLimits());
        PerBitWriter writer = new PerBitWriter(new AsnResourceLimits());
        PerTableOpenTypeResult encoded = codec.WriteTableOpenType(
            writer,
            table,
            open,
            [0x40],
            2,
            AsnUnknownDataPolicy.Reject,
            rules,
            rules == AsnEncodingRules.CanonicalAlignedPer);
        PerBitWriteResult output = writer.GetOutput();
        PerBitReader reader = new PerBitReader(new AsnResourceLimits());
        reader.AppendInputBits(
            output.Data,
            0,
            output.WrittenBitCount,
            true);
        PerTableOpenTypeResult decoded = codec.ReadTableOpenType(
            reader,
            table,
            Integer(7),
            AsnUnknownDataPolicy.Reject,
            rules,
            Integer(1),
            rules == AsnEncodingRules.CanonicalAlignedPer);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(output.Data,
                Is.EqualTo(new byte[] { 0x01, 0x40 }));
            Assert.That(output.WrittenBitCount, Is.EqualTo(16));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.ResolvedType, Is.SameAs(payloadType));
            Assert.That(decoded.OpenValue?.Resolved, Is.True);
            Assert.That(decoded.OpenValue?.ResolvedTypeId,
                Is.EqualTo(payloadType.StableId));
            Assert.That(decoded.OpenValue?.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(1)));
        });
    }

    private static void AssertInvalidReport(
        AsnCanonicalityResult result,
        AsnEncodingRules rules,
        string code,
        int bitOffset)
    {
        Assert.That(result.Status,
            Is.EqualTo(AsnCanonicalityStatus.Invalid));
        Assert.That(result.EncodingRules, Is.EqualTo(rules));
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(code));
        Assert.That(result.Diagnostics.Single().Category,
            Is.EqualTo(AsnDiagnosticCategory.Decoding));
        Assert.That(result.HasViolationOffset, Is.True);
        Assert.That(result.ViolationOffset, Is.EqualTo(bitOffset));
        Assert.That(result.ViolationOffsetIsBit, Is.True);
        Assert.That(result.Repairable, Is.False);
        Assert.That(result.Path, Is.EqualTo("$"));
    }

    private static byte[] EncodeSet(
        PerSchemaCodec codec,
        AsnTypeDescriptor set,
        AsnRuntimeValue[] items,
        AsnEncodingRules rules)
    {
        AsnEncodeResult encoded = codec.EncodeRequest(new AsnEncodeRequest
        {
            TypeName = "Module." + set.Name,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.SetOf,
                Items = items
            },
            Options = Options(rules, AsnUnknownDataPolicy.Reject)
        });
        Assert.That(encoded.Success, Is.True);
        return encoded.Data;
    }

    private static IAsnCodec PublicCodec(
        AsnTypeDescriptor type,
        AsnEncodingRules rules,
        AsnUnknownDataPolicy policy)
    {
        return new CodecProviderImplementation().CreateCodec(
            Catalog(type),
            Options(rules, policy));
    }

    private static PerSchemaCodec SchemaCodec(
        AsnTypeDescriptor type,
        AsnEncodingRules rules)
    {
        return new PerSchemaCodec(Catalog(type), rules);
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

    private static AsnTypeDescriptor NewerMessage()
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
        AsnFieldDescriptor second = Field(
            "second",
            Type(AsnTypeKind.Boolean, "Second"),
            2);
        second.Extension = true;
        second.Optional = true;
        AsnFieldDescriptor tail = Field(
            "tail",
            Type(AsnTypeKind.Boolean, "Tail"),
            3);
        tail.Extension = true;
        tail.Optional = true;
        AsnTypeDescriptor result = Type(AsnTypeKind.Sequence, "Message");
        result.Extensible = true;
        result.Fields = [root, known, second, tail];
        result.RootFields = [root];
        result.ExtensionFields = [known, second, tail];
        return result;
    }

    private static AsnTypeDescriptor OlderMessage()
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

    private static AsnObjectTableDescriptor Table(
        AsnTypeDescriptor type,
        int key)
    {
        return new AsnObjectTableDescriptor
        {
            Entries =
            [
                new AsnObjectTableEntryDescriptor
                {
                    Key = new AsnValueDescriptor
                    {
                        Value = new AsnValue
                        {
                            Kind = AsnValueKind.Integer,
                            HasIntegerValue = true,
                            IntegerValue = key
                        }
                    },
                    Type = type
                }
            ]
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
