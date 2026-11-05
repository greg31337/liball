using System.Numerics;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborStreamSequenceEdgeTests
{
    [Test]
    public void LargeFragmentedItemRetainsOverreadAfterBufferGrowth()
    {
        byte[] payload = new byte[5000];
        for (int index = 0; index < payload.Length; index++)
        {
            payload[index] = (byte)(index % 251);
        }
        byte[] source = [0x59, 0x13, 0x88, .. payload, 0x01, 0x02];
        PatternReadStream input = new PatternReadStream(source, [4096, 913]);
        CborStreamReader reader = new CborStreamReader(
            input,
            new CborDecodeOptions());

        CborReadResult bytes = reader.ReadNext();
        int readsAfterBytes = input.ReadCalls;
        CborReadResult firstTrailing = reader.ReadNext();
        CborReadResult secondTrailing = reader.ReadNext();
        int readsAfterValues = input.ReadCalls;
        CborReadResult end = reader.ReadNext();

        Assert.Multiple(() =>
        {
            Assert.That(input.CanSeek, Is.False);
            Assert.That(bytes.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(bytes.Consumed, Is.EqualTo(5003));
            Assert.That(bytes.Remaining, Is.EqualTo(2));
            Assert.That(bytes.Value?.ByteStringValue, Is.EqualTo(payload));
            Assert.That(firstTrailing.Value?.IntegerValue, Is.EqualTo(BigInteger.One));
            Assert.That(secondTrailing.Value?.IntegerValue, Is.EqualTo(new BigInteger(2)));
            Assert.That(readsAfterBytes, Is.EqualTo(2));
            Assert.That(readsAfterValues, Is.EqualTo(2));
            Assert.That(end.Status, Is.EqualTo(CborReadStatus.EndOfData));
            Assert.That(input.ReadCalls, Is.EqualTo(3));
        });
    }

    [Test]
    public void SequenceByteLimitAcceptsTheExactBoundary()
    {
        byte[] source = [0x61, 0x78, 0x00];
        List<CborReadResult> exact = new CborDecoder().DecodeSequence(
            source,
            new CborDecodeOptions { MaxSequenceBytes = source.Length });
        List<CborReadResult> shortLimit = new CborDecoder().DecodeSequence(
            source,
            new CborDecodeOptions { MaxSequenceBytes = source.Length - 1 });

        Assert.Multiple(() =>
        {
            Assert.That(exact, Has.Count.EqualTo(2));
            Assert.That(exact.All(result => result.Status == CborReadStatus.Done),
                Is.True);
            Assert.That(exact[1].Remaining, Is.Zero);
            Assert.That(shortLimit, Has.Count.EqualTo(2));
            Assert.That(shortLimit[0].Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(shortLimit[1].Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(shortLimit[1].ErrorCode,
                Is.EqualTo("CBOR_MAX_SEQUENCE_BYTES"));
            Assert.That(shortLimit[1].SourceOffset, Is.EqualTo(2));
        });
    }

    [Test]
    public void SequenceEncodingResetsPerValueLimits()
    {
        CborEncodeOptions options = new CborEncodeOptions
        {
            MaxOutputBytes = 1,
            MaxTotalItems = 1,
            MaxSequenceBytes = 3,
            MaxSequenceItems = 3
        };

        byte[] encoded = new CborEncoder().EncodeSequence(
            [Integer(0), Integer(1), Integer(2)],
            options);

        Assert.That(encoded, Is.EqualTo(new byte[] { 0x00, 0x01, 0x02 }));
    }

    [Test]
    public void LeaveOpenSurvivesPartialWriteFailureWithoutFlushing()
    {
        PartialFailureStream output = new PartialFailureStream();
        CborStreamWriter writer = new CborStreamWriter(
            output,
            new CborEncodeOptions { LeaveOpen = true });

        Assert.Throws<IOException>(() => writer.WriteNext(Integer(1000)));
        writer.Dispose();
        output.ThrowOnWrite = false;
        output.WriteByte(0x02);

        Assert.Multiple(() =>
        {
            Assert.That(output.Disposed, Is.False);
            Assert.That(output.FlushCalls, Is.Zero);
            Assert.That(output.WriteCalls, Is.EqualTo(2));
            Assert.That(output.ToArray(), Is.EqualTo(new byte[] { 0x19, 0x02 }));
        });
    }

    private static CborValue Integer(int value)
    {
        return new CborValue
        {
            Kind = CborValueKind.UnsignedInteger,
            IntegerValue = new BigInteger(value)
        };
    }

    private sealed class PatternReadStream : Stream
    {
        private readonly byte[] source;
        private readonly int[] pattern;
        private int sourceOffset;

        public PatternReadStream(byte[] source, int[] pattern)
        {
            this.source = source;
            this.pattern = pattern;
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
            int patternIndex = ReadCalls % pattern.Length;
            ReadCalls++;
            int remaining = source.Length - sourceOffset;
            if (remaining == 0)
            {
                return 0;
            }
            int read = Math.Min(remaining, Math.Min(count, pattern[patternIndex]));
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

    private sealed class PartialFailureStream : Stream
    {
        private readonly MemoryStream storage = new MemoryStream();

        public bool ThrowOnWrite = true;
        public bool Disposed;
        public int WriteCalls;
        public int FlushCalls;

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => !Disposed;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public byte[] ToArray()
        {
            return storage.ToArray();
        }

        public override void Flush()
        {
            FlushCalls++;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
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
            WriteCalls++;
            if (ThrowOnWrite)
            {
                if (count > 0)
                {
                    storage.Write(buffer, offset, 1);
                }
                throw new IOException("Injected partial write failure.");
            }
            storage.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            storage.Dispose();
            base.Dispose(disposing);
        }
    }
}
