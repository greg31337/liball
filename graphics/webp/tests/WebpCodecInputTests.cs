using webp;

namespace webp.Tests;

[TestFixture]
public class WebpCodecInputTests
{
    [Test]
    public void SeekableAndNonSeekableStreamReadersUseCurrentPositionAndLeaveStreamsOpen()
    {
        WebpCodec codec = new WebpCodec();
        IWebpDecoder decoder = codec;
        IWebpInfoReader infoReader = codec;
        IWebpValidator validator = codec;
        IWebpMux mux = codec;
        WebpDecodeOptions options = new WebpDecodeOptions();
        byte[] data = EncodeStill(codec);
        MemoryStream decodeStream = NewPositionedStream(data);
        MemoryStream infoStream = NewPositionedStream(data);
        MemoryStream validationStream = NewPositionedStream(data);
        MemoryStream metadataStream = NewPositionedStream(data);

        WebpDecodeResult decoded = decoder.DecodeStream(decodeStream, options);
        WebpInfo info = infoReader.ReadStream(infoStream, options);
        WebpValidationResult validation = validator.ValidateStream(validationStream, options);
        WebpMetadata metadata = mux.GetMetadataStream(metadataStream);

        Assert.That(decoded.IsValid, Is.True);
        Assert.That(info.Width, Is.EqualTo(2));
        Assert.That(validation.IsValid, Is.True);
        Assert.That(metadata.IccProfile, Is.Null);
        AssertConsumedAndOpen(decodeStream);
        AssertConsumedAndOpen(infoStream);
        AssertConsumedAndOpen(validationStream);
        AssertConsumedAndOpen(metadataStream);

        NonSeekableReadStream nonSeekableDecode = new NonSeekableReadStream(data);
        NonSeekableReadStream nonSeekableInfo = new NonSeekableReadStream(data);
        NonSeekableReadStream nonSeekableValidation = new NonSeekableReadStream(data);
        NonSeekableReadStream nonSeekableMetadata = new NonSeekableReadStream(data);

        Assert.That(decoder.DecodeStream(nonSeekableDecode, options).IsValid, Is.True);
        Assert.That(infoReader.ReadStream(nonSeekableInfo, options).Width, Is.EqualTo(2));
        Assert.That(validator.ValidateStream(nonSeekableValidation, options).IsValid, Is.True);
        Assert.That(mux.GetMetadataStream(nonSeekableMetadata).Exif, Is.Null);
        AssertNonSeekableConsumedAndOpen(nonSeekableDecode, data.Length);
        AssertNonSeekableConsumedAndOpen(nonSeekableInfo, data.Length);
        AssertNonSeekableConsumedAndOpen(nonSeekableValidation, data.Length);
        AssertNonSeekableConsumedAndOpen(nonSeekableMetadata, data.Length);
    }

    [Test]
    public void MetadataStreamEntryPointsSupportEveryMuxOperation()
    {
        WebpCodec codec = new WebpCodec();
        IWebpMux mux = codec;
        byte[] original = EncodeStill(codec);
        WebpMetadata initial = NewMetadata(1, 2, 3);
        byte[] source = mux.ReplaceMetadata(original, initial);
        MemoryStream readStream = NewPositionedStream(source);
        MemoryStream replaceStream = NewPositionedStream(source);
        MemoryStream stripStream = NewPositionedStream(source);
        MemoryStream iccStream = NewPositionedStream(source);
        MemoryStream exifStream = NewPositionedStream(source);
        MemoryStream xmpStream = NewPositionedStream(source);

        WebpMetadata read = mux.GetMetadataStream(readStream);
        byte[] replaced = mux.ReplaceMetadataStream(replaceStream, NewMetadata(4, 5, 6));
        byte[] stripped = mux.StripMetadataStream(stripStream);
        byte[] iccChanged = mux.SetIccProfileStream(iccStream, new byte[] { 7 });
        byte[] exifChanged = mux.SetExifStream(exifStream, new byte[] { 8 });
        byte[] xmpChanged = mux.SetXmpStream(xmpStream, new byte[] { 9 });

        Assert.That(read.IccProfile, Is.EqualTo(new byte[] { 1 }));
        AssertMetadata(mux.GetMetadata(replaced), 4, 5, 6);
        Assert.That(mux.GetMetadata(stripped).IccProfile, Is.Null);
        Assert.That(mux.GetMetadata(iccChanged).IccProfile, Is.EqualTo(new byte[] { 7 }));
        Assert.That(mux.GetMetadata(exifChanged).Exif, Is.EqualTo(new byte[] { 8 }));
        Assert.That(mux.GetMetadata(xmpChanged).Xmp, Is.EqualTo(new byte[] { 9 }));
        AssertConsumedAndOpen(readStream);
        AssertConsumedAndOpen(replaceStream);
        AssertConsumedAndOpen(stripStream);
        AssertConsumedAndOpen(iccStream);
        AssertConsumedAndOpen(exifStream);
        AssertConsumedAndOpen(xmpStream);
    }

    [Test]
    public void AnimationStreamEntryPointsSupportExtractionAndGlobalEdits()
    {
        WebpCodec codec = new WebpCodec();
        IWebpMux mux = codec;
        byte[] data = EncodeAnimation(codec);
        MemoryStream framesStream = NewPositionedStream(data);
        MemoryStream frameStream = NewPositionedStream(data);
        MemoryStream loopStream = NewPositionedStream(data);
        MemoryStream backgroundStream = NewPositionedStream(data);
        MemoryStream bothStream = NewPositionedStream(data);

        List<WebpFrame> frames = mux.ExtractFramesStream(framesStream);
        WebpFrame frame = mux.ExtractFrameStream(frameStream, 1);
        byte[] loopChanged = mux.SetAnimationLoopCountStream(loopStream, 7);
        byte[] backgroundChanged = mux.SetAnimationBackgroundColorStream(backgroundStream, 0xAABBCCDDU);
        byte[] bothChanged = mux.SetAnimationLoopAndBackgroundStream(bothStream, 0, 0x01020304U);

        Assert.That(frames, Has.Count.EqualTo(2));
        Assert.That(frame.X, Is.EqualTo(2));
        AssertAnimationGlobals(loopChanged, 7, 0xFF010203U);
        AssertAnimationGlobals(backgroundChanged, 4, 0xAABBCCDDU);
        AssertAnimationGlobals(bothChanged, 0, 0x01020304U);
        AssertConsumedAndOpen(framesStream);
        AssertConsumedAndOpen(frameStream);
        AssertConsumedAndOpen(loopStream);
        AssertConsumedAndOpen(backgroundStream);
        AssertConsumedAndOpen(bothStream);
    }

    [Test]
    public void FileEntryPointsSupportDecodeIdentifyValidateAndMetadataMuxOperations()
    {
        WebpCodec codec = new WebpCodec();
        IWebpDecoder decoder = codec;
        IWebpInfoReader infoReader = codec;
        IWebpValidator validator = codec;
        IWebpMux mux = codec;
        WebpDecodeOptions options = new WebpDecodeOptions();
        byte[] original = EncodeStill(codec);
        byte[] source = mux.ReplaceMetadata(original, NewMetadata(1, 2, 3));
        string path = NewTempPath();
        File.WriteAllBytes(path, source);

        try
        {
            Assert.That(decoder.DecodeFile(path, options).IsValid, Is.True);
            Assert.That(infoReader.ReadFile(path, options).Width, Is.EqualTo(2));
            Assert.That(validator.ValidateFile(path, options).IsValid, Is.True);
            AssertMetadata(mux.GetMetadataFile(path), 1, 2, 3);
            AssertMetadata(mux.GetMetadata(mux.ReplaceMetadataFile(path, NewMetadata(4, 5, 6))), 4, 5, 6);
            Assert.That(mux.GetMetadata(mux.StripMetadataFile(path)).Exif, Is.Null);
            Assert.That(mux.GetMetadata(mux.SetIccProfileFile(path, new byte[] { 7 })).IccProfile, Is.EqualTo(new byte[] { 7 }));
            Assert.That(mux.GetMetadata(mux.SetExifFile(path, new byte[] { 8 })).Exif, Is.EqualTo(new byte[] { 8 }));
            Assert.That(mux.GetMetadata(mux.SetXmpFile(path, new byte[] { 9 })).Xmp, Is.EqualTo(new byte[] { 9 }));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void AnimationFileEntryPointsSupportExtractionAndGlobalEdits()
    {
        WebpCodec codec = new WebpCodec();
        IWebpMux mux = codec;
        string path = NewTempPath();
        File.WriteAllBytes(path, EncodeAnimation(codec));

        try
        {
            Assert.That(mux.ExtractFramesFile(path), Has.Count.EqualTo(2));
            Assert.That(mux.ExtractFrameFile(path, 1).X, Is.EqualTo(2));
            AssertAnimationGlobals(mux.SetAnimationLoopCountFile(path, 7), 7, 0xFF010203U);
            AssertAnimationGlobals(mux.SetAnimationBackgroundColorFile(path, 0xAABBCCDDU), 4, 0xAABBCCDDU);
            AssertAnimationGlobals(mux.SetAnimationLoopAndBackgroundFile(path, 0, 0x01020304U), 0, 0x01020304U);
        }
        finally
        {
            File.Delete(path);
        }
    }

    static void AssertConsumedAndOpen(MemoryStream stream)
    {
        Assert.That(stream.Position, Is.EqualTo(stream.Length));
        Assert.That(stream.CanRead, Is.True);
    }

    static void AssertNonSeekableConsumedAndOpen(NonSeekableReadStream stream, int expectedBytes)
    {
        Assert.That(stream.BytesRead, Is.EqualTo(expectedBytes));
        Assert.That(stream.WasDisposed, Is.False);
        Assert.That(stream.CanRead, Is.True);
    }

    static void AssertMetadata(WebpMetadata metadata, byte icc, byte exif, byte xmp)
    {
        Assert.That(metadata.IccProfile, Is.EqualTo(new byte[] { icc }));
        Assert.That(metadata.Exif, Is.EqualTo(new byte[] { exif }));
        Assert.That(metadata.Xmp, Is.EqualTo(new byte[] { xmp }));
    }

    static void AssertAnimationGlobals(byte[] data, ushort loopCount, uint backgroundColor)
    {
        WebpExtendedContainerParseResult parsed = WebpExtendedContainerParser.Parse(data, WebpStrictness.Strict);
        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.AnimationLoopCount, Is.EqualTo(loopCount));
        Assert.That(parsed.AnimationBackgroundColorBgra, Is.EqualTo(backgroundColor));
        Assert.That(parsed.Frames, Has.Count.EqualTo(2));
    }

    static WebpMetadata NewMetadata(byte icc, byte exif, byte xmp)
    {
        WebpMetadata metadata = new WebpMetadata();
        metadata.IccProfile = new byte[] { icc };
        metadata.Exif = new byte[] { exif };
        metadata.Xmp = new byte[] { xmp };
        return metadata;
    }

    static MemoryStream NewPositionedStream(byte[] data)
    {
        byte[] prefixed = new byte[data.Length + 3];
        prefixed[0] = 90;
        prefixed[1] = 91;
        prefixed[2] = 92;
        Array.Copy(data, 0, prefixed, 3, data.Length);
        MemoryStream stream = new MemoryStream(prefixed);
        stream.Position = 3;
        return stream;
    }

    static string NewTempPath()
    {
        return Path.Combine(TestContext.CurrentContext.WorkDirectory, "webp-input-" + Guid.NewGuid().ToString("N") + ".webp");
    }

    static byte[] EncodeStill(WebpCodec codec)
    {
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossless;
        return codec.Encode(NewBuffer(2, 1, 10, 20, 30, 255), options);
    }

    static byte[] EncodeAnimation(WebpCodec codec)
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 3;
        image.CanvasHeight = 1;
        image.HasAnimation = true;
        image.LoopCount = 4;
        image.BackgroundColorBgra = 0xFF010203U;
        image.Frames.Add(NewFrame(0, 25, NewBuffer(1, 1, 255, 0, 0, 255)));
        image.Frames.Add(NewFrame(2, 40, NewBuffer(1, 1, 0, 0, 255, 255)));
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = WebpEncodeMode.Lossless;
        return codec.EncodeImage(image, options);
    }

    static WebpFrame NewFrame(int x, int duration, WebpPixelBuffer pixels)
    {
        WebpFrame frame = new WebpFrame();
        frame.X = x;
        frame.Y = 0;
        frame.Width = 1;
        frame.Height = 1;
        frame.DurationMilliseconds = duration;
        frame.BlendMode = WebpFrameBlendMode.Replace;
        frame.DisposeMode = WebpFrameDisposeMode.DoNotDispose;
        frame.BitstreamKind = WebpBitstreamKind.Lossless;
        frame.Pixels = pixels;
        return frame;
    }

    static WebpPixelBuffer NewBuffer(int width, int height, byte red, byte green, byte blue, byte alpha)
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(width, height);
        int offset = 0;
        while (offset < buffer.Rgba.Length)
        {
            buffer.Rgba[offset] = red;
            buffer.Rgba[offset + 1] = green;
            buffer.Rgba[offset + 2] = blue;
            buffer.Rgba[offset + 3] = alpha;
            offset = offset + 4;
        }

        return buffer;
    }
}

public class NonSeekableReadStream : Stream
{
    MemoryStream inner;
    public int BytesRead;
    public bool WasDisposed;

    public NonSeekableReadStream(byte[] data)
    {
        inner = new MemoryStream(data);
        BytesRead = 0;
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
        int read = inner.Read(buffer, offset, count);
        BytesRead = BytesRead + read;
        return read;
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
        inner.Dispose();
        base.Dispose(disposing);
    }
}
