using System;
using System.IO;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class StreamSessionTransportTests
{
    [Test]
    public void ConstructorRequiresCallerSuppliedDuplexStream()
    {
        CapabilityStream unreadable = new CapabilityStream(false, true);
        CapabilityStream unwritable = new CapabilityStream(true, false);

        Assert.Multiple(() =>
        {
            Assert.Throws<FixTransportException>(() => new StreamSessionTransport(null!));
            Assert.Throws<FixTransportException>(() => new StreamSessionTransport(unreadable));
            Assert.Throws<FixTransportException>(() => new StreamSessionTransport(unwritable));
        });
    }

    [Test]
    public void OpenIsIdempotentForCallerSuppliedConnectedStream()
    {
        ChunkedDuplexStream stream = new ChunkedDuplexStream([1, 2, 3], 2);
        StreamSessionTransport transport = new StreamSessionTransport(stream);

        TransportWriteResult first = transport.OpenTransport(new SessionEndpoint(), new SessionTlsSettings());
        TransportWriteResult second = transport.OpenTransport(new SessionEndpoint(), new SessionTlsSettings());

        Assert.Multiple(() =>
        {
            AssertSuccess(first, TransportState.Connected, 0);
            AssertSuccess(second, TransportState.Connected, 0);
            Assert.That(stream.WasDisposed, Is.False);
        });
    }

    [Test]
    public void ReadsPreservePartialCountsAndOnlyNonEmptyZeroReadSignalsEof()
    {
        ChunkedDuplexStream stream = new ChunkedDuplexStream([10, 11, 12, 13, 14], 2);
        StreamSessionTransport transport = new StreamSessionTransport(stream);
        byte[] destination = new byte[4];

        TransportReadResult empty = transport.ReadTransport(destination.AsSpan(0, 0));
        TransportReadResult first = transport.ReadTransport(destination);
        byte[] firstBytes = destination.AsSpan(0, first.BytesRead).ToArray();
        TransportReadResult second = transport.ReadTransport(destination);
        byte[] secondBytes = destination.AsSpan(0, second.BytesRead).ToArray();
        TransportReadResult third = transport.ReadTransport(destination);
        byte[] thirdBytes = destination.AsSpan(0, third.BytesRead).ToArray();
        TransportReadResult eof = transport.ReadTransport(destination);

        Assert.Multiple(() =>
        {
            AssertRead(empty, TransportState.Connected, 0, false);
            AssertRead(first, TransportState.Connected, 2, false);
            Assert.That(firstBytes, Is.EqualTo(new byte[] { 10, 11 }));
            AssertRead(second, TransportState.Connected, 2, false);
            Assert.That(secondBytes, Is.EqualTo(new byte[] { 12, 13 }));
            AssertRead(third, TransportState.Connected, 1, false);
            Assert.That(thirdBytes, Is.EqualTo(new byte[] { 14 }));
            AssertRead(eof, TransportState.Closed, 0, true);
            Assert.That(stream.WasDisposed, Is.True);
        });
    }

    [Test]
    public void WritesConsumeExactlyTheCallerSuppliedSlice()
    {
        ChunkedDuplexStream stream = new ChunkedDuplexStream([], 2);
        StreamSessionTransport transport = new StreamSessionTransport(stream);
        byte[] source = [9, 20, 21, 22, 8];

        TransportWriteResult empty = transport.WriteTransport(source.AsSpan(0, 0));
        TransportWriteResult written = transport.WriteTransport(source.AsSpan(1, 3));

        Assert.Multiple(() =>
        {
            AssertSuccess(empty, TransportState.Connected, 0);
            AssertSuccess(written, TransportState.Connected, 3);
            Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 20, 21, 22 }));
        });
    }

    [Test]
    public void CloseIsIdempotentAndFurtherOperationsReportClosedState()
    {
        ChunkedDuplexStream stream = new ChunkedDuplexStream([1], 1);
        StreamSessionTransport transport = new StreamSessionTransport(stream);

        TransportWriteResult firstClose = transport.CloseTransport();
        TransportReadResult read = transport.ReadTransport(new byte[1]);
        TransportWriteResult write = transport.WriteTransport([2]);
        TransportWriteResult open = transport.OpenTransport(new SessionEndpoint(), new SessionTlsSettings());
        TransportWriteResult secondClose = transport.CloseTransport();

        Assert.Multiple(() =>
        {
            AssertSuccess(firstClose, TransportState.Closed, 0);
            AssertFailure(read.State, read.ErrorCode, TransportState.Closed, "transport-closed");
            Assert.That(read.BytesRead, Is.Zero);
            Assert.That(read.EndOfStream, Is.False);
            AssertFailure(write.State, write.ErrorCode, TransportState.Closed, "transport-closed");
            AssertFailure(open.State, open.ErrorCode, TransportState.Closed, "transport-closed");
            AssertSuccess(secondClose, TransportState.Closed, 0);
            Assert.That(stream.DisposeCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void LostStreamCapabilityTransitionsTransportToFaulted()
    {
        CapabilityStream stream = new CapabilityStream(true, true);
        StreamSessionTransport transport = new StreamSessionTransport(stream);
        stream.AllowWrite = false;

        TransportWriteResult write = transport.WriteTransport([1]);
        TransportReadResult read = transport.ReadTransport(new byte[1]);
        TransportWriteResult open = transport.OpenTransport(new SessionEndpoint(), new SessionTlsSettings());

        Assert.Multiple(() =>
        {
            AssertFailure(write.State, write.ErrorCode, TransportState.Faulted, "stream-not-writable");
            AssertFailure(read.State, read.ErrorCode, TransportState.Faulted, "transport-faulted");
            AssertFailure(open.State, open.ErrorCode, TransportState.Faulted, "transport-faulted");
        });
    }

    [Test]
    public void OpenAndReadDetectCapabilitiesLostAfterConstruction()
    {
        CapabilityStream openStream = new CapabilityStream(true, true);
        StreamSessionTransport openTransport = new StreamSessionTransport(openStream);
        openStream.AllowRead = false;
        CapabilityStream readStream = new CapabilityStream(true, true);
        StreamSessionTransport readTransport = new StreamSessionTransport(readStream);
        readStream.AllowRead = false;

        TransportWriteResult open = openTransport.OpenTransport(new SessionEndpoint(), new SessionTlsSettings());
        TransportReadResult read = readTransport.ReadTransport(new byte[1]);

        Assert.Multiple(() =>
        {
            AssertFailure(open.State, open.ErrorCode, TransportState.Faulted, "stream-not-duplex");
            AssertFailure(read.State, read.ErrorCode, TransportState.Faulted, "stream-not-readable");
        });
    }

    private static void AssertRead(TransportReadResult result, TransportState state, int count, bool eof)
    {
        Assert.That(result.State, Is.EqualTo(state));
        Assert.That(result.BytesRead, Is.EqualTo(count));
        Assert.That(result.EndOfStream, Is.EqualTo(eof));
        Assert.That(result.WouldBlock, Is.False);
        Assert.That(result.ErrorCode, Is.Empty);
        Assert.That(result.ErrorMessage, Is.Empty);
    }

    private static void AssertSuccess(TransportWriteResult result, TransportState state, int count)
    {
        Assert.That(result.State, Is.EqualTo(state));
        Assert.That(result.BytesWritten, Is.EqualTo(count));
        Assert.That(result.WouldBlock, Is.False);
        Assert.That(result.ErrorCode, Is.Empty);
        Assert.That(result.ErrorMessage, Is.Empty);
    }

    private static void AssertFailure(TransportState actualState, string actualCode, TransportState state, string code)
    {
        Assert.That(actualState, Is.EqualTo(state));
        Assert.That(actualCode, Is.EqualTo(code));
    }

    private class ChunkedDuplexStream : MemoryStream
    {
        private readonly int MaximumReadCount;

        public ChunkedDuplexStream(byte[] bytes, int maximumReadCount) : base()
        {
            MaximumReadCount = maximumReadCount;
            Write(bytes, 0, bytes.Length);
            Position = 0;
        }

        public bool WasDisposed { get; private set; }

        public int DisposeCount { get; private set; }

        public override int Read(Span<byte> buffer)
        {
            return base.Read(buffer.Slice(0, Math.Min(buffer.Length, MaximumReadCount)));
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCount++;
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    private class CapabilityStream : MemoryStream
    {
        public CapabilityStream(bool allowRead, bool allowWrite)
        {
            AllowRead = allowRead;
            AllowWrite = allowWrite;
        }

        public bool AllowRead { get; set; }

        public bool AllowWrite { get; set; }

        public override bool CanRead => AllowRead && base.CanRead;

        public override bool CanWrite => AllowWrite && base.CanWrite;
    }
}
