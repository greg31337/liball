using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8LLiteralDecoderTests
{
    [Test]
    public void Decode_ReturnsArgbLiteralFromSingleSymbolTables()
    {
        WebpVp8LPrefixCodeGroup group = new WebpVp8LPrefixCodeGroup();
        group.Green = NewSingleSymbolTable(256, 34);
        group.Red = NewSingleSymbolTable(256, 12);
        group.Blue = NewSingleSymbolTable(256, 56);
        group.Alpha = NewSingleSymbolTable(256, 78);

        WebpVp8LLiteralDecodeResult result = WebpVp8LLiteralDecoder.Decode(new WebpVp8LBitReader(Array.Empty<byte>()), group);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.IsLiteral, Is.True);
        Assert.That(result.GreenSymbol, Is.EqualTo(34));
        Assert.That(result.Color.Red, Is.EqualTo(12));
        Assert.That(result.Color.Green, Is.EqualTo(34));
        Assert.That(result.Color.Blue, Is.EqualTo(56));
        Assert.That(result.Color.Alpha, Is.EqualTo(78));
        Assert.That(result.Color.ToArgb(), Is.EqualTo(0x4E0C2238U));
    }

    [Test]
    public void Decode_ConsumesGreenRedBlueAlphaSymbolsInOrder()
    {
        WebpVp8LPrefixCodeGroup group = new WebpVp8LPrefixCodeGroup();
        group.Green = WebpVp8LPrefixCodeParser.BuildTable(new int[] { 1, 1 });
        group.Red = WebpVp8LPrefixCodeParser.BuildTable(new int[] { 1, 1 });
        group.Blue = WebpVp8LPrefixCodeParser.BuildTable(new int[] { 1, 1 });
        group.Alpha = WebpVp8LPrefixCodeParser.BuildTable(new int[] { 1, 1 });
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        writer.WriteBits(1, 1);
        writer.WriteBits(0, 1);
        writer.WriteBits(1, 1);
        writer.WriteBits(0, 1);
        WebpVp8LBitReader reader = new WebpVp8LBitReader(writer.ToArray());

        WebpVp8LLiteralDecodeResult result = WebpVp8LLiteralDecoder.Decode(reader, group);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Color.Green, Is.EqualTo(1));
        Assert.That(result.Color.Red, Is.EqualTo(0));
        Assert.That(result.Color.Blue, Is.EqualTo(1));
        Assert.That(result.Color.Alpha, Is.EqualTo(0));
        Assert.That(reader.BitPosition(), Is.EqualTo(4));
    }

    [Test]
    public void Decode_ReportsNonLiteralGreenSymbol()
    {
        WebpVp8LPrefixCodeGroup group = new WebpVp8LPrefixCodeGroup();
        group.Green = NewSingleSymbolTable(281, 256);
        group.Red = NewSingleSymbolTable(256, 1);
        group.Blue = NewSingleSymbolTable(256, 2);
        group.Alpha = NewSingleSymbolTable(256, 3);

        WebpVp8LLiteralDecodeResult result = WebpVp8LLiteralDecoder.Decode(new WebpVp8LBitReader(Array.Empty<byte>()), group);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.IsLiteral, Is.False);
        Assert.That(result.GreenSymbol, Is.EqualTo(256));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("not a literal"));
    }

    [Test]
    public void Decode_ReportsInvalidChannelSymbols()
    {
        WebpVp8LPrefixCodeGroup group = new WebpVp8LPrefixCodeGroup();
        group.Green = NewSingleSymbolTable(256, 1);
        group.Red = NewSingleSymbolTable(257, 256);
        group.Blue = NewSingleSymbolTable(256, 2);
        group.Alpha = NewSingleSymbolTable(256, 3);

        WebpVp8LLiteralDecodeResult result = WebpVp8LLiteralDecoder.Decode(new WebpVp8LBitReader(Array.Empty<byte>()), group);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.IsLiteral, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("red"));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("byte range"));
    }

    [Test]
    public void Decode_RejectsNullInputs()
    {
        Assert.Throws<ArgumentNullException>(() => WebpVp8LLiteralDecoder.Decode(null!, new WebpVp8LPrefixCodeGroup()));
        Assert.Throws<ArgumentNullException>(() => WebpVp8LLiteralDecoder.Decode(new WebpVp8LBitReader(Array.Empty<byte>()), null!));
    }

    static WebpVp8LPrefixCodeTable NewSingleSymbolTable(int alphabetSize, int symbol)
    {
        int[] lengths = new int[alphabetSize];
        lengths[symbol] = 1;
        return WebpVp8LPrefixCodeParser.BuildTable(lengths);
    }
}
