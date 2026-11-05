using webp;

namespace webp.Tests;

[TestFixture]
public class WebpEntryPointParityTests
{
    [Test]
    public void DecodeIdentifyAndValidateHaveParityAcrossEveryInputFormAndPartialReads()
    {
        WebpCodec codec = new WebpCodec();
        byte[] data = EncodeMetadataStill(codec);
        byte[] originalData = CopyBytes(data);
        byte[] wrapped = Wrap(data);
        WebpDecodeOptions options = new WebpDecodeOptions();
        WebpDecodeResult arrayDecode = codec.Decode(data, options);
        WebpInfo arrayInfo = codec.Read(data, options);
        WebpValidationResult arrayValidation = codec.Validate(data, options);
        WebpDecodeResult spanDecode = codec.DecodeSpan(wrapped.AsSpan(3, data.Length), options);
        WebpInfo spanInfo = codec.ReadSpan(wrapped.AsSpan(3, data.Length), options);
        WebpValidationResult spanValidation = codec.ValidateSpan(wrapped.AsSpan(3, data.Length), options);
        PartialReadTestStream seekDecodeStream = NewSeekablePartialStream(data);
        PartialReadTestStream seekInfoStream = NewSeekablePartialStream(data);
        PartialReadTestStream seekValidationStream = NewSeekablePartialStream(data);
        PartialReadTestStream nonSeekDecodeStream = NewNonSeekablePartialStream(data);
        PartialReadTestStream nonSeekInfoStream = NewNonSeekablePartialStream(data);
        PartialReadTestStream nonSeekValidationStream = NewNonSeekablePartialStream(data);
        WebpDecodeResult seekDecode = codec.DecodeStream(seekDecodeStream, options);
        WebpInfo seekInfo = codec.ReadStream(seekInfoStream, options);
        WebpValidationResult seekValidation = codec.ValidateStream(seekValidationStream, options);
        WebpDecodeResult nonSeekDecode = codec.DecodeStream(nonSeekDecodeStream, options);
        WebpInfo nonSeekInfo = codec.ReadStream(nonSeekInfoStream, options);
        WebpValidationResult nonSeekValidation = codec.ValidateStream(nonSeekValidationStream, options);
        string path = NewTempPath();
        File.WriteAllBytes(path, data);

        try
        {
            WebpDecodeResult fileDecode = codec.DecodeFile(path, options);
            WebpInfo fileInfo = codec.ReadFile(path, options);
            WebpValidationResult fileValidation = codec.ValidateFile(path, options);

            AssertDecodeParity(arrayDecode, spanDecode);
            AssertDecodeParity(arrayDecode, seekDecode);
            AssertDecodeParity(arrayDecode, nonSeekDecode);
            AssertDecodeParity(arrayDecode, fileDecode);
            AssertInfoParity(arrayInfo, spanInfo);
            AssertInfoParity(arrayInfo, seekInfo);
            AssertInfoParity(arrayInfo, nonSeekInfo);
            AssertInfoParity(arrayInfo, fileInfo);
            AssertValidationParity(arrayValidation, spanValidation);
            AssertValidationParity(arrayValidation, seekValidation);
            AssertValidationParity(arrayValidation, nonSeekValidation);
            AssertValidationParity(arrayValidation, fileValidation);
        }
        finally
        {
            File.Delete(path);
        }

        AssertSeekableConsumedAndOpen(seekDecodeStream);
        AssertSeekableConsumedAndOpen(seekInfoStream);
        AssertSeekableConsumedAndOpen(seekValidationStream);
        AssertNonSeekableConsumedAndOpen(nonSeekDecodeStream, data.Length);
        AssertNonSeekableConsumedAndOpen(nonSeekInfoStream, data.Length);
        AssertNonSeekableConsumedAndOpen(nonSeekValidationStream, data.Length);
        Assert.That(data, Is.EqualTo(originalData));
        arrayDecode.Image.Frames[0].Pixels!.Rgba[0] = 99;
        arrayDecode.Image.Metadata.Exif![0] = 88;
        Assert.That(spanDecode.Image.Frames[0].Pixels!.Rgba[0], Is.EqualTo(10));
        Assert.That(spanDecode.Image.Metadata.Exif![0], Is.EqualTo(4));
    }

    [Test]
    public void EncodeHasByteStreamAndFileParityWithoutTakingInputOrOutputOwnership()
    {
        WebpCodec codec = new WebpCodec();
        WebpPixelBuffer pixels = NewPixels();
        byte[] originalPixels = CopyBytes(pixels.Rgba);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossless;
        byte[] arrayOutput = codec.Encode(pixels, options);
        MemoryStream seekableOutput = new MemoryStream();
        seekableOutput.WriteByte(0xAA);
        NonSeekableWriteTestStream nonSeekableOutput = new NonSeekableWriteTestStream();
        codec.EncodeStream(pixels, seekableOutput, options);
        codec.EncodeStream(pixels, nonSeekableOutput, options);
        string path = NewTempPath();
        File.WriteAllBytes(path, new byte[] { 9, 8, 7, 6 });

        try
        {
            codec.EncodeFile(pixels, path, options);
            byte[] seekableBytes = seekableOutput.ToArray();
            byte[] seekableEncoded = CopyBytes(seekableBytes, 1, seekableBytes.Length - 1);
            byte[] nonSeekableEncoded = nonSeekableOutput.ToArray();
            byte[] fileOutput = File.ReadAllBytes(path);

            Assert.That(seekableBytes[0], Is.EqualTo(0xAA));
            Assert.That(seekableEncoded, Is.EqualTo(arrayOutput));
            Assert.That(nonSeekableEncoded, Is.EqualTo(arrayOutput));
            Assert.That(fileOutput, Is.EqualTo(arrayOutput));
        }
        finally
        {
            File.Delete(path);
        }

        Assert.That(pixels.Rgba, Is.EqualTo(originalPixels));
        Assert.That(seekableOutput.Position, Is.EqualTo(seekableOutput.Length));
        Assert.That(seekableOutput.CanWrite, Is.True);
        Assert.That(nonSeekableOutput.WasDisposed, Is.False);
        Assert.That(nonSeekableOutput.CanWrite, Is.True);
        byte originalFirstByte = nonSeekableOutput.ToArray()[0];
        arrayOutput[0] = 0;
        Assert.That(nonSeekableOutput.ToArray()[0], Is.EqualTo(originalFirstByte));
    }

    [Test]
    public void MetadataMuxHasParityAcrossEveryInputFormAndReturnsIndependentOutput()
    {
        WebpCodec codec = new WebpCodec();
        byte[] source = codec.Encode(NewPixels(), NewLosslessOptions());
        byte[] originalSource = CopyBytes(source);
        byte[] wrapped = Wrap(source);
        WebpMetadata metadata = NewMetadata();
        byte[] arrayOutput = codec.ReplaceMetadata(source, metadata);
        byte[] spanOutput = codec.ReplaceMetadataSpan(wrapped.AsSpan(3, source.Length), metadata);
        PartialReadTestStream seekableStream = NewSeekablePartialStream(source);
        PartialReadTestStream nonSeekableStream = NewNonSeekablePartialStream(source);
        byte[] seekableOutput = codec.ReplaceMetadataStream(seekableStream, metadata);
        byte[] nonSeekableOutput = codec.ReplaceMetadataStream(nonSeekableStream, metadata);
        string path = NewTempPath();
        File.WriteAllBytes(path, source);

        try
        {
            byte[] fileOutput = codec.ReplaceMetadataFile(path, metadata);
            Assert.That(spanOutput, Is.EqualTo(arrayOutput));
            Assert.That(seekableOutput, Is.EqualTo(arrayOutput));
            Assert.That(nonSeekableOutput, Is.EqualTo(arrayOutput));
            Assert.That(fileOutput, Is.EqualTo(arrayOutput));
        }
        finally
        {
            File.Delete(path);
        }

        AssertSeekableConsumedAndOpen(seekableStream);
        AssertNonSeekableConsumedAndOpen(nonSeekableStream, source.Length);
        Assert.That(source, Is.EqualTo(originalSource));
        byte independentByte = spanOutput[0];
        arrayOutput[0] = 0;
        Assert.That(spanOutput[0], Is.EqualTo(independentByte));
        WebpMetadata extracted = codec.GetMetadata(spanOutput);
        extracted.IccProfile![0] = 99;
        Assert.That(codec.GetMetadata(spanOutput).IccProfile![0], Is.EqualTo(1));

        byte[] stripped = codec.StripMetadata(spanOutput);
        WebpMetadata strippedMetadata = codec.GetMetadata(stripped);
        Assert.That(strippedMetadata.IccProfile, Is.Null);
        Assert.That(strippedMetadata.Exif, Is.Null);
        Assert.That(strippedMetadata.Xmp, Is.Null);
        Assert.That(codec.GetMetadata(spanOutput).Exif, Is.EqualTo(metadata.Exif));
    }

    [Test]
    public void IoFailuresPropagateAndCallerOwnedStreamsRemainOpen()
    {
        WebpCodec codec = new WebpCodec();
        WebpDecodeOptions decodeOptions = new WebpDecodeOptions();
        ThrowingReadTestStream decodeStream = new ThrowingReadTestStream();
        ThrowingReadTestStream infoStream = new ThrowingReadTestStream();
        ThrowingReadTestStream validationStream = new ThrowingReadTestStream();
        ThrowingReadTestStream muxStream = new ThrowingReadTestStream();
        ThrowingWriteTestStream outputStream = new ThrowingWriteTestStream();

        Assert.Throws<IOException>(() => codec.DecodeStream(decodeStream, decodeOptions));
        Assert.Throws<IOException>(() => codec.ReadStream(infoStream, decodeOptions));
        Assert.Throws<IOException>(() => codec.ValidateStream(validationStream, decodeOptions));
        Assert.Throws<IOException>(() => codec.GetMetadataStream(muxStream));
        Assert.Throws<IOException>(() => codec.EncodeStream(NewPixels(), outputStream, NewLosslessOptions()));
        Assert.That(decodeStream.WasDisposed, Is.False);
        Assert.That(infoStream.WasDisposed, Is.False);
        Assert.That(validationStream.WasDisposed, Is.False);
        Assert.That(muxStream.WasDisposed, Is.False);
        Assert.That(outputStream.WasDisposed, Is.False);

        string missingPath = NewTempPath();
        Assert.Throws<FileNotFoundException>(() => codec.DecodeFile(missingPath, decodeOptions));
        Assert.Throws<FileNotFoundException>(() => codec.ReadFile(missingPath, decodeOptions));
        Assert.Throws<FileNotFoundException>(() => codec.ValidateFile(missingPath, decodeOptions));
        Assert.Throws<FileNotFoundException>(() => codec.GetMetadataFile(missingPath));
        Assert.Throws<UnauthorizedAccessException>(() => codec.EncodeFile(NewPixels(), TestContext.CurrentContext.WorkDirectory, NewLosslessOptions()));
    }

    [Test]
    public void InvalidMuxArgumentsAreRejectedBeforeStreamOrFileIo()
    {
        WebpCodec codec = new WebpCodec();
        WebpMetadata invalidMetadata = new WebpMetadata();
        invalidMetadata.Exif = Array.Empty<byte>();
        ThrowingReadTestStream metadataStream = new ThrowingReadTestStream();
        ThrowingReadTestStream unknownStream = new ThrowingReadTestStream();
        ThrowingReadTestStream frameStream = new ThrowingReadTestStream();
        string missingPath = NewTempPath();

        Assert.Throws<ArgumentException>(() => codec.ReplaceMetadataStream(metadataStream, invalidMetadata));
        Assert.Throws<ArgumentException>(() => codec.SetXmpFile(missingPath, Array.Empty<byte>()));
        Assert.Throws<ArgumentNullException>(() => codec.ReplaceUnknownChunksStream(unknownStream, null!, WebpUnknownChunkPlacement.AfterRecognizedChunks));
        Assert.Throws<ArgumentOutOfRangeException>(() => codec.ExtractFrameStream(frameStream, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => codec.RemoveAnimationFrameFile(missingPath, -1));
        Assert.That(metadataStream.ReadCount, Is.EqualTo(0));
        Assert.That(unknownStream.ReadCount, Is.EqualTo(0));
        Assert.That(frameStream.ReadCount, Is.EqualTo(0));
    }

    static void AssertDecodeParity(WebpDecodeResult expected, WebpDecodeResult actual)
    {
        Assert.That(actual.IsValid, Is.EqualTo(expected.IsValid));
        Assert.That(actual.Diagnostics.Count, Is.EqualTo(expected.Diagnostics.Count));
        Assert.That(actual.Image.CanvasWidth, Is.EqualTo(expected.Image.CanvasWidth));
        Assert.That(actual.Image.CanvasHeight, Is.EqualTo(expected.Image.CanvasHeight));
        Assert.That(actual.Image.Metadata.IccProfile, Is.EqualTo(expected.Image.Metadata.IccProfile));
        Assert.That(actual.Image.Metadata.Exif, Is.EqualTo(expected.Image.Metadata.Exif));
        Assert.That(actual.Image.Metadata.Xmp, Is.EqualTo(expected.Image.Metadata.Xmp));
        Assert.That(actual.Image.Frames, Has.Count.EqualTo(expected.Image.Frames.Count));
        Assert.That(actual.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(expected.Image.Frames[0].Pixels!.Rgba));
    }

    static void AssertInfoParity(WebpInfo expected, WebpInfo actual)
    {
        Assert.That(actual.Width, Is.EqualTo(expected.Width));
        Assert.That(actual.Height, Is.EqualTo(expected.Height));
        Assert.That(actual.HasAlpha, Is.EqualTo(expected.HasAlpha));
        Assert.That(actual.HasIccProfile, Is.EqualTo(expected.HasIccProfile));
        Assert.That(actual.HasExif, Is.EqualTo(expected.HasExif));
        Assert.That(actual.HasXmp, Is.EqualTo(expected.HasXmp));
        Assert.That(actual.ContainerKind, Is.EqualTo(expected.ContainerKind));
        Assert.That(actual.BitstreamKind, Is.EqualTo(expected.BitstreamKind));
        Assert.That(actual.Chunks.Count, Is.EqualTo(expected.Chunks.Count));
        Assert.That(actual.Diagnostics.Count, Is.EqualTo(expected.Diagnostics.Count));
    }

    static void AssertValidationParity(WebpValidationResult expected, WebpValidationResult actual)
    {
        Assert.That(actual.IsValid, Is.EqualTo(expected.IsValid));
        Assert.That(actual.Diagnostics.Count, Is.EqualTo(expected.Diagnostics.Count));
    }

    static void AssertSeekableConsumedAndOpen(PartialReadTestStream stream)
    {
        Assert.That(stream.Position, Is.EqualTo(stream.Length));
        Assert.That(stream.WasDisposed, Is.False);
        Assert.That(stream.CanRead, Is.True);
        Assert.That(stream.ReadCalls, Is.GreaterThan(1));
    }

    static void AssertNonSeekableConsumedAndOpen(PartialReadTestStream stream, int expectedBytes)
    {
        Assert.That(stream.BytesRead, Is.EqualTo(expectedBytes));
        Assert.That(stream.WasDisposed, Is.False);
        Assert.That(stream.CanRead, Is.True);
        Assert.That(stream.ReadCalls, Is.GreaterThan(1));
    }

    static PartialReadTestStream NewSeekablePartialStream(byte[] data)
    {
        byte[] prefixed = new byte[data.Length + 3];
        prefixed[0] = 90;
        prefixed[1] = 91;
        prefixed[2] = 92;
        Array.Copy(data, 0, prefixed, 3, data.Length);
        return new PartialReadTestStream(prefixed, true, 3, 3);
    }

    static PartialReadTestStream NewNonSeekablePartialStream(byte[] data)
    {
        return new PartialReadTestStream(data, false, 0, 3);
    }

    static byte[] EncodeMetadataStill(WebpCodec codec)
    {
        byte[] data = codec.Encode(NewPixels(), NewLosslessOptions());
        return codec.ReplaceMetadata(data, NewMetadata());
    }

    static WebpMetadata NewMetadata()
    {
        WebpMetadata metadata = new WebpMetadata();
        metadata.IccProfile = new byte[] { 1, 2, 3 };
        metadata.Exif = new byte[] { 4, 5 };
        metadata.Xmp = new byte[] { 6 };
        return metadata;
    }

    static WebpEncodeOptions NewLosslessOptions()
    {
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossless;
        return options;
    }

    static WebpPixelBuffer NewPixels()
    {
        WebpPixelBuffer pixels = new WebpPixelBuffer();
        pixels.Width = 2;
        pixels.Height = 1;
        pixels.Stride = 8;
        pixels.Rgba = new byte[] { 10, 20, 30, 255, 40, 50, 60, 128 };
        return pixels;
    }

    static byte[] Wrap(byte[] data)
    {
        byte[] wrapped = new byte[data.Length + 6];
        wrapped[0] = 90;
        wrapped[1] = 91;
        wrapped[2] = 92;
        Array.Copy(data, 0, wrapped, 3, data.Length);
        wrapped[wrapped.Length - 3] = 93;
        wrapped[wrapped.Length - 2] = 94;
        wrapped[wrapped.Length - 1] = 95;
        return wrapped;
    }

    static byte[] CopyBytes(byte[] data)
    {
        byte[] copy = new byte[data.Length];
        Array.Copy(data, copy, data.Length);
        return copy;
    }

    static byte[] CopyBytes(byte[] data, int offset, int length)
    {
        byte[] copy = new byte[length];
        Array.Copy(data, offset, copy, 0, length);
        return copy;
    }

    static string NewTempPath()
    {
        return Path.Combine(TestContext.CurrentContext.WorkDirectory, "webp-parity-" + Guid.NewGuid().ToString("N") + ".webp");
    }
}

public class PartialReadTestStream : Stream
{
    MemoryStream inner;
    bool seekable;
    int maximumRead;
    public int BytesRead;
    public int ReadCalls;
    public bool WasDisposed;

    public PartialReadTestStream(byte[] data, bool canSeek, int initialPosition, int maximumReadSize)
    {
        inner = new MemoryStream(data);
        inner.Position = initialPosition;
        seekable = canSeek;
        maximumRead = maximumReadSize;
        BytesRead = 0;
        ReadCalls = 0;
        WasDisposed = false;
    }

    public override bool CanRead => !WasDisposed;
    public override bool CanSeek => seekable && !WasDisposed;
    public override bool CanWrite => false;
    public override long Length => seekable ? inner.Length : throw new NotSupportedException();
    public override long Position
    {
        get => seekable ? inner.Position : throw new NotSupportedException();
        set
        {
            if (!seekable)
            {
                throw new NotSupportedException();
            }

            inner.Position = value;
        }
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int requested = count;
        if (requested > maximumRead)
        {
            requested = maximumRead;
        }

        int read = inner.Read(buffer, offset, requested);
        BytesRead = BytesRead + read;
        ReadCalls = ReadCalls + 1;
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        if (!seekable)
        {
            throw new NotSupportedException();
        }

        return inner.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
        WasDisposed = true;
        inner.Dispose();
        base.Dispose(disposing);
    }
}

public class NonSeekableWriteTestStream : Stream
{
    MemoryStream inner;
    public bool WasDisposed;

    public NonSeekableWriteTestStream()
    {
        inner = new MemoryStream();
        WasDisposed = false;
    }

    public byte[] ToArray()
    {
        return inner.ToArray();
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => !WasDisposed;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
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
        inner.Write(buffer, offset, count);
    }

    protected override void Dispose(bool disposing)
    {
        WasDisposed = true;
        inner.Dispose();
        base.Dispose(disposing);
    }
}

public class ThrowingReadTestStream : Stream
{
    public int ReadCount;
    public bool WasDisposed;

    public ThrowingReadTestStream()
    {
        ReadCount = 0;
        WasDisposed = false;
    }

    public override bool CanRead => !WasDisposed;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ReadCount = ReadCount + 1;
        throw new IOException("simulated read failure");
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

    protected override void Dispose(bool disposing)
    {
        WasDisposed = true;
        base.Dispose(disposing);
    }
}

public class ThrowingWriteTestStream : Stream
{
    public bool WasDisposed;

    public ThrowingWriteTestStream()
    {
        WasDisposed = false;
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => !WasDisposed;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
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
        throw new IOException("simulated write failure");
    }

    protected override void Dispose(bool disposing)
    {
        WasDisposed = true;
        base.Dispose(disposing);
    }
}
