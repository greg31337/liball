namespace chart.Tests;

[TestFixture]
public class BmpPngConformanceTests
{
    [TestCase(ChartBmpBitDepth.Rgb24, ChartBmpRowOrientation.TopDown)]
    [TestCase(ChartBmpBitDepth.Rgb24, ChartBmpRowOrientation.BottomUp)]
    [TestCase(ChartBmpBitDepth.Rgba32, ChartBmpRowOrientation.TopDown)]
    [TestCase(ChartBmpBitDepth.Rgba32, ChartBmpRowOrientation.BottomUp)]
    public void Bmp_RoundTripsDirectPixelsWithIndependentDecoder(ChartBmpBitDepth bitDepth, ChartBmpRowOrientation orientation)
    {
        RawBitmap source = CreateColorBitmap();
        ChartBmpOptions options = new ChartBmpOptions();
        options.BitDepth = bitDepth;
        options.RowOrientation = orientation;
        options.HorizontalPixelsPerMeter = 3780;
        options.VerticalPixelsPerMeter = 1890;

        byte[] encoded = new ChartExport().ToBmpBytesUsingOptions(source, options);
        ReferenceDecodedImage decoded = ReferenceBmpDecoder.Decode(encoded);

        Assert.That(decoded.Width, Is.EqualTo(3));
        Assert.That(decoded.Height, Is.EqualTo(2));
        Assert.That(ReadInt32LittleEndian(encoded, 2), Is.EqualTo(encoded.Length));
        Assert.That(ReadInt32LittleEndian(encoded, 10), Is.EqualTo(54));
        Assert.That(ReadInt32LittleEndian(encoded, 14), Is.EqualTo(40));
        Assert.That(ReadInt32LittleEndian(encoded, 18), Is.EqualTo(3));
        Assert.That(ReadInt32LittleEndian(encoded, 22), Is.EqualTo(orientation == ChartBmpRowOrientation.TopDown ? -2 : 2));
        Assert.That(ReadUInt16LittleEndian(encoded, 26), Is.EqualTo(1));
        Assert.That(ReadUInt16LittleEndian(encoded, 28), Is.EqualTo((int)bitDepth));
        Assert.That(ReadInt32LittleEndian(encoded, 30), Is.EqualTo(0));
        Assert.That(ReadInt32LittleEndian(encoded, 38), Is.EqualTo(3780));
        Assert.That(ReadInt32LittleEndian(encoded, 42), Is.EqualTo(1890));
        AssertDirectPixels(source, decoded, bitDepth == ChartBmpBitDepth.Rgba32);
    }

    [TestCase(ChartBmpRowOrientation.TopDown)]
    [TestCase(ChartBmpRowOrientation.BottomUp)]
    public void Bmp_IndexedPaletteRowsAndPaddingRoundTripWithIndependentDecoder(ChartBmpRowOrientation orientation)
    {
        ChartColor[] palette = new ChartColor[]
        {
            new ChartColor(10, 20, 30, 0),
            new ChartColor(40, 50, 60, 128),
            new ChartColor(70, 80, 90, 255)
        };
        RawBitmap source = new RawBitmapFactory().WrapIndexed(3, 2, 3, new byte[] { 0, 1, 2, 2, 1, 0 }, palette);
        ChartBmpOptions options = new ChartBmpOptions();
        options.BitDepth = ChartBmpBitDepth.Indexed8;
        options.RowOrientation = orientation;

        byte[] encoded = new ChartExport().ToBmpBytesUsingOptions(source, options);
        ReferenceDecodedImage decoded = ReferenceBmpDecoder.Decode(encoded);
        int pixelOffset = ReadInt32LittleEndian(encoded, 10);

        Assert.That(pixelOffset, Is.EqualTo(66));
        Assert.That(ReadUInt16LittleEndian(encoded, 28), Is.EqualTo(8));
        Assert.That(ReadInt32LittleEndian(encoded, 34), Is.EqualTo(8));
        Assert.That(ReadInt32LittleEndian(encoded, 46), Is.EqualTo(3));
        Assert.That(encoded.Length, Is.EqualTo(74));
        AssertColor(decoded.GetPixel(0, 0), 10, 20, 30, 255);
        AssertColor(decoded.GetPixel(1, 0), 40, 50, 60, 255);
        AssertColor(decoded.GetPixel(2, 0), 70, 80, 90, 255);
        AssertColor(decoded.GetPixel(0, 1), 70, 80, 90, 255);
        AssertColor(decoded.GetPixel(2, 1), 10, 20, 30, 255);
        Assert.That(encoded[pixelOffset + 3], Is.EqualTo(0));
        Assert.That(encoded[pixelOffset + 7], Is.EqualTo(0));
    }

    [Test]
    public void Bmp_ThreeByteRowsUseDwordPaddingAndSpecifiedStorageOrder()
    {
        RawBitmap source = CreateColorBitmap();
        ChartBmpOptions topDownOptions = new ChartBmpOptions();
        ChartBmpOptions bottomUpOptions = new ChartBmpOptions();
        topDownOptions.BitDepth = ChartBmpBitDepth.Rgb24;
        topDownOptions.RowOrientation = ChartBmpRowOrientation.TopDown;
        bottomUpOptions.BitDepth = ChartBmpBitDepth.Rgb24;
        bottomUpOptions.RowOrientation = ChartBmpRowOrientation.BottomUp;

        byte[] topDown = new ChartExport().ToBmpBytesUsingOptions(source, topDownOptions);
        byte[] bottomUp = new ChartExport().ToBmpBytesUsingOptions(source, bottomUpOptions);

        Assert.That(ReadInt32LittleEndian(topDown, 34), Is.EqualTo(24));
        Assert.That(topDown.Length, Is.EqualTo(78));
        Assert.That(topDown[54], Is.EqualTo(source.GetPixel(0, 0).B));
        Assert.That(bottomUp[54], Is.EqualTo(source.GetPixel(0, 1).B));
        Assert.That(topDown[63], Is.EqualTo(0));
        Assert.That(topDown[64], Is.EqualTo(0));
        Assert.That(topDown[65], Is.EqualTo(0));
        Assert.That(topDown[75], Is.EqualTo(0));
        Assert.That(topDown[76], Is.EqualTo(0));
        Assert.That(topDown[77], Is.EqualTo(0));
    }

    [TestCase(ChartPngColorType.Grayscale, 0)]
    [TestCase(ChartPngColorType.Truecolor, 2)]
    [TestCase(ChartPngColorType.GrayscaleAlpha, 4)]
    [TestCase(ChartPngColorType.TruecolorAlpha, 6)]
    public void Png_EveryDirectColorTypeRoundTripsWithIndependentDecoder(ChartPngColorType colorType, int expectedCode)
    {
        RawBitmap source = CreateColorBitmap();
        ChartPngOptions options = new ChartPngOptions();
        options.ColorType = colorType;
        options.Filter = ChartPngFilter.Paeth;

        byte[] encoded = new ChartExport().ToPngBytesWithOptions(source, options);
        ReferenceDecodedImage decoded = ReferencePngDecoder.Decode(encoded);
        byte[] header = GetChunkData(encoded, "IHDR");

        Assert.That(header[8], Is.EqualTo(8));
        Assert.That(header[9], Is.EqualTo(expectedCode));
        Assert.That(header[10], Is.EqualTo(0));
        Assert.That(header[11], Is.EqualTo(0));
        Assert.That(header[12], Is.EqualTo(0));
        AssertPngPixels(source, decoded, colorType);
    }

    [Test]
    public void Png_IndexedColorPaletteAndTransparencyRoundTripWithIndependentDecoder()
    {
        RawBitmap source = new RawBitmapFactory().WrapIndexed(
            3,
            2,
            3,
            new byte[] { 0, 1, 2, 2, 1, 0 },
            new ChartColor[]
            {
                new ChartColor(10, 20, 30, 0),
                new ChartColor(40, 50, 60, 128),
                new ChartColor(70, 80, 90, 255)
            });
        ChartPngOptions options = new ChartPngOptions();
        options.ColorType = ChartPngColorType.Indexed;
        options.Filter = ChartPngFilter.Sub;

        byte[] encoded = new ChartExport().ToPngBytesWithOptions(source, options);
        ReferenceDecodedImage decoded = ReferencePngDecoder.Decode(encoded);

        Assert.That(ChunkTypes(encoded), Is.EqualTo(new string[] { "IHDR", "PLTE", "tRNS", "IDAT", "IEND" }));
        Assert.That(GetChunkData(encoded, "PLTE"), Is.EqualTo(new byte[] { 10, 20, 30, 40, 50, 60, 70, 80, 90 }));
        Assert.That(GetChunkData(encoded, "tRNS"), Is.EqualTo(new byte[] { 0, 128 }));
        AssertColor(decoded.GetPixel(0, 0), 10, 20, 30, 0);
        AssertColor(decoded.GetPixel(1, 0), 40, 50, 60, 128);
        AssertColor(decoded.GetPixel(2, 0), 70, 80, 90, 255);
        AssertColor(decoded.GetPixel(0, 1), 70, 80, 90, 255);
    }

    [TestCase(ChartPngFilter.None, 0)]
    [TestCase(ChartPngFilter.Sub, 1)]
    [TestCase(ChartPngFilter.Up, 2)]
    [TestCase(ChartPngFilter.Average, 3)]
    [TestCase(ChartPngFilter.Paeth, 4)]
    public void Png_EveryForcedFilterRoundTripsAndWritesRequestedFilterByte(ChartPngFilter filter, int expectedFilter)
    {
        RawBitmap source = CreateColorBitmap();
        ChartPngOptions options = new ChartPngOptions();
        options.ColorType = ChartPngColorType.TruecolorAlpha;
        options.Filter = filter;
        options.Compression = ChartPngCompression.NoCompression;

        byte[] encoded = new ChartExport().ToPngBytesWithOptions(source, options);
        byte[] filtered = DecompressZlib(GetChunkData(encoded, "IDAT"));
        ReferenceDecodedImage decoded = ReferencePngDecoder.Decode(encoded);

        Assert.That(filtered[0], Is.EqualTo(expectedFilter));
        Assert.That(filtered[13], Is.EqualTo(expectedFilter));
        AssertDirectPixels(source, decoded, true);
    }

    [Test]
    public void Png_AdaptiveFilteringProducesOnlyValidRowsAndRoundTrips()
    {
        RawBitmap source = CreateColorBitmap();
        ChartPngOptions options = new ChartPngOptions();
        options.ColorType = ChartPngColorType.TruecolorAlpha;
        options.Filter = ChartPngFilter.Adaptive;

        byte[] encoded = new ChartExport().ToPngBytesWithOptions(source, options);
        byte[] filtered = DecompressZlib(GetChunkData(encoded, "IDAT"));
        ReferenceDecodedImage decoded = ReferencePngDecoder.Decode(encoded);

        Assert.That(filtered[0], Is.InRange(0, 4));
        Assert.That(filtered[13], Is.InRange(0, 4));
        AssertDirectPixels(source, decoded, true);
    }

    [Test]
    public void Png_ChunksCrcZlibHeaderAndAdlerConformToSpecifications()
    {
        RawBitmap source = CreateColorBitmap();
        ChartPngOptions options = new ChartPngOptions();
        options.ColorType = ChartPngColorType.TruecolorAlpha;

        byte[] encoded = new ChartExport().ToPngBytesWithOptions(source, options);
        byte[] compressed = GetChunkData(encoded, "IDAT");
        byte[] filtered = DecompressZlib(compressed);

        Assert.That(CopyRange(encoded, 0, 8), Is.EqualTo(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }));
        Assert.That(ChunkTypes(encoded), Is.EqualTo(new string[] { "IHDR", "IDAT", "IEND" }));
        AssertEveryChunkCrc(encoded);
        Assert.That(compressed[0] & 15, Is.EqualTo(8));
        Assert.That(((compressed[0] << 8) + compressed[1]) % 31, Is.EqualTo(0));
        Assert.That(compressed[1] & 32, Is.EqualTo(0));
        Assert.That(ReadUInt32BigEndian(compressed, compressed.Length - 4), Is.EqualTo(ReferencePngDecoder.Adler32(filtered)));
        Assert.DoesNotThrow(() => ReferencePngDecoder.Decode(encoded));
    }

    [Test]
    public void Png_MetadataUsesUncompressedInternationalTextChunksBeforeImageData()
    {
        RawBitmap source = CreateColorBitmap();
        ChartPngOptions options = new ChartPngOptions();
        options.Metadata.Title = "Résumé";
        options.Metadata.Author = "Ada Lovelace";
        options.Metadata.Description = "Axes, points, and Ω";
        options.Metadata.Copyright = "Public domain";
        options.Metadata.Software = "chart.Tests";

        byte[] encoded = new ChartExport().ToPngBytesWithOptions(source, options);
        byte[][] chunks = GetChunkDataItems(encoded, "iTXt");

        Assert.That(ChunkTypes(encoded), Is.EqualTo(new string[] { "IHDR", "iTXt", "iTXt", "iTXt", "iTXt", "iTXt", "IDAT", "IEND" }));
        Assert.That(ParseInternationalText(chunks[0]), Is.EqualTo("Title=Résumé"));
        Assert.That(ParseInternationalText(chunks[1]), Is.EqualTo("Author=Ada Lovelace"));
        Assert.That(ParseInternationalText(chunks[2]), Is.EqualTo("Description=Axes, points, and Ω"));
        Assert.That(ParseInternationalText(chunks[3]), Is.EqualTo("Copyright=Public domain"));
        Assert.That(ParseInternationalText(chunks[4]), Is.EqualTo("Software=chart.Tests"));
        AssertEveryChunkCrc(encoded);
        Assert.DoesNotThrow(() => ReferencePngDecoder.Decode(encoded));
    }

    [Test]
    public void BmpAndPng_StreamWritesAppendWithoutClosingCallerStream()
    {
        RawBitmap source = CreateColorBitmap();
        ChartExport export = new ChartExport();
        System.IO.MemoryStream bmpStream = CreatePrefixedStream();
        System.IO.MemoryStream pngStream = CreatePrefixedStream();

        export.WriteBmp(source, bmpStream, new ChartBmpOptions());
        export.WritePng(source, pngStream, new ChartPngOptions());

        Assert.That(bmpStream.CanWrite, Is.True);
        Assert.That(pngStream.CanWrite, Is.True);
        Assert.That(CopyRange(bmpStream.ToArray(), 0, 3), Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(CopyRange(pngStream.ToArray(), 0, 3), Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.DoesNotThrow(() => ReferenceBmpDecoder.Decode(CopyRange(bmpStream.ToArray(), 3, (int)bmpStream.Length - 3)));
        Assert.DoesNotThrow(() => ReferencePngDecoder.Decode(CopyRange(pngStream.ToArray(), 3, (int)pngStream.Length - 3)));
        bmpStream.WriteByte(4);
        pngStream.WriteByte(4);
    }

    [Test]
    public void Encoders_RejectMalformedOptionsAndUnwritableDestinations()
    {
        RawBitmap source = CreateColorBitmap();
        ChartExport export = new ChartExport();
        ChartBmpOptions bmp = new ChartBmpOptions();
        ChartPngOptions png = new ChartPngOptions();
        System.IO.MemoryStream readOnly = new System.IO.MemoryStream(new byte[1], false);

        bmp.BitDepth = (ChartBmpBitDepth)16;
        Assert.Throws<System.ArgumentOutOfRangeException>(() => export.ToBmpBytesUsingOptions(source, bmp));
        bmp.BitDepth = ChartBmpBitDepth.Rgb24;
        bmp.RowOrientation = (ChartBmpRowOrientation)2;
        Assert.Throws<System.ArgumentOutOfRangeException>(() => export.ToBmpBytesUsingOptions(source, bmp));
        bmp.RowOrientation = ChartBmpRowOrientation.TopDown;
        bmp.HorizontalPixelsPerMeter = -1;
        Assert.Throws<System.ArgumentOutOfRangeException>(() => export.ToBmpBytesUsingOptions(source, bmp));
        bmp.HorizontalPixelsPerMeter = 0;
        bmp.VerticalPixelsPerMeter = -1;
        Assert.Throws<System.ArgumentOutOfRangeException>(() => export.ToBmpBytesUsingOptions(source, bmp));
        Assert.Throws<System.ArgumentNullException>(() => export.ToBmpBytesUsingOptions(source, null!));
        Assert.Throws<System.ArgumentNullException>(() => export.WriteBmp(source, null!, new ChartBmpOptions()));
        Assert.Throws<System.ArgumentException>(() => export.WriteBmp(source, readOnly, new ChartBmpOptions()));

        png.ColorType = (ChartPngColorType)1;
        Assert.Throws<System.ArgumentOutOfRangeException>(() => export.ToPngBytesWithOptions(source, png));
        png.ColorType = ChartPngColorType.Automatic;
        png.Filter = (ChartPngFilter)5;
        Assert.Throws<System.ArgumentOutOfRangeException>(() => export.ToPngBytesWithOptions(source, png));
        png.Filter = ChartPngFilter.Adaptive;
        png.Compression = (ChartPngCompression)4;
        Assert.Throws<System.ArgumentOutOfRangeException>(() => export.ToPngBytesWithOptions(source, png));
        png.Compression = ChartPngCompression.Optimal;
        png.Metadata = null!;
        Assert.Throws<System.ArgumentNullException>(() => export.ToPngBytesWithOptions(source, png));
        png.Metadata = new ChartPngMetadata();
        png.Metadata.Description = "bad\0metadata";
        Assert.Throws<System.ArgumentException>(() => export.ToPngBytesWithOptions(source, png));
        png.Metadata.Description = null!;
        Assert.Throws<System.ArgumentNullException>(() => export.ToPngBytesWithOptions(source, png));
        Assert.Throws<System.ArgumentNullException>(() => export.ToPngBytesWithOptions(source, null!));
        Assert.Throws<System.ArgumentNullException>(() => export.WritePng(source, null!, new ChartPngOptions()));
        Assert.Throws<System.ArgumentException>(() => export.WritePng(source, readOnly, new ChartPngOptions()));
    }

    [Test]
    public void IndependentPngDecoderRejectsCorruptedChunkCrc()
    {
        byte[] encoded = new ChartExport().ToPngBytesWithOptions(CreateColorBitmap(), new ChartPngOptions());
        byte[] corrupted = (byte[])encoded.Clone();
        int idatOffset = FindChunkOffset(corrupted, "IDAT");
        corrupted[idatOffset + 8] = (byte)(corrupted[idatOffset + 8] ^ 1);

        Assert.Throws<System.IO.InvalidDataException>(() => ReferencePngDecoder.Decode(corrupted));
    }

    [Test]
    public void IndependentPngDecoderRejectsCorruptedZlibAdlerWithValidChunkCrc()
    {
        byte[] encoded = new ChartExport().ToPngBytesWithOptions(CreateColorBitmap(), new ChartPngOptions());
        byte[] corrupted = (byte[])encoded.Clone();
        int idatOffset = FindChunkOffset(corrupted, "IDAT");
        int idatLength = (int)ReadUInt32BigEndian(corrupted, idatOffset);
        int adlerOffset = idatOffset + 8 + idatLength - 1;
        corrupted[adlerOffset] = (byte)(corrupted[adlerOffset] ^ 1);
        byte[] crcInput = CopyRange(corrupted, idatOffset + 4, idatLength + 4);
        WriteUInt32BigEndian(corrupted, idatOffset + 8 + idatLength, ReferencePngDecoder.Crc32(crcInput));

        Assert.Throws<System.IO.InvalidDataException>(() => ReferencePngDecoder.Decode(corrupted));
    }

    private static RawBitmap CreateColorBitmap()
    {
        RawBitmap bitmap = new RawBitmap(3, 2, ChartPixelFormat.Rgba32);
        bitmap.SetPixel(0, 0, new ChartColor(255, 0, 0, 10));
        bitmap.SetPixel(1, 0, new ChartColor(0, 255, 0, 20));
        bitmap.SetPixel(2, 0, new ChartColor(0, 0, 255, 30));
        bitmap.SetPixel(0, 1, new ChartColor(255, 255, 255, 40));
        bitmap.SetPixel(1, 1, new ChartColor(30, 60, 90, 50));
        bitmap.SetPixel(2, 1, new ChartColor(90, 60, 30, 60));
        return bitmap;
    }

    private static void AssertDirectPixels(RawBitmap source, ReferenceDecodedImage decoded, bool preservesAlpha)
    {
        int y = 0;
        while (y < source.GetHeight())
        {
            int x = 0;
            while (x < source.GetWidth())
            {
                ChartColor expected = source.GetPixel(x, y);
                int alpha = 255;
                if (preservesAlpha)
                {
                    alpha = expected.A;
                }

                AssertColor(decoded.GetPixel(x, y), expected.R, expected.G, expected.B, alpha);
                x++;
            }

            y++;
        }
    }

    private static void AssertPngPixels(RawBitmap source, ReferenceDecodedImage decoded, ChartPngColorType colorType)
    {
        int y = 0;
        while (y < source.GetHeight())
        {
            int x = 0;
            while (x < source.GetWidth())
            {
                ChartColor expected = source.GetPixel(x, y);
                if (colorType == ChartPngColorType.Grayscale || colorType == ChartPngColorType.GrayscaleAlpha)
                {
                    byte luminance = ReferenceLuminance(expected);
                    int alpha = colorType == ChartPngColorType.GrayscaleAlpha ? expected.A : 255;
                    AssertColor(decoded.GetPixel(x, y), luminance, luminance, luminance, alpha);
                }
                else
                {
                    int alpha = colorType == ChartPngColorType.TruecolorAlpha ? expected.A : 255;
                    AssertColor(decoded.GetPixel(x, y), expected.R, expected.G, expected.B, alpha);
                }

                x++;
            }

            y++;
        }
    }

    private static byte ReferenceLuminance(ChartColor color)
    {
        return (byte)System.Math.Round(color.R * 0.299 + color.G * 0.587 + color.B * 0.114, System.MidpointRounding.AwayFromZero);
    }

    private static System.IO.MemoryStream CreatePrefixedStream()
    {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        stream.Write(new byte[] { 1, 2, 3 }, 0, 3);
        return stream;
    }

    private static byte[] DecompressZlib(byte[] data)
    {
        System.IO.MemoryStream input = new System.IO.MemoryStream(data);
        System.IO.MemoryStream output = new System.IO.MemoryStream();
        using (System.IO.Compression.ZLibStream zlib = new System.IO.Compression.ZLibStream(input, System.IO.Compression.CompressionMode.Decompress))
        {
            zlib.CopyTo(output);
        }

        return output.ToArray();
    }

    private static void AssertEveryChunkCrc(byte[] png)
    {
        int offset = 8;
        while (offset < png.Length)
        {
            int length = (int)ReadUInt32BigEndian(png, offset);
            byte[] input = CopyRange(png, offset + 4, length + 4);
            uint stored = ReadUInt32BigEndian(png, offset + length + 8);
            Assert.That(stored, Is.EqualTo(ReferencePngDecoder.Crc32(input)));
            offset = offset + length + 12;
        }
    }

    private static byte[] GetChunkData(byte[] png, string requestedType)
    {
        byte[][] items = GetChunkDataItems(png, requestedType);
        Assert.That(items.Length, Is.GreaterThan(0), "missing PNG chunk " + requestedType);
        return items[0];
    }

    private static byte[][] GetChunkDataItems(byte[] png, string requestedType)
    {
        System.Collections.Generic.List<byte[]> items = new System.Collections.Generic.List<byte[]>();
        int offset = 8;
        while (offset < png.Length)
        {
            int length = (int)ReadUInt32BigEndian(png, offset);
            string type = System.Text.Encoding.ASCII.GetString(png, offset + 4, 4);
            if (type == requestedType)
            {
                items.Add(CopyRange(png, offset + 8, length));
            }

            offset = offset + length + 12;
        }

        return items.ToArray();
    }

    private static string[] ChunkTypes(byte[] png)
    {
        System.Collections.Generic.List<string> types = new System.Collections.Generic.List<string>();
        int offset = 8;
        while (offset < png.Length)
        {
            int length = (int)ReadUInt32BigEndian(png, offset);
            types.Add(System.Text.Encoding.ASCII.GetString(png, offset + 4, 4));
            offset = offset + length + 12;
        }

        return types.ToArray();
    }

    private static string ParseInternationalText(byte[] data)
    {
        int separator = System.Array.IndexOf(data, (byte)0);
        Assert.That(separator, Is.GreaterThan(0));
        Assert.That(data[separator + 1], Is.EqualTo(0));
        Assert.That(data[separator + 2], Is.EqualTo(0));
        Assert.That(data[separator + 3], Is.EqualTo(0));
        Assert.That(data[separator + 4], Is.EqualTo(0));
        string keyword = System.Text.Encoding.ASCII.GetString(data, 0, separator);
        string value = System.Text.Encoding.UTF8.GetString(data, separator + 5, data.Length - separator - 5);
        return keyword + "=" + value;
    }

    private static int FindChunkOffset(byte[] png, string requestedType)
    {
        int offset = 8;
        while (offset < png.Length)
        {
            int length = (int)ReadUInt32BigEndian(png, offset);
            if (System.Text.Encoding.ASCII.GetString(png, offset + 4, 4) == requestedType)
            {
                return offset;
            }

            offset = offset + length + 12;
        }

        throw new System.InvalidOperationException("Missing PNG chunk " + requestedType);
    }

    private static byte[] CopyRange(byte[] source, int offset, int length)
    {
        byte[] copy = new byte[length];
        System.Array.Copy(source, offset, copy, 0, length);
        return copy;
    }

    private static int ReadUInt16LittleEndian(byte[] data, int offset)
    {
        return data[offset] | (data[offset + 1] << 8);
    }

    private static int ReadInt32LittleEndian(byte[] data, int offset)
    {
        return data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24);
    }

    private static uint ReadUInt32BigEndian(byte[] data, int offset)
    {
        return ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];
    }

    private static void WriteUInt32BigEndian(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }

    private static void AssertColor(ChartColor color, int red, int green, int blue, int alpha)
    {
        Assert.That(color.R, Is.EqualTo(red));
        Assert.That(color.G, Is.EqualTo(green));
        Assert.That(color.B, Is.EqualTo(blue));
        Assert.That(color.A, Is.EqualTo(alpha));
    }
}
