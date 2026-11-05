using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PngProgressiveDecoderTests
{
    [Test]
    public void NonInterlacedDecode_DeliversRowsAndStableOptionalPreviews()
    {
        byte[] pixels = new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8,
            11, 12, 13, 14, 15, 16, 17, 18,
            21, 22, 23, 24, 25, 26, 27, 28
        };
        byte[] png = CreatePng(2, 3, pixels, PngInterlaceMethod.None);
        PngDecoderOptions options = new PngDecoderOptions();
        options.PreferredPixelFormat = PngPixelFormat.Rgb8;
        RecordingProgressiveSink sink = new RecordingProgressiveSink();
        sink.RequestPreviews = true;
        IPngProgressiveDecoder decoder = new PngProgressiveDecoder();

        PngImage image = decoder.DecodeProgressiveBytes(png, options, sink);
        PngImage full = Png.DecodeBytes(png, options);

        Assert.That(sink.Rows.Select(value => value.RowIndex), Is.EqualTo(new int[] { 0, 1, 2 }));
        Assert.That(sink.Rows.Select(value => value.Pixels.Data), Is.EqualTo(new byte[][]
        {
            new byte[] { 1, 2, 3, 5, 6, 7 },
            new byte[] { 11, 12, 13, 15, 16, 17 },
            new byte[] { 21, 22, 23, 25, 26, 27 }
        }));
        Assert.That(sink.Passes, Is.Empty);
        Assert.That(sink.Previews, Has.Count.EqualTo(3));
        Assert.That(sink.Previews.Select(value => value.PassIndex), Is.All.EqualTo(0));
        Assert.That(sink.Previews.Select(value => value.UpdateIndex), Is.EqualTo(new int[] { 1, 2, 3 }));
        Assert.That(sink.Previews[0].Pixels.Data, Is.EqualTo(new byte[]
        {
            1, 2, 3, 5, 6, 7,
            0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0
        }));
        Assert.That(sink.Previews[2].Pixels.Data, Is.EqualTo(full.Pixels.Data));
        Assert.That(image.Pixels.Data, Is.EqualTo(full.Pixels.Data));
        Assert.That(sink.WantsPreviewsCallCount, Is.EqualTo(1));
    }

    [Test]
    public void Adam7Decode_DeliversEveryPassAndCumulativePreviews()
    {
        byte[] pixels = CreateRgbaPixels(8, 8);
        byte[] png = CreatePng(8, 8, pixels, PngInterlaceMethod.Adam7);
        PngDecoderOptions options = new PngDecoderOptions();
        RecordingProgressiveSink sink = new RecordingProgressiveSink();
        sink.RequestPreviews = true;

        PngImage image = new PngProgressiveDecoder().DecodeProgressiveBytes(png, options, sink);
        PngImage full = Png.DecodeBytes(png, options);

        Assert.That(sink.Rows, Is.Empty);
        Assert.That(sink.Passes.Select(value => value.PassIndex), Is.EqualTo(new int[] { 1, 2, 3, 4, 5, 6, 7 }));
        Assert.That(sink.Passes.Select(value => value.Width), Is.EqualTo(new int[] { 1, 1, 2, 2, 4, 4, 8 }));
        Assert.That(sink.Passes.Select(value => value.Height), Is.EqualTo(new int[] { 1, 1, 1, 2, 2, 4, 4 }));
        Assert.That(sink.Passes.Select(value => value.RowsDecoded), Is.EqualTo(sink.Passes.Select(value => value.Height)));
        Assert.That(sink.Passes.Select(value => value.IsEmpty), Is.All.False);
        Assert.That(sink.Previews.Select(value => value.PassIndex), Is.EqualTo(new int[] { 1, 2, 3, 4, 5, 6, 7 }));
        Assert.That(sink.Previews[6].Pixels.Data, Is.EqualTo(full.Pixels.Data));
        Assert.That(image.Pixels.Data, Is.EqualTo(full.Pixels.Data));
        Assert.That(sink.Previews[0].Pixels.Data, Is.Not.SameAs(sink.Previews[6].Pixels.Data));
    }

    [Test]
    public void Adam7Decode_ReportsEmptyPassesWithoutDuplicatePreviews()
    {
        byte[] png = CreatePng(1, 1, new byte[] { 1, 2, 3, 4 }, PngInterlaceMethod.Adam7);
        RecordingProgressiveSink sink = new RecordingProgressiveSink();
        sink.RequestPreviews = true;

        new PngProgressiveDecoder().DecodeProgressiveBytes(png, new PngDecoderOptions(), sink);

        Assert.That(sink.Passes, Has.Count.EqualTo(7));
        Assert.That(sink.Passes.Select(value => value.IsEmpty), Is.EqualTo(new bool[] { false, true, true, true, true, true, true }));
        Assert.That(sink.Previews, Has.Count.EqualTo(1));
        Assert.That(sink.Previews[0].PassIndex, Is.EqualTo(1));
    }

    [Test]
    public void ProgressiveEntryPoints_ReturnEquivalentResultsWithoutRequiredPreviews()
    {
        byte[] png = CreatePng(1, 2, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, PngInterlaceMethod.None);
        byte[] prefixed = Join(new byte[] { 90, 91 }, png);
        byte[] bounded = Join(new byte[] { 70 }, png, new byte[] { 80 });
        IPngProgressiveDecoder decoder = new PngProgressiveDecoder();
        RecordingProgressiveSink bytesSink = new RecordingProgressiveSink();
        RecordingProgressiveSink offsetSink = new RecordingProgressiveSink();
        RecordingProgressiveSink spanSink = new RecordingProgressiveSink();
        RecordingProgressiveSink streamSink = new RecordingProgressiveSink();

        PngImage bytes = decoder.DecodeProgressiveBytes(png, new PngDecoderOptions(), bytesSink);
        PngImage offset = decoder.DecodeProgressiveBytesAtOffset(prefixed, 2, new PngDecoderOptions(), offsetSink);
        PngImage span = decoder.DecodeProgressiveSpan(bounded.AsSpan(1, png.Length), new PngDecoderOptions(), spanSink);
        PngImage stream = decoder.DecodeProgressiveStream(new MemoryStream(png), new PngDecoderOptions(), streamSink);

        Assert.That(offset.Pixels.Data, Is.EqualTo(bytes.Pixels.Data));
        Assert.That(span.Pixels.Data, Is.EqualTo(bytes.Pixels.Data));
        Assert.That(stream.Pixels.Data, Is.EqualTo(bytes.Pixels.Data));
        Assert.That(bytesSink.Rows, Has.Count.EqualTo(2));
        Assert.That(offsetSink.Rows, Has.Count.EqualTo(2));
        Assert.That(spanSink.Rows, Has.Count.EqualTo(2));
        Assert.That(streamSink.Rows, Has.Count.EqualTo(2));
        Assert.That(bytesSink.Previews, Is.Empty);
        Assert.That(offsetSink.Previews, Is.Empty);
        Assert.That(spanSink.Previews, Is.Empty);
        Assert.That(streamSink.Previews, Is.Empty);
    }

    [Test]
    public void AnimationDecode_DeliversFramesAccordingToDecoderOption()
    {
        byte[] png = CreateAnimatedPng();
        RecordingProgressiveSink decodeSink = new RecordingProgressiveSink();
        RecordingProgressiveSink skipSink = new RecordingProgressiveSink();
        PngDecoderOptions skipOptions = new PngDecoderOptions();
        skipOptions.DecodeAnimation = false;
        IPngProgressiveDecoder decoder = new PngProgressiveDecoder();

        PngImage decoded = decoder.DecodeProgressiveBytes(png, new PngDecoderOptions(), decodeSink);
        PngImage skipped = decoder.DecodeProgressiveBytes(png, skipOptions, skipSink);

        Assert.That(decodeSink.Frames, Has.Count.EqualTo(2));
        Assert.That(decodeSink.Frames[0], Is.SameAs(decoded.Animation.Frames[0]));
        Assert.That(decodeSink.Frames[1].Pixels.Data, Is.EqualTo(new byte[] { 21, 22, 23, 24 }));
        Assert.That(skipSink.Frames, Is.Empty);
        Assert.That(skipped.Info.IsAnimated, Is.True);
        Assert.That(skipped.Animation.Frames, Is.Empty);
    }

    [Test]
    public void WarningAndSecurityPolicies_MatchFullDecoderBehavior()
    {
        byte[] png = AddUnknownChunk(CreatePng(1, 1, new byte[] { 1, 2, 3, 4 }, PngInterlaceMethod.None));
        byte[] badCrc = CorruptChunkCrc(png, "vpAg");
        PngDecoderOptions tolerant = new PngDecoderOptions();
        tolerant.ErrorPolicy = PngErrorPolicy.TolerantMetadata;
        RecordingProgressiveSink tolerantSink = new RecordingProgressiveSink();
        PngDecoderOptions lowLimit = new PngDecoderOptions();
        lowLimit.Limits.MaxWidth = 0;
        lowLimit.Limits.MaxPixelCount = 0;
        lowLimit.Limits.MaxHeight = 0;
        lowLimit.Limits.MaxDecompressedBytes = 4;
        RecordingProgressiveSink limitedSink = new RecordingProgressiveSink();
        IPngProgressiveDecoder decoder = new PngProgressiveDecoder();

        PngImage image = decoder.DecodeProgressiveBytes(badCrc, tolerant, tolerantSink);

        Assert.That(tolerantSink.Warnings.Select(value => value.Code), Is.EqualTo(new string[] { "PNG_CHUNK_CRC_MISMATCH" }));
        Assert.That(tolerantSink.Warnings[0], Is.SameAs(image.Warnings[0]));
        Assert.Throws<InvalidDataException>(() => decoder.DecodeProgressiveBytes(badCrc, new PngDecoderOptions(), new RecordingProgressiveSink()));
        Assert.Throws<OverflowException>(() => decoder.DecodeProgressiveBytes(png, lowLimit, limitedSink));
        Assert.That(limitedSink.Rows, Is.Empty);
        Assert.That(limitedSink.Passes, Is.Empty);
        Assert.That(limitedSink.WantsPreviewsCallCount, Is.EqualTo(0));
    }

    [Test]
    public void ProgressiveDecoder_RejectsInvalidArguments()
    {
        byte[] png = CreatePng(1, 1, new byte[] { 1, 2, 3, 4 }, PngInterlaceMethod.None);
        IPngProgressiveDecoder decoder = new PngProgressiveDecoder();
        RecordingProgressiveSink sink = new RecordingProgressiveSink();
        PngDecoderOptions nullLimits = new PngDecoderOptions();
        nullLimits.Limits = null!;

        Assert.Throws<ArgumentNullException>(() => decoder.DecodeProgressiveBytes(null!, new PngDecoderOptions(), sink));
        Assert.Throws<ArgumentNullException>(() => decoder.DecodeProgressiveBytes(png, null!, sink));
        Assert.Throws<ArgumentNullException>(() => decoder.DecodeProgressiveBytes(png, nullLimits, sink));
        Assert.Throws<ArgumentNullException>(() => decoder.DecodeProgressiveBytes(png, new PngDecoderOptions(), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => decoder.DecodeProgressiveBytesAtOffset(png, -1, new PngDecoderOptions(), sink));
        Assert.Throws<ArgumentNullException>(() => decoder.DecodeProgressiveStream(null!, new PngDecoderOptions(), sink));
        Assert.Throws<InvalidDataException>(() => DecodeEmptySpan(decoder, sink));
    }

    static PngImage DecodeEmptySpan(IPngProgressiveDecoder decoder, IPngProgressiveSink sink)
    {
        return decoder.DecodeProgressiveSpan(ReadOnlySpan<byte>.Empty, new PngDecoderOptions(), sink);
    }

    static byte[] CreatePng(int width, int height, byte[] pixels, PngInterlaceMethod interlaceMethod)
    {
        PngImage image = Png.CreateImage(width, height, PngPixelFormat.Rgba8, pixels);
        PngEncoderOptions options = new PngEncoderOptions();
        options.UseAutomaticColorType = false;
        options.UseAutomaticBitDepth = false;
        options.ColorType = PngColorType.TruecolorAlpha;
        options.BitDepth = PngBitDepth.Eight;
        options.InterlaceMethod = interlaceMethod;
        return Png.EncodeToBytes(image, options);
    }

    static byte[] CreateRgbaPixels(int width, int height)
    {
        byte[] pixels = new byte[width * height * 4];
        int index = 0;
        while (index < pixels.Length)
        {
            pixels[index] = (byte)(index % 251);
            pixels[index + 1] = (byte)((index + 1) % 251);
            pixels[index + 2] = (byte)((index + 2) % 251);
            pixels[index + 3] = 255;
            index += 4;
        }

        return pixels;
    }

    static byte[] CreateAnimatedPng()
    {
        PngIhdr ihdr = new PngIhdr();
        ihdr.Width = 1;
        ihdr.Height = 1;
        ihdr.ColorType = PngColorType.TruecolorAlpha;
        ihdr.BitDepth = PngBitDepth.Eight;
        ihdr.CompressionMethod = PngCompressionMethod.Deflate;
        ihdr.FilterMethod = PngFilterMethod.Adaptive;
        ihdr.InterlaceMethod = PngInterlaceMethod.None;
        PngAnimation animation = new PngAnimation();
        animation.FrameCount = 2;
        animation.PlayCount = 3;
        animation.Frames = new PngFrame[]
        {
            CreateFrame(new byte[] { 11, 12, 13, 14 }, true),
            CreateFrame(new byte[] { 21, 22, 23, 24 }, false)
        };
        PngChunk[] frameChunks = PngApngFullCanvasEncoder.Encode(ihdr, animation);
        PngChunk[] chunks = new PngChunk[frameChunks.Length + 2];
        chunks[0] = PngIhdrWriter.WriteChunk(ihdr);
        Array.Copy(frameChunks, 0, chunks, 1, frameChunks.Length);
        chunks[chunks.Length - 1] = CreateChunk(PngConstants.ChunkIEND, new byte[0]);
        return PngChunkDatastreamWriter.WriteToBytes(chunks);
    }

    static PngFrame CreateFrame(byte[] data, bool usesDefaultImage)
    {
        PngPixelBuffer pixels = new PngPixelBuffer();
        pixels.Format = PngPixelFormat.Rgba8;
        pixels.Width = 1;
        pixels.Height = 1;
        pixels.Stride = 4;
        pixels.Data = data;
        PngFrame frame = new PngFrame();
        frame.Width = 1;
        frame.Height = 1;
        frame.DelayNumerator = 1;
        frame.DelayDenominator = 100;
        frame.BlendOperation = PngApngBlendOperation.Source;
        frame.DisposeOperation = PngApngDisposeOperation.None;
        frame.Pixels = pixels;
        frame.UsesDefaultImage = usesDefaultImage;
        return frame;
    }

    static byte[] AddUnknownChunk(byte[] png)
    {
        PngChunk[] source = PngChunkListReader.Read(png, PngConstants.SignatureLength).Chunks;
        PngChunk[] chunks = new PngChunk[source.Length + 1];
        chunks[0] = source[0];
        chunks[1] = CreateChunk("vpAg", new byte[] { 9 });
        Array.Copy(source, 1, chunks, 2, source.Length - 1);
        return PngChunkDatastreamWriter.WriteToBytes(chunks);
    }

    static byte[] CorruptChunkCrc(byte[] png, string chunkType)
    {
        byte[] result = (byte[])png.Clone();
        PngChunk[] chunks = PngChunkListReader.Read(result, PngConstants.SignatureLength).Chunks;
        PngChunk chunk = chunks.Single(value => value.Type == chunkType);
        int crcOffset = (int)chunk.Offset + 8 + chunk.Data.Length;
        result[crcOffset] ^= 1;
        return result;
    }

    static PngChunk CreateChunk(string type, byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = type;
        chunk.Data = data;
        return chunk;
    }

    static byte[] Join(params byte[][] arrays)
    {
        int totalLength = arrays.Sum(value => value.Length);
        byte[] result = new byte[totalLength];
        int offset = 0;
        int index = 0;
        while (index < arrays.Length)
        {
            Array.Copy(arrays[index], 0, result, offset, arrays[index].Length);
            offset += arrays[index].Length;
            index += 1;
        }

        return result;
    }

    class RecordingProgressiveSink : IPngProgressiveSink
    {
        public bool RequestPreviews;
        public int WantsPreviewsCallCount;
        public List<PngProgressiveRow> Rows = new List<PngProgressiveRow>();
        public List<PngAdam7PassUpdate> Passes = new List<PngAdam7PassUpdate>();
        public List<PngProgressivePreview> Previews = new List<PngProgressivePreview>();
        public List<PngFrame> Frames = new List<PngFrame>();
        public List<PngWarning> Warnings = new List<PngWarning>();

        public bool WantsPreviews()
        {
            WantsPreviewsCallCount += 1;
            return RequestPreviews;
        }

        public void ReceiveRow(PngProgressiveRow row)
        {
            Rows.Add(row);
        }

        public void ReceivePass(PngAdam7PassUpdate pass)
        {
            Passes.Add(pass);
        }

        public void ReceivePreview(PngProgressivePreview preview)
        {
            Previews.Add(preview);
        }

        public void ReceiveFrame(PngFrame frame)
        {
            Frames.Add(frame);
        }

        public void ReceiveWarning(PngWarning warning)
        {
            Warnings.Add(warning);
        }
    }
}
