using asn1;

namespace asn1.Tests;

public class OerFailureDiagnosticTests
{
    [Test]
    public void MalformedAndUnknownChoiceTagsReportExactOffsets()
    {
        AsnTypeDescriptor choice = Type(AsnTypeKind.Choice, "Selection");
        choice.Fields =
        [
            Field("flag", Type(AsnTypeKind.Boolean, "Flag"), 0)
        ];
        AsnTypeDescriptor packet = Sequence(
            "Packet",
            Field("prefix", Type(AsnTypeKind.Boolean, "Prefix"), 0),
            Field("selection", choice, 1));

        AsnDecodeResult malformed = Decode(
            packet,
            [0xff, 0x3f, 0x00],
            AsnEncodingRules.Oer);
        AsnDecodeResult unknown = Decode(
            packet,
            [0xff, 0x02],
            AsnEncodingRules.Oer);

        AssertDiagnostic(malformed, "ASNO0071", 2);
        AssertDiagnostic(unknown, "ASNO0077", 1);
    }

    [Test]
    public void NestedPreamblePaddingReportsItsContainingOctet()
    {
        AsnFieldDescriptor optional = Field(
            "optional",
            Type(AsnTypeKind.Boolean, "Optional"),
            0);
        optional.Optional = true;
        AsnTypeDescriptor inner = Sequence("Inner", optional);
        AsnTypeDescriptor packet = Sequence(
            "Packet",
            Field("prefix", Type(AsnTypeKind.Boolean, "Prefix"), 0),
            Field("inner", inner, 1));

        AsnDecodeResult result = Decode(
            packet,
            [0xff, 0x01],
            AsnEncodingRules.Coer);

        AssertDiagnostic(result, "ASNO0051", 1);
    }

    [Test]
    public void NestedNoncanonicalLengthReportsTheDeterminantOctet()
    {
        AsnTypeDescriptor packet = Sequence(
            "Packet",
            Field("prefix", Type(AsnTypeKind.Boolean, "Prefix"), 0),
            Field(
                "payload",
                Type(AsnTypeKind.OctetString, "Payload"),
                1));

        AsnDecodeResult result = Decode(
            packet,
            [0xff, 0x81, 0x01, 0xaa],
            AsnEncodingRules.Coer);

        AssertDiagnostic(result, "ASNO0004", 1);
    }

    [Test]
    public void FinalIncrementalTruncationReportsTheFirstMissingOctet()
    {
        AsnTypeDescriptor integer = Type(
            AsnTypeKind.Integer,
            "Number");
        integer.Constraints = [Bounds(0, 65535)];
        AsnTypeDescriptor packet = Sequence(
            "Packet",
            Field("prefix", Type(AsnTypeKind.Boolean, "Prefix"), 0),
            Field("number", integer, 1));
        AsnCodecOptions options = Options(AsnEncodingRules.Oer);
        OerIncrementalSchemaDecoder decoder =
            new OerIncrementalSchemaDecoder(
                Catalog(packet),
                "Module.Packet",
                options);

        AsnIncrementalResult waiting = decoder.Consume(
            new AsnIncrementalInput { Data = [0xff, 0x12] });
        AsnIncrementalResult result = decoder.Complete();

        Assert.Multiple(() =>
        {
            Assert.That(waiting.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo("ASNO0105"));
            Assert.That(result.Diagnostics.Single().HasBitOffset, Is.True);
            Assert.That(result.Diagnostics.Single().BitOffset,
                Is.EqualTo(16));
            Assert.That(result.Diagnostics.Single().Message,
                Does.Contain("Byte offset: 2."));
        });
    }

    [Test]
    public void ImpossibleEffectiveIntegerBoundsReportTheFieldStart()
    {
        AsnTypeDescriptor integer = Type(
            AsnTypeKind.Integer,
            "Number");
        integer.Constraints = [Bounds(0, 10), Bounds(20, 30)];
        AsnTypeDescriptor packet = Sequence(
            "Packet",
            Field("prefix", Type(AsnTypeKind.Boolean, "Prefix"), 0),
            Field("number", integer, 1));

        AsnDecodeResult result = Decode(
            packet,
            [0xff, 0x00],
            AsnEncodingRules.Oer);

        AssertDiagnostic(result, "ASNO0011", 1);
    }

    [Test]
    public void ResourceViolationReportsTheLengthThatExceedsTheLimit()
    {
        AsnTypeDescriptor packet = Sequence(
            "Packet",
            Field("prefix", Type(AsnTypeKind.Boolean, "Prefix"), 0),
            Field(
                "payload",
                Type(AsnTypeKind.OctetString, "Payload"),
                1));
        AsnCodecOptions options = Options(AsnEncodingRules.Oer);
        options.ResourceLimits = new AsnResourceLimits
        {
            MaximumOpenTypeBytes = 1,
            MaximumTotalAllocationBytes = 32
        };

        AsnDecodeResult result = new OerSchemaCodec(
            Catalog(packet),
            AsnEncodingRules.Oer).DecodeRequest(new AsnDecodeRequest
            {
                TypeName = "Module.Packet",
                Data = [0xff, 0x02, 0xaa, 0xbb],
                Options = options
            });
        AsnDiagnostic diagnostic = result.Diagnostics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(diagnostic.Code, Is.EqualTo("ASNR0020"));
            Assert.That(diagnostic.Category,
                Is.EqualTo(AsnDiagnosticCategory.Resource));
            Assert.That(diagnostic.HasBitOffset, Is.True);
            Assert.That(diagnostic.BitOffset, Is.EqualTo(8));
        });
    }

    [Test]
    public void TrailingDataReportsTheFirstUnconsumedOctet()
    {
        AsnDecodeResult result = Decode(
            Type(AsnTypeKind.Boolean, "Flag"),
            [0xff, 0x00],
            AsnEncodingRules.Oer);

        AssertDiagnostic(result, "ASNO0103", 1);
    }

    private static void AssertDiagnostic(
        AsnDecodeResult result,
        string code,
        int byteOffset)
    {
        AsnDiagnostic diagnostic = result.Diagnostics.Single();
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(diagnostic.Code, Is.EqualTo(code));
            Assert.That(diagnostic.HasBitOffset, Is.True);
            Assert.That(diagnostic.BitOffset, Is.EqualTo(byteOffset * 8));
            Assert.That(diagnostic.Message,
                Does.Contain("Byte offset: " + byteOffset + "."));
        });
    }

    private static AsnDecodeResult Decode(
        AsnTypeDescriptor type,
        byte[] data,
        AsnEncodingRules rules)
    {
        return new OerSchemaCodec(Catalog(type), rules).DecodeRequest(
            new AsnDecodeRequest
            {
                TypeName = "Module." + type.Name,
                Data = data,
                Options = Options(rules)
            });
    }

    private static AsnReflectionCatalog Catalog(AsnTypeDescriptor type)
    {
        return new AsnReflectionCatalog
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
        };
    }

    private static AsnCodecOptions Options(AsnEncodingRules rules)
    {
        return new AsnCodecOptions
        {
            EncodingRules = rules,
            UnknownDataPolicy = AsnUnknownDataPolicy.Preserve,
            ValidateValues = false
        };
    }

    private static AsnTypeDescriptor Sequence(
        string name,
        params AsnFieldDescriptor[] fields)
    {
        return new AsnTypeDescriptor
        {
            StableId = "type." + name,
            Name = name,
            Kind = AsnTypeKind.Sequence,
            Fields = fields,
            RootFields = fields
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
}
