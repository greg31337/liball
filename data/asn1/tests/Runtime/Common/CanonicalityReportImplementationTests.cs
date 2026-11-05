using asn1;

namespace asn1.Tests;

public class CanonicalityReportImplementationTests
{
    [TestCase(AsnEncodingRules.Der)]
    [TestCase(AsnEncodingRules.Cer)]
    public void BerCanonicalityReportsRuleByteOffsetCodeAndRepairability(
        AsnEncodingRules rules)
    {
        IAsnCodec codec = Codec(
            Type(AsnTypeKind.Boolean, "Flag"),
            rules);

        AsnCanonicalityResult canonical = Check(
            codec,
            "Schema.Flag",
            [0x01, 0x01, 0xff]);
        AsnCanonicalityResult noncanonical = Check(
            codec,
            "Schema.Flag",
            [0x01, 0x01, 0x01]);
        AsnCanonicalityResult repeated = Check(
            codec,
            "Schema.Flag",
            [0x01, 0x01, 0x01]);
        AsnCanonicalityResult repaired = RepairAndCheck(
            Type(AsnTypeKind.Boolean, "Flag"),
            rules,
            [0x01, 0x01, 0x01]);

        Assert.Multiple(() =>
        {
            AssertCanonical(canonical, rules);
            Assert.That(noncanonical.Status,
                Is.EqualTo(AsnCanonicalityStatus.NonCanonical));
            Assert.That(noncanonical.HasEncodingRules, Is.True);
            Assert.That(noncanonical.EncodingRules, Is.EqualTo(rules));
            Assert.That(noncanonical.HasViolationOffset, Is.True);
            Assert.That(noncanonical.ViolationOffset, Is.EqualTo(2));
            Assert.That(noncanonical.ViolationOffsetIsBit, Is.False);
            Assert.That(noncanonical.Path, Is.EqualTo("$"));
            Assert.That(noncanonical.Diagnostics.Single().Code,
                Is.EqualTo("ASNCAN0005"));
            Assert.That(noncanonical.Diagnostics.Single().Category,
                Is.EqualTo(AsnDiagnosticCategory.Canonicality));
            Assert.That(noncanonical.Repairable, Is.True);
            AssertStable(noncanonical, repeated);
            AssertCanonical(repaired, rules);
        });
    }

    [TestCase(AsnEncodingRules.CanonicalAlignedPer)]
    [TestCase(AsnEncodingRules.CanonicalUnalignedPer)]
    public void CanonicalPerReportsUnsortedSetOfAsRepairable(
        AsnEncodingRules rules)
    {
        AsnTypeDescriptor item = Type(AsnTypeKind.Integer, "Item");
        item.Constraints = [Bounds(AsnTypeKind.Integer, 0, 3)];
        AsnTypeDescriptor numbers = Type(AsnTypeKind.SetOf, "Numbers");
        numbers.ElementType = item;
        numbers.Constraints = [Bounds(AsnTypeKind.SetOf, 3, 3)];
        IAsnCodec codec = Codec(numbers, rules);

        AsnCanonicalityResult canonical = Check(
            codec,
            "Schema.Numbers",
            [0x18]);
        AsnCanonicalityResult noncanonical = Check(
            codec,
            "Schema.Numbers",
            [0x84]);
        AsnCanonicalityResult repeated = Check(
            codec,
            "Schema.Numbers",
            [0x84]);
        AsnCanonicalityResult repaired = RepairAndCheck(
            numbers,
            rules,
            [0x84]);

        Assert.Multiple(() =>
        {
            AssertCanonical(canonical, rules);
            AssertNoncanonical(
                noncanonical,
                rules,
                "ASNP0070",
                true,
                6,
                "$");
            AssertStable(noncanonical, repeated);
            AssertCanonical(repaired, rules);
        });
    }

    [Test]
    public void CoerReportsNoncanonicalSetOfOrderAsRepairable()
    {
        AsnTypeDescriptor numbers = Type(AsnTypeKind.SetOf, "Numbers");
        numbers.ElementType = Type(AsnTypeKind.Integer, "Item");
        IAsnCodec codec = Codec(numbers, AsnEncodingRules.Coer);

        AsnCanonicalityResult canonical = Check(
            codec,
            "Schema.Numbers",
            [0x01, 0x02, 0x01, 0x01, 0x01, 0x02]);
        AsnCanonicalityResult noncanonical = Check(
            codec,
            "Schema.Numbers",
            [0x01, 0x02, 0x01, 0x02, 0x01, 0x01]);
        AsnCanonicalityResult repeated = Check(
            codec,
            "Schema.Numbers",
            [0x01, 0x02, 0x01, 0x02, 0x01, 0x01]);
        AsnCanonicalityResult repaired = RepairAndCheck(
            numbers,
            AsnEncodingRules.Coer,
            [0x01, 0x02, 0x01, 0x02, 0x01, 0x01]);

        Assert.Multiple(() =>
        {
            AssertCanonical(canonical, AsnEncodingRules.Coer);
            AssertNoncanonical(
                noncanonical,
                AsnEncodingRules.Coer,
                "ASNO0085",
                false,
                4,
                "$");
            AssertStable(noncanonical, repeated);
            AssertCanonical(repaired, AsnEncodingRules.Coer);
        });
    }

    [Test]
    public void CanonicalXerReportsNoncanonicalEscapingAsRepairable()
    {
        IAsnCodec codec = Codec(
            Type(AsnTypeKind.Utf8String, "Text"),
            AsnEncodingRules.CanonicalXer);

        AsnCanonicalityResult canonical = Check(
            codec,
            "Schema.Text",
            System.Text.Encoding.UTF8.GetBytes("<Text>&lt;</Text>"));
        AsnCanonicalityResult noncanonical = Check(
            codec,
            "Schema.Text",
            System.Text.Encoding.UTF8.GetBytes("<Text>&#60;</Text>"));
        AsnCanonicalityResult repeated = Check(
            codec,
            "Schema.Text",
            System.Text.Encoding.UTF8.GetBytes("<Text>&#60;</Text>"));
        AsnCanonicalityResult repaired = RepairAndCheck(
            Type(AsnTypeKind.Utf8String, "Text"),
            AsnEncodingRules.CanonicalXer,
            System.Text.Encoding.UTF8.GetBytes("<Text>&#60;</Text>"));

        Assert.Multiple(() =>
        {
            AssertCanonical(canonical, AsnEncodingRules.CanonicalXer);
            AssertNoncanonical(
                noncanonical,
                AsnEncodingRules.CanonicalXer,
                "ASNXER0200",
                false,
                7,
                "$");
            AssertStable(noncanonical, repeated);
            AssertCanonical(repaired, AsnEncodingRules.CanonicalXer);
        });
    }

    [TestCase(AsnEncodingRules.Der)]
    [TestCase(AsnEncodingRules.Cer)]
    [TestCase(AsnEncodingRules.CanonicalAlignedPer)]
    [TestCase(AsnEncodingRules.CanonicalUnalignedPer)]
    [TestCase(AsnEncodingRules.Coer)]
    [TestCase(AsnEncodingRules.CanonicalXer)]
    public void CanonicalEncoderOutputAndSemanticInvalidityAgreeAcrossFamilies(
        AsnEncodingRules rules)
    {
        AsnReflectionCatalog catalog = SemanticCatalog();
        IAsnCodec encoder = Codec(catalog, rules, false);
        IAsnCodec checker = Codec(catalog, rules, true);
        AsnEncodeResult validBytes = EncodeSemantic(encoder, false);
        AsnEncodeResult invalidBytes = EncodeSemantic(encoder, true);

        AsnCanonicalityResult valid = Check(
            checker,
            "Semantic.Packet",
            validBytes.Data);
        AsnCanonicalityResult invalid = Check(
            checker,
            "Semantic.Packet",
            invalidBytes.Data);
        AsnCanonicalityResult repeated = Check(
            checker,
            "Semantic.Packet",
            invalidBytes.Data);

        Assert.Multiple(() =>
        {
            Assert.That(validBytes.Success, Is.True, Format(validBytes));
            Assert.That(invalidBytes.Success, Is.True, Format(invalidBytes));
            AssertCanonical(valid, rules);
            Assert.That(invalid.Status,
                Is.EqualTo(AsnCanonicalityStatus.Invalid));
            Assert.That(invalid.HasEncodingRules, Is.True);
            Assert.That(invalid.EncodingRules, Is.EqualTo(rules));
            Assert.That(invalid.HasViolationOffset, Is.True);
            Assert.That(invalid.ViolationOffset,
                Is.EqualTo(invalidBytes.Data.Length));
            Assert.That(invalid.ViolationOffsetIsBit, Is.False);
            Assert.That(invalid.Path, Is.EqualTo("$.text"));
            Assert.That(invalid.Diagnostics.Single().Category,
                Is.EqualTo(AsnDiagnosticCategory.Constraint));
            Assert.That(invalid.Repairable, Is.False);
            AssertStable(invalid, repeated);
        });
    }

    [Test]
    public void InvalidAndUnsupportedInputsReturnDiagnostics()
    {
        AsnTypeDescriptor flag = Type(AsnTypeKind.Boolean, "Flag");
        AsnCanonicalityResult invalid = Check(
            Codec(flag, AsnEncodingRules.Der),
            "Schema.Flag",
            [0x01, 0x01]);
        AsnCanonicalityResult unsupported = Check(
            Codec(flag, AsnEncodingRules.Ber),
            "Schema.Flag",
            [0x01, 0x01, 0xff]);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Status,
                Is.EqualTo(AsnCanonicalityStatus.Invalid));
            Assert.That(invalid.Diagnostics, Is.Not.Empty);
            Assert.That(invalid.Repairable, Is.False);
            Assert.That(unsupported.Status,
                Is.EqualTo(AsnCanonicalityStatus.Invalid));
            Assert.That(unsupported.EncodingRules,
                Is.EqualTo(AsnEncodingRules.Ber));
            Assert.That(unsupported.Diagnostics.Single().Code,
                Is.EqualTo("ASNCODEC0005"));
            Assert.That(unsupported.Repairable, Is.False);
        });
    }

    private static void AssertCanonical(
        AsnCanonicalityResult result,
        AsnEncodingRules rules)
    {
        Assert.That(result.Status,
            Is.EqualTo(AsnCanonicalityStatus.Canonical));
        Assert.That(result.HasEncodingRules, Is.True);
        Assert.That(result.EncodingRules, Is.EqualTo(rules));
        Assert.That(result.HasViolationOffset, Is.False);
        Assert.That(result.Path, Is.EqualTo("$"));
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.Repairable, Is.False);
    }

    private static void AssertNoncanonical(
        AsnCanonicalityResult result,
        AsnEncodingRules rules,
        string code,
        bool bitOffset,
        int offset,
        string path)
    {
        Assert.That(result.Status,
            Is.EqualTo(AsnCanonicalityStatus.NonCanonical));
        Assert.That(result.HasEncodingRules, Is.True);
        Assert.That(result.EncodingRules, Is.EqualTo(rules));
        Assert.That(result.HasViolationOffset, Is.True);
        Assert.That(result.ViolationOffset, Is.EqualTo(offset));
        Assert.That(result.ViolationOffsetIsBit, Is.EqualTo(bitOffset));
        Assert.That(result.Path, Is.EqualTo(path));
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(code));
        Assert.That(result.Diagnostics.Single().Category,
            Is.EqualTo(AsnDiagnosticCategory.Canonicality));
        Assert.That(result.Repairable, Is.True);
    }

    private static void AssertStable(
        AsnCanonicalityResult first,
        AsnCanonicalityResult second)
    {
        Assert.That(second.Status, Is.EqualTo(first.Status));
        Assert.That(second.HasViolationOffset,
            Is.EqualTo(first.HasViolationOffset));
        Assert.That(second.ViolationOffset,
            Is.EqualTo(first.ViolationOffset));
        Assert.That(second.ViolationOffsetIsBit,
            Is.EqualTo(first.ViolationOffsetIsBit));
        Assert.That(second.Path, Is.EqualTo(first.Path));
        Assert.That(second.Diagnostics.Select(item => item.Code),
            Is.EqualTo(first.Diagnostics.Select(item => item.Code)));
    }

    private static AsnCanonicalityResult RepairAndCheck(
        AsnTypeDescriptor type,
        AsnEncodingRules canonicalRules,
        byte[] data)
    {
        AsnEncodingRules basicRules = BasicRule(canonicalRules);
        IAsnCodec basic = Codec(type, basicRules);
        AsnDecodeResult decoded = basic.Decode(new AsnDecodeRequest
        {
            TypeName = "Schema." + type.Name,
            Data = data
        });
        Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
        IAsnCodec canonical = Codec(type, canonicalRules);
        AsnEncodeResult repaired = canonical.Encode(new AsnEncodeRequest
        {
            TypeName = "Schema." + type.Name,
            Value = decoded.Value
        });
        Assert.That(repaired.Success, Is.True, Format(repaired));
        return Check(canonical, "Schema." + type.Name, repaired.Data);
    }

    private static AsnCanonicalityResult Check(
        IAsnCodec codec,
        string typeName,
        byte[] data)
    {
        return codec.CheckCanonicality(new AsnDecodeRequest
        {
            TypeName = typeName,
            Data = data
        });
    }

    private static IAsnCodec Codec(
        AsnTypeDescriptor type,
        AsnEncodingRules rules)
    {
        return Codec(type, rules, true);
    }

    private static IAsnCodec Codec(
        AsnTypeDescriptor type,
        AsnEncodingRules rules,
        bool validateValues)
    {
        AsnCodecOptions options = Options(rules, validateValues);
        return new CodecProviderImplementation().CreateCodec(
            Catalog(type),
            options);
    }

    private static IAsnCodec Codec(
        AsnReflectionCatalog catalog,
        AsnEncodingRules rules,
        bool validateValues)
    {
        return new CodecProviderImplementation().CreateCodec(
            catalog,
            Options(rules, validateValues));
    }

    private static AsnCodecOptions Options(AsnEncodingRules rules)
    {
        return Options(rules, true);
    }

    private static AsnCodecOptions Options(
        AsnEncodingRules rules,
        bool validateValues)
    {
        return new AsnCodecOptions
        {
            EncodingRules = rules,
            UnknownDataPolicy = AsnUnknownDataPolicy.Reject,
            ValidateValues = validateValues
        };
    }

    private static AsnReflectionCatalog SemanticCatalog()
    {
        AsnTypeDescriptor text = new AsnTypeDescriptor
        {
            StableId = "type.Packet.text",
            Name = "text",
            GeneratedName = "text",
            Kind = AsnTypeKind.Utf8String,
            Constraints =
            [
                new AsnConstraintDescriptor
                {
                    StableId = "constraint.Packet.text.application",
                    Kind = AsnConstraintKind.UserDefined,
                    DomainKind = AsnTypeKind.Utf8String,
                    Constraint = new AsnConstraint
                    {
                        Kind = AsnConstraintKind.UserDefined,
                        RequiresApplicationValidation = true
                    }
                }
            ]
        };
        AsnFieldDescriptor field = new AsnFieldDescriptor
        {
            StableId = "field.Packet.text",
            Name = "text",
            GeneratedName = "text",
            Index = 0,
            Type = text,
            Optional = true
        };
        AsnTypeDescriptor packet = new AsnTypeDescriptor
        {
            StableId = "type.Packet",
            Name = "Packet",
            GeneratedName = "Packet",
            Kind = AsnTypeKind.Sequence,
            Fields = [field],
            RootFields = [field]
        };
        return new AsnReflectionCatalog
        {
            Assignments =
            [
                new AsnAssignmentDescriptor
                {
                    StableId = "assignment.Packet",
                    Name = "Packet",
                    QualifiedName = "Semantic.Packet",
                    GeneratedName = "Packet",
                    Kind = AsnAssignmentKind.Type,
                    Type = packet
                }
            ]
        };
    }

    private static AsnEncodeResult EncodeSemantic(
        IAsnCodec codec,
        bool present)
    {
        return codec.Encode(new AsnEncodeRequest
        {
            TypeName = "Semantic.Packet",
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Sequence,
                Fields = present
                    ?
                    [
                        new AsnRuntimeFieldValue
                        {
                            Name = "text",
                            Present = true,
                            Value = new AsnRuntimeValue
                            {
                                Kind = AsnRuntimeValueKind.CharacterString,
                                TextValue = "VALID"
                            }
                        }
                    ]
                    : []
            }
        });
    }

    private static AsnEncodingRules BasicRule(AsnEncodingRules rules)
    {
        if (rules == AsnEncodingRules.Der || rules == AsnEncodingRules.Cer)
        {
            return AsnEncodingRules.Ber;
        }
        if (rules == AsnEncodingRules.CanonicalAlignedPer)
        {
            return AsnEncodingRules.Aper;
        }
        if (rules == AsnEncodingRules.CanonicalUnalignedPer)
        {
            return AsnEncodingRules.Uper;
        }
        if (rules == AsnEncodingRules.Coer)
        {
            return AsnEncodingRules.Oer;
        }
        return AsnEncodingRules.BasicXer;
    }

    private static string Format(AsnEncodeResult result)
    {
        return string.Join(
            " | ",
            result.Diagnostics.Select(item => item.Code + ":" + item.Message));
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

    private static AsnConstraintDescriptor Bounds(
        AsnTypeKind domain,
        int lower,
        int upper)
    {
        return new AsnConstraintDescriptor
        {
            DomainKind = domain,
            Kind = AsnConstraintKind.ValueRange,
            RootIntervals =
            [
                new AsnIntervalDescriptor
                {
                    HasLowerBound = true,
                    LowerBound = new System.Numerics.BigInteger(lower),
                    HasUpperBound = true,
                    UpperBound = new System.Numerics.BigInteger(upper)
                }
            ]
        };
    }
}
