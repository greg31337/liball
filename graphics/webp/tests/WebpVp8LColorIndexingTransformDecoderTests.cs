using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8LColorIndexingTransformDecoderTests
{
    static WebpPixelBuffer ColorTableImageForTest = new WebpPixelBuffer();
    static int ObservedColorTableWidth;
    static int ObservedColorTableHeight;

    [Test]
    public void Parse_ReadsColorTableSizeAndDecodesDeltaPalette()
    {
        ColorTableImageForTest = NewColorTableImage(new WebpVp8LColor[]
        {
            NewColor(10, 20, 30, 40),
            NewColor(1, 2, 3, 4),
            NewColor(250, 250, 250, 250)
        });
        ObservedColorTableWidth = 0;
        ObservedColorTableHeight = 0;
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        writer.WriteBits(2, 8);

        WebpVp8LColorIndexingTransformParseResult result = WebpVp8LColorIndexingTransformDecoder.Parse(new WebpVp8LBitReader(writer.ToArray()), 9, 2, DecodeColorTableImageForTest);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Transform.ColorTableSize, Is.EqualTo(3));
        Assert.That(result.Transform.WidthBits, Is.EqualTo(2));
        Assert.That(result.Transform.PackedWidth, Is.EqualTo(3));
        Assert.That(result.Transform.Height, Is.EqualTo(2));
        Assert.That(result.Transform.ColorTable[0].ToArgb(), Is.EqualTo(NewColor(10, 20, 30, 40).ToArgb()));
        Assert.That(result.Transform.ColorTable[1].ToArgb(), Is.EqualTo(NewColor(11, 22, 33, 44).ToArgb()));
        Assert.That(result.Transform.ColorTable[2].ToArgb(), Is.EqualTo(NewColor(5, 16, 27, 38).ToArgb()));
        Assert.That(ObservedColorTableWidth, Is.EqualTo(3));
        Assert.That(ObservedColorTableHeight, Is.EqualTo(1));
    }

    [Test]
    public void ComputeWidthBits_FollowsColorTableThresholds()
    {
        Assert.That(WebpVp8LColorIndexingTransformDecoder.ComputeWidthBits(2), Is.EqualTo(3));
        Assert.That(WebpVp8LColorIndexingTransformDecoder.ComputeWidthBits(4), Is.EqualTo(2));
        Assert.That(WebpVp8LColorIndexingTransformDecoder.ComputeWidthBits(16), Is.EqualTo(1));
        Assert.That(WebpVp8LColorIndexingTransformDecoder.ComputeWidthBits(17), Is.EqualTo(0));
    }

    [Test]
    public void ExtractPackedIndex_ReadsLowBitsFirstForEachPackingMode()
    {
        Assert.That(WebpVp8LColorIndexingTransformDecoder.ExtractPackedIndex(0xAB, 0, 1), Is.EqualTo(11));
        Assert.That(WebpVp8LColorIndexingTransformDecoder.ExtractPackedIndex(0xAB, 1, 1), Is.EqualTo(10));
        Assert.That(WebpVp8LColorIndexingTransformDecoder.ExtractPackedIndex(0xE4, 0, 2), Is.EqualTo(0));
        Assert.That(WebpVp8LColorIndexingTransformDecoder.ExtractPackedIndex(0xE4, 1, 2), Is.EqualTo(1));
        Assert.That(WebpVp8LColorIndexingTransformDecoder.ExtractPackedIndex(0xE4, 2, 2), Is.EqualTo(2));
        Assert.That(WebpVp8LColorIndexingTransformDecoder.ExtractPackedIndex(0xE4, 3, 2), Is.EqualTo(3));
        Assert.That(WebpVp8LColorIndexingTransformDecoder.ExtractPackedIndex(0xB4, 2, 3), Is.EqualTo(1));
        Assert.That(WebpVp8LColorIndexingTransformDecoder.ExtractPackedIndex(0xB4, 3, 3), Is.EqualTo(0));
    }

    [Test]
    public void ApplyInverse_ReplacesUnpackedGreenIndicesWithPaletteColors()
    {
        WebpVp8LColorIndexingTransform transform = NewTransform(17, 0, 3, 1);
        WebpVp8LColor[] transformed = new WebpVp8LColor[]
        {
            NewColor(0, 0, 0, 255),
            NewColor(0, 16, 0, 255),
            NewColor(0, 17, 0, 255)
        };

        WebpVp8LColor[] output = WebpVp8LColorIndexingTransformDecoder.ApplyInverse(transformed, 3, 1, transform);

        Assert.That(output[0].ToArgb(), Is.EqualTo(transform.ColorTable[0].ToArgb()));
        Assert.That(output[1].ToArgb(), Is.EqualTo(transform.ColorTable[16].ToArgb()));
        Assert.That(output[2].ToArgb(), Is.EqualTo(0U));
    }

    [Test]
    public void ApplyInverse_ExpandsPackedGreenIndices()
    {
        WebpVp8LColorIndexingTransform transform = NewTransform(3, 2, 2, 1);
        WebpVp8LColor[] transformed = new WebpVp8LColor[]
        {
            NewColor(0, 0xE4, 0, 255),
            NewColor(0, 0x01, 0, 255)
        };

        WebpVp8LColor[] output = WebpVp8LColorIndexingTransformDecoder.ApplyInverse(transformed, 5, 1, transform);

        Assert.That(output[0].ToArgb(), Is.EqualTo(transform.ColorTable[0].ToArgb()));
        Assert.That(output[1].ToArgb(), Is.EqualTo(transform.ColorTable[1].ToArgb()));
        Assert.That(output[2].ToArgb(), Is.EqualTo(transform.ColorTable[2].ToArgb()));
        Assert.That(output[3].ToArgb(), Is.EqualTo(0U));
        Assert.That(output[4].ToArgb(), Is.EqualTo(transform.ColorTable[1].ToArgb()));
    }

    [Test]
    public void Parse_RejectsWrongColorTableImageDimensions()
    {
        ColorTableImageForTest = WebpPixelBufferHelper.AllocateRgba(2, 1);
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        writer.WriteBits(2, 8);

        Assert.Throws<ArgumentException>(() => WebpVp8LColorIndexingTransformDecoder.Parse(new WebpVp8LBitReader(writer.ToArray()), 4, 1, DecodeColorTableImageForTest));
    }

    [Test]
    public void ApplyInverse_RejectsInvalidPixelCountAndTransform()
    {
        WebpVp8LColorIndexingTransform transform = NewTransform(2, 3, 1, 1);

        Assert.Throws<ArgumentException>(() => WebpVp8LColorIndexingTransformDecoder.ApplyInverse(new WebpVp8LColor[2], 1, 1, transform));
        transform.ColorTable = Array.Empty<WebpVp8LColor>();
        Assert.Throws<ArgumentException>(() => WebpVp8LColorIndexingTransformDecoder.ApplyInverse(new WebpVp8LColor[] { NewColor(0, 0, 0, 0) }, 1, 1, transform));
    }

    static WebpVp8LColorIndexingTransform NewTransform(int colorTableSize, int widthBits, int packedWidth, int height)
    {
        WebpVp8LColorIndexingTransform transform = new WebpVp8LColorIndexingTransform();
        transform.ColorTableSize = colorTableSize;
        transform.WidthBits = widthBits;
        transform.PackedWidth = packedWidth;
        transform.Height = height;
        transform.ColorTable = new WebpVp8LColor[colorTableSize];
        int index = 0;
        while (index < colorTableSize)
        {
            transform.ColorTable[index] = NewColor((byte)index, (byte)(index + 1), (byte)(index + 2), (byte)(255 - index));
            index = index + 1;
        }

        return transform;
    }

    static WebpPixelBuffer NewColorTableImage(WebpVp8LColor[] colors)
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(colors.Length, 1);
        int index = 0;
        while (index < colors.Length)
        {
            int pixel = index * 4;
            buffer.Rgba[pixel] = colors[index].Red;
            buffer.Rgba[pixel + 1] = colors[index].Green;
            buffer.Rgba[pixel + 2] = colors[index].Blue;
            buffer.Rgba[pixel + 3] = colors[index].Alpha;
            index = index + 1;
        }

        return buffer;
    }

    static WebpPixelBuffer DecodeColorTableImageForTest(WebpVp8LBitReader reader, int width, int height)
    {
        ObservedColorTableWidth = width;
        ObservedColorTableHeight = height;
        return WebpPixelBufferHelper.Copy(ColorTableImageForTest);
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
