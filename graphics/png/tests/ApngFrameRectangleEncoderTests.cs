using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ApngFrameRectangleEncoderTests
{
    [Test]
    public void Encode_WritesFrameLocalDataForRectangleFrames()
    {
        PngIhdr ihdr = CreateIhdr(3, 2, PngInterlaceMethod.None);
        PngAnimation animation = new PngAnimation();
        animation.FrameCount = 2;
        animation.PlayCount = 4;
        animation.Frames = new PngFrame[]
        {
            CreateFrame(3, 2, 0, 0, 5, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateRgba8Buffer(3, 2, 12, new byte[]
            {
                1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12,
                13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24
            })),
            CreateFrame(2, 1, 1, 1, 6, 100, PngApngDisposeOperation.Background, PngApngBlendOperation.Over, CreateRgba8Buffer(2, 1, 12, new byte[]
            {
                50, 51, 52, 53, 60, 61, 62, 63, 99, 99, 99, 99
            }))
        };

        PngChunk[] chunks = PngApngFrameRectangleEncoder.Encode(ihdr, animation);

        Assert.That(chunks.Length, Is.EqualTo(5));
        Assert.That(chunks[0].Type, Is.EqualTo(PngConstants.ChunkAnimationControl));
        Assert.That(chunks[1].Type, Is.EqualTo(PngConstants.ChunkFrameControl));
        Assert.That(chunks[2].Type, Is.EqualTo(PngConstants.ChunkIDAT));
        Assert.That(chunks[3].Type, Is.EqualTo(PngConstants.ChunkFrameControl));
        Assert.That(chunks[4].Type, Is.EqualTo(PngConstants.ChunkFrameData));
        AssertAnimationControl(PngAnimationControlParser.Parse(chunks[0]), 2, 4);
        AssertFrameControl(PngFrameControlParser.Parse(chunks[1]), 0, 3, 2, 0, 0, 5, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source);
        AssertFrameControl(PngFrameControlParser.Parse(chunks[3]), 1, 2, 1, 1, 1, 6, 100, PngApngDisposeOperation.Background, PngApngBlendOperation.Over);

        PngFrameData frameData = PngFrameDataParser.Parse(chunks[4]);
        Assert.That(frameData.SequenceNumber, Is.EqualTo(2));
        AssertDecodedFrame(PngApngFrameDecoder.Decode(PngFrameControlParser.Parse(chunks[1]), ihdr, chunks[2].Data), 3, 2, 12, new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12,
            13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24
        });
        AssertDecodedFrame(PngApngFrameDecoder.Decode(PngFrameControlParser.Parse(chunks[3]), ihdr, new PngFrameData[] { frameData }), 2, 1, 8, new byte[]
        {
            50, 51, 52, 53, 60, 61, 62, 63
        });

        PngValidationResult sequenceResult = PngApngSequenceValidator.Validate(chunks);
        Assert.That(sequenceResult.IsValid, Is.True);
    }

    [Test]
    public void Encode_EncodesAdam7RectangleFrameData()
    {
        PngIhdr ihdr = CreateIhdr(2, 2, PngInterlaceMethod.Adam7);
        PngAnimation animation = new PngAnimation();
        animation.Frames = new PngFrame[]
        {
            CreateFrame(2, 2, 0, 0, 0, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateRgba8Buffer(2, 2, 8, new byte[]
            {
                1, 2, 3, 4, 5, 6, 7, 8,
                9, 10, 11, 12, 13, 14, 15, 16
            })),
            CreateFrame(1, 1, 1, 1, 0, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateRgba8Buffer(1, 1, 8, new byte[]
            {
                90, 91, 92, 93, 99, 99, 99, 99
            }))
        };

        PngChunk[] chunks = PngApngFrameRectangleEncoder.Encode(ihdr, animation);
        PngFrameData frameData = PngFrameDataParser.Parse(chunks[4]);

        AssertDecodedFrame(PngApngFrameDecoder.Decode(PngFrameControlParser.Parse(chunks[3]), ihdr, new PngFrameData[] { frameData }), 1, 1, 4, new byte[]
        {
            90, 91, 92, 93
        });
    }

    [Test]
    public void Encode_NormalizesReorderedFramePixels()
    {
        PngIhdr ihdr = CreateIhdr(1, 1, PngInterlaceMethod.None);
        PngAnimation animation = new PngAnimation();
        animation.Frames = new PngFrame[]
        {
            CreateFrame(1, 1, 0, 0, 0, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateBuffer(PngPixelFormat.Bgra8, 1, 1, 4, new byte[] { 30, 20, 10, 40 })),
            CreateFrame(1, 1, 0, 0, 0, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateBuffer(PngPixelFormat.Bgra8, 1, 1, 4, new byte[] { 70, 60, 50, 80 }))
        };

        PngChunk[] chunks = PngApngFrameRectangleEncoder.Encode(ihdr, animation);
        PngFrameData frameData = PngFrameDataParser.Parse(chunks[4]);

        AssertDecodedFrame(PngApngFrameDecoder.Decode(PngFrameControlParser.Parse(chunks[1]), ihdr, chunks[2].Data), 1, 1, 4, new byte[] { 10, 20, 30, 40 });
        AssertDecodedFrame(PngApngFrameDecoder.Decode(PngFrameControlParser.Parse(chunks[3]), ihdr, new PngFrameData[] { frameData }), 1, 1, 4, new byte[] { 50, 60, 70, 80 });
    }

    [Test]
    public void Encode_RejectsInvalidRectangleInputs()
    {
        PngIhdr ihdr = CreateIhdr(2, 2, PngInterlaceMethod.None);
        PngFrame full = CreateFrame(2, 2, 0, 0, 0, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateRgba8Buffer(2, 2, 8, new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8,
            9, 10, 11, 12, 13, 14, 15, 16
        }));

        Assert.Throws<ArgumentNullException>(() => PngApngFrameRectangleEncoder.Encode(null!, CreateAnimation(new PngFrame[] { full })));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameRectangleEncoder.Encode(ihdr, null!));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameRectangleEncoder.Encode(ihdr, CreateAnimation(new PngFrame[] { null! })));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameRectangleEncoder.Encode(ihdr, CreateAnimation(new PngFrame[] { CreateFrame(2, 2, 0, 0, 0, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, null!) })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameRectangleEncoder.Encode(ihdr, new PngAnimation()));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameRectangleEncoder.Encode(ihdr, CreateAnimation(new PngFrame[] { CreateFrame(1, 1, 0, 0, 0, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateRgba8Buffer(1, 1, 4, new byte[] { 1, 2, 3, 4 })) })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameRectangleEncoder.Encode(ihdr, CreateAnimation(new PngFrame[] { CreateFrame(2, 2, 1, 0, 0, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateRgba8Buffer(2, 2, 8, new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8,
            9, 10, 11, 12, 13, 14, 15, 16
        })) })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameRectangleEncoder.Encode(ihdr, CreateAnimation(new PngFrame[] { full, CreateFrame(2, 1, 1, 0, 0, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateRgba8Buffer(2, 1, 8, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })) })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameRectangleEncoder.Encode(ihdr, CreateAnimation(new PngFrame[] { full, CreateFrame(1, 2, 0, 1, 0, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateRgba8Buffer(1, 2, 4, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })) })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameRectangleEncoder.Encode(ihdr, CreateAnimation(new PngFrame[] { full, CreateFrame(1, 1, -1, 0, 0, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateRgba8Buffer(1, 1, 4, new byte[] { 1, 2, 3, 4 })) })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameRectangleEncoder.Encode(ihdr, CreateAnimation(new PngFrame[] { full, CreateFrame(1, 1, 0, -1, 0, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateRgba8Buffer(1, 1, 4, new byte[] { 1, 2, 3, 4 })) })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameRectangleEncoder.Encode(ihdr, CreateAnimation(new PngFrame[] { full, CreateFrame(1, 1, 0, 0, 0, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateRgba8Buffer(2, 1, 8, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })) })));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngFrameRectangleEncoder.Encode(ihdr, CreateAnimation(new PngFrame[] { full, CreateFrame(1, 1, 0, 0, 0, 100, PngApngDisposeOperation.None, PngApngBlendOperation.Source, CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 1, 2, 3 })) })));
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

    static void AssertDecodedFrame(PngPixelBuffer buffer, int width, int height, int stride, byte[] data)
    {
        Assert.That(buffer.Format, Is.EqualTo(PngPixelFormat.Rgba8));
        Assert.That(buffer.Width, Is.EqualTo(width));
        Assert.That(buffer.Height, Is.EqualTo(height));
        Assert.That(buffer.Stride, Is.EqualTo(stride));
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
