namespace ftp.Tests;

public class AsciiTransferCodecTests
{
    [Test]
    public void Encoder_writes_network_ascii_across_partial_read_boundaries()
    {
        byte[] localBytes = new byte[] { 65, 10, 66, 13, 67, 13, 10, 68, 13, 0, 69, 13 };
        byte[] networkBytes = new byte[] { 65, 13, 10, 66, 13, 0, 67, 13, 10, 68, 13, 0, 0, 69, 13, 0 };
        TrackingReadStream source = new TrackingReadStream(localBytes, 1);
        TrackingWriteStream destination = new TrackingWriteStream();
        FtpTransferCodecRequest request = CreateRequest(source, destination, FtpTransferType.Ascii, FtpTextFormat.NonPrint, 3, true, true);

        long bytesWritten = AsciiTransferCodec.EncodeStream(request);

        Assert.Multiple(() =>
        {
            Assert.That(bytesWritten, Is.EqualTo(networkBytes.Length));
            Assert.That(destination.ToArray(), Is.EqualTo(networkBytes));
            Assert.That(source.ReadCalls, Is.GreaterThan(localBytes.Length));
            Assert.That(destination.LargestWrite, Is.LessThanOrEqualTo(3));
            Assert.That(destination.WriteCalls, Is.GreaterThan(1));
            Assert.That(source.Disposed, Is.False);
            Assert.That(destination.Disposed, Is.False);
        });
    }

    [Test]
    public void Decoder_restores_line_feeds_and_literal_carriage_returns_across_boundaries()
    {
        byte[] networkBytes = new byte[] { 65, 13, 10, 66, 13, 0, 67, 10, 0 };
        byte[] localBytes = new byte[] { 65, 10, 66, 13, 67, 10, 0 };
        if (Environment.NewLine.Length == 2)
        {
            localBytes = new byte[] { 65, 13, 10, 66, 13, 67, 10, 0 };
        }
        TrackingReadStream source = new TrackingReadStream(networkBytes, 1);
        TrackingWriteStream destination = new TrackingWriteStream();
        FtpTransferCodecRequest request = CreateRequest(source, destination, FtpTransferType.Ascii, FtpTextFormat.NonPrint, 2, true, true);

        long bytesWritten = AsciiTransferCodec.DecodeStream(request);

        Assert.Multiple(() =>
        {
            Assert.That(bytesWritten, Is.EqualTo(localBytes.Length));
            Assert.That(destination.ToArray(), Is.EqualTo(localBytes));
            Assert.That(source.ReadCalls, Is.GreaterThan(networkBytes.Length));
            Assert.That(destination.LargestWrite, Is.LessThanOrEqualTo(2));
        });
    }

    [Test]
    public void Encoder_and_decoder_reject_bytes_outside_seven_bit_ascii()
    {
        TrackingWriteStream encodedDestination = new TrackingWriteStream();
        TrackingWriteStream decodedDestination = new TrackingWriteStream();
        FtpTransferCodecRequest encode = CreateRequest(
            new TrackingReadStream(new byte[] { 128 }, 1), encodedDestination,
            FtpTransferType.Ascii, FtpTextFormat.NonPrint, 4, true, true);
        FtpTransferCodecRequest decode = CreateRequest(
            new TrackingReadStream(new byte[] { 255 }, 1), decodedDestination,
            FtpTransferType.Ascii, FtpTextFormat.NonPrint, 4, true, true);

        FtpTransferException? encodeFailure = Assert.Throws<FtpTransferException>(() => AsciiTransferCodec.EncodeStream(encode));
        FtpTransferException? decodeFailure = Assert.Throws<FtpTransferException>(() => AsciiTransferCodec.DecodeStream(decode));

        Assert.Multiple(() =>
        {
            Assert.That(encodeFailure!.InnerException, Is.TypeOf<InvalidDataException>());
            Assert.That(decodeFailure!.InnerException, Is.TypeOf<InvalidDataException>());
        });
    }

    [Test]
    public void Decoder_rejects_invalid_and_incomplete_carriage_return_sequences()
    {
        FtpTransferCodecRequest invalid = CreateRequest(
            new TrackingReadStream(new byte[] { 13, 65 }, 1), new TrackingWriteStream(),
            FtpTransferType.Ascii, FtpTextFormat.NonPrint, 2, true, true);
        FtpTransferCodecRequest incomplete = CreateRequest(
            new TrackingReadStream(new byte[] { 13 }, 1), new TrackingWriteStream(),
            FtpTransferType.Ascii, FtpTextFormat.NonPrint, 2, true, true);

        FtpTransferException? invalidFailure = Assert.Throws<FtpTransferException>(() => AsciiTransferCodec.DecodeStream(invalid));
        FtpTransferException? incompleteFailure = Assert.Throws<FtpTransferException>(() => AsciiTransferCodec.DecodeStream(incomplete));

        Assert.Multiple(() =>
        {
            Assert.That(invalidFailure!.InnerException, Is.TypeOf<InvalidDataException>());
            Assert.That(incompleteFailure!.InnerException, Is.TypeOf<InvalidDataException>());
        });
    }

    [Test]
    public void Codec_rejects_non_ascii_representation_types()
    {
        FtpTransferCodecRequest image = CreateRequest(
            new TrackingReadStream(new byte[] { 65 }, 1), new TrackingWriteStream(),
            FtpTransferType.Image, FtpTextFormat.NonPrint, 4, true, true);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => AsciiTransferCodec.EncodeStream(image));
            Assert.Throws<FtpConfigurationException>(() => AsciiTransferCodec.DecodeStream(image));
        });
    }

    [Test]
    public void Owned_streams_are_closed_after_success_and_conversion_failure()
    {
        TrackingReadStream successfulSource = new TrackingReadStream(new byte[] { 65 }, 1);
        TrackingWriteStream successfulDestination = new TrackingWriteStream();
        FtpTransferCodecRequest successful = CreateRequest(
            successfulSource, successfulDestination,
            FtpTransferType.Ascii, FtpTextFormat.NonPrint, 4, false, false);
        TrackingReadStream failingSource = new TrackingReadStream(new byte[] { 13 }, 1);
        TrackingWriteStream failingDestination = new TrackingWriteStream();
        FtpTransferCodecRequest failing = CreateRequest(
            failingSource, failingDestination,
            FtpTransferType.Ascii, FtpTextFormat.NonPrint, 4, false, false);

        AsciiTransferCodec.EncodeStream(successful);
        Assert.Throws<FtpTransferException>(() => AsciiTransferCodec.DecodeStream(failing));

        Assert.Multiple(() =>
        {
            Assert.That(successfulSource.Disposed, Is.True);
            Assert.That(successfulDestination.Disposed, Is.True);
            Assert.That(failingSource.Disposed, Is.True);
            Assert.That(failingDestination.Disposed, Is.True);
        });
    }

    private static FtpTransferCodecRequest CreateRequest(
        Stream source,
        Stream destination,
        FtpTransferType type,
        FtpTextFormat format,
        int bufferBytes,
        bool leaveSourceOpen,
        bool leaveDestinationOpen)
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Type = type;
        options.TextFormat = format;
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

        public bool Disposed;
        public int ReadCalls;

        public TrackingReadStream(byte[] content, int maximumChunk)
            : base(content)
        {
            this.maximumChunk = maximumChunk;
            Disposed = false;
            ReadCalls = 0;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCalls++;
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
        public bool Disposed;
        public int LargestWrite;
        public int WriteCalls;

        public TrackingWriteStream()
        {
            Disposed = false;
            LargestWrite = 0;
            WriteCalls = 0;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteCalls++;
            LargestWrite = Math.Max(LargestWrite, count);
            base.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
