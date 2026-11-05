using asn1;

namespace asn1.Tests;

public class BerPrimitiveCodecTests
{
    [Test]
    public void BooleanAndNullUseTheirRequiredPrimitiveContents()
    {
        BerPrimitiveCodec codec = CreateCodec();
        BerPrimitiveEncodeResult trueEncoding = codec.EncodePrimitive(
            AsnTypeKind.Boolean,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Boolean,
                HasBooleanValue = true,
                BooleanValue = true
            },
            AsnEncodingRules.Ber);
        BerPrimitiveDecodeResult berTrue = codec.DecodePrimitive(
            AsnTypeKind.Boolean,
            MakeTlv(AsnUniversalTags.Boolean, [0x01]),
            AsnEncodingRules.Ber);
        BerPrimitiveDecodeResult derTrue = codec.DecodePrimitive(
            AsnTypeKind.Boolean,
            MakeTlv(AsnUniversalTags.Boolean, [0x01]),
            AsnEncodingRules.Der);
        BerPrimitiveEncodeResult nullEncoding = codec.EncodePrimitive(
            AsnTypeKind.Null,
            new AsnRuntimeValue { Kind = AsnRuntimeValueKind.Null },
            AsnEncodingRules.Der);
        BerPrimitiveDecodeResult invalidNull = codec.DecodePrimitive(
            AsnTypeKind.Null,
            MakeTlv(AsnUniversalTags.Null, [0x00]),
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(trueEncoding.Success, Is.True);
            Assert.That(trueEncoding.Tlv?.Content,
                Is.EqualTo(new byte[] { 0xff }));
            Assert.That(berTrue.Value?.BooleanValue, Is.True);
            Assert.That(derTrue.Success, Is.False);
            Assert.That(derTrue.Diagnostics.Single().Code,
                Is.EqualTo("ASND0052"));
            Assert.That(nullEncoding.Success, Is.True);
            Assert.That(nullEncoding.Tlv?.Content, Is.Empty);
            Assert.That(invalidNull.Success, Is.False);
        });
    }

    [Test]
    public void IntegerAndEnumeratedUseMinimalTwosComplementContents()
    {
        BerPrimitiveCodec codec = CreateCodec();
        System.Numerics.BigInteger[] values =
        [
            new System.Numerics.BigInteger(0),
            new System.Numerics.BigInteger(127),
            new System.Numerics.BigInteger(128),
            new System.Numerics.BigInteger(-128),
            new System.Numerics.BigInteger(-129),
            System.Numerics.BigInteger.One << 200
        ];
        byte[][] expected =
        [
            [0x00],
            [0x7f],
            [0x00, 0x80],
            [0x80],
            [0xff, 0x7f],
            [0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]
        ];

        int index = 0;
        while (index < values.Length)
        {
            BerPrimitiveEncodeResult encoded = codec.EncodePrimitive(
                AsnTypeKind.Integer,
                new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.Integer,
                    HasIntegerValue = true,
                    IntegerValue = values[index]
                },
                AsnEncodingRules.Der);
            BerPrimitiveDecodeResult decoded = codec.DecodePrimitive(
                AsnTypeKind.Integer,
                encoded.Tlv ?? throw new InvalidOperationException(),
                AsnEncodingRules.Der);

            Assert.That(encoded.Tlv?.Content, Is.EqualTo(expected[index]));
            Assert.That(decoded.Value?.IntegerValue, Is.EqualTo(values[index]));
            index++;
        }

        BerPrimitiveDecodeResult enumerated = codec.DecodePrimitive(
            AsnTypeKind.Enumerated,
            MakeTlv(AsnUniversalTags.Enumerated, [0x05]),
            AsnEncodingRules.Ber);
        BerPrimitiveDecodeResult redundant = codec.DecodePrimitive(
            AsnTypeKind.Integer,
            MakeTlv(AsnUniversalTags.Integer, [0x00, 0x7f]),
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(enumerated.Value?.Kind,
                Is.EqualTo(AsnRuntimeValueKind.Enumerated));
            Assert.That(enumerated.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(5)));
            Assert.That(redundant.Success, Is.False);
            Assert.That(redundant.Diagnostics.Single().Code,
                Is.EqualTo("ASND0033"));
        });
    }

    [Test]
    public void IntegerLimitsMeasureValueBitsRatherThanSignOctets()
    {
        AsnResourceLimits limits = new AsnResourceLimits
        {
            MaximumIntegerBits = 8
        };
        BerPrimitiveCodec codec = new BerPrimitiveCodec(limits);
        BerPrimitiveEncodeResult encoded = codec.EncodePrimitive(
            AsnTypeKind.Integer,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = 128
            },
            AsnEncodingRules.Ber);
        BerPrimitiveDecodeResult decoded = codec.DecodePrimitive(
            AsnTypeKind.Integer,
            encoded.Tlv ?? throw new InvalidOperationException(),
            AsnEncodingRules.Ber);
        BerPrimitiveEncodeResult tooLarge = codec.EncodePrimitive(
            AsnTypeKind.Integer,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = 256
            },
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Tlv?.Content,
                Is.EqualTo(new byte[] { 0x00, 0x80 }));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(128)));
            Assert.That(tooLarge.Success, Is.False);
            Assert.That(tooLarge.Diagnostics.Single().Category,
                Is.EqualTo(AsnDiagnosticCategory.Resource));
        });
    }

    [Test]
    public void BitAndOctetStringsPreserveLengthsAndCopyPayloads()
    {
        BerPrimitiveCodec codec = CreateCodec();
        byte[] bitBytes = [0xaa, 0xc0];
        BerPrimitiveEncodeResult bitEncoding = codec.EncodePrimitive(
            AsnTypeKind.BitString,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.BitString,
                BitStringValue = new AsnBitStringValue
                {
                    Bytes = bitBytes,
                    BitLength = 10
                }
            },
            AsnEncodingRules.Ber);
        bitBytes[0] = 0;
        BerPrimitiveDecodeResult bitDecoded = codec.DecodePrimitive(
            AsnTypeKind.BitString,
            bitEncoding.Tlv ?? throw new InvalidOperationException(),
            AsnEncodingRules.Ber);
        BerPrimitiveDecodeResult dirtyUnused = codec.DecodePrimitive(
            AsnTypeKind.BitString,
            MakeTlv(AsnUniversalTags.BitString, [0x03, 0x07]),
            AsnEncodingRules.Ber);

        byte[] octets = [1, 2, 3];
        BerPrimitiveEncodeResult octetEncoding = codec.EncodePrimitive(
            AsnTypeKind.OctetString,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.OctetString,
                Octets = octets
            },
            AsnEncodingRules.Ber);
        octets[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(bitEncoding.Tlv?.Content,
                Is.EqualTo(new byte[] { 6, 0xaa, 0xc0 }));
            Assert.That(bitDecoded.Value?.BitStringValue?.BitLength,
                Is.EqualTo(10));
            Assert.That(bitDecoded.Value?.BitStringValue?.Bytes,
                Is.EqualTo(new byte[] { 0xaa, 0xc0 }));
            Assert.That(dirtyUnused.Success, Is.False);
            Assert.That(octetEncoding.Tlv?.Content,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void ObjectIdentifiersUseMinimalBase128Subidentifiers()
    {
        BerPrimitiveCodec codec = CreateCodec();
        AsnObjectIdentifierValue oid = new AsnObjectIdentifierValue
        {
            Arcs =
            [
                1,
                2,
                840,
                113549,
                System.Numerics.BigInteger.One << 100
            ]
        };
        BerPrimitiveEncodeResult encoded = codec.EncodePrimitive(
            AsnTypeKind.ObjectIdentifier,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.ObjectIdentifier,
                ObjectIdentifierValue = oid
            },
            AsnEncodingRules.Der);
        BerPrimitiveDecodeResult decoded = codec.DecodePrimitive(
            AsnTypeKind.ObjectIdentifier,
            encoded.Tlv ?? throw new InvalidOperationException(),
            AsnEncodingRules.Der);
        BerPrimitiveEncodeResult relative = codec.EncodePrimitive(
            AsnTypeKind.RelativeObjectIdentifier,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.RelativeObjectIdentifier,
                ObjectIdentifierValue = new AsnObjectIdentifierValue
                {
                    Relative = true,
                    Arcs = [8571, 3, 2]
                }
            },
            AsnEncodingRules.Ber);
        BerPrimitiveDecodeResult nonMinimal = codec.DecodePrimitive(
            AsnTypeKind.ObjectIdentifier,
            MakeTlv(AsnUniversalTags.ObjectIdentifier, [0x2a, 0x80, 0x01]),
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Tlv?.Content.Take(6),
                Is.EqualTo(new byte[] { 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d }));
            Assert.That(decoded.Value?.ObjectIdentifierValue?.Arcs,
                Is.EqualTo(oid.Arcs));
            Assert.That(relative.Tlv?.Content,
                Is.EqualTo(new byte[] { 0xc2, 0x7b, 0x03, 0x02 }));
            Assert.That(nonMinimal.Success, Is.False);
            Assert.That(nonMinimal.Diagnostics.Single().Code,
                Is.EqualTo("ASND0035"));
        });
    }

    [Test]
    public void AbsoluteObjectIdentifierLimitsCountExpandedFirstArcs()
    {
        BerPrimitiveCodec codec = new BerPrimitiveCodec(
            new AsnResourceLimits { MaximumCollectionElements = 1 });
        BerPrimitiveDecodeResult decoded = codec.DecodePrimitive(
            AsnTypeKind.ObjectIdentifier,
            MakeTlv(AsnUniversalTags.ObjectIdentifier, [0x2a]),
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.False);
            Assert.That(decoded.Diagnostics.Single().Code,
                Is.EqualTo("ASNR0011"));
            Assert.That(decoded.Diagnostics.Single().Category,
                Is.EqualTo(AsnDiagnosticCategory.Resource));
        });
    }

    [Test]
    public void RealSupportsSpecialDecimalAndBinaryForms()
    {
        BerPrimitiveCodec codec = CreateCodec();
        BerPrimitiveEncodeResult infinity = EncodeReal(
            codec,
            new AsnRealValue { Kind = AsnRealKind.PositiveInfinity });
        BerPrimitiveEncodeResult decimalValue = EncodeReal(
            codec,
            new AsnRealValue
            {
                Kind = AsnRealKind.FiniteDecimal,
                HasDecimalValue = true,
                DecimalText = "1200.0E-2"
            });
        BerPrimitiveEncodeResult binaryValue = EncodeReal(
            codec,
            new AsnRealValue
            {
                Kind = AsnRealKind.FiniteBinary,
                HasBinaryValue = true,
                Mantissa = -40,
                Radix = 2,
                Exponent = -3
            });
        BerPrimitiveDecodeResult binaryDecoded = codec.DecodePrimitive(
            AsnTypeKind.Real,
            binaryValue.Tlv ?? throw new InvalidOperationException(),
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(infinity.Tlv?.Content,
                Is.EqualTo(new byte[] { 0x40 }));
            Assert.That(decimalValue.Tlv?.Content,
                Is.EqualTo(new byte[]
                {
                    0x03, 0x31, 0x2e, 0x32, 0x45, 0x2b, 0x31
                }));
            Assert.That(binaryValue.Tlv?.Content,
                Is.EqualTo(new byte[] { 0xc0, 0x00, 0x05 }));
            Assert.That(binaryDecoded.Value?.RealValue?.Kind,
                Is.EqualTo(AsnRealKind.FiniteBinary));
            Assert.That(binaryDecoded.Value?.RealValue?.Mantissa,
                Is.EqualTo(new System.Numerics.BigInteger(-5)));
            Assert.That(binaryDecoded.Value?.RealValue?.Exponent,
                Is.EqualTo(System.Numerics.BigInteger.Zero));
        });
    }

    [Test]
    public void DerRealUsesTheCanonicalDecimalAndBinaryForms()
    {
        BerPrimitiveCodec codec = CreateCodec();
        AsnRuntimeValue decimalValue = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Real,
            RealValue = new AsnRealValue
            {
                Kind = AsnRealKind.FiniteDecimal,
                HasDecimalValue = true,
                DecimalText = "1200.0E-2"
            }
        };
        BerPrimitiveEncodeResult ber = codec.EncodePrimitive(
            AsnTypeKind.Real,
            decimalValue,
            AsnEncodingRules.Ber);
        BerPrimitiveEncodeResult der = codec.EncodePrimitive(
            AsnTypeKind.Real,
            decimalValue,
            AsnEncodingRules.Der);
        BerPrimitiveEncodeResult cer = codec.EncodePrimitive(
            AsnTypeKind.Real,
            decimalValue,
            AsnEncodingRules.Cer);
        BerPrimitiveEncodeResult binary = codec.EncodePrimitive(
            AsnTypeKind.Real,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Real,
                RealValue = new AsnRealValue
                {
                    Kind = AsnRealKind.FiniteBinary,
                    HasBinaryValue = true,
                    Mantissa = -40,
                    Radix = 8,
                    Exponent = -1
                }
            },
            AsnEncodingRules.Der);

        Assert.Multiple(() =>
        {
            Assert.That(ber.Tlv?.Content,
                Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("\u00031.2E+1")));
            Assert.That(der.Tlv?.Content,
                Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("\u000312.E+0")));
            Assert.That(cer.Tlv?.Content,
                Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("\u000312.E+0")));
            Assert.That(binary.Tlv?.Content,
                Is.EqualTo(new byte[] { 0xc0, 0x00, 0x05 }));
        });
    }

    [Test]
    public void EveryPrimitiveCharacterStringRoundTripsWithItsUniversalTag()
    {
        AsnTypeKind[] kinds =
        [
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
        string[] texts =
        [
            "Hi 😀",
            "12 3",
            "AZaz09 +/=?",
            "caf\u00e9",
            "video\u00e9",
            "line\n",
            "graphic\u00e9",
            "Visible 123",
            "general\u00e9",
            "A😀",
            "AΩ"
        ];
        int[] tags = [12, 18, 19, 20, 21, 22, 25, 26, 27, 28, 30];
        BerPrimitiveCodec codec = CreateCodec();

        int index = 0;
        while (index < kinds.Length)
        {
            BerPrimitiveEncodeResult encoded = codec.EncodePrimitive(
                kinds[index],
                new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.CharacterString,
                    TextValue = texts[index]
                },
                AsnEncodingRules.Ber);
            BerPrimitiveDecodeResult decoded = codec.DecodePrimitive(
                kinds[index],
                encoded.Tlv ?? throw new InvalidOperationException(),
                AsnEncodingRules.Ber);

            Assert.That(encoded.Tlv?.Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(tags[index])));
            Assert.That(decoded.Value?.TextValue, Is.EqualTo(texts[index]));
            index++;
        }
    }

    [Test]
    public void CharacterStringsRejectInvalidAlphabetsAndMalformedUnicode()
    {
        BerPrimitiveCodec codec = CreateCodec();
        BerPrimitiveEncodeResult numeric = codec.EncodePrimitive(
            AsnTypeKind.NumericString,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.CharacterString,
                TextValue = "12A"
            },
            AsnEncodingRules.Ber);
        BerPrimitiveDecodeResult utf8 = codec.DecodePrimitive(
            AsnTypeKind.Utf8String,
            MakeTlv(AsnUniversalTags.Utf8String, [0xc0, 0x80]),
            AsnEncodingRules.Ber);
        BerPrimitiveDecodeResult universal = codec.DecodePrimitive(
            AsnTypeKind.UniversalString,
            MakeTlv(AsnUniversalTags.UniversalString, [0x00, 0x01, 0x00]),
            AsnEncodingRules.Ber);
        BerPrimitiveDecodeResult bmp = codec.DecodePrimitive(
            AsnTypeKind.BmpString,
            MakeTlv(AsnUniversalTags.BmpString, [0xd8, 0x00]),
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(numeric.Success, Is.False);
            Assert.That(utf8.Success, Is.False);
            Assert.That(universal.Success, Is.False);
            Assert.That(bmp.Success, Is.False);
        });
    }

    [Test]
    public void UnicodeStringTypesUseTheirSpecifiedOctetEncodings()
    {
        BerPrimitiveCodec codec = CreateCodec();
        BerPrimitiveEncodeResult utf8 = codec.EncodePrimitive(
            AsnTypeKind.Utf8String,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.CharacterString,
                TextValue = "A😀"
            },
            AsnEncodingRules.Ber);
        BerPrimitiveEncodeResult universal = codec.EncodePrimitive(
            AsnTypeKind.UniversalString,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.CharacterString,
                TextValue = "A😀"
            },
            AsnEncodingRules.Ber);
        BerPrimitiveEncodeResult bmp = codec.EncodePrimitive(
            AsnTypeKind.BmpString,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.CharacterString,
                TextValue = "AΩ"
            },
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(utf8.Tlv?.Content,
                Is.EqualTo(new byte[] { 0x41, 0xf0, 0x9f, 0x98, 0x80 }));
            Assert.That(universal.Tlv?.Content,
                Is.EqualTo(new byte[]
                {
                    0x00, 0x00, 0x00, 0x41,
                    0x00, 0x01, 0xf6, 0x00
                }));
            Assert.That(bmp.Tlv?.Content,
                Is.EqualTo(new byte[] { 0x00, 0x41, 0x03, 0xa9 }));
        });
    }

    [Test]
    public void ObjectIdentifierIrisUseStrictUtf8AndIriSegmentRules()
    {
        BerPrimitiveCodec codec = CreateCodec();
        BerPrimitiveEncodeResult absolute = codec.EncodePrimitive(
            AsnTypeKind.ObjectIdentifierIri,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.ObjectIdentifierIri,
                TextValue = "/iso/member-body/840"
            },
            AsnEncodingRules.Ber);
        BerPrimitiveEncodeResult relative = codec.EncodePrimitive(
            AsnTypeKind.RelativeObjectIdentifierIri,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.RelativeObjectIdentifierIri,
                TextValue = "member-body/840"
            },
            AsnEncodingRules.Ber);
        BerPrimitiveDecodeResult absoluteDecoded = codec.DecodePrimitive(
            AsnTypeKind.ObjectIdentifierIri,
            absolute.Tlv ?? throw new InvalidOperationException(),
            AsnEncodingRules.Ber);
        BerPrimitiveEncodeResult invalid = codec.EncodePrimitive(
            AsnTypeKind.ObjectIdentifierIri,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.ObjectIdentifierIri,
                TextValue = "iso//member"
            },
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(absolute.Tlv?.Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(35)));
            Assert.That(relative.Tlv?.Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(36)));
            Assert.That(absoluteDecoded.Value?.TextValue,
                Is.EqualTo("/iso/member-body/840"));
            Assert.That(invalid.Success, Is.False);
        });
    }

    [Test]
    public void DecoderRejectsWrongTagsConstructedValuesAndResourceExcess()
    {
        BerPrimitiveCodec codec = CreateCodec();
        BerPrimitiveDecodeResult wrongTag = codec.DecodePrimitive(
            AsnTypeKind.Integer,
            MakeTlv(AsnUniversalTags.OctetString, [0x01]),
            AsnEncodingRules.Ber);
        BerTlv constructed = MakeTlv(AsnUniversalTags.Integer, []);
        constructed.Constructed = true;
        BerPrimitiveDecodeResult wrongForm = codec.DecodePrimitive(
            AsnTypeKind.Integer,
            constructed,
            AsnEncodingRules.Ber);
        BerPrimitiveCodec limited = new BerPrimitiveCodec(
            new AsnResourceLimits { MaximumOpenTypeBytes = 2 });
        BerPrimitiveDecodeResult tooLarge = limited.DecodePrimitive(
            AsnTypeKind.OctetString,
            MakeTlv(AsnUniversalTags.OctetString, [1, 2, 3]),
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(wrongTag.Diagnostics.Single().Code,
                Is.EqualTo("ASND0049"));
            Assert.That(wrongForm.Diagnostics.Single().Code,
                Is.EqualTo("ASND0050"));
            Assert.That(tooLarge.Diagnostics.Single().Category,
                Is.EqualTo(AsnDiagnosticCategory.Resource));
        });
    }

    private static BerPrimitiveCodec CreateCodec()
    {
        return new BerPrimitiveCodec(new AsnResourceLimits());
    }

    private static BerTlv MakeTlv(int tag, byte[] content)
    {
        return new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.Universal,
                Number = tag
            },
            Content = content
        };
    }

    private static BerPrimitiveEncodeResult EncodeReal(
        BerPrimitiveCodec codec,
        AsnRealValue value)
    {
        return codec.EncodePrimitive(
            AsnTypeKind.Real,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Real,
                RealValue = value
            },
            AsnEncodingRules.Ber);
    }
}
