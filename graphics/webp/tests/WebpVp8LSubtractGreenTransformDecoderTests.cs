using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8LSubtractGreenTransformDecoderTests
{
    [Test]
    public void ApplyInverseColor_AddsGreenToRedAndBlue()
    {
        WebpVp8LColor transformed = NewColor(10, 20, 30, 40);

        WebpVp8LColor output = WebpVp8LSubtractGreenTransformDecoder.ApplyInverseColor(transformed);

        Assert.That(output.Red, Is.EqualTo(30));
        Assert.That(output.Green, Is.EqualTo(20));
        Assert.That(output.Blue, Is.EqualTo(50));
        Assert.That(output.Alpha, Is.EqualTo(40));
    }

    [Test]
    public void ApplyInverseColor_WrapsRedAndBlueToBytes()
    {
        WebpVp8LColor transformed = NewColor(250, 20, 245, 255);

        WebpVp8LColor output = WebpVp8LSubtractGreenTransformDecoder.ApplyInverseColor(transformed);

        Assert.That(output.Red, Is.EqualTo(14));
        Assert.That(output.Blue, Is.EqualTo(9));
    }

    [Test]
    public void ApplyInverse_TransformsEveryPixelInScanlineOrder()
    {
        WebpVp8LColor[] transformed = new WebpVp8LColor[]
        {
            NewColor(1, 2, 3, 4),
            NewColor(10, 20, 30, 40),
            NewColor(250, 20, 245, 255)
        };

        WebpVp8LColor[] output = WebpVp8LSubtractGreenTransformDecoder.ApplyInverse(transformed, 3, 1);

        Assert.That(output[0].ToArgb(), Is.EqualTo(NewColor(3, 2, 5, 4).ToArgb()));
        Assert.That(output[1].ToArgb(), Is.EqualTo(NewColor(30, 20, 50, 40).ToArgb()));
        Assert.That(output[2].ToArgb(), Is.EqualTo(NewColor(14, 20, 9, 255).ToArgb()));
    }

    [Test]
    public void ApplyInverse_ReturnsNewColorsWithoutMutatingInput()
    {
        WebpVp8LColor[] transformed = new WebpVp8LColor[] { NewColor(1, 2, 3, 4) };

        WebpVp8LColor[] output = WebpVp8LSubtractGreenTransformDecoder.ApplyInverse(transformed, 1, 1);

        Assert.That(output[0], Is.Not.SameAs(transformed[0]));
        Assert.That(transformed[0].ToArgb(), Is.EqualTo(NewColor(1, 2, 3, 4).ToArgb()));
    }

    [Test]
    public void ApplyInverse_RejectsInvalidDimensionsAndPixelCount()
    {
        WebpVp8LColor[] transformed = new WebpVp8LColor[] { NewColor(0, 0, 0, 0) };

        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LSubtractGreenTransformDecoder.ApplyInverse(transformed, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LSubtractGreenTransformDecoder.ApplyInverse(transformed, 1, 0));
        Assert.Throws<ArgumentException>(() => WebpVp8LSubtractGreenTransformDecoder.ApplyInverse(transformed, 2, 1));
    }

    static WebpVp8LColor NewColor(byte red, byte green, byte blue, byte alpha)
    {
        WebpVp8LColor color = new WebpVp8LColor();
        color.Red = red;
        color.Green = green;
        color.Blue = blue;
        color.Alpha = alpha;
        return color;
    }
}
