namespace ftp.Tests;

public class ModeZCodecTests
{
    [Test]
    public void Repetitive_data_round_trips_through_built_in_deflate_streams()
    {
        byte[] content = new byte[8192];
        int index = 0;
        while (index < content.Length)
        {
            content[index] = (byte)(index % 4);
            index++;
        }

        MemoryStream encoded = new MemoryStream();
        FtpTransferCodecRequest encode = CreateRequest(
            new MemoryStream(content), encoded, 97, true, true);

        long encodedLogicalBytes = ModeZCodec.EncodeStream(encode, true);

        byte[] compressed = encoded.ToArray();
        MemoryStream decoded = new MemoryStream();
        FtpTransferCodecRequest decode = CreateRequest(
            new MemoryStream(compressed), decoded, 113, true, true);
        long decodedLogicalBytes = ModeZCodec.DecodeStream(decode, true);

        Assert.Multiple(() =>
        {
            Assert.That(encodedLogicalBytes, Is.EqualTo(content.Length));
            Assert.That(decodedLogicalBytes, Is.EqualTo(content.Length));
            Assert.That(compressed.Length, Is.LessThan(content.Length));
            Assert.That(compressed[0], Is.EqualTo(0x78));
            Assert.That(decoded.ToArray(), Is.EqualTo(content));
            Assert.That(encoded.CanWrite, Is.True);
            Assert.That(decoded.CanWrite, Is.True);
        });
    }

    [Test]
    public void Every_byte_value_round_trips_across_small_logical_buffers()
    {
        byte[] content = new byte[1024];
        int index = 0;
        while (index < content.Length)
        {
            content[index] = (byte)index;
            index++;
        }

        TrackingReadStream source = new TrackingReadStream(content, 3, null);
        MemoryStream encoded = new MemoryStream();
        ModeZCodec.EncodeStream(CreateRequest(source, encoded, 11, true, true), true);
        TrackingWriteStream decoded = new TrackingWriteStream(null);

        ModeZCodec.DecodeStream(CreateRequest(
            new MemoryStream(encoded.ToArray()), decoded, 7, true, true), true);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.ToArray(), Is.EqualTo(content));
            Assert.That(source.ReadCalls, Is.GreaterThan(1));
            Assert.That(source.LargestRequestedRead, Is.LessThanOrEqualTo(11));
            Assert.That(decoded.LargestWrite, Is.LessThanOrEqualTo(7));
        });
    }

    [Test]
    public void Empty_input_produces_a_complete_deflate_stream_and_round_trips()
    {
        MemoryStream encoded = new MemoryStream();

        long encodedCount = ModeZCodec.EncodeStream(CreateRequest(
            new MemoryStream(), encoded, 16, true, true), true);
        MemoryStream decoded = new MemoryStream();
        long decodedCount = ModeZCodec.DecodeStream(CreateRequest(
            new MemoryStream(encoded.ToArray()), decoded, 16, true, true), true);

        Assert.Multiple(() =>
        {
            Assert.That(encodedCount, Is.Zero);
            Assert.That(decodedCount, Is.Zero);
            Assert.That(decoded.ToArray(), Is.Empty);
        });
    }

    [Test]
    public void Mode_z_must_be_enabled_and_is_not_rfc_959_mode_c()
    {
        FtpTransferCodecRequest disabled = CreateRequest(
            new MemoryStream(new byte[] { 1 }), new MemoryStream(), 8, true, true);
        FtpTransferCodecRequest modeC = CreateRequest(
            new MemoryStream(new byte[] { 1 }), new MemoryStream(), 8, true, true);
        modeC.Options!.Mode = FtpTransferMode.Compressed;
        FtpTransferOptions deflateOptions = new FtpTransferOptions();
        deflateOptions.Mode = FtpTransferMode.Deflate;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => ModeZCodec.EncodeStream(disabled, false));
            Assert.Throws<FtpConfigurationException>(() => ModeZCodec.EncodeStream(modeC, true));
            Assert.Throws<FtpConfigurationException>(() => CompressedModeCodec.CreateFramingState(deflateOptions));
        });
    }

    [Test]
    public void Caller_and_codec_owned_stream_lifetimes_are_independent()
    {
        TrackingReadStream callerSource = new TrackingReadStream(
            new byte[] { 1, 2, 3 }, 3, null);
        TrackingWriteStream ownedDestination = new TrackingWriteStream(null);

        ModeZCodec.EncodeStream(CreateRequest(
            callerSource, ownedDestination, 8, true, false), true);

        TrackingReadStream ownedSource = new TrackingReadStream(
            ownedDestination.ToArray(), 3, null);
        TrackingWriteStream callerDestination = new TrackingWriteStream(null);
        ModeZCodec.DecodeStream(CreateRequest(
            ownedSource, callerDestination, 8, false, true), true);

        Assert.Multiple(() =>
        {
            Assert.That(callerSource.Disposed, Is.False);
            Assert.That(ownedDestination.Disposed, Is.True);
            Assert.That(ownedSource.Disposed, Is.True);
            Assert.That(callerDestination.Disposed, Is.False);
            Assert.That(callerDestination.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void Owned_streams_are_cleaned_up_after_copy_and_cleanup_failures()
    {
        IOException readFailure = new IOException("read failed");
        TrackingReadStream failedSource = new TrackingReadStream(
            new byte[] { 1 }, 1, readFailure);
        TrackingWriteStream destination = new TrackingWriteStream(null);
        FtpTransferException? copyError = Assert.Throws<FtpTransferException>(() =>
            ModeZCodec.EncodeStream(CreateRequest(
                failedSource, destination, 8, false, false), true));

        IOException cleanupFailure = new IOException("close failed");
        TrackingReadStream source = new TrackingReadStream(
            new byte[] { 1, 2 }, 2, null);
        TrackingWriteStream failedDestination = new TrackingWriteStream(cleanupFailure);
        FtpTransferException? cleanupError = Assert.Throws<FtpTransferException>(() =>
            ModeZCodec.EncodeStream(CreateRequest(
                source, failedDestination, 8, false, false), true));

        Assert.Multiple(() =>
        {
            Assert.That(copyError!.InnerException, Is.SameAs(readFailure));
            Assert.That(failedSource.Disposed, Is.True);
            Assert.That(destination.Disposed, Is.True);
            Assert.That(cleanupError!.InnerException, Is.SameAs(cleanupFailure));
            Assert.That(source.Disposed, Is.True);
            Assert.That(failedDestination.Disposed, Is.True);
        });
    }

    [Test]
    public void Invalid_deflate_data_is_reported_as_a_transfer_failure()
    {
        byte[] invalidDeflate = new byte[] { 0x78, 0x9C, 0x07 };
        FtpTransferCodecRequest request = CreateRequest(
            new MemoryStream(invalidDeflate), new MemoryStream(), 8, true, true);

        FtpTransferException? error = Assert.Throws<FtpTransferException>(() =>
            ModeZCodec.DecodeStream(request, true));

        Assert.That(error!.InnerException, Is.TypeOf<InvalidDataException>());
    }

    [Test]
    public void Codec_rejects_missing_same_and_incapable_streams()
    {
        MemoryStream same = new MemoryStream();
        FtpTransferCodecRequest sameRequest = CreateRequest(same, same, 8, true, true);
        FtpTransferCodecRequest missing = new FtpTransferCodecRequest();
        missing.Options!.Mode = FtpTransferMode.Deflate;
        MemoryStream closedSource = new MemoryStream(new byte[] { 1 });
        closedSource.Dispose();
        FtpTransferCodecRequest unreadable = CreateRequest(
            closedSource, new MemoryStream(), 8, true, true);
        MemoryStream closedDestination = new MemoryStream();
        closedDestination.Dispose();
        FtpTransferCodecRequest unwritable = CreateRequest(
            new MemoryStream(new byte[] { 1 }), closedDestination, 8, true, true);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => ModeZCodec.EncodeStream(null, true));
            Assert.Throws<FtpConfigurationException>(() => ModeZCodec.EncodeStream(missing, true));
            Assert.Throws<FtpConfigurationException>(() => ModeZCodec.EncodeStream(sameRequest, true));
            Assert.Throws<FtpConfigurationException>(() => ModeZCodec.EncodeStream(unreadable, true));
            Assert.Throws<FtpConfigurationException>(() => ModeZCodec.EncodeStream(unwritable, true));
        });
    }

    private static FtpTransferCodecRequest CreateRequest(
        Stream source,
        Stream destination,
        int bufferBytes,
        bool leaveSourceOpen,
        bool leaveDestinationOpen)
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Mode = FtpTransferMode.Deflate;
        options.BufferBytes = bufferBytes;
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

        public TrackingReadStream(
            byte[] content,
            int maximumChunk,
            Exception? readFailure)
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
        public int LargestWrite;

        public TrackingWriteStream(Exception? disposeFailure)
        {
            this.disposeFailure = disposeFailure;
            Disposed = false;
            LargestWrite = 0;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            LargestWrite = Math.Max(LargestWrite, count);
            base.Write(buffer, offset, count);
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
