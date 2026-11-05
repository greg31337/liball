using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PngDecoderOptionsBehaviorTests
{
    [Test]
    public void PreferredPixelFormat_ConvertsByteAndStreamResults()
    {
        byte[] png = CreateRgbaPng(1, 1, new byte[] { 10, 20, 30, 40 });
        PngDecoderOptions rgbOptions = new PngDecoderOptions();
        rgbOptions.PreferredPixelFormat = PngPixelFormat.Rgb8;
        PngDecoderOptions rgba16Options = new PngDecoderOptions();
        rgba16Options.PreferredPixelFormat = PngPixelFormat.Rgba16;
        byte[] rgbPng = CreateRgbPng(new byte[] { 50, 60, 70 });

        PngImage rgb = new PngCodec().DecodeBytes(png, rgbOptions);
        PngImage rgba16 = new PngCodec().DecodeStream(new MemoryStream(png), rgba16Options);
        PngImage normalizedDefault = new PngCodec().DecodeBytes(rgbPng, new PngDecoderOptions());

        AssertBuffer(rgb, PngPixelFormat.Rgb8, 3, new byte[] { 10, 20, 30 });
        AssertBuffer(rgba16, PngPixelFormat.Rgba16, 8, new byte[] { 10, 10, 20, 20, 30, 30, 40, 40 });
        AssertBuffer(normalizedDefault, PngPixelFormat.Rgba8, 4, new byte[] { 50, 60, 70, 255 });
    }

    [Test]
    public void PreferredPixelFormat_ProducesEveryNormalizedNonIndexedLayout()
    {
        byte[] png = CreateRgbaPng(1, 1, new byte[] { 255, 255, 255, 128 });
        PngPixelFormat[] formats = new PngPixelFormat[]
        {
            PngPixelFormat.Grayscale1,
            PngPixelFormat.Grayscale2,
            PngPixelFormat.Grayscale4,
            PngPixelFormat.Grayscale8,
            PngPixelFormat.Grayscale16,
            PngPixelFormat.Rgb8,
            PngPixelFormat.Rgb16,
            PngPixelFormat.GrayscaleAlpha8,
            PngPixelFormat.GrayscaleAlpha16,
            PngPixelFormat.Rgba8,
            PngPixelFormat.Rgba16
        };
        byte[][] expected = new byte[][]
        {
            new byte[] { 0x80 },
            new byte[] { 0xC0 },
            new byte[] { 0xF0 },
            new byte[] { 255 },
            new byte[] { 255, 255 },
            new byte[] { 255, 255, 255 },
            new byte[] { 255, 255, 255, 255, 255, 255 },
            new byte[] { 255, 128 },
            new byte[] { 255, 255, 128, 128 },
            new byte[] { 255, 255, 255, 128 },
            new byte[] { 255, 255, 255, 255, 255, 255, 128, 128 }
        };

        int index = 0;
        while (index < formats.Length)
        {
            PngDecoderOptions options = new PngDecoderOptions();
            options.PreferredPixelFormat = formats[index];
            PngImage image = Png.DecodeBytes(png, options);
            AssertBuffer(image, formats[index], expected[index].Length, expected[index]);
            index += 1;
        }
    }

    [Test]
    public void PreferredPixelFormat_RepacksIndexedDepthAndExpandsPaletteTransparency()
    {
        byte[] png = CreateIndexedPng();
        PngPixelFormat[] formats = new PngPixelFormat[]
        {
            PngPixelFormat.Indexed1,
            PngPixelFormat.Indexed2,
            PngPixelFormat.Indexed4,
            PngPixelFormat.Indexed8
        };
        byte[][] expected = new byte[][]
        {
            new byte[] { 0x40 },
            new byte[] { 0x10 },
            new byte[] { 0x01 },
            new byte[] { 0, 1 }
        };
        int index = 0;
        while (index < formats.Length)
        {
            PngDecoderOptions options = new PngDecoderOptions();
            options.PreferredPixelFormat = formats[index];
            AssertBuffer(Png.DecodeBytes(png, options), formats[index], expected[index].Length, expected[index]);
            index += 1;
        }

        PngChunk[] chunks = ReadChunks(png);
        int idatIndex = Array.FindIndex(chunks, value => value.Type == PngConstants.ChunkIDAT);
        byte[] transparentPng = AddChunk(png, CreateChunk(PngConstants.ChunkTransparency, new byte[] { 255, 0 }), idatIndex);
        AssertBuffer(Png.DecodeBytes(transparentPng, new PngDecoderOptions()), PngPixelFormat.Rgba8, 8, new byte[]
        {
            10, 20, 30, 255,
            40, 50, 60, 0
        });
    }

    [Test]
    public void CrcAndErrorPolicies_ControlFatalAndWarningResults()
    {
        byte[] png = AddChunk(CreateRgbaPng(1, 1, new byte[] { 1, 2, 3, 4 }), CreateChunk("vpAg", new byte[] { 9 }), 1);
        byte[] badAncillaryCrc = CorruptChunkCrc(png, "vpAg");
        byte[] badCriticalCrc = CorruptChunkCrc(png, PngConstants.ChunkIDAT);
        PngDecoderOptions skipCrc = new PngDecoderOptions();
        skipCrc.ValidateCrc = false;
        PngDecoderOptions tolerant = new PngDecoderOptions();
        tolerant.ErrorPolicy = PngErrorPolicy.TolerantMetadata;
        PngDecoderOptions bestEffort = new PngDecoderOptions();
        bestEffort.ErrorPolicy = PngErrorPolicy.BestEffortInspection;

        Assert.Throws<InvalidDataException>(() => Png.DecodeBytes(badAncillaryCrc, new PngDecoderOptions()));
        Assert.That(Png.DecodeBytes(badAncillaryCrc, skipCrc).Warnings, Is.Empty);
        Assert.That(Png.DecodeBytes(badAncillaryCrc, tolerant).Warnings.Select(value => value.Code), Does.Contain("PNG_CHUNK_CRC_MISMATCH"));
        Assert.That(Png.DecodeBytes(badCriticalCrc, bestEffort).Warnings.Select(value => value.Code), Does.Contain("PNG_CHUNK_CRC_MISMATCH"));
        Assert.Throws<InvalidDataException>(() => Png.DecodeBytes(badCriticalCrc, tolerant));
    }

    [Test]
    public void PreserveUnknownChunks_ControlsUnknownChunkCollection()
    {
        byte[] png = AddChunk(CreateRgbaPng(1, 1, new byte[] { 1, 2, 3, 4 }), CreateChunk("vpAg", new byte[] { 7, 8 }), 1);
        PngDecoderOptions preserve = new PngDecoderOptions();
        PngDecoderOptions discard = new PngDecoderOptions();
        discard.PreserveUnknownChunks = false;

        PngImage preserved = Png.DecodeBytes(png, preserve);
        PngImage discarded = Png.DecodeBytes(png, discard);

        Assert.That(preserved.Metadata.UnknownChunks.Select(value => value.Type), Is.EqualTo(new string[] { "vpAg" }));
        Assert.That(discarded.Metadata.UnknownChunks, Is.Empty);
        Assert.That(discarded.Metadata.Chunks.Select(value => value.Type), Does.Contain("vpAg"));
    }

    [Test]
    public void DecodeAnimation_ControlsFrameInflationWithoutHidingAnimationInfo()
    {
        byte[] png = CreateAnimatedPng();
        PngDecoderOptions decode = new PngDecoderOptions();
        PngDecoderOptions skip = new PngDecoderOptions();
        skip.DecodeAnimation = false;

        PngImage decoded = Png.DecodeBytes(png, decode);
        PngImage skipped = Png.DecodeBytes(png, skip);

        Assert.That(decoded.Info.IsAnimated, Is.True);
        Assert.That(decoded.Animation.FrameCount, Is.EqualTo(2));
        Assert.That(decoded.Animation.Frames, Has.Length.EqualTo(2));
        Assert.That(decoded.Animation.Frames[0].UsesDefaultImage, Is.True);
        Assert.That(decoded.Animation.Frames[0].Pixels.Data, Is.EqualTo(new byte[] { 11, 12, 13, 14 }));
        Assert.That(decoded.Animation.Frames[1].UsesDefaultImage, Is.False);
        Assert.That(decoded.Animation.Frames[1].Pixels.Data, Is.EqualTo(new byte[] { 21, 22, 23, 24 }));
        Assert.That(skipped.Info.IsAnimated, Is.True);
        Assert.That(skipped.Info.FrameCount, Is.EqualTo(2));
        Assert.That(skipped.Animation.Frames, Is.Empty);
    }

    [Test]
    public void IgnoreTrailingBytes_ControlsDatastreamFraming()
    {
        byte[] png = Join(CreateRgbaPng(1, 1, new byte[] { 1, 2, 3, 4 }), new byte[] { 99, 88 });
        PngDecoderOptions ignore = new PngDecoderOptions();
        ignore.IgnoreTrailingBytes = true;

        Assert.Throws<InvalidDataException>(() => Png.DecodeBytes(png, new PngDecoderOptions()));
        Assert.That(Png.DecodeBytes(png, ignore).Pixels.Data, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void SecurityLimits_AllBoundDecoderWork()
    {
        byte[] png = CreateRgbaPng(2, 2, new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8,
            9, 10, 11, 12, 13, 14, 15, 16
        });
        byte[] metadataPng = AddChunk(png, CreateChunk("vpAg", new byte[] { 1, 2 }), 1);
        byte[] oneTextPng = AddChunk(png, CreateCompressedTextChunk("Comment", "0123456789"), 1);
        byte[] twoTextPng = AddChunk(oneTextPng, CreateChunk(PngConstants.ChunkText, new byte[] { 75, 0, 86 }), 2);
        byte[] animationPng = CreateAnimatedPng();
        int idatLength = FindChunk(ReadChunks(png), PngConstants.ChunkIDAT).Data.Length;

        AssertLimit(png, value => value.MaxWidth = 1);
        AssertLimit(png, value => value.MaxHeight = 1);
        AssertLimit(png, value => value.MaxPixelCount = 3);
        AssertLimit(png, value => value.MaxChunkCount = 2);
        AssertLimit(metadataPng, value => value.MaxMetadataBytes = 1);
        AssertLimit(oneTextPng, value => value.MaxTextBytes = 9);
        AssertLimit(twoTextPng, value => value.MaxTextEntries = 1);
        AssertLimit(png, value => value.MaxIdatBytes = idatLength - 1);
        AssertLimit(png, value => value.MaxDecompressedBytes = 17);
        AssertLimit(animationPng, value => value.MaxAnimationFrames = 1);
    }

    [Test]
    public void Decoder_RejectsUndefinedOptionValues()
    {
        byte[] png = CreateRgbaPng(1, 1, new byte[] { 1, 2, 3, 4 });
        PngDecoderOptions badPolicy = new PngDecoderOptions();
        badPolicy.ErrorPolicy = (PngErrorPolicy)99;
        PngDecoderOptions badFormat = new PngDecoderOptions();
        badFormat.PreferredPixelFormat = (PngPixelFormat)99;

        Assert.Throws<ArgumentOutOfRangeException>(() => Png.DecodeBytes(png, badPolicy));
        Assert.Throws<ArgumentOutOfRangeException>(() => Png.DecodeBytes(png, badFormat));
    }

    static void AssertLimit(byte[] png, Action<PngSecurityLimits> configure)
    {
        PngDecoderOptions options = new PngDecoderOptions();
        configure(options.Limits);
        Exception? exception = Assert.Catch(() => Png.DecodeBytes(png, options));
        Assert.That(exception, Is.TypeOf<OverflowException>().Or.TypeOf<InvalidDataException>());
    }

    static byte[] CreateRgbaPng(int width, int height, byte[] pixels)
    {
        PngImage image = Png.CreateImage(width, height, PngPixelFormat.Rgba8, pixels);
        PngEncoderOptions options = new PngEncoderOptions();
        options.UseAutomaticColorType = false;
        options.UseAutomaticBitDepth = false;
        options.ColorType = PngColorType.TruecolorAlpha;
        options.BitDepth = PngBitDepth.Eight;
        return Png.EncodeToBytes(image, options);
    }

    static byte[] CreateRgbPng(byte[] pixels)
    {
        PngImage image = Png.CreateImage(1, 1, PngPixelFormat.Rgb8, pixels);
        PngEncoderOptions options = new PngEncoderOptions();
        options.UseAutomaticColorType = false;
        options.UseAutomaticBitDepth = false;
        options.ColorType = PngColorType.Truecolor;
        options.BitDepth = PngBitDepth.Eight;
        return Png.EncodeToBytes(image, options);
    }

    static byte[] CreateIndexedPng()
    {
        PngImage image = Png.CreateImage(2, 1, PngPixelFormat.Indexed8, new byte[] { 0, 1 });
        PngPaletteEntry first = new PngPaletteEntry();
        first.Red = 10;
        first.Green = 20;
        first.Blue = 30;
        PngPaletteEntry second = new PngPaletteEntry();
        second.Red = 40;
        second.Green = 50;
        second.Blue = 60;
        image.Metadata.Palette.Entries = new PngPaletteEntry[] { first, second };
        PngEncoderOptions options = new PngEncoderOptions();
        options.UseAutomaticColorType = false;
        options.UseAutomaticBitDepth = false;
        options.ColorType = PngColorType.IndexedColor;
        options.BitDepth = PngBitDepth.Eight;
        return Png.EncodeToBytes(image, options);
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
        PngPixelBuffer pixels = new PngPixelBuffer();
        pixels.Format = PngPixelFormat.Rgba8;
        pixels.Width = 1;
        pixels.Height = 1;
        pixels.Stride = 4;
        pixels.Data = new byte[] { 11, 12, 13, 14 };
        PngFrame frame = new PngFrame();
        frame.Width = 1;
        frame.Height = 1;
        frame.DelayNumerator = 1;
        frame.DelayDenominator = 0;
        frame.BlendOperation = PngApngBlendOperation.Source;
        frame.DisposeOperation = PngApngDisposeOperation.None;
        frame.Pixels = pixels;
        frame.UsesDefaultImage = true;
        PngPixelBuffer secondPixels = new PngPixelBuffer();
        secondPixels.Format = PngPixelFormat.Rgba8;
        secondPixels.Width = 1;
        secondPixels.Height = 1;
        secondPixels.Stride = 4;
        secondPixels.Data = new byte[] { 21, 22, 23, 24 };
        PngFrame secondFrame = new PngFrame();
        secondFrame.Width = 1;
        secondFrame.Height = 1;
        secondFrame.DelayNumerator = 2;
        secondFrame.DelayDenominator = 100;
        secondFrame.BlendOperation = PngApngBlendOperation.Over;
        secondFrame.DisposeOperation = PngApngDisposeOperation.Background;
        secondFrame.Pixels = secondPixels;
        PngAnimation animation = new PngAnimation();
        animation.FrameCount = 2;
        animation.PlayCount = 2;
        animation.Frames = new PngFrame[] { frame, secondFrame };
        PngChunk[] frameChunks = PngApngFullCanvasEncoder.Encode(ihdr, animation);
        PngChunk[] chunks = new PngChunk[frameChunks.Length + 2];
        chunks[0] = PngIhdrWriter.WriteChunk(ihdr);
        Array.Copy(frameChunks, 0, chunks, 1, frameChunks.Length);
        chunks[chunks.Length - 1] = CreateChunk(PngConstants.ChunkIEND, new byte[0]);
        return PngChunkDatastreamWriter.WriteToBytes(chunks);
    }

    static PngChunk CreateCompressedTextChunk(string keyword, string value)
    {
        PngCompressedText text = new PngCompressedText();
        text.Keyword = keyword;
        text.Text = value;
        text.CompressionMethod = PngCompressedTextParser.CompressionMethodDeflate;
        return PngCompressedTextWriter.WriteChunk(text);
    }

    static byte[] AddChunk(byte[] png, PngChunk chunk, int index)
    {
        PngChunk[] source = ReadChunks(png);
        PngChunk[] result = new PngChunk[source.Length + 1];
        Array.Copy(source, 0, result, 0, index);
        result[index] = chunk;
        Array.Copy(source, index, result, index + 1, source.Length - index);
        return PngChunkDatastreamWriter.WriteToBytes(result);
    }

    static byte[] CorruptChunkCrc(byte[] png, string chunkType)
    {
        byte[] result = (byte[])png.Clone();
        PngChunk chunk = FindChunk(ReadChunks(result), chunkType);
        int crcOffset = (int)chunk.Offset + 8 + chunk.Data.Length;
        result[crcOffset] ^= 1;
        return result;
    }

    static PngChunk[] ReadChunks(byte[] png)
    {
        return PngChunkListReader.Read(png, PngConstants.SignatureLength).Chunks;
    }

    static PngChunk FindChunk(PngChunk[] chunks, string type)
    {
        return chunks.Single(value => value.Type == type);
    }

    static PngChunk CreateChunk(string type, byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = type;
        chunk.Data = data;
        return chunk;
    }

    static byte[] Join(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        Array.Copy(first, 0, result, 0, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        return result;
    }

    static void AssertBuffer(PngImage image, PngPixelFormat format, int stride, byte[] data)
    {
        Assert.That(image.Info.PixelFormat, Is.EqualTo(format));
        Assert.That(image.Pixels.Format, Is.EqualTo(format));
        Assert.That(image.Pixels.Stride, Is.EqualTo(stride));
        Assert.That(image.Pixels.Data, Is.EqualTo(data));
    }
}
