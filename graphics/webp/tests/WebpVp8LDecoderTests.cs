using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8LDecoderTests
{
    [Test]
    public void Decode_DecodesSingleLiteralPixelPayload()
    {
        byte[] payload = BuildPayload(1, 1, false, new WebpVp8LColor[] { NewColor(10, 20, 30, 255) });

        WebpVp8LDecodeResult result = WebpVp8LDecoder.Decode(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Header.Width, Is.EqualTo(1));
        Assert.That(result.Header.Height, Is.EqualTo(1));
        Assert.That(result.PixelBuffer.Width, Is.EqualTo(1));
        Assert.That(result.PixelBuffer.Height, Is.EqualTo(1));
        Assert.That(result.PixelBuffer.Rgba, Is.EqualTo(new byte[] { 10, 20, 30, 255 }));
    }

    [Test]
    public void Decode_DecodesHandcraftedTwoByTwoLiteralPayloadWithAlpha()
    {
        byte[] payload = BuildPayload(2, 2, true, new WebpVp8LColor[]
        {
            NewColor(10, 20, 30, 255),
            NewColor(40, 20, 30, 255),
            NewColor(10, 60, 30, 128),
            NewColor(40, 60, 90, 128)
        });

        WebpVp8LDecodeResult result = WebpVp8LDecoder.Decode(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Header.Width, Is.EqualTo(2));
        Assert.That(result.Header.Height, Is.EqualTo(2));
        Assert.That(result.Header.HasAlpha, Is.True);
        Assert.That(result.PixelBuffer.Rgba, Is.EqualTo(new byte[]
        {
            10, 20, 30, 255,
            40, 20, 30, 255,
            10, 60, 30, 128,
            40, 60, 90, 128
        }));
    }

    [Test]
    public void Decode_DecodesReferenceFixturePayloads()
    {
        AssertFixtureDecode(
            "literal-rgba-1x1.vp8l.hex",
            1,
            1,
            false,
            new byte[] { 10, 20, 30, 255 });
        AssertFixtureDecode(
            "literal-rgba-alpha-2x2.vp8l.hex",
            2,
            2,
            true,
            new byte[]
            {
                10, 20, 30, 255,
                40, 20, 30, 255,
                10, 60, 30, 128,
                40, 60, 90, 128
            });
    }

    [Test]
    public void Decode_AppliesSubtractGreenTransform()
    {
        WebpBitWriter writer = NewWriter();
        WriteHeader(writer, 1, 1, false);
        writer.WriteBits(1, 1);
        writer.WriteBits(WebpVp8LDecoder.SubtractGreenTransformType, 2);
        writer.WriteBits(0, 1);
        WriteImageData(writer, new WebpVp8LColor[] { NewColor(10, 20, 30, 255) });

        WebpVp8LDecodeResult result = WebpVp8LDecoder.Decode(writer.ToArray(), WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.PixelBuffer.Rgba, Is.EqualTo(new byte[] { 30, 20, 50, 255 }));
    }

    [Test]
    public void Decode_AppliesColorIndexingTransformAndNestedPaletteImage()
    {
        WebpBitWriter writer = NewWriter();
        WriteHeader(writer, 2, 1, false);
        writer.WriteBits(1, 1);
        writer.WriteBits(WebpVp8LDecoder.ColorIndexingTransformType, 2);
        writer.WriteBits(1, 8);
        WriteSubresolutionImageData(writer, new WebpVp8LColor[]
        {
            NewColor(10, 20, 30, 255),
            NewColor(30, 30, 30, 0)
        });
        writer.WriteBits(0, 1);
        WriteImageData(writer, new WebpVp8LColor[] { NewColor(0, 2, 0, 255) });

        WebpVp8LDecodeResult result = WebpVp8LDecoder.Decode(writer.ToArray(), WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.PixelBuffer.Width, Is.EqualTo(2));
        Assert.That(result.PixelBuffer.Rgba, Is.EqualTo(new byte[] { 10, 20, 30, 255, 40, 50, 60, 255 }));
    }

    [Test]
    public void Decode_AppliesMultipleTransformsInReverseOrder()
    {
        WebpBitWriter writer = NewWriter();
        WriteHeader(writer, 2, 2, true);
        writer.WriteBits(1, 1);
        writer.WriteBits(WebpVp8LDecoder.ColorTransformType, 2);
        writer.WriteBits(0, 3);
        WriteSubresolutionImageData(writer, new WebpVp8LColor[] { NewColor(32, 16, 32, 255) });
        writer.WriteBits(1, 1);
        writer.WriteBits(WebpVp8LDecoder.SubtractGreenTransformType, 2);
        writer.WriteBits(0, 1);
        WriteImageData(writer, new WebpVp8LColor[]
        {
            NewColor(10, 20, 30, 100),
            NewColor(40, 20, 30, 100),
            NewColor(10, 60, 70, 150),
            NewColor(40, 60, 70, 150)
        });

        WebpVp8LDecodeResult result = WebpVp8LDecoder.Decode(writer.ToArray(), WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Header.HasAlpha, Is.True);
        Assert.That(result.PixelBuffer.Width, Is.EqualTo(2));
        Assert.That(result.PixelBuffer.Height, Is.EqualTo(2));
        Assert.That(result.PixelBuffer.Rgba, Is.EqualTo(new byte[]
        {
            50, 20, 110, 100,
            80, 20, 140, 100,
            130, 60, 34, 150,
            160, 60, 64, 150
        }));
    }

    [Test]
    public void DecodePixels_HandlesLz77ReferencesAndColorCacheSymbols()
    {
        WebpVp8LColor color = NewColor(10, 20, 30, 255);
        WebpVp8LColorCache cacheForIndex = new WebpVp8LColorCache(1);
        int cacheSymbol = 280 + cacheForIndex.GetIndex(color);
        WebpVp8LPrefixCodeGroup group = NewPrefixGroupForLz77AndCache(cacheSymbol);
        WebpVp8LMetaPrefixParseResult metaPrefix = new WebpVp8LMetaPrefixParseResult();
        metaPrefix.PrefixCodeGroups.Add(group);
        WebpVp8LColorCache cache = new WebpVp8LColorCache(1);
        List<WebpDiagnostic> diagnostics = new List<WebpDiagnostic>();
        WebpBitWriter writer = NewWriter();
        WriteSymbol(group.Green, 20, writer);
        WriteSymbol(group.Green, 257, writer);
        WriteSymbol(group.Green, cacheSymbol, writer);

        WebpVp8LColor[] pixels = WebpVp8LDecoder.DecodePixels(new WebpVp8LBitReader(writer.ToArray()), 4, 1, metaPrefix, cache, diagnostics);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(pixels[0].ToArgb(), Is.EqualTo(color.ToArgb()));
        Assert.That(pixels[1].ToArgb(), Is.EqualTo(color.ToArgb()));
        Assert.That(pixels[2].ToArgb(), Is.EqualTo(color.ToArgb()));
        Assert.That(pixels[3].ToArgb(), Is.EqualTo(color.ToArgb()));
    }

    [Test]
    public void DecodePixels_MapsUninitializedColorCacheEntryToTransparentBlack()
    {
        WebpVp8LPrefixCodeGroup group = new WebpVp8LPrefixCodeGroup();
        group.Green = BuildTable(new int[] { 280 }, 282);
        group.Red = BuildSimpleTable(new int[] { 0 }, 256);
        group.Blue = BuildSimpleTable(new int[] { 0 }, 256);
        group.Alpha = BuildSimpleTable(new int[] { 0 }, 256);
        group.Distance = BuildSimpleTable(new int[] { 0 }, 40);
        WebpVp8LMetaPrefixParseResult metaPrefix = new WebpVp8LMetaPrefixParseResult();
        metaPrefix.PrefixCodeGroups.Add(group);
        List<WebpDiagnostic> diagnostics = new List<WebpDiagnostic>();

        WebpVp8LColor[] pixels = WebpVp8LDecoder.DecodePixels(new WebpVp8LBitReader(Array.Empty<byte>()), 1, 1, metaPrefix, new WebpVp8LColorCache(1), diagnostics);

        Assert.That(diagnostics, Is.Empty);
        Assert.That(pixels[0].ToArgb(), Is.EqualTo(0U));
    }

    [Test]
    public void DecodePixels_ReportsBadBackwardReferenceDistance()
    {
        WebpVp8LPrefixCodeGroup group = new WebpVp8LPrefixCodeGroup();
        group.Green = BuildSimpleTable(new int[] { 257 }, 282);
        group.Red = BuildSimpleTable(new int[] { 0 }, 256);
        group.Blue = BuildSimpleTable(new int[] { 0 }, 256);
        group.Alpha = BuildSimpleTable(new int[] { 0 }, 256);
        group.Distance = BuildSimpleTable(new int[] { 0 }, 40);
        WebpVp8LMetaPrefixParseResult metaPrefix = new WebpVp8LMetaPrefixParseResult();
        metaPrefix.PrefixCodeGroups.Add(group);
        List<WebpDiagnostic> diagnostics = new List<WebpDiagnostic>();

        WebpVp8LDecoder.DecodePixels(new WebpVp8LBitReader(Array.Empty<byte>()), 2, 1, metaPrefix, null, diagnostics);

        Assert.That(diagnostics[0].Message, Does.Contain("bad backward reference distance"));
    }

    [Test]
    public void Decode_ReportsInvalidHeader()
    {
        byte[] payload = BuildPayload(1, 1, false, new WebpVp8LColor[] { NewColor(0, 0, 0, 255) });
        payload[0] = 0;

        WebpVp8LDecodeResult result = WebpVp8LDecoder.Decode(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("signature"));
    }

    [Test]
    public void Decode_ReportsNonZeroVersion()
    {
        WebpBitWriter writer = NewWriter();
        WriteHeaderWithVersion(writer, 1, 1, false, 1);

        WebpVp8LDecodeResult result = WebpVp8LDecoder.Decode(writer.ToArray(), WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("version"));
    }

    [Test]
    public void Decode_ReportsDuplicateTransforms()
    {
        WebpBitWriter writer = NewWriter();
        WriteHeader(writer, 1, 1, false);
        writer.WriteBits(1, 1);
        writer.WriteBits(WebpVp8LDecoder.SubtractGreenTransformType, 2);
        writer.WriteBits(1, 1);
        writer.WriteBits(WebpVp8LDecoder.SubtractGreenTransformType, 2);

        WebpVp8LDecodeResult result = WebpVp8LDecoder.Decode(writer.ToArray(), WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("duplicate"));
    }

    [Test]
    public void Decode_ReportsInvalidColorCacheBits()
    {
        WebpBitWriter writer = NewWriter();
        WriteHeader(writer, 1, 1, false);
        writer.WriteBits(0, 1);
        writer.WriteBits(1, 1);
        writer.WriteBits(0, 4);

        WebpVp8LDecodeResult result = WebpVp8LDecoder.Decode(writer.ToArray(), WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("color cache bits"));
    }

    [Test]
    public void Decode_ReportsTruncatedDataAfterHeader()
    {
        WebpBitWriter writer = NewWriter();
        WriteHeader(writer, 1, 1, false);

        WebpVp8LDecodeResult result = WebpVp8LDecoder.Decode(writer.ToArray(), WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("truncated"));
    }

    static byte[] BuildPayload(int width, int height, bool hasAlpha, WebpVp8LColor[] colors)
    {
        WebpBitWriter writer = NewWriter();
        WriteHeader(writer, width, height, hasAlpha);
        writer.WriteBits(0, 1);
        WriteImageData(writer, colors);
        return writer.ToArray();
    }

    static void AssertFixtureDecode(string fileName, int width, int height, bool hasAlpha, byte[] expectedRgba)
    {
        byte[] payload = ReadFixtureHex(fileName);

        WebpVp8LDecodeResult result = WebpVp8LDecoder.Decode(payload, WebpStrictness.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Header.Width, Is.EqualTo(width));
        Assert.That(result.Header.Height, Is.EqualTo(height));
        Assert.That(result.Header.HasAlpha, Is.EqualTo(hasAlpha));
        Assert.That(result.PixelBuffer.Width, Is.EqualTo(width));
        Assert.That(result.PixelBuffer.Height, Is.EqualTo(height));
        Assert.That(result.PixelBuffer.Rgba, Is.EqualTo(expectedRgba));
    }

    static byte[] ReadFixtureHex(string fileName)
    {
        string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Vp8L", fileName);
        string hex = File.ReadAllText(path).Trim();
        byte[] payload = new byte[hex.Length / 2];
        int index = 0;
        while (index < payload.Length)
        {
            payload[index] = Convert.ToByte(hex.Substring(index * 2, 2), 16);
            index = index + 1;
        }

        return payload;
    }

    static void WriteHeader(WebpBitWriter writer, int width, int height, bool hasAlpha)
    {
        WriteHeaderWithVersion(writer, width, height, hasAlpha, WebpConstants.Vp8LVersion);
    }

    static void WriteHeaderWithVersion(WebpBitWriter writer, int width, int height, bool hasAlpha, int version)
    {
        writer.WriteBits(WebpConstants.Vp8LSignature, 8);
        writer.WriteBits((uint)(width - 1), 14);
        writer.WriteBits((uint)(height - 1), 14);
        if (hasAlpha)
        {
            writer.WriteBits(1, 1);
        }
        else
        {
            writer.WriteBits(0, 1);
        }

        writer.WriteBits((uint)version, 3);
    }

    static void WriteImageData(WebpBitWriter writer, WebpVp8LColor[] colors)
    {
        writer.WriteBits(0, 1);
        writer.WriteBits(0, 1);
        WritePrefixCodesAndPixels(writer, colors);
    }

    static void WriteSubresolutionImageData(WebpBitWriter writer, WebpVp8LColor[] colors)
    {
        writer.WriteBits(0, 1);
        WritePrefixCodesAndPixels(writer, colors);
    }

    static void WritePrefixCodesAndPixels(WebpBitWriter writer, WebpVp8LColor[] colors)
    {
        WebpVp8LPrefixCodeGroup group = NewPrefixGroup(colors);
        WriteSimplePrefixCode(writer, ChannelSymbols(colors, 1));
        WriteSimplePrefixCode(writer, ChannelSymbols(colors, 0));
        WriteSimplePrefixCode(writer, ChannelSymbols(colors, 2));
        WriteSimplePrefixCode(writer, ChannelSymbols(colors, 3));
        WriteSimplePrefixCode(writer, new int[] { 0 });

        int index = 0;
        while (index < colors.Length)
        {
            WriteSymbol(group.Green, colors[index].Green, writer);
            WriteSymbol(group.Red, colors[index].Red, writer);
            WriteSymbol(group.Blue, colors[index].Blue, writer);
            WriteSymbol(group.Alpha, colors[index].Alpha, writer);
            index = index + 1;
        }
    }

    static WebpVp8LPrefixCodeGroup NewPrefixGroup(WebpVp8LColor[] colors)
    {
        WebpVp8LPrefixCodeGroup group = new WebpVp8LPrefixCodeGroup();
        group.Green = BuildSimpleTable(ChannelSymbols(colors, 1), 280);
        group.Red = BuildSimpleTable(ChannelSymbols(colors, 0), 256);
        group.Blue = BuildSimpleTable(ChannelSymbols(colors, 2), 256);
        group.Alpha = BuildSimpleTable(ChannelSymbols(colors, 3), 256);
        group.Distance = BuildSimpleTable(new int[] { 0 }, 40);
        return group;
    }

    static WebpVp8LPrefixCodeGroup NewPrefixGroupForLz77AndCache(int cacheSymbol)
    {
        WebpVp8LPrefixCodeGroup group = new WebpVp8LPrefixCodeGroup();
        group.Green = BuildTable(new int[] { 0, 20, 257, cacheSymbol }, 282);
        group.Red = BuildSimpleTable(new int[] { 10 }, 256);
        group.Blue = BuildSimpleTable(new int[] { 30 }, 256);
        group.Alpha = BuildSimpleTable(new int[] { 255 }, 256);
        group.Distance = BuildSimpleTable(new int[] { 1 }, 40);
        return group;
    }

    static WebpVp8LPrefixCodeTable BuildSimpleTable(int[] symbols, int alphabetSize)
    {
        return BuildTable(symbols, alphabetSize);
    }

    static WebpVp8LPrefixCodeTable BuildTable(int[] symbols, int alphabetSize)
    {
        int[] codeLengths = new int[alphabetSize];
        int index = 0;
        while (index < symbols.Length)
        {
            codeLengths[symbols[index]] = 1;
            if (symbols.Length == 4)
            {
                codeLengths[symbols[index]] = 2;
            }

            index = index + 1;
        }

        return WebpVp8LPrefixCodeParser.BuildTable(codeLengths);
    }

    static void WriteSimplePrefixCode(WebpBitWriter writer, int[] symbols)
    {
        if (symbols.Length > 2)
        {
            throw new ArgumentException("test helper supports up to two simple symbols", "symbols");
        }

        writer.WriteBits(1, 1);
        if (symbols.Length == 1)
        {
            writer.WriteBits(0, 1);
            WriteFirstSimpleSymbol(writer, symbols[0]);
        }
        else
        {
            writer.WriteBits(1, 1);
            writer.WriteBits(1, 1);
            writer.WriteBits((uint)symbols[0], 8);
            writer.WriteBits((uint)symbols[1], 8);
        }
    }

    static void WriteFirstSimpleSymbol(WebpBitWriter writer, int symbol)
    {
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

    static void WriteSymbol(WebpVp8LPrefixCodeTable table, int symbol, WebpBitWriter writer)
    {
        if (table.IsSingleSymbol)
        {
            return;
        }

        int index = 0;
        while (index < table.Entries.Count)
        {
            if (table.Entries[index].Symbol == symbol)
            {
                writer.WriteBits((uint)table.Entries[index].ReversedCode, table.Entries[index].CodeLength);
                return;
            }

            index = index + 1;
        }

        throw new ArgumentException("symbol is missing from the prefix table", "symbol");
    }

    static int[] ChannelSymbols(WebpVp8LColor[] colors, int channel)
    {
        List<int> symbols = new List<int>();
        int index = 0;
        while (index < colors.Length)
        {
            int symbol = ChannelValue(colors[index], channel);
            if (!Contains(symbols, symbol))
            {
                symbols.Add(symbol);
            }

            index = index + 1;
        }

        symbols.Sort();
        return symbols.ToArray();
    }

    static int ChannelValue(WebpVp8LColor color, int channel)
    {
        if (channel == 0)
        {
            return color.Red;
        }

        if (channel == 1)
        {
            return color.Green;
        }

        if (channel == 2)
        {
            return color.Blue;
        }

        return color.Alpha;
    }

    static bool Contains(List<int> values, int value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static WebpBitWriter NewWriter()
    {
        return new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
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
