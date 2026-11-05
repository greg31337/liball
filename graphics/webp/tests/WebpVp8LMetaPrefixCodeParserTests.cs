using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8LMetaPrefixCodeParserTests
{
    static WebpPixelBuffer EntropyImageForTest = new WebpPixelBuffer();
    static int ObservedEntropyWidth;
    static int ObservedEntropyHeight;

    [Test]
    public void Parse_ZeroMetaPrefixFlagReadsOnePrefixCodeGroup()
    {
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        writer.WriteBits(0, 1);
        WritePrefixCodeGroup(writer, 0);

        WebpVp8LMetaPrefixParseResult result = WebpVp8LMetaPrefixCodeParser.Parse(new WebpVp8LBitReader(writer.ToArray()), 8, 8, 0);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HasEntropyImage, Is.False);
        Assert.That(result.EntropyImage.NumPrefixGroups, Is.EqualTo(1));
        Assert.That(result.PrefixCodeGroups, Has.Count.EqualTo(1));
        Assert.That(result.PrefixCodeGroups[0].Green.DecodeSymbol(new WebpVp8LBitReader(Array.Empty<byte>())), Is.EqualTo(0));
        Assert.That(result.PrefixCodeGroups[0].Distance.DecodeSymbol(new WebpVp8LBitReader(Array.Empty<byte>())), Is.EqualTo(0));
    }

    [Test]
    public void Parse_EntropyImageDerivesDimensionsAndPrefixGroupCount()
    {
        EntropyImageForTest = NewEntropyImage(3, 3, 2);
        ObservedEntropyWidth = 0;
        ObservedEntropyHeight = 0;
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        writer.WriteBits(1, 1);
        writer.WriteBits(0, 3);
        WritePrefixCodeGroup(writer, 0);
        WritePrefixCodeGroup(writer, 1);
        WritePrefixCodeGroup(writer, 2);

        WebpVp8LMetaPrefixParseResult result = WebpVp8LMetaPrefixCodeParser.Parse(new WebpVp8LBitReader(writer.ToArray()), 10, 9, 0, DecodeEntropyImageForTest);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HasEntropyImage, Is.True);
        Assert.That(result.EntropyImage.PrefixBits, Is.EqualTo(2));
        Assert.That(result.EntropyImage.Width, Is.EqualTo(3));
        Assert.That(result.EntropyImage.Height, Is.EqualTo(3));
        Assert.That(result.EntropyImage.NumPrefixGroups, Is.EqualTo(3));
        Assert.That(result.EntropyImage.MetaPrefixCodes[8], Is.EqualTo(2));
        Assert.That(result.PrefixCodeGroups, Has.Count.EqualTo(3));
        Assert.That(result.PrefixCodeGroups[2].Green.DecodeSymbol(new WebpVp8LBitReader(Array.Empty<byte>())), Is.EqualTo(2));
        Assert.That(ObservedEntropyWidth, Is.EqualTo(3));
        Assert.That(ObservedEntropyHeight, Is.EqualTo(3));
    }

    [Test]
    public void Parse_EntropyImageCombinesRedAndGreenIntoMetaPrefixCode()
    {
        EntropyImageForTest = NewEntropyImage(1, 1, 0);
        EntropyImageForTest.Rgba[0] = 1;
        EntropyImageForTest.Rgba[1] = 2;
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        writer.WriteBits(1, 1);
        writer.WriteBits(0, 3);
        int group = 0;
        while (group < 259)
        {
            WritePrefixCodeGroup(writer, 0);
            group = group + 1;
        }

        WebpVp8LMetaPrefixParseResult result = WebpVp8LMetaPrefixCodeParser.Parse(new WebpVp8LBitReader(writer.ToArray()), 1, 1, 0, DecodeEntropyImageForTest);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.EntropyImage.MetaPrefixCodes[0], Is.EqualTo(258));
        Assert.That(result.EntropyImage.NumPrefixGroups, Is.EqualTo(259));
        Assert.That(result.PrefixCodeGroups, Has.Count.EqualTo(259));
    }

    [Test]
    public void Parse_ReportsMissingEntropyImageDecoder()
    {
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        writer.WriteBits(1, 1);
        writer.WriteBits(0, 3);

        WebpVp8LMetaPrefixParseResult result = WebpVp8LMetaPrefixCodeParser.Parse(new WebpVp8LBitReader(writer.ToArray()), 4, 4, 0);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("entropy image decoder"));
        Assert.That(result.PrefixCodeGroups, Is.Empty);
    }

    [Test]
    public void Parse_CopiesPrefixCodeDiagnostics()
    {
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        writer.WriteBits(0, 1);
        WriteSimplePrefixCode(writer, 0);
        WriteSimplePrefixCode(writer, 0);
        WriteSimplePrefixCode(writer, 0);
        WriteSimplePrefixCode(writer, 0);
        WriteSimplePrefixCode(writer, 200);

        WebpVp8LMetaPrefixParseResult result = WebpVp8LMetaPrefixCodeParser.Parse(new WebpVp8LBitReader(writer.ToArray()), 4, 4, 0);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("alphabet"));
        Assert.That(result.PrefixCodeGroups, Is.Empty);
    }

    [Test]
    public void Parse_RejectsInvalidDimensionsAndColorCacheSize()
    {
        WebpVp8LBitReader reader = new WebpVp8LBitReader(new byte[] { 0 });

        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LMetaPrefixCodeParser.Parse(reader, 0, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LMetaPrefixCodeParser.Parse(reader, 1, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LMetaPrefixCodeParser.Parse(reader, 1, 1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LMetaPrefixCodeParser.Parse(reader, 1, 1, 4096));
    }

    static void WritePrefixCodeGroup(WebpBitWriter writer, int symbol)
    {
        WriteSimplePrefixCode(writer, symbol);
        WriteSimplePrefixCode(writer, symbol);
        WriteSimplePrefixCode(writer, symbol);
        WriteSimplePrefixCode(writer, symbol);
        WriteSimplePrefixCode(writer, symbol);
    }

    static void WriteSimplePrefixCode(WebpBitWriter writer, int symbol)
    {
        writer.WriteBits(1, 1);
        writer.WriteBits(0, 1);
        if (symbol <= 1)
        {
            writer.WriteBits(0, 1);
            writer.WriteBits((uint)symbol, 1);
        }
        else
        {
            writer.WriteBits(1, 1);
            writer.WriteBits((uint)symbol, 8);
        }
    }

    static WebpPixelBuffer NewEntropyImage(int width, int height, int maxMetaPrefixCode)
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(width, height);
        int index = 0;
        while (index < width * height)
        {
            int pixel = index * 4;
            buffer.Rgba[pixel] = 0;
            buffer.Rgba[pixel + 1] = 0;
            buffer.Rgba[pixel + 2] = 0;
            buffer.Rgba[pixel + 3] = 255;
            index = index + 1;
        }

        int lastPixel = (width * height - 1) * 4;
        buffer.Rgba[lastPixel] = (byte)((maxMetaPrefixCode >> 8) & 255);
        buffer.Rgba[lastPixel + 1] = (byte)(maxMetaPrefixCode & 255);
        return buffer;
    }

    static WebpPixelBuffer DecodeEntropyImageForTest(WebpVp8LBitReader reader, int width, int height)
    {
        ObservedEntropyWidth = width;
        ObservedEntropyHeight = height;
        return WebpPixelBufferHelper.Copy(EntropyImageForTest);
    }
}
