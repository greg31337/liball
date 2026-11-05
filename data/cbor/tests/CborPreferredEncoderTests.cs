using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborPreferredEncoderTests
{
    [Test]
    public void PreferredIntegersAndLengthsUseTheirShortestHeads()
    {
        CborValue[] values =
        [
            Integer(CborValueKind.UnsignedInteger, 23),
            Integer(CborValueKind.UnsignedInteger, 24),
            Integer(CborValueKind.UnsignedInteger, 255),
            Integer(CborValueKind.UnsignedInteger, 256),
            Integer(CborValueKind.NegativeInteger, -24),
            Integer(CborValueKind.NegativeInteger, -25),
            new CborValue
            {
                Kind = CborValueKind.ByteString,
                ByteStringValue = new byte[23]
            },
            new CborValue
            {
                Kind = CborValueKind.ByteString,
                ByteStringValue = new byte[24]
            },
            new CborValue
            {
                Kind = CborValueKind.Array,
                ArrayItems = CreateNullItems(24)
            }
        ];
        string[] prefixes =
        [
            "17", "1818", "18FF", "190100", "37", "3818", "57", "5818", "9818"
        ];
        CborEncoder encoder = new CborEncoder();
        CborEncodeOptions options = new CborEncodeOptions
        {
            EncodingProfile = CborEncodingProfile.Preferred
        };

        int index = 0;
        while (index < values.Length)
        {
            byte[] encoded = encoder.Encode(values[index], options);

            Assert.Multiple(() =>
            {
                Assert.That(Convert.ToHexString(encoded), Does.StartWith(prefixes[index]));
                Assert.That(encoder.Measure(values[index], options), Is.EqualTo(encoded.Length));
            });
            index = index + 1;
        }
    }

    [Test]
    public void PreferredFloatsReduceOnlyToAnExactlyRepresentableWidth()
    {
        CborValue[] values =
        [
            RetainedDouble(0x3ff8000000000000),
            RetainedDouble(0x40f86a0000000000),
            RetainedDouble(0x3ff199999999999a)
        ];
        string[] expected =
        [
            "F93E00",
            "FA47C35000",
            "FB3FF199999999999A"
        ];
        CborEncoder encoder = new CborEncoder();
        CborEncodeOptions options = new CborEncodeOptions
        {
            EncodingProfile = CborEncodingProfile.Preferred
        };

        int index = 0;
        while (index < values.Length)
        {
            byte[] encoded = encoder.Encode(values[index], options);
            Assert.That(encoded, Is.EqualTo(Convert.FromHexString(expected[index])));
            Assert.That(encoder.Measure(values[index], options), Is.EqualTo(encoded.Length));
            index = index + 1;
        }
    }

    [Test]
    public void PreferredKnownLengthsUseDefiniteEncoding()
    {
        CborValue value = new CborValue
        {
            Kind = CborValueKind.Array,
            LengthKind = CborLengthKind.Indefinite,
            ArrayItems =
            [
                new CborValue
                {
                    Kind = CborValueKind.ByteString,
                    LengthKind = CborLengthKind.Indefinite,
                    ByteStringValue = [1, 2]
                },
                new CborValue
                {
                    Kind = CborValueKind.TextString,
                    LengthKind = CborLengthKind.Indefinite,
                    TextStringValue = "¢"
                },
                new CborValue
                {
                    Kind = CborValueKind.Map,
                    LengthKind = CborLengthKind.Indefinite,
                    MapEntries = []
                }
            ]
        };
        CborEncoder encoder = new CborEncoder();
        CborEncodeOptions options = new CborEncodeOptions
        {
            EncodingProfile = CborEncodingProfile.Preferred,
            AllowIndefiniteLength = false
        };

        byte[] encoded = encoder.Encode(value, options);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(Convert.FromHexString("8342010262C2A2A0")));
            Assert.That(encoder.Measure(value, options), Is.EqualTo(encoded.Length));
        });
    }

    [Test]
    public void PreferredReductionPreservesSignedZeroAndInfinities()
    {
        CborValue[] values =
        [
            RetainedDouble(0x8000000000000000),
            RetainedDouble(0x7ff0000000000000),
            RetainedDouble(0xfff0000000000000)
        ];
        string[] expected = ["F98000", "F97C00", "F9FC00"];
        CborEncoder encoder = new CborEncoder();
        CborEncodeOptions options = new CborEncodeOptions
        {
            EncodingProfile = CborEncodingProfile.Preferred
        };

        int index = 0;
        while (index < values.Length)
        {
            byte[] encoded = encoder.Encode(values[index], options);
            CborReadResult decoded = new CborDecoder().Decode(
                encoded,
                new CborDecodeOptions());

            Assert.That(encoded, Is.EqualTo(Convert.FromHexString(expected[index])));
            Assert.That(decoded.Status, Is.EqualTo(CborReadStatus.Done));
            if (index == 0)
            {
                Assert.That(
                    BitConverter.DoubleToUInt64Bits(decoded.Value?.FloatValue?.Value ?? 0),
                    Is.EqualTo(0x8000000000000000UL));
            }
            index = index + 1;
        }
    }

    [Test]
    public void PreferredReductionRetainsNanWidthPayloadAndSignalingState()
    {
        CborValue quietDouble = RetainedDouble(0x7ff8123456789abc);
        CborValue signalingSingle = new CborValue
        {
            Kind = CborValueKind.Float,
            FloatWidth = CborFloatWidth.Single,
            FloatValue = new CborFloatValue
            {
                Width = CborFloatWidth.Single,
                Value = double.NaN,
                RawBits = 0x7fa12345,
                IsNaN = true,
                IsSignalingNaN = true
            }
        };
        CborEncoder encoder = new CborEncoder();
        CborEncodeOptions options = new CborEncodeOptions
        {
            EncodingProfile = CborEncodingProfile.Preferred
        };

        byte[] quietEncoded = encoder.Encode(quietDouble, options);
        byte[] signalingEncoded = encoder.Encode(signalingSingle, options);
        CborReadResult quietDecoded = new CborDecoder().Decode(
            quietEncoded,
            new CborDecodeOptions());
        CborReadResult signalingDecoded = new CborDecoder().Decode(
            signalingEncoded,
            new CborDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(
                quietEncoded,
                Is.EqualTo(Convert.FromHexString("FB7FF8123456789ABC")));
            Assert.That(
                signalingEncoded,
                Is.EqualTo(Convert.FromHexString("FA7FA12345")));
            Assert.That(quietDecoded.Value?.FloatValue?.IsNaN, Is.True);
            Assert.That(quietDecoded.Value?.FloatValue?.RawBits, Is.EqualTo(0x7ff8123456789abc));
            Assert.That(signalingDecoded.Value?.FloatValue?.IsSignalingNaN, Is.True);
            Assert.That(signalingDecoded.Value?.FloatValue?.RawBits, Is.EqualTo(0x7fa12345));
        });
    }

    [Test]
    public void PreferredReductionNarrowsExactlyReconstructableNanPayloads()
    {
        CborValue quietDouble = RetainedDouble(0x7ff8000000000000);
        CborValue signalingSingle = new CborValue
        {
            Kind = CborValueKind.Float,
            FloatWidth = CborFloatWidth.Single,
            FloatValue = new CborFloatValue
            {
                Width = CborFloatWidth.Single,
                RawBits = 0xffa00000,
                Value = BitConverter.UInt32BitsToSingle(0xffa00000),
                IsNaN = true,
                IsSignalingNaN = true
            }
        };
        CborEncoder encoder = new CborEncoder();
        CborEncodeOptions options = new CborEncodeOptions
        {
            EncodingProfile = CborEncodingProfile.Preferred
        };

        byte[] quietEncoded = encoder.Encode(quietDouble, options);
        byte[] signalingEncoded = encoder.Encode(signalingSingle, options);

        Assert.Multiple(() =>
        {
            Assert.That(quietEncoded, Is.EqualTo(Convert.FromHexString("F97E00")));
            Assert.That(signalingEncoded, Is.EqualTo(Convert.FromHexString("F9FD00")));
            Assert.That(encoder.Measure(quietDouble, options), Is.EqualTo(3));
            Assert.That(encoder.Measure(signalingSingle, options), Is.EqualTo(3));
        });
    }

    [Test]
    public void FloatReductionCanBeDisabledAndDoesNotApplyToBasicEncoding()
    {
        CborValue value = RetainedDouble(0x3ff8000000000000);
        CborEncoder encoder = new CborEncoder();
        CborEncodeOptions disabled = new CborEncodeOptions
        {
            EncodingProfile = CborEncodingProfile.Preferred,
            ReduceFloatWidth = false
        };

        byte[] preferredWithoutReduction = encoder.Encode(value, disabled);
        byte[] basic = encoder.Encode(value, new CborEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(
                preferredWithoutReduction,
                Is.EqualTo(Convert.FromHexString("FB3FF8000000000000")));
            Assert.That(basic, Is.EqualTo(preferredWithoutReduction));
        });
    }

    [Test]
    public void ExplicitSingleWidthNeverWidensAnInexactNumericValue()
    {
        CborValue value = new CborValue
        {
            Kind = CborValueKind.Float,
            FloatWidth = CborFloatWidth.Single,
            FloatValue = new CborFloatValue
            {
                Value = 1.1
            }
        };
        CborEncoder encoder = new CborEncoder();
        CborEncodeOptions options = new CborEncodeOptions
        {
            EncodingProfile = CborEncodingProfile.Preferred
        };

        byte[] encoded = encoder.Encode(value, options);

        Assert.That(encoded, Is.EqualTo(Convert.FromHexString("FA3F8CCCCD")));
    }

    private static CborValue Integer(CborValueKind kind, int value)
    {
        return new CborValue
        {
            Kind = kind,
            IntegerValue = new BigInteger(value)
        };
    }

    private static List<CborValue> CreateNullItems(int count)
    {
        List<CborValue> items = [];
        int index = 0;
        while (index < count)
        {
            items.Add(new CborValue
            {
                Kind = CborValueKind.SimpleValue,
                SimpleValue = CborSimpleValues.Null
            });
            index = index + 1;
        }
        return items;
    }

    private static CborValue RetainedDouble(ulong bits)
    {
        return new CborValue
        {
            Kind = CborValueKind.Float,
            FloatWidth = CborFloatWidth.Double,
            FloatValue = new CborFloatValue
            {
                Width = CborFloatWidth.Double,
                Value = BitConverter.UInt64BitsToDouble(bits),
                RawBits = bits,
                IsNaN = double.IsNaN(BitConverter.UInt64BitsToDouble(bits))
            }
        };
    }
}
