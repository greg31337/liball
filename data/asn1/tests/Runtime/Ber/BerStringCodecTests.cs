using asn1;

namespace asn1.Tests;

public class BerStringCodecTests
{
    [Test]
    public void OctetStringSegmentsAndFlattensRecursiveConstructedForms()
    {
        BerStringCodec codec = CreateCodec();
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OctetString,
            Octets = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9]
        };
        BerStringResult encoded = codec.EncodeConstructedString(
            AsnTypeKind.OctetString,
            value,
            AsnEncodingRules.Ber,
            4,
            true);
        BerTlv root = encoded.Tlv ?? throw new InvalidOperationException();
        root.Children =
        [
            new BerTlv
            {
                Tag = new AsnTag { Number = AsnUniversalTags.OctetString },
                Constructed = true,
                IndefiniteLength = true,
                Children = [root.Children[0], root.Children[1]]
            },
            root.Children[2]
        ];
        BerStringResult decoded = codec.DecodeConstructedString(
            AsnTypeKind.OctetString,
            root,
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(root.IndefiniteLength, Is.True);
            Assert.That(root.Children[0].Children.Select(item => item.Content.Length),
                Is.EqualTo(new int[] { 4, 4 }));
            Assert.That(root.Children[1].Content.Length, Is.EqualTo(2));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Value?.Octets,
                Is.EqualTo(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
        });
    }

    [Test]
    public void BitStringPlacesUnusedBitsOnlyOnTheFinalSegment()
    {
        BerStringCodec codec = CreateCodec();
        BerStringResult encoded = codec.EncodeConstructedString(
            AsnTypeKind.BitString,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.BitString,
                BitStringValue = new AsnBitStringValue
                {
                    Bytes = [0xaa, 0xbb, 0xc0],
                    BitLength = 18
                }
            },
            AsnEncodingRules.Ber,
            3,
            true);
        BerStringResult decoded = codec.DecodeConstructedString(
            AsnTypeKind.BitString,
            encoded.Tlv ?? throw new InvalidOperationException(),
            AsnEncodingRules.Ber);
        BerTlv malformed = new BerTlv
        {
            Tag = new AsnTag { Number = AsnUniversalTags.BitString },
            Constructed = true,
            Children =
            [
                MakeFragment(AsnUniversalTags.BitString, [1, 0x80]),
                MakeFragment(AsnUniversalTags.BitString, [0, 0x00])
            ]
        };
        BerStringResult invalid = codec.DecodeConstructedString(
            AsnTypeKind.BitString,
            malformed,
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Tlv?.Children[0].Content,
                Is.EqualTo(new byte[] { 0, 0xaa, 0xbb }));
            Assert.That(encoded.Tlv?.Children[1].Content,
                Is.EqualTo(new byte[] { 6, 0xc0 }));
            Assert.That(decoded.Value?.BitStringValue?.BitLength, Is.EqualTo(18));
            Assert.That(decoded.Value?.BitStringValue?.Bytes,
                Is.EqualTo(new byte[] { 0xaa, 0xbb, 0xc0 }));
            Assert.That(invalid.ErrorCode, Is.EqualTo("ASND0084"));
        });
    }

    [Test]
    public void CharacterStringSegmentsDoNotSplitEncodedCharacters()
    {
        BerStringCodec codec = CreateCodec();
        BerStringResult utf8 = codec.EncodeConstructedString(
            AsnTypeKind.Utf8String,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.CharacterString,
                TextValue = "A😀B"
            },
            AsnEncodingRules.Ber,
            4,
            false);
        BerStringResult universal = codec.EncodeConstructedString(
            AsnTypeKind.UniversalString,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.CharacterString,
                TextValue = "A😀"
            },
            AsnEncodingRules.Ber,
            5,
            false);
        BerStringResult tooSmall = codec.EncodeConstructedString(
            AsnTypeKind.Utf8String,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.CharacterString,
                TextValue = "😀"
            },
            AsnEncodingRules.Ber,
            3,
            false);
        BerStringResult decoded = codec.DecodeConstructedString(
            AsnTypeKind.Utf8String,
            utf8.Tlv ?? throw new InvalidOperationException(),
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(utf8.Tlv?.Children.Select(item => item.Content.Length),
                Is.EqualTo(new int[] { 1, 4, 1 }));
            Assert.That(universal.Tlv?.Children.Select(item => item.Content.Length),
                Is.EqualTo(new int[] { 4, 4 }));
            Assert.That(tooSmall.ErrorCode, Is.EqualTo("ASNE0069"));
            Assert.That(decoded.Value?.TextValue, Is.EqualTo("A😀B"));
        });
    }

    [Test]
    public void CerSelectsCanonicalStringFormsAndThousandOctetFragments()
    {
        BerPrimitiveCodec primitives = new BerPrimitiveCodec(
            new AsnResourceLimits());
        BerPrimitiveEncodeResult exact = primitives.EncodePrimitive(
            AsnTypeKind.OctetString,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.OctetString,
                Octets = new byte[1000]
            },
            AsnEncodingRules.Cer);
        BerPrimitiveEncodeResult octets = primitives.EncodePrimitive(
            AsnTypeKind.OctetString,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.OctetString,
                Octets = new byte[1001]
            },
            AsnEncodingRules.Cer);
        BerPrimitiveEncodeResult bits = primitives.EncodePrimitive(
            AsnTypeKind.BitString,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.BitString,
                BitStringValue = new AsnBitStringValue
                {
                    Bytes = new byte[1000],
                    BitLength = 8000
                }
            },
            AsnEncodingRules.Cer);
        BerPrimitiveEncodeResult bmp = primitives.EncodePrimitive(
            AsnTypeKind.BmpString,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.CharacterString,
                TextValue = new string('A', 501)
            },
            AsnEncodingRules.Cer);
        BerWriteResult written = new BerTlvWriterImplementation(
            new AsnResourceLimits()).Write(new BerWriteRequest
            {
                Tlv = octets.Tlv,
                EncodingRules = AsnEncodingRules.Cer
            });

        Assert.Multiple(() =>
        {
            Assert.That(exact.Tlv?.Constructed, Is.False);
            Assert.That(exact.Tlv?.Content.Length, Is.EqualTo(1000));
            Assert.That(octets.Tlv?.Constructed, Is.True);
            Assert.That(octets.Tlv?.IndefiniteLength, Is.True);
            Assert.That(octets.Tlv?.Children.Select(child => child.Content.Length),
                Is.EqualTo(new int[] { 1000, 1 }));
            Assert.That(bits.Tlv?.Children.Select(child => child.Content.Length),
                Is.EqualTo(new int[] { 1000, 2 }));
            Assert.That(bmp.Tlv?.Children.Select(child => child.Content.Length),
                Is.EqualTo(new int[] { 1000, 2 }));
            Assert.That(written.Data.Take(4),
                Is.EqualTo(new byte[] { 0x24, 0x80, 0x04, 0x82 }));
            Assert.That(written.Data.TakeLast(2),
                Is.EqualTo(new byte[] { 0x00, 0x00 }));
        });
    }

    [Test]
    public void CerStringFragmentsRespectEncodedCharacterBoundaries()
    {
        BerPrimitiveEncodeResult encoded = new BerPrimitiveCodec(
            new AsnResourceLimits()).EncodePrimitive(
                AsnTypeKind.Utf8String,
                new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.CharacterString,
                    TextValue = new string('A', 999) + "😀"
                },
                AsnEncodingRules.Cer);
        BerStringResult decoded = CreateCodec().DecodeConstructedString(
            AsnTypeKind.Utf8String,
            encoded.Tlv ?? throw new InvalidOperationException(),
            AsnEncodingRules.Cer);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Tlv?.Children.Select(child => child.Content.Length),
                Is.EqualTo(new int[] { 999, 4 }));
            Assert.That(decoded.Value?.TextValue,
                Is.EqualTo(new string('A', 999) + "😀"));
        });
    }

    [Test]
    public void ConstructedStringsRejectDerAndMismatchedFragmentTags()
    {
        BerStringCodec codec = CreateCodec();
        BerTlv constructed = new BerTlv
        {
            Tag = new AsnTag { Number = AsnUniversalTags.OctetString },
            Constructed = true,
            Children = [MakeFragment(AsnUniversalTags.Utf8String, [1])]
        };
        BerStringResult der = codec.DecodeConstructedString(
            AsnTypeKind.OctetString,
            constructed,
            AsnEncodingRules.Der);
        BerStringResult wrongFragment = codec.DecodeConstructedString(
            AsnTypeKind.OctetString,
            constructed,
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(der.ErrorCode, Is.EqualTo("ASND0082"));
            Assert.That(wrongFragment.ErrorCode, Is.EqualTo("ASND0083"));
        });
    }

    private static BerStringCodec CreateCodec()
    {
        return new BerStringCodec(new AsnResourceLimits());
    }

    private static BerTlv MakeFragment(int tag, byte[] content)
    {
        return new BerTlv
        {
            Tag = new AsnTag { Number = tag },
            Content = content
        };
    }
}
