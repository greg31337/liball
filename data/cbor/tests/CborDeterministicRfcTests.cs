using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborDeterministicRfcTests
{
    [Test]
    public void RfcCoreDeterministicMapKeyExampleMatchesExactly()
    {
        CborValue map = RfcMapInShuffledOrder();
        CborEncodeOptions options = Options(
            CborEncodingProfile.CoreDeterministic);
        byte[] expected = Convert.FromHexString(
            "A8"
            + "0AF6"
            + "1864F6"
            + "20F6"
            + "617AF6"
            + "626161F6"
            + "811864F6"
            + "8120F6"
            + "F4F6");

        AssertDeterministicEncoding(map, options, expected);
    }

    [Test]
    public void RfcLengthFirstMapKeyExampleMatchesExactly()
    {
        CborValue map = RfcMapInShuffledOrder();
        CborEncodeOptions options = Options(
            CborEncodingProfile.LengthFirstCoreDeterministic);
        byte[] expected = Convert.FromHexString(
            "A8"
            + "0AF6"
            + "20F6"
            + "F4F6"
            + "1864F6"
            + "617AF6"
            + "8120F6"
            + "626161F6"
            + "811864F6");

        AssertDeterministicEncoding(map, options, expected);
    }

    [TestCase(CborEncodingProfile.CoreDeterministic)]
    [TestCase(CborEncodingProfile.LengthFirstCoreDeterministic)]
    public void RfcPreferredFloatsAndBignumsEncodeExactly(
        CborEncodingProfile profile)
    {
        CborValue value = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems =
            [
                NumericDouble(1.5),
                NumericDouble(1000000.5),
                RetainedFloat(CborFloatWidth.Double, 0x8000000000000000),
                RetainedFloat(CborFloatWidth.Double, 0x7ff0000000000000),
                RetainedFloat(CborFloatWidth.Double, 0xfff8123456789abc),
                Bignum(CborTagNumbers.PositiveBignum),
                Bignum(CborTagNumbers.NegativeBignum)
            ]
        };
        byte[] expected = Convert.FromHexString(
            "87"
            + "F93E00"
            + "FA49742408"
            + "F98000"
            + "F97C00"
            + "F97E00"
            + "C249010000000000000000"
            + "C349010000000000000000");

        AssertDeterministicEncoding(value, Options(profile), expected);
    }

    [Test]
    public void IndependentlyComputedCoreMixedKeyOrderMatchesExactly()
    {
        CborValue map = MixedKeyMapInShuffledOrder();
        byte[] expected = Convert.FromHexString(
            "A7"
            + "1864F6"
            + "626161F6"
            + "C249010000000000000000F6"
            + "C349010000000000000000F6"
            + "F4F6"
            + "F97E00F6"
            + "F98000F6");

        AssertDeterministicEncoding(
            map,
            Options(CborEncodingProfile.CoreDeterministic),
            expected);
    }

    [Test]
    public void IndependentlyComputedLengthFirstMixedKeyOrderMatchesExactly()
    {
        CborValue map = MixedKeyMapInShuffledOrder();
        byte[] expected = Convert.FromHexString(
            "A7"
            + "F4F6"
            + "1864F6"
            + "626161F6"
            + "F97E00F6"
            + "F98000F6"
            + "C249010000000000000000F6"
            + "C349010000000000000000F6");

        AssertDeterministicEncoding(
            map,
            Options(CborEncodingProfile.LengthFirstCoreDeterministic),
            expected);
    }

    [TestCase(CborEncodingProfile.CoreDeterministic)]
    [TestCase(CborEncodingProfile.LengthFirstCoreDeterministic)]
    public void EveryDuplicateClassFailsAtomically(
        CborEncodingProfile profile)
    {
        CborValue[][] keyPairs =
        [
            [Text("same"), Text("same")],
            [
                Bignum(CborTagNumbers.PositiveBignum),
                Bignum(CborTagNumbers.PositiveBignum)
            ],
            [
                RetainedFloat(CborFloatWidth.Single, 0x7fa12345),
                RetainedFloat(CborFloatWidth.Double, 0xfff8123456789abc)
            ],
            [
                RetainedFloat(CborFloatWidth.Half, 0x0000),
                RetainedFloat(CborFloatWidth.Double, 0x8000000000000000)
            ]
        ];
        CborEncodeOptions options = Options(profile);
        CborEncoder encoder = new CborEncoder();

        foreach (CborValue[] keys in keyPairs)
        {
            CborValue map = new CborValue
            {
                Kind = CborValueKind.Map,
                MapEntries =
                [
                    Pair(keys[0], Integer(CborValueKind.UnsignedInteger, 1)),
                    Pair(keys[1], Integer(CborValueKind.UnsignedInteger, 2))
                ]
            };
            byte[] destination = Enumerable.Repeat((byte)0xa5, 64).ToArray();
            byte[] original = destination.ToArray();

            CborWriteResult result = encoder.EncodeInto(map, destination, options);
            CborEncodeException? failure = Assert.Throws<CborEncodeException>(
                () => encoder.Measure(map, options));

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(CborWriteStatus.Error));
                Assert.That(
                    result.ErrorCode,
                    Is.EqualTo("CBOR_DETERMINISTIC_DUPLICATE_KEY"));
                Assert.That(result.ErrorPath, Is.EqualTo("${key:1}"));
                Assert.That(result.Written, Is.Zero);
                Assert.That(destination, Is.EqualTo(original));
                Assert.That(
                    failure?.ErrorCode,
                    Is.EqualTo("CBOR_DETERMINISTIC_DUPLICATE_KEY"));
            });
        }
    }

    private static void AssertDeterministicEncoding(
        CborValue value,
        CborEncodeOptions options,
        byte[] expected)
    {
        CborEncoder encoder = new CborEncoder();
        byte[] encoded = encoder.Encode(value, options);
        byte[] destination = Enumerable.Repeat((byte)0xa5, expected.Length).ToArray();
        CborWriteResult written = encoder.EncodeInto(value, destination, options);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(expected));
            Assert.That(encoder.Measure(value, options), Is.EqualTo(expected.Length));
            Assert.That(written.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(written.Written, Is.EqualTo(expected.Length));
            Assert.That(destination, Is.EqualTo(expected));
        });
    }

    private static CborValue RfcMapInShuffledOrder()
    {
        return new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries =
            [
                Pair(Simple(CborSimpleValues.False), Null()),
                Pair(Array(Integer(CborValueKind.NegativeInteger, -1)), Null()),
                Pair(Text("aa"), Null()),
                Pair(Integer(CborValueKind.UnsignedInteger, 100), Null()),
                Pair(Text("z"), Null()),
                Pair(Array(Integer(CborValueKind.UnsignedInteger, 100)), Null()),
                Pair(Integer(CborValueKind.NegativeInteger, -1), Null()),
                Pair(Integer(CborValueKind.UnsignedInteger, 10), Null())
            ]
        };
    }

    private static CborValue MixedKeyMapInShuffledOrder()
    {
        return new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries =
            [
                Pair(RetainedFloat(
                    CborFloatWidth.Double,
                    0x8000000000000000), Null()),
                Pair(Bignum(CborTagNumbers.NegativeBignum), Null()),
                Pair(Simple(CborSimpleValues.False), Null()),
                Pair(Text("aa"), Null()),
                Pair(RetainedFloat(
                    CborFloatWidth.Single,
                    0x7fa12345), Null()),
                Pair(Integer(CborValueKind.UnsignedInteger, 100), Null()),
                Pair(Bignum(CborTagNumbers.PositiveBignum), Null())
            ]
        };
    }

    private static CborEncodeOptions Options(CborEncodingProfile profile)
    {
        return new CborEncodeOptions
        {
            EncodingProfile = profile
        };
    }

    private static CborMapEntry Pair(CborValue key, CborValue value)
    {
        return new CborMapEntry
        {
            Key = key,
            Value = value
        };
    }

    private static CborValue Integer(CborValueKind kind, int value)
    {
        return new CborValue
        {
            Kind = kind,
            IntegerValue = new BigInteger(value)
        };
    }

    private static CborValue Text(string value)
    {
        return new CborValue
        {
            Kind = CborValueKind.TextString,
            TextStringValue = value
        };
    }

    private static CborValue Array(CborValue value)
    {
        return new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = [value]
        };
    }

    private static CborValue Bignum(ulong tagNumber)
    {
        return new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = tagNumber,
            TaggedValue = new CborValue
            {
                Kind = CborValueKind.ByteString,
                ByteStringValue = [1, 0, 0, 0, 0, 0, 0, 0, 0]
            }
        };
    }

    private static CborValue NumericDouble(double value)
    {
        return new CborValue
        {
            Kind = CborValueKind.Float,
            FloatWidth = CborFloatWidth.Double,
            FloatValue = new CborFloatValue
            {
                Value = value
            }
        };
    }

    private static CborValue RetainedFloat(CborFloatWidth width, ulong bits)
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

    private static CborValue Null()
    {
        return Simple(CborSimpleValues.Null);
    }
}
