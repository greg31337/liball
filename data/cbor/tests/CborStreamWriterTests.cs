using cbor;
using System.Numerics;

namespace cbor.Tests;

[TestFixture]
public class CborStreamWriterTests
{
    [Test]
    public void SuccessiveValuesAreConcatenatedWithoutSeekingOrImplicitFlush()
    {
        TrackingWriteStream output = new TrackingWriteStream();
        CborStreamWriter writer = new CborStreamWriter(output, new CborEncodeOptions());
        CborValue array = new CborValue
        {
            Kind = CborValueKind.Array,
            ArrayItems =
            [
                new CborValue
                {
                    Kind = CborValueKind.SimpleValue,
                    SimpleValue = CborSimpleValues.True
                },
                new CborValue
                {
                    Kind = CborValueKind.TextString,
                    TextStringValue = "x"
                }
            ]
        };

        CborWriteResult first = writer.WriteNext(CreateUnsigned(1000));
        CborWriteResult second = writer.WriteNext(array);
        byte[] beforeFlush = output.ToArray();
        writer.FlushOutput();

        Assert.Multiple(() =>
        {
            Assert.That(output.CanSeek, Is.False);
            Assert.That(first.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(first.Written, Is.EqualTo(3));
            Assert.That(first.Required, Is.EqualTo(3));
            Assert.That(second.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(second.Written, Is.EqualTo(4));
            Assert.That(second.Required, Is.EqualTo(4));
            Assert.That(beforeFlush, Is.EqualTo(new byte[]
            {
                0x19, 0x03, 0xe8, 0x82, 0xf5, 0x61, 0x78
            }));
            Assert.That(output.WriteCalls, Is.EqualTo(2));
            Assert.That(output.FlushCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void EncodingFailureIsAtomicAndDoesNotFaultTheWriter()
    {
        TrackingWriteStream output = new TrackingWriteStream();
        CborStreamWriter writer = new CborStreamWriter(output, new CborEncodeOptions());

        CborWriteResult invalid = writer.WriteNext(new CborValue());
        CborWriteResult valid = writer.WriteNext(CreateUnsigned(1));

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Status, Is.EqualTo(CborWriteStatus.Error));
            Assert.That(invalid.ErrorCode, Is.EqualTo("CBOR_VALUE_KIND_INVALID"));
            Assert.That(invalid.Written, Is.Zero);
            Assert.That(output.WriteCalls, Is.EqualTo(1));
            Assert.That(valid.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(output.ToArray(), Is.EqualTo(new byte[] { 0x01 }));
        });
    }

    [Test]
    public void EncodingLimitsApplyIndependentlyToEachValue()
    {
        CborEncodeOptions options = new CborEncodeOptions
        {
            MaxOutputBytes = 1,
            MaxTotalItems = 1
        };
        TrackingWriteStream output = new TrackingWriteStream();
        CborStreamWriter writer = new CborStreamWriter(output, options);

        CborWriteResult first = writer.WriteNext(CreateUnsigned(0));
        CborWriteResult second = writer.WriteNext(CreateUnsigned(1));

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(second.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(output.ToArray(), Is.EqualTo(new byte[] { 0x00, 0x01 }));
        });
    }

    [Test]
    public void DisposeClosesOwnedStreamsAndIsIdempotent()
    {
        TrackingWriteStream output = new TrackingWriteStream();
        CborStreamWriter writer = new CborStreamWriter(output, new CborEncodeOptions());

        writer.Dispose();
        writer.Dispose();
        CborWriteResult afterDispose = writer.WriteNext(CreateUnsigned(0));
        CborEncodeException flushFailure = Assert.Throws<CborEncodeException>(
            () => writer.FlushOutput()
        )!;

        Assert.Multiple(() =>
        {
            Assert.That(output.Disposed, Is.True);
            Assert.That(output.DisposeCalls, Is.EqualTo(1));
            Assert.That(afterDispose.Status, Is.EqualTo(CborWriteStatus.Error));
            Assert.That(afterDispose.ErrorCode, Is.EqualTo("CBOR_STREAM_WRITER_DISPOSED"));
            Assert.That(flushFailure.ErrorCode, Is.EqualTo("CBOR_STREAM_WRITER_DISPOSED"));
        });
    }

    [Test]
    public void LeaveOpenPreservesTheCallerOwnedStream()
    {
        TrackingWriteStream output = new TrackingWriteStream();
        CborStreamWriter writer = new CborStreamWriter(
            output,
            new CborEncodeOptions { LeaveOpen = true }
        );

        CborWriteResult written = writer.WriteNext(CreateUnsigned(2));
        writer.Dispose();
        output.WriteByte(0x03);

        Assert.Multiple(() =>
        {
            Assert.That(written.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(output.Disposed, Is.False);
            Assert.That(output.DisposeCalls, Is.Zero);
            Assert.That(output.FlushCalls, Is.EqualTo(1));
            Assert.That(output.ToArray(), Is.EqualTo(new byte[] { 0x02, 0x03 }));
        });
    }

    [Test]
    public void WriteFailurePermanentlyFaultsTheWriterWithoutRetryingPartialOutput()
    {
        TrackingWriteStream output = new TrackingWriteStream
        {
            ThrowOnWrite = true,
            WriteOneByteBeforeThrow = true
        };
        CborStreamWriter writer = new CborStreamWriter(output, new CborEncodeOptions());

        Assert.Throws<IOException>(() => writer.WriteNext(CreateUnsigned(1000)));
        CborWriteResult afterFailure = writer.WriteNext(CreateUnsigned(1));
        CborEncodeException flushFailure = Assert.Throws<CborEncodeException>(
            () => writer.FlushOutput()
        )!;

        Assert.Multiple(() =>
        {
            Assert.That(output.ToArray(), Is.EqualTo(new byte[] { 0x19 }));
            Assert.That(output.WriteCalls, Is.EqualTo(1));
            Assert.That(afterFailure.Status, Is.EqualTo(CborWriteStatus.Error));
            Assert.That(afterFailure.ErrorCode, Is.EqualTo("CBOR_STREAM_WRITER_FAULTED"));
            Assert.That(flushFailure.ErrorCode, Is.EqualTo("CBOR_STREAM_WRITER_FAULTED"));
            Assert.That(output.FlushCalls, Is.Zero);
        });
    }

    [Test]
    public void FlushFailurePermanentlyFaultsTheWriter()
    {
        TrackingWriteStream output = new TrackingWriteStream { ThrowOnFlush = true };
        CborStreamWriter writer = new CborStreamWriter(output, new CborEncodeOptions());
        CborWriteResult written = writer.WriteNext(CreateUnsigned(1));

        Assert.Throws<IOException>(() => writer.FlushOutput());
        CborWriteResult afterFailure = writer.WriteNext(CreateUnsigned(2));

        Assert.Multiple(() =>
        {
            Assert.That(written.Status, Is.EqualTo(CborWriteStatus.Done));
            Assert.That(output.FlushCalls, Is.EqualTo(1));
            Assert.That(output.WriteCalls, Is.EqualTo(1));
            Assert.That(afterFailure.Status, Is.EqualTo(CborWriteStatus.Error));
            Assert.That(afterFailure.ErrorCode, Is.EqualTo("CBOR_STREAM_WRITER_FAULTED"));
        });
    }

    [Test]
    public void NonWritableStreamProducesAStableStateFailure()
    {
        MemoryStream output = new MemoryStream([], false);
        CborStreamWriter writer = new CborStreamWriter(output, new CborEncodeOptions());

        CborWriteResult first = writer.WriteNext(CreateUnsigned(0));
        CborWriteResult second = writer.WriteNext(CreateUnsigned(0));

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(CborWriteStatus.Error));
            Assert.That(first.ErrorCode, Is.EqualTo("CBOR_STREAM_NOT_WRITABLE"));
            Assert.That(second.Status, Is.EqualTo(CborWriteStatus.Error));
            Assert.That(second.ErrorCode, Is.EqualTo("CBOR_STREAM_WRITER_FAULTED"));
        });
    }

    private static CborValue CreateUnsigned(int value)
    {
        return new CborValue
        {
            Kind = CborValueKind.UnsignedInteger,
            IntegerValue = new BigInteger(value)
        };
    }

    private sealed class TrackingWriteStream : Stream
    {
        private readonly MemoryStream storage = new MemoryStream();

        public bool ThrowOnWrite;
        public bool WriteOneByteBeforeThrow;
        public bool ThrowOnFlush;
        public bool Disposed;
        public int WriteCalls;
        public int FlushCalls;
        public int DisposeCalls;

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
            if (ThrowOnFlush)
            {
                throw new IOException("Injected flush failure.");
            }
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
                if (WriteOneByteBeforeThrow && count > 0)
                {
                    storage.Write(buffer, offset, 1);
                }
                throw new IOException("Injected write failure.");
            }
            storage.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCalls++;
            Disposed = true;
            storage.Dispose();
            base.Dispose(disposing);
        }
    }
}
