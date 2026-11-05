using webp;

namespace webp.Tests;

[TestFixture]
public class WebpDecodeFrameSelectorTests
{
    [Test]
    public void SelectFrames_DefaultOptionsReturnDeepCopiesOfAllFrames()
    {
        List<WebpFrame> frames = NewFrames();
        WebpDecodeOptions options = new WebpDecodeOptions();

        List<WebpFrame> selected = WebpDecodeFrameSelector.SelectFrames(frames, options);
        selected[0].Pixels!.Rgba[0] = 99;
        selected[0].EncodedBitstream![0] = 88;
        selected[0].AlphaData![0] = 77;
        selected[0].UnknownChunks[0].Data[0] = 66;

        Assert.That(selected, Has.Count.EqualTo(3));
        Assert.That(selected[0].DurationMilliseconds, Is.EqualTo(10));
        Assert.That(selected[1].DurationMilliseconds, Is.EqualTo(20));
        Assert.That(selected[2].DurationMilliseconds, Is.EqualTo(30));
        Assert.That(frames[0].Pixels!.Rgba[0], Is.EqualTo(1));
        Assert.That(frames[0].EncodedBitstream![0], Is.EqualTo(5));
        Assert.That(frames[0].AlphaData![0], Is.EqualTo(9));
        Assert.That(frames[0].UnknownChunks[0].Data[0], Is.EqualTo(7));
    }

    [Test]
    public void SelectFrames_FirstFrameOnlyReturnsOnlyFrameZero()
    {
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.FirstFrameOnly = true;

        List<WebpFrame> selected = WebpDecodeFrameSelector.SelectFrames(NewFrames(), options);

        Assert.That(selected, Has.Count.EqualTo(1));
        Assert.That(selected[0].DurationMilliseconds, Is.EqualTo(10));
    }

    [Test]
    public void SelectFrames_FrameIndexSelectsExplicitFrameAndOverridesFirstFrameOnly()
    {
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.FirstFrameOnly = true;
        options.FrameIndex = 2;

        List<WebpFrame> selected = WebpDecodeFrameSelector.SelectFrames(NewFrames(), options);

        Assert.That(selected, Has.Count.EqualTo(1));
        Assert.That(selected[0].DurationMilliseconds, Is.EqualTo(30));
    }

    [Test]
    public void SelectFrames_EmptyFrameListCanRemainEmpty()
    {
        WebpDecodeOptions options = new WebpDecodeOptions();

        List<WebpFrame> selected = WebpDecodeFrameSelector.SelectFrames(new List<WebpFrame>(), options);

        Assert.That(selected, Is.Empty);
    }

    [Test]
    public void SelectFrames_RejectsInvalidFrameIndexes()
    {
        WebpDecodeOptions negative = new WebpDecodeOptions();
        negative.FrameIndex = -2;
        WebpDecodeOptions tooHigh = new WebpDecodeOptions();
        tooHigh.FrameIndex = 3;

        Assert.Throws<ArgumentOutOfRangeException>(() => WebpDecodeFrameSelector.SelectFrames(NewFrames(), negative));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpDecodeFrameSelector.SelectFrames(NewFrames(), tooHigh));
    }

    [Test]
    public void SelectFrames_ImageSelectionCopiesMetadataUnknownChunksAndSelectedFrames()
    {
        WebpImage image = NewImage();
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.FrameIndex = 1;

        WebpImage selected = WebpDecodeFrameSelector.SelectFrames(image, options);
        selected.Metadata.IccProfile![0] = 99;
        selected.UnknownChunks[0].Data[0] = 88;
        selected.Frames[0].Pixels!.Rgba[0] = 77;

        Assert.That(selected.CanvasWidth, Is.EqualTo(3));
        Assert.That(selected.CanvasHeight, Is.EqualTo(2));
        Assert.That(selected.HasAlpha, Is.True);
        Assert.That(selected.HasAnimation, Is.False);
        Assert.That(selected.LoopCount, Is.EqualTo(5));
        Assert.That(selected.BackgroundColorBgra, Is.EqualTo(0x01020304U));
        Assert.That(selected.Frames, Has.Count.EqualTo(1));
        Assert.That(selected.Frames[0].DurationMilliseconds, Is.EqualTo(20));
        Assert.That(image.Metadata.IccProfile![0], Is.EqualTo(1));
        Assert.That(image.UnknownChunks[0].Data[0], Is.EqualTo(4));
        Assert.That(image.Frames[1].Pixels!.Rgba[0], Is.EqualTo(2));
    }

    [Test]
    public void SelectFrames_ImageKeepsAnimationFlagWhenMultipleFramesRemain()
    {
        WebpImage image = NewImage();
        WebpDecodeOptions options = new WebpDecodeOptions();

        WebpImage selected = WebpDecodeFrameSelector.SelectFrames(image, options);

        Assert.That(selected.Frames, Has.Count.EqualTo(3));
        Assert.That(selected.HasAnimation, Is.True);
    }

    [Test]
    public void SelectFrames_ImageFirstFrameOnlyClearsAnimationFlag()
    {
        WebpImage image = NewImage();
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.FirstFrameOnly = true;

        WebpImage selected = WebpDecodeFrameSelector.SelectFrames(image, options);

        Assert.That(selected.Frames, Has.Count.EqualTo(1));
        Assert.That(selected.HasAnimation, Is.False);
    }

    static WebpImage NewImage()
    {
        WebpImage image = new WebpImage();
        image.CanvasWidth = 3;
        image.CanvasHeight = 2;
        image.HasAlpha = true;
        image.HasAnimation = true;
        image.LoopCount = 5;
        image.BackgroundColorBgra = 0x01020304U;
        image.Metadata.IccProfile = new byte[] { 1 };
        image.Metadata.Exif = new byte[] { 2 };
        image.Metadata.Xmp = new byte[] { 3 };
        image.UnknownChunks.Add(new WebpUnknownChunk("zzzz", new byte[] { 4 }));

        List<WebpFrame> frames = NewFrames();
        int index = 0;
        while (index < frames.Count)
        {
            image.Frames.Add(frames[index]);
            index = index + 1;
        }

        return image;
    }

    static List<WebpFrame> NewFrames()
    {
        List<WebpFrame> frames = new List<WebpFrame>();
        frames.Add(NewFrame(10, 1));
        frames.Add(NewFrame(20, 2));
        frames.Add(NewFrame(30, 3));
        return frames;
    }

    static WebpFrame NewFrame(int duration, byte pixelValue)
    {
        WebpFrame frame = new WebpFrame();
        frame.Width = 1;
        frame.Height = 1;
        frame.DurationMilliseconds = duration;
        frame.BlendMode = WebpFrameBlendMode.Replace;
        frame.DisposeMode = WebpFrameDisposeMode.DisposeToBackground;
        frame.BitstreamKind = WebpBitstreamKind.Lossy;
        frame.Pixels = WebpPixelBufferHelper.AllocateRgba(1, 1);
        frame.Pixels.Rgba[0] = pixelValue;
        frame.Pixels.Rgba[1] = pixelValue;
        frame.Pixels.Rgba[2] = pixelValue;
        frame.Pixels.Rgba[3] = 255;
        frame.EncodedBitstream = new byte[] { 5, pixelValue };
        frame.AlphaData = new byte[] { 9, pixelValue };
        frame.UnknownChunks.Add(new WebpUnknownChunk("frmx", new byte[] { 7, pixelValue }));
        return frame;
    }
}
