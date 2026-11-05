namespace ftp.Tests;

public class LocalByteTransferCodecTests
{
    [Test]
    public void Twelve_bit_values_pack_msb_first_and_round_trip_across_boundaries()
    {
        byte[] localBytes = new byte[] { 0x0A, 0xBC, 0x01, 0x23 };
        byte[] wireBytes = new byte[] { 0xAB, 0xC1, 0x23 };
        TrackingWriteStream encoded = new TrackingWriteStream();
        FtpTransferCodecRequest encode = CreateRequest(
            new ChunkedReadStream(localBytes, 1), encoded, 12, 2, false, 0, true, true);

        long encodedCount = LocalByteTransferCodec.EncodeStream(encode);

        TrackingWriteStream decoded = new TrackingWriteStream();
        FtpTransferCodecRequest decode = CreateRequest(
            new ChunkedReadStream(wireBytes, 1), decoded, 12, 2, false, 0, true, true);
        long decodedCount = LocalByteTransferCodec.DecodeStream(decode);

        Assert.Multiple(() =>
        {
            Assert.That(encodedCount, Is.EqualTo(wireBytes.Length));
            Assert.That(encoded.ToArray(), Is.EqualTo(wireBytes));
            Assert.That(decodedCount, Is.EqualTo(localBytes.Length));
            Assert.That(decoded.ToArray(), Is.EqualTo(localBytes));
            Assert.That(encoded.LargestWrite, Is.LessThanOrEqualTo(2));
            Assert.That(decoded.LargestWrite, Is.LessThanOrEqualTo(2));
        });
    }

    [Test]
    public void Thirty_six_bit_values_match_the_rfc_packing_example_shape()
    {
        byte[] localBytes = new byte[]
        {
            0x0A, 0xBC, 0xDE, 0xF1, 0x23,
            0x01, 0x23, 0x45, 0x67, 0x89
        };
        byte[] wireBytes = new byte[] { 0xAB, 0xCD, 0xEF, 0x12, 0x31, 0x23, 0x45, 0x67, 0x89 };
        MemoryStream encoded = new MemoryStream();
        FtpTransferCodecRequest encode = CreateRequest(
            new ChunkedReadStream(localBytes, 1), encoded, 36, 3, false, 0, true, true);
        MemoryStream decoded = new MemoryStream();
        FtpTransferCodecRequest decode = CreateRequest(
            new ChunkedReadStream(wireBytes, 1), decoded, 36, 3, false, 0, true, true);

        LocalByteTransferCodec.EncodeStream(encode);
        LocalByteTransferCodec.DecodeStream(decode);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.ToArray(), Is.EqualTo(wireBytes));
            Assert.That(decoded.ToArray(), Is.EqualTo(localBytes));
        });
    }

    [Test]
    public void Sub_byte_values_use_the_expected_count_to_remove_ambiguous_padding()
    {
        byte[] localBytes = new byte[] { 5, 0, 7 };
        byte[] wireBytes = new byte[] { 0xA3, 0x80 };
        MemoryStream encoded = new MemoryStream();
        FtpTransferCodecRequest encode = CreateRequest(
            new ChunkedReadStream(localBytes, 1), encoded, 3, 2, true, 3, true, true);
        MemoryStream decoded = new MemoryStream();
        FtpTransferCodecRequest decode = CreateRequest(
            new ChunkedReadStream(wireBytes, 1), decoded, 3, 2, true, 3, true, true);

        LocalByteTransferCodec.EncodeStream(encode);
        LocalByteTransferCodec.DecodeStream(decode);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.ToArray(), Is.EqualTo(wireBytes));
            Assert.That(decoded.ToArray(), Is.EqualTo(localBytes));
        });
    }

    [Test]
    public void Maximum_width_values_round_trip_without_fixed_integer_storage()
    {
        byte[] localBytes = new byte[64];
        int index = 0;
        while (index < localBytes.Length)
        {
            localBytes[index] = (byte)(index * 3);
            index++;
        }

        localBytes[0] = (byte)(localBytes[0] & 0x7F);
        localBytes[32] = (byte)(localBytes[32] & 0x7F);
        MemoryStream encoded = new MemoryStream();
        FtpTransferCodecRequest encode = CreateRequest(
            new ChunkedReadStream(localBytes, 1), encoded, 255, 5, false, 0, true, true);
        MemoryStream decoded = new MemoryStream();

        LocalByteTransferCodec.EncodeStream(encode);

        FtpTransferCodecRequest decode = CreateRequest(
            new ChunkedReadStream(encoded.ToArray(), 1), decoded, 255, 5, false, 0, true, true);
        LocalByteTransferCodec.DecodeStream(decode);

        Assert.That(decoded.ToArray(), Is.EqualTo(localBytes));
    }

    [Test]
    public void Encoder_rejects_high_storage_padding_and_incomplete_logical_values()
    {
        FtpTransferCodecRequest highPadding = CreateRequest(
            new ChunkedReadStream(new byte[] { 0xFA, 0xBC }, 1), new MemoryStream(),
            12, 2, false, 0, true, true);
        FtpTransferCodecRequest incomplete = CreateRequest(
            new ChunkedReadStream(new byte[] { 0x0A }, 1), new MemoryStream(),
            12, 2, false, 0, true, true);
        FtpTransferCodecRequest countMismatch = CreateRequest(
            new ChunkedReadStream(new byte[] { 0x0A, 0xBC }, 1), new MemoryStream(),
            12, 2, true, 2, true, true);

        FtpTransferException? highPaddingFailure = Assert.Throws<FtpTransferException>(() => LocalByteTransferCodec.EncodeStream(highPadding));
        FtpTransferException? incompleteFailure = Assert.Throws<FtpTransferException>(() => LocalByteTransferCodec.EncodeStream(incomplete));
        FtpTransferException? countFailure = Assert.Throws<FtpTransferException>(() => LocalByteTransferCodec.EncodeStream(countMismatch));

        Assert.Multiple(() =>
        {
            Assert.That(highPaddingFailure!.InnerException, Is.TypeOf<InvalidDataException>());
            Assert.That(incompleteFailure!.InnerException, Is.TypeOf<InvalidDataException>());
            Assert.That(countFailure!.InnerException, Is.TypeOf<InvalidDataException>());
        });
    }

    [Test]
    public void Decoder_rejects_nonzero_padding_truncation_and_count_mismatch()
    {
        FtpTransferCodecRequest nonzeroPadding = CreateRequest(
            new ChunkedReadStream(new byte[] { 0xAB, 0xC1 }, 1), new MemoryStream(),
            12, 2, false, 0, true, true);
        FtpTransferCodecRequest truncated = CreateRequest(
            new ChunkedReadStream(new byte[] { 0xAB }, 1), new MemoryStream(),
            12, 2, false, 0, true, true);
        FtpTransferCodecRequest missingLogicalByte = CreateRequest(
            new ChunkedReadStream(new byte[] { 0xAB, 0xC0 }, 1), new MemoryStream(),
            12, 2, true, 2, true, true);

        FtpTransferException? paddingFailure = Assert.Throws<FtpTransferException>(() => LocalByteTransferCodec.DecodeStream(nonzeroPadding));
        FtpTransferException? truncatedFailure = Assert.Throws<FtpTransferException>(() => LocalByteTransferCodec.DecodeStream(truncated));
        FtpTransferException? countFailure = Assert.Throws<FtpTransferException>(() => LocalByteTransferCodec.DecodeStream(missingLogicalByte));

        Assert.Multiple(() =>
        {
            Assert.That(paddingFailure!.InnerException, Is.TypeOf<InvalidDataException>());
            Assert.That(truncatedFailure!.InnerException, Is.TypeOf<InvalidDataException>());
            Assert.That(countFailure!.InnerException, Is.TypeOf<InvalidDataException>());
        });
    }

    [Test]
    public void Sub_byte_decode_requires_a_count_and_type_l_eight_uses_the_zero_conversion_path()
    {
        FtpTransferCodecRequest missingCount = CreateRequest(
            new ChunkedReadStream(new byte[] { 0xA0 }, 1), new MemoryStream(),
            3, 2, false, 0, true, true);
        FtpTransferCodecRequest eightBit = CreateRequest(
            new ChunkedReadStream(new byte[] { 0xA0 }, 1), new MemoryStream(),
            8, 2, false, 0, true, true);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => LocalByteTransferCodec.DecodeStream(missingCount));
            Assert.Throws<FtpConfigurationException>(() => LocalByteTransferCodec.EncodeStream(eightBit));
        });
    }

    [Test]
    public void Owned_streams_are_closed_when_packing_fails()
    {
        TrackingReadStream source = new TrackingReadStream(new byte[] { 0xFA, 0xBC }, 1);
        TrackingWriteStream destination = new TrackingWriteStream();
        FtpTransferCodecRequest request = CreateRequest(
            source, destination, 12, 2, false, 0, false, false);

        Assert.Throws<FtpTransferException>(() => LocalByteTransferCodec.EncodeStream(request));

        Assert.Multiple(() =>
        {
            Assert.That(source.Disposed, Is.True);
            Assert.That(destination.Disposed, Is.True);
        });
    }

    private static FtpTransferCodecRequest CreateRequest(
        Stream source,
        Stream destination,
        int localByteSize,
        int bufferBytes,
        bool hasLogicalByteCount,
        long logicalByteCount,
        bool leaveSourceOpen,
        bool leaveDestinationOpen)
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Type = FtpTransferType.LocalByte;
        options.HasLocalByteSize = true;
        options.LocalByteSize = localByteSize;
        options.BufferBytes = bufferBytes;

        FtpTransferCodecRequest request = new FtpTransferCodecRequest();
        request.Source = source;
        request.Destination = destination;
        request.Options = options;
        request.HasLogicalByteCount = hasLogicalByteCount;
        request.LogicalByteCount = logicalByteCount;
        request.LeaveSourceOpen = leaveSourceOpen;
        request.LeaveDestinationOpen = leaveDestinationOpen;
        return request;
    }

    private class ChunkedReadStream : MemoryStream
    {
        private readonly int maximumChunk;

        public ChunkedReadStream(byte[] content, int maximumChunk)
            : base(content)
        {
            this.maximumChunk = maximumChunk;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return base.Read(buffer, offset, Math.Min(count, maximumChunk));
        }
    }

    private class TrackingReadStream : MemoryStream
    {
        private readonly int maximumChunk;

        public bool Disposed;

        public TrackingReadStream(byte[] content, int maximumChunk)
            : base(content)
        {
            this.maximumChunk = maximumChunk;
            Disposed = false;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
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

        public TrackingWriteStream()
        {
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
        }
    }
}
