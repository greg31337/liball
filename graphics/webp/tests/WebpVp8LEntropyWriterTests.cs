using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8LEntropyWriterTests
{
    [Test]
    public void BuildCode_GeneratesCompleteCanonicalCodeForUsedSymbols()
    {
        WebpVp8LEntropyCode code = WebpVp8LEntropyWriter.BuildCode(10, new int[] { 2, 4, 7 });

        Assert.That(code.AlphabetSize, Is.EqualTo(10));
        Assert.That(code.Table.Entries, Has.Count.EqualTo(3));
        Assert.That(code.CodeLengths[2], Is.GreaterThan(0));
        Assert.That(code.CodeLengths[4], Is.GreaterThan(0));
        Assert.That(code.CodeLengths[7], Is.GreaterThan(0));
    }

    [Test]
    public void WriteCode_RoundTripsThroughPrefixCodeParser()
    {
        WebpVp8LEntropyCode code = WebpVp8LEntropyWriter.BuildCode(300, new int[] { 5, 20, 257 });
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        WebpVp8LEntropyWriter.WriteCode(writer, code);
        WebpVp8LEntropyWriter.WriteSymbol(writer, code, 5);
        WebpVp8LEntropyWriter.WriteSymbol(writer, code, 20);
        WebpVp8LEntropyWriter.WriteSymbol(writer, code, 257);

        WebpVp8LCodeLengthsParseResult parsed = WebpVp8LPrefixCodeParser.ParseCodeLengths(new WebpVp8LBitReader(writer.ToArray()), 300);

        Assert.That(parsed.IsValid, Is.True);
        Assert.That(parsed.Table!.DecodeSymbol(new WebpVp8LBitReader(SymbolBytes(code, 5))), Is.EqualTo(5));
        WebpVp8LBitReader reader = new WebpVp8LBitReader(writer.ToArray());
        WebpVp8LPrefixCodeParser.ParseCodeLengths(reader, 300);
        Assert.That(parsed.Table.DecodeSymbol(reader), Is.EqualTo(5));
        Assert.That(parsed.Table.DecodeSymbol(reader), Is.EqualTo(20));
        Assert.That(parsed.Table.DecodeSymbol(reader), Is.EqualTo(257));
    }

    [Test]
    public void WriteImageData_WritesLiteralAndLz77ReferenceTokens()
    {
        WebpVp8LColor first = NewColor(10, 20, 30, 255);
        WebpVp8LColor second = NewColor(40, 50, 60, 255);
        List<WebpVp8LLz77Token> tokens = new List<WebpVp8LLz77Token>();
        tokens.Add(NewLiteralToken(first));
        tokens.Add(NewLiteralToken(second));
        WebpVp8LLz77Token reference = new WebpVp8LLz77Token();
        reference.IsLiteral = false;
        reference.Length = 2;
        reference.Distance = 2;
        reference.DistanceCode = WebpVp8LLz77Encoder.EncodeDistanceCode(reference.Distance, 4);
        tokens.Add(reference);
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);

        WebpVp8LEntropyWriter.WriteImageData(writer, tokens, 4, 1);
        WebpVp8LImageDataDecodeResult result = WebpVp8LDecoder.DecodeImageData(new WebpVp8LBitReader(writer.ToArray()), 4, 1);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.PixelBuffer.Rgba, Is.EqualTo(new byte[] { 10, 20, 30, 255, 40, 50, 60, 255, 10, 20, 30, 255, 40, 50, 60, 255 }));
    }

    [Test]
    public void Encoder_StillRoundTripsAfterEntropyWriterIntegration()
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(2, 2);
        SetPixel(buffer, 0, 0, 1, 2, 3, 255);
        SetPixel(buffer, 1, 0, 4, 5, 6, 255);
        SetPixel(buffer, 0, 1, 7, 8, 9, 255);
        SetPixel(buffer, 1, 1, 10, 11, 12, 200);

        WebpVp8LDecodeResult decoded = WebpVp8LDecoder.Decode(WebpVp8LEncoder.Encode(buffer), WebpStrictness.Strict);

        Assert.That(decoded.IsValid, Is.True);
        Assert.That(decoded.PixelBuffer.Rgba, Is.EqualTo(buffer.Rgba));
    }

    [Test]
    public void BuildCode_RejectsInvalidSymbols()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LEntropyWriter.BuildCode(10, new int[] { 10 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8LEntropyWriter.BuildCode(0, new int[] { 0 }));
    }

    static byte[] SymbolBytes(WebpVp8LEntropyCode code, int symbol)
    {
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        WebpVp8LEntropyWriter.WriteSymbol(writer, code, symbol);
        return writer.ToArray();
    }

    static WebpVp8LLz77Token NewLiteralToken(WebpVp8LColor color)
    {
        WebpVp8LLz77Token token = new WebpVp8LLz77Token();
        token.IsLiteral = true;
        token.Color = color.Copy();
        return token;
    }

    static void SetPixel(WebpPixelBuffer buffer, int x, int y, byte red, byte green, byte blue, byte alpha)
    {
        int pixel = y * buffer.Stride + x * 4;
        buffer.Rgba[pixel] = red;
        buffer.Rgba[pixel + 1] = green;
        buffer.Rgba[pixel + 2] = blue;
        buffer.Rgba[pixel + 3] = alpha;
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
