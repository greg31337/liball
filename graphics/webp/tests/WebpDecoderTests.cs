using webp;

namespace webp.Tests;

[TestFixture]
public class WebpDecoderTests
{
    [Test]
    public void Decode_ByteArrayDecodesBasicLosslessContainer()
    {
        WebpPixelBuffer buffer = NewBuffer(2, 1);
        SetPixel(buffer, 0, 0, 10, 20, 30, 255);
        SetPixel(buffer, 1, 0, 40, 50, 60, 128);
        byte[] vp8L = WebpVp8LEncoder.Encode(buffer);
        byte[] data = WebpContainerWriter.WriteBasicStill(vp8L, WebpBitstreamKind.Lossless);

        WebpDecodeResult result = WebpDecoder.Decode(data);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Image.CanvasWidth, Is.EqualTo(2));
        Assert.That(result.Image.CanvasHeight, Is.EqualTo(1));
        Assert.That(result.Image.HasAlpha, Is.True);
        Assert.That(result.Image.Frames, Has.Count.EqualTo(1));
        Assert.That(result.Image.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(result.Image.Frames[0].Pixels, Is.Not.Null);
        Assert.That(result.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(buffer.Rgba));
        Assert.That(result.Image.Frames[0].EncodedBitstream, Is.EqualTo(vp8L));
    }

    [Test]
    public void Decode_ReadOnlySpanDecodesBasicLossyContainer()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        SetPixel(buffer, 0, 0, 20, 30, 40, 255);
        byte[] vp8 = WebpVp8KeyFrameEncoder.Encode(buffer);
        byte[] data = WebpContainerWriter.WriteBasicStill(vp8, WebpBitstreamKind.Lossy);

        WebpDecodeResult result = WebpDecoder.Decode(data.AsSpan());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Image.CanvasWidth, Is.EqualTo(1));
        Assert.That(result.Image.CanvasHeight, Is.EqualTo(1));
        Assert.That(result.Image.HasAlpha, Is.False);
        Assert.That(result.Image.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(result.Image.Frames[0].Pixels, Is.Not.Null);
        Assert.That(result.Image.Frames[0].Pixels!.Width, Is.EqualTo(1));
        Assert.That(result.Image.Frames[0].Pixels!.Height, Is.EqualTo(1));
        Assert.That(result.Image.Frames[0].Pixels!.Rgba[3], Is.EqualTo(255));
    }

    [Test]
    public void Decode_ExtendedLossyStillAppliesAlphPayload()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        SetPixel(buffer, 0, 0, 20, 30, 40, 128);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.AlphaCompression = WebpAlphaCompressionMethod.None;
        options.AlphaFilter = WebpAlphaFilterMode.None;
        byte[] data = WebpLossyAlphaEncoder.EncodeStill(buffer, options);

        WebpDecodeResult result = WebpDecoder.Decode(data, new WebpDecodeOptions());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Image.HasAlpha, Is.True);
        Assert.That(result.Image.Frames[0].Pixels, Is.Not.Null);
        Assert.That(result.Image.Frames[0].Pixels!.Rgba[3], Is.EqualTo(128));
        Assert.That(result.Image.Frames[0].AlphaData, Is.Null);
    }

    [Test]
    public void Decode_AnimatedContainerDecodesIndependentFrameRectangles()
    {
        WebpPixelBuffer firstPixels = NewBuffer(1, 1);
        SetPixel(firstPixels, 0, 0, 10, 20, 30, 255);
        byte[] data = NewAnimatedContainer(firstPixels);
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.UnknownChunkPolicy = WebpUnknownChunkPolicy.Preserve;

        WebpDecodeResult result = WebpDecoder.Decode(data, options);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Image.CanvasWidth, Is.EqualTo(4));
        Assert.That(result.Image.CanvasHeight, Is.EqualTo(2));
        Assert.That(result.Image.HasAnimation, Is.True);
        Assert.That(result.Image.HasAlpha, Is.True);
        Assert.That(result.Image.LoopCount, Is.EqualTo(3));
        Assert.That(result.Image.BackgroundColorBgra, Is.EqualTo(0x11223344U));
        Assert.That(result.Image.UnknownChunks, Has.Count.EqualTo(1));
        Assert.That(result.Image.UnknownChunks[0].FourCc, Is.EqualTo("zzzz"));
        Assert.That(result.Image.Frames, Has.Count.EqualTo(2));

        Assert.That(result.Image.Frames[0].X, Is.EqualTo(2));
        Assert.That(result.Image.Frames[0].Y, Is.EqualTo(0));
        Assert.That(result.Image.Frames[0].Width, Is.EqualTo(1));
        Assert.That(result.Image.Frames[0].Height, Is.EqualTo(1));
        Assert.That(result.Image.Frames[0].DurationMilliseconds, Is.EqualTo(50));
        Assert.That(result.Image.Frames[0].BlendMode, Is.EqualTo(WebpFrameBlendMode.Replace));
        Assert.That(result.Image.Frames[0].DisposeMode, Is.EqualTo(WebpFrameDisposeMode.DoNotDispose));
        Assert.That(result.Image.Frames[0].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossless));
        Assert.That(result.Image.Frames[0].Pixels, Is.Not.Null);
        Assert.That(result.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(firstPixels.Rgba));
        Assert.That(result.Image.Frames[0].UnknownChunks, Has.Count.EqualTo(1));
        Assert.That(result.Image.Frames[0].UnknownChunks[0].FourCc, Is.EqualTo("frmx"));

        Assert.That(result.Image.Frames[1].X, Is.EqualTo(0));
        Assert.That(result.Image.Frames[1].Y, Is.EqualTo(0));
        Assert.That(result.Image.Frames[1].Width, Is.EqualTo(1));
        Assert.That(result.Image.Frames[1].Height, Is.EqualTo(1));
        Assert.That(result.Image.Frames[1].DurationMilliseconds, Is.EqualTo(70));
        Assert.That(result.Image.Frames[1].BlendMode, Is.EqualTo(WebpFrameBlendMode.AlphaBlend));
        Assert.That(result.Image.Frames[1].DisposeMode, Is.EqualTo(WebpFrameDisposeMode.DisposeToBackground));
        Assert.That(result.Image.Frames[1].BitstreamKind, Is.EqualTo(WebpBitstreamKind.Lossy));
        Assert.That(result.Image.Frames[1].Pixels, Is.Not.Null);
        Assert.That(result.Image.Frames[1].Pixels!.Width, Is.EqualTo(1));
        Assert.That(result.Image.Frames[1].Pixels!.Height, Is.EqualTo(1));
        Assert.That(result.Image.Frames[1].Pixels!.Rgba[3], Is.EqualTo(64));
        Assert.That(result.Image.Frames[1].AlphaData, Is.Null);
    }

    [Test]
    public void Decode_AnimatedIdentifyOnlyKeepsFrameMetadataWithoutPixels()
    {
        WebpPixelBuffer firstPixels = NewBuffer(1, 1);
        SetPixel(firstPixels, 0, 0, 10, 20, 30, 255);
        byte[] data = NewAnimatedContainer(firstPixels);
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.IdentifyOnly = true;

        WebpDecodeResult result = WebpDecoder.Decode(data, options);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Image.HasAnimation, Is.True);
        Assert.That(result.Image.Frames, Has.Count.EqualTo(2));
        Assert.That(result.Image.Frames[0].X, Is.EqualTo(2));
        Assert.That(result.Image.Frames[0].DurationMilliseconds, Is.EqualTo(50));
        Assert.That(result.Image.Frames[0].Pixels, Is.Null);
        Assert.That(result.Image.Frames[0].EncodedBitstream, Is.Not.Null);
        Assert.That(result.Image.Frames[1].DisposeMode, Is.EqualTo(WebpFrameDisposeMode.DisposeToBackground));
        Assert.That(result.Image.Frames[1].Pixels, Is.Null);
        Assert.That(result.Image.Frames[1].EncodedBitstream, Is.Not.Null);
    }

    [Test]
    public void Decode_AnimatedFrameIndexSelectsIndependentFrameRectangle()
    {
        WebpPixelBuffer firstPixels = NewBuffer(1, 1);
        SetPixel(firstPixels, 0, 0, 10, 20, 30, 255);
        byte[] data = NewAnimatedContainer(firstPixels);
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.FrameIndex = 1;

        WebpDecodeResult result = WebpDecoder.Decode(data, options);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Image.HasAnimation, Is.False);
        Assert.That(result.Image.Frames, Has.Count.EqualTo(1));
        Assert.That(result.Image.Frames[0].X, Is.EqualTo(0));
        Assert.That(result.Image.Frames[0].DurationMilliseconds, Is.EqualTo(70));
        Assert.That(result.Image.Frames[0].DisposeMode, Is.EqualTo(WebpFrameDisposeMode.DisposeToBackground));
        Assert.That(result.Image.Frames[0].Pixels, Is.Not.Null);
        Assert.That(result.Image.Frames[0].Pixels!.Rgba[3], Is.EqualTo(64));
    }

    [Test]
    public void Decode_AnimatedCompositedModeReturnsFullCanvasFrames()
    {
        WebpPixelBuffer firstPixels = NewBuffer(1, 1);
        SetPixel(firstPixels, 0, 0, 10, 20, 30, 255);
        byte[] data = NewAnimatedContainer(firstPixels);
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.AnimationDecodeMode = WebpAnimationDecodeMode.CompositedFrames;

        WebpDecodeResult result = WebpDecoder.Decode(data, options);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Image.CanvasWidth, Is.EqualTo(4));
        Assert.That(result.Image.CanvasHeight, Is.EqualTo(2));
        Assert.That(result.Image.HasAnimation, Is.True);
        Assert.That(result.Image.Frames, Has.Count.EqualTo(2));
        Assert.That(result.Image.Frames[0].X, Is.EqualTo(0));
        Assert.That(result.Image.Frames[0].Y, Is.EqualTo(0));
        Assert.That(result.Image.Frames[0].Width, Is.EqualTo(4));
        Assert.That(result.Image.Frames[0].Height, Is.EqualTo(2));
        Assert.That(result.Image.Frames[0].DurationMilliseconds, Is.EqualTo(50));
        Assert.That(result.Image.Frames[0].Pixels, Is.Not.Null);
        Assert.That(result.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(new byte[]
        {
            34, 51, 68, 17,
            34, 51, 68, 17,
            10, 20, 30, 255,
            34, 51, 68, 17,
            34, 51, 68, 17,
            34, 51, 68, 17,
            34, 51, 68, 17,
            34, 51, 68, 17
        }));
        Assert.That(result.Image.Frames[1].X, Is.EqualTo(0));
        Assert.That(result.Image.Frames[1].Y, Is.EqualTo(0));
        Assert.That(result.Image.Frames[1].Width, Is.EqualTo(4));
        Assert.That(result.Image.Frames[1].Height, Is.EqualTo(2));
        Assert.That(result.Image.Frames[1].DurationMilliseconds, Is.EqualTo(70));
        Assert.That(result.Image.Frames[1].BlendMode, Is.EqualTo(WebpFrameBlendMode.Replace));
        Assert.That(result.Image.Frames[1].DisposeMode, Is.EqualTo(WebpFrameDisposeMode.DoNotDispose));
        Assert.That(result.Image.Frames[1].Pixels, Is.Not.Null);
        Assert.That(result.Image.Frames[1].Pixels!.Rgba[3], Is.EqualTo(77));
        Assert.That(result.Image.Frames[1].Pixels!.Rgba[8], Is.EqualTo(10));
        Assert.That(result.Image.Frames[1].Pixels!.Rgba[9], Is.EqualTo(20));
        Assert.That(result.Image.Frames[1].Pixels!.Rgba[10], Is.EqualTo(30));
        Assert.That(result.Image.Frames[1].Pixels!.Rgba[11], Is.EqualTo(255));
    }

    [Test]
    public void Decode_IdentifyOnlySkipsPixelDecodeButKeepsFrameInfo()
    {
        WebpPixelBuffer buffer = NewBuffer(1, 1);
        SetPixel(buffer, 0, 0, 10, 20, 30, 255);
        byte[] data = WebpContainerWriter.WriteBasicStill(WebpVp8LEncoder.Encode(buffer), WebpBitstreamKind.Lossless);
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.IdentifyOnly = true;

        WebpDecodeResult result = WebpDecoder.Decode(data, options);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Image.CanvasWidth, Is.EqualTo(1));
        Assert.That(result.Image.Frames[0].Pixels, Is.Null);
        Assert.That(result.Image.Frames[0].EncodedBitstream, Is.Not.Null);
    }

    [Test]
    public void Decode_StreamReadsSeekableStreamFromCurrentPosition()
    {
        byte[] data = NewLosslessContainer(1, 1);
        byte[] prefixed = new byte[data.Length + 3];
        prefixed[0] = 9;
        prefixed[1] = 8;
        prefixed[2] = 7;
        Array.Copy(data, 0, prefixed, 3, data.Length);
        MemoryStream stream = new MemoryStream(prefixed);
        stream.Position = 3;

        WebpDecodeResult result = WebpDecoder.Decode(stream);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Image.CanvasWidth, Is.EqualTo(1));
        Assert.That(stream.Position, Is.EqualTo(stream.Length));
    }

    [Test]
    public void Decode_StreamReadsNonSeekableStream()
    {
        byte[] data = NewLosslessContainer(2, 1);
        NonSeekableReadStream stream = new NonSeekableReadStream(data);

        WebpDecodeResult result = WebpDecoder.Decode(stream, new WebpDecodeOptions());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Image.CanvasWidth, Is.EqualTo(2));
        Assert.That(result.Image.CanvasHeight, Is.EqualTo(1));
        Assert.That(result.Image.Frames[0].Pixels, Is.Not.Null);
    }

    [Test]
    public void Decode_FilePathReadsAndDecodesImage()
    {
        byte[] data = NewLosslessContainer(1, 1);
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "decode-path-" + Guid.NewGuid().ToString("N") + ".webp");
        File.WriteAllBytes(path, data);

        WebpDecodeResult result = WebpDecoder.Decode(path);
        File.Delete(path);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Image.CanvasWidth, Is.EqualTo(1));
        Assert.That(result.Image.Frames[0].Pixels, Is.Not.Null);
    }

    [Test]
    public void Decode_FilePathUsesOptions()
    {
        byte[] data = NewLosslessContainer(1, 1);
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "decode-path-" + Guid.NewGuid().ToString("N") + ".webp");
        File.WriteAllBytes(path, data);
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.IdentifyOnly = true;

        WebpDecodeResult result = WebpDecoder.Decode(path, options);
        File.Delete(path);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Image.Frames[0].Pixels, Is.Null);
        Assert.That(result.Image.Frames[0].EncodedBitstream, Is.Not.Null);
    }

    [Test]
    public void Decode_ReturnsDiagnosticsForInvalidInput()
    {
        WebpDecodeResult result = WebpDecoder.Decode(new byte[] { 1, 2, 3 });

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics, Is.Not.Empty);
        Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(WebpDiagnosticSeverity.Error));
    }

    [Test]
    public void Decode_RejectsNullByteArrayInputs()
    {
        Assert.Throws<ArgumentNullException>(() => WebpDecoder.Decode((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => WebpDecoder.Decode((byte[])null!, new WebpDecodeOptions()));
        Assert.Throws<ArgumentNullException>(() => WebpDecoder.Decode(new byte[] { 1 }, null!));
    }

    [Test]
    public void Decode_RejectsInvalidStreamInputs()
    {
        Assert.Throws<ArgumentNullException>(() => WebpDecoder.Decode((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => WebpDecoder.Decode(new MemoryStream(new byte[] { 1 }), null!));
        Assert.Throws<ArgumentException>(() => WebpDecoder.Decode(new WriteOnlyTestStream()));
    }

    [Test]
    public void Decode_RejectsInvalidPathInputs()
    {
        string missing = Path.Combine(TestContext.CurrentContext.WorkDirectory, "missing-" + Guid.NewGuid().ToString("N") + ".webp");

        Assert.Throws<ArgumentNullException>(() => WebpDecoder.Decode((string)null!));
        Assert.Throws<ArgumentNullException>(() => WebpDecoder.Decode(missing, null!));
        Assert.Throws<FileNotFoundException>(() => WebpDecoder.Decode(missing));
    }

    static byte[] NewLosslessContainer(int width, int height)
    {
        WebpPixelBuffer buffer = NewBuffer(width, height);
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                SetPixel(buffer, x, y, (byte)(10 + x), (byte)(20 + y), 30, 255);
                x = x + 1;
            }

            y = y + 1;
        }

        return WebpContainerWriter.WriteBasicStill(WebpVp8LEncoder.Encode(buffer), WebpBitstreamKind.Lossless);
    }

    static byte[] NewAnimatedContainer(WebpPixelBuffer firstPixels)
    {
        WebpPixelBuffer secondPixels = NewBuffer(1, 1);
        SetPixel(secondPixels, 0, 0, 90, 80, 70, 255);

        WebpImage image = new WebpImage();
        image.CanvasWidth = 4;
        image.CanvasHeight = 2;
        image.HasAnimation = true;
        image.HasAlpha = true;
        image.LoopCount = 3;
        image.BackgroundColorBgra = 0x11223344U;
        image.UnknownChunks.Add(new WebpUnknownChunk("zzzz", new byte[] { 9 }));

        WebpFrame first = new WebpFrame();
        first.X = 2;
        first.Y = 0;
        first.Width = 1;
        first.Height = 1;
        first.DurationMilliseconds = 50;
        first.BlendMode = WebpFrameBlendMode.Replace;
        first.DisposeMode = WebpFrameDisposeMode.DoNotDispose;
        first.BitstreamKind = WebpBitstreamKind.Lossless;
        first.EncodedBitstream = WebpVp8LEncoder.Encode(firstPixels);
        first.UnknownChunks.Add(new WebpUnknownChunk("frmx", new byte[] { 7 }));
        image.Frames.Add(first);

        WebpFrame second = new WebpFrame();
        second.X = 0;
        second.Y = 0;
        second.Width = 1;
        second.Height = 1;
        second.DurationMilliseconds = 70;
        second.BlendMode = WebpFrameBlendMode.AlphaBlend;
        second.DisposeMode = WebpFrameDisposeMode.DisposeToBackground;
        second.BitstreamKind = WebpBitstreamKind.Lossy;
        second.EncodedBitstream = WebpVp8KeyFrameEncoder.Encode(secondPixels);
        second.AlphaData = WebpAlphaEncoder.EncodeUncompressed(new byte[] { 64 }, 1, 1);
        image.Frames.Add(second);

        return WebpContainerWriter.WriteAnimated(image);
    }

    static WebpPixelBuffer NewBuffer(int width, int height)
    {
        return WebpPixelBufferHelper.AllocateRgba(width, height);
    }

    static void SetPixel(WebpPixelBuffer buffer, int x, int y, byte red, byte green, byte blue, byte alpha)
    {
        int offset = (y * buffer.Stride) + (x * 4);
        buffer.Rgba[offset] = red;
        buffer.Rgba[offset + 1] = green;
        buffer.Rgba[offset + 2] = blue;
        buffer.Rgba[offset + 3] = alpha;
    }

    class NonSeekableReadStream : Stream
    {
        readonly byte[] Data;
        int Offset;

        public NonSeekableReadStream(byte[] data)
        {
            Data = WebpUnknownChunk.CopyBytes(data);
            Offset = 0;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => Offset;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Offset >= Data.Length)
            {
                return 0;
            }

            int remaining = Data.Length - Offset;
            if (count > remaining)
            {
                count = remaining;
            }

            Array.Copy(Data, Offset, buffer, offset, count);
            Offset = Offset + count;
            return count;
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
    }

    class WriteOnlyTestStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => 0;
        public override long Position
        {
            get => 0;
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
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
        }
    }
}
