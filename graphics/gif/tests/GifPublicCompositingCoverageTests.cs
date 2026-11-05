using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifPublicCompositingCoverageTests
{
    [Test]
    public void PublicAnimationDecodeAppliesEveryDefinedDisposalMethod()
    {
        IGifDecoder decoder = new GifCodec();

        GifAnimation unspecified = decoder.DecodeAnimationBytes(GifFixtures.AnimatedDisposalMethod0Gif89a(), null);
        GifAnimation doNotDispose = decoder.DecodeAnimationBytes(GifFixtures.AnimatedDisposalMethod1Gif89a(), null);
        GifAnimation restoreBackground = decoder.DecodeAnimationBytes(GifFixtures.AnimatedDisposalMethod2Gif89a(), null);
        GifAnimation restorePrevious = decoder.DecodeAnimationBytes(GifFixtures.AnimatedDisposalMethod3Gif89a(), null);

        AssertDisposalResult(unspecified, GifDisposalMethod.DoNotDispose, new byte[] { 0, 0, 255, 255, 0, 255, 0, 255 });
        AssertDisposalResult(doNotDispose, GifDisposalMethod.DoNotDispose, new byte[] { 0, 0, 255, 255, 0, 255, 0, 255 });
        AssertDisposalResult(restoreBackground, GifDisposalMethod.RestoreToBackground, new byte[] { 0, 0, 0, 255, 0, 255, 0, 255 });
        AssertDisposalResult(restorePrevious, GifDisposalMethod.RestoreToPrevious, new byte[] { 255, 0, 0, 255, 0, 255, 0, 255 });
    }

    [Test]
    public void PublicAnimationDecodeAppliesEveryReservedDisposalFallbackOutcome()
    {
        IGifDecoder decoder = new GifCodec();
        GifAnimation reserved4 = DecodeReserved(decoder, 4, GifDisposalMethod.DoNotDispose);
        GifAnimation reserved5 = DecodeReserved(decoder, 5, GifDisposalMethod.RestoreToBackground);
        GifAnimation reserved6 = DecodeReserved(decoder, 6, GifDisposalMethod.RestoreToPrevious);
        GifAnimation reserved7 = DecodeReserved(decoder, 7, GifDisposalMethod.Reserved7);

        AssertReservedDisposalResult(reserved4, GifDisposalMethod.DoNotDispose, new byte[] { 0, 0, 255, 255, 0, 255, 0, 255 });
        AssertReservedDisposalResult(reserved5, GifDisposalMethod.RestoreToBackground, new byte[] { 0, 0, 0, 255, 0, 255, 0, 255 });
        AssertReservedDisposalResult(reserved6, GifDisposalMethod.RestoreToPrevious, new byte[] { 255, 0, 0, 255, 0, 255, 0, 255 });
        AssertReservedDisposalResult(reserved7, GifDisposalMethod.DoNotDispose, new byte[] { 0, 0, 255, 255, 0, 255, 0, 255 });
    }

    [Test]
    public void PublicAnimationDecodeAppliesLogicalTransparentAndSolidBackgrounds()
    {
        IGifDecoder decoder = new GifCodec();
        byte[] source = GifFixtures.TransparentGif89a();
        GifDecodeOptions logicalOptions = new GifDecodeOptions();
        logicalOptions.BackgroundMode = GifBackgroundMode.LogicalScreen;
        GifDecodeOptions transparentOptions = new GifDecodeOptions();
        transparentOptions.BackgroundMode = GifBackgroundMode.Transparent;
        GifDecodeOptions solidOptions = new GifDecodeOptions();
        solidOptions.BackgroundMode = GifBackgroundMode.SolidColor;
        solidOptions.SolidBackgroundColor = new GifRgbaColor(9, 8, 7, 6);

        GifAnimation logical = decoder.DecodeAnimationBytes(source, logicalOptions);
        GifAnimation transparent = decoder.DecodeAnimationBytes(source, transparentOptions);
        GifAnimation solid = decoder.DecodeAnimationBytes(source, solidOptions);

        Assert.That(logical.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 0, 0, 0, 255 }));
        Assert.That(transparent.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 0, 0, 0, 0 }));
        Assert.That(solid.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[] { 9, 8, 7, 6 }));
    }

    [Test]
    public void PublicCoalesceClipsRawSubRectangleAndReportsWarning()
    {
        IGifDecoder decoder = new GifCodec();
        GifDocument document = CreateDocument(2, 1);
        GifFrame frame = CreateFrame(1, 0, 2, 1, new byte[] { 1, 2 });
        frame.DisposalMethod = GifDisposalMethod.DoNotDispose;
        document.Frames = new GifFrame[] { frame };

        GifAnimation animation = decoder.Coalesce(document, null);

        Assert.That(animation.Width, Is.EqualTo(2));
        Assert.That(animation.Height, Is.EqualTo(1));
        Assert.That(animation.Frames, Has.Length.EqualTo(1));
        Assert.That(animation.Frames[0].Bitmap.RgbaPixels, Is.EqualTo(new byte[]
        {
            0, 0, 0, 255,
            255, 0, 0, 255
        }));
        Assert.That(animation.Frames[0].SourceLeft, Is.EqualTo(1));
        Assert.That(animation.Frames[0].SourceTop, Is.EqualTo(0));
        Assert.That(animation.Frames[0].SourceWidth, Is.EqualTo(2));
        Assert.That(animation.Frames[0].SourceHeight, Is.EqualTo(1));
        Assert.That(animation.Warnings, Has.Length.EqualTo(1));
        Assert.That(animation.Warnings[0].Code, Is.EqualTo(GifWarningCode.FrameClippedToLogicalScreen));
        Assert.That(animation.Warnings[0].Position.FrameIndex, Is.EqualTo(0));
    }

    [Test]
    public void PublicCoalescePreservesRawRectangleWhileReturningFullCanvasBitmap()
    {
        IGifDecoder decoder = new GifCodec();
        GifDocument document = CreateDocument(3, 2);
        GifFrame frame = CreateFrame(1, 1, 1, 1, new byte[] { 3 });
        frame.DisposalMethod = GifDisposalMethod.DoNotDispose;
        document.Frames = new GifFrame[] { frame };

        GifAnimation animation = decoder.Coalesce(document, null);
        GifAnimationFrame output = animation.Frames[0];

        Assert.That(output.SourceLeft, Is.EqualTo(1));
        Assert.That(output.SourceTop, Is.EqualTo(1));
        Assert.That(output.SourceWidth, Is.EqualTo(1));
        Assert.That(output.SourceHeight, Is.EqualTo(1));
        Assert.That(output.Bitmap.Width, Is.EqualTo(3));
        Assert.That(output.Bitmap.Height, Is.EqualTo(2));
        Assert.That(output.Bitmap.Stride, Is.EqualTo(12));
        Assert.That(output.Bitmap.RgbaPixels, Is.EqualTo(new byte[]
        {
            0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255,
            0, 0, 0, 255, 0, 0, 255, 255, 0, 0, 0, 255
        }));
        Assert.That(animation.Warnings, Is.Empty);
    }

    [Test]
    public void PublicCoalesceNormalizesOnlyZeroDelaysAndAccumulatesDuration()
    {
        IGifDecoder decoder = new GifCodec();
        GifDocument document = CreateDocument(1, 1);
        GifFrame first = CreateFrame(0, 0, 1, 1, new byte[] { 1 });
        first.DelayHundredths = 0;
        GifFrame second = CreateFrame(0, 0, 1, 1, new byte[] { 2 });
        second.DelayHundredths = 2;
        GifFrame third = CreateFrame(0, 0, 1, 1, new byte[] { 3 });
        third.DelayHundredths = 3;
        document.Frames = new GifFrame[] { first, second, third };
        GifDecodeOptions options = new GifDecodeOptions();
        options.MinimumFrameDelayMilliseconds = 25;

        GifAnimation animation = decoder.Coalesce(document, options);

        Assert.That(animation.Frames[0].DelayMilliseconds, Is.EqualTo(25));
        Assert.That(animation.Frames[1].DelayMilliseconds, Is.EqualTo(20));
        Assert.That(animation.Frames[2].DelayMilliseconds, Is.EqualTo(30));
        Assert.That(animation.TotalDurationMilliseconds, Is.EqualTo(75));
        Assert.That(animation.Warnings, Has.Length.EqualTo(1));
        Assert.That(animation.Warnings[0].Code, Is.EqualTo(GifWarningCode.ZeroDelayNormalized));
        Assert.That(animation.Warnings[0].Position.FrameIndex, Is.EqualTo(0));
    }

    static GifAnimation DecodeReserved(IGifDecoder decoder, int reservedValue, GifDisposalMethod fallback)
    {
        GifDecodeOptions options = new GifDecodeOptions();
        options.DisposalFallback = fallback;
        return decoder.DecodeAnimationBytes(CreateReservedDisposalGif(reservedValue), options);
    }

    static byte[] CreateReservedDisposalGif(int disposalValue)
    {
        byte[] data = GifFixtures.AnimatedDisposalMethod0Gif89a();
        int graphicControlIndex = 0;
        int index = 0;
        while (index + 3 < data.Length)
        {
            if (data[index] == 0x21 && data[index + 1] == 0xF9 && data[index + 2] == 4)
            {
                if (graphicControlIndex == 1)
                {
                    data[index + 3] = (byte)(disposalValue * 4);
                    return data;
                }

                graphicControlIndex = graphicControlIndex + 1;
            }

            index = index + 1;
        }

        return data;
    }

    static void AssertDisposalResult(GifAnimation animation, GifDisposalMethod effectiveDisposal, byte[] expectedLastFrame)
    {
        Assert.That(animation.Frames, Has.Length.EqualTo(3));
        Assert.That(animation.Frames[1].DisposalMethod, Is.EqualTo(effectiveDisposal));
        Assert.That(animation.Frames[2].Bitmap.RgbaPixels, Is.EqualTo(expectedLastFrame));
        Assert.That(animation.TotalDurationMilliseconds, Is.EqualTo(30));
    }

    static void AssertReservedDisposalResult(GifAnimation animation, GifDisposalMethod effectiveDisposal, byte[] expectedLastFrame)
    {
        AssertDisposalResult(animation, effectiveDisposal, expectedLastFrame);
        Assert.That(animation.Warnings, Has.Length.EqualTo(1));
        Assert.That(animation.Warnings[0].Code, Is.EqualTo(GifWarningCode.ReservedDisposalMethod));
        Assert.That(animation.Warnings[0].Position.FrameIndex, Is.EqualTo(1));
    }

    static GifDocument CreateDocument(int width, int height)
    {
        GifDocument document = new GifDocument();
        document.LogicalScreen = new GifLogicalScreen(width, height);
        document.LogicalScreen.HasGlobalColorTable = true;
        document.LogicalScreen.GlobalColorTableSize = 4;
        document.LogicalScreen.BackgroundColorIndex = 0;
        document.GlobalColorTable = new GifPalette(new GifColor[]
        {
            new GifColor(0, 0, 0),
            new GifColor(255, 0, 0),
            new GifColor(0, 255, 0),
            new GifColor(0, 0, 255)
        });
        return document;
    }

    static GifFrame CreateFrame(int left, int top, int width, int height, byte[] indexedPixels)
    {
        GifFrame frame = new GifFrame(left, top, width, height);
        frame.IndexedPixels = indexedPixels;
        frame.DisposalMethod = GifDisposalMethod.DoNotDispose;
        return frame;
    }
}
