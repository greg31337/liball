using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8LPredictorTransformDecoderTests
{
    static WebpPixelBuffer PredictorImageForTest = new WebpPixelBuffer();
    static int ObservedPredictorWidth;
    static int ObservedPredictorHeight;

    [Test]
    public void Parse_ReadsSizeBitsAndPredictorModes()
    {
        PredictorImageForTest = NewPredictorImage(2, 1, new byte[] { 0, 13 });
        ObservedPredictorWidth = 0;
        ObservedPredictorHeight = 0;
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        writer.WriteBits(0, 3);

        WebpVp8LPredictorTransformParseResult result = WebpVp8LPredictorTransformDecoder.Parse(new WebpVp8LBitReader(writer.ToArray()), 5, 4, DecodePredictorImageForTest);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Transform.SizeBits, Is.EqualTo(2));
        Assert.That(result.Transform.Width, Is.EqualTo(2));
        Assert.That(result.Transform.Height, Is.EqualTo(1));
        Assert.That(result.Transform.Modes, Is.EqualTo(new byte[] { 0, 13 }));
        Assert.That(ObservedPredictorWidth, Is.EqualTo(2));
        Assert.That(ObservedPredictorHeight, Is.EqualTo(1));
    }

    [Test]
    public void Parse_RejectsInvalidPredictorMode()
    {
        PredictorImageForTest = NewPredictorImage(1, 1, new byte[] { 14 });
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        writer.WriteBits(0, 3);

        WebpVp8LPredictorTransformParseResult result = WebpVp8LPredictorTransformDecoder.Parse(new WebpVp8LBitReader(writer.ToArray()), 1, 1, DecodePredictorImageForTest);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("between 0 and 13"));
    }

    [Test]
    public void Predict_ReturnsRepresentativePredictorModes()
    {
        WebpVp8LColor left = NewColor(10, 40, 70, 100);
        WebpVp8LColor top = NewColor(20, 50, 80, 110);
        WebpVp8LColor topRight = NewColor(30, 60, 90, 120);
        WebpVp8LColor topLeft = NewColor(5, 15, 25, 35);

        Assert.That(WebpVp8LPredictorTransformDecoder.Predict(0, left, top, topRight, topLeft).ToArgb(), Is.EqualTo(0xFF000000U));
        Assert.That(WebpVp8LPredictorTransformDecoder.Predict(1, left, top, topRight, topLeft).ToArgb(), Is.EqualTo(left.ToArgb()));
        Assert.That(WebpVp8LPredictorTransformDecoder.Predict(2, left, top, topRight, topLeft).ToArgb(), Is.EqualTo(top.ToArgb()));
        Assert.That(WebpVp8LPredictorTransformDecoder.Predict(3, left, top, topRight, topLeft).ToArgb(), Is.EqualTo(topRight.ToArgb()));
        Assert.That(WebpVp8LPredictorTransformDecoder.Predict(4, left, top, topRight, topLeft).ToArgb(), Is.EqualTo(topLeft.ToArgb()));
        Assert.That(WebpVp8LPredictorTransformDecoder.Predict(7, left, top, topRight, topLeft).ToArgb(), Is.EqualTo(NewColor(15, 45, 75, 105).ToArgb()));
        Assert.That(WebpVp8LPredictorTransformDecoder.Predict(10, left, top, topRight, topLeft).ToArgb(), Is.EqualTo(NewColor(16, 41, 66, 91).ToArgb()));
        Assert.That(WebpVp8LPredictorTransformDecoder.Predict(11, left, top, topRight, topLeft).ToArgb(), Is.EqualTo(top.ToArgb()));
        Assert.That(WebpVp8LPredictorTransformDecoder.Predict(12, left, top, topRight, topLeft).ToArgb(), Is.EqualTo(NewColor(25, 75, 125, 175).ToArgb()));
        Assert.That(WebpVp8LPredictorTransformDecoder.Predict(13, left, top, topRight, topLeft).ToArgb(), Is.EqualTo(NewColor(20, 60, 100, 140).ToArgb()));
    }

    [Test]
    public void Predict_ClampModesClampToByteRange()
    {
        WebpVp8LColor high = NewColor(250, 250, 250, 250);
        WebpVp8LColor low = NewColor(0, 0, 0, 0);

        WebpVp8LColor full = WebpVp8LPredictorTransformDecoder.Predict(12, high, high, high, low);

        Assert.That(full.Red, Is.EqualTo(255));
        Assert.That(full.Green, Is.EqualTo(255));
        Assert.That(full.Blue, Is.EqualTo(255));
        Assert.That(full.Alpha, Is.EqualTo(255));
    }

    [Test]
    public void ApplyInverse_AppliesBorderRulesAndBlockPredictorModes()
    {
        WebpVp8LPredictorTransform transform = new WebpVp8LPredictorTransform();
        transform.SizeBits = 2;
        transform.Width = 1;
        transform.Height = 1;
        transform.Modes = new byte[] { 7 };
        WebpVp8LColor[] residuals = new WebpVp8LColor[]
        {
            NewColor(0, 0, 0, 0),
            NewColor(1, 1, 1, 0),
            NewColor(2, 2, 2, 0),
            NewColor(4, 4, 4, 0),
            NewColor(10, 10, 10, 0),
            NewColor(1, 1, 1, 0)
        };

        WebpVp8LColor[] output = WebpVp8LPredictorTransformDecoder.ApplyInverse(residuals, 3, 2, transform);

        Assert.That(output[0].ToArgb(), Is.EqualTo(0xFF000000U));
        Assert.That(output[1].ToArgb(), Is.EqualTo(NewColor(1, 1, 1, 255).ToArgb()));
        Assert.That(output[2].ToArgb(), Is.EqualTo(NewColor(3, 3, 3, 255).ToArgb()));
        Assert.That(output[3].ToArgb(), Is.EqualTo(NewColor(4, 4, 4, 255).ToArgb()));
        Assert.That(output[4].ToArgb(), Is.EqualTo(NewColor(12, 12, 12, 255).ToArgb()));
        Assert.That(output[5].ToArgb(), Is.EqualTo(NewColor(8, 8, 8, 255).ToArgb()));
    }

    [Test]
    public void ApplyInverse_RejectsInvalidResidualsAndTransform()
    {
        WebpVp8LPredictorTransform transform = new WebpVp8LPredictorTransform();
        transform.SizeBits = 2;
        transform.Width = 1;
        transform.Height = 1;
        transform.Modes = new byte[] { 0 };

        Assert.Throws<ArgumentException>(() => WebpVp8LPredictorTransformDecoder.ApplyInverse(new WebpVp8LColor[1], 2, 1, transform));
        transform.Modes = Array.Empty<byte>();
        Assert.Throws<ArgumentException>(() => WebpVp8LPredictorTransformDecoder.ApplyInverse(new WebpVp8LColor[] { NewColor(0, 0, 0, 0) }, 1, 1, transform));
    }

    static WebpPixelBuffer NewPredictorImage(int width, int height, byte[] modes)
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(width, height);
        int index = 0;
        while (index < modes.Length)
        {
            int pixel = index * 4;
            buffer.Rgba[pixel + 1] = modes[index];
            buffer.Rgba[pixel + 3] = 255;
            index = index + 1;
        }

        return buffer;
    }

    static WebpPixelBuffer DecodePredictorImageForTest(WebpVp8LBitReader reader, int width, int height)
    {
        ObservedPredictorWidth = width;
        ObservedPredictorHeight = height;
        return WebpPixelBufferHelper.Copy(PredictorImageForTest);
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
