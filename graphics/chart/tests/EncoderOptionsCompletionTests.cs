namespace chart.Tests;

[TestFixture]
public class EncoderOptionsCompletionTests
{
    [Test]
    public void BmpIndexedOptions_WritePalettePaddingOrientationAndResolution()
    {
        RawBitmap bitmap = new RawBitmapFactory().WrapIndexed(
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
        ChartBmpOptions options = new ChartBmpOptions();
        options.BitDepth = ChartBmpBitDepth.Indexed8;
        options.RowOrientation = ChartBmpRowOrientation.BottomUp;
        options.HorizontalPixelsPerMeter = 3780;
        options.VerticalPixelsPerMeter = 1890;

        byte[] bmp = new ChartExport().ToBmpBytesUsingOptions(bitmap, options);

        Assert.That(BmpEncoder.ReadInt32(bmp, 2), Is.EqualTo(74));
        Assert.That(BmpEncoder.ReadInt32(bmp, 10), Is.EqualTo(66));
        Assert.That(BmpEncoder.ReadInt32(bmp, 22), Is.EqualTo(2));
        Assert.That(BmpEncoder.ReadInt16(bmp, 28), Is.EqualTo(8));
        Assert.That(BmpEncoder.ReadInt32(bmp, 34), Is.EqualTo(8));
        Assert.That(BmpEncoder.ReadInt32(bmp, 38), Is.EqualTo(3780));
        Assert.That(BmpEncoder.ReadInt32(bmp, 42), Is.EqualTo(1890));
        Assert.That(BmpEncoder.ReadInt32(bmp, 46), Is.EqualTo(3));
        Assert.That(new byte[] { bmp[54], bmp[55], bmp[56], bmp[57] }, Is.EqualTo(new byte[] { 30, 20, 10, 0 }));
        Assert.That(new byte[] { bmp[62], bmp[63], bmp[64], bmp[65] }, Is.EqualTo(new byte[] { 90, 80, 70, 0 }));
        Assert.That(new byte[] { bmp[66], bmp[67], bmp[68], bmp[69], bmp[70], bmp[71], bmp[72], bmp[73] }, Is.EqualTo(new byte[] { 2, 1, 0, 0, 0, 1, 2, 0 }));
    }

    [Test]
    public void BmpIndexedOptions_ConvertDirectColorPixelsToGrayscaleIndices()
    {
        RawBitmap bitmap = new RawBitmap(2, 1, ChartPixelFormat.Rgba32);
        bitmap.SetPixel(0, 0, new ChartColor(255, 0, 0, 20));
        bitmap.SetPixel(1, 0, new ChartColor(0, 255, 0, 40));
        ChartBmpOptions options = new ChartBmpOptions();
        options.BitDepth = ChartBmpBitDepth.Indexed8;

        byte[] bmp = BmpEncoder.EncodeWithOptions(bitmap, options);
        int pixelOffset = BmpEncoder.ReadInt32(bmp, 10);

        Assert.That(pixelOffset, Is.EqualTo(1078));
        Assert.That(BmpEncoder.ReadInt32(bmp, 46), Is.EqualTo(256));
        Assert.That(new byte[] { bmp[pixelOffset], bmp[pixelOffset + 1] }, Is.EqualTo(new byte[] { 76, 150 }));
    }

    [TestCase(ChartPixelFormat.Grayscale8, 0)]
    [TestCase(ChartPixelFormat.Rgb24, 2)]
    [TestCase(ChartPixelFormat.Indexed8, 3)]
    [TestCase(ChartPixelFormat.Alpha8, 4)]
    [TestCase(ChartPixelFormat.Rgba32, 6)]
    [TestCase(ChartPixelFormat.Bgra32, 6)]
    public void PngAutomaticColorType_MatchesEveryRawBitmapFormat(ChartPixelFormat pixelFormat, int expectedColorType)
    {
        RawBitmap bitmap = new RawBitmap(1, 1, pixelFormat);
        bitmap.SetPixel(0, 0, new ChartColor(10, 20, 30, 40));
        ChartPngOptions options = new ChartPngOptions();
        options.Filter = ChartPngFilter.None;

        byte[] png = PngEncoder.EncodeWithOptions(bitmap, options);
        byte[] ihdr = GetChunkData(png, "IHDR");

        Assert.That(ihdr[8], Is.EqualTo(8));
        Assert.That(ihdr[9], Is.EqualTo((byte)expectedColorType));
    }

    [Test]
    public void PngExplicitColorTypes_WriteExpectedEightBitSamples()
    {
        RawBitmap bitmap = new RawBitmap(1, 1, ChartPixelFormat.Rgba32);
        bitmap.SetPixel(0, 0, new ChartColor(10, 20, 30, 40));

        Assert.That(EncodePngScanline(bitmap, ChartPngColorType.Grayscale), Is.EqualTo(new byte[] { 0, 18 }));
        Assert.That(EncodePngScanline(bitmap, ChartPngColorType.Truecolor), Is.EqualTo(new byte[] { 0, 10, 20, 30 }));
        Assert.That(EncodePngScanline(bitmap, ChartPngColorType.Indexed), Is.EqualTo(new byte[] { 0, 0 }));
        Assert.That(EncodePngScanline(bitmap, ChartPngColorType.GrayscaleAlpha), Is.EqualTo(new byte[] { 0, 18, 40 }));
        Assert.That(EncodePngScanline(bitmap, ChartPngColorType.TruecolorAlpha), Is.EqualTo(new byte[] { 0, 10, 20, 30, 40 }));
    }

    [Test]
    public void PngIndexedOutput_WritesPaletteTransparencyAndExactIndices()
    {
        RawBitmap bitmap = new RawBitmapFactory().WrapIndexed(
            2,
            1,
            2,
            new byte[] { 1, 0 },
            new ChartColor[]
            {
                new ChartColor(1, 2, 3, 0),
                new ChartColor(10, 20, 30, 128)
            });
        ChartPngOptions options = new ChartPngOptions();
        options.Filter = ChartPngFilter.None;

        byte[] png = new ChartExport().ToPngBytesWithOptions(bitmap, options);

        Assert.That(GetChunkData(png, "PLTE"), Is.EqualTo(new byte[] { 1, 2, 3, 10, 20, 30 }));
        Assert.That(GetChunkData(png, "tRNS"), Is.EqualTo(new byte[] { 0, 128 }));
        Assert.That(DecompressZlib(GetChunkData(png, "IDAT")), Is.EqualTo(new byte[] { 0, 1, 0 }));
        Assert.That(ChunkTypes(png), Is.EqualTo(new string[] { "IHDR", "PLTE", "tRNS", "IDAT", "IEND" }));
    }

    [Test]
    public void PngOptions_WriteUtf8MetadataForcedFilterAndCompressionLevel()
    {
        RawBitmap bitmap = new RawBitmap(2, 1, ChartPixelFormat.Rgba32);
        bitmap.SetPixel(0, 0, new ChartColor(10, 20, 30, 40));
        bitmap.SetPixel(1, 0, new ChartColor(20, 40, 60, 80));
        ChartPngOptions options = new ChartPngOptions();
        options.ColorType = ChartPngColorType.TruecolorAlpha;
        options.Filter = ChartPngFilter.Paeth;
        options.Compression = ChartPngCompression.Fastest;
        options.Metadata.Title = "Résumé";
        options.Metadata.Author = "Chart tests";

        byte[] png = PngEncoder.EncodeWithOptions(bitmap, options);
        byte[] idat = GetChunkData(png, "IDAT");
        byte[][] textChunks = GetChunkDataItems(png, "iTXt");
        byte[] scanlines = DecompressZlib(idat);

        Assert.That(idat[0], Is.EqualTo(120));
        Assert.That(idat[1], Is.EqualTo(1));
        Assert.That(scanlines[0], Is.EqualTo(PngEncoder.FilterPaeth));
        Assert.That(InternationalText(textChunks[0]), Is.EqualTo("Title=Résumé"));
        Assert.That(InternationalText(textChunks[1]), Is.EqualTo("Author=Chart tests"));
        Assert.That(ChunkTypes(png), Is.EqualTo(new string[] { "IHDR", "iTXt", "iTXt", "IDAT", "IEND" }));
    }

    [TestCase(ChartPngCompression.Fastest, 1)]
    [TestCase(ChartPngCompression.Optimal, 156)]
    [TestCase(ChartPngCompression.SmallestSize, 218)]
    [TestCase(ChartPngCompression.NoCompression, 1)]
    public void PngCompressionSettings_WriteValidZlibStreams(ChartPngCompression compression, int expectedHeader)
    {
        byte[] source = new byte[] { 0, 10, 20, 30, 40, 50, 60, 70, 80 };

        byte[] compressed = PngEncoder.CompressZlibWithLevel(source, compression);

        Assert.That(compressed[0], Is.EqualTo(120));
        Assert.That(compressed[1], Is.EqualTo((byte)expectedHeader));
        Assert.That(DecompressZlib(compressed), Is.EqualTo(source));
        Assert.That(PngEncoder.ReadUInt32BigEndian(compressed, compressed.Length - 4), Is.EqualTo(PngEncoder.Adler32(source)));
    }

    [Test]
    public void EncoderOptions_WriteStreamsAndFilesWithoutClosingCallerStreams()
    {
        RawBitmap bitmap = new RawBitmap(1, 1, ChartPixelFormat.Rgba32);
        bitmap.SetPixel(0, 0, new ChartColor(1, 2, 3, 4));
        ChartBmpOptions bmpOptions = new ChartBmpOptions();
        ChartPngOptions pngOptions = new ChartPngOptions();
        ChartExport export = new ChartExport();
        System.IO.MemoryStream bmpStream = new System.IO.MemoryStream();
        System.IO.MemoryStream pngStream = new System.IO.MemoryStream();
        string bmpPath = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, "options-output.bmp");
        string pngPath = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, "options-output.png");

        export.WriteBmp(bitmap, bmpStream, bmpOptions);
        export.WritePng(bitmap, pngStream, pngOptions);
        export.SaveBmpUsingOptions(bitmap, bmpPath, bmpOptions);
        export.SavePngWithOptions(bitmap, pngPath, pngOptions);

        Assert.That(bmpStream.CanWrite, Is.True);
        Assert.That(pngStream.CanWrite, Is.True);
        Assert.That(bmpStream.ToArray(), Is.EqualTo(export.ToBmpBytesUsingOptions(bitmap, bmpOptions)));
        Assert.That(pngStream.ToArray(), Is.EqualTo(export.ToPngBytesWithOptions(bitmap, pngOptions)));
        Assert.That(System.IO.File.ReadAllBytes(bmpPath), Is.EqualTo(bmpStream.ToArray()));
        Assert.That(System.IO.File.ReadAllBytes(pngPath), Is.EqualTo(pngStream.ToArray()));
    }

    [Test]
    public void EncoderOptions_ValidateEnumsMetadataAndDestinations()
    {
        RawBitmap bitmap = new RawBitmap(1, 1, ChartPixelFormat.Rgba32);
        ChartBmpOptions bmp = new ChartBmpOptions();
        ChartPngOptions png = new ChartPngOptions();
        System.IO.MemoryStream readOnly = new System.IO.MemoryStream(new byte[4], false);

        bmp.BitDepth = (ChartBmpBitDepth)9;
        Assert.Throws<System.ArgumentOutOfRangeException>(() => BmpEncoder.EncodeWithOptions(bitmap, bmp));
        bmp.BitDepth = ChartBmpBitDepth.Rgb24;
        bmp.RowOrientation = (ChartBmpRowOrientation)9;
        Assert.Throws<System.ArgumentOutOfRangeException>(() => BmpEncoder.EncodeWithOptions(bitmap, bmp));
        Assert.Throws<System.ArgumentException>(() => BmpEncoder.WriteStreamWithOptions(bitmap, readOnly, new ChartBmpOptions()));

        png.ColorType = (ChartPngColorType)9;
        Assert.Throws<System.ArgumentOutOfRangeException>(() => PngEncoder.EncodeWithOptions(bitmap, png));
        png.ColorType = ChartPngColorType.Automatic;
        png.Filter = (ChartPngFilter)9;
        Assert.Throws<System.ArgumentOutOfRangeException>(() => PngEncoder.EncodeWithOptions(bitmap, png));
        png.Filter = ChartPngFilter.Adaptive;
        png.Compression = (ChartPngCompression)9;
        Assert.Throws<System.ArgumentOutOfRangeException>(() => PngEncoder.EncodeWithOptions(bitmap, png));
        png.Compression = ChartPngCompression.Optimal;
        png.Metadata.Title = "bad\0title";
        Assert.Throws<System.ArgumentException>(() => PngEncoder.EncodeWithOptions(bitmap, png));
        Assert.Throws<System.ArgumentException>(() => PngEncoder.WriteStreamWithOptions(bitmap, readOnly, new ChartPngOptions()));
        Assert.Throws<System.ArgumentNullException>(() => new ChartExport().ToPngBytesWithOptions(bitmap, null!));
    }

    static byte[] EncodePngScanline(RawBitmap bitmap, ChartPngColorType colorType)
    {
        ChartPngOptions options = new ChartPngOptions();
        options.ColorType = colorType;
        options.Filter = ChartPngFilter.None;
        options.Compression = ChartPngCompression.NoCompression;
        return DecompressZlib(GetChunkData(PngEncoder.EncodeWithOptions(bitmap, options), "IDAT"));
    }

    static byte[] DecompressZlib(byte[] data)
    {
        System.IO.MemoryStream input = new System.IO.MemoryStream(data);
        System.IO.MemoryStream output = new System.IO.MemoryStream();
        using (System.IO.Compression.ZLibStream zlib = new System.IO.Compression.ZLibStream(input, System.IO.Compression.CompressionMode.Decompress))
        {
            zlib.CopyTo(output);
        }

        return output.ToArray();
    }

    static byte[] GetChunkData(byte[] png, string requestedType)
    {
        byte[][] items = GetChunkDataItems(png, requestedType);
        Assert.That(items.Length, Is.GreaterThan(0), "missing PNG chunk " + requestedType);
        return items[0];
    }

    static byte[][] GetChunkDataItems(byte[] png, string requestedType)
    {
        System.Collections.Generic.List<byte[]> items = new System.Collections.Generic.List<byte[]>();
        int offset = 8;
        while (offset < png.Length)
        {
            int length = (int)PngEncoder.ReadUInt32BigEndian(png, offset);
            string type = System.Text.Encoding.ASCII.GetString(png, offset + 4, 4);
            if (type == requestedType)
            {
                byte[] data = new byte[length];
                System.Array.Copy(png, offset + 8, data, 0, length);
                items.Add(data);
            }

            offset = offset + length + 12;
        }

        return items.ToArray();
    }

    static string[] ChunkTypes(byte[] png)
    {
        System.Collections.Generic.List<string> types = new System.Collections.Generic.List<string>();
        int offset = 8;
        while (offset < png.Length)
        {
            int length = (int)PngEncoder.ReadUInt32BigEndian(png, offset);
            types.Add(System.Text.Encoding.ASCII.GetString(png, offset + 4, 4));
            offset = offset + length + 12;
        }

        return types.ToArray();
    }

    static string InternationalText(byte[] data)
    {
        int separator = System.Array.IndexOf(data, (byte)0);
        string keyword = System.Text.Encoding.ASCII.GetString(data, 0, separator);
        string value = System.Text.Encoding.UTF8.GetString(data, separator + 5, data.Length - separator - 5);
        return keyword + "=" + value;
    }
}
