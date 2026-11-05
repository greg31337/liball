using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ApngAnimationLimitEnforcerTests
{
    [Test]
    public void CalculateDecodedPixelBytes_SumsFrameLocalAndComposedPixelBuffers()
    {
        PngAnimation animation = CreateAnimation(new PngFrame[]
        {
            CreateFrame(2, 1, CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 8, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })),
            CreateFrame(1, 1, CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 9, 10, 11, 12 })),
            CreateFrame(0, 0, CreateBuffer(PngPixelFormat.Grayscale1, 8, 1, 1, new byte[] { 0xAA }))
        });
        animation.ComposedFrames = new PngPixelBuffer[]
        {
            CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 8, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })
        };

        long decodedPixelBytes = PngApngAnimationLimitEnforcer.CalculateDecodedPixelBytes(animation, CreateIhdr(2, 1));

        Assert.That(decodedPixelBytes, Is.EqualTo(21));
    }

    [Test]
    public void Enforce_AcceptsAnimationWithinFrameAndMemoryLimits()
    {
        PngAnimation animation = CreateAnimation(new PngFrame[]
        {
            CreateFrame(2, 1, CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 8, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })),
            CreateFrame(1, 1, CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 9, 10, 11, 12 }))
        });
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxAnimationFrames = 2;
        limits.MaxDecompressedBytes = 12;

        PngApngAnimationLimitEnforcer.Enforce(animation, CreateIhdr(2, 1), limits);
        PngApngAnimationLimitEnforcer.EnforceFrameCount(2, limits);
    }

    [Test]
    public void Enforce_AllowsUnlimitedLimitsWhenZero()
    {
        PngAnimation animation = CreateAnimation(new PngFrame[]
        {
            CreateFrame(2, 1, CreateBuffer(PngPixelFormat.Rgba8, 2, 1, 8, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })),
            CreateFrame(1, 1, CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 9, 10, 11, 12 }))
        });
        PngSecurityLimits limits = new PngSecurityLimits();
        limits.MaxAnimationFrames = 0;
        limits.MaxDecompressedBytes = 0;

        PngApngAnimationLimitEnforcer.Enforce(animation, CreateIhdr(2, 1), limits);
        PngApngAnimationLimitEnforcer.EnforceFrameCount(2, limits);
    }

    [Test]
    public void Encoders_EnforceAggregateAnimationMemoryLimit()
    {
        PngIhdr ihdr = CreateIhdr(1, 1);
        PngEncoderOptions options = new PngEncoderOptions();
        options.Limits.MaxAnimationBytes = 7;
        PngAnimation animation = CreateAnimation(new PngFrame[]
        {
            CreatePlacedFrame(1, 1, 0, 0, CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 1, 2, 3, 4 })),
            CreatePlacedFrame(1, 1, 0, 0, CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 5, 6, 7, 8 }))
        });

        Assert.Throws<OverflowException>(() => PngApngFullCanvasEncoder.Encode(ihdr, animation, options));
        Assert.Throws<OverflowException>(() => PngApngFrameRectangleEncoder.Encode(ihdr, animation, options));
    }

    [Test]
    public void Enforce_RejectsInvalidInputsAndLimits()
    {
        PngAnimation animation = CreateAnimation(new PngFrame[]
        {
            CreateFrame(1, 1, CreateBuffer(PngPixelFormat.Rgba8, 1, 1, 4, new byte[] { 1, 2, 3, 4 }))
        });
        PngAnimation nullFrames = new PngAnimation();
        nullFrames.Frames = null!;
        PngAnimation nullComposedFrames = CreateAnimation(animation.Frames);
        nullComposedFrames.ComposedFrames = null!;
        PngAnimation empty = new PngAnimation();
        PngAnimation mismatchedCount = CreateAnimation(animation.Frames);
        mismatchedCount.FrameCount = 2;
        PngSecurityLimits zeroFrameLimit = new PngSecurityLimits();
        zeroFrameLimit.MaxAnimationFrames = 0;
        PngSecurityLimits oneFrameLimit = new PngSecurityLimits();
        oneFrameLimit.MaxAnimationFrames = 1;
        PngSecurityLimits lowMemoryLimit = new PngSecurityLimits();
        lowMemoryLimit.MaxDecompressedBytes = 3;
        PngSecurityLimits lowFrameLimit = new PngSecurityLimits();
        lowFrameLimit.MaxFrameBytes = 3;
        PngSecurityLimits lowAnimationLimit = new PngSecurityLimits();
        lowAnimationLimit.MaxAnimationBytes = 3;

        Assert.Throws<ArgumentNullException>(() => PngApngAnimationLimitEnforcer.Enforce(null!, new PngSecurityLimits()));
        Assert.Throws<ArgumentNullException>(() => PngApngAnimationLimitEnforcer.Enforce(nullFrames, new PngSecurityLimits()));
        Assert.Throws<ArgumentNullException>(() => PngApngAnimationLimitEnforcer.Enforce(animation, (PngSecurityLimits)null!));
        Assert.Throws<ArgumentNullException>(() => PngApngAnimationLimitEnforcer.Enforce(animation, null!, new PngSecurityLimits()));
        Assert.Throws<ArgumentNullException>(() => PngApngAnimationLimitEnforcer.CalculateDecodedPixelBytes(null!, CreateIhdr(1, 1)));
        Assert.Throws<ArgumentNullException>(() => PngApngAnimationLimitEnforcer.CalculateDecodedPixelBytes(nullFrames, CreateIhdr(1, 1)));
        Assert.Throws<ArgumentNullException>(() => PngApngAnimationLimitEnforcer.CalculateDecodedPixelBytes(nullComposedFrames, CreateIhdr(1, 1)));
        Assert.Throws<ArgumentNullException>(() => PngApngAnimationLimitEnforcer.CalculateDecodedPixelBytes(animation, null!));
        Assert.Throws<ArgumentNullException>(() => PngApngAnimationLimitEnforcer.EnforceFrameCount(1, null!));
        Assert.Throws<ArgumentNullException>(() => PngApngAnimationLimitEnforcer.Enforce(CreateAnimation(new PngFrame[] { null! }), CreateIhdr(1, 1), new PngSecurityLimits()));
        Assert.Throws<ArgumentNullException>(() => PngApngAnimationLimitEnforcer.Enforce(CreateAnimation(new PngFrame[] { CreateFrame(1, 1, null!) }), CreateIhdr(1, 1), new PngSecurityLimits()));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngAnimationLimitEnforcer.Enforce(empty, new PngSecurityLimits()));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngAnimationLimitEnforcer.Enforce(mismatchedCount, new PngSecurityLimits()));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngAnimationLimitEnforcer.EnforceFrameCount(0, new PngSecurityLimits()));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngAnimationLimitEnforcer.Enforce(CreateAnimation(new PngFrame[] { CreateFrame(1, 1, CreateBuffer(PngPixelFormat.Unknown, 1, 1, 1, new byte[] { 0 })) }), CreateIhdr(1, 1), new PngSecurityLimits()));
        Assert.Throws<OverflowException>(() => PngApngAnimationLimitEnforcer.Enforce(CreateAnimation(new PngFrame[] { animation.Frames[0], animation.Frames[0] }), CreateIhdr(1, 1), oneFrameLimit));
        Assert.Throws<OverflowException>(() => PngApngAnimationLimitEnforcer.Enforce(animation, CreateIhdr(1, 1), lowMemoryLimit));
        Assert.Throws<OverflowException>(() => PngApngAnimationLimitEnforcer.Enforce(animation, CreateIhdr(1, 1), lowFrameLimit));
        Assert.Throws<OverflowException>(() => PngApngAnimationLimitEnforcer.Enforce(animation, CreateIhdr(1, 1), lowAnimationLimit));
        Assert.Throws<OverflowException>(() => PngApngAnimationLimitEnforcer.EnforceFrameCount((uint)int.MaxValue + 1u, zeroFrameLimit));
        Assert.Throws<OverflowException>(() => PngApngAnimationLimitEnforcer.EnforceFrameCount(2, oneFrameLimit));
    }

    static PngIhdr CreateIhdr(int width, int height)
    {
        PngIhdr ihdr = new PngIhdr();
        ihdr.Width = width;
        ihdr.Height = height;
        ihdr.BitDepth = PngBitDepth.Eight;
        ihdr.ColorType = PngColorType.TruecolorAlpha;
        ihdr.CompressionMethod = PngCompressionMethod.Deflate;
        ihdr.FilterMethod = PngFilterMethod.Adaptive;
        ihdr.InterlaceMethod = PngInterlaceMethod.None;
        return ihdr;
    }

    static PngAnimation CreateAnimation(PngFrame[] frames)
    {
        PngAnimation animation = new PngAnimation();
        animation.Frames = frames;
        return animation;
    }

    static PngFrame CreateFrame(int width, int height, PngPixelBuffer pixels)
    {
        return CreatePlacedFrame(width, height, 0, 0, pixels);
    }

    static PngFrame CreatePlacedFrame(int width, int height, int xOffset, int yOffset, PngPixelBuffer pixels)
    {
        PngFrame frame = new PngFrame();
        frame.Width = width;
        frame.Height = height;
        frame.XOffset = xOffset;
        frame.YOffset = yOffset;
        frame.DelayDenominator = 100;
        frame.DisposeOperation = PngApngDisposeOperation.None;
        frame.BlendOperation = PngApngBlendOperation.Source;
        frame.Pixels = pixels;
        return frame;
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
