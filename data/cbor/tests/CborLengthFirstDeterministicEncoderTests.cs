using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborLengthFirstDeterministicEncoderTests
{
    [Test]
    public void LengthFirstMapsSortByEncodedLengthThenLexically()
    {
        CborMapEntry arrayEntry = Pair(
            new CborValue { Kind = CborValueKind.Array, ArrayItems = [] },
            Null());
        CborMapEntry textEntry = Pair(Text("z"), Null());
        CborMapEntry negativeEntry = Pair(Integer(CborValueKind.NegativeInteger, -1), Null());
        CborMapEntry hundredEntry = Pair(Integer(CborValueKind.UnsignedInteger, 100), Null());
        CborMapEntry tenEntry = Pair(Integer(CborValueKind.UnsignedInteger, 10), Null());
        CborValue map = new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries =
            [
                textEntry,
                hundredEntry,
                arrayEntry,
                negativeEntry,
                tenEntry
            ]
        };
        CborEncodeOptions options = LengthFirstOptions();
        options.MapKeyOrder = CborMapKeyOrder.Lexicographic;
        CborEncoder encoder = new CborEncoder();

        byte[] encoded = encoder.Encode(map, options);

        Assert.Multiple(() =>
        {
            Assert.That(
                encoded,
                Is.EqualTo(Convert.FromHexString("A50AF620F680F61864F6617AF6")));
            Assert.That(encoder.Measure(map, options), Is.EqualTo(encoded.Length));
            Assert.That(map.MapEntries?[0], Is.SameAs(textEntry));
            Assert.That(map.MapEntries?[2], Is.SameAs(arrayEntry));
            Assert.That(map.MapEntries?[4], Is.SameAs(tenEntry));
        });
    }

    [Test]
    public void LengthFirstProfileAlsoAppliesAllCoreDeterministicScalarRules()
    {
        CborValue value = new CborValue
        {
            Kind = CborValueKind.Array,
            LengthKind = CborLengthKind.Indefinite,
            ArrayItems =
            [
                Integer(CborValueKind.UnsignedInteger, 24),
                RetainedFloat(CborFloatWidth.Double, 0x7ff8123456789abc)
            ]
        };
        CborEncodeOptions options = LengthFirstOptions();
        options.AllowIndefiniteLength = false;
        options.ReduceFloatWidth = false;
        options.PreserveRepresentation = true;

        byte[] encoded = new CborEncoder().Encode(value, options);

        Assert.That(encoded, Is.EqualTo(Convert.FromHexString("821818F97E00")));
    }

    [TestCase(CborEncodingProfile.CoreDeterministic)]
    [TestCase(CborEncodingProfile.LengthFirstCoreDeterministic)]
    public void DeterministicProfilesRejectRepeatedKeysAtomically(
        CborEncodingProfile profile)
    {
        CborValue map = new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries =
            [
                Pair(Text("a"), Integer(CborValueKind.UnsignedInteger, 1)),
                Pair(Text("a"), Integer(CborValueKind.UnsignedInteger, 2))
            ]
        };
        CborEncodeOptions options = new CborEncodeOptions
        {
            EncodingProfile = profile
        };
        CborEncoder encoder = new CborEncoder();
        byte[] destination = [0xa5, 0xa5, 0xa5, 0xa5, 0xa5, 0xa5, 0xa5];
        byte[] original = destination.ToArray();

        CborWriteResult result = encoder.EncodeInto(map, destination, options);
        CborEncodeException? exception = Assert.Throws<CborEncodeException>(
            () => encoder.Measure(map, options));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborWriteStatus.Error));
            Assert.That(result.ErrorCode, Is.EqualTo("CBOR_DETERMINISTIC_DUPLICATE_KEY"));
            Assert.That(result.ErrorPath, Is.EqualTo("${key:1}"));
            Assert.That(result.Written, Is.Zero);
            Assert.That(destination, Is.EqualTo(original));
            Assert.That(
                exception?.ErrorCode,
                Is.EqualTo("CBOR_DETERMINISTIC_DUPLICATE_KEY"));
        });
    }

    [Test]
    public void EquivalentSignedZeroKeysAreRejectedEvenWhenTheirBytesDiffer()
    {
        CborValue positiveZero = RetainedFloat(CborFloatWidth.Half, 0x0000);
        CborValue negativeZero = RetainedFloat(
            CborFloatWidth.Double,
            0x8000000000000000);
        CborValue map = new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries =
            [
                Pair(positiveZero, Null()),
                Pair(negativeZero, Null())
            ]
        };

        CborEncodeException? exception = Assert.Throws<CborEncodeException>(
            () => new CborEncoder().Encode(map, LengthFirstOptions()));

        Assert.Multiple(() =>
        {
            Assert.That(CborKeyComparer.AreEquivalent(positiveZero, negativeZero), Is.True);
            Assert.That(
                exception?.ErrorCode,
                Is.EqualTo("CBOR_DETERMINISTIC_DUPLICATE_KEY"));
            Assert.That(exception?.ErrorPath, Is.EqualTo("${key:1}"));
        });
    }

    [Test]
    public void DistinctNanPayloadKeysAreRejectedAfterCanonicalization()
    {
        CborValue firstNan = RetainedFloat(
            CborFloatWidth.Single,
            0x7fa12345);
        CborValue secondNan = RetainedFloat(
            CborFloatWidth.Double,
            0x7ff8123456789abc);
        CborValue map = new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries =
            [
                Pair(firstNan, Null()),
                Pair(secondNan, Null())
            ]
        };

        CborEncodeException? exception = Assert.Throws<CborEncodeException>(
            () => new CborEncoder().Encode(map, LengthFirstOptions()));

        Assert.Multiple(() =>
        {
            Assert.That(CborKeyComparer.AreEquivalent(firstNan, secondNan), Is.False);
            Assert.That(
                exception?.ErrorCode,
                Is.EqualTo("CBOR_DETERMINISTIC_DUPLICATE_KEY"));
        });
    }

    [Test]
    public void BasicEncodingContinuesToPreserveDuplicateMapKeys()
    {
        CborValue map = new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries =
            [
                Pair(Text("a"), Integer(CborValueKind.UnsignedInteger, 1)),
                Pair(Text("a"), Integer(CborValueKind.UnsignedInteger, 2))
            ]
        };

        byte[] encoded = new CborEncoder().Encode(map, new CborEncodeOptions());

        Assert.That(encoded, Is.EqualTo(Convert.FromHexString("A2616101616102")));
    }

    private static CborEncodeOptions LengthFirstOptions()
    {
        return new CborEncodeOptions
        {
            EncodingProfile = CborEncodingProfile.LengthFirstCoreDeterministic
        };
    }

    private static CborMapEntry Pair(CborValue key, CborValue value)
    {
        return new CborMapEntry { Key = key, Value = value };
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

    private static CborValue Null()
    {
        return new CborValue
        {
            Kind = CborValueKind.SimpleValue,
            SimpleValue = CborSimpleValues.Null
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
}
