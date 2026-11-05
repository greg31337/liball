namespace svg.Tests;

public class SvgPngCodecTests
{
    [Test]
    public void DecodesEveryRequiredColorTypeAndBitDepth()
    {
        byte[] palette = [0, 0, 0, 0, 255, 0];
        byte[] paletteAlpha = [255, 128];

        AssertPixel(Decode(1, 0, [0, 128]), 255, 255, 255, 255);
        AssertPixel(Decode(2, 0, [0, 128]), 170, 170, 170, 255);
        AssertPixel(Decode(4, 0, [0, 128]), 136, 136, 136, 255);
        AssertPixel(Decode(8, 0, [0, 128]), 128, 128, 128, 255);
        AssertPixel(Decode(16, 0, [0, 128, 0]), 128, 128, 128, 255);
        AssertPixel(Decode(8, 2, [0, 255, 0, 0]), 255, 0, 0, 255);
        AssertPixel(Decode(16, 2, [0, 255, 255, 0, 0, 128, 0]), 255, 0, 128, 255);
        AssertPixel(Decode(1, 3, [0, 128], palette, paletteAlpha), 0, 128, 0, 128);
        AssertPixel(Decode(2, 3, [0, 64], palette, paletteAlpha), 0, 128, 0, 128);
        AssertPixel(Decode(4, 3, [0, 16], palette, paletteAlpha), 0, 128, 0, 128);
        AssertPixel(Decode(8, 3, [0, 1], palette, paletteAlpha), 0, 128, 0, 128);
        AssertPixel(Decode(8, 4, [0, 200, 128]), 100, 100, 100, 128);
        AssertPixel(Decode(16, 4, [0, 255, 255, 128, 0]), 128, 128, 128, 128);
        AssertPixel(Decode(8, 6, [0, 255, 0, 128, 128]), 128, 0, 64, 128);
        SvgPngDecodeResult rgba16 = Decode(16, 6, [0, 255, 255, 128, 0, 0, 0, 128, 0]);
        AssertPixel(rgba16, 128, 64, 0, 128);

        Assert.Multiple(() =>
        {
            Assert.That(rgba16.Success, Is.True);
            Assert.That(rgba16.BitDepth, Is.EqualTo(16));
            Assert.That(rgba16.ColorType, Is.EqualTo(6));
            Assert.That(rgba16.InterlaceMethod, Is.EqualTo(0));
            Assert.That(rgba16.Pixels.ColorSpace, Is.EqualTo(SvgPixelColorSpace.Srgb));
        });
    }

    [Test]
    public void AppliesGrayscaleTruecolorAndPaletteTransparency()
    {
        SvgPngDecodeResult gray = Decode(8, 0, [0, 128], null, [0, 128]);
        SvgPngDecodeResult rgb = Decode(8, 2, [0, 255, 0, 0], null, [0, 255, 0, 0, 0, 0]);
        SvgPngDecodeResult indexed = Decode(8, 3, [0, 1], [0, 0, 0, 0, 0, 255], [255, 0]);

        AssertPixel(gray, 0, 0, 0, 0);
        AssertPixel(rgb, 0, 0, 0, 0);
        AssertPixel(indexed, 0, 0, 0, 0);
    }

    [Test]
    public void ReconstructsAllFiveScanlineFilters()
    {
        byte[][] rows = new byte[5][];
        int row = 0;
        while (row < rows.Length)
        {
            rows[row] = [(byte)(10 + row), (byte)(20 + row), (byte)(30 + row), 255, (byte)(100 + row), (byte)(110 + row), (byte)(120 + row), 255];
            row = row + 1;
        }
        byte[] filtered = FilterRows(rows, 4, [0, 1, 2, 3, 4]);
        SvgPngDecodeResult result = new SvgPngCodec().DecodeWithBytes(Png(2, 5, 8, 6, 0, filtered, null, null));

        Assert.That(result.Success, Is.True);
        row = 0;
        while (row < rows.Length)
        {
            AssertPixel(result, 0, row, rows[row][0], rows[row][1], rows[row][2], 255);
            AssertPixel(result, 1, row, rows[row][4], rows[row][5], rows[row][6], 255);
            row = row + 1;
        }
    }

    [Test]
    public void ReconstructsAdam7PassesIntoFinalPixelPositions()
    {
        byte[] pixels = new byte[4 * 5 * 5];
        int y = 0;
        while (y < 5)
        {
            int x = 0;
            while (x < 5)
            {
                int offset = (y * 5 + x) * 4;
                pixels[offset] = (byte)(x * 30 + y * 3);
                pixels[offset + 1] = (byte)(y * 40);
                pixels[offset + 2] = (byte)(x * 20);
                pixels[offset + 3] = 255;
                x = x + 1;
            }
            y = y + 1;
        }
        byte[] adam7 = Adam7Rgba(pixels, 5, 5);

        SvgPngDecodeResult result = new SvgPngCodec().DecodeWithBytes(Png(5, 5, 8, 6, 1, adam7, null, null));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.InterlaceMethod, Is.EqualTo(1));
            AssertPixel(result, 0, 0, 0, 0, 0, 255);
            AssertPixel(result, 4, 0, 120, 0, 80, 255);
            AssertPixel(result, 0, 4, 12, 160, 0, 255);
            AssertPixel(result, 4, 4, 132, 160, 80, 255);
            AssertPixel(result, 2, 3, 69, 120, 40, 255);
        });
    }

    [Test]
    public void EnforcesChunkOrderCrcPaletteAndCompressionIntegrity()
    {
        byte[] valid = Png(1, 1, 8, 6, 0, [0, 1, 2, 3, 255], null, null);
        byte[] badCrc = (byte[])valid.Clone();
        int dataOffset = FindChunk(badCrc, "IDAT");
        badCrc[dataOffset + ChunkLength(badCrc, dataOffset) + 11] = (byte)(badCrc[dataOffset + ChunkLength(badCrc, dataOffset) + 11] ^ 1);
        byte[] invalidZlib = (byte[])valid.Clone();
        dataOffset = FindChunk(invalidZlib, "IDAT");
        invalidZlib[dataOffset + 8] = (byte)(invalidZlib[dataOffset + 8] ^ 64);
        RewriteChunkCrc(invalidZlib, dataOffset);
        byte[] missingPalette = Png(1, 1, 8, 3, 0, [0, 0], null, null);
        byte[] indexed = Png(1, 1, 8, 3, 0, [0, 0], [10, 20, 30], null);
        byte[] latePalette = MovePaletteAfterData(indexed);
        byte[] separatedData = SeparatedDataPng([0, 1, 2, 3, 255]);
        byte[] unknownCritical = (byte[])valid.Clone();
        dataOffset = FindChunk(unknownCritical, "IDAT");
        unknownCritical[dataOffset + 4] = 65;
        RewriteChunkCrc(unknownCritical, dataOffset);
        byte[] invalidFilter = Png(1, 1, 8, 6, 0, [5, 1, 2, 3, 255], null, null);
        byte[] invalidIndex = Png(1, 1, 8, 3, 0, [0, 1], [10, 20, 30], null);
        byte[] missingEnd = new byte[valid.Length - 12];
        System.Array.Copy(valid, missingEnd, missingEnd.Length);

        SvgPngDecodeResult crc = new SvgPngCodec().DecodeWithBytes(badCrc);
        SvgPngDecodeResult zlib = new SvgPngCodec().DecodeWithBytes(invalidZlib);
        SvgPngDecodeResult palette = new SvgPngCodec().DecodeWithBytes(missingPalette);
        SvgPngDecodeResult late = new SvgPngCodec().DecodeWithBytes(latePalette);
        SvgPngDecodeResult separated = new SvgPngCodec().DecodeWithBytes(separatedData);
        SvgPngDecodeResult critical = new SvgPngCodec().DecodeWithBytes(unknownCritical);
        SvgPngDecodeResult filter = new SvgPngCodec().DecodeWithBytes(invalidFilter);
        SvgPngDecodeResult index = new SvgPngCodec().DecodeWithBytes(invalidIndex);
        SvgPngDecodeResult end = new SvgPngCodec().DecodeWithBytes(missingEnd);

        Assert.Multiple(() =>
        {
            Assert.That(crc.Error, Is.EqualTo(SvgPngDecodeError.CrcMismatch));
            Assert.That(zlib.Error, Is.EqualTo(SvgPngDecodeError.DecompressionFailed));
            Assert.That(palette.Error, Is.EqualTo(SvgPngDecodeError.InvalidPalette));
            Assert.That(late.Error, Is.EqualTo(SvgPngDecodeError.InvalidPalette));
            Assert.That(separated.Error, Is.EqualTo(SvgPngDecodeError.InvalidChunk));
            Assert.That(critical.Error, Is.EqualTo(SvgPngDecodeError.InvalidChunk));
            Assert.That(filter.Error, Is.EqualTo(SvgPngDecodeError.InvalidScanline));
            Assert.That(index.Error, Is.EqualTo(SvgPngDecodeError.InvalidPalette));
            Assert.That(end.Error, Is.EqualTo(SvgPngDecodeError.InvalidChunk));
        });
    }

    [Test]
    public void EnforcesChunkDimensionOutputAndInflateLimits()
    {
        byte[] png = Png(2, 2, 8, 6, 0, [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], null, null);
        SvgRasterLimits chunkLimits = new SvgRasterLimits();
        chunkLimits.MaxPngChunks = 2;
        SvgRasterLimits dimensionLimits = new SvgRasterLimits();
        dimensionLimits.MaxWidth = 1;
        SvgRasterLimits inflateLimits = new SvgRasterLimits();
        inflateLimits.MaxPngDecodedBytes = 17;
        SvgRasterLimits byteLimits = new SvgRasterLimits();
        byteLimits.MaxPixelBytes = 15;

        SvgPngDecodeResult chunks = new SvgPngCodec().DecodeWithBytesAndLimits(png, chunkLimits);
        SvgPngDecodeResult dimensions = new SvgPngCodec().DecodeWithBytesAndLimits(png, dimensionLimits);
        SvgPngDecodeResult inflated = new SvgPngCodec().DecodeWithBytesAndLimits(png, inflateLimits);
        SvgPngDecodeResult bytes = new SvgPngCodec().DecodeWithBytesAndLimits(png, byteLimits);

        Assert.Multiple(() =>
        {
            Assert.That(chunks.Error, Is.EqualTo(SvgPngDecodeError.ChunkLimitExceeded));
            Assert.That(dimensions.Error, Is.EqualTo(SvgPngDecodeError.DimensionLimitExceeded));
            Assert.That(inflated.Error, Is.EqualTo(SvgPngDecodeError.DecodedSizeLimitExceeded));
            Assert.That(bytes.Error, Is.EqualTo(SvgPngDecodeError.DimensionLimitExceeded));
        });
    }

    [Test]
    public void SceneConstructionDecodesPortablePngResources()
    {
        byte[] png = Png(1, 1, 8, 6, 0, [0, 200, 100, 50, 128], null, null);
        string source = "<svg xmlns='http://www.w3.org/2000/svg' width='1' height='1'><image width='1' height='1' href='data:image/png;base64," + System.Convert.ToBase64String(png) + "'/></svg>";
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(loaded.Success, Is.True);
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(loaded.Document);
        Assert.That(resolved.Success, Is.True);

        SvgSceneBuildResult built = resolved.BuildScene();

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            Assert.That(built.Scene.Resources, Has.Count.EqualTo(1));
            Assert.That(built.Scene.Resources[0].Image.DecodedContentAvailable, Is.True);
            AssertPixel(built.Scene.Resources[0].Image.DecodedPixels, 100, 50, 25, 128);
        });
    }

    static SvgPngDecodeResult Decode(int bitDepth, int colorType, byte[] scanlines)
    {
        return Decode(bitDepth, colorType, scanlines, null, null);
    }

    static SvgPngDecodeResult Decode(int bitDepth, int colorType, byte[] scanlines, byte[] palette, byte[] transparency)
    {
        return new SvgPngCodec().DecodeWithBytes(Png(1, 1, bitDepth, colorType, 0, scanlines, palette, transparency));
    }

    static void AssertPixel(SvgPngDecodeResult result, byte red, byte green, byte blue, byte alpha)
    {
        AssertPixel(result, 0, 0, red, green, blue, alpha);
    }

    static void AssertPixel(SvgPixelBuffer pixels, byte red, byte green, byte blue, byte alpha)
    {
        SvgPixelReadResult pixel = pixels.Read(0, 0);
        Assert.That(pixel.Success, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(pixel.Pixel.Red, Is.EqualTo(red));
            Assert.That(pixel.Pixel.Green, Is.EqualTo(green));
            Assert.That(pixel.Pixel.Blue, Is.EqualTo(blue));
            Assert.That(pixel.Pixel.Alpha, Is.EqualTo(alpha));
        });
    }

    static void AssertPixel(SvgPngDecodeResult result, int x, int y, byte red, byte green, byte blue, byte alpha)
    {
        Assert.That(result.Success, Is.True, "PNG decode failed with " + result.Error);
        SvgPixelReadResult pixel = result.Pixels.Read(x, y);
        Assert.That(pixel.Success, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(pixel.Pixel.Red, Is.EqualTo(red));
            Assert.That(pixel.Pixel.Green, Is.EqualTo(green));
            Assert.That(pixel.Pixel.Blue, Is.EqualTo(blue));
            Assert.That(pixel.Pixel.Alpha, Is.EqualTo(alpha));
        });
    }

    static byte[] Png(int width, int height, int bitDepth, int colorType, int interlace, byte[] scanlines, byte[] palette, byte[] transparency)
    {
        List<byte> result = new List<byte>([137, 80, 78, 71, 13, 10, 26, 10]);
        byte[] header = new byte[13];
        WriteUInt32(header, 0, (uint)width);
        WriteUInt32(header, 4, (uint)height);
        header[8] = (byte)bitDepth;
        header[9] = (byte)colorType;
        header[12] = (byte)interlace;
        result.AddRange(Chunk("IHDR", header));
        if (palette != null) result.AddRange(Chunk("PLTE", palette));
        if (transparency != null) result.AddRange(Chunk("tRNS", transparency));
        result.AddRange(Chunk("IDAT", Compress(scanlines)));
        result.AddRange(Chunk("IEND", []));
        return result.ToArray();
    }

    static byte[] Compress(byte[] bytes)
    {
        using MemoryStream output = new MemoryStream();
        using (System.IO.Compression.ZLibStream zlib = new System.IO.Compression.ZLibStream(output, System.IO.Compression.CompressionLevel.SmallestSize, true))
        {
            zlib.Write(bytes, 0, bytes.Length);
        }
        return output.ToArray();
    }

    static byte[] Chunk(string type, byte[] data)
    {
        byte[] result = new byte[data.Length + 12];
        WriteUInt32(result, 0, (uint)data.Length);
        int index = 0;
        while (index < 4)
        {
            result[4 + index] = (byte)type[index];
            index = index + 1;
        }
        System.Array.Copy(data, 0, result, 8, data.Length);
        WriteUInt32(result, result.Length - 4, Crc(result, 4, data.Length + 4));
        return result;
    }

    static byte[] FilterRows(byte[][] rows, int bytesPerPixel, int[] filters)
    {
        List<byte> result = new List<byte>();
        int rowIndex = 0;
        while (rowIndex < rows.Length)
        {
            int filter = filters[rowIndex];
            result.Add((byte)filter);
            int index = 0;
            while (index < rows[rowIndex].Length)
            {
                int left = index < bytesPerPixel ? 0 : rows[rowIndex][index - bytesPerPixel];
                int above = rowIndex == 0 ? 0 : rows[rowIndex - 1][index];
                int upperLeft = rowIndex == 0 || index < bytesPerPixel ? 0 : rows[rowIndex - 1][index - bytesPerPixel];
                int predictor = TestPredictor(filter, left, above, upperLeft);
                result.Add((byte)(rows[rowIndex][index] - predictor));
                index = index + 1;
            }
            rowIndex = rowIndex + 1;
        }
        return result.ToArray();
    }

    static int TestPredictor(int filter, int left, int above, int upperLeft)
    {
        if (filter == 0) return 0;
        if (filter == 1) return left;
        if (filter == 2) return above;
        if (filter == 3) return (left + above) / 2;
        int estimate = left + above - upperLeft;
        int leftDistance = System.Math.Abs(estimate - left);
        int aboveDistance = System.Math.Abs(estimate - above);
        int upperLeftDistance = System.Math.Abs(estimate - upperLeft);
        if (leftDistance <= aboveDistance && leftDistance <= upperLeftDistance) return left;
        return aboveDistance <= upperLeftDistance ? above : upperLeft;
    }

    static byte[] Adam7Rgba(byte[] pixels, int width, int height)
    {
        int[] startsX = [0, 4, 0, 2, 0, 1, 0];
        int[] startsY = [0, 0, 4, 0, 2, 0, 1];
        int[] stepsX = [8, 8, 4, 4, 2, 2, 1];
        int[] stepsY = [8, 8, 8, 4, 4, 2, 2];
        List<byte> result = new List<byte>();
        int pass = 0;
        while (pass < 7)
        {
            int y = startsY[pass];
            while (y < height)
            {
                if (startsX[pass] < width) result.Add(0);
                int x = startsX[pass];
                while (x < width)
                {
                    int offset = (y * width + x) * 4;
                    result.Add(pixels[offset]);
                    result.Add(pixels[offset + 1]);
                    result.Add(pixels[offset + 2]);
                    result.Add(pixels[offset + 3]);
                    x = x + stepsX[pass];
                }
                y = y + stepsY[pass];
            }
            pass = pass + 1;
        }
        return result.ToArray();
    }

    static byte[] MovePaletteAfterData(byte[] png)
    {
        int paletteOffset = FindChunk(png, "PLTE");
        int dataOffset = FindChunk(png, "IDAT");
        int paletteLength = ChunkLength(png, paletteOffset) + 12;
        int dataLength = ChunkLength(png, dataOffset) + 12;
        byte[] result = new byte[png.Length];
        System.Array.Copy(png, 0, result, 0, paletteOffset);
        System.Array.Copy(png, dataOffset, result, paletteOffset, dataLength);
        System.Array.Copy(png, paletteOffset, result, paletteOffset + dataLength, paletteLength);
        int sourceEnd = dataOffset + dataLength;
        int destinationEnd = paletteOffset + dataLength + paletteLength;
        System.Array.Copy(png, sourceEnd, result, destinationEnd, png.Length - sourceEnd);
        return result;
    }

    static byte[] SeparatedDataPng(byte[] scanlines)
    {
        byte[] valid = Png(1, 1, 8, 6, 0, scanlines, null, null);
        int dataOffset = FindChunk(valid, "IDAT");
        int dataLength = ChunkLength(valid, dataOffset);
        byte[] compressed = new byte[dataLength];
        System.Array.Copy(valid, dataOffset + 8, compressed, 0, dataLength);
        int split = compressed.Length / 2;
        byte[] first = new byte[split];
        byte[] second = new byte[compressed.Length - split];
        System.Array.Copy(compressed, 0, first, 0, first.Length);
        System.Array.Copy(compressed, split, second, 0, second.Length);
        List<byte> result = new List<byte>();
        System.Array.ForEach(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, value => result.Add(value));
        result.AddRange(Chunk("IHDR", Header(1, 1, 8, 6, 0)));
        result.AddRange(Chunk("IDAT", first));
        result.AddRange(Chunk("tEXt", []));
        result.AddRange(Chunk("IDAT", second));
        result.AddRange(Chunk("IEND", []));
        return result.ToArray();
    }

    static byte[] Header(int width, int height, int bitDepth, int colorType, int interlace)
    {
        byte[] header = new byte[13];
        WriteUInt32(header, 0, (uint)width);
        WriteUInt32(header, 4, (uint)height);
        header[8] = (byte)bitDepth;
        header[9] = (byte)colorType;
        header[12] = (byte)interlace;
        return header;
    }

    static int FindChunk(byte[] png, string type)
    {
        int offset = 8;
        while (offset < png.Length)
        {
            if (png[offset + 4] == type[0] && png[offset + 5] == type[1] && png[offset + 6] == type[2] && png[offset + 7] == type[3]) return offset;
            offset = offset + ChunkLength(png, offset) + 12;
        }
        return -1;
    }

    static int ChunkLength(byte[] bytes, int offset)
    {
        return (int)ReadUInt32(bytes, offset);
    }

    static void RewriteChunkCrc(byte[] bytes, int offset)
    {
        int length = ChunkLength(bytes, offset);
        WriteUInt32(bytes, offset + length + 8, Crc(bytes, offset + 4, length + 4));
    }

    static uint Crc(byte[] bytes, int offset, int count)
    {
        uint crc = 4294967295;
        int index = offset;
        while (index < offset + count)
        {
            crc = crc ^ bytes[index];
            int bit = 0;
            while (bit < 8)
            {
                crc = (crc & 1) == 0 ? crc >> 1 : 3988292384 ^ crc >> 1;
                bit = bit + 1;
            }
            index = index + 1;
        }
        return crc ^ 4294967295;
    }

    static uint ReadUInt32(byte[] bytes, int offset)
    {
        return (uint)bytes[offset] << 24 | (uint)bytes[offset + 1] << 16 | (uint)bytes[offset + 2] << 8 | bytes[offset + 3];
    }

    static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

}
