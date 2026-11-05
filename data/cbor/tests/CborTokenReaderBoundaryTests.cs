using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborTokenReaderBoundaryTests
{
    [Test]
    public void EveryTokenKindTraversesOneByteFragmentsAndContainerTransitions()
    {
        byte[] source =
        [
            0x9f,
            0x00,
            0x20,
            0x41, 0xaa,
            0x61, 0x61,
            0x5f, 0x41, 0xbb, 0xff,
            0x7f, 0x61, 0x62, 0xff,
            0x81, 0x01,
            0xa1, 0x02, 0x03,
            0xc0, 0x04,
            0xf4,
            0xf9, 0x3c, 0x00,
            0xff
        ];
        CborTokenKind[] expectedKinds =
        [
            CborTokenKind.StartArray,
            CborTokenKind.UnsignedInteger,
            CborTokenKind.NegativeInteger,
            CborTokenKind.ByteString,
            CborTokenKind.TextString,
            CborTokenKind.StartByteString,
            CborTokenKind.ByteStringChunk,
            CborTokenKind.EndByteString,
            CborTokenKind.StartTextString,
            CborTokenKind.TextStringChunk,
            CborTokenKind.EndTextString,
            CborTokenKind.StartArray,
            CborTokenKind.UnsignedInteger,
            CborTokenKind.EndArray,
            CborTokenKind.StartMap,
            CborTokenKind.UnsignedInteger,
            CborTokenKind.UnsignedInteger,
            CborTokenKind.EndMap,
            CborTokenKind.Tag,
            CborTokenKind.UnsignedInteger,
            CborTokenKind.SimpleValue,
            CborTokenKind.Float,
            CborTokenKind.EndArray
        ];
        CborTokenReader reader = CreateReader(new CborDecodeOptions());
        int sourceOffset = 0;
        int tokenIndex = 0;
        int needMoreCount = 0;

        while (tokenIndex < expectedKinds.Length)
        {
            byte[] fragment = sourceOffset < source.Length ? [source[sourceOffset]] : [];
            CborTokenReadResult result = reader.ReadToken(fragment);
            sourceOffset = sourceOffset + result.Consumed;

            if (result.Status == CborReadStatus.NeedMoreData)
            {
                needMoreCount = needMoreCount + 1;
                Assert.That(result.Consumed, Is.EqualTo(fragment.Length));
                continue;
            }

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done));
                Assert.That(result.Token, Is.Not.Null);
                Assert.That(result.Token?.Kind, Is.EqualTo(expectedKinds[tokenIndex]));
            });
            tokenIndex = tokenIndex + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(sourceOffset, Is.EqualTo(source.Length));
            Assert.That(needMoreCount, Is.GreaterThan(0));
            Assert.That(reader.Core.Frames, Is.Empty);
        });
    }

    [Test]
    public void PendingByteLimitsRejectInitialAndContinuationGrowthAtomically()
    {
        CborTokenReader initialReader = CreateReader(
            new CborDecodeOptions { MaxTotalBytes = 1 });
        CborTokenReadResult initial = initialReader.ReadToken([0x1a, 0x00]);

        CborTokenReader continuationReader = CreateReader(
            new CborDecodeOptions { MaxTotalBytes = 4 });
        CborTokenReadResult pending = continuationReader.ReadToken([0x1a]);
        CborTokenReadResult continuation = continuationReader.ReadToken([0x00, 0x00, 0x00, 0x00]);

        Assert.Multiple(() =>
        {
            Assert.That(initial.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(initial.ErrorCode, Is.EqualTo("CBOR_MAX_TOTAL_BYTES"));
            Assert.That(initial.Consumed, Is.Zero);
            Assert.That(initial.Remaining, Is.EqualTo(2));
            Assert.That(initialReader.Core.PendingBytes, Is.Empty);
            Assert.That(initialReader.Core.Limits.TotalBytes, Is.Zero);
            Assert.That(pending.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
            Assert.That(pending.Consumed, Is.EqualTo(1));
            Assert.That(continuation.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(continuation.ErrorCode, Is.EqualTo("CBOR_MAX_TOTAL_BYTES"));
            Assert.That(continuation.Consumed, Is.Zero);
            Assert.That(continuation.Remaining, Is.EqualTo(4));
            Assert.That(continuationReader.Core.PendingBytes, Is.EqualTo(new byte[] { 0x1a }));
            Assert.That(continuationReader.Core.Limits.TotalBytes, Is.Zero);
        });
    }

    [Test]
    public void DeclaredStringEncodingMustFitTheTotalByteBudgetBeforeBuffering()
    {
        CborDecodeOptions options = new CborDecodeOptions
        {
            MaxStringBytes = 4,
            MaxTotalBytes = 3
        };
        CborTokenReader byteReader = CreateReader(options);
        CborTokenReader textReader = CreateReader(options);

        CborTokenReadResult bytes = byteReader.ReadToken([0x44]);
        CborTokenReadResult text = textReader.ReadToken([0x64]);

        Assert.Multiple(() =>
        {
            Assert.That(bytes.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(bytes.ErrorCode, Is.EqualTo("CBOR_MAX_TOTAL_BYTES"));
            Assert.That(bytes.Consumed, Is.Zero);
            Assert.That(byteReader.Core.PendingBytes, Is.Empty);
            Assert.That(text.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(text.ErrorCode, Is.EqualTo("CBOR_MAX_TOTAL_BYTES"));
            Assert.That(text.Consumed, Is.Zero);
            Assert.That(textReader.Core.PendingBytes, Is.Empty);
        });
    }

    [Test]
    public void IndefiniteStartsAndBreaksRespectBudgetsWithoutChangingFrames()
    {
        CborTokenReader itemReader = CreateReader(
            new CborDecodeOptions { MaxTotalItems = 0 });
        CborTokenReadResult itemFailure = itemReader.ReadToken([0x9f]);

        CborTokenReader byteReader = CreateReader(
            new CborDecodeOptions { MaxTotalBytes = 1 });
        CborTokenReadResult start = byteReader.ReadToken([0x9f]);
        CborTokenReadResult breakFailure = byteReader.ReadToken([0xff]);

        Assert.Multiple(() =>
        {
            Assert.That(itemFailure.ErrorCode, Is.EqualTo("CBOR_MAX_TOTAL_ITEMS"));
            Assert.That(itemFailure.Consumed, Is.Zero);
            Assert.That(itemReader.Core.Frames, Is.Empty);
            Assert.That(itemReader.Core.Limits.TotalItems, Is.Zero);
            Assert.That(start.Token?.Kind, Is.EqualTo(CborTokenKind.StartArray));
            Assert.That(breakFailure.ErrorCode, Is.EqualTo("CBOR_MAX_TOTAL_BYTES"));
            Assert.That(breakFailure.Consumed, Is.Zero);
            Assert.That(byteReader.Core.Frames, Has.Count.EqualTo(1));
            Assert.That(byteReader.Core.Frames[0].Kind, Is.EqualTo(CborTokenKind.StartArray));
            Assert.That(byteReader.Core.Limits.TotalBytes, Is.EqualTo(1));
        });
    }

    [Test]
    public void WrongIndefiniteStringHeadsAreRejectedWithoutLosingState()
    {
        CborTokenReader byteReader = CreateReader(new CborDecodeOptions());
        byteReader.ReadToken([0x5f]);
        CborTokenReadResult wrongTextStart = byteReader.ReadToken([0x7f]);
        CborTokenReadResult byteChunk = byteReader.ReadToken([0x41, 0xaa]);
        CborTokenReadResult byteEnd = byteReader.ReadToken([0xff]);

        CborTokenReader textReader = CreateReader(new CborDecodeOptions());
        textReader.ReadToken([0x7f]);
        CborTokenReadResult wrongByteStart = textReader.ReadToken([0x5f]);
        CborTokenReadResult textChunk = textReader.ReadToken([0x61, 0x61]);
        CborTokenReadResult textEnd = textReader.ReadToken([0xff]);

        Assert.Multiple(() =>
        {
            Assert.That(wrongTextStart.ErrorCode, Is.EqualTo("CBOR_INVALID_BYTE_STRING_CHUNK"));
            Assert.That(wrongTextStart.Consumed, Is.Zero);
            Assert.That(byteChunk.Token?.Kind, Is.EqualTo(CborTokenKind.ByteStringChunk));
            Assert.That(byteEnd.Token?.Kind, Is.EqualTo(CborTokenKind.EndByteString));
            Assert.That(wrongByteStart.ErrorCode, Is.EqualTo("CBOR_INVALID_TEXT_STRING_CHUNK"));
            Assert.That(wrongByteStart.Consumed, Is.Zero);
            Assert.That(textChunk.Token?.Kind, Is.EqualTo(CborTokenKind.TextStringChunk));
            Assert.That(textEnd.Token?.Kind, Is.EqualTo(CborTokenKind.EndTextString));
        });
    }

    [Test]
    public void SkipRejectsChunkAndContainerEndPositionsWithoutConsumingTheNextValue()
    {
        CborTokenReader chunkReader = CreateReader(new CborDecodeOptions());
        chunkReader.ReadToken([0x5f]);
        CborReadResult chunkFailure = chunkReader.SkipValue([0x41, 0xaa, 0xff]);
        CborTokenReadResult stringEnd = chunkReader.ReadToken([0xff]);

        CborTokenReader endReader = CreateReader(new CborDecodeOptions());
        endReader.ReadToken([0x81]);
        endReader.ReadToken([0x00]);
        CborReadResult endFailure = endReader.SkipValue([0x07]);
        CborTokenReadResult trailing = endReader.ReadToken([0x07]);

        Assert.Multiple(() =>
        {
            Assert.That(chunkFailure.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(chunkFailure.ErrorCode, Is.EqualTo("CBOR_NO_VALUE_TO_SKIP"));
            Assert.That(chunkFailure.Consumed, Is.EqualTo(2));
            Assert.That(chunkFailure.Remaining, Is.EqualTo(1));
            Assert.That(stringEnd.Token?.Kind, Is.EqualTo(CborTokenKind.EndByteString));
            Assert.That(endFailure.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(endFailure.ErrorCode, Is.EqualTo("CBOR_NO_VALUE_TO_SKIP"));
            Assert.That(endFailure.Consumed, Is.Zero);
            Assert.That(endFailure.Remaining, Is.EqualTo(1));
            Assert.That(trailing.Token?.Kind, Is.EqualTo(CborTokenKind.UnsignedInteger));
            Assert.That(trailing.Token?.IntegerValue, Is.EqualTo(new BigInteger(7)));
        });
    }

    [Test]
    public void FragmentedMalformedHeadKeepsItsAbsoluteErrorOffsetAndPendingInput()
    {
        CborTokenReader reader = CreateReader(new CborDecodeOptions());

        CborTokenReadResult first = reader.ReadToken([0xf8]);
        CborTokenReadResult malformed = reader.ReadToken([0x1f, 0x07]);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
            Assert.That(first.Consumed, Is.EqualTo(1));
            Assert.That(malformed.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(malformed.ErrorCode, Is.EqualTo("CBOR_INVALID_SIMPLE_VALUE_ENCODING"));
            Assert.That(malformed.ErrorOffset, Is.EqualTo(1));
            Assert.That(malformed.Consumed, Is.Zero);
            Assert.That(malformed.Remaining, Is.EqualTo(2));
            Assert.That(reader.Core.PendingBytes, Is.EqualTo(new byte[] { 0xf8 }));
        });
    }

    [Test]
    public void SourceOffsetOverflowIsReportedWithoutCommittingTokenLimits()
    {
        CborTokenReader overflowReader = CreateReader(new CborDecodeOptions());
        overflowReader.Core.AbsoluteOffset = long.MaxValue;
        CborTokenReadResult overflow = overflowReader.ReadToken([0x00]);

        CborTokenReader malformedReader = CreateReader(new CborDecodeOptions());
        malformedReader.Core.AbsoluteOffset = long.MaxValue;
        CborTokenReadResult saturated = malformedReader.ReadToken([0xf8, 0x00]);

        Assert.Multiple(() =>
        {
            Assert.That(overflow.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(overflow.ErrorCode, Is.EqualTo("CBOR_SOURCE_OFFSET_OVERFLOW"));
            Assert.That(overflow.ErrorOffset, Is.EqualTo(long.MaxValue));
            Assert.That(overflow.Consumed, Is.Zero);
            Assert.That(overflowReader.Core.AbsoluteOffset, Is.EqualTo(long.MaxValue));
            Assert.That(overflowReader.Core.Limits.TotalBytes, Is.Zero);
            Assert.That(overflowReader.Core.Limits.TotalItems, Is.Zero);
            Assert.That(saturated.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(saturated.ErrorCode, Is.EqualTo("CBOR_INVALID_SIMPLE_VALUE_ENCODING"));
            Assert.That(saturated.ErrorOffset, Is.EqualTo(long.MaxValue));
        });
    }

    private static CborTokenReader CreateReader(CborDecodeOptions options)
    {
        options.RetainRepresentation = true;
        options.RetainSourceRanges = true;
        return new CborTokenReader(options);
    }
}
