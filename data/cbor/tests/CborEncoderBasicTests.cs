using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborEncoderBasicTests
{
    [Test]
    public void EveryScalarKindEncodesAndMeasuresItsExactOutput()
    {
        CborValue[] values =
        [
            new CborValue
            {
                Kind = CborValueKind.UnsignedInteger,
                IntegerValue = new BigInteger(1000)
            },
            new CborValue
            {
                Kind = CborValueKind.NegativeInteger,
                IntegerValue = new BigInteger(-1000)
            },
            new CborValue
            {
                Kind = CborValueKind.ByteString,
                ByteStringValue = [1, 2]
            },
            new CborValue
            {
                Kind = CborValueKind.TextString,
                TextStringValue = "¢"
            },
            new CborValue
            {
                Kind = CborValueKind.Float,
                FloatWidth = CborFloatWidth.Half,
                FloatValue = new CborFloatValue
                {
                    Width = CborFloatWidth.Half,
                    Value = 1.5,
                    RawBits = 0x3e00
                }
            },
            new CborValue
            {
                Kind = CborValueKind.SimpleValue,
                SimpleValue = 255
            }
        ];
        string[] expected =
        [
            "1903E8",
            "3903E7",
            "420102",
            "62C2A2",
            "F93E00",
            "F8FF"
        ];
        CborEncoder encoder = new CborEncoder();
        CborEncodeOptions options = new CborEncodeOptions();

        int index = 0;
        while (index < values.Length)
        {
            byte[] encoded = encoder.Encode(values[index], options);

            Assert.Multiple(() =>
            {
                Assert.That(encoded, Is.EqualTo(Convert.FromHexString(expected[index])));
                Assert.That(encoder.Measure(values[index], options), Is.EqualTo(encoded.Length));
            });
            index = index + 1;
        }
    }

    [Test]
    public void ArbitraryMapKeysAndNestedTagsRoundTripInInsertionOrder()
    {
        CborValue key = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems =
            [
                new CborValue
                {
                    Kind = CborValueKind.UnsignedInteger,
                    IntegerValue = BigInteger.One
                },
                new CborValue
                {
                    Kind = CborValueKind.TextString,
                    TextStringValue = "k"
                }
            ]
        };
        CborValue tagged = new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = 1000,
            TaggedValue = new CborValue
            {
                Kind = CborValueKind.Tag,
                TagNumber = CborTagNumbers.EncodedCbor,
                TaggedValue = new CborValue
                {
                    Kind = CborValueKind.ByteString,
                    ByteStringValue = [0xf6]
                }
            }
        };
        CborValue map = new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries =
            [
                new CborMapEntry { Key = key, Value = tagged },
                new CborMapEntry
                {
                    Key = new CborValue
                    {
                        Kind = CborValueKind.SimpleValue,
                        SimpleValue = CborSimpleValues.False
                    },
                    Value = new CborValue
                    {
                        Kind = CborValueKind.SimpleValue,
                        SimpleValue = 255
                    }
                }
            ]
        };
        CborEncoder encoder = new CborEncoder();

        byte[] encoded = encoder.Encode(map, new CborEncodeOptions());
        CborReadResult decoded = new CborDecoder().Decode(encoded, new CborDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(
                encoded,
                Is.EqualTo(Convert.FromHexString("A28201616BD903E8D81841F6F4F8FF")));
            Assert.That(decoded.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(decoded.Value?.MapEntries?[0].Key?.Kind, Is.EqualTo(CborValueKind.Array));
            Assert.That(decoded.Value?.MapEntries?[0].Value?.TagNumber, Is.EqualTo(1000));
            Assert.That(
                decoded.Value?.MapEntries?[0].Value?.TaggedValue?.TagNumber,
                Is.EqualTo(CborTagNumbers.EncodedCbor));
            Assert.That(decoded.Value?.MapEntries?[1].Value?.SimpleValue, Is.EqualTo(255));
        });
    }

    [Test]
    public void IndefiniteDomValuesUseLegalHeadsChunksAndBreaks()
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
        CborEncodeOptions options = new CborEncodeOptions();

        byte[] encoded = encoder.Encode(value, options);

        Assert.Multiple(() =>
        {
            Assert.That(
                encoded,
                Is.EqualTo(Convert.FromHexString("9F5F420102FF7F62C2A2FFBFFFFF")));
            Assert.That(encoder.Measure(value, options), Is.EqualTo(encoded.Length));
            Assert.That(
                new CborDecoder().Decode(encoded, new CborDecodeOptions()).Status,
                Is.EqualTo(CborReadStatus.Done));
        });
    }

    [Test]
    public void EncodeIntoIsAtomicForShortDestinationsAndInvalidGraphs()
    {
        CborEncoder encoder = new CborEncoder();
        CborEncodeOptions options = new CborEncodeOptions();
        CborValue valid = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems =
            [
                new CborValue
                {
                    Kind = CborValueKind.UnsignedInteger,
                    IntegerValue = new BigInteger(24)
                }
            ]
        };
        byte[] tooSmallDestination = [0xa5, 0xa5];
        CborWriteResult tooSmall = encoder.EncodeInto(valid, tooSmallDestination, options);
        byte[] roomyDestination = [0xa5, 0xa5, 0xa5, 0xa5];
        CborWriteResult written = encoder.EncodeInto(valid, roomyDestination, options);
        CborValue invalid = new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries = [new CborMapEntry()]
        };
        byte[] invalidDestination = [0xa5, 0xa5, 0xa5, 0xa5];
        CborWriteResult invalidResult = encoder.EncodeInto(invalid, invalidDestination, options);

        Assert.Multiple(() =>
        {
            Assert.That(tooSmall.Status, Is.EqualTo(CborWriteStatus.DestinationTooSmall));
            Assert.That(tooSmall.Required, Is.EqualTo(3));
            Assert.That(tooSmall.Written, Is.Zero);
            Assert.That(tooSmallDestination, Is.EqualTo(new byte[] { 0xa5, 0xa5 }));
            Assert.That(written.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(written.Written, Is.EqualTo(3));
            Assert.That(roomyDestination, Is.EqualTo(new byte[] { 0x81, 0x18, 0x18, 0xa5 }));
            Assert.That(invalidResult.Status, Is.EqualTo(CborWriteStatus.Error));
            Assert.That(invalidResult.ErrorCode, Is.EqualTo("CBOR_VALUE_MISSING"));
            Assert.That(invalidResult.ErrorPath, Is.EqualTo("${key:0}"));
            Assert.That(invalidDestination, Is.EqualTo(new byte[] { 0xa5, 0xa5, 0xa5, 0xa5 }));
        });
    }

    [Test]
    public void InvalidValuesAndCyclesProduceStructuredEncodeFailures()
    {
        CborEncoder encoder = new CborEncoder();
        CborValue reservedSimple = new CborValue
        {
            Kind = CborValueKind.SimpleValue,
            SimpleValue = 24
        };
        CborValue cycle = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = []
        };
        cycle.ArrayItems.Add(cycle);

        CborEncodeException? simpleFailure = Assert.Throws<CborEncodeException>(
            () => encoder.Measure(reservedSimple, new CborEncodeOptions()));
        CborEncodeException? cycleFailure = Assert.Throws<CborEncodeException>(
            () => encoder.Encode(cycle, new CborEncodeOptions()));

        Assert.Multiple(() =>
        {
            Assert.That(simpleFailure?.ErrorCode, Is.EqualTo("CBOR_SIMPLE_VALUE_RESERVED"));
            Assert.That(simpleFailure?.ErrorPath, Is.EqualTo("$"));
            Assert.That(cycleFailure?.ErrorCode, Is.EqualTo("CBOR_CYCLIC_VALUE_UNSUPPORTED"));
            Assert.That(cycleFailure?.ErrorPath, Is.EqualTo("$[0]"));
        });
    }

    [Test]
    public void EncodingLimitsAndIndefinitePolicyApplyDuringMeasurement()
    {
        CborEncoder encoder = new CborEncoder();
        CborValue indefinite = new CborValue
        {
            Kind = CborValueKind.Array,
            LengthKind = CborLengthKind.Indefinite,
            ArrayItems = []
        };
        CborValue nested = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems =
            [
                new CborValue
                {
                    Kind = CborValueKind.Array,
                    ArrayItems = []
                }
            ]
        };

        CborEncodeException? indefiniteFailure = Assert.Throws<CborEncodeException>(
            () => encoder.Measure(
                indefinite,
                new CborEncodeOptions { AllowIndefiniteLength = false }));
        CborEncodeException? depthFailure = Assert.Throws<CborEncodeException>(
            () => encoder.Measure(
                nested,
                new CborEncodeOptions { MaxNestingDepth = 1 }));
        CborWriteResult byteFailure = encoder.EncodeInto(
            new CborValue
            {
                Kind = CborValueKind.TextString,
                TextStringValue = "ab"
            },
            new byte[3],
            new CborEncodeOptions { MaxOutputBytes = 2 });

        Assert.Multiple(() =>
        {
            Assert.That(
                indefiniteFailure?.ErrorCode,
                Is.EqualTo("CBOR_INDEFINITE_LENGTH_DISABLED"));
            Assert.That(depthFailure?.ErrorCode, Is.EqualTo("CBOR_MAX_NESTING_DEPTH"));
            Assert.That(depthFailure?.ErrorPath, Is.EqualTo("$[0]"));
            Assert.That(byteFailure.ErrorCode, Is.EqualTo("CBOR_MAX_OUTPUT_BYTES"));
            Assert.That(byteFailure.Written, Is.Zero);
        });
    }
}
