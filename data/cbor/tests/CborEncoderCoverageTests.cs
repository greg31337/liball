using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborEncoderCoverageTests
{
    [Test]
    public void EveryGenericValueKindEncodesMeasuresAndRoundTrips()
    {
        CborValue value = CreateEveryKindValue();
        byte[] expected = Convert.FromHexString(
            "89002042010262C2A281F5A1016176D903E8F6F93E00F8FF");
        CborEncoder encoder = new CborEncoder();
        CborEncodeOptions options = new CborEncodeOptions();

        byte[] encoded = encoder.Encode(value, options);
        byte[] destination = Enumerable.Repeat((byte)0xa5, expected.Length).ToArray();
        CborWriteResult written = encoder.EncodeInto(value, destination, options);
        CborReadResult decoded = new CborDecoder().Decode(
            encoded,
            new CborDecodeOptions
            {
                DuplicateKeyPolicy = CborDuplicateKeyPolicy.Preserve
            });

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(expected));
            Assert.That(encoder.Measure(value, options), Is.EqualTo(expected.Length));
            Assert.That(written.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(written.Written, Is.EqualTo(expected.Length));
            Assert.That(written.Required, Is.EqualTo(expected.Length));
            Assert.That(destination, Is.EqualTo(expected));
            Assert.That(decoded.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(decoded.Value?.ArrayItems, Has.Count.EqualTo(9));
            Assert.That(decoded.Value?.ArrayItems?[4].Kind, Is.EqualTo(CborValueKind.Array));
            Assert.That(decoded.Value?.ArrayItems?[5].Kind, Is.EqualTo(CborValueKind.Map));
            Assert.That(decoded.Value?.ArrayItems?[6].TagNumber, Is.EqualTo(1000));
            Assert.That(decoded.Value?.ArrayItems?[7].FloatWidth, Is.EqualTo(CborFloatWidth.Half));
        });
    }

    [Test]
    public void EveryIntegerHeadBoundaryEncodesAndMeasuresExactly()
    {
        string[][] unsignedCases =
        [
            ["0", "00"],
            ["23", "17"],
            ["24", "1818"],
            ["255", "18FF"],
            ["256", "190100"],
            ["65535", "19FFFF"],
            ["65536", "1A00010000"],
            ["4294967295", "1AFFFFFFFF"],
            ["4294967296", "1B0000000100000000"],
            ["18446744073709551615", "1BFFFFFFFFFFFFFFFF"]
        ];
        string[][] negativeCases =
        [
            ["-1", "20"],
            ["-24", "37"],
            ["-25", "3818"],
            ["-256", "38FF"],
            ["-257", "390100"],
            ["-65536", "39FFFF"],
            ["-65537", "3A00010000"],
            ["-4294967296", "3AFFFFFFFF"],
            ["-4294967297", "3B0000000100000000"],
            ["-18446744073709551616", "3BFFFFFFFFFFFFFFFF"]
        ];

        foreach (string[] item in unsignedCases)
        {
            AssertEncoding(
                Integer(CborValueKind.UnsignedInteger, BigInteger.Parse(item[0])),
                item[1]);
        }
        foreach (string[] item in negativeCases)
        {
            AssertEncoding(
                Integer(CborValueKind.NegativeInteger, BigInteger.Parse(item[0])),
                item[1]);
        }
    }

    [Test]
    public void EveryTagHeadBoundaryEncodesAndMeasuresExactly()
    {
        string[][] cases =
        [
            ["0", "C0F6"],
            ["23", "D7F6"],
            ["24", "D818F6"],
            ["255", "D8FFF6"],
            ["256", "D90100F6"],
            ["65535", "D9FFFFF6"],
            ["65536", "DA00010000F6"],
            ["4294967295", "DAFFFFFFFFF6"],
            ["4294967296", "DB0000000100000000F6"],
            ["18446744073709551615", "DBFFFFFFFFFFFFFFFFF6"]
        ];

        foreach (string[] item in cases)
        {
            CborValue value = new CborValue
            {
                Kind = CborValueKind.Tag,
                TagNumber = ulong.Parse(item[0]),
                TaggedValue = Simple(CborSimpleValues.Null)
            };
            byte[] expected = Convert.FromHexString(item[1]);
            CborEncoder encoder = new CborEncoder();
            CborEncodeOptions options = new CborEncodeOptions
            {
                SharedValuePolicy = CborSharedValuePolicy.Preserve
            };

            byte[] encoded = encoder.Encode(value, options);

            Assert.Multiple(() =>
            {
                Assert.That(encoded, Is.EqualTo(expected));
                Assert.That(
                    encoder.Measure(value, options),
                    Is.EqualTo(expected.Length));
            });
        }
    }

    [Test]
    public void EveryFloatWidthPreservesItsBasicProfileBits()
    {
        CborValue[] values =
        [
            Float(CborFloatWidth.Half, 0x8000),
            Float(CborFloatWidth.Single, 0x7fa12345),
            Float(CborFloatWidth.Double, 0xfff8123456789abc)
        ];
        string[] expected =
        [
            "F98000",
            "FA7FA12345",
            "FBFFF8123456789ABC"
        ];

        int index = 0;
        while (index < values.Length)
        {
            AssertEncoding(values[index], expected[index]);
            index = index + 1;
        }
    }

    [Test]
    public void EveryIndefiniteFormEncodesMeasuresAndHonorsThePolicy()
    {
        CborValue[] values =
        [
            new CborValue
            {
                Kind = CborValueKind.ByteString,
                LengthKind = CborLengthKind.Indefinite,
                ByteStringValue = []
            },
            new CborValue
            {
                Kind = CborValueKind.TextString,
                LengthKind = CborLengthKind.Indefinite,
                TextStringValue = "x"
            },
            new CborValue
            {
                Kind = CborValueKind.Array,
                LengthKind = CborLengthKind.Indefinite,
                ArrayItems = [Integer(CborValueKind.UnsignedInteger, BigInteger.Zero)]
            },
            new CborValue
            {
                Kind = CborValueKind.Map,
                LengthKind = CborLengthKind.Indefinite,
                MapEntries =
                [
                    new CborMapEntry
                    {
                        Key = Integer(CborValueKind.UnsignedInteger, BigInteger.Zero),
                        Value = Integer(CborValueKind.UnsignedInteger, BigInteger.One)
                    }
                ]
            }
        ];
        string[] expected = ["5FFF", "7F6178FF", "9F00FF", "BF0001FF"];
        CborEncoder encoder = new CborEncoder();

        int index = 0;
        while (index < values.Length)
        {
            AssertEncoding(values[index], expected[index]);
            byte[] destination = Enumerable.Repeat((byte)0xa5, 16).ToArray();
            byte[] original = destination.ToArray();
            CborWriteResult rejected = encoder.EncodeInto(
                values[index],
                destination,
                new CborEncodeOptions { AllowIndefiniteLength = false });

            Assert.Multiple(() =>
            {
                Assert.That(rejected.Status, Is.EqualTo(CborWriteStatus.Error));
                Assert.That(
                    rejected.ErrorCode,
                    Is.EqualTo("CBOR_INDEFINITE_LENGTH_DISABLED"));
                Assert.That(destination, Is.EqualTo(original));
            });
            index = index + 1;
        }
    }

    [Test]
    public void RetainedRepresentationsForEveryGenericKindRoundTripExactly()
    {
        byte[] source = Convert.FromHexString(
            "9B0000000000000009"
            + "1A00000000"
            + "3A00000000"
            + "590000"
            + "7A00000000"
            + "9800"
            + "B90000"
            + "DB0000000000000000F4"
            + "F98000"
            + "F8FF");
        CborReadResult decoded = new CborDecoder().Decode(
            source,
            new CborDecodeOptions
            {
                TagPolicy = CborTagPolicy.Preserve,
                RetainRepresentation = true,
                RetainSourceRanges = true
            });
        CborEncodeOptions options = new CborEncodeOptions
        {
            PreserveRepresentation = true
        };
        CborEncoder encoder = new CborEncoder();

        byte[] encoded = encoder.Encode(decoded.Value!, options);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(encoded, Is.EqualTo(source));
            Assert.That(encoder.Measure(decoded.Value!, options), Is.EqualTo(source.Length));
            Assert.That(decoded.Value?.ArrayItems, Has.Count.EqualTo(9));
        });
    }

    [Test]
    public void EveryShortDestinationIsAtomicAndExactOrLargerDestinationsSucceed()
    {
        CborValue value = CreateEveryKindValue();
        CborEncoder encoder = new CborEncoder();
        CborEncodeOptions options = new CborEncodeOptions();
        byte[] expected = encoder.Encode(value, options);

        int length = 0;
        while (length < expected.Length)
        {
            byte[] destination = Enumerable.Repeat((byte)0xa5, length).ToArray();
            byte[] original = destination.ToArray();

            CborWriteResult result = encoder.EncodeInto(value, destination, options);

            Assert.Multiple(() =>
            {
                Assert.That(
                    result.Status,
                    Is.EqualTo(CborWriteStatus.DestinationTooSmall));
                Assert.That(result.Written, Is.Zero);
                Assert.That(result.Required, Is.EqualTo(expected.Length));
                Assert.That(destination, Is.EqualTo(original));
            });
            length = length + 1;
        }

        byte[] exact = Enumerable.Repeat((byte)0xa5, expected.Length).ToArray();
        CborWriteResult exactResult = encoder.EncodeInto(value, exact, options);
        byte[] larger = Enumerable.Repeat((byte)0xa5, expected.Length + 3).ToArray();
        CborWriteResult largerResult = encoder.EncodeInto(value, larger, options);

        Assert.Multiple(() =>
        {
            Assert.That(exactResult.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(exact, Is.EqualTo(expected));
            Assert.That(largerResult.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(largerResult.Written, Is.EqualTo(expected.Length));
            Assert.That(larger.AsSpan(0, expected.Length).ToArray(), Is.EqualTo(expected));
            Assert.That(
                larger.AsSpan(expected.Length).ToArray(),
                Is.EqualTo(new byte[] { 0xa5, 0xa5, 0xa5 }));
        });
    }

    [Test]
    public void InvalidGenericValuesFailMeasurementAndLeaveDestinationsUntouched()
    {
        InvalidCase[] cases =
        [
            new InvalidCase(new CborValue(), "CBOR_VALUE_KIND_INVALID"),
            new InvalidCase(
                Integer(CborValueKind.UnsignedInteger, new BigInteger(-1)),
                "CBOR_UNSIGNED_INTEGER_OUT_OF_RANGE"),
            new InvalidCase(
                Integer(CborValueKind.NegativeInteger, BigInteger.Zero),
                "CBOR_NEGATIVE_INTEGER_OUT_OF_RANGE"),
            new InvalidCase(
                new CborValue { Kind = CborValueKind.ByteString },
                "CBOR_BYTE_STRING_VALUE_MISSING"),
            new InvalidCase(
                new CborValue { Kind = CborValueKind.TextString },
                "CBOR_TEXT_STRING_VALUE_MISSING"),
            new InvalidCase(
                new CborValue
                {
                    Kind = CborValueKind.TextString,
                    TextStringValue = "\ud800"
                },
                "CBOR_TEXT_STRING_INVALID_UTF16"),
            new InvalidCase(
                new CborValue { Kind = CborValueKind.Array },
                "CBOR_ARRAY_ITEMS_MISSING"),
            new InvalidCase(
                new CborValue { Kind = CborValueKind.Map },
                "CBOR_MAP_ENTRIES_MISSING"),
            new InvalidCase(
                new CborValue
                {
                    Kind = CborValueKind.Tag,
                    TagNumber = 1
                },
                "CBOR_TAGGED_VALUE_MISSING"),
            new InvalidCase(
                new CborValue { Kind = CborValueKind.Float },
                "CBOR_FLOAT_VALUE_MISSING"),
            new InvalidCase(
                Simple(24),
                "CBOR_SIMPLE_VALUE_RESERVED")
        ];
        CborEncoder encoder = new CborEncoder();
        CborEncodeOptions options = new CborEncodeOptions();

        foreach (InvalidCase item in cases)
        {
            byte[] destination = Enumerable.Repeat((byte)0xa5, 64).ToArray();
            byte[] original = destination.ToArray();

            CborWriteResult result = encoder.EncodeInto(
                item.Value,
                destination,
                options);
            CborEncodeException? failure = Assert.Throws<CborEncodeException>(
                () => encoder.Measure(item.Value, options));

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(CborWriteStatus.Error));
                Assert.That(result.ErrorCode, Is.EqualTo(item.ErrorCode));
                Assert.That(result.Written, Is.Zero);
                Assert.That(destination, Is.EqualTo(original));
                Assert.That(failure?.ErrorCode, Is.EqualTo(item.ErrorCode));
            });
        }
    }

    private static void AssertEncoding(CborValue value, string expectedHex)
    {
        byte[] expected = Convert.FromHexString(expectedHex);
        CborEncoder encoder = new CborEncoder();
        CborEncodeOptions options = new CborEncodeOptions();

        byte[] encoded = encoder.Encode(value, options);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(expected));
            Assert.That(encoder.Measure(value, options), Is.EqualTo(expected.Length));
        });
    }

    private static CborValue CreateEveryKindValue()
    {
        return new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems =
            [
                Integer(CborValueKind.UnsignedInteger, BigInteger.Zero),
                Integer(CborValueKind.NegativeInteger, new BigInteger(-1)),
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
                    Kind = CborValueKind.Array,
                    ArrayItems = [Simple(CborSimpleValues.True)]
                },
                new CborValue
                {
                    Kind = CborValueKind.Map,
                    MapEntries =
                    [
                        new CborMapEntry
                        {
                            Key = Integer(CborValueKind.UnsignedInteger, BigInteger.One),
                            Value = new CborValue
                            {
                                Kind = CborValueKind.TextString,
                                TextStringValue = "v"
                            }
                        }
                    ]
                },
                new CborValue
                {
                    Kind = CborValueKind.Tag,
                    TagNumber = 1000,
                    TaggedValue = Simple(CborSimpleValues.Null)
                },
                Float(CborFloatWidth.Half, 0x3e00),
                Simple(255)
            ]
        };
    }

    private static CborValue Integer(CborValueKind kind, BigInteger value)
    {
        return new CborValue
        {
            Kind = kind,
            IntegerValue = value
        };
    }

    private static CborValue Float(CborFloatWidth width, ulong bits)
    {
        CborFloatValue floatValue;
        if (width == CborFloatWidth.Half)
        {
            floatValue = CborHalfPrecision.CreateHalfFloat((ushort)bits);
        }
        else if (width == CborFloatWidth.Single)
        {
            floatValue = CborHalfPrecision.CreateSingleFloat((uint)bits);
        }
        else
        {
            floatValue = CborHalfPrecision.CreateDoubleFloat(bits);
        }
        return new CborValue
        {
            Kind = CborValueKind.Float,
            FloatWidth = width,
            FloatValue = floatValue
        };
    }

    private static CborValue Simple(byte value)
    {
        return new CborValue
        {
            Kind = CborValueKind.SimpleValue,
            SimpleValue = value
        };
    }

    private class InvalidCase
    {
        public CborValue Value;
        public string ErrorCode;

        public InvalidCase(CborValue value, string errorCode)
        {
            Value = value;
            ErrorCode = errorCode;
        }
    }
}
