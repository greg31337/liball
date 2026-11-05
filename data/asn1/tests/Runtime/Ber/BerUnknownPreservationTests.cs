using asn1;

namespace asn1.Tests;

public class BerUnknownPreservationTests
{
    [Test]
    public void UnknownExtensionsRetainExactSlicesTagsLocationsAndOrdering()
    {
        AsnTypeDescriptor sequence = ExtensibleSequence();
        AsnReflectionCatalog catalog = CatalogWithType(
            "Module.Container",
            sequence);
        byte[] data =
        [
            0xff, 0xff,
            0x30, 0x0a,
            0x80, 0x01, 0x01,
            0x89, 0x81, 0x01, 0x7f,
            0x82, 0x01, 0x02
        ];
        AsnCodecOptions options = PreserveOptions();

        AsnDecodeResult decoded = new BerSchemaDecoder(catalog).DecodeRequest(
            new AsnDecodeRequest
            {
                TypeName = "Module.Container",
                Data = data,
                Offset = 2,
                HasCount = true,
                Count = 12,
                Options = options
            });
        AsnRuntimeValue value = decoded.Value
            ?? throw new InvalidOperationException();
        AsnUnknownExtension unknown = value.UnknownExtensions.Single();
        AsnEncodeResult encoded = new BerSchemaEncoder(catalog).EncodeRequest(
            new AsnEncodeRequest
            {
                TypeName = "Module.Container",
                Value = value,
                Options = options
            });

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(unknown.Tag?.Class,
                Is.EqualTo(AsnTagClass.ContextSpecific));
            Assert.That(unknown.Tag?.Number,
                Is.EqualTo(new System.Numerics.BigInteger(9)));
            Assert.That(unknown.HasEncodedIndex, Is.True);
            Assert.That(unknown.EncodedIndex, Is.EqualTo(1));
            Assert.That(unknown.HasEncodedLocation, Is.True);
            Assert.That(unknown.EncodedOffset, Is.EqualTo(7));
            Assert.That(unknown.EncodedLength, Is.EqualTo(4));
            Assert.That(unknown.BinaryData,
                Is.EqualTo(new byte[] { 0x89, 0x81, 0x01, 0x7f }));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Data, Is.EqualTo(data.Skip(2).ToArray()));
        });
    }

    [Test]
    public void ReencodingCanChangeKnownValuesWithoutChangingUnknownSlices()
    {
        AsnTypeDescriptor sequence = ExtensibleSequence();
        AsnReflectionCatalog catalog = CatalogWithType(
            "Module.Container",
            sequence);
        AsnCodecOptions options = PreserveOptions();
        byte[] original =
        [
            0x30, 0x0a,
            0x80, 0x01, 0x01,
            0x89, 0x81, 0x01, 0x7f,
            0x82, 0x01, 0x02
        ];
        AsnRuntimeValue value = new BerSchemaDecoder(catalog).DecodeRequest(
            new AsnDecodeRequest
            {
                TypeName = "Module.Container",
                Data = original,
                Options = options
            }).Value ?? throw new InvalidOperationException();
        AsnRuntimeValue known = value.Fields[0].Value
            ?? throw new InvalidOperationException();
        known.IntegerValue = 5;

        AsnEncodeResult encoded = new BerSchemaEncoder(catalog).EncodeRequest(
            new AsnEncodeRequest
            {
                TypeName = "Module.Container",
                Value = value,
                Options = options
            });

        Assert.That(encoded.Data, Is.EqualTo(new byte[]
        {
            0x30, 0x0a,
            0x80, 0x01, 0x05,
            0x89, 0x81, 0x01, 0x7f,
            0x82, 0x01, 0x02
        }));
    }

    [Test]
    public void UnknownNamedFieldsCanPreserveIndefiniteConstructedSlices()
    {
        AsnTypeDescriptor sequence = new AsnTypeDescriptor
        {
            Name = "OpenContainer",
            Kind = AsnTypeKind.Sequence,
            Extensible = true
        };
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            UnknownFields =
            [
                new AsnUnknownField
                {
                    Name = "future",
                    Tag = new AsnTag
                    {
                        Class = AsnTagClass.ContextSpecific,
                        Number = 3
                    },
                    HasEncodedIndex = true,
                    EncodedIndex = 0,
                    HasEncodedLocation = true,
                    EncodedOffset = 20,
                    EncodedLength = 7,
                    EncodingRules = AsnEncodingRules.Ber,
                    HasEncodingRules = true,
                    HasBinaryData = true,
                    BinaryData = [0xa3, 0x80, 0x02, 0x01, 0x01, 0x00, 0x00]
                }
            ]
        };
        value = new ValueAccess().NormalizeValue(
            value,
            sequence,
            new AsnReflectionCatalog());

        byte[] encoded = Write(new BerSchemaEncoder(
            new AsnReflectionCatalog()).EncodeTlv(
                sequence,
                value,
                PreserveOptions()));

        Assert.That(encoded, Is.EqualTo(new byte[]
        {
            0x30, 0x07,
            0xa3, 0x80, 0x02, 0x01, 0x01, 0x00, 0x00
        }));
        Assert.That(value.UnknownFields[0].EncodedOffset, Is.EqualTo(20));
    }

    [Test]
    public void OpenFieldsRetainExactSlicesAndLocationMetadata()
    {
        AsnTypeDescriptor open = new AsnTypeDescriptor
        {
            Name = "Open",
            Kind = AsnTypeKind.OpenType
        };
        AsnReflectionCatalog catalog = CatalogWithType("Module.Open", open);
        AsnCodecOptions options = PreserveOptions();
        byte[] input = [0xff, 0x02, 0x81, 0x01, 0x2a];

        AsnRuntimeValue value = new BerSchemaDecoder(catalog).DecodeRequest(
            new AsnDecodeRequest
            {
                TypeName = "Module.Open",
                Data = input,
                Offset = 1,
                HasCount = true,
                Count = 4,
                Options = options
            }).Value ?? throw new InvalidOperationException();
        AsnOpenTypeValue openValue = value.OpenTypeValue
            ?? throw new InvalidOperationException();
        AsnEncodeResult encoded = new BerSchemaEncoder(catalog).EncodeRequest(
            new AsnEncodeRequest
            {
                TypeName = "Module.Open",
                Value = value,
                Options = options
            });

        Assert.Multiple(() =>
        {
            Assert.That(openValue.Unresolved, Is.True);
            Assert.That(openValue.OriginalTag?.Number,
                Is.EqualTo(new System.Numerics.BigInteger(2)));
            Assert.That(openValue.HasEncodedLocation, Is.True);
            Assert.That(openValue.EncodedOffset, Is.EqualTo(1));
            Assert.That(openValue.EncodedLength, Is.EqualTo(4));
            Assert.That(openValue.OriginalData,
                Is.EqualTo(new byte[] { 0x02, 0x81, 0x01, 0x2a }));
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[] { 0x02, 0x81, 0x01, 0x2a }));
        });
    }

    [Test]
    public void ExactPreservationRejectsMismatchedMetadataAndNonBerOutput()
    {
        AsnTypeDescriptor sequence = ExtensibleSequence();
        AsnReflectionCatalog catalog = CatalogWithType(
            "Module.Container",
            sequence);
        AsnCodecOptions preserve = PreserveOptions();
        byte[] original =
        [
            0x30, 0x0a,
            0x80, 0x01, 0x01,
            0x89, 0x81, 0x01, 0x7f,
            0x82, 0x01, 0x02
        ];
        AsnRuntimeValue value = new BerSchemaDecoder(catalog).DecodeRequest(
            new AsnDecodeRequest
            {
                TypeName = "Module.Container",
                Data = original,
                Options = preserve
            }).Value ?? throw new InvalidOperationException();
        value.UnknownExtensions[0].Tag = new AsnTag
        {
            Class = AsnTagClass.ContextSpecific,
            Number = 8
        };
        BerSchemaEncodeResult mismatched = new BerSchemaEncoder(catalog)
            .EncodeTlv(sequence, value, preserve);

        value.UnknownExtensions[0].Tag = new AsnTag
        {
            Class = AsnTagClass.ContextSpecific,
            Number = 9
        };
        AsnCodecOptions der = PreserveOptions();
        der.EncodingRules = AsnEncodingRules.Der;
        BerSchemaEncodeResult nonBer = new BerSchemaEncoder(catalog)
            .EncodeTlv(sequence, value, der);

        Assert.Multiple(() =>
        {
            Assert.That(mismatched.Diagnostics.Single().Code,
                Is.EqualTo("ASNE0103"));
            Assert.That(nonBer.Diagnostics.Single().Code,
                Is.EqualTo("ASNE0094"));
        });
    }

    private static AsnTypeDescriptor ExtensibleSequence()
    {
        AsnFieldDescriptor id = Field("id", 0);
        AsnFieldDescriptor later = Field("later", 2);
        later.Extension = true;
        return new AsnTypeDescriptor
        {
            Name = "Container",
            Kind = AsnTypeKind.Sequence,
            Extensible = true,
            Fields = [id, later]
        };
    }

    private static AsnFieldDescriptor Field(string name, int tag)
    {
        return new AsnFieldDescriptor
        {
            Name = name,
            Type = new AsnTypeDescriptor { Kind = AsnTypeKind.Integer },
            Tags =
            [
                new AsnTagDescriptor
                {
                    Class = AsnTagClass.ContextSpecific,
                    Mode = AsnTaggingMode.Implicit,
                    Number = tag
                }
            ]
        };
    }

    private static AsnReflectionCatalog CatalogWithType(
        string name,
        AsnTypeDescriptor type)
    {
        return new AsnReflectionCatalog
        {
            Assignments =
            [
                new AsnAssignmentDescriptor
                {
                    Name = type.Name,
                    QualifiedName = name,
                    Type = type
                }
            ]
        };
    }

    private static AsnCodecOptions PreserveOptions()
    {
        return new AsnCodecOptions
        {
            EncodingRules = AsnEncodingRules.Ber,
            UnknownDataPolicy = AsnUnknownDataPolicy.Preserve
        };
    }

    private static byte[] Write(BerSchemaEncodeResult result)
    {
        BerTlv tlv = result.Tlv ?? throw new InvalidOperationException(
            string.Join(" | ", result.Diagnostics.Select(item =>
                item.Code + ":" + item.Message)));
        BerWriteResult written = new BerTlvWriterImplementation(
            new AsnResourceLimits()).Write(new BerWriteRequest
            {
                Tlv = tlv,
                EncodingRules = AsnEncodingRules.Ber
            });
        return written.Success
            ? written.Data
            : throw new InvalidOperationException(string.Join(
                " | ",
                written.Diagnostics.Select(item => item.Code + ":" + item.Message)));
    }
}
