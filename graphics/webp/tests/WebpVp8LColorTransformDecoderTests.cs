using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8LColorTransformDecoderTests
{
    static WebpPixelBuffer ColorTransformImageForTest = new WebpPixelBuffer();
    static int ObservedColorTransformWidth;
    static int ObservedColorTransformHeight;

    [Test]
    public void Parse_ReadsSizeBitsAndColorTransformElements()
    {
        ColorTransformImageForTest = NewColorTransformImage(2, 1, new byte[] { 3, 5 }, new byte[] { 7, 11 }, new byte[] { 13, 17 }, 255);
        ObservedColorTransformWidth = 0;
        ObservedColorTransformHeight = 0;
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        writer.WriteBits(0, 3);

        WebpVp8LColorTransformParseResult result = WebpVp8LColorTransformDecoder.Parse(new WebpVp8LBitReader(writer.ToArray()), 5, 4, DecodeColorTransformImageForTest);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Transform.SizeBits, Is.EqualTo(2));
        Assert.That(result.Transform.Width, Is.EqualTo(2));
        Assert.That(result.Transform.Height, Is.EqualTo(1));
        Assert.That(result.Transform.Elements[0].GreenToRed, Is.EqualTo(3));
        Assert.That(result.Transform.Elements[0].GreenToBlue, Is.EqualTo(7));
        Assert.That(result.Transform.Elements[0].RedToBlue, Is.EqualTo(13));
        Assert.That(result.Transform.Elements[1].GreenToRed, Is.EqualTo(5));
        Assert.That(result.Transform.Elements[1].GreenToBlue, Is.EqualTo(11));
        Assert.That(result.Transform.Elements[1].RedToBlue, Is.EqualTo(17));
        Assert.That(ObservedColorTransformWidth, Is.EqualTo(2));
        Assert.That(ObservedColorTransformHeight, Is.EqualTo(1));
    }

    [Test]
    public void Parse_RejectsNonOpaqueTransformImageAlpha()
    {
        ColorTransformImageForTest = NewColorTransformImage(1, 1, new byte[] { 0 }, new byte[] { 0 }, new byte[] { 0 }, 254);
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        writer.WriteBits(0, 3);

        WebpVp8LColorTransformParseResult result = WebpVp8LColorTransformDecoder.Parse(new WebpVp8LBitReader(writer.ToArray()), 1, 1, DecodeColorTransformImageForTest);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("alpha"));
    }

    [Test]
    public void ColorTransformDelta_UsesSignedEightBitFixedPointValues()
    {
        Assert.That(WebpVp8LColorTransformDecoder.ColorTransformDelta(32, 64), Is.EqualTo(64));
        Assert.That(WebpVp8LColorTransformDecoder.ColorTransformDelta(224, 64), Is.EqualTo(-64));
        Assert.That(WebpVp8LColorTransformDecoder.ColorTransformDelta(32, 192), Is.EqualTo(-64));
        Assert.That(WebpVp8LColorTransformDecoder.ColorTransformDelta(224, 192), Is.EqualTo(64));
    }

    [Test]
    public void ApplyInverseColor_AddsGreenAndUpdatedRedDeltas()
    {
        WebpVp8LColorTransformElement element = NewElement(32, 16, 32);
        WebpVp8LColor transformed = NewColor(10, 64, 20, 200);

        WebpVp8LColor output = WebpVp8LColorTransformDecoder.ApplyInverseColor(transformed, element);

        Assert.That(output.Red, Is.EqualTo(74));
        Assert.That(output.Green, Is.EqualTo(64));
        Assert.That(output.Blue, Is.EqualTo(126));
        Assert.That(output.Alpha, Is.EqualTo(200));
    }

    [Test]
    public void ApplyInverseColor_WrapsRedAndBlueToBytes()
    {
        WebpVp8LColorTransformElement element = NewElement(64, 64, 64);
        WebpVp8LColor transformed = NewColor(250, 64, 250, 255);

        WebpVp8LColor output = WebpVp8LColorTransformDecoder.ApplyInverseColor(transformed, element);

        Assert.That(output.Red, Is.EqualTo(122));
        Assert.That(output.Blue, Is.EqualTo(110));
    }

    [Test]
    public void ApplyInverse_UsesBlockTransformElements()
    {
        WebpVp8LColorTransform transform = new WebpVp8LColorTransform();
        transform.SizeBits = 2;
        transform.Width = 2;
        transform.Height = 1;
        transform.Elements = new WebpVp8LColorTransformElement[]
        {
            NewElement(32, 0, 0),
            NewElement(0, 32, 0)
        };
        WebpVp8LColor[] transformed = new WebpVp8LColor[]
        {
            NewColor(1, 64, 5, 255),
            NewColor(2, 64, 6, 255),
            NewColor(3, 64, 7, 255),
            NewColor(4, 64, 8, 255),
            NewColor(5, 64, 9, 255)
        };

        WebpVp8LColor[] output = WebpVp8LColorTransformDecoder.ApplyInverse(transformed, 5, 1, transform);

        Assert.That(output[0].Red, Is.EqualTo(65));
        Assert.That(output[1].Red, Is.EqualTo(66));
        Assert.That(output[2].Red, Is.EqualTo(67));
        Assert.That(output[3].Red, Is.EqualTo(68));
        Assert.That(output[4].Red, Is.EqualTo(5));
        Assert.That(output[4].Blue, Is.EqualTo(73));
    }

    [Test]
    public void ApplyInverse_RejectsInvalidPixelsAndTransform()
    {
        WebpVp8LColorTransform transform = new WebpVp8LColorTransform();
        transform.SizeBits = 2;
        transform.Width = 1;
        transform.Height = 1;
        transform.Elements = new WebpVp8LColorTransformElement[] { NewElement(0, 0, 0) };

        Assert.Throws<ArgumentException>(() => WebpVp8LColorTransformDecoder.ApplyInverse(new WebpVp8LColor[1], 2, 1, transform));
        transform.Elements = Array.Empty<WebpVp8LColorTransformElement>();
        Assert.Throws<ArgumentException>(() => WebpVp8LColorTransformDecoder.ApplyInverse(new WebpVp8LColor[] { NewColor(0, 0, 0, 0) }, 1, 1, transform));
    }

    static WebpPixelBuffer NewColorTransformImage(int width, int height, byte[] greenToRed, byte[] greenToBlue, byte[] redToBlue, byte alpha)
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(width, height);
        int index = 0;
        while (index < width * height)
        {
            int pixel = index * 4;
            buffer.Rgba[pixel] = redToBlue[index];
            buffer.Rgba[pixel + 1] = greenToBlue[index];
            buffer.Rgba[pixel + 2] = greenToRed[index];
            buffer.Rgba[pixel + 3] = alpha;
            index = index + 1;
        }

        return buffer;
    }

    static WebpPixelBuffer DecodeColorTransformImageForTest(WebpVp8LBitReader reader, int width, int height)
    {
        ObservedColorTransformWidth = width;
        ObservedColorTransformHeight = height;
        return WebpPixelBufferHelper.Copy(ColorTransformImageForTest);
    }

    static WebpVp8LColorTransformElement NewElement(byte greenToRed, byte greenToBlue, byte redToBlue)
    {
        WebpVp8LColorTransformElement element = new WebpVp8LColorTransformElement();
        element.GreenToRed = greenToRed;
        element.GreenToBlue = greenToBlue;
        element.RedToBlue = redToBlue;
        return element;
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
