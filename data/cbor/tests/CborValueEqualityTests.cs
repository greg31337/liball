using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborValueEqualityTests
{
    [Test]
    public void ScalarEqualityUsesGenericDataModelGroupsAndIgnoresRepresentation()
    {
        CborValue firstInteger = new CborValue
        {
            Kind = CborValueKind.UnsignedInteger,
            IntegerValue = new BigInteger(42),
            Representation = new CborRepresentation { HeadArgumentWidth = 1 }
        };
        CborValue secondInteger = new CborValue
        {
            Kind = CborValueKind.UnsignedInteger,
            IntegerValue = new BigInteger(42),
            Representation = new CborRepresentation { HeadArgumentWidth = 8 }
        };
        CborValue negativeInteger = new CborValue
        {
            Kind = CborValueKind.NegativeInteger,
            IntegerValue = new BigInteger(42)
        };
        CborValue bytes = new CborValue
        {
            Kind = CborValueKind.ByteString,
            ByteStringValue = [0x61]
        };
        CborValue text = new CborValue
        {
            Kind = CborValueKind.TextString,
            TextStringValue = "a"
        };

        Assert.Multiple(() =>
        {
            Assert.That(CborValueComparer.AreEqual(firstInteger, secondInteger), Is.True);
            Assert.That(CborKeyComparer.GetEquivalentHash(firstInteger),
                Is.EqualTo(CborKeyComparer.GetEquivalentHash(secondInteger)));
            Assert.That(CborValueComparer.AreEqual(firstInteger, negativeInteger), Is.False);
            Assert.That(CborValueComparer.AreEqual(bytes, text), Is.False);
            Assert.That(CborValueComparer.AreEqual(
                new CborValue { Kind = CborValueKind.SimpleValue, SimpleValue = 16 },
                new CborValue { Kind = CborValueKind.SimpleValue, SimpleValue = 16 }), Is.True);
        });
    }

    [Test]
    public void FloatKeysUseNumericEqualityAndNormalizedNanSignificands()
    {
        CborValue halfValue = WrapFloat(CborHalfPrecision.CreateHalfFloat(0x3e00));
        CborValue doubleValue = WrapFloat(CborHalfPrecision.CreateDoubleFloat(0x3ff8000000000000));
        CborValue positiveZero = WrapFloat(CborHalfPrecision.CreateHalfFloat(0x0000));
        CborValue negativeZero = WrapFloat(CborHalfPrecision.CreateDoubleFloat(0x8000000000000000));
        CborValue halfNan = WrapFloat(CborHalfPrecision.CreateHalfFloat(0x7e00));
        CborValue singleNan = WrapFloat(CborHalfPrecision.CreateSingleFloat(0x7fc00000));
        CborValue doubleNan = WrapFloat(CborHalfPrecision.CreateDoubleFloat(0x7ff8000000000000));
        CborValue differentNan = WrapFloat(CborHalfPrecision.CreateHalfFloat(0x7e01));
        CborValue integer = new CborValue
        {
            Kind = CborValueKind.UnsignedInteger,
            IntegerValue = BigInteger.One
        };

        Assert.Multiple(() =>
        {
            Assert.That(CborKeyComparer.AreEquivalent(halfValue, doubleValue), Is.True);
            Assert.That(CborKeyComparer.GetEquivalentHash(halfValue),
                Is.EqualTo(CborKeyComparer.GetEquivalentHash(doubleValue)));
            Assert.That(CborKeyComparer.AreEquivalent(positiveZero, negativeZero), Is.True);
            Assert.That(CborKeyComparer.GetEquivalentHash(positiveZero),
                Is.EqualTo(CborKeyComparer.GetEquivalentHash(negativeZero)));
            Assert.That(CborKeyComparer.AreEquivalent(halfNan, singleNan), Is.True);
            Assert.That(CborKeyComparer.AreEquivalent(singleNan, doubleNan), Is.True);
            Assert.That(CborKeyComparer.GetEquivalentHash(halfNan),
                Is.EqualTo(CborKeyComparer.GetEquivalentHash(doubleNan)));
            Assert.That(CborKeyComparer.AreEquivalent(halfNan, differentNan), Is.False);
            Assert.That(CborKeyComparer.AreEquivalent(doubleValue, integer), Is.False);
        });
    }

    [Test]
    public void ArraysTagsAndByteStringsCompareStructurally()
    {
        CborValue left = new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = 1000,
            TaggedValue = new CborValue
            {
                Kind = CborValueKind.Array,
                ArrayItems =
                [
                    new CborValue { Kind = CborValueKind.ByteString, ByteStringValue = [1, 2, 3] },
                    new CborValue { Kind = CborValueKind.TextString, TextStringValue = "value" }
                ]
            }
        };
        CborValue right = new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = 1000,
            TaggedValue = new CborValue
            {
                Kind = CborValueKind.Array,
                ArrayItems =
                [
                    new CborValue { Kind = CborValueKind.ByteString, ByteStringValue = [1, 2, 3] },
                    new CborValue { Kind = CborValueKind.TextString, TextStringValue = "value" }
                ]
            }
        };
        CborValue differentTag = new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = 1001,
            TaggedValue = right.TaggedValue
        };

        Assert.Multiple(() =>
        {
            Assert.That(CborValueComparer.AreEqual(left, right), Is.True);
            Assert.That(CborKeyComparer.GetEquivalentHash(left),
                Is.EqualTo(CborKeyComparer.GetEquivalentHash(right)));
            Assert.That(CborValueComparer.AreEqual(left, differentTag), Is.False);
        });
    }

    [Test]
    public void MapsCompareAsUnorderedPairsAndRetainDuplicateOccurrences()
    {
        CborMapEntry firstPair = CreateTextPair("a", 1);
        CborMapEntry secondPair = CreateTextPair("b", 2);
        CborValue left = new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries = [firstPair, secondPair, CreateTextPair("a", 1)]
        };
        CborValue right = new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries = [CreateTextPair("a", 1), CreateTextPair("a", 1), CreateTextPair("b", 2)]
        };
        CborValue different = new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries = [CreateTextPair("a", 1), CreateTextPair("a", 2), CreateTextPair("b", 2)]
        };

        Assert.Multiple(() =>
        {
            Assert.That(CborValueComparer.AreEqual(left, right), Is.True);
            Assert.That(CborKeyComparer.GetEquivalentHash(left),
                Is.EqualTo(CborKeyComparer.GetEquivalentHash(right)));
            Assert.That(CborValueComparer.AreEqual(left, different), Is.False);
        });
    }

    [Test]
    public void EquivalentKeyLookupUsesHashAndFullEquivalence()
    {
        CborValue halfValue = WrapFloat(CborHalfPrecision.CreateHalfFloat(0x3e00));
        CborValue integerValue = new CborValue
        {
            Kind = CborValueKind.UnsignedInteger,
            IntegerValue = BigInteger.One
        };
        List<CborMapEntry> entries =
        [
            new CborMapEntry { Key = halfValue, Value = new CborValue() },
            new CborMapEntry { Key = integerValue, Value = new CborValue() }
        ];
        CborValue equivalentDouble = WrapFloat(
            CborHalfPrecision.CreateDoubleFloat(0x3ff8000000000000));
        CborValue missing = new CborValue
        {
            Kind = CborValueKind.TextString,
            TextStringValue = "missing"
        };

        Assert.Multiple(() =>
        {
            Assert.That(CborKeyComparer.FindEquivalentKeyIndex(entries, equivalentDouble), Is.EqualTo(0));
            Assert.That(CborKeyComparer.FindEquivalentKeyIndex(entries, integerValue), Is.EqualTo(1));
            Assert.That(CborKeyComparer.FindEquivalentKeyIndex(entries, missing), Is.EqualTo(-1));
        });
    }

    [Test]
    public void CyclicGraphsCompareAndHashWithoutRecursingForever()
    {
        List<CborValue> leftItems = [];
        CborValue left = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = leftItems,
            IsSharedValue = true,
            SharedValueIndex = 0
        };
        leftItems.Add(left);

        List<CborValue> rightItems = [];
        CborValue right = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = rightItems,
            IsSharedValue = true,
            SharedValueIndex = 99
        };
        rightItems.Add(right);

        CborValue different = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = [new CborValue { Kind = CborValueKind.Array, ArrayItems = [] }]
        };
        List<CborValue> twoNodeOuterItems = [];
        List<CborValue> twoNodeInnerItems = [];
        CborValue twoNodeOuter = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = twoNodeOuterItems
        };
        CborValue twoNodeInner = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = twoNodeInnerItems
        };
        twoNodeOuterItems.Add(twoNodeInner);
        twoNodeInnerItems.Add(twoNodeOuter);

        Assert.Multiple(() =>
        {
            Assert.That(CborValueComparer.AreEqual(left, right), Is.True);
            Assert.That(CborKeyComparer.GetEquivalentHash(left),
                Is.EqualTo(CborKeyComparer.GetEquivalentHash(right)));
            Assert.That(CborValueComparer.AreEqual(left, different), Is.False);
            Assert.That(CborValueComparer.AreEqual(left, twoNodeOuter), Is.False);
        });
    }

    [Test]
    public void DeterministicEncodedKeyComparisonSupportsBothRfcOrders()
    {
        byte[] ten = [0x0a];
        byte[] hundred = [0x18, 0x64];
        byte[] negativeOne = [0x20];

        CborEncodeException? invalidOrder = Assert.Throws<CborEncodeException>(
            () => CborKeyComparer.CompareDeterministicEncoding(
                ten,
                hundred,
                CborMapKeyOrder.Preserve));

        Assert.Multiple(() =>
        {
            Assert.That(CborKeyComparer.CompareDeterministicEncoding(
                ten, hundred, CborMapKeyOrder.Lexicographic), Is.EqualTo(-1));
            Assert.That(CborKeyComparer.CompareDeterministicEncoding(
                hundred, negativeOne, CborMapKeyOrder.Lexicographic), Is.EqualTo(-1));
            Assert.That(CborKeyComparer.CompareDeterministicEncoding(
                hundred, negativeOne, CborMapKeyOrder.LengthFirst), Is.EqualTo(1));
            Assert.That(CborKeyComparer.CompareDeterministicEncoding(
                ten, negativeOne, CborMapKeyOrder.LengthFirst), Is.EqualTo(-1));
            Assert.That(CborKeyComparer.CompareDeterministicEncoding(
                ten, ten, CborMapKeyOrder.Lexicographic), Is.Zero);
            Assert.That(CborKeyComparer.CompareDeterministicEncoding(
                [0x01], [0x01, 0x00], CborMapKeyOrder.Lexicographic), Is.EqualTo(-1));
            Assert.That(invalidOrder?.ErrorCode, Is.EqualTo("CBOR_MAP_KEY_ORDER_INVALID"));
        });
    }

    private static CborValue WrapFloat(CborFloatValue value)
    {
        return new CborValue
        {
            Kind = CborValueKind.Float,
            FloatValue = value,
            FloatWidth = value.Width
        };
    }

    private static CborMapEntry CreateTextPair(string key, int value)
    {
        return new CborMapEntry
        {
            Key = new CborValue { Kind = CborValueKind.TextString, TextStringValue = key },
            Value = new CborValue
            {
                Kind = CborValueKind.UnsignedInteger,
                IntegerValue = new BigInteger(value)
            }
        };
    }
}
