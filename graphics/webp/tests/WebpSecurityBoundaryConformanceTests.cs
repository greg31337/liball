using webp;

namespace webp.Tests;

[TestFixture]
public class WebpSecurityBoundaryConformanceTests
{
    [Test]
    public void DecodeReadAndValidateAllInputFormsRejectOversizedInputBeforeWork()
    {
        byte[] data = NewLosslessContainer(1, 1);
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.Limits.MaxRiffSize = data.Length - 1;
        WebpCodec codec = new WebpCodec();
        string path = NewTempPath();
        File.WriteAllBytes(path, data);

        try
        {
            Assert.That(codec.Decode(data, options).IsValid, Is.False);
            Assert.That(codec.DecodeSpan(data, options).IsValid, Is.False);
            Assert.That(HasError(codec.Read(data, options), "RIFF size"), Is.True);
            Assert.That(HasError(codec.ReadSpan(data, options), "RIFF size"), Is.True);
            Assert.That(codec.Validate(data, options).IsValid, Is.False);
            Assert.That(codec.ValidateSpan(data, options).IsValid, Is.False);
            Assert.Throws<ArgumentException>(() => codec.DecodeStream(new MemoryStream(data), options));
            Assert.Throws<ArgumentException>(() => codec.DecodeFile(path, options));
            Assert.Throws<ArgumentException>(() => codec.ReadStream(new MemoryStream(data), options));
            Assert.Throws<ArgumentException>(() => codec.ReadFile(path, options));
            Assert.Throws<ArgumentException>(() => codec.ValidateStream(new MemoryStream(data), options));
            Assert.Throws<ArgumentException>(() => codec.ValidateFile(path, options));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void SharedInspectionPreflightEnforcesDimensionsPixelProductsAndCounts()
    {
        byte[] animation = NewAnimatedContainer();
        WebpDecodeOptions width = new WebpDecodeOptions();
        width.Limits.MaxCanvasWidth = 3;
        WebpDecodeOptions rectanglePixels = new WebpDecodeOptions();
        rectanglePixels.Limits.MaxCanvasPixels = 7;
        WebpDecodeOptions chunks = new WebpDecodeOptions();
        chunks.Limits.MaxChunkCount = 5;
        WebpDecodeOptions frames = new WebpDecodeOptions();
        frames.Limits.MaxFrameCount = 1;
        WebpDecodeOptions totalPixels = new WebpDecodeOptions();
        totalPixels.Limits.MaxTotalPixels = 7;

        AssertLimitAcrossInspectionServices(animation, width, "dimensions");
        AssertLimitAcrossInspectionServices(animation, rectanglePixels, "maximum pixel count");
        AssertLimitAcrossInspectionServices(animation, chunks, "chunk count");
        AssertLimitAcrossInspectionServices(animation, frames, "frame count");
        AssertLimitAcrossInspectionServices(animation, totalPixels, "total pixel count");
    }

    [Test]
    public void SharedInspectionPreflightEnforcesMetadataUnknownAndAggregateBytes()
    {
        WebpPixelBuffer pixel = NewBuffer(1, 1);
        byte[] payload = WebpVp8LEncoder.Encode(pixel);
        WebpMetadata metadata = new WebpMetadata();
        metadata.Exif = new byte[] { 1, 2, 3 };
        byte[] metadataData = WebpContainerWriter.WriteExtendedStill(payload, WebpBitstreamKind.Lossless, metadata, null);
        List<WebpRiffChunk> unknownChunks = new List<WebpRiffChunk>();
        unknownChunks.Add(new WebpRiffChunk("VP8L", payload));
        unknownChunks.Add(new WebpRiffChunk("u001", new byte[] { 4, 5, 6 }));
        byte[] unknownData = WebpRiffWriter.Write("WEBP", unknownChunks);
        WebpDecodeOptions metadataOptions = new WebpDecodeOptions();
        metadataOptions.Limits.MaxMetadataBytes = 2;
        WebpDecodeOptions unknownOptions = new WebpDecodeOptions();
        unknownOptions.Limits.MaxUnknownChunkBytes = 2;
        WebpDecodeOptions aggregateOptions = new WebpDecodeOptions();
        aggregateOptions.Limits.MaxAggregateAllocationBytes = payload.Length - 1;

        AssertLimitAcrossInspectionServices(metadataData, metadataOptions, "metadata payload");
        AssertLimitAcrossInspectionServices(unknownData, unknownOptions, "unknown chunk payload");
        AssertLimitAcrossInspectionServices(unknownData, aggregateOptions, "aggregate allocation");
    }

    [Test]
    public void DecodeEnforcesVp8LSubresolutionRecursionDepth()
    {
        WebpPixelBuffer buffer = NewBuffer(3, 2);
        SetPixel(buffer, 0, 0, 10, 0, 1, 255);
        SetPixel(buffer, 1, 0, 20, 0, 2, 255);
        SetPixel(buffer, 2, 0, 30, 0, 3, 255);
        SetPixel(buffer, 0, 1, 10, 0, 1, 255);
        SetPixel(buffer, 1, 1, 20, 0, 2, 255);
        SetPixel(buffer, 2, 1, 30, 0, 3, 255);
        byte[] data = WebpContainerWriter.WriteBasicStill(WebpVp8LEncoder.Encode(buffer), WebpBitstreamKind.Lossless);
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.Limits.MaxRecursionDepth = 1;

        WebpDecodeResult result = new WebpCodec().Decode(data, options);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasError(result, "VP8L recursion depth"), Is.True, JoinDiagnostics(result.Diagnostics));
        Assert.That(result.Image.Frames, Has.Count.EqualTo(1));
        Assert.That(result.Image.Frames[0].Pixels, Is.Null);
    }

    [Test]
    public void MaximumVp8XPixelProductUsesWideArithmeticAcrossInspectionServices()
    {
        byte[] data = WriteChunks(new WebpRiffChunk(
            "VP8X",
            NewVp8XPayload(WebpConstants.Vp8XMaxCanvasDimension, WebpConstants.Vp8XMaxCanvasDimension)));
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.Limits.MaxCanvasWidth = WebpConstants.Vp8XMaxCanvasDimension;
        options.Limits.MaxCanvasHeight = WebpConstants.Vp8XMaxCanvasDimension;
        options.Limits.MaxCanvasPixels = uint.MaxValue;
        options.Limits.MaxTotalPixels = long.MaxValue;

        AssertLimitAcrossInspectionServices(data, options, "maximum pixel count");
    }

    [Test]
    public void ClaimedMaximumChunkLengthsNeverOverflowOrSliceOutsideInput()
    {
        byte[] retained = NewClaimedChunk("VP8 ", uint.MaxValue);
        byte[] metadata = NewClaimedChunk("EXIF", uint.MaxValue);
        byte[] frame = NewClaimedChunk("ANMF", uint.MaxValue);
        WebpCodec codec = new WebpCodec();

        Assert.DoesNotThrow(() => codec.Decode(retained, new WebpDecodeOptions()));
        Assert.DoesNotThrow(() => codec.ReadSpan(retained, new WebpDecodeOptions()));
        Assert.DoesNotThrow(() => codec.Validate(retained, new WebpDecodeOptions()));
        Assert.That(HasError(codec.Decode(retained, new WebpDecodeOptions()), "aggregate allocation"), Is.True);
        Assert.That(HasError(codec.Read(metadata, new WebpDecodeOptions()), "metadata payload"), Is.True);
        Assert.That(codec.ValidateSpan(frame, new WebpDecodeOptions()).IsValid, Is.False);
        Assert.Throws<ArgumentException>(() => codec.GetMetadata(metadata));
        Assert.Throws<ArgumentException>(() => codec.GetMetadataSpan(metadata));
        Assert.Throws<ArgumentException>(() => codec.EnumerateUnknownChunks(retained));
    }

    [Test]
    public void TruncatedInputIsContainedByEveryPublicInputWorkflow()
    {
        byte[] truncated = new byte[]
        {
            (byte)'R', (byte)'I', (byte)'F', (byte)'F',
            20, 0, 0, 0,
            (byte)'W', (byte)'E', (byte)'B', (byte)'P',
            (byte)'A', (byte)'N', (byte)'M', (byte)'F'
        };
        WebpCodec codec = new WebpCodec();
        string path = NewTempPath();
        File.WriteAllBytes(path, truncated);

        try
        {
            Assert.That(codec.HasRiffWebpSignature(truncated), Is.True);
            Assert.DoesNotThrow(() => codec.Decode(truncated, new WebpDecodeOptions()));
            Assert.DoesNotThrow(() => codec.DecodeSpan(truncated, new WebpDecodeOptions()));
            Assert.DoesNotThrow(() => codec.Read(truncated, new WebpDecodeOptions()));
            Assert.DoesNotThrow(() => codec.ReadSpan(truncated, new WebpDecodeOptions()));
            Assert.DoesNotThrow(() => codec.Validate(truncated, new WebpDecodeOptions()));
            Assert.DoesNotThrow(() => codec.ValidateSpan(truncated, new WebpDecodeOptions()));
            Assert.DoesNotThrow(() => codec.NormalizeContainer(truncated));
            Assert.DoesNotThrow(() => codec.NormalizeContainerSpan(truncated));
            Assert.DoesNotThrow(() => codec.NormalizeContainerStream(new MemoryStream(truncated)));
            Assert.DoesNotThrow(() => codec.NormalizeContainerFile(path));
            Assert.Throws<ArgumentException>(() => codec.GetMetadata(truncated));
            Assert.Throws<ArgumentException>(() => codec.EnumerateUnknownChunksSpan(truncated));
            Assert.Throws<ArgumentException>(() => codec.ExtractFrames(truncated));
            Assert.Throws<ArgumentException>(() => codec.SetAnimationLoopCountSpan(truncated, 1));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void EveryEncodeOutputFormRejectsLimitsBeforeWriting()
    {
        WebpPixelBuffer buffer = NewBuffer(2, 1);
        WebpFrame frame = new WebpFrame();
        frame.Width = 2;
        frame.Height = 1;
        frame.Pixels = buffer;
        WebpImage image = new WebpImage();
        image.CanvasWidth = 2;
        image.CanvasHeight = 1;
        image.Frames.Add(frame);
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Limits.MaxCanvasWidth = 1;
        WebpCodec codec = new WebpCodec();
        MemoryStream bufferStream = new MemoryStream();
        MemoryStream imageStream = new MemoryStream();
        string bufferPath = NewTempPath();
        string imagePath = NewTempPath();

        Assert.Throws<ArgumentException>(() => codec.Encode(buffer, options));
        Assert.Throws<ArgumentException>(() => codec.EncodeImage(image, options));
        Assert.Throws<ArgumentException>(() => codec.EncodeStream(buffer, bufferStream, options));
        Assert.Throws<ArgumentException>(() => codec.EncodeImageStream(image, imageStream, options));
        Assert.Throws<ArgumentException>(() => codec.EncodeFile(buffer, bufferPath, options));
        Assert.Throws<ArgumentException>(() => codec.EncodeImageFile(image, imagePath, options));
        Assert.That(bufferStream.Length, Is.Zero);
        Assert.That(imageStream.Length, Is.Zero);
        Assert.That(File.Exists(bufferPath), Is.False);
        Assert.That(File.Exists(imagePath), Is.False);
    }

    static void AssertLimitAcrossInspectionServices(byte[] data, WebpDecodeOptions options, string message)
    {
        WebpCodec codec = new WebpCodec();
        WebpDecodeResult decoded = codec.Decode(data, options);
        WebpInfo info = codec.ReadSpan(data, options);
        WebpValidationResult validation = codec.Validate(data, options);
        Assert.That(decoded.IsValid, Is.False);
        Assert.That(HasError(decoded, message), Is.True, JoinDiagnostics(decoded.Diagnostics));
        Assert.That(HasError(info, message), Is.True, JoinDiagnostics(info.Diagnostics));
        Assert.That(validation.IsValid, Is.False);
        Assert.That(HasError(validation, message), Is.True, JoinDiagnostics(validation.Diagnostics));
    }

    static bool HasError(WebpDecodeResult result, string message)
    {
        return HasError(result.Diagnostics, message);
    }

    static bool HasError(WebpInfo info, string message)
    {
        return HasError(info.Diagnostics, message);
    }

    static bool HasError(WebpValidationResult result, string message)
    {
        return HasError(result.Diagnostics, message);
    }

    static bool HasError(List<WebpDiagnostic> diagnostics, string message)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index].Severity == WebpDiagnosticSeverity.Error
                && diagnostics[index].Message.Contains(message))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static string JoinDiagnostics(List<WebpDiagnostic> diagnostics)
    {
        List<string> messages = new List<string>();
        int index = 0;
        while (index < diagnostics.Count)
        {
            messages.Add(diagnostics[index].Message);
            index = index + 1;
        }

        return string.Join("; ", messages);
    }

    static byte[] NewAnimatedContainer()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 4;
        image.CanvasHeight = 2;
        image.HasAnimation = true;
        image.Frames.Add(NewFrame(0, 2, 2));
        image.Frames.Add(NewFrame(2, 2, 2));
        return WebpContainerWriter.WriteAnimated(image);
    }

    static WebpFrame NewFrame(int x, int width, int height)
    {
        WebpPixelBuffer buffer = NewBuffer(width, height);
        WebpFrame frame = new WebpFrame();
        frame.X = x;
        frame.Width = width;
        frame.Height = height;
        frame.DurationMilliseconds = 1;
        frame.BitstreamKind = WebpBitstreamKind.Lossless;
        frame.EncodedBitstream = WebpVp8LEncoder.Encode(buffer);
        return frame;
    }

    static byte[] NewLosslessContainer(int width, int height)
    {
        WebpPixelBuffer buffer = NewBuffer(width, height);
        return WebpContainerWriter.WriteBasicStill(WebpVp8LEncoder.Encode(buffer), WebpBitstreamKind.Lossless);
    }

    static WebpPixelBuffer NewBuffer(int width, int height)
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(width, height);
        int index = 0;
        while (index < buffer.Rgba.Length)
        {
            buffer.Rgba[index] = 1;
            buffer.Rgba[index + 1] = 2;
            buffer.Rgba[index + 2] = 3;
            buffer.Rgba[index + 3] = 255;
            index = index + 4;
        }

        return buffer;
    }

    static void SetPixel(WebpPixelBuffer buffer, int x, int y, byte red, byte green, byte blue, byte alpha)
    {
        int offset = y * buffer.Stride + x * 4;
        buffer.Rgba[offset] = red;
        buffer.Rgba[offset + 1] = green;
        buffer.Rgba[offset + 2] = blue;
        buffer.Rgba[offset + 3] = alpha;
    }

    static byte[] NewVp8XPayload(int width, int height)
    {
        byte[] payload = new byte[WebpConstants.Vp8XChunkPayloadSize];
        WebpEndianWriter.WriteUInt24(payload, 4, (uint)(width - 1));
        WebpEndianWriter.WriteUInt24(payload, 7, (uint)(height - 1));
        return payload;
    }

    static byte[] NewClaimedChunk(string fourCc, uint payloadLength)
    {
        List<byte> data = new List<byte>();
        AppendFourCc(data, "RIFF");
        WebpEndianWriter.AppendUInt32(data, 12);
        AppendFourCc(data, "WEBP");
        AppendFourCc(data, fourCc);
        WebpEndianWriter.AppendUInt32(data, payloadLength);
        return data.ToArray();
    }

    static byte[] WriteChunks(params WebpRiffChunk[] chunks)
    {
        return WebpRiffWriter.Write("WEBP", new List<WebpRiffChunk>(chunks));
    }

    static void AppendFourCc(List<byte> data, string fourCc)
    {
        data.Add((byte)fourCc[0]);
        data.Add((byte)fourCc[1]);
        data.Add((byte)fourCc[2]);
        data.Add((byte)fourCc[3]);
    }

    static string NewTempPath()
    {
        return Path.Combine(TestContext.CurrentContext.WorkDirectory, "security-boundary-" + Guid.NewGuid().ToString("N") + ".webp");
    }
}
