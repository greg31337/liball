using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborTokenContainerTests
{
    [Test]
    public void NestedArraysEndOnlyAfterTheirDeclaredItemCounts()
    {
        byte[] source = [0x83, 0x01, 0x82, 0x02, 0x03, 0x04, 0x05];
        CborTokenKind[] expectedKinds =
        [
            CborTokenKind.StartArray,
            CborTokenKind.UnsignedInteger,
            CborTokenKind.StartArray,
            CborTokenKind.UnsignedInteger,
            CborTokenKind.UnsignedInteger,
            CborTokenKind.EndArray,
            CborTokenKind.UnsignedInteger,
            CborTokenKind.EndArray
        ];
        int[] expectedConsumed = [1, 1, 1, 1, 1, 0, 1, 0];
        CborTokenReader reader = CreateReader();
        int offset = 0;

        for (int index = 0; index < expectedKinds.Length; index++)
        {
            CborTokenReadResult result = reader.ReadToken(source.AsSpan(offset).ToArray());
            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done));
                Assert.That(result.Token?.Kind, Is.EqualTo(expectedKinds[index]));
                Assert.That(result.Consumed, Is.EqualTo(expectedConsumed[index]));
                Assert.That(result.Remaining, Is.EqualTo(source.Length - offset - expectedConsumed[index]));
            });
            offset += result.Consumed;
        }

        CborTokenReadResult trailing = reader.ReadToken(source.AsSpan(offset).ToArray());
        Assert.Multiple(() =>
        {
            Assert.That(offset, Is.EqualTo(6));
            Assert.That(trailing.Token?.Kind, Is.EqualTo(CborTokenKind.UnsignedInteger));
            Assert.That(trailing.Token!.IntegerValue, Is.EqualTo(new System.Numerics.BigInteger(5)));
            Assert.That(trailing.Consumed, Is.EqualTo(1));
        });
    }

    [Test]
    public void MapPairsCountNestedKeysAndTaggedValuesAsSingleItems()
    {
        byte[] source = [0xa2, 0x01, 0x02, 0x81, 0x03, 0xc0, 0x04, 0x05];
        CborTokenKind[] expectedKinds =
        [
            CborTokenKind.StartMap,
            CborTokenKind.UnsignedInteger,
            CborTokenKind.UnsignedInteger,
            CborTokenKind.StartArray,
            CborTokenKind.UnsignedInteger,
            CborTokenKind.EndArray,
            CborTokenKind.Tag,
            CborTokenKind.UnsignedInteger,
            CborTokenKind.EndMap
        ];
        int[] expectedConsumed = [1, 1, 1, 1, 1, 0, 1, 1, 0];
        CborTokenReader reader = CreateReader();
        int offset = 0;

        for (int index = 0; index < expectedKinds.Length; index++)
        {
            CborTokenReadResult result = reader.ReadToken(source.AsSpan(offset).ToArray());
            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done));
                Assert.That(result.Token?.Kind, Is.EqualTo(expectedKinds[index]));
                Assert.That(result.Consumed, Is.EqualTo(expectedConsumed[index]));
            });
            offset += result.Consumed;
        }

        CborTokenReadResult trailing = reader.ReadToken(source.AsSpan(offset).ToArray());
        Assert.Multiple(() =>
        {
            Assert.That(offset, Is.EqualTo(7));
            Assert.That(trailing.Token?.Kind, Is.EqualTo(CborTokenKind.UnsignedInteger));
            Assert.That(trailing.Token!.IntegerValue, Is.EqualTo(new System.Numerics.BigInteger(5)));
        });
    }

    [Test]
    public void EmptyNestedContainersProduceOrderedZeroConsumptionEnds()
    {
        CborTokenReader reader = CreateReader();

        CborTokenReadResult arrayStart = reader.ReadToken([0x81, 0xa0, 0x07]);
        CborTokenReadResult mapStart = reader.ReadToken([0xa0, 0x07]);
        CborTokenReadResult mapEnd = reader.ReadToken([0x07]);
        CborTokenReadResult arrayEnd = reader.ReadToken([0x07]);
        CborTokenReadResult trailing = reader.ReadToken([0x07]);

        Assert.Multiple(() =>
        {
            Assert.That(arrayStart.Token?.Kind, Is.EqualTo(CborTokenKind.StartArray));
            Assert.That(mapStart.Token?.Kind, Is.EqualTo(CborTokenKind.StartMap));
            Assert.That(mapEnd.Token?.Kind, Is.EqualTo(CborTokenKind.EndMap));
            Assert.That(mapEnd.Token?.LengthKind, Is.EqualTo(CborLengthKind.Definite));
            Assert.That(mapEnd.Consumed, Is.Zero);
            Assert.That(mapEnd.Remaining, Is.EqualTo(1));
            Assert.That(arrayEnd.Token?.Kind, Is.EqualTo(CborTokenKind.EndArray));
            Assert.That(arrayEnd.Token?.LengthKind, Is.EqualTo(CborLengthKind.Definite));
            Assert.That(arrayEnd.Consumed, Is.Zero);
            Assert.That(arrayEnd.Remaining, Is.EqualTo(1));
            Assert.That(trailing.Token!.IntegerValue, Is.EqualTo(new System.Numerics.BigInteger(7)));
            Assert.That(trailing.Consumed, Is.EqualTo(1));
        });
    }

    [Test]
    public void NestedTagsCompleteOneParentItemWithoutEndTokens()
    {
        CborTokenReader reader = CreateReader();

        CborTokenReadResult arrayStart = reader.ReadToken([0x81]);
        CborTokenReadResult firstTag = reader.ReadToken([0xc0]);
        CborTokenReadResult secondTag = reader.ReadToken([0xc1]);
        CborTokenReadResult taggedValue = reader.ReadToken([0x00]);
        CborTokenReadResult arrayEnd = reader.ReadToken([0x09]);
        CborTokenReadResult trailing = reader.ReadToken([0x09]);

        Assert.Multiple(() =>
        {
            Assert.That(arrayStart.Token?.Kind, Is.EqualTo(CborTokenKind.StartArray));
            Assert.That(firstTag.Token?.Kind, Is.EqualTo(CborTokenKind.Tag));
            Assert.That(firstTag.Token?.TagNumber, Is.Zero);
            Assert.That(secondTag.Token?.Kind, Is.EqualTo(CborTokenKind.Tag));
            Assert.That(secondTag.Token?.TagNumber, Is.EqualTo(1));
            Assert.That(taggedValue.Token?.Kind, Is.EqualTo(CborTokenKind.UnsignedInteger));
            Assert.That(arrayEnd.Token?.Kind, Is.EqualTo(CborTokenKind.EndArray));
            Assert.That(arrayEnd.Consumed, Is.Zero);
            Assert.That(trailing.Token!.IntegerValue, Is.EqualTo(new System.Numerics.BigInteger(9)));
        });
    }

    [Test]
    public void MapDoesNotEndBetweenItsKeyAndValue()
    {
        CborTokenReader reader = CreateReader();

        CborTokenReadResult mapStart = reader.ReadToken([0xa1]);
        CborTokenReadResult key = reader.ReadToken([0x01]);
        CborTokenReadResult waitingForValue = reader.ReadToken([]);
        CborTokenReadResult value = reader.ReadToken([0x02]);
        CborTokenReadResult mapEnd = reader.ReadToken([]);

        Assert.Multiple(() =>
        {
            Assert.That(mapStart.Token?.Kind, Is.EqualTo(CborTokenKind.StartMap));
            Assert.That(key.Token?.Kind, Is.EqualTo(CborTokenKind.UnsignedInteger));
            Assert.That(waitingForValue.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
            Assert.That(waitingForValue.Token, Is.Null);
            Assert.That(value.Token?.Kind, Is.EqualTo(CborTokenKind.UnsignedInteger));
            Assert.That(mapEnd.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(mapEnd.Token?.Kind, Is.EqualTo(CborTokenKind.EndMap));
            Assert.That(mapEnd.Consumed, Is.Zero);
        });
    }

    [Test]
    public void HugeMapPairCountsAreRejectedWithoutOverflow()
    {
        CborTokenReader reader = CreateReader();
        CborTokenReadResult start = reader.ReadToken(
            [0xbb, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff]);
        Assert.Multiple(() =>
        {
            Assert.That(start.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(start.ErrorCode, Is.EqualTo("CBOR_MAX_MAP_PAIRS"));
            Assert.That(start.Consumed, Is.Zero);
            Assert.That(start.Remaining, Is.EqualTo(9));
            Assert.That(start.Token, Is.Null);
        });
    }

    [Test]
    public void ResetClearsOpenFramesAndRestartsSourceOffsets()
    {
        CborTokenReader reader = CreateReader();
        CborTokenReadResult start = reader.ReadToken([0x81]);

        reader.ResetReader();
        CborTokenReadResult scalar = reader.ReadToken([0x01]);
        CborTokenReadResult afterScalar = reader.ReadToken([]);

        Assert.Multiple(() =>
        {
            Assert.That(start.Token?.Kind, Is.EqualTo(CborTokenKind.StartArray));
            Assert.That(scalar.Token?.Kind, Is.EqualTo(CborTokenKind.UnsignedInteger));
            Assert.That(scalar.Token?.Representation?.SourceOffset, Is.Zero);
            Assert.That(afterScalar.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
            Assert.That(afterScalar.Token, Is.Null);
        });
    }

    private static CborTokenReader CreateReader()
    {
        CborDecodeOptions options = new CborDecodeOptions
        {
            RetainRepresentation = true,
            RetainSourceRanges = true
        };
        return new CborTokenReader(options);
    }
}
