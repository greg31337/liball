using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborGenericModelTests
{
    [Test]
    public void IntegerValuesPreserveTheCompleteCborRange()
    {
        BigInteger largestUnsigned = new BigInteger(ulong.MaxValue);
        BigInteger smallestNegative = -BigInteger.One - largestUnsigned;
        CborValue unsignedValue = new CborValue
        {
            Kind = CborValueKind.UnsignedInteger,
            IntegerValue = largestUnsigned
        };
        CborValue negativeValue = new CborValue
        {
            Kind = CborValueKind.NegativeInteger,
            IntegerValue = smallestNegative
        };

        Assert.Multiple(() =>
        {
            Assert.That(unsignedValue.IntegerValue, Is.EqualTo(largestUnsigned));
            Assert.That(negativeValue.IntegerValue, Is.EqualTo(smallestNegative));
        });
    }

    [Test]
    public void StringsRetainIndefiniteChunkAndSourceMetadata()
    {
        List<int> chunkLengths = [2, 3];
        CborRepresentation representation = new CborRepresentation
        {
            HeadArgumentWidth = 0,
            LengthKind = CborLengthKind.Indefinite,
            ChunkByteLengths = chunkLengths,
            HasSourceRange = true,
            SourceOffset = 11,
            SourceLength = 8
        };
        CborValue value = new CborValue
        {
            Kind = CborValueKind.ByteString,
            ByteStringValue = [1, 2, 3, 4, 5],
            LengthKind = CborLengthKind.Indefinite,
            Representation = representation
        };

        Assert.Multiple(() =>
        {
            Assert.That(value.ByteStringValue, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
            Assert.That(value.Representation, Is.SameAs(representation));
            Assert.That(representation.ChunkByteLengths, Is.SameAs(chunkLengths));
            Assert.That(representation.SourceOffset, Is.EqualTo(11));
            Assert.That(representation.SourceLength, Is.EqualTo(8));
        });
    }

    [Test]
    public void MapsPreserveArbitraryKeysOrderAndDuplicates()
    {
        CborValue key = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = []
        };
        CborValue firstValue = new CborValue
        {
            Kind = CborValueKind.TextString,
            TextStringValue = "first"
        };
        CborValue secondValue = new CborValue
        {
            Kind = CborValueKind.TextString,
            TextStringValue = "second"
        };
        List<CborMapEntry> entries =
        [
            new CborMapEntry { Key = key, Value = firstValue },
            new CborMapEntry { Key = key, Value = secondValue }
        ];
        CborValue map = new CborValue
        {
            Kind = CborValueKind.Map,
            MapEntries = entries
        };

        Assert.Multiple(() =>
        {
            Assert.That(map.MapEntries, Is.SameAs(entries));
            Assert.That(entries[0].Key, Is.SameAs(key));
            Assert.That(entries[1].Key, Is.SameAs(key));
            Assert.That(entries[0].Value, Is.SameAs(firstValue));
            Assert.That(entries[1].Value, Is.SameAs(secondValue));
        });
    }

    [Test]
    public void TagsPreserveTheFullUnsignedTagNumberAndNesting()
    {
        CborValue content = new CborValue
        {
            Kind = CborValueKind.SimpleValue,
            SimpleValue = CborSimpleValues.Null
        };
        CborValue inner = new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = CborTagNumbers.EncodedCbor,
            TaggedValue = content
        };
        CborValue outer = new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = ulong.MaxValue,
            TaggedValue = inner
        };

        Assert.Multiple(() =>
        {
            Assert.That(outer.TagNumber, Is.EqualTo(ulong.MaxValue));
            Assert.That(outer.TaggedValue, Is.SameAs(inner));
            Assert.That(inner.TaggedValue, Is.SameAs(content));
        });
    }

    [Test]
    public void FloatingPointValuesRetainWidthAndRawNanBits()
    {
        const ulong signalingHalfNan = 0x7d01;
        CborFloatValue floatValue = new CborFloatValue
        {
            Value = double.NaN,
            Width = CborFloatWidth.Half,
            RawBits = signalingHalfNan,
            IsNaN = true,
            IsSignalingNaN = true
        };
        CborRepresentation representation = new CborRepresentation
        {
            FloatWidth = CborFloatWidth.Half,
            FloatBits = signalingHalfNan,
            HasFloatBits = true
        };
        CborValue value = new CborValue
        {
            Kind = CborValueKind.Float,
            FloatValue = floatValue,
            FloatWidth = CborFloatWidth.Half,
            Representation = representation
        };

        Assert.Multiple(() =>
        {
            Assert.That(double.IsNaN(floatValue.Value), Is.True);
            Assert.That(floatValue.RawBits, Is.EqualTo(signalingHalfNan));
            Assert.That(floatValue.IsSignalingNaN, Is.True);
            Assert.That(value.FloatValue, Is.SameAs(floatValue));
            Assert.That(representation.FloatBits, Is.EqualTo(signalingHalfNan));
        });
    }

    [Test]
    public void SharedReferencesCanRepresentCycles()
    {
        List<CborValue> items = [];
        CborValue shared = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems = items,
            IsSharedValue = true,
            SharedValueIndex = 0
        };
        CborValue reference = new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = CborTagNumbers.SharedValueReference,
            SharedValueIndex = 0,
            ReferencedValue = shared
        };
        items.Add(reference);

        Assert.Multiple(() =>
        {
            Assert.That(shared.IsSharedValue, Is.True);
            Assert.That(shared.ArrayItems, Is.SameAs(items));
            Assert.That(items[0], Is.SameAs(reference));
            Assert.That(reference.ReferencedValue, Is.SameAs(shared));
            Assert.That(new CborValue().SharedValueIndex, Is.EqualTo(-1));
        });
    }
}
