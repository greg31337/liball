using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborCoreDeterministicEncoderTests
{
    [Test]
    public void CoreDeterministicMapsUseLexicalEncodedKeyOrderWithoutMutation()
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
                arrayEntry,
                textEntry,
                negativeEntry,
                hundredEntry,
                tenEntry
            ]
        };
        CborEncodeOptions options = DeterministicOptions();
        options.MapKeyOrder = CborMapKeyOrder.LengthFirst;
        CborEncoder encoder = new CborEncoder();

        byte[] encoded = encoder.Encode(map, options);

        Assert.Multiple(() =>
        {
            Assert.That(
                encoded,
                Is.EqualTo(Convert.FromHexString("A50AF61864F620F6617AF680F6")));
            Assert.That(encoder.Measure(map, options), Is.EqualTo(encoded.Length));
            Assert.That(map.MapEntries?[0], Is.SameAs(arrayEntry));
            Assert.That(map.MapEntries?[1], Is.SameAs(textEntry));
            Assert.That(map.MapEntries?[4], Is.SameAs(tenEntry));
        });
    }

    [Test]
    public void NestedMapKeysAreRecursivelyDeterministic()
    {
        CborValue mapKey = new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries =
            [
                Pair(Text("b"), Integer(CborValueKind.UnsignedInteger, 1)),
                Pair(Text("a"), Integer(CborValueKind.UnsignedInteger, 2))
            ]
        };
        CborValue root = new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries = [Pair(mapKey, Null())]
        };

        byte[] encoded = new CborEncoder().Encode(root, DeterministicOptions());

        Assert.That(
            encoded,
            Is.EqualTo(Convert.FromHexString("A1A2616102616201F6")));
    }

    [Test]
    public void CoreDeterministicEncodingForcesShortestHeadsFloatsAndDefiniteLengths()
    {
        CborValue value = new CborValue
        {
            Kind = CborValueKind.Array,
            LengthKind = CborLengthKind.Indefinite,
            ArrayItems =
            [
                Integer(CborValueKind.UnsignedInteger, 24),
                Integer(CborValueKind.NegativeInteger, -25),
                new CborValue
                {
                    Kind = CborValueKind.ByteString,
                    LengthKind = CborLengthKind.Indefinite,
                    ByteStringValue = new byte[24]
                },
                RetainedDouble(0x3ff8000000000000),
                RetainedDouble(0x8000000000000000),
                RetainedDouble(0x7ff0000000000000),
                RetainedDouble(0x7ff8123456789abc)
            ]
        };
        CborEncodeOptions options = DeterministicOptions();
        options.ReduceFloatWidth = false;
        options.PreserveRepresentation = true;
        CborEncoder encoder = new CborEncoder();
        string expected = "87181838185818"
            + new string('0', 48)
            + "F93E00F98000F97C00F97E00";

        byte[] encoded = encoder.Encode(value, options);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(Convert.FromHexString(expected)));
            Assert.That(encoder.Measure(value, options), Is.EqualTo(encoded.Length));
            Assert.That(Convert.ToHexString(encoded), Does.Not.Contain("9F"));
            Assert.That(Convert.ToHexString(encoded), Does.Not.Contain("5F"));
        });
    }

    [Test]
    public void EveryNanWidthAndPayloadUsesTheCanonicalHalfNan()
    {
        CborValue value = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems =
            [
                RetainedFloat(CborFloatWidth.Half, 0xfd55),
                RetainedFloat(CborFloatWidth.Single, 0x7fa12345),
                RetainedFloat(CborFloatWidth.Double, 0xfff8123456789abc)
            ]
        };

        byte[] encoded = new CborEncoder().Encode(value, DeterministicOptions());

        Assert.That(
            encoded,
            Is.EqualTo(Convert.FromHexString("83F97E00F97E00F97E00")));
    }

    [Test]
    public void CoreDeterministicFloatSelectionUsesHalfSingleAndDoubleAsNeeded()
    {
        CborValue value = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems =
            [
                RetainedDouble(0x3ff8000000000000),
                RetainedDouble(0x40f86a0000000000),
                RetainedDouble(0x3ff199999999999a)
            ]
        };

        byte[] encoded = new CborEncoder().Encode(value, DeterministicOptions());

        Assert.That(
            encoded,
            Is.EqualTo(Convert.FromHexString(
                "83F93E00FA47C35000FB3FF199999999999A")));
    }

    [Test]
    public void DeterministicEncodeIntoMeasuresBeforeTouchingTheDestination()
    {
        CborValue map = new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries =
            [
                Pair(Text("b"), Integer(CborValueKind.UnsignedInteger, 1)),
                Pair(Text("a"), Integer(CborValueKind.UnsignedInteger, 2))
            ]
        };
        CborEncoder encoder = new CborEncoder();
        CborEncodeOptions options = DeterministicOptions();
        int required = encoder.Measure(map, options);
        byte[] destination = Enumerable.Repeat((byte)0xa5, required - 1).ToArray();
        byte[] original = destination.ToArray();

        CborWriteResult result = encoder.EncodeInto(map, destination, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborWriteStatus.DestinationTooSmall));
            Assert.That(result.Required, Is.EqualTo(required));
            Assert.That(result.Written, Is.Zero);
            Assert.That(destination, Is.EqualTo(original));
        });
    }

    private static CborEncodeOptions DeterministicOptions()
    {
        return new CborEncodeOptions
        {
            EncodingProfile = CborEncodingProfile.CoreDeterministic
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

    private static CborValue RetainedDouble(ulong bits)
    {
        return RetainedFloat(CborFloatWidth.Double, bits);
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
