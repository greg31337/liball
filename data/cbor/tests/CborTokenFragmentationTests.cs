using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborTokenFragmentationTests
{
    [Test]
    public void EveryDefiniteTokenFormResumesAtEveryByteBoundary()
    {
        AssertEverySplit(
            [0x1b, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff],
            CborTokenKind.UnsignedInteger
        );
        AssertEverySplit(
            [0x3a, 0x00, 0x01, 0x00, 0x00],
            CborTokenKind.NegativeInteger
        );
        AssertEverySplit(
            [0x5a, 0x00, 0x00, 0x00, 0x03, 0x01, 0x02, 0x03],
            CborTokenKind.ByteString
        );
        AssertEverySplit(
            [0x7a, 0x00, 0x00, 0x00, 0x03, 0xe2, 0x82, 0xac],
            CborTokenKind.TextString
        );
        AssertEverySplit(
            [0x9a, 0x00, 0x01, 0x00, 0x00],
            CborTokenKind.StartArray
        );
        AssertEverySplit(
            [0xba, 0x00, 0x01, 0x00, 0x00],
            CborTokenKind.StartMap
        );
        AssertEverySplit(
            [0xdb, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff],
            CborTokenKind.Tag
        );
        AssertEverySplit([0xf4], CborTokenKind.SimpleValue);
        AssertEverySplit([0xf8, 0x20], CborTokenKind.SimpleValue);
        AssertEverySplit([0xf9, 0x3e, 0x00], CborTokenKind.Float);
        AssertEverySplit([0xfa, 0x3f, 0xc0, 0x00, 0x00], CborTokenKind.Float);
        AssertEverySplit(
            [0xfb, 0x3f, 0xf8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
            CborTokenKind.Float
        );
        AssertEverySplit([0x9f], CborTokenKind.StartArray);
    }

    [Test]
    public void NestedDocumentCanBeReadOneNewByteAtATime()
    {
        byte[] encoded =
        [0x82, 0x1a, 0x00, 0x00, 0x01, 0x00, 0x63, 0xe2, 0x82, 0xac, 0x07];
        CborTokenReader reader = CreateReader();

        CborTokenReadResult start = reader.ReadToken([encoded[0]]);
        CborTokenReadResult integer = new CborTokenReadResult();
        for (int index = 1; index <= 5; index++)
        {
            integer = reader.ReadToken([encoded[index]]);
            if (index < 5)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(integer.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
                    Assert.That(integer.Consumed, Is.EqualTo(1));
                    Assert.That(integer.Remaining, Is.Zero);
                });
            }
        }

        CborTokenReadResult text = new CborTokenReadResult();
        for (int index = 6; index <= 9; index++)
        {
            text = reader.ReadToken([encoded[index]]);
            if (index < 9)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(text.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
                    Assert.That(text.Consumed, Is.EqualTo(1));
                    Assert.That(text.Remaining, Is.Zero);
                });
            }
        }

        CborTokenReadResult end = reader.ReadToken([encoded[10]]);
        CborTokenReadResult trailing = reader.ReadToken([encoded[10]]);

        Assert.Multiple(() =>
        {
            Assert.That(start.Token?.Kind, Is.EqualTo(CborTokenKind.StartArray));
            Assert.That(integer.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(integer.Token?.Kind, Is.EqualTo(CborTokenKind.UnsignedInteger));
            Assert.That(integer.Consumed, Is.EqualTo(1));
            Assert.That(integer.Token?.Representation?.SourceOffset, Is.EqualTo(1));
            Assert.That(integer.Token?.Representation?.SourceLength, Is.EqualTo(5));
            Assert.That(text.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(text.Token?.Kind, Is.EqualTo(CborTokenKind.TextString));
            Assert.That(text.Token?.TextStringValue, Is.EqualTo("€"));
            Assert.That(text.Consumed, Is.EqualTo(1));
            Assert.That(text.Token?.Representation?.SourceOffset, Is.EqualTo(6));
            Assert.That(text.Token?.Representation?.SourceLength, Is.EqualTo(4));
            Assert.That(end.Token?.Kind, Is.EqualTo(CborTokenKind.EndArray));
            Assert.That(end.Consumed, Is.Zero);
            Assert.That(end.Remaining, Is.EqualTo(1));
            Assert.That(trailing.Token?.Kind, Is.EqualTo(CborTokenKind.UnsignedInteger));
            Assert.That(trailing.Consumed, Is.EqualTo(1));
        });
    }

    [Test]
    public void CompletionConsumesOnlyTheRequiredPartOfTheLatestFragment()
    {
        CborTokenReader reader = CreateReader();

        CborTokenReadResult first = reader.ReadToken([0x1a, 0x00]);
        CborTokenReadResult completed = reader.ReadToken([0x00, 0x01, 0x00, 0x07]);
        CborTokenReadResult trailing = reader.ReadToken([0x07]);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
            Assert.That(first.Consumed, Is.EqualTo(2));
            Assert.That(first.Remaining, Is.Zero);
            Assert.That(completed.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(completed.Token?.Kind, Is.EqualTo(CborTokenKind.UnsignedInteger));
            Assert.That(completed.Consumed, Is.EqualTo(3));
            Assert.That(completed.Remaining, Is.EqualTo(1));
            Assert.That(completed.Token?.Representation?.SourceLength, Is.EqualTo(5));
            Assert.That(trailing.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(trailing.Consumed, Is.EqualTo(1));
            Assert.That(trailing.Token?.Representation?.SourceOffset, Is.EqualTo(5));
        });
    }

    [Test]
    public void IndefiniteStringChunksResumeAtEveryHeadAndPayloadBoundary()
    {
        AssertEveryChunkSplit(
            0x5f,
            [0x5a, 0x00, 0x00, 0x00, 0x03, 0x01, 0x02, 0x03],
            CborTokenKind.ByteStringChunk,
            CborTokenKind.EndByteString
        );
        AssertEveryChunkSplit(
            0x7f,
            [0x7a, 0x00, 0x00, 0x00, 0x03, 0xe2, 0x82, 0xac],
            CborTokenKind.TextStringChunk,
            CborTokenKind.EndTextString
        );
    }

    [Test]
    public void BufferedBytesAreOwnedByTheReaderAndResetDiscardsThem()
    {
        CborTokenReader reader = CreateReader();
        byte[] firstFragment = [0x1a, 0x00];
        CborTokenReadResult buffered = reader.ReadToken(firstFragment);
        firstFragment[0] = 0xff;
        firstFragment[1] = 0xff;
        CborTokenReadResult completed = reader.ReadToken([0x00, 0x01, 0x00]);

        CborTokenReadResult secondBuffered = reader.ReadToken([0x1a, 0x00]);
        reader.ResetReader();
        CborTokenReadResult afterReset = reader.ReadToken([0x01]);

        Assert.Multiple(() =>
        {
            Assert.That(buffered.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
            Assert.That(completed.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(completed.Token!.IntegerValue, Is.EqualTo(new System.Numerics.BigInteger(256)));
            Assert.That(secondBuffered.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
            Assert.That(afterReset.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(afterReset.Token?.Kind, Is.EqualTo(CborTokenKind.UnsignedInteger));
            Assert.That(afterReset.Token!.IntegerValue, Is.EqualTo(new System.Numerics.BigInteger(1)));
            Assert.That(afterReset.Token?.Representation?.SourceOffset, Is.Zero);
        });
    }

    [Test]
    public void FragmentedMalformedTextReportsItsAbsolutePayloadOffset()
    {
        CborTokenReader reader = CreateReader();
        CborTokenReadResult first = reader.ReadToken([0x63, 0xe2]);
        CborTokenReadResult failure = reader.ReadToken([0x28, 0xa1]);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
            Assert.That(first.Consumed, Is.EqualTo(2));
            Assert.That(failure.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(failure.ErrorCode, Is.EqualTo("CBOR_UTF8_INVALID_CONTINUATION"));
            Assert.That(failure.ErrorOffset, Is.EqualTo(2));
            Assert.That(failure.Consumed, Is.Zero);
            Assert.That(failure.Remaining, Is.EqualTo(2));
        });
    }

    private static void AssertEverySplit(byte[] encoding, CborTokenKind expectedKind)
    {
        for (int split = 0; split < encoding.Length; split++)
        {
            CborTokenReader reader = CreateReader();
            CborTokenReadResult first = reader.ReadToken(encoding.AsSpan(0, split).ToArray());
            byte[] remainderAndTrailing = new byte[encoding.Length - split + 1];
            encoding.AsSpan(split).CopyTo(remainderAndTrailing);
            remainderAndTrailing[remainderAndTrailing.Length - 1] = 0x00;
            CborTokenReadResult completed = reader.ReadToken(remainderAndTrailing);

            Assert.Multiple(() =>
            {
                Assert.That(first.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
                Assert.That(first.Consumed, Is.EqualTo(split));
                Assert.That(first.Remaining, Is.Zero);
                Assert.That(completed.Status, Is.EqualTo(CborReadStatus.Done));
                Assert.That(completed.Token?.Kind, Is.EqualTo(expectedKind));
                Assert.That(completed.Consumed, Is.EqualTo(encoding.Length - split));
                Assert.That(completed.Remaining, Is.EqualTo(1));
                Assert.That(completed.Token?.Representation?.SourceOffset, Is.Zero);
                Assert.That(completed.Token?.Representation?.SourceLength, Is.EqualTo(encoding.Length));
            });
        }
    }

    private static void AssertEveryChunkSplit(
        byte startByte,
        byte[] chunk,
        CborTokenKind expectedChunkKind,
        CborTokenKind expectedEndKind)
    {
        for (int split = 0; split < chunk.Length; split++)
        {
            CborTokenReader reader = CreateReader();
            CborTokenReadResult start = reader.ReadToken([startByte]);
            CborTokenReadResult first = reader.ReadToken(chunk.AsSpan(0, split).ToArray());
            byte[] remainderAndBreak = new byte[chunk.Length - split + 1];
            chunk.AsSpan(split).CopyTo(remainderAndBreak);
            remainderAndBreak[remainderAndBreak.Length - 1] = 0xff;
            CborTokenReadResult completed = reader.ReadToken(remainderAndBreak);
            CborTokenReadResult end = reader.ReadToken([0xff]);

            Assert.Multiple(() =>
            {
                Assert.That(start.Status, Is.EqualTo(CborReadStatus.Done));
                Assert.That(first.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
                Assert.That(first.Consumed, Is.EqualTo(split));
                Assert.That(completed.Status, Is.EqualTo(CborReadStatus.Done));
                Assert.That(completed.Token?.Kind, Is.EqualTo(expectedChunkKind));
                Assert.That(completed.Consumed, Is.EqualTo(chunk.Length - split));
                Assert.That(completed.Remaining, Is.EqualTo(1));
                Assert.That(start.Token?.Representation?.ChunkByteLengths, Is.EqualTo(new[] { 3 }));
                Assert.That(end.Token?.Kind, Is.EqualTo(expectedEndKind));
                Assert.That(end.Consumed, Is.EqualTo(1));
            });
        }
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
