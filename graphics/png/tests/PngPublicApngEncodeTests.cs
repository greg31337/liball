using System.IO;

namespace png.Tests;

[TestFixture]
public class PngPublicApngEncodeTests
{
    [Test]
    public void EncodeToBytes_WritesSelectedDefaultFrameRectanglesMetadataAndSplitData()
    {
        PngImage image = CreateRgbaImage(2, 1, new byte[] { 90, 91, 92, 255, 93, 94, 95, 255 });
        image.Animation.FrameCount = 2;
        image.Animation.PlayCount = 5;
        image.Animation.Frames = new PngFrame[]
        {
            CreateFrame(2, 1, 0, new byte[] { 10, 20, 30, 255, 40, 50, 60, 255 }, 3, 47, PngApngDisposeOperation.Previous, PngApngBlendOperation.Source, true),
            CreateFrame(1, 1, 1, new byte[] { 70, 80, 90, 128 }, 7, 113, PngApngDisposeOperation.Background, PngApngBlendOperation.Over, false)
        };
        image.Metadata.Chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkGamma, new byte[] { 0, 0, 177, 143 }),
            CreateChunk(PngConstants.ChunkText, new byte[] { 75, 0, 86 })
        };
        PngEncoderOptions options = CreateAnimationOptions();
        options.MaxIdatChunkBytes = 1;
        options.MaxFdatChunkBytes = 1;

        byte[] encoded = new PngCodec().EncodeToBytes(image, options);
        PngChunk[] chunks = Png.ReadChunks(encoded).Chunks;
        PngImage decoded = new PngCodec().DecodeBytes(encoded, new PngDecoderOptions());

        int animationControlIndex = FindChunk(chunks, PngConstants.ChunkAnimationControl, 0);
        int firstFrameControlIndex = FindChunk(chunks, PngConstants.ChunkFrameControl, 0);
        int firstIdatIndex = FindChunk(chunks, PngConstants.ChunkIDAT, 0);
        int secondFrameControlIndex = FindChunk(chunks, PngConstants.ChunkFrameControl, 1);
        Assert.That(FindChunk(chunks, PngConstants.ChunkGamma, 0), Is.LessThan(animationControlIndex));
        Assert.That(animationControlIndex, Is.LessThan(firstFrameControlIndex));
        Assert.That(firstFrameControlIndex, Is.LessThan(firstIdatIndex));
        Assert.That(FindLastChunk(chunks, PngConstants.ChunkIDAT), Is.LessThan(secondFrameControlIndex));
        Assert.That(FindChunk(chunks, PngConstants.ChunkText, 0), Is.GreaterThan(FindLastChunk(chunks, PngConstants.ChunkFrameData)));
        AssertSplitPayloads(chunks, PngConstants.ChunkIDAT, 1);
        AssertSplitPayloads(chunks, PngConstants.ChunkFrameData, 5);
        AssertCanonicalSequence(chunks);

        Assert.That(decoded.Pixels.Data, Is.EqualTo(new byte[] { 10, 20, 30, 255, 40, 50, 60, 255 }));
        Assert.That(decoded.Animation.FrameCount, Is.EqualTo(2));
        Assert.That(decoded.Animation.PlayCount, Is.EqualTo(5));
        AssertFrame(decoded.Animation.Frames[0], 2, 0, 3, 47, PngApngDisposeOperation.Previous, PngApngBlendOperation.Source, true, new byte[] { 10, 20, 30, 255, 40, 50, 60, 255 });
        AssertFrame(decoded.Animation.Frames[1], 1, 1, 7, 113, PngApngDisposeOperation.Background, PngApngBlendOperation.Over, false, new byte[] { 70, 80, 90, 128 });
        Assert.That(HasChunk(decoded.Metadata.Chunks, PngConstants.ChunkGamma), Is.True);
        Assert.That(HasChunk(decoded.Metadata.Chunks, PngConstants.ChunkText), Is.True);
    }

    [Test]
    public void EncodeToBytes_KeepsStaticImageOutsideAnimationWhenNoFrameSelectsIt()
    {
        PngImage image = CreateRgbaImage(2, 1, new byte[] { 1, 2, 3, 255, 4, 5, 6, 255 });
        image.Animation.FrameCount = 2;
        image.Animation.PlayCount = 0;
        image.Animation.Frames = new PngFrame[]
        {
            CreateFrame(1, 1, 1, new byte[] { 20, 30, 40, 255 }, 2, 25, PngApngDisposeOperation.None, PngApngBlendOperation.Source, false),
            CreateFrame(2, 1, 0, new byte[] { 50, 60, 70, 255, 80, 90, 100, 255 }, 4, 50, PngApngDisposeOperation.Previous, PngApngBlendOperation.Over, false)
        };
        PngEncoderOptions options = CreateAnimationOptions();
        options.MaxIdatChunkBytes = 2;
        options.MaxFdatChunkBytes = 2;

        byte[] encoded = new PngCodec().EncodeToBytes(image, options);
        PngChunk[] chunks = Png.ReadChunks(encoded).Chunks;
        PngImage decoded = new PngCodec().DecodeBytes(encoded, new PngDecoderOptions());

        Assert.That(FindLastChunk(chunks, PngConstants.ChunkIDAT), Is.LessThan(FindChunk(chunks, PngConstants.ChunkFrameControl, 0)));
        AssertCanonicalSequence(chunks);
        Assert.That(decoded.Pixels.Data, Is.EqualTo(new byte[] { 1, 2, 3, 255, 4, 5, 6, 255 }));
        Assert.That(decoded.Animation.FrameCount, Is.EqualTo(2));
        Assert.That(decoded.Animation.Frames.All(value => !value.UsesDefaultImage), Is.True);
        AssertFrame(decoded.Animation.Frames[0], 1, 1, 2, 25, PngApngDisposeOperation.None, PngApngBlendOperation.Source, false, new byte[] { 20, 30, 40, 255 });
        AssertFrame(decoded.Animation.Frames[1], 2, 0, 4, 50, PngApngDisposeOperation.Previous, PngApngBlendOperation.Over, false, new byte[] { 50, 60, 70, 255, 80, 90, 100, 255 });
    }

    [Test]
    public void EncodeToBytes_PreservesPaletteForIndexedAnimation()
    {
        PngImage image = Png.CreateImage(2, 1, PngPixelFormat.Indexed8, new byte[] { 0, 1 });
        PngPaletteEntry first = CreatePaletteEntry(10, 20, 30);
        PngPaletteEntry second = CreatePaletteEntry(40, 50, 60);
        image.Metadata.Palette.Entries = new PngPaletteEntry[] { first, second };
        image.Animation.FrameCount = 2;
        image.Animation.Frames = new PngFrame[]
        {
            CreateIndexedFrame(2, 0, new byte[] { 0, 1 }, true),
            CreateIndexedFrame(1, 1, new byte[] { 0 }, false)
        };

        byte[] encoded = new PngCodec().EncodeToBytes(image, CreateAnimationOptions());
        PngChunk[] chunks = Png.ReadChunks(encoded).Chunks;
        PngImage decoded = new PngCodec().DecodeBytes(encoded, new PngDecoderOptions());

        Assert.That(FindChunk(chunks, PngConstants.ChunkPLTE, 0), Is.LessThan(FindChunk(chunks, PngConstants.ChunkAnimationControl, 0)));
        Assert.That(decoded.Metadata.Palette.Entries, Has.Length.EqualTo(2));
        Assert.That(decoded.Pixels.Data, Is.EqualTo(new byte[] { 10, 20, 30, 255, 40, 50, 60, 255 }));
        Assert.That(decoded.Animation.Frames[1].Pixels.Data, Is.EqualTo(new byte[] { 10, 20, 30, 255 }));
    }

    [Test]
    public void EncodeToBytes_EnforcesAnimationLimitsAndDefaultSelectionRules()
    {
        PngImage image = CreateRgbaImage(1, 1, new byte[] { 1, 2, 3, 255 });
        image.Animation.FrameCount = 2;
        image.Animation.Frames = new PngFrame[]
        {
            CreateFrame(1, 1, 0, new byte[] { 4, 5, 6, 255 }, 1, 10, PngApngDisposeOperation.None, PngApngBlendOperation.Source, false),
            CreateFrame(1, 1, 0, new byte[] { 7, 8, 9, 255 }, 2, 10, PngApngDisposeOperation.None, PngApngBlendOperation.Source, false)
        };
        PngEncoderOptions lowFrameLimit = CreateAnimationOptions();
        lowFrameLimit.Limits.MaxAnimationFrames = 1;
        PngEncoderOptions negativeFrameSplit = CreateAnimationOptions();
        negativeFrameSplit.MaxFdatChunkBytes = -1;

        Assert.Throws<OverflowException>(() => new PngCodec().EncodeToBytes(image, lowFrameLimit));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PngCodec().EncodeToBytes(image, negativeFrameSplit));

        image.Animation.Frames[1].UsesDefaultImage = true;
        Assert.Throws<ArgumentOutOfRangeException>(() => new PngCodec().EncodeToBytes(image, CreateAnimationOptions()));
    }

    static PngEncoderOptions CreateAnimationOptions()
    {
        PngEncoderOptions options = new PngEncoderOptions();
        options.EncodeAnimation = true;
        options.UseAutomaticColorType = true;
        options.UseAutomaticBitDepth = true;
        options.UseFixedFilter = true;
        options.FixedFilterType = PngFilterType.None;
        return options;
    }

    static PngImage CreateRgbaImage(int width, int height, byte[] data)
    {
        return Png.CreateImage(width, height, PngPixelFormat.Rgba8, data);
    }

    static PngFrame CreateFrame(int width, int height, int xOffset, byte[] data, int delayNumerator, int delayDenominator, PngApngDisposeOperation disposeOperation, PngApngBlendOperation blendOperation, bool usesDefaultImage)
    {
        PngPixelBuffer pixels = new PngPixelBuffer();
        pixels.Format = PngPixelFormat.Rgba8;
        pixels.Width = width;
        pixels.Height = height;
        pixels.Stride = width * 4;
        pixels.Data = data;
        PngFrame frame = new PngFrame();
        frame.Width = width;
        frame.Height = height;
        frame.XOffset = xOffset;
        frame.DelayNumerator = delayNumerator;
        frame.DelayDenominator = delayDenominator;
        frame.DisposeOperation = disposeOperation;
        frame.BlendOperation = blendOperation;
        frame.Pixels = pixels;
        frame.UsesDefaultImage = usesDefaultImage;
        return frame;
    }

    static PngFrame CreateIndexedFrame(int width, int xOffset, byte[] data, bool usesDefaultImage)
    {
        PngPixelBuffer pixels = new PngPixelBuffer();
        pixels.Format = PngPixelFormat.Indexed8;
        pixels.Width = width;
        pixels.Height = 1;
        pixels.Stride = width;
        pixels.Data = data;
        PngFrame frame = new PngFrame();
        frame.Width = width;
        frame.Height = 1;
        frame.XOffset = xOffset;
        frame.DelayDenominator = 100;
        frame.Pixels = pixels;
        frame.UsesDefaultImage = usesDefaultImage;
        return frame;
    }

    static PngPaletteEntry CreatePaletteEntry(int red, int green, int blue)
    {
        PngPaletteEntry entry = new PngPaletteEntry();
        entry.Red = (byte)red;
        entry.Green = (byte)green;
        entry.Blue = (byte)blue;
        return entry;
    }

    static PngChunk CreateChunk(string type, byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = type;
        chunk.Data = data;
        return chunk;
    }

    static int FindChunk(PngChunk[] chunks, string type, int occurrence)
    {
        int seen = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == type)
            {
                if (seen == occurrence)
                {
                    return index;
                }

                seen += 1;
            }

            index += 1;
        }

        return -1;
    }

    static int FindLastChunk(PngChunk[] chunks, string type)
    {
        int result = -1;
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == type)
            {
                result = index;
            }

            index += 1;
        }

        return result;
    }

    static bool HasChunk(PngChunk[] chunks, string type)
    {
        return FindChunk(chunks, type, 0) >= 0;
    }

    static void AssertSplitPayloads(PngChunk[] chunks, string type, int maximumLength)
    {
        int count = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == type)
            {
                Assert.That(chunks[index].Data, Has.Length.LessThanOrEqualTo(maximumLength));
                count += 1;
            }

            index += 1;
        }

        Assert.That(count, Is.GreaterThan(1));
    }

    static void AssertCanonicalSequence(PngChunk[] chunks)
    {
        uint expected = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == PngConstants.ChunkFrameControl || chunks[index].Type == PngConstants.ChunkFrameData)
            {
                Assert.That(PngBinaryReader.ReadUInt32(chunks[index].Data, 0), Is.EqualTo(expected));
                expected += 1;
            }

            index += 1;
        }

        Assert.That(PngApngSequenceValidator.Validate(chunks).IsValid, Is.True);
    }

    static void AssertFrame(PngFrame frame, int width, int xOffset, int delayNumerator, int delayDenominator, PngApngDisposeOperation disposeOperation, PngApngBlendOperation blendOperation, bool usesDefaultImage, byte[] data)
    {
        Assert.That(frame.Width, Is.EqualTo(width));
        Assert.That(frame.Height, Is.EqualTo(1));
        Assert.That(frame.XOffset, Is.EqualTo(xOffset));
        Assert.That(frame.DelayNumerator, Is.EqualTo(delayNumerator));
        Assert.That(frame.DelayDenominator, Is.EqualTo(delayDenominator));
        Assert.That(frame.DisposeOperation, Is.EqualTo(disposeOperation));
        Assert.That(frame.BlendOperation, Is.EqualTo(blendOperation));
        Assert.That(frame.UsesDefaultImage, Is.EqualTo(usesDefaultImage));
        Assert.That(frame.Pixels.Data, Is.EqualTo(data));
    }
}
