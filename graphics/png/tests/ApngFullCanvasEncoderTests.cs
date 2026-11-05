using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ApngFullCanvasEncoderTests
{
    [Test]
    public void Encode_WritesAnimationChunksForFullCanvasFrames()
    {
        PngIhdr ihdr = CreateIhdr(2, 1, PngInterlaceMethod.None);
        PngAnimation animation = new PngAnimation();
        animation.FrameCount = 2;
        animation.PlayCount = 3;
        animation.Frames = new PngFrame[]
        {
            CreateFrame(2, 1, 0, 0, 10, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateRgba8Buffer(2, 1, 8, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })),
            CreateFrame(2, 1, 0, 0, 20, 100, PngApngDisposeOperation.Background, PngApngBlendOperation.Over, CreateRgba8Buffer(2, 1, 8, new byte[] { 10, 20, 30, 40, 50, 60, 70, 80 }))
        };

        PngChunk[] chunks = PngApngFullCanvasEncoder.Encode(ihdr, animation);

        Assert.That(chunks.Length, Is.EqualTo(5));
        Assert.That(chunks[0].Type, Is.EqualTo(PngConstants.ChunkAnimationControl));
        Assert.That(chunks[1].Type, Is.EqualTo(PngConstants.ChunkFrameControl));
        Assert.That(chunks[2].Type, Is.EqualTo(PngConstants.ChunkIDAT));
        Assert.That(chunks[3].Type, Is.EqualTo(PngConstants.ChunkFrameControl));
        Assert.That(chunks[4].Type, Is.EqualTo(PngConstants.ChunkFrameData));
        AssertAnimationControl(PngAnimationControlParser.Parse(chunks[0]), 2, 3);
        AssertFrameControl(PngFrameControlParser.Parse(chunks[1]), 0, 2, 1, 0, 0, 10, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source);
        AssertFrameControl(PngFrameControlParser.Parse(chunks[3]), 1, 2, 1, 0, 0, 20, 100, PngApngDisposeOperation.Background, PngApngBlendOperation.Over);

        PngFrameData frameData = PngFrameDataParser.Parse(chunks[4]);
        Assert.That(frameData.SequenceNumber, Is.EqualTo(2));
        AssertDecodedFrame(PngApngFrameDecoder.Decode(PngFrameControlParser.Parse(chunks[1]), ihdr, chunks[2].Data), new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        AssertDecodedFrame(PngApngFrameDecoder.Decode(PngFrameControlParser.Parse(chunks[3]), ihdr, new PngFrameData[] { frameData }), new byte[] { 10, 20, 30, 40, 50, 60, 70, 80 });

        PngValidationResult sequenceResult = PngApngSequenceValidator.Validate(chunks);
        Assert.That(sequenceResult.IsValid, Is.True);
    }

    [Test]
    public void Encode_InfersFullCanvasFrameDimensionsWithoutMutatingFrame()
    {
        PngIhdr ihdr = CreateIhdr(1, 1, PngInterlaceMethod.None);
        PngFrame frame = CreateFrame(0, 0, 0, 0, 1, 24, PngApngDisposeOperation.Previous, PngApngBlendOperation.Source, CreateRgba8Buffer(1, 1, 4, new byte[] { 9, 8, 7, 6 }));
        PngAnimation animation = new PngAnimation();
        animation.Frames = new PngFrame[] { frame };

        PngChunk[] chunks = PngApngFullCanvasEncoder.Encode(ihdr, animation);

        AssertFrameControl(PngFrameControlParser.Parse(chunks[1]), 0, 1, 1, 0, 0, 1, 24, PngApngDisposeOperation.Previous, PngApngBlendOperation.Source);
        Assert.That(frame.Width, Is.EqualTo(0));
        Assert.That(frame.Height, Is.EqualTo(0));
    }

    [Test]
    public void Encode_EncodesAdam7FramesUsingPixelRows()
    {
        PngIhdr ihdr = CreateIhdr(1, 1, PngInterlaceMethod.Adam7);
        PngAnimation animation = new PngAnimation();
        animation.Frames = new PngFrame[]
        {
            CreateFrame(1, 1, 0, 0, 0, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateRgba8Buffer(1, 1, 8, new byte[] { 11, 12, 13, 14, 99, 99, 99, 99 }))
        };

        PngChunk[] chunks = PngApngFullCanvasEncoder.Encode(ihdr, animation);

        AssertDecodedFrame(PngApngFrameDecoder.Decode(PngFrameControlParser.Parse(chunks[1]), ihdr, chunks[2].Data), new byte[] { 11, 12, 13, 14 });
    }

    [Test]
    public void Encode_RejectsInvalidInputs()
    {
        PngIhdr ihdr = CreateIhdr(1, 1, PngInterlaceMethod.None);
        PngAnimation animation = new PngAnimation();
        animation.Frames = new PngFrame[] { CreateFrame(1, 1, 0, 0, 0, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateRgba8Buffer(1, 1, 4, new byte[] { 1, 2, 3, 4 })) };
        PngAnimation nullFrames = new PngAnimation();
        nullFrames.Frames = null!;
        PngAnimation emptyFrames = new PngAnimation();
        PngAnimation mismatchedCount = new PngAnimation();
        mismatchedCount.FrameCount = 2;
        mismatchedCount.Frames = animation.Frames;
        PngEncoderOptions nullLimits = new PngEncoderOptions();
        nullLimits.Limits = null!;
        PngEncoderOptions oneFrameLimit = new PngEncoderOptions();
        oneFrameLimit.Limits.MaxAnimationFrames = 1;

        Assert.Throws<ArgumentNullException>(() => PngApngFullCanvasEncoder.Encode(null!, animation));
        Assert.Throws<ArgumentNullException>(() => PngApngFullCanvasEncoder.Encode(ihdr, null!));
        Assert.Throws<ArgumentNullException>(() => PngApngFullCanvasEncoder.Encode(ihdr, nullFrames));
        Assert.Throws<ArgumentNullException>(() => PngApngFullCanvasEncoder.Encode(ihdr, animation, null!));
        Assert.Throws<ArgumentNullException>(() => PngApngFullCanvasEncoder.Encode(ihdr, animation, nullLimits));
        Assert.Throws<ArgumentNullException>(() => PngApngFullCanvasEncoder.Encode(ihdr, CreateAnimation(new PngFrame[] { null! })));
        Assert.Throws<ArgumentNullException>(() => PngApngFullCanvasEncoder.Encode(ihdr, CreateAnimation(new PngFrame[] { CreateFrame(1, 1, 0, 0, 0, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, null!) })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFullCanvasEncoder.Encode(ihdr, emptyFrames));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFullCanvasEncoder.Encode(ihdr, mismatchedCount));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFullCanvasEncoder.Encode(ihdr, CreateAnimation(new PngFrame[] { CreateFrame(2, 1, 0, 0, 0, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateRgba8Buffer(2, 1, 8, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })) })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFullCanvasEncoder.Encode(ihdr, CreateAnimation(new PngFrame[] { CreateFrame(1, 2, 0, 0, 0, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateRgba8Buffer(1, 2, 4, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })) })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFullCanvasEncoder.Encode(ihdr, CreateAnimation(new PngFrame[] { CreateFrame(1, 1, 1, 0, 0, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateRgba8Buffer(1, 1, 4, new byte[] { 1, 2, 3, 4 })) })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFullCanvasEncoder.Encode(ihdr, CreateAnimation(new PngFrame[] { CreateFrame(1, 1, 0, 1, 0, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateRgba8Buffer(1, 1, 4, new byte[] { 1, 2, 3, 4 })) })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFullCanvasEncoder.Encode(ihdr, CreateAnimation(new PngFrame[] { CreateFrame(1, 1, 0, 0, -1, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateRgba8Buffer(1, 1, 4, new byte[] { 1, 2, 3, 4 })) })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFullCanvasEncoder.Encode(ihdr, CreateAnimation(new PngFrame[] { CreateFrame(1, 1, 0, 0, 0, 100000, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateRgba8Buffer(1, 1, 4, new byte[] { 1, 2, 3, 4 })) })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFullCanvasEncoder.Encode(ihdr, CreateAnimation(new PngFrame[] { CreateFrame(1, 1, 0, 0, 0, 100, (PngApngDisposeOperation)9, PngApngBlendOperation.Source, CreateRgba8Buffer(1, 1, 4, new byte[] { 1, 2, 3, 4 })) })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFullCanvasEncoder.Encode(ihdr, CreateAnimation(new PngFrame[] { CreateFrame(1, 1, 0, 0, 0, 100, PngApngDisposeOperation.None, (PngApngBlendOperation)9, CreateRgba8Buffer(1, 1, 4, new byte[] { 1, 2, 3, 4 })) })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFullCanvasEncoder.Encode(ihdr, CreateAnimation(new PngFrame[] { CreateFrame(1, 1, 0, 0, 0, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 1, 2, 3 })) })));
        Assert.Throws<OverflowException>(() => PngApngFullCanvasEncoder.Encode(ihdr, CreateAnimation(new PngFrame[] { animation.Frames[0], animation.Frames[0] }), oneFrameLimit));
    }

    static void AssertAnimationControl(PngAnimationControl control, uint frameCount, uint playCount)
    {
        Assert.That(control.FrameCount, Is.EqualTo(frameCount));
        Assert.That(control.PlayCount, Is.EqualTo(playCount));
    }

    static void AssertFrameControl(PngFrameControl control, uint sequenceNumber, uint width, uint height, uint xOffset, uint yOffset, ushort delayNumerator, ushort delayDenominator, PngApngDisposeOperation disposeOperation, PngApngBlendOperation blendOperation)
    {
        Assert.That(control.SequenceNumber, Is.EqualTo(sequenceNumber));
        Assert.That(control.Width, Is.EqualTo(width));
        Assert.That(control.Height, Is.EqualTo(height));
        Assert.That(control.XOffset, Is.EqualTo(xOffset));
        Assert.That(control.YOffset, Is.EqualTo(yOffset));
        Assert.That(control.DelayNumerator, Is.EqualTo(delayNumerator));
        Assert.That(control.DelayDenominator, Is.EqualTo(delayDenominator));
        Assert.That(control.DisposeOperation, Is.EqualTo(disposeOperation));
        Assert.That(control.BlendOperation, Is.EqualTo(blendOperation));
    }

    static void AssertDecodedFrame(PngPixelBuffer buffer, byte[] data)
    {
        Assert.That(buffer.Format, Is.EqualTo(PngPixelFormat.Rgba8));
        Assert.That(buffer.Width, Is.EqualTo(data.Length / 4));
        Assert.That(buffer.Height, Is.EqualTo(1));
        Assert.That(buffer.Stride, Is.EqualTo(data.Length));
        Assert.That(buffer.Data, Is.EqualTo(data));
    }

    static PngIhdr CreateIhdr(int width, int height, PngInterlaceMethod interlaceMethod)
    {
        PngIhdr ihdr = new PngIhdr();
        ihdr.Width = width;
        ihdr.Height = height;
        ihdr.BitDepth = PngBitDepth.Eight;
        ihdr.ColorType = PngColorType.TruecolorAlpha;
        ihdr.CompressionMethod = PngCompressionMethod.Deflate;
        ihdr.FilterMethod = PngFilterMethod.Adaptive;
        ihdr.InterlaceMethod = interlaceMethod;
        return ihdr;
    }

    static PngAnimation CreateAnimation(PngFrame[] frames)
    {
        PngAnimation animation = new PngAnimation();
        animation.Frames = frames;
        return animation;
    }

    static PngFrame CreateFrame(int width, int height, int xOffset, int yOffset, int delayNumerator, int delayDenominator, PngApngDisposeOperation disposeOperation, PngApngBlendOperation blendOperation, PngPixelBuffer pixels)
    {
        PngFrame frame = new PngFrame();
        frame.Width = width;
        frame.Height = height;
        frame.XOffset = xOffset;
        frame.YOffset = yOffset;
        frame.DelayNumerator = delayNumerator;
        frame.DelayDenominator = delayDenominator;
        frame.DisposeOperation = disposeOperation;
        frame.BlendOperation = blendOperation;
        frame.Pixels = pixels;
        return frame;
    }

    static PngPixelBuffer CreateRgba8Buffer(int width, int height, int stride, byte[] data)
    {
        return CreateBuffer(PngPixelFormat.Rgba8, width, height, stride, data);
    }

    static PngPixelBuffer CreateBuffer(PngPixelFormat format, int width, int height, int stride, byte[] data)
    {
        PngPixelBuffer buffer = new PngPixelBuffer();
        buffer.Format = format;
        buffer.Width = width;
        buffer.Height = height;
        buffer.Stride = stride;
        buffer.Data = data;
        return buffer;
    }
}
