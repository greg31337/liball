using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ApngDefaultImageFrameHandlerTests
{
    [Test]
    public void UsesDefaultImageFrame_ReturnsTrueWhenFirstFctlPrecedesIdat()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkAnimationControl),
            CreateChunk(PngConstants.ChunkFrameControl),
            CreateChunk(PngConstants.ChunkIDAT),
            CreateChunk(PngConstants.ChunkFrameControl)
        };

        bool usesDefaultImage = PngApngDefaultImageFrameHandler.UsesDefaultImageFrame(chunks);

        Assert.That(usesDefaultImage, Is.True);
    }

    [Test]
    public void UsesDefaultImageFrame_ReturnsFalseWhenIdatPrecedesFirstFctl()
    {
        PngChunk[] chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkAnimationControl),
            CreateChunk(PngConstants.ChunkIDAT),
            CreateChunk(PngConstants.ChunkFrameControl)
        };

        bool usesDefaultImage = PngApngDefaultImageFrameHandler.UsesDefaultImageFrame(chunks);

        Assert.That(usesDefaultImage, Is.False);
    }

    [Test]
    public void UsesDefaultImageFrame_ReturnsFalseWhenFctlOrIdatIsMissing()
    {
        PngChunk[] noFrameControl = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkAnimationControl),
            CreateChunk(PngConstants.ChunkIDAT)
        };
        PngChunk[] noIdat = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkAnimationControl),
            CreateChunk(PngConstants.ChunkFrameControl)
        };

        Assert.That(PngApngDefaultImageFrameHandler.UsesDefaultImageFrame(noFrameControl), Is.False);
        Assert.That(PngApngDefaultImageFrameHandler.UsesDefaultImageFrame(noIdat), Is.False);
    }

    [Test]
    public void UsesDefaultImageFrame_AcceptsChunkListResult()
    {
        PngChunkListResult chunkList = new PngChunkListResult();
        chunkList.Chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkFrameControl),
            CreateChunk(PngConstants.ChunkIDAT)
        };

        bool usesDefaultImage = PngApngDefaultImageFrameHandler.UsesDefaultImageFrame(chunkList);

        Assert.That(usesDefaultImage, Is.True);
    }

    [Test]
    public void CreateDefaultImageFrame_MapsFrameControlAndPixels()
    {
        PngFrameControl control = CreateFrameControl();
        PngPixelBuffer pixels = CreatePixels();

        PngFrame frame = PngApngDefaultImageFrameHandler.CreateDefaultImageFrame(control, pixels);

        Assert.That(frame.Width, Is.EqualTo(320));
        Assert.That(frame.Height, Is.EqualTo(240));
        Assert.That(frame.XOffset, Is.EqualTo(0));
        Assert.That(frame.YOffset, Is.EqualTo(0));
        Assert.That(frame.DelayNumerator, Is.EqualTo(1));
        Assert.That(frame.DelayDenominator, Is.EqualTo(24));
        Assert.That(frame.DisposeOperation, Is.EqualTo(PngApngDisposeOperation.Background));
        Assert.That(frame.BlendOperation, Is.EqualTo(PngApngBlendOperation.Over));
        Assert.That(frame.Pixels, Is.SameAs(pixels));
        Assert.That(frame.UsesDefaultImage, Is.True);
    }

    [Test]
    public void PrependDefaultImageFrame_AddsDefaultFrameWithoutMutatingOriginal()
    {
        PngAnimation animation = new PngAnimation();
        PngFrame laterFrame = new PngFrame();
        laterFrame.Width = 10;
        animation.FrameCount = 2;
        animation.PlayCount = 3;
        animation.Frames = new PngFrame[] { laterFrame };
        PngFrame defaultFrame = PngApngDefaultImageFrameHandler.CreateDefaultImageFrame(CreateFrameControl(), CreatePixels());

        PngAnimation result = PngApngDefaultImageFrameHandler.PrependDefaultImageFrame(animation, defaultFrame);

        Assert.That(result.FrameCount, Is.EqualTo(2));
        Assert.That(result.PlayCount, Is.EqualTo(3));
        Assert.That(result.Frames, Has.Length.EqualTo(2));
        Assert.That(result.Frames[0], Is.SameAs(defaultFrame));
        Assert.That(result.Frames[1], Is.SameAs(laterFrame));
        Assert.That(animation.Frames, Has.Length.EqualTo(1));
    }

    [Test]
    public void RejectsInvalidInputs()
    {
        PngChunk nullType = CreateChunk(PngConstants.ChunkIDAT);
        nullType.Type = null!;
        PngAnimation nullFrames = new PngAnimation();
        nullFrames.Frames = null!;
        PngAnimation nullFrameEntry = new PngAnimation();
        nullFrameEntry.Frames = new PngFrame[] { null! };
        PngFrameControl tooWide = CreateFrameControl();
        tooWide.Width = (uint)int.MaxValue + 1;
        PngFrameControl tooHigh = CreateFrameControl();
        tooHigh.Height = (uint)int.MaxValue + 1;
        PngFrameControl tooFarRight = CreateFrameControl();
        tooFarRight.XOffset = (uint)int.MaxValue + 1;
        PngFrameControl tooFarDown = CreateFrameControl();
        tooFarDown.YOffset = (uint)int.MaxValue + 1;

        Assert.Throws<ArgumentNullException>(() => PngApngDefaultImageFrameHandler.UsesDefaultImageFrame((PngChunk[])null!));
        Assert.Throws<ArgumentNullException>(() => PngApngDefaultImageFrameHandler.UsesDefaultImageFrame((PngChunkListResult)null!));
        Assert.Throws<ArgumentNullException>(() => PngApngDefaultImageFrameHandler.UsesDefaultImageFrame(new PngChunk[] { null! }));
        Assert.Throws<ArgumentNullException>(() => PngApngDefaultImageFrameHandler.UsesDefaultImageFrame(new PngChunk[] { nullType }));
        Assert.Throws<ArgumentNullException>(() => PngApngDefaultImageFrameHandler.CreateDefaultImageFrame(null!, CreatePixels()));
        Assert.Throws<ArgumentNullException>(() => PngApngDefaultImageFrameHandler.CreateDefaultImageFrame(CreateFrameControl(), null!));
        Assert.Throws<ArgumentNullException>(() => PngApngDefaultImageFrameHandler.PrependDefaultImageFrame(null!, new PngFrame()));
        Assert.Throws<ArgumentNullException>(() => PngApngDefaultImageFrameHandler.PrependDefaultImageFrame(nullFrames, new PngFrame()));
        Assert.Throws<ArgumentNullException>(() => PngApngDefaultImageFrameHandler.PrependDefaultImageFrame(new PngAnimation(), null!));
        Assert.Throws<ArgumentNullException>(() => PngApngDefaultImageFrameHandler.PrependDefaultImageFrame(nullFrameEntry, new PngFrame()));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngDefaultImageFrameHandler.CreateDefaultImageFrame(tooWide, CreatePixels()));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngDefaultImageFrameHandler.CreateDefaultImageFrame(tooHigh, CreatePixels()));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngDefaultImageFrameHandler.CreateDefaultImageFrame(tooFarRight, CreatePixels()));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngApngDefaultImageFrameHandler.CreateDefaultImageFrame(tooFarDown, CreatePixels()));
    }

    static PngFrameControl CreateFrameControl()
    {
        PngFrameControl control = new PngFrameControl();
        control.SequenceNumber = 0;
        control.Width = 320;
        control.Height = 240;
        control.XOffset = 0;
        control.YOffset = 0;
        control.DelayNumerator = 1;
        control.DelayDenominator = 24;
        control.DisposeOperation = PngApngDisposeOperation.Background;
        control.BlendOperation = PngApngBlendOperation.Over;
        return control;
    }

    static PngPixelBuffer CreatePixels()
    {
        PngPixelBuffer pixels = new PngPixelBuffer();
        pixels.Format = PngPixelFormat.Rgba8;
        pixels.Width = 320;
        pixels.Height = 240;
        pixels.Stride = 1280;
        pixels.Data = new byte[] { 1, 2, 3, 4 };
        return pixels;
    }

    static PngChunk CreateChunk(string chunkType)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = chunkType;
        return chunk;
    }
}
