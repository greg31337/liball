using System.Text;

namespace ftp.Tests;

public class EbcdicTransferCodecTests
{
    [Test]
    public void Code_page_037_table_is_reversible_for_every_byte()
    {
        int value = 0;
        while (value <= 255)
        {
            int character = EbcdicCodePage037.DecodeByte((byte)value);
            byte encoded = EbcdicCodePage037.EncodeCharacter(character);
            Assert.That(encoded, Is.EqualTo((byte)value));
            value++;
        }
    }

    [Test]
    public void Code_page_037_known_text_encodes_and_decodes_exactly()
    {
        byte[] localBytes = Encoding.UTF8.GetBytes("Hello, World! 0123");
        byte[] ebcdicBytes = new byte[]
        {
            0xC8, 0x85, 0x93, 0x93, 0x96, 0x6B, 0x40, 0xE6, 0x96,
            0x99, 0x93, 0x84, 0x5A, 0x40, 0xF0, 0xF1, 0xF2, 0xF3
        };
        TrackingWriteStream encoded = new TrackingWriteStream();
        FtpTransferCodecRequest encode = CreateRequest(
            new ChunkedReadStream(localBytes, 2), encoded, FtpTransferType.Ebcdic,
            FtpTextFormat.NonPrint, 3, true, true);

        long encodedCount = EbcdicTransferCodec.EncodeStream(encode);

        TrackingWriteStream decoded = new TrackingWriteStream();
        FtpTransferCodecRequest decode = CreateRequest(
            new ChunkedReadStream(ebcdicBytes, 2), decoded, FtpTransferType.Ebcdic,
            FtpTextFormat.NonPrint, 3, true, true);
        long decodedCount = EbcdicTransferCodec.DecodeStream(decode);

        Assert.Multiple(() =>
        {
            Assert.That(encodedCount, Is.EqualTo(ebcdicBytes.Length));
            Assert.That(encoded.ToArray(), Is.EqualTo(ebcdicBytes));
            Assert.That(decodedCount, Is.EqualTo(localBytes.Length));
            Assert.That(decoded.ToArray(), Is.EqualTo(localBytes));
            Assert.That(encoded.LargestWrite, Is.LessThanOrEqualTo(3));
            Assert.That(decoded.LargestWrite, Is.LessThanOrEqualTo(3));
        });
    }

    [Test]
    public void Extended_code_page_characters_round_trip_across_single_byte_reads()
    {
        byte[] localBytes = Encoding.UTF8.GetBytes("£éÄñ");
        byte[] ebcdicBytes = new byte[] { 0xB1, 0x51, 0x63, 0x49 };
        MemoryStream encoded = new MemoryStream();
        FtpTransferCodecRequest encode = CreateRequest(
            new ChunkedReadStream(localBytes, 1), encoded, FtpTransferType.Ebcdic,
            FtpTextFormat.NonPrint, 2, true, true);

        EbcdicTransferCodec.EncodeStream(encode);

        MemoryStream decoded = new MemoryStream();
        FtpTransferCodecRequest decode = CreateRequest(
            new ChunkedReadStream(ebcdicBytes, 1), decoded, FtpTransferType.Ebcdic,
            FtpTextFormat.NonPrint, 2, true, true);
        EbcdicTransferCodec.DecodeStream(decode);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.ToArray(), Is.EqualTo(ebcdicBytes));
            Assert.That(decoded.ToArray(), Is.EqualTo(localBytes));
        });
    }

    [Test]
    public void Local_line_endings_use_the_ebcdic_new_line_character()
    {
        byte[] localBytes = Encoding.UTF8.GetBytes($"A{Environment.NewLine}B");
        byte[] ebcdicBytes = new byte[] { 0xC1, 0x15, 0xC2 };
        MemoryStream encoded = new MemoryStream();
        FtpTransferCodecRequest encode = CreateRequest(
            new ChunkedReadStream(localBytes, 1), encoded, FtpTransferType.Ebcdic,
            FtpTextFormat.NonPrint, 2, true, true);

        EbcdicTransferCodec.EncodeStream(encode);

        MemoryStream decoded = new MemoryStream();
        FtpTransferCodecRequest decode = CreateRequest(
            new ChunkedReadStream(ebcdicBytes, 1), decoded, FtpTransferType.Ebcdic,
            FtpTextFormat.NonPrint, 2, true, true);
        EbcdicTransferCodec.DecodeStream(decode);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.ToArray(), Is.EqualTo(ebcdicBytes));
            Assert.That(decoded.ToArray(), Is.EqualTo(localBytes));
        });
    }

    [Test]
    public void Telnet_format_preserves_ebcdic_vertical_controls()
    {
        byte[] localBytes = new byte[] { 13, 11, 12, 10, 0xC2, 0x85 };
        byte[] ebcdicBytes = new byte[] { 0x0D, 0x0B, 0x0C, 0x25, 0x15 };
        MemoryStream encoded = new MemoryStream();
        FtpTransferCodecRequest encode = CreateRequest(
            new ChunkedReadStream(localBytes, 1), encoded, FtpTransferType.Ebcdic,
            FtpTextFormat.Telnet, 2, true, true);
        MemoryStream decoded = new MemoryStream();
        FtpTransferCodecRequest decode = CreateRequest(
            new ChunkedReadStream(ebcdicBytes, 1), decoded, FtpTransferType.Ebcdic,
            FtpTextFormat.Telnet, 2, true, true);

        EbcdicTransferCodec.EncodeStream(encode);
        EbcdicTransferCodec.DecodeStream(decode);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.ToArray(), Is.EqualTo(ebcdicBytes));
            Assert.That(decoded.ToArray(), Is.EqualTo(localBytes));
        });
    }

    [Test]
    public void Asa_format_uses_ebcdic_controls_and_validates_each_record()
    {
        byte[] localBytes = Encoding.UTF8.GetBytes($" A{Environment.NewLine}0B{Environment.NewLine}1C{Environment.NewLine}+D");
        byte[] ebcdicBytes = new byte[]
        {
            0x40, 0xC1, 0x15, 0xF0, 0xC2, 0x15,
            0xF1, 0xC3, 0x15, 0x4E, 0xC4
        };
        MemoryStream encoded = new MemoryStream();
        FtpTransferCodecRequest encode = CreateRequest(
            new ChunkedReadStream(localBytes, 1), encoded, FtpTransferType.Ebcdic,
            FtpTextFormat.CarriageControl, 2, true, true);
        MemoryStream decoded = new MemoryStream();
        FtpTransferCodecRequest decode = CreateRequest(
            new ChunkedReadStream(ebcdicBytes, 1), decoded, FtpTransferType.Ebcdic,
            FtpTextFormat.CarriageControl, 2, true, true);

        EbcdicTransferCodec.EncodeStream(encode);
        EbcdicTransferCodec.DecodeStream(decode);

        FtpTransferCodecRequest invalidEncode = CreateRequest(
            new ChunkedReadStream(Encoding.UTF8.GetBytes("XA"), 1), new MemoryStream(),
            FtpTransferType.Ebcdic, FtpTextFormat.CarriageControl, 2, true, true);
        FtpTransferCodecRequest invalidDecode = CreateRequest(
            new ChunkedReadStream(new byte[] { 0xE7, 0xC1 }, 1), new MemoryStream(),
            FtpTransferType.Ebcdic, FtpTextFormat.CarriageControl, 2, true, true);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.ToArray(), Is.EqualTo(ebcdicBytes));
            Assert.That(decoded.ToArray(), Is.EqualTo(localBytes));
            Assert.Throws<FtpTransferException>(() => EbcdicTransferCodec.EncodeStream(invalidEncode));
            Assert.Throws<FtpTransferException>(() => EbcdicTransferCodec.DecodeStream(invalidDecode));
        });
    }

    [Test]
    public void Unmappable_and_malformed_utf8_fail_deterministically()
    {
        FtpTransferCodecRequest unmappable = CreateRequest(
            new ChunkedReadStream(Encoding.UTF8.GetBytes("€"), 1), new MemoryStream(),
            FtpTransferType.Ebcdic, FtpTextFormat.NonPrint, 2, true, true);
        FtpTransferCodecRequest malformed = CreateRequest(
            new ChunkedReadStream(new byte[] { 0xC2, 0x41 }, 1), new MemoryStream(),
            FtpTransferType.Ebcdic, FtpTextFormat.NonPrint, 2, true, true);
        FtpTransferCodecRequest incomplete = CreateRequest(
            new ChunkedReadStream(new byte[] { 0xE2, 0x82 }, 1), new MemoryStream(),
            FtpTransferType.Ebcdic, FtpTextFormat.NonPrint, 2, true, true);

        FtpTransferException? unmappableFailure = Assert.Throws<FtpTransferException>(() => EbcdicTransferCodec.EncodeStream(unmappable));
        FtpTransferException? malformedFailure = Assert.Throws<FtpTransferException>(() => EbcdicTransferCodec.EncodeStream(malformed));
        FtpTransferException? incompleteFailure = Assert.Throws<FtpTransferException>(() => EbcdicTransferCodec.EncodeStream(incomplete));

        Assert.Multiple(() =>
        {
            Assert.That(unmappableFailure!.InnerException, Is.TypeOf<InvalidDataException>());
            Assert.That(malformedFailure!.InnerException, Is.TypeOf<InvalidDataException>());
            Assert.That(incompleteFailure!.InnerException, Is.TypeOf<InvalidDataException>());
        });
    }

    [Test]
    public void Codec_validates_type_and_closes_owned_streams_after_failure()
    {
        TrackingReadStream ownedSource = new TrackingReadStream(Encoding.UTF8.GetBytes("€"), 1);
        TrackingWriteStream ownedDestination = new TrackingWriteStream();
        FtpTransferCodecRequest owned = CreateRequest(
            ownedSource, ownedDestination, FtpTransferType.Ebcdic,
            FtpTextFormat.NonPrint, 2, false, false);
        FtpTransferCodecRequest ascii = CreateRequest(
            new ChunkedReadStream(new byte[] { 65 }, 1), new MemoryStream(),
            FtpTransferType.Ascii, FtpTextFormat.NonPrint, 2, true, true);

        Assert.Throws<FtpTransferException>(() => EbcdicTransferCodec.EncodeStream(owned));

        Assert.Multiple(() =>
        {
            Assert.That(ownedSource.Disposed, Is.True);
            Assert.That(ownedDestination.Disposed, Is.True);
            Assert.Throws<FtpConfigurationException>(() => EbcdicTransferCodec.EncodeStream(ascii));
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
