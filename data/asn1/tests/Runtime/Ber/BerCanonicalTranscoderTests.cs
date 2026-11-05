using asn1;

namespace asn1.Tests;

public class BerCanonicalTranscoderTests
{
    [Test]
    public void SchemaAwareTranscodingAppliesImplicitTypesAndOmitsDefaults()
    {
        AsnFieldDescriptor id = Field(
            "id",
            new AsnTypeDescriptor { Kind = AsnTypeKind.Integer },
            0);
        AsnFieldDescriptor count = Field(
            "count",
            new AsnTypeDescriptor { Kind = AsnTypeKind.Integer },
            1);
        count.HasDefaultValue = true;
        count.DefaultValue = new AsnValueDescriptor
        {
            Kind = AsnValueKind.Integer,
            Value = new AsnValue
            {
                Kind = AsnValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = 3
            }
        };
        AsnTypeDescriptor sequence = new AsnTypeDescriptor
        {
            StableId = "type.message",
            Name = "Message",
            Kind = AsnTypeKind.Sequence,
            Fields = [id, count]
        };
        AsnEncodeResult result = CreateTranscoder(Catalog(
            "Module.Message",
            sequence)).TranscodeSchema(
                new AsnDecodeRequest
                {
                    TypeName = "Module.Message",
                    Data = [0x30, 0x06, 0x80, 0x01, 0x05, 0x81, 0x01, 0x03],
                    Options = new AsnCodecOptions
                    {
                        ValidateValues = false
                    }
                },
                AsnEncodingRules.Der);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data,
                Is.EqualTo(new byte[] { 0x30, 0x03, 0x80, 0x01, 0x05 }));
            Assert.That(result.WrittenCount, Is.EqualTo(5));
        });
    }

    [Test]
    public void SchemaAwareCerTranscodingSegmentsImplicitLongStrings()
    {
        AsnTypeDescriptor octets = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.OctetString
        };
        AsnFieldDescriptor data = Field("data", octets, 0);
        AsnTypeDescriptor sequence = new AsnTypeDescriptor
        {
            StableId = "type.payload",
            Name = "Payload",
            Kind = AsnTypeKind.Sequence,
            Fields = [data]
        };
        BerTlv input = new BerTlv
        {
            Tag = UniversalTag(AsnUniversalTags.Sequence),
            Constructed = true,
            Children =
            [
                new BerTlv
                {
                    Tag = new AsnTag
                    {
                        Class = AsnTagClass.ContextSpecific,
                        Number = 0
                    },
                    Content = new byte[1001]
                }
            ]
        };
        byte[] ber = Write(input, AsnEncodingRules.Ber);
        AsnEncodeResult result = CreateTranscoder(Catalog(
            "Module.Payload",
            sequence)).TranscodeSchema(
                new AsnDecodeRequest
                {
                    TypeName = "Module.Payload",
                    Data = ber,
                    Options = new AsnCodecOptions
                    {
                        ValidateValues = false
                    }
                },
                AsnEncodingRules.Cer);
        BerReadResult read = Read(result.Data);
        BerTlv root = read.Tlv ?? throw new InvalidOperationException();
        BerTlv encodedData = root.Children.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(root.IndefiniteLength, Is.True);
            Assert.That(encodedData.Tag.Class,
                Is.EqualTo(AsnTagClass.ContextSpecific));
            Assert.That(encodedData.Constructed, Is.True);
            Assert.That(encodedData.IndefiniteLength, Is.True);
            Assert.That(encodedData.Children.Select(item => item.Content.Length),
                Is.EqualTo(new int[] { 1000, 1 }));
            Assert.That(result.Data.TakeLast(4),
                Is.EqualTo(new byte[] { 0x00, 0x00, 0x00, 0x00 }));
        });
    }

    [Test]
    public void SchemaLessTranscodingCanonicalizesKnownUniversalValues()
    {
        BerCanonicalTranscoder transcoder = CreateTranscoder(null);
        AsnEncodeResult sequence = transcoder.TranscodeTlv(
            [0x30, 0x80, 0x01, 0x01, 0x01, 0x02, 0x01, 0x01, 0x00, 0x00],
            0,
            10,
            AsnEncodingRules.Der);
        AsnEncodeResult set = transcoder.TranscodeTlv(
            [0x31, 0x06, 0x02, 0x01, 0x02, 0x02, 0x01, 0x01],
            0,
            8,
            AsnEncodingRules.Der);
        AsnEncodeResult stringResult = transcoder.TranscodeTlv(
            [0x24, 0x80, 0x04, 0x01, 0x41, 0x04, 0x01, 0x42, 0x00, 0x00],
            0,
            10,
            AsnEncodingRules.Der);

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Data, Is.EqualTo(new byte[]
            {
                0x30, 0x06, 0x01, 0x01, 0xff, 0x02, 0x01, 0x01
            }));
            Assert.That(set.Data, Is.EqualTo(new byte[]
            {
                0x31, 0x06, 0x02, 0x01, 0x01, 0x02, 0x01, 0x02
            }));
            Assert.That(stringResult.Data,
                Is.EqualTo(new byte[] { 0x04, 0x02, 0x41, 0x42 }));
            Assert.That(sequence.Success, Is.True);
            Assert.That(set.Success, Is.True);
            Assert.That(stringResult.Success, Is.True);
        });
    }

    [Test]
    public void SchemaLessCerTranscodingSelectsCanonicalStringSegments()
    {
        BerTlv primitive = new BerTlv
        {
            Tag = UniversalTag(AsnUniversalTags.OctetString),
            Content = new byte[1001]
        };
        byte[] ber = Write(primitive, AsnEncodingRules.Ber);
        AsnEncodeResult result = CreateTranscoder(null).TranscodeTlv(
            ber,
            0,
            ber.Length,
            AsnEncodingRules.Cer);
        BerTlv transcoded = Read(result.Data).Tlv
            ?? throw new InvalidOperationException();
        AsnCanonicalityResult canonical = new BerCanonicalValidator(
            new AsnResourceLimits()).ValidateInput(
                result.Data,
                0,
                result.Data.Length,
                AsnEncodingRules.Cer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(transcoded.Constructed, Is.True);
            Assert.That(transcoded.IndefiniteLength, Is.True);
            Assert.That(transcoded.Children.Select(item => item.Content.Length),
                Is.EqualTo(new int[] { 1000, 1 }));
            Assert.That(canonical.Status,
                Is.EqualTo(AsnCanonicalityStatus.Canonical));
        });
    }

    [Test]
    public void TranscodingRejectsValuesWhoseSemanticsAreUnavailable()
    {
        BerCanonicalTranscoder schemaLess = CreateTranscoder(null);
        AsnEncodeResult implicitValue = schemaLess.TranscodeTlv(
            [0x80, 0x01, 0x01],
            0,
            3,
            AsnEncodingRules.Der);
        AsnEncodeResult unknownUniversal = schemaLess.TranscodeTlv(
            [0x0f, 0x00],
            0,
            2,
            AsnEncodingRules.Der);

        AsnTypeDescriptor extensible = new AsnTypeDescriptor
        {
            StableId = "type.extensible",
            Name = "Extensible",
            Kind = AsnTypeKind.Sequence,
            Extensible = true
        };
        AsnEncodeResult unknownExtension = CreateTranscoder(Catalog(
            "Module.Extensible",
            extensible)).TranscodeSchema(
                new AsnDecodeRequest
                {
                    TypeName = "Module.Extensible",
                    Data = [0x30, 0x03, 0x80, 0x01, 0x01],
                    Options = new AsnCodecOptions
                    {
                        ValidateValues = false
                    }
                },
                AsnEncodingRules.Der);

        Assert.Multiple(() =>
        {
            Assert.That(implicitValue.Success, Is.False);
            Assert.That(implicitValue.Diagnostics.Single().Code,
                Is.EqualTo("ASNT0002"));
            Assert.That(implicitValue.Diagnostics.Single().Message,
                Does.Contain("byte offset 0"));
            Assert.That(unknownUniversal.Diagnostics.Single().Code,
                Is.EqualTo("ASNT0002"));
            Assert.That(unknownExtension.Diagnostics.Single().Code,
                Is.EqualTo("ASNT0002"));
            Assert.That(unknownExtension.Diagnostics.Single().Message,
                Does.Contain("Module.Extensible"));
        });
    }

    private static BerCanonicalTranscoder CreateTranscoder(
        AsnReflectionCatalog? catalog)
    {
        return new BerCanonicalTranscoder(catalog, new AsnResourceLimits());
    }

    private static AsnFieldDescriptor Field(
        string name,
        AsnTypeDescriptor type,
        int tag)
    {
        return new AsnFieldDescriptor
        {
            Name = name,
            Type = type,
            Tags =
            [
                new AsnTagDescriptor
                {
                    Class = AsnTagClass.ContextSpecific,
                    Number = tag,
                    Mode = AsnTaggingMode.Implicit
                }
            ]
        };
    }

    private static AsnReflectionCatalog Catalog(
        string qualifiedName,
        AsnTypeDescriptor type)
    {
        return new AsnReflectionCatalog
        {
            Assignments =
            [
                new AsnAssignmentDescriptor
                {
                    Kind = AsnAssignmentKind.Type,
                    Name = type.Name,
                    QualifiedName = qualifiedName,
                    StableId = type.StableId,
                    Type = type
                }
            ]
        };
    }

    private static AsnTag UniversalTag(int number)
    {
        return new AsnTag
        {
            Class = AsnTagClass.Universal,
            Number = number
        };
    }

    private static byte[] Write(BerTlv tlv, AsnEncodingRules rules)
    {
        BerWriteResult result = new BerTlvWriterImplementation(
            new AsnResourceLimits()).Write(new BerWriteRequest
            {
                Tlv = tlv,
                EncodingRules = rules
            });
        Assert.That(result.Success, Is.True);
        return result.Data;
    }

    private static BerReadResult Read(byte[] data)
    {
        BerReadResult result = new BerTlvReaderImplementation(
            new AsnResourceLimits()).Read(new BerReadRequest
            {
                Data = data,
                HasCount = true,
                Count = data.Length,
                EncodingRules = AsnEncodingRules.Ber,
                PreserveEncodedData = true
            });
        Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Done));
        return result;
    }
}
