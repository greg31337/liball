using webp;

namespace webp.Tests;

[TestFixture]
public class WebpAlphaHelperTests
{
    [Test]
    public void HasNonOpaqueAlpha_DetectsAnyAlphaBelowOpaque()
    {
        byte[] opaque = new byte[]
        {
            1, 2, 3, 255, 4, 5, 6, 255,
            7, 8, 9, 255, 10, 11, 12, 255
        };
        byte[] partial = new byte[]
        {
            1, 2, 3, 255, 4, 5, 6, 128,
            7, 8, 9, 255, 10, 11, 12, 255
        };

        Assert.That(WebpAlphaHelper.HasNonOpaqueAlpha(opaque, 2, 2, 8), Is.False);
        Assert.That(WebpAlphaHelper.HasNonOpaqueAlpha(partial, 2, 2, 8), Is.True);
    }

    [Test]
    public void HasTransparentPixels_DetectsOnlyZeroAlpha()
    {
        byte[] partial = new byte[] { 1, 2, 3, 128 };
        byte[] transparent = new byte[] { 1, 2, 3, 0 };

        Assert.That(WebpAlphaHelper.HasTransparentPixels(partial, 1, 1, 4), Is.False);
        Assert.That(WebpAlphaHelper.HasTransparentPixels(transparent, 1, 1, 4), Is.True);
    }

    [Test]
    public void HasTransparentRgb_DetectsColorInFullyTransparentPixels()
    {
        byte[] transparentBlack = new byte[] { 0, 0, 0, 0 };
        byte[] transparentColor = new byte[] { 1, 0, 0, 0 };
        byte[] semiTransparentColor = new byte[] { 1, 2, 3, 1 };

        Assert.That(WebpAlphaHelper.HasTransparentRgb(transparentBlack, 1, 1, 4), Is.False);
        Assert.That(WebpAlphaHelper.HasTransparentRgb(transparentColor, 1, 1, 4), Is.True);
        Assert.That(WebpAlphaHelper.HasTransparentRgb(semiTransparentColor, 1, 1, 4), Is.False);
    }

    [Test]
    public void Scanners_HandlePaddedStrides()
    {
        byte[] rgba = new byte[]
        {
            1, 2, 3, 255, 4, 5, 6, 255, 99, 99, 99, 99,
            7, 8, 9, 0, 10, 11, 12, 255, 88, 88, 88, 88
        };

        Assert.That(WebpAlphaHelper.HasNonOpaqueAlpha(rgba, 2, 2, 12), Is.True);
        Assert.That(WebpAlphaHelper.HasTransparentPixels(rgba, 2, 2, 12), Is.True);
        Assert.That(WebpAlphaHelper.HasTransparentRgb(rgba, 2, 2, 12), Is.True);
    }

    [Test]
    public void ClearTransparentRgb_ZeroesRgbForOnlyFullyTransparentPixels()
    {
        byte[] rgba = new byte[]
        {
            1, 2, 3, 0, 4, 5, 6, 128, 99, 99, 99, 99,
            7, 8, 9, 255, 10, 11, 12, 0, 88, 88, 88, 88
        };

        WebpAlphaHelper.ClearTransparentRgb(rgba, 2, 2, 12);

        Assert.That(rgba, Is.EqualTo(new byte[]
        {
            0, 0, 0, 0, 4, 5, 6, 128, 99, 99, 99, 99,
            7, 8, 9, 255, 0, 0, 0, 0, 88, 88, 88, 88
        }));
    }

    [Test]
    public void PreserveTransparentRgb_CopiesRgbOnlyForStillTransparentPixels()
    {
        byte[] source = new byte[]
        {
            1, 2, 3, 0, 4, 5, 6, 0, 99, 99, 99, 99,
            7, 8, 9, 255, 10, 11, 12, 0, 88, 88, 88, 88
        };
        byte[] destination = new byte[]
        {
            20, 21, 22, 0, 30, 31, 32, 255, 0, 0, 0, 0,
            40, 41, 42, 0, 50, 51, 52, 0, 0, 0, 0, 0
        };

        WebpAlphaHelper.PreserveTransparentRgb(source, 2, 2, 12, destination, 12);

        Assert.That(destination, Is.EqualTo(new byte[]
        {
            1, 2, 3, 0, 30, 31, 32, 255, 0, 0, 0, 0,
            40, 41, 42, 0, 10, 11, 12, 0, 0, 0, 0, 0
        }));
    }

    [Test]
    public void PreserveTransparentRgb_WorksWithPixelBufferModels()
    {
        WebpPixelBuffer source = WebpPixelBufferHelper.AllocateRgba(1, 1);
        source.Rgba[0] = 1;
        source.Rgba[1] = 2;
        source.Rgba[2] = 3;
        source.Rgba[3] = 0;
        WebpPixelBuffer destination = WebpPixelBufferHelper.AllocateRgba(1, 1);
        destination.Rgba[3] = 0;

        WebpAlphaHelper.PreserveTransparentRgb(source, destination);

        Assert.That(destination.Rgba, Is.EqualTo(new byte[] { 1, 2, 3, 0 }));
    }

    [Test]
    public void PreserveTransparentRgb_RejectsMismatchedPixelBufferDimensions()
    {
        WebpPixelBuffer source = WebpPixelBufferHelper.AllocateRgba(1, 1);
        WebpPixelBuffer destination = WebpPixelBufferHelper.AllocateRgba(2, 1);

        Assert.Throws<ArgumentException>(() => WebpAlphaHelper.PreserveTransparentRgb(source, destination));
    }

    [Test]
    public void Helpers_RejectInvalidBuffers()
    {
        Assert.Throws<ArgumentException>(() => WebpAlphaHelper.HasNonOpaqueAlpha(new byte[3], 1, 1, 4));
        Assert.Throws<ArgumentException>(() => WebpAlphaHelper.ClearTransparentRgb(new byte[3], 1, 1, 4));
        Assert.Throws<ArgumentException>(() => WebpAlphaHelper.PreserveTransparentRgb(new byte[4], 1, 1, 4, new byte[3], 4));
    }
}
