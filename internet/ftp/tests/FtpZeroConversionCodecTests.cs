namespace ftp.Tests;

public class FtpZeroConversionCodecTests
{
    [Test]
    public void Type_image_copies_exact_bytes_in_bounded_chunks_and_leaves_streams_open()
    {
        byte[] content = new byte[] { 0, 1, 2, 13, 10, 255, 128, 42, 7 };
        TrackingReadStream source = new TrackingReadStream(content, 3, null);
        TrackingWriteStream destination = new TrackingWriteStream(null);
        FtpTransferCodecRequest request = CreateRequest(source, destination, FtpTransferType.Image, 0, 4, true, true);

        long copied = FtpZeroConversionCodec.CopyStream(request);

        Assert.Multiple(() =>
        {
            Assert.That(copied, Is.EqualTo(content.Length));
            Assert.That(destination.ToArray(), Is.EqualTo(content));
            Assert.That(source.LargestRequestedRead, Is.LessThanOrEqualTo(4));
            Assert.That(source.ReadCalls, Is.GreaterThan(1));
            Assert.That(source.Disposed, Is.False);
            Assert.That(destination.Disposed, Is.False);
        });
    }

    [Test]
    public void Type_local_byte_eight_copies_without_conversion()
    {
        byte[] content = new byte[] { 0, 13, 10, 127, 128, 255 };
        TrackingReadStream source = new TrackingReadStream(content, 2, null);
        TrackingWriteStream destination = new TrackingWriteStream(null);
        FtpTransferCodecRequest request = CreateRequest(source, destination, FtpTransferType.LocalByte, 8, 2, true, true);

        long copied = FtpZeroConversionCodec.CopyStream(request);

        Assert.Multiple(() =>
        {
            Assert.That(copied, Is.EqualTo(content.Length));
            Assert.That(destination.ToArray(), Is.EqualTo(content));
        });
    }

    [Test]
    public void Owned_streams_are_closed_after_a_successful_copy()
    {
        TrackingReadStream source = new TrackingReadStream(new byte[] { 1, 2, 3 }, 3, null);
        TrackingWriteStream destination = new TrackingWriteStream(null);
        FtpTransferCodecRequest request = CreateRequest(source, destination, FtpTransferType.Image, 0, 8, false, false);

        long copied = FtpZeroConversionCodec.CopyStream(request);

        Assert.Multiple(() =>
        {
            Assert.That(copied, Is.EqualTo(3));
            Assert.That(source.Disposed, Is.True);
            Assert.That(destination.Disposed, Is.True);
        });
    }

    [Test]
    public void Source_and_destination_ownership_are_controlled_independently()
    {
        TrackingReadStream source = new TrackingReadStream(new byte[] { 1, 2, 3 }, 3, null);
        TrackingWriteStream destination = new TrackingWriteStream(null);
        FtpTransferCodecRequest request = CreateRequest(source, destination, FtpTransferType.Image, 0, 8, true, false);

        FtpZeroConversionCodec.CopyStream(request);

        Assert.Multiple(() =>
        {
            Assert.That(source.Disposed, Is.False);
            Assert.That(destination.Disposed, Is.True);
        });
    }

    [Test]
    public void Owned_streams_are_closed_when_copying_fails()
    {
        IOException failure = new IOException("read failed");
        TrackingReadStream source = new TrackingReadStream(new byte[] { 1 }, 1, failure);
        TrackingWriteStream destination = new TrackingWriteStream(null);
        FtpTransferCodecRequest request = CreateRequest(source, destination, FtpTransferType.Image, 0, 8, false, false);

        FtpTransferException? thrown = Assert.Throws<FtpTransferException>(() => FtpZeroConversionCodec.CopyStream(request));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.InnerException, Is.SameAs(failure));
            Assert.That(source.Disposed, Is.True);
            Assert.That(destination.Disposed, Is.True);
        });
    }

    [Test]
    public void Caller_owned_streams_remain_open_when_copying_fails()
    {
        TrackingReadStream source = new TrackingReadStream(new byte[] { 1 }, 1, new IOException("read failed"));
        TrackingWriteStream destination = new TrackingWriteStream(null);
        FtpTransferCodecRequest request = CreateRequest(source, destination, FtpTransferType.Image, 0, 8, true, true);

        Assert.Throws<FtpTransferException>(() => FtpZeroConversionCodec.CopyStream(request));

        Assert.Multiple(() =>
        {
            Assert.That(source.Disposed, Is.False);
            Assert.That(destination.Disposed, Is.False);
        });
    }

    [Test]
    public void Source_cleanup_still_runs_when_destination_cleanup_fails()
    {
        IOException failure = new IOException("close failed");
        TrackingReadStream source = new TrackingReadStream(new byte[] { 1 }, 1, null);
        TrackingWriteStream destination = new TrackingWriteStream(failure);
        FtpTransferCodecRequest request = CreateRequest(source, destination, FtpTransferType.Image, 0, 8, false, false);

        FtpTransferException? thrown = Assert.Throws<FtpTransferException>(() => FtpZeroConversionCodec.CopyStream(request));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.InnerException, Is.SameAs(failure));
            Assert.That(source.Disposed, Is.True);
            Assert.That(destination.Disposed, Is.True);
        });
    }

    [Test]
    public void Unsupported_representation_paths_are_rejected()
    {
        TrackingReadStream source = new TrackingReadStream(new byte[] { 1 }, 1, null);
        TrackingWriteStream destination = new TrackingWriteStream(null);
        FtpTransferCodecRequest ascii = CreateRequest(source, destination, FtpTransferType.Ascii, 0, 8, true, true);
        FtpTransferCodecRequest localByteSeven = CreateRequest(source, destination, FtpTransferType.LocalByte, 7, 8, true, true);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => FtpZeroConversionCodec.CopyStream(ascii));
            Assert.Throws<FtpConfigurationException>(() => FtpZeroConversionCodec.CopyStream(localByteSeven));
        });
    }

    private static FtpTransferCodecRequest CreateRequest(
        Stream source,
        Stream destination,
        FtpTransferType type,
        int localByteSize,
        int bufferBytes,
        bool leaveSourceOpen,
        bool leaveDestinationOpen)
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Type = type;
        options.BufferBytes = bufferBytes;
        if (type == FtpTransferType.LocalByte)
        {
            options.HasLocalByteSize = true;
            options.LocalByteSize = localByteSize;
        }

        FtpTransferCodecRequest request = new FtpTransferCodecRequest();
        request.Source = source;
        request.Destination = destination;
        request.Options = options;
        request.LeaveSourceOpen = leaveSourceOpen;
        request.LeaveDestinationOpen = leaveDestinationOpen;
        return request;
    }

    private class TrackingReadStream : MemoryStream
    {
        private readonly int maximumChunk;
        private readonly Exception? readFailure;

        public bool Disposed;
        public int LargestRequestedRead;
        public int ReadCalls;

        public TrackingReadStream(byte[] content, int maximumChunk, Exception? readFailure)
            : base(content)
        {
            this.maximumChunk = maximumChunk;
            this.readFailure = readFailure;
            Disposed = false;
            LargestRequestedRead = 0;
            ReadCalls = 0;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCalls++;
            LargestRequestedRead = Math.Max(LargestRequestedRead, count);
            if (readFailure != null)
            {
                throw readFailure;
            }

            return base.Read(buffer, offset, Math.Min(count, maximumChunk));
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    private class TrackingWriteStream : MemoryStream
    {
        private readonly Exception? disposeFailure;

        public bool Disposed;

        public TrackingWriteStream(Exception? disposeFailure)
        {
            this.disposeFailure = disposeFailure;
            Disposed = false;
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
            if (disposeFailure != null)
            {
                throw disposeFailure;
            }
        }
    }
}
