using webp;

namespace webp.Tests;

[TestFixture]
public class WebpNearLosslessPreprocessorTests
{
    [Test]
    public void Apply_Quality100CopiesPixelsExactly()
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(1, 1);
        SetPixel(buffer, 0, 0, 13, 21, 250, 128);

        WebpPixelBuffer output = WebpNearLosslessPreprocessor.Apply(buffer, 100, false);

        Assert.That(output, Is.Not.SameAs(buffer));
        Assert.That(output.Rgba, Is.EqualTo(buffer.Rgba));
    }

    [Test]
    public void Apply_QuantizesRgbAndPreservesAlpha()
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(1, 1);
        SetPixel(buffer, 0, 0, 13, 21, 250, 128);

        WebpPixelBuffer output = WebpNearLosslessPreprocessor.Apply(buffer, 50, false);

        Assert.That(WebpNearLosslessPreprocessor.ComputeQuantizationStep(50), Is.EqualTo(8));
        Assert.That(output.Rgba, Is.EqualTo(new byte[] { 16, 24, 248, 128 }));
    }

    [Test]
    public void Apply_ExactTransparentRgbPreservesFullyTransparentRgb()
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(2, 1);
        SetPixel(buffer, 0, 0, 13, 21, 250, 0);
        SetPixel(buffer, 1, 0, 13, 21, 250, 1);

        WebpPixelBuffer output = WebpNearLosslessPreprocessor.Apply(buffer, 50, true);

        Assert.That(output.Rgba, Is.EqualTo(new byte[] { 13, 21, 250, 0, 16, 24, 248, 1 }));
    }

    [Test]
    public void Apply_RejectsInvalidQuality()
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(1, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => WebpNearLosslessPreprocessor.Apply(buffer, -1, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpNearLosslessPreprocessor.Apply(buffer, 101, false));
    }

    static void SetPixel(WebpPixelBuffer buffer, int x, int y, byte red, byte green, byte blue, byte alpha)
    {
        int pixel = y * buffer.Stride + x * 4;
        buffer.Rgba[pixel] = red;
        buffer.Rgba[pixel + 1] = green;
        buffer.Rgba[pixel + 2] = blue;
        buffer.Rgba[pixel + 3] = alpha;
    }
}
