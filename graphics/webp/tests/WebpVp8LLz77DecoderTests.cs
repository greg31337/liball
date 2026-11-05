using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8LLz77DecoderTests
{
    [Test]
    public void DecodePrefixValue_ExpandsBaseAndExtraBits()
    {
        Assert.That(WebpVp8LLz77Decoder.DecodePrefixValue(new WebpVp8LBitReader(Array.Empty<byte>()), 0), Is.EqualTo(1));
        Assert.That(WebpVp8LLz77Decoder.DecodePrefixValue(new WebpVp8LBitReader(Array.Empty<byte>()), 3), Is.EqualTo(4));

        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        writer.WriteBits(1, 1);
        writer.WriteBits(1023, 10);
        WebpVp8LBitReader reader = new WebpVp8LBitReader(writer.ToArray());

        Assert.That(WebpVp8LLz77Decoder.DecodePrefixValue(reader, 4), Is.EqualTo(6));
        Assert.That(WebpVp8LLz77Decoder.DecodePrefixValue(reader, 23), Is.EqualTo(4096));
    }

    [Test]
    public void DecodeLength_UsesGreenLengthSymbols()
    {
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        writer.WriteBits(1023, 10);

        Assert.That(WebpVp8LLz77Decoder.DecodeLength(new WebpVp8LBitReader(Array.Empty<byte>()), 256), Is.EqualTo(1));
        Assert.That(WebpVp8LLz77Decoder.DecodeLength(new WebpVp8LBitReader(writer.ToArray()), 279), Is.EqualTo(4096));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LLz77Decoder.DecodeLength(new WebpVp8LBitReader(Array.Empty<byte>()), 255));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LLz77Decoder.DecodeLength(new WebpVp8LBitReader(Array.Empty<byte>()), 280));
    }

    [Test]
    public void MapDistanceCode_ConvertsSpecialAndLinearCodes()
    {
        Assert.That(WebpVp8LLz77Decoder.MapDistanceCode(1, 10), Is.EqualTo(10));
        Assert.That(WebpVp8LLz77Decoder.MapDistanceCode(2, 10), Is.EqualTo(1));
        Assert.That(WebpVp8LLz77Decoder.MapDistanceCode(3, 10), Is.EqualTo(11));
        Assert.That(WebpVp8LLz77Decoder.MapDistanceCode(4, 10), Is.EqualTo(9));
        Assert.That(WebpVp8LLz77Decoder.MapDistanceCode(97, 10), Is.EqualTo(8));
        Assert.That(WebpVp8LLz77Decoder.MapDistanceCode(120, 10), Is.EqualTo(78));
        Assert.That(WebpVp8LLz77Decoder.MapDistanceCode(121, 10), Is.EqualTo(1));
        Assert.That(WebpVp8LLz77Decoder.MapDistanceCode(130, 10), Is.EqualTo(10));
    }

    [Test]
    public void DecodeReference_ReadsLengthAndDistanceCode()
    {
        WebpVp8LPrefixCodeGroup group = new WebpVp8LPrefixCodeGroup();
        group.Distance = NewSingleSymbolTable(40, 0);
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        writer.WriteBits(1, 1);

        WebpVp8LLz77Reference reference = WebpVp8LLz77Decoder.DecodeReference(new WebpVp8LBitReader(writer.ToArray()), group, 260, 10);

        Assert.That(reference.Length, Is.EqualTo(6));
        Assert.That(reference.DistanceCode, Is.EqualTo(1));
        Assert.That(reference.Distance, Is.EqualTo(10));
    }

    [Test]
    public void CopyPixels_CopiesBackwardReferencesAndSupportsOverlap()
    {
        WebpVp8LColor[] direct = NewPixels(new byte[] { 1, 2, 3, 0, 0, 0 });
        WebpVp8LLz77Decoder.CopyPixels(direct, 3, 3, 3);

        Assert.That(direct[3].Green, Is.EqualTo(1));
        Assert.That(direct[4].Green, Is.EqualTo(2));
        Assert.That(direct[5].Green, Is.EqualTo(3));
        Assert.That(direct[3], Is.Not.SameAs(direct[0]));

        WebpVp8LColor[] overlap = NewPixels(new byte[] { 7, 9, 0, 0, 0, 0 });
        WebpVp8LLz77Decoder.CopyPixels(overlap, 2, 4, 1);

        Assert.That(overlap[2].Green, Is.EqualTo(9));
        Assert.That(overlap[3].Green, Is.EqualTo(9));
        Assert.That(overlap[4].Green, Is.EqualTo(9));
        Assert.That(overlap[5].Green, Is.EqualTo(9));
    }

    [Test]
    public void CopyPixels_RejectsInvalidRanges()
    {
        WebpVp8LColor[] pixels = NewPixels(new byte[] { 1, 2, 3 });

        Assert.Throws<ArgumentException>(() => WebpVp8LLz77Decoder.CopyPixels(pixels, 1, 1, 2));
        Assert.Throws<ArgumentException>(() => WebpVp8LLz77Decoder.CopyPixels(pixels, 2, 2, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LLz77Decoder.CopyPixels(pixels, 1, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LLz77Decoder.MapDistanceCode(0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LLz77Decoder.MapDistanceCode(1, 0));
    }

    static WebpVp8LPrefixCodeTable NewSingleSymbolTable(int alphabetSize, int symbol)
    {
        int[] lengths = new int[alphabetSize];
        lengths[symbol] = 1;
        return WebpVp8LPrefixCodeParser.BuildTable(lengths);
    }

    static WebpVp8LColor[] NewPixels(byte[] greenValues)
    {
        WebpVp8LColor[] pixels = new WebpVp8LColor[greenValues.Length];
        int index = 0;
        while (index < greenValues.Length)
        {
            WebpVp8LColor color = new WebpVp8LColor();
            color.Red = 1;
            color.Green = greenValues[index];
            color.Blue = 2;
            color.Alpha = 255;
            pixels[index] = color;
            index = index + 1;
        }

        return pixels;
    }
}
