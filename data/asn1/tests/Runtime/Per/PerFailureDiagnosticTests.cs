using asn1;

namespace asn1.Tests;

public class PerFailureDiagnosticTests
{
    [Test]
    public void PaddingAndTrailingDataReportExactOffendingBits()
    {
        AsnTypeDescriptor flag = Type(AsnTypeKind.Boolean, "Flag");

        AsnDecodeResult padding = Decode(flag, [0xc0]);
        AsnDecodeResult trailing = Decode(flag, [0x80, 0x80]);

        AssertDiagnostic(padding, "ASNP0007", 1);
        AssertDiagnostic(trailing, "ASNP0008", 8);
    }

    [Test]
    public void UnusedConstrainedOffsetReportsFieldStart()
    {
        AsnTypeDescriptor integer = Type(AsnTypeKind.Integer, "Number");
        integer.Constraints =
        [
            Bounds(AsnTypeKind.Integer, 0, 2)
        ];

        AsnDecodeResult result = Decode(integer, [0xc0]);

        AssertDiagnostic(result, "ASNP0021", 0);
    }

    [Test]
    public void ForbiddenExtensionIndexReportsIndexStart()
    {
        AsnFieldDescriptor root = Field(
            "root",
            Type(AsnTypeKind.Boolean, "Root"),
            0);
        AsnFieldDescriptor extension = Field(
            "extension",
            Type(AsnTypeKind.Boolean, "Extension"),
            1);
        extension.Extension = true;
        AsnTypeDescriptor choice = Type(AsnTypeKind.Choice, "Selection");
        choice.Extensible = true;
        choice.Fields = [root, extension];
        choice.RootFields = [root];
        choice.ExtensionFields = [extension];

        AsnDecodeResult result = Decode(choice, [0x81]);

        AssertDiagnostic(result, "ASNP0064", 1);
    }

    [Test]
    public void MalformedOctetFragmentsReportFirstBadDeterminant()
    {
        byte[] malformed = new byte[32771];
        malformed[0] = 0xc1;
        malformed[16385] = 0xc1;
        malformed[32770] = 0x00;
        AsnTypeDescriptor octets = Type(
            AsnTypeKind.OctetString,
            "Payload");

        AsnDecodeResult result = Decode(octets, malformed);

        AssertDiagnostic(result, "ASNP0038", 0);
    }

    [Test]
    public void MalformedCollectionFragmentsReportFirstBadDeterminant()
    {
        AsnTypeDescriptor collection = Type(
            AsnTypeKind.SequenceOf,
            "Nulls");
        collection.ElementType = Type(AsnTypeKind.Null, "Item");

        AsnDecodeResult result = Decode(collection, [0xc1, 0xc1, 0x00]);

        AssertDiagnostic(result, "ASNP0071", 0);
    }

    [Test]
    public void TruncatedOpenTypeAndImpossibleLengthReportDeterminants()
    {
        AsnTypeDescriptor open = Type(AsnTypeKind.OpenType, "Open");
        AsnTypeDescriptor octets = Type(
            AsnTypeKind.OctetString,
            "Payload");

        AsnDecodeResult truncated = Decode(open, [0x01]);
        AsnDecodeResult impossible = Decode(octets, [0x80, 0x7f]);

        AssertDiagnostic(truncated, "ASNP0004", 8);
        AssertDiagnostic(impossible, "ASNP0036", 0);
    }

    [Test]
    public void ResourceFailureReportsInputStartAndResourceCategory()
    {
        AsnTypeDescriptor octets = Type(
            AsnTypeKind.OctetString,
            "Payload");
        AsnCodecOptions options = Options();
        options.ResourceLimits = new AsnResourceLimits
        {
            MaximumOpenTypeBytes = 1,
            MaximumTotalAllocationBytes = 16
        };

        AsnDecodeResult result = Decode(octets, [0x02, 0xaa, 0xbb], options);
        AsnDiagnostic diagnostic = result.Diagnostics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(diagnostic.Code, Is.EqualTo("ASNR0006"));
            Assert.That(diagnostic.Category,
                Is.EqualTo(AsnDiagnosticCategory.Resource));
            Assert.That(diagnostic.HasBitOffset, Is.True);
            Assert.That(diagnostic.BitOffset, Is.Zero);
        });
    }

    [Test]
    public void IncrementalFailureRetainsExactBitOffset()
    {
        AsnTypeDescriptor flag = Type(AsnTypeKind.Boolean, "Flag");
        PerIncrementalSchemaDecoder decoder =
            new PerIncrementalSchemaDecoder(
                Catalog(flag),
                "Module.Flag",
                Options());

        AsnIncrementalResult result = decoder.Consume(
            new AsnIncrementalInput
            {
                Data = [0xc0],
                IsFinalBlock = true
            });
        AsnDiagnostic diagnostic = result.Diagnostics.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(diagnostic.Code, Is.EqualTo("ASNP0007"));
            Assert.That(diagnostic.HasBitOffset, Is.True);
            Assert.That(diagnostic.BitOffset, Is.EqualTo(1));
        });
    }

    private static void AssertDiagnostic(
        AsnDecodeResult result,
        string code,
        int bitOffset)
    {
        AsnDiagnostic diagnostic = result.Diagnostics.Single();
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(diagnostic.Code, Is.EqualTo(code));
            Assert.That(diagnostic.HasBitOffset, Is.True);
            Assert.That(diagnostic.BitOffset, Is.EqualTo(bitOffset));
        });
    }

    private static AsnDecodeResult Decode(
        AsnTypeDescriptor type,
        byte[] data)
    {
        return Decode(type, data, Options());
    }

    private static AsnDecodeResult Decode(
        AsnTypeDescriptor type,
        byte[] data,
        AsnCodecOptions options)
    {
        return new PerSchemaCodec(
            Catalog(type),
            AsnEncodingRules.Aper).DecodeRequest(new AsnDecodeRequest
            {
                TypeName = "Module." + type.Name,
                Data = data,
                Options = options
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
}
