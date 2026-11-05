using cbor;
using System.Numerics;

namespace cbor.Tests;

[TestFixture]
public class CborStreamReaderTests
{
    [Test]
    public void EmptyInputReturnsCleanEndOfDataRepeatedly()
    {
        ChunkedReadStream input = new ChunkedReadStream([], 8);
        CborStreamReader reader = new CborStreamReader(input, new CborDecodeOptions());

        CborReadResult first = reader.ReadNext();
        CborReadResult second = reader.ReadNext();

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(CborReadStatus.EndOfData));
            Assert.That(first.Value, Is.Null);
            Assert.That(first.Consumed, Is.Zero);
            Assert.That(first.Remaining, Is.Zero);
            Assert.That(first.ErrorCode, Is.Null);
            Assert.That(second.Status, Is.EqualTo(CborReadStatus.EndOfData));
            Assert.That(input.ReadCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void ConcatenatedValuesRetainBytesReadPastEachItem()
    {
        byte[] encoded =
        [
            0x01,
            0x82, 0x02, 0x03,
            0x64, 0x74, 0x65, 0x73, 0x74
        ];
        ChunkedReadStream input = new ChunkedReadStream(encoded, encoded.Length);
        CborStreamReader reader = new CborStreamReader(input, new CborDecodeOptions());

        CborReadResult first = reader.ReadNext();
        int readsAfterFirst = input.ReadCalls;
        CborReadResult second = reader.ReadNext();
        CborReadResult third = reader.ReadNext();
        int readsAfterValues = input.ReadCalls;
        CborReadResult end = reader.ReadNext();

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(first.Value?.IntegerValue, Is.EqualTo(new BigInteger(1)));
            Assert.That(first.Consumed, Is.EqualTo(1));
            Assert.That(first.Remaining, Is.EqualTo(8));
            Assert.That(second.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(second.Value?.ArrayItems, Has.Count.EqualTo(2));
            Assert.That(second.Value?.ArrayItems?[0].IntegerValue, Is.EqualTo(new BigInteger(2)));
            Assert.That(second.Value?.ArrayItems?[1].IntegerValue, Is.EqualTo(new BigInteger(3)));
            Assert.That(second.Consumed, Is.EqualTo(3));
            Assert.That(second.Remaining, Is.EqualTo(5));
            Assert.That(third.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(third.Value?.TextStringValue, Is.EqualTo("test"));
            Assert.That(third.Consumed, Is.EqualTo(5));
            Assert.That(third.Remaining, Is.Zero);
            Assert.That(readsAfterFirst, Is.EqualTo(1));
            Assert.That(readsAfterValues, Is.EqualTo(1));
            Assert.That(end.Status, Is.EqualTo(CborReadStatus.EndOfData));
            Assert.That(input.ReadCalls, Is.EqualTo(2));
        });
    }

    [Test]
    public void NonSeekableOneByteReadsAssembleNestedValues()
    {
        byte[] encoded =
        [0x82, 0x1a, 0x00, 0x00, 0x01, 0x00, 0x63, 0xe2, 0x82, 0xac];
        ChunkedReadStream input = new ChunkedReadStream(encoded, 1);
        CborStreamReader reader = new CborStreamReader(input, new CborDecodeOptions());

        CborReadResult result = reader.ReadNext();
        CborReadResult end = reader.ReadNext();

        Assert.Multiple(() =>
        {
            Assert.That(input.CanSeek, Is.False);
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(encoded.Length));
            Assert.That(result.Value?.ArrayItems, Has.Count.EqualTo(2));
            Assert.That(result.Value?.ArrayItems?[0].IntegerValue, Is.EqualTo(new BigInteger(256)));
            Assert.That(result.Value?.ArrayItems?[1].TextStringValue, Is.EqualTo("€"));
            Assert.That(end.Status, Is.EqualTo(CborReadStatus.EndOfData));
            Assert.That(input.ReadCalls, Is.EqualTo(encoded.Length + 1));
        });
    }

    [Test]
    public void SourceRangesRemainAbsoluteAcrossSuccessiveItems()
    {
        CborDecodeOptions options = new CborDecodeOptions { RetainSourceRanges = true };
        ChunkedReadStream input = new ChunkedReadStream([0x01, 0x82, 0x02, 0x03], 4);
        CborStreamReader reader = new CborStreamReader(input, options);

        CborReadResult first = reader.ReadNext();
        CborReadResult second = reader.ReadNext();

        Assert.Multiple(() =>
        {
            Assert.That(first.Value?.Representation?.SourceOffset, Is.Zero);
            Assert.That(first.Value?.Representation?.SourceLength, Is.EqualTo(1));
            Assert.That(second.Value?.Representation?.SourceOffset, Is.EqualTo(1));
            Assert.That(second.Value?.Representation?.SourceLength, Is.EqualTo(3));
            Assert.That(second.Value?.ArrayItems?[0].Representation?.SourceOffset, Is.EqualTo(2));
            Assert.That(second.Value?.ArrayItems?[1].Representation?.SourceOffset, Is.EqualTo(3));
        });
    }

    [Test]
    public void DecoderLimitsResetForEveryStreamItem()
    {
        CborDecodeOptions options = new CborDecodeOptions
        {
            MaxTotalBytes = 1,
            MaxTotalItems = 1
        };
        CborStreamReader reader = new CborStreamReader(
            new ChunkedReadStream([0x00, 0x01], 2),
            options
        );

        CborReadResult first = reader.ReadNext();
        CborReadResult second = reader.ReadNext();

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(first.Value?.IntegerValue, Is.EqualTo(BigInteger.Zero));
            Assert.That(second.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(second.Value?.IntegerValue, Is.EqualTo(BigInteger.One));
        });
    }

    [Test]
    public void PartialItemsAtEndOfStreamReportTruncation()
    {
        byte[][] encodings =
        [
            [0x1a, 0x00, 0x00],
            [0x82, 0x01],
            [0x5f, 0x42, 0x01],
            [0xc1]
        ];

        foreach (byte[] encoding in encodings)
        {
            CborStreamReader reader = new CborStreamReader(
                new ChunkedReadStream(encoding, 1),
                new CborDecodeOptions()
            );

            CborReadResult failure = reader.ReadNext();
            CborReadResult afterFailure = reader.ReadNext();

            Assert.Multiple(() =>
            {
                Assert.That(failure.Status, Is.EqualTo(CborReadStatus.Error));
                Assert.That(failure.ErrorCode, Is.EqualTo("CBOR_UNEXPECTED_END_OF_DATA"));
                Assert.That(failure.ErrorOffset, Is.EqualTo(encoding.Length));
                Assert.That(failure.Consumed, Is.EqualTo(encoding.Length));
                Assert.That(afterFailure.Status, Is.EqualTo(CborReadStatus.Error));
                Assert.That(afterFailure.ErrorCode, Is.EqualTo("CBOR_STREAM_READER_FAULTED"));
            });
        }
    }

    [Test]
    public void MalformedItemAfterACompleteItemUsesAbsoluteErrorOffsets()
    {
        CborDecodeOptions options = new CborDecodeOptions { RetainSourceRanges = true };
        CborStreamReader reader = new CborStreamReader(
            new ChunkedReadStream([0x00, 0x63, 0xe2, 0x28, 0xa1], 5),
            options
        );

        CborReadResult first = reader.ReadNext();
        CborReadResult failure = reader.ReadNext();

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(failure.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(failure.ErrorCode, Is.EqualTo("CBOR_UTF8_INVALID_CONTINUATION"));
            Assert.That(failure.ErrorOffset, Is.EqualTo(3));
            Assert.That(failure.Remaining, Is.EqualTo(4));
        });
    }

    private sealed class ChunkedReadStream : Stream
    {
        private readonly byte[] source;
        private readonly int maximumRead;
        private int sourceOffset;

        public ChunkedReadStream(byte[] source, int maximumRead)
        {
            this.source = source;
            this.maximumRead = maximumRead;
        }

        public int ReadCalls { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCalls++;
            int remaining = source.Length - sourceOffset;
            if (remaining == 0)
            {
                return 0;
            }
            int read = Math.Min(remaining, Math.Min(count, maximumRead));
            source.AsSpan(sourceOffset, read).CopyTo(buffer.AsSpan(offset, read));
            sourceOffset += read;
            return read;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
