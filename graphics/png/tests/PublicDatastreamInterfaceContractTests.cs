using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PublicDatastreamInterfaceContractTests
{
    [Test]
    public void CodecAndProgressiveContracts_ProduceEquivalentResultsAcrossAllInputForms()
    {
        IPngImageFactory factory = new PngImageFactory();
        IPngEncoder encoder = new PngCodec();
        IPngDecoder decoder = new PngCodec();
        IPngProgressiveDecoder progressive = new PngProgressiveDecoder();
        PngImage source = factory.CreateImageWithData(2, 1, PngPixelFormat.Rgba8, new byte[] { 10, 20, 30, 255, 40, 50, 60, 255 });
        PngEncoderOptions encoderOptions = CreateExplicitRgbaOptions();
        byte[] png = encoder.EncodeToBytes(source, encoderOptions);
        MemoryStream encodedStream = new MemoryStream();
        encodedStream.WriteByte(99);
        encoder.EncodeToStream(encodedStream, source, CreateExplicitRgbaOptions());
        Assert.That(encodedStream.Position, Is.EqualTo(png.Length + 1));
        Assert.That(encodedStream.ToArray().AsSpan(1).ToArray(), Is.EqualTo(png));

        byte[] embedded = Join(new byte[] { 1, 2, 3 }, png);
        PngDecoderOptions options = CreateRgbDecodeOptions();
        PngImage fromBytes = decoder.DecodeBytes(png, options);
        PngImage fromOffset = decoder.DecodeBytesAtOffset(embedded, 3, CreateRgbDecodeOptions());
        PngImage fromSpan = decoder.DecodeSpan(embedded.AsSpan(3, png.Length), CreateRgbDecodeOptions());
        MemoryStream seekable = new MemoryStream(embedded);
        seekable.Position = 3;
        PngImage fromStream = decoder.DecodeStream(seekable, CreateRgbDecodeOptions());
        ShortReadStream shortReads = new ShortReadStream(png, 1);
        PngImage fromShortNonSeekable = decoder.DecodeStream(shortReads, CreateRgbDecodeOptions());

        AssertEquivalentPixels(fromBytes, fromOffset, fromSpan, fromStream, fromShortNonSeekable);
        Assert.That(fromBytes.Pixels.Format, Is.EqualTo(PngPixelFormat.Rgb8));
        Assert.That(seekable.Position, Is.EqualTo(embedded.Length));
        Assert.That(shortReads.BytesRead, Is.EqualTo(png.Length));
        Assert.That(fromShortNonSeekable.Metadata.Chunks[0].Offset, Is.EqualTo(8));

        RecordingProgressiveSink byteSink = new RecordingProgressiveSink();
        RecordingProgressiveSink offsetSink = new RecordingProgressiveSink();
        RecordingProgressiveSink spanSink = new RecordingProgressiveSink();
        RecordingProgressiveSink streamSink = new RecordingProgressiveSink();
        PngImage progressiveBytes = progressive.DecodeProgressiveBytes(png, CreateRgbDecodeOptions(), byteSink);
        PngImage progressiveOffset = progressive.DecodeProgressiveBytesAtOffset(embedded, 3, CreateRgbDecodeOptions(), offsetSink);
        PngImage progressiveSpan = progressive.DecodeProgressiveSpan(embedded.AsSpan(3, png.Length), CreateRgbDecodeOptions(), spanSink);
        ShortReadStream progressiveStream = new ShortReadStream(png, 2);
        PngImage progressiveResult = progressive.DecodeProgressiveStream(progressiveStream, CreateRgbDecodeOptions(), streamSink);

        AssertEquivalentPixels(progressiveBytes, progressiveOffset, progressiveSpan, progressiveResult, fromBytes);
        Assert.That(byteSink.Rows, Is.EqualTo(1));
        Assert.That(offsetSink.Rows, Is.EqualTo(1));
        Assert.That(spanSink.Rows, Is.EqualTo(1));
        Assert.That(streamSink.Rows, Is.EqualTo(1));
        Assert.That(progressiveResult.Metadata.Chunks[0].Offset, Is.EqualTo(8));
    }

    [Test]
    public void IdentifierAndValidatorContracts_HonorOffsetsOptionsStreamsAndPolicies()
    {
        byte[] png = CreatePng();
        byte[] embedded = Join(new byte[] { 7, 8 }, png, new byte[] { 9, 10 });
        byte[] offsetOnly = Join(new byte[] { 7, 8 }, png);
        IPngIdentifier identifier = new PngIdentifierService();
        IPngValidator validator = new PngValidationService();

        PngIdentifyResult identified = identifier.IdentifyBytes(png);
        PngIdentifyResult identifiedWithOptions = identifier.IdentifyBytesWithOptions(png, new PngDecoderOptions());
        PngIdentifyResult identifiedAtOffset = identifier.IdentifyBytesAtOffset(embedded, 2, new PngDecoderOptions());
        MemoryStream identifyStream = new MemoryStream(embedded);
        identifyStream.Position = 2;
        PngIdentifyResult identifiedFromStream = identifier.IdentifyStream(identifyStream);
        ShortReadStream shortIdentifyStream = new ShortReadStream(png, 1);
        PngIdentifyResult identifiedFromShortReads = identifier.IdentifyStreamWithOptions(shortIdentifyStream, new PngDecoderOptions());

        Assert.That(new PngIdentifyResult[] { identified, identifiedWithOptions, identifiedAtOffset, identifiedFromStream, identifiedFromShortReads }.Select(result => result.IsValid), Is.All.True);
        Assert.That(new PngIdentifyResult[] { identified, identifiedWithOptions, identifiedAtOffset, identifiedFromStream, identifiedFromShortReads }.Select(result => result.Info.Width), Is.All.EqualTo(2));
        Assert.That(identifyStream.Position, Is.EqualTo(2 + png.Length));
        Assert.That(identifiedAtOffset.Metadata.Chunks[0].Offset, Is.EqualTo(10));
        Assert.That(identifiedFromShortReads.Metadata.Chunks[0].Offset, Is.EqualTo(8));

        PngSecurityLimits narrowLimits = new PngSecurityLimits();
        narrowLimits.MaxWidth = 1;
        PngDecoderOptions narrowOptions = new PngDecoderOptions();
        narrowOptions.Limits = narrowLimits;
        PngIdentifyResult limited = identifier.IdentifyBytesWithOptions(png, narrowOptions);
        Assert.That(limited.IsValid, Is.False);
        Assert.That(limited.Errors.Select(error => error.Code), Does.Contain("PNG_IHDR_WIDTH_LIMIT_EXCEEDED"));

        PngValidationResult strict = validator.ValidateBytes(png);
        PngValidationResult explicitStrict = validator.ValidateBytesWithOptions(png, new PngDecoderOptions());
        PngValidationResult offsetStrict = validator.ValidateBytesAtOffset(embedded, 2, CreateIgnoreTrailingOptions());
        MemoryStream validateStream = new MemoryStream(embedded);
        validateStream.Position = 2;
        PngValidationResult streamStrict = validator.ValidateStreamWithOptions(validateStream, CreateIgnoreTrailingOptions());
        PngValidationResult defaultStreamStrict = validator.ValidateStream(new ShortReadStream(png, 1));
        PngValidationResult inspected = validator.InspectBestEffortBytes(png);
        PngValidationResult inspectedOffset = validator.InspectBestEffortBytesAtOffset(offsetOnly, 2);
        PngValidationResult inspectedStream = validator.InspectBestEffortStream(new ShortReadStream(png, 2));
        PngValidationResult repairTolerant = validator.ValidateTolerantRepairBytes(png);
        PngValidationResult repairTolerantOffset = validator.ValidateTolerantRepairBytesAtOffset(offsetOnly, 2);
        PngValidationResult repairTolerantStream = validator.ValidateTolerantRepairStream(new ShortReadStream(png, 3));

        Assert.That(new PngValidationResult[] { strict, explicitStrict, offsetStrict, streamStrict, defaultStreamStrict, inspected, inspectedOffset, inspectedStream, repairTolerant, repairTolerantOffset, repairTolerantStream }.Select(result => result.IsValid), Is.All.True);
        Assert.That(validateStream.Position, Is.EqualTo(embedded.Length));
    }

    [Test]
    public void ChunkCodecContract_RoundTripsEveryArrayOffsetAndStreamVariant()
    {
        byte[] png = CreatePng();
        byte[] embedded = Join(new byte[] { 3, 4, 5 }, png);
        IPngChunkCodec codec = new PngChunkService();
        PngSecurityLimits limits = new PngSecurityLimits();

        PngChunkListResult bytes = codec.ReadChunks(png);
        PngChunkListResult withLimits = codec.ReadChunksWithLimits(png, limits);
        PngChunkListResult offset = codec.ReadChunksAtOffset(embedded, 3);
        PngChunkListResult offsetWithLimits = codec.ReadChunksAtOffsetWithLimits(embedded, 3, limits);
        PngChunkListResult stream = codec.ReadChunksStream(new MemoryStream(png));
        ShortReadStream shortStream = new ShortReadStream(png, 1);
        PngChunkListResult streamWithLimits = codec.ReadChunksStreamWithLimits(shortStream, limits);

        Assert.That(new PngChunkListResult[] { bytes, withLimits, offset, offsetWithLimits, stream, streamWithLimits }.Select(result => result.FoundIend), Is.All.True);
        Assert.That(new PngChunkListResult[] { bytes, withLimits, offset, offsetWithLimits, stream, streamWithLimits }.Select(result => result.Chunks.Length), Is.All.EqualTo(bytes.Chunks.Length));
        Assert.That(offset.Chunks[0].Offset, Is.EqualTo(11));
        Assert.That(streamWithLimits.Chunks[0].Offset, Is.EqualTo(8));

        byte[] defaultBytes = codec.WriteChunks(bytes.Chunks);
        byte[] limitedBytes = codec.WriteChunksWithLimits(bytes.Chunks, limits);
        PngChunkWriteOptions writeOptions = new PngChunkWriteOptions();
        byte[] optionBytes = codec.WriteChunksWithOptions(bytes.Chunks, writeOptions);
        MemoryStream defaultOutput = new MemoryStream();
        MemoryStream limitedOutput = new MemoryStream();
        MemoryStream optionOutput = new MemoryStream();
        codec.WriteChunksStream(defaultOutput, bytes.Chunks);
        codec.WriteChunksStreamWithLimits(limitedOutput, bytes.Chunks, limits);
        codec.WriteChunksStreamWithOptions(optionOutput, bytes.Chunks, writeOptions);

        Assert.That(defaultBytes, Is.EqualTo(png));
        Assert.That(limitedBytes, Is.EqualTo(defaultBytes));
        Assert.That(optionBytes, Is.EqualTo(defaultBytes));
        Assert.That(defaultOutput.ToArray(), Is.EqualTo(defaultBytes));
        Assert.That(limitedOutput.ToArray(), Is.EqualTo(defaultBytes));
        Assert.That(optionOutput.ToArray(), Is.EqualTo(defaultBytes));

        PngSecurityLimits oneChunk = new PngSecurityLimits();
        oneChunk.MaxChunkCount = 1;
        Assert.Throws<OverflowException>(() => codec.ReadChunksWithLimits(png, oneChunk));
        Assert.Throws<OverflowException>(() => codec.WriteChunksWithLimits(bytes.Chunks, oneChunk));
    }

    [Test]
    public void DatastreamContracts_HaveDeterministicFailureSemantics()
    {
        byte[] png = CreatePng();
        IPngDecoder decoder = new PngCodec();
        IPngProgressiveDecoder progressive = new PngProgressiveDecoder();
        IPngIdentifier identifier = new PngIdentifierService();
        IPngChunkCodec chunks = new PngChunkService();
        RecordingProgressiveSink sink = new RecordingProgressiveSink();

        Assert.Throws<ArgumentOutOfRangeException>(() => decoder.DecodeBytesAtOffset(png, -1, new PngDecoderOptions()));
        Assert.Throws<ArgumentOutOfRangeException>(() => progressive.DecodeProgressiveBytesAtOffset(png, -1, new PngDecoderOptions(), sink));
        Assert.Throws<ArgumentOutOfRangeException>(() => identifier.IdentifyBytesAtOffset(png, -1, new PngDecoderOptions()));
        Assert.Throws<ArgumentOutOfRangeException>(() => chunks.ReadChunksAtOffset(png, -1));
        Assert.Throws<EndOfStreamException>(() => decoder.DecodeStream(new ShortReadStream(png.AsSpan(0, png.Length - 1).ToArray(), 1), new PngDecoderOptions()));
        Assert.Throws<EndOfStreamException>(() => identifier.IdentifyStream(new ShortReadStream(png.AsSpan(0, png.Length - 1).ToArray(), 1)));
        Assert.Throws<EndOfStreamException>(() => chunks.ReadChunksStream(new ShortReadStream(png.AsSpan(0, png.Length - 1).ToArray(), 1)));
        Assert.Throws<ArgumentNullException>(() => decoder.DecodeBytes(null!, new PngDecoderOptions()));
        Assert.Throws<ArgumentNullException>(() => identifier.IdentifyStream(null!));
        Assert.Throws<ArgumentNullException>(() => chunks.ReadChunksStream(null!));
    }

    static byte[] CreatePng()
    {
        IPngImageFactory factory = new PngImageFactory();
        IPngEncoder encoder = new PngCodec();
        PngImage image = factory.CreateImageWithData(2, 1, PngPixelFormat.Rgba8, new byte[] { 10, 20, 30, 255, 40, 50, 60, 255 });
        return encoder.EncodeToBytes(image, CreateExplicitRgbaOptions());
    }

    static PngEncoderOptions CreateExplicitRgbaOptions()
    {
        PngEncoderOptions options = new PngEncoderOptions();
        options.UseAutomaticColorType = false;
        options.UseAutomaticBitDepth = false;
        options.ColorType = PngColorType.TruecolorAlpha;
        options.BitDepth = PngBitDepth.Eight;
        return options;
    }

    static PngDecoderOptions CreateRgbDecodeOptions()
    {
        PngDecoderOptions options = new PngDecoderOptions();
        options.PreferredPixelFormat = PngPixelFormat.Rgb8;
        return options;
    }

    static PngDecoderOptions CreateIgnoreTrailingOptions()
    {
        PngDecoderOptions options = new PngDecoderOptions();
        options.IgnoreTrailingBytes = true;
        return options;
    }

    static byte[] Join(params byte[][] values)
    {
        byte[] result = new byte[values.Sum(value => value.Length)];
        int offset = 0;
        foreach (byte[] value in values)
        {
            Array.Copy(value, 0, result, offset, value.Length);
            offset += value.Length;
        }

        return result;
    }

    static void AssertEquivalentPixels(params PngImage[] images)
    {
        Assert.That(images.Select(image => image.Pixels.Format), Is.All.EqualTo(images[0].Pixels.Format));
        Assert.That(images.Select(image => image.Pixels.Stride), Is.All.EqualTo(images[0].Pixels.Stride));
        int index = 1;
        while (index < images.Length)
        {
            Assert.That(images[index].Pixels.Data, Is.EqualTo(images[0].Pixels.Data));
            index += 1;
        }
    }

    class RecordingProgressiveSink : IPngProgressiveSink
    {
        public int Rows;

        public bool WantsPreviews() { return false; }
        public void ReceiveRow(PngProgressiveRow row) { Rows += 1; }
        public void ReceivePass(PngAdam7PassUpdate pass) { }
        public void ReceivePreview(PngProgressivePreview preview) { }
        public void ReceiveFrame(PngFrame frame) { }
        public void ReceiveWarning(PngWarning warning) { }
    }

    class ShortReadStream : Stream
    {
        byte[] data;
        int maximumRead;
        int offset;

        public ShortReadStream(byte[] source, int maximum)
        {
            data = source;
            maximumRead = maximum;
        }

        public int BytesRead { get { return offset; } }
        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return false; } }
        public override long Length { get { throw new NotSupportedException(); } }
        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        public override void Flush() { }

        public override int Read(byte[] buffer, int bufferOffset, int count)
        {
            if (offset >= data.Length)
            {
                return 0;
            }

            int available = data.Length - offset;
            int length = Math.Min(Math.Min(count, maximumRead), available);
            Array.Copy(data, offset, buffer, bufferOffset, length);
            offset += length;
            return length;
        }

        public override long Seek(long value, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int bufferOffset, int count) { throw new NotSupportedException(); }
    }
}
