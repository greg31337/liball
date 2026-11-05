namespace ftp.Tests;

public class AsciiFormatControlTests
{
    [Test]
    public void Telnet_format_preserves_vertical_controls_and_applies_network_ascii_framing()
    {
        byte[] localBytes = new byte[] { 65, 11, 12, 13, 66, 10, 67 };
        byte[] decodedBytes = localBytes;
        if (Environment.NewLine.Length == 2)
        {
            decodedBytes = new byte[] { 65, 11, 12, 13, 66, 13, 10, 67 };
        }

        byte[] networkBytes = new byte[] { 65, 11, 12, 13, 0, 66, 13, 10, 67 };
        MemoryStream encoded = new MemoryStream();
        FtpTransferCodecRequest encode = CreateRequest(
            new ChunkedReadStream(localBytes, 1), encoded, FtpTextFormat.Telnet, 2);

        long encodedCount = AsciiTransferCodec.EncodeStream(encode);

        MemoryStream decoded = new MemoryStream();
        FtpTransferCodecRequest decode = CreateRequest(
            new ChunkedReadStream(networkBytes, 1), decoded, FtpTextFormat.Telnet, 2);
        long decodedCount = AsciiTransferCodec.DecodeStream(decode);

        Assert.Multiple(() =>
        {
            Assert.That(encodedCount, Is.EqualTo(networkBytes.Length));
            Assert.That(encoded.ToArray(), Is.EqualTo(networkBytes));
            Assert.That(decodedCount, Is.EqualTo(decodedBytes.Length));
            Assert.That(decoded.ToArray(), Is.EqualTo(decodedBytes));
        });
    }

    [Test]
    public void Asa_format_preserves_each_control_character_across_partial_buffers()
    {
        byte[] localBytes = new byte[] { 32, 65, 10, 48, 66, 10, 49, 67, 10, 43, 68 };
        if (Environment.NewLine.Length == 2)
        {
            localBytes = new byte[] { 32, 65, 13, 10, 48, 66, 13, 10, 49, 67, 13, 10, 43, 68 };
        }

        byte[] networkBytes = new byte[] { 32, 65, 13, 10, 48, 66, 13, 10, 49, 67, 13, 10, 43, 68 };
        MemoryStream encoded = new MemoryStream();
        FtpTransferCodecRequest encode = CreateRequest(
            new ChunkedReadStream(localBytes, 1), encoded, FtpTextFormat.CarriageControl, 3);

        long encodedCount = AsciiTransferCodec.EncodeStream(encode);

        MemoryStream decoded = new MemoryStream();
        FtpTransferCodecRequest decode = CreateRequest(
            new ChunkedReadStream(networkBytes, 1), decoded, FtpTextFormat.CarriageControl, 3);
        long decodedCount = AsciiTransferCodec.DecodeStream(decode);

        Assert.Multiple(() =>
        {
            Assert.That(encodedCount, Is.EqualTo(networkBytes.Length));
            Assert.That(encoded.ToArray(), Is.EqualTo(networkBytes));
            Assert.That(decodedCount, Is.EqualTo(localBytes.Length));
            Assert.That(decoded.ToArray(), Is.EqualTo(localBytes));
        });
    }

    [Test]
    public void Asa_encoder_rejects_missing_controls_on_initial_and_later_records()
    {
        byte[] laterRecord = new byte[] { 32, 65, 10, 88, 66 };
        if (Environment.NewLine.Length == 2)
        {
            laterRecord = new byte[] { 32, 65, 13, 10, 88, 66 };
        }

        FtpTransferCodecRequest initial = CreateRequest(
            new ChunkedReadStream(new byte[] { 88, 65 }, 1), new MemoryStream(),
            FtpTextFormat.CarriageControl, 2);
        FtpTransferCodecRequest later = CreateRequest(
            new ChunkedReadStream(laterRecord, 1), new MemoryStream(),
            FtpTextFormat.CarriageControl, 2);

        FtpTransferException? initialFailure = Assert.Throws<FtpTransferException>(() => AsciiTransferCodec.EncodeStream(initial));
        FtpTransferException? laterFailure = Assert.Throws<FtpTransferException>(() => AsciiTransferCodec.EncodeStream(later));

        Assert.Multiple(() =>
        {
            Assert.That(initialFailure!.InnerException, Is.TypeOf<InvalidDataException>());
            Assert.That(laterFailure!.InnerException, Is.TypeOf<InvalidDataException>());
        });
    }

    [Test]
    public void Asa_decoder_rejects_missing_controls_after_network_record_boundaries()
    {
        FtpTransferCodecRequest initial = CreateRequest(
            new ChunkedReadStream(new byte[] { 88, 65 }, 1), new MemoryStream(),
            FtpTextFormat.CarriageControl, 2);
        FtpTransferCodecRequest later = CreateRequest(
            new ChunkedReadStream(new byte[] { 32, 65, 13, 10, 88, 66 }, 1), new MemoryStream(),
            FtpTextFormat.CarriageControl, 2);

        FtpTransferException? initialFailure = Assert.Throws<FtpTransferException>(() => AsciiTransferCodec.DecodeStream(initial));
        FtpTransferException? laterFailure = Assert.Throws<FtpTransferException>(() => AsciiTransferCodec.DecodeStream(later));

        Assert.Multiple(() =>
        {
            Assert.That(initialFailure!.InnerException, Is.TypeOf<InvalidDataException>());
            Assert.That(laterFailure!.InnerException, Is.TypeOf<InvalidDataException>());
        });
    }

    [Test]
    public void Asa_format_accepts_empty_content_and_a_trailing_record_boundary()
    {
        MemoryStream emptyDestination = new MemoryStream();
        FtpTransferCodecRequest empty = CreateRequest(
            new ChunkedReadStream(Array.Empty<byte>(), 1), emptyDestination,
            FtpTextFormat.CarriageControl, 2);
        MemoryStream trailingDestination = new MemoryStream();
        FtpTransferCodecRequest trailing = CreateRequest(
            new ChunkedReadStream(new byte[] { 32, 65, 13, 10 }, 1), trailingDestination,
            FtpTextFormat.CarriageControl, 2);

        long emptyCount = AsciiTransferCodec.DecodeStream(empty);
        long trailingCount = AsciiTransferCodec.DecodeStream(trailing);

        Assert.Multiple(() =>
        {
            Assert.That(emptyCount, Is.Zero);
            Assert.That(emptyDestination.Length, Is.Zero);
            Assert.That(trailingCount, Is.EqualTo(2 + Environment.NewLine.Length));
        });
    }

    private static FtpTransferCodecRequest CreateRequest(
        Stream source,
        Stream destination,
        FtpTextFormat format,
        int bufferBytes)
    {
        FtpTransferOptions options = new FtpTransferOptions();
        options.Type = FtpTransferType.Ascii;
        options.TextFormat = format;
        options.BufferBytes = bufferBytes;

        FtpTransferCodecRequest request = new FtpTransferCodecRequest();
        request.Source = source;
        request.Destination = destination;
        request.Options = options;
        request.LeaveSourceOpen = true;
        request.LeaveDestinationOpen = true;
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
}
