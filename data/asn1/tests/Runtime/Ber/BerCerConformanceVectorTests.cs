using asn1;

namespace asn1.Tests;

public class BerCerConformanceVectorTests
{
    [Test]
    public void EveryStringKindRoundTripsAcrossTheCanonicalContentBoundary()
    {
        AsnTypeKind[] kinds =
        [
            AsnTypeKind.BitString,
            AsnTypeKind.OctetString,
            AsnTypeKind.Utf8String,
            AsnTypeKind.NumericString,
            AsnTypeKind.PrintableString,
            AsnTypeKind.TeletexString,
            AsnTypeKind.VideotexString,
            AsnTypeKind.Ia5String,
            AsnTypeKind.GraphicString,
            AsnTypeKind.VisibleString,
            AsnTypeKind.GeneralString,
            AsnTypeKind.UniversalString,
            AsnTypeKind.BmpString
        ];
        BerPrimitiveCodec primitives = new BerPrimitiveCodec(
            new AsnResourceLimits());
        BerStringCodec strings = new BerStringCodec(new AsnResourceLimits());

        int kindIndex = 0;
        while (kindIndex < kinds.Length)
        {
            AsnTypeKind kind = kinds[kindIndex];
            int[] lengths = BoundaryContentLengths(kind);
            int boundaryIndex = 0;
            while (boundaryIndex < lengths.Length)
            {
                AsnRuntimeValue source = StringValue(kind, lengths[boundaryIndex]);
                BerPrimitiveEncodeResult encoded = primitives.EncodePrimitive(
                    kind,
                    source,
                    AsnEncodingRules.Cer);
                BerTlv tlv = encoded.Tlv ?? throw new InvalidOperationException(
                    Format(encoded.Diagnostics));
                BerStringResult decoded = strings.DecodeConstructedString(
                    kind,
                    tlv,
                    AsnEncodingRules.Cer);
                byte[] data = Write(tlv, AsnEncodingRules.Cer);
                AsnCanonicalityResult canonical = Validate(data);

                Assert.That(encoded.Success, Is.True,
                    kind + " length " + lengths[boundaryIndex]);
                Assert.That(tlv.Constructed,
                    Is.EqualTo(boundaryIndex == 2),
                    kind + " length " + lengths[boundaryIndex]);
                Assert.That(decoded.Success, Is.True,
                    kind + ": " + decoded.ErrorMessage);
                AssertEquivalent(kind, source, decoded.Value);
                Assert.That(canonical.Status,
                    Is.EqualTo(AsnCanonicalityStatus.Canonical),
                    kind + " length " + lengths[boundaryIndex]);
                if (boundaryIndex == 2)
                {
                    Assert.That(tlv.IndefiniteLength, Is.True);
                    Assert.That(tlv.Children[0].Content, Has.Length.EqualTo(1000));
                    Assert.That(data.TakeLast(2),
                        Is.EqualTo(new byte[] { 0x00, 0x00 }));
                }

                boundaryIndex++;
            }

            kindIndex++;
        }
    }

    [Test]
    public void ObjectDescriptorUsesTheSame999To1001CerBoundary()
    {
        BerUsefulTypeCodec codec = new BerUsefulTypeCodec(
            new AsnResourceLimits());
        int[] lengths = [999, 1000, 1001];

        int index = 0;
        while (index < lengths.Length)
        {
            string text = new string('A', lengths[index]);
            BerPrimitiveEncodeResult encoded = codec.EncodeUsefulType(
                AsnTypeKind.ObjectDescriptor,
                new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.CharacterString,
                    TextValue = text
                },
                AsnEncodingRules.Cer);
            BerPrimitiveDecodeResult decoded = codec.DecodeUsefulType(
                AsnTypeKind.ObjectDescriptor,
                encoded.Tlv ?? throw new InvalidOperationException(),
                AsnEncodingRules.Cer);
            byte[] data = Write(
                encoded.Tlv ?? throw new InvalidOperationException(),
                AsnEncodingRules.Cer);

            Assert.That(encoded.Tlv?.Constructed,
                Is.EqualTo(index == 2));
            Assert.That(decoded.Value?.TextValue, Is.EqualTo(text));
            Assert.That(Validate(data).Status,
                Is.EqualTo(AsnCanonicalityStatus.Canonical));
            index++;
        }
    }

    [Test]
    public void CerDecoderRejectsNonCanonicalPrimitiveAndSegmentedStrings()
    {
        BerStringCodec strings = new BerStringCodec(new AsnResourceLimits());
        BerPrimitiveDecodeResult primitive = new BerPrimitiveCodec(
            new AsnResourceLimits()).DecodePrimitive(
                AsnTypeKind.OctetString,
                PrimitiveString(1001),
                AsnEncodingRules.Cer);
        BerTlv shortConstructed = ConstructedString(
            true,
            PrimitiveString(10));
        BerTlv shortFirst = ConstructedString(
            true,
            PrimitiveString(999),
            PrimitiveString(2));
        BerTlv definite = ConstructedString(
            false,
            PrimitiveString(1000),
            PrimitiveString(1));
        BerTlv nested = ConstructedString(
            true,
            ConstructedString(true, PrimitiveString(1000)),
            PrimitiveString(1));

        BerStringResult shortResult = strings.DecodeConstructedString(
            AsnTypeKind.OctetString,
            shortConstructed,
            AsnEncodingRules.Cer);
        BerStringResult firstResult = strings.DecodeConstructedString(
            AsnTypeKind.OctetString,
            shortFirst,
            AsnEncodingRules.Cer);
        BerStringResult definiteResult = strings.DecodeConstructedString(
            AsnTypeKind.OctetString,
            definite,
            AsnEncodingRules.Cer);
        BerStringResult nestedResult = strings.DecodeConstructedString(
            AsnTypeKind.OctetString,
            nested,
            AsnEncodingRules.Cer);
        BerStringResult permissiveBer = strings.DecodeConstructedString(
            AsnTypeKind.OctetString,
            shortFirst,
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(primitive.Success, Is.False);
            Assert.That(primitive.Diagnostics.Single().Code,
                Is.EqualTo("ASNCAN0011"));
            AssertCerStringFailure(shortResult);
            AssertCerStringFailure(firstResult);
            AssertCerStringFailure(definiteResult);
            AssertCerStringFailure(nestedResult);
            Assert.That(permissiveBer.Success, Is.True);
            Assert.That(permissiveBer.Value?.Octets, Has.Length.EqualTo(1001));
        });
    }

    [Test]
    public void CerRequiresIndefiniteCollectionsAndCanonicalSetOfOrder()
    {
        BerConstructedCodec codec = new BerConstructedCodec(
            new AsnResourceLimits());
        BerTlv definiteSequence = new BerTlv
        {
            Tag = UniversalTag(AsnUniversalTags.Sequence),
            Constructed = true,
            Children = [Integer(1)]
        };
        BerTlv indefiniteSequence = new BerTlv
        {
            Tag = UniversalTag(AsnUniversalTags.Sequence),
            Constructed = true,
            IndefiniteLength = true,
            Children = [Integer(1)]
        };
        BerConstructedResult rejected = codec.ReadSequenceValue(
            definiteSequence,
            AsnEncodingRules.Cer);
        BerConstructedResult accepted = codec.ReadSequenceValue(
            indefiniteSequence,
            AsnEncodingRules.Cer);
        BerConstructedResult sorted = codec.EncodeSetOfValue(
            [Integer(2), Integer(1)],
            AsnEncodingRules.Cer,
            false);
        byte[] canonicalData = Write(
            sorted.Tlv ?? throw new InvalidOperationException(),
            AsnEncodingRules.Cer);
        BerTlv unsorted = new BerTlv
        {
            Tag = UniversalTag(AsnUniversalTags.Set),
            Constructed = true,
            IndefiniteLength = true,
            Children = [Integer(2), Integer(1)]
        };
        AsnCanonicalityResult unsortedResult = Validate(
            Write(unsorted, AsnEncodingRules.Cer));

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Success, Is.False);
            Assert.That(accepted.Success, Is.True);
            Assert.That(sorted.Tlv?.IndefiniteLength, Is.True);
            Assert.That(sorted.Tlv?.Children.Select(item => item.Content[0]),
                Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(canonicalData.TakeLast(2),
                Is.EqualTo(new byte[] { 0x00, 0x00 }));
            Assert.That(Validate(canonicalData).Status,
                Is.EqualTo(AsnCanonicalityStatus.Canonical));
            Assert.That(unsortedResult.Status,
                Is.EqualTo(AsnCanonicalityStatus.NonCanonical));
            Assert.That(unsortedResult.Diagnostics.Single().Code,
                Is.EqualTo("ASNCAN0008"));
        });
    }

    private static int[] BoundaryContentLengths(AsnTypeKind kind)
    {
        if (kind == AsnTypeKind.UniversalString)
        {
            return [996, 1000, 1004];
        }

        if (kind == AsnTypeKind.BmpString)
        {
            return [998, 1000, 1002];
        }

        return [999, 1000, 1001];
    }

    private static AsnRuntimeValue StringValue(
        AsnTypeKind kind,
        int contentLength)
    {
        if (kind == AsnTypeKind.BitString)
        {
            return new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.BitString,
                BitStringValue = new AsnBitStringValue
                {
                    Bytes = new byte[contentLength - 1],
                    BitLength = (contentLength - 1) * 8
                }
            };
        }

        if (kind == AsnTypeKind.OctetString)
        {
            return new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.OctetString,
                Octets = new byte[contentLength]
            };
        }

        int unit = kind == AsnTypeKind.UniversalString
            ? 4
            : kind == AsnTypeKind.BmpString ? 2 : 1;
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.CharacterString,
            TextValue = new string('1', contentLength / unit)
        };
    }

    private static void AssertEquivalent(
        AsnTypeKind kind,
        AsnRuntimeValue expected,
        AsnRuntimeValue? actual)
    {
        Assert.That(actual, Is.Not.Null);
        if (kind == AsnTypeKind.BitString)
        {
            Assert.That(actual?.BitStringValue?.BitLength,
                Is.EqualTo(expected.BitStringValue?.BitLength));
        }
        else if (kind == AsnTypeKind.OctetString)
        {
            Assert.That(actual?.Octets, Is.EqualTo(expected.Octets));
        }
        else
        {
            Assert.That(actual?.TextValue, Is.EqualTo(expected.TextValue));
        }
    }

    private static void AssertCerStringFailure(BerStringResult result)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("ASNCAN0011"));
    }

    private static BerTlv PrimitiveString(int count)
    {
        return new BerTlv
        {
            Tag = UniversalTag(AsnUniversalTags.OctetString),
            Content = new byte[count]
        };
    }

    private static BerTlv ConstructedString(
        bool indefinite,
        params BerTlv[] children)
    {
        return new BerTlv
        {
            Tag = UniversalTag(AsnUniversalTags.OctetString),
            Constructed = true,
            IndefiniteLength = indefinite,
            Children = children
        };
    }

    private static BerTlv Integer(int value)
    {
        return new BerTlv
        {
            Tag = UniversalTag(AsnUniversalTags.Integer),
            Content = [(byte)value]
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
        Assert.That(result.Success, Is.True, Format(result.Diagnostics));
        return result.Data;
    }

    private static AsnCanonicalityResult Validate(byte[] data)
    {
        return new BerCanonicalValidator(new AsnResourceLimits())
            .ValidateInput(data, 0, data.Length, AsnEncodingRules.Cer);
    }

    private static string Format(AsnDiagnostic[] diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(item =>
            item.Code + ":" + item.Message));
    }
}
