using System.IO;
using System.Text;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PublicApngEndToEndTests
{
    [Test]
    public void PublicCodec_RoundTripsIncludedDefaultAnimationWithEveryCompositionOperationAndDelayEdge()
    {
        PngImage animation = CreateIncludedDefaultAnimation();
        PngCodec codec = new PngCodec();
        PngEncoderOptions encodeOptions = CreateAnimationOptions();

        byte[] encoded = codec.EncodeToBytes(animation, encodeOptions);
        PngValidationResult validation = new PngValidationService().ValidateBytes(encoded);
        PngIdentifyResult identified = new PngIdentifierService().IdentifyBytes(encoded);
        PngImage decoded = codec.DecodeBytes(encoded, CreateDecodeOptions());
        byte[] reencoded = codec.EncodeToBytes(decoded, CreateAnimationOptions());
        PngImage decodedAgain = codec.DecodeBytes(reencoded, CreateDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(validation.IsValid, Is.True);
            Assert.That(identified.IsValid, Is.True);
            Assert.That(identified.Info.IsAnimated, Is.True);
            Assert.That(identified.Info.FrameCount, Is.EqualTo(4));
            Assert.That(identified.Info.PlayCount, Is.EqualTo(3));
            Assert.That(HasChunk(encoded, PngConstants.ChunkAnimationControl), Is.True);
            Assert.That(HasChunk(encoded, PngConstants.ChunkFrameControl), Is.True);
            Assert.That(HasChunk(encoded, PngConstants.ChunkFrameData), Is.True);
        });

        AssertIncludedDefaultAnimation(decoded);
        AssertIncludedDefaultAnimation(decodedAgain);
        Assert.That(new PngValidationService().ValidateBytes(reencoded).IsValid, Is.True);
    }

    [Test]
    public void PublicCodec_RoundTripsAnimationWithSeparateStaticDefaultImage()
    {
        PngImage animation = CreateSeparateDefaultAnimation();
        PngCodec codec = new PngCodec();

        byte[] encoded = codec.EncodeToBytes(animation, CreateAnimationOptions());
        PngChunk[] chunks = Png.ReadChunks(encoded).Chunks;
        PngImage decoded = codec.DecodeBytes(encoded, CreateDecodeOptions());
        byte[] reencoded = codec.EncodeToBytes(decoded, CreateAnimationOptions());
        PngImage decodedAgain = codec.DecodeBytes(reencoded, CreateDecodeOptions());

        Assert.That(FindLastChunk(chunks, PngConstants.ChunkIDAT), Is.LessThan(FindChunk(chunks, PngConstants.ChunkFrameControl)));
        Assert.That(decoded.Pixels.Data, Is.EqualTo(animation.Pixels.Data));
        AssertSeparateDefaultAnimation(decoded);
        Assert.That(decodedAgain.Pixels.Data, Is.EqualTo(animation.Pixels.Data));
        AssertSeparateDefaultAnimation(decodedAgain);
        Assert.That(new PngValidationService().ValidateBytes(reencoded).IsValid, Is.True);
    }

    [Test]
    public void PublicEncoder_EmitsStaticFallbackWhenAnimationEncodingIsDisabled()
    {
        PngImage animation = CreateSeparateDefaultAnimation();
        PngEncoderOptions options = CreateAnimationOptions();
        options.EncodeAnimation = false;

        byte[] encoded = new PngCodec().EncodeToBytes(animation, options);
        PngIdentifyResult identified = new PngIdentifierService().IdentifyBytes(encoded);
        PngImage decoded = new PngCodec().DecodeBytes(encoded, CreateDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(identified.Info.IsAnimated, Is.False);
            Assert.That(HasChunk(encoded, PngConstants.ChunkAnimationControl), Is.False);
            Assert.That(HasChunk(encoded, PngConstants.ChunkFrameControl), Is.False);
            Assert.That(HasChunk(encoded, PngConstants.ChunkFrameData), Is.False);
            Assert.That(decoded.Pixels.Data, Is.EqualTo(animation.Pixels.Data));
            Assert.That(decoded.Animation.FrameCount, Is.EqualTo(0));
            Assert.That(decoded.Animation.Frames, Is.Empty);
            Assert.That(decoded.Animation.ComposedFrames, Is.Empty);
        });
    }

    [Test]
    public void PublicDecoder_ReturnsStaticFallbackWhenAnimationDecodingIsDisabled()
    {
        PngImage animation = CreateIncludedDefaultAnimation();
        byte[] encoded = new PngCodec().EncodeToBytes(animation, CreateAnimationOptions());
        PngDecoderOptions options = CreateDecodeOptions();
        options.DecodeAnimation = false;

        PngImage decoded = new PngCodec().DecodeBytes(encoded, options);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Info.IsAnimated, Is.True);
            Assert.That(decoded.Info.FrameCount, Is.EqualTo(4));
            Assert.That(decoded.Info.PlayCount, Is.EqualTo(3));
            Assert.That(decoded.Pixels.Data, Is.EqualTo(animation.Animation.Frames[0].Pixels.Data));
            Assert.That(decoded.Animation.FrameCount, Is.EqualTo(0));
            Assert.That(decoded.Animation.Frames, Is.Empty);
            Assert.That(decoded.Animation.ComposedFrames, Is.Empty);
        });
    }

    [TestCase("fcTL")]
    [TestCase("fdAT")]
    public void PublicDecoder_RejectsBrokenApngSequenceNumbers(string chunkType)
    {
        byte[] encoded = new PngCodec().EncodeToBytes(CreateSeparateDefaultAnimation(), CreateAnimationOptions());
        byte[] corrupted = ReplaceFirstSequenceNumber(encoded, chunkType, 17);
        PngDecoderOptions options = CreateDecodeOptions();
        options.ValidateCrc = false;

        Assert.Throws<InvalidDataException>(() => new PngCodec().DecodeBytes(corrupted, options));
    }

    [Test]
    public void PublicCodec_EnforcesAnimationFrameCountFrameBytesAndAggregateLimits()
    {
        PngImage animation = CreateIncludedDefaultAnimation();
        byte[] encoded = new PngCodec().EncodeToBytes(animation, CreateAnimationOptions());

        PngEncoderOptions encodeFrameCountLimit = CreateAnimationOptions();
        encodeFrameCountLimit.Limits.MaxAnimationFrames = 3;
        PngDecoderOptions decodeFrameCountLimit = CreateDecodeOptions();
        decodeFrameCountLimit.Limits.MaxAnimationFrames = 3;
        PngDecoderOptions decodeFrameByteLimit = CreateDecodeOptions();
        decodeFrameByteLimit.Limits.MaxFrameBytes = 23;
        PngDecoderOptions decodeAnimationByteLimit = CreateDecodeOptions();
        decodeAnimationByteLimit.Limits.MaxAnimationBytes = 139;

        Assert.Throws<OverflowException>(() => new PngCodec().EncodeToBytes(animation, encodeFrameCountLimit));
        Assert.Throws<OverflowException>(() => new PngCodec().DecodeBytes(encoded, decodeFrameCountLimit));
        Assert.Throws<OverflowException>(() => new PngCodec().DecodeBytes(encoded, decodeFrameByteLimit));
        Assert.Throws<OverflowException>(() => new PngCodec().DecodeBytes(encoded, decodeAnimationByteLimit));
    }

    static PngImage CreateIncludedDefaultAnimation()
    {
        byte[] redCanvas = new byte[]
        {
            255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255,
            255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255
        };
        PngImage image = new PngImageFactory().CreateImageWithData(3, 2, PngPixelFormat.Rgba8, redCanvas);
        image.Metadata.Chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkGamma, new byte[] { 0, 0, 177, 143 }),
            CreateChunk(PngConstants.ChunkText, Encoding.Latin1.GetBytes("Author\0Codex"))
        };

        PngImageFactory factory = new PngImageFactory();
        PngFrame[] frames = new PngFrame[]
        {
            factory.CreateFrameWithData(3, 2, 0, 0, PngPixelFormat.Rgba8, 12, redCanvas, 0, 0, PngApngDisposeOperation.None, PngApngBlendOperation.Source),
            factory.CreateFrameWithData(1, 1, 1, 0, PngPixelFormat.Rgba8, 4, new byte[] { 0, 0, 255, 128 }, 1, 100, PngApngDisposeOperation.Background, PngApngBlendOperation.Over),
            factory.CreateFrameWithData(2, 1, 0, 1, PngPixelFormat.Rgba8, 8, new byte[] { 0, 255, 0, 255, 255, 255, 0, 255 }, 65535, 1, PngApngDisposeOperation.Previous, PngApngBlendOperation.Source),
            factory.CreateFrameWithData(1, 2, 1, 0, PngPixelFormat.Rgba8, 4, new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 }, 1, 65535, PngApngDisposeOperation.None, PngApngBlendOperation.Source)
        };
        return factory.CreateAnimationImage(image, 3, true, frames);
    }

    static PngImage CreateSeparateDefaultAnimation()
    {
        PngImageFactory factory = new PngImageFactory();
        PngImage image = factory.CreateImageWithData(2, 1, PngPixelFormat.Rgba8, new byte[] { 1, 2, 3, 255, 4, 5, 6, 255 });
        PngFrame[] frames = new PngFrame[]
        {
            factory.CreateFrameWithData(2, 1, 0, 0, PngPixelFormat.Rgba8, 8, new byte[] { 0, 0, 255, 255, 0, 0, 255, 255 }, 2, 25, PngApngDisposeOperation.None, PngApngBlendOperation.Source),
            factory.CreateFrameWithData(1, 1, 1, 0, PngPixelFormat.Rgba8, 4, new byte[] { 255, 0, 0, 128 }, 4, 50, PngApngDisposeOperation.Previous, PngApngBlendOperation.Over)
        };
        return factory.CreateAnimationImage(image, 0, false, frames);
    }

    static PngEncoderOptions CreateAnimationOptions()
    {
        PngEncoderOptions options = new PngEncoderOptions();
        options.EncodeAnimation = true;
        options.UseAutomaticColorType = false;
        options.ColorType = PngColorType.TruecolorAlpha;
        options.UseAutomaticBitDepth = false;
        options.BitDepth = PngBitDepth.Eight;
        options.UseFixedFilter = true;
        options.RowFilterStrategy = PngRowFilterStrategy.Fixed;
        options.FixedFilterType = PngFilterType.None;
        return options;
    }

    static PngDecoderOptions CreateDecodeOptions()
    {
        PngDecoderOptions options = new PngDecoderOptions();
        options.PreferredPixelFormat = PngPixelFormat.Rgba8;
        return options;
    }

    static void AssertIncludedDefaultAnimation(PngImage image)
    {
        Assert.That(image.Animation.FrameCount, Is.EqualTo(4));
        Assert.That(image.Animation.PlayCount, Is.EqualTo(3));
        Assert.That(image.Animation.Frames, Has.Length.EqualTo(4));
        Assert.That(image.Animation.ComposedFrames, Has.Length.EqualTo(4));
        AssertFrame(image.Animation.Frames[0], 3, 2, 0, 0, 0, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, true);
        AssertFrame(image.Animation.Frames[1], 1, 1, 1, 0, 1, 100, PngApngDisposeOperation.Background, PngApngBlendOperation.Over, false);
        AssertFrame(image.Animation.Frames[2], 2, 1, 0, 1, 65535, 1, PngApngDisposeOperation.Previous, PngApngBlendOperation.Source, false);
        AssertFrame(image.Animation.Frames[3], 1, 2, 1, 0, 1, 65535, PngApngDisposeOperation.None, PngApngBlendOperation.Source, false);
        Assert.That(image.Animation.Frames[0].Pixels.Data, Is.EqualTo(new byte[]
        {
            255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255,
            255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255
        }));
        Assert.That(image.Animation.Frames[1].Pixels.Data, Is.EqualTo(new byte[] { 0, 0, 255, 128 }));
        Assert.That(image.Animation.Frames[2].Pixels.Data, Is.EqualTo(new byte[] { 0, 255, 0, 255, 255, 255, 0, 255 }));
        Assert.That(image.Animation.Frames[3].Pixels.Data, Is.EqualTo(new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 }));

        AssertCanvas(image.Animation.ComposedFrames[0], 3, 2, new byte[]
        {
            255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255,
            255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255
        });
        AssertCanvas(image.Animation.ComposedFrames[1], 3, 2, new byte[]
        {
            255, 0, 0, 255, 127, 0, 128, 255, 255, 0, 0, 255,
            255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255
        });
        AssertCanvas(image.Animation.ComposedFrames[2], 3, 2, new byte[]
        {
            255, 0, 0, 255, 0, 0, 0, 0, 255, 0, 0, 255,
            0, 255, 0, 255, 255, 255, 0, 255, 255, 0, 0, 255
        });
        AssertCanvas(image.Animation.ComposedFrames[3], 3, 2, new byte[]
        {
            255, 0, 0, 255, 255, 255, 255, 255, 255, 0, 0, 255,
            255, 0, 0, 255, 255, 255, 255, 255, 255, 0, 0, 255
        });
        Assert.That(HasMetadataChunk(image, PngConstants.ChunkGamma), Is.True);
        Assert.That(HasMetadataChunk(image, PngConstants.ChunkText), Is.True);
    }

    static void AssertSeparateDefaultAnimation(PngImage image)
    {
        Assert.That(image.Info.IsAnimated, Is.True);
        Assert.That(image.Animation.FrameCount, Is.EqualTo(2));
        Assert.That(image.Animation.PlayCount, Is.EqualTo(0));
        Assert.That(image.Animation.Frames.All(frame => !frame.UsesDefaultImage), Is.True);
        AssertFrame(image.Animation.Frames[0], 2, 1, 0, 0, 2, 25, PngApngDisposeOperation.None, PngApngBlendOperation.Source, false);
        AssertFrame(image.Animation.Frames[1], 1, 1, 1, 0, 4, 50, PngApngDisposeOperation.Previous, PngApngBlendOperation.Over, false);
        Assert.That(image.Animation.Frames[0].Pixels.Data, Is.EqualTo(new byte[] { 0, 0, 255, 255, 0, 0, 255, 255 }));
        Assert.That(image.Animation.Frames[1].Pixels.Data, Is.EqualTo(new byte[] { 255, 0, 0, 128 }));
        AssertCanvas(image.Animation.ComposedFrames[0], 2, 1, new byte[] { 0, 0, 255, 255, 0, 0, 255, 255 });
        AssertCanvas(image.Animation.ComposedFrames[1], 2, 1, new byte[] { 0, 0, 255, 255, 128, 0, 127, 255 });
    }

    static void AssertFrame(PngFrame frame, int width, int height, int xOffset, int yOffset, int delayNumerator, int delayDenominator, PngApngDisposeOperation disposeOperation, PngApngBlendOperation blendOperation, bool usesDefaultImage)
    {
        Assert.Multiple(() =>
        {
            Assert.That(frame.Width, Is.EqualTo(width));
            Assert.That(frame.Height, Is.EqualTo(height));
            Assert.That(frame.XOffset, Is.EqualTo(xOffset));
            Assert.That(frame.YOffset, Is.EqualTo(yOffset));
            Assert.That(frame.DelayNumerator, Is.EqualTo(delayNumerator));
            Assert.That(frame.DelayDenominator, Is.EqualTo(delayDenominator));
            Assert.That(frame.DisposeOperation, Is.EqualTo(disposeOperation));
            Assert.That(frame.BlendOperation, Is.EqualTo(blendOperation));
            Assert.That(frame.UsesDefaultImage, Is.EqualTo(usesDefaultImage));
        });
    }

    static void AssertCanvas(PngPixelBuffer canvas, int width, int height, byte[] expected)
    {
        Assert.That(canvas.Format, Is.EqualTo(PngPixelFormat.Rgba8));
        Assert.That(canvas.Width, Is.EqualTo(width));
        Assert.That(canvas.Height, Is.EqualTo(height));
        Assert.That(canvas.Stride, Is.EqualTo(width * 4));
        Assert.That(canvas.Data, Is.EqualTo(expected));
    }

    static PngChunk CreateChunk(string type, byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = type;
        chunk.Data = data;
        return chunk;
    }

    static bool HasMetadataChunk(PngImage image, string type)
    {
        return image.Metadata.Chunks.Any(chunk => chunk.Type == type);
    }

    static bool HasChunk(byte[] encoded, string type)
    {
        return Png.ReadChunks(encoded).Chunks.Any(chunk => chunk.Type == type);
    }

    static int FindChunk(PngChunk[] chunks, string type)
    {
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == type)
            {
                return index;
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

    static byte[] ReplaceFirstSequenceNumber(byte[] encoded, string chunkType, uint sequenceNumber)
    {
        byte[] result = encoded.ToArray();
        int offset = PngConstants.SignatureLength;
        while (offset <= result.Length - 12)
        {
            int length = ReadInt32(result, offset);
            string type = Encoding.ASCII.GetString(result, offset + 4, 4);
            if (type == chunkType)
            {
                WriteUInt32(result, offset + 8, sequenceNumber);
                return result;
            }

            offset += length + 12;
        }

        throw new InvalidOperationException("APNG sequence chunk was not found.");
    }

    static int ReadInt32(byte[] data, int offset)
    {
        return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
    }

    static void WriteUInt32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }
}
