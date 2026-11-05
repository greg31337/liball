namespace chart.Tests;

[TestFixture]
public class PngEncoderTests
{
    [Test]
    public void Crc32AndAdler32_MatchKnownVectors()
    {
        byte[] data = System.Text.Encoding.ASCII.GetBytes("123456789");
        byte[] wikipedia = System.Text.Encoding.ASCII.GetBytes("Wikipedia");

        Assert.That(PngEncoder.Crc32(data), Is.EqualTo(0xcbf43926));
        Assert.That(PngEncoder.Adler32(wikipedia), Is.EqualTo(0x11e60398));
    }

    [Test]
    public void CreateIhdrData_WritesExpectedRgbaHeader()
    {
        byte[] data = PngEncoder.CreateIhdrData(300, 200);

        Assert.That(PngEncoder.ReadUInt32BigEndian(data, 0), Is.EqualTo(300));
        Assert.That(PngEncoder.ReadUInt32BigEndian(data, 4), Is.EqualTo(200));
        Assert.That(data[8], Is.EqualTo(8));
        Assert.That(data[9], Is.EqualTo(6));
        Assert.That(data[10], Is.EqualTo(0));
        Assert.That(data[11], Is.EqualTo(0));
        Assert.That(data[12], Is.EqualTo(0));
    }

    [Test]
    public void WriteChunk_WritesLengthTypeDataAndCrc()
    {
        System.IO.MemoryStream stream = new System.IO.MemoryStream();
        byte[] data = new byte[] { 1, 2, 3 };

        PngEncoder.WriteChunk(stream, "tEXt", data);
        byte[] bytes = stream.ToArray();

        Assert.That(PngEncoder.ReadUInt32BigEndian(bytes, 0), Is.EqualTo(3));
        Assert.That(System.Text.Encoding.ASCII.GetString(bytes, 4, 4), Is.EqualTo("tEXt"));
        Assert.That(bytes[8], Is.EqualTo(1));
        Assert.That(bytes[9], Is.EqualTo(2));
        Assert.That(bytes[10], Is.EqualTo(3));
        Assert.That(PngEncoder.ReadUInt32BigEndian(bytes, 11), Is.EqualTo(PngEncoder.Crc32(new byte[] { 116, 69, 88, 116, 1, 2, 3 })));
    }

    [Test]
    public void CreateRgbaScanlines_PrependsFilterByteAndWritesPixelsTopDown()
    {
        RawBitmap bitmap = new RawBitmap(2, 1, ChartPixelFormat.Rgba32);
        bitmap.SetPixel(0, 0, new ChartColor(10, 20, 30, 40));
        bitmap.SetPixel(1, 0, new ChartColor(50, 60, 70, 80));

        byte[] data = PngEncoder.CreateRgbaScanlines(bitmap);

        Assert.That(data, Is.EqualTo(new byte[] { 0, 10, 20, 30, 40, 50, 60, 70, 80 }));
    }

    [Test]
    public void CreateFilteredScanlinesNone_WritesKnownTinyImageRows()
    {
        RawBitmap bitmap = CreateKnownTwoByTwoBitmap();
        byte[] expected = KnownTwoByTwoFilterNoneScanlines();

        Assert.That(PngEncoder.CreateFilteredScanlinesNone(bitmap), Is.EqualTo(expected));
        Assert.That(PngEncoder.CreateFilteredScanlines(bitmap, PngEncoder.FilterNone), Is.EqualTo(expected));
        Assert.That(PngEncoder.CreateRgbaScanlines(bitmap), Is.EqualTo(expected));
    }

    [Test]
    public void CreateFilteredScanlines_AppliesSubUpAverageAndPaeth()
    {
        RawBitmap bitmap = CreateKnownTwoByTwoBitmap();

        Assert.That(PngEncoder.CreateFilteredScanlines(bitmap, PngEncoder.FilterSub), Is.EqualTo(KnownTwoByTwoFilterSubScanlines()));
        Assert.That(PngEncoder.CreateFilteredScanlines(bitmap, PngEncoder.FilterUp), Is.EqualTo(KnownTwoByTwoFilterUpScanlines()));
        Assert.That(PngEncoder.CreateFilteredScanlines(bitmap, PngEncoder.FilterAverage), Is.EqualTo(KnownTwoByTwoFilterAverageScanlines()));
        Assert.That(PngEncoder.CreateFilteredScanlines(bitmap, PngEncoder.FilterPaeth), Is.EqualTo(KnownTwoByTwoFilterPaethScanlines()));
    }

    [Test]
    public void CreateAdaptiveFilteredScanlines_SelectsLowestScorePerRow()
    {
        RawBitmap bitmap = CreateAdaptiveFilterBitmap();
        byte[] filtered = PngEncoder.CreateAdaptiveFilteredScanlines(bitmap);

        Assert.That(filtered, Is.EqualTo(new byte[]
        {
            1, 10, 20, 30, 40, 10, 20, 30, 40,
            2, 0, 0, 0, 0, 0, 0, 0, 0
        }));
        Assert.That(UnfilterRgbaScanlines(filtered, 2, 2), Is.EqualTo(KnownAdaptiveRgbaRows()));
    }

    [Test]
    public void CompressZlib_WrapsDeflateDataWithHeaderAndAdler()
    {
        byte[] data = new byte[] { 0, 10, 20, 30, 40 };

        byte[] compressed = PngEncoder.CompressZlib(data);
        byte[] decompressed = DecompressZlib(compressed);

        Assert.That(compressed[0], Is.EqualTo(120));
        Assert.That(compressed[1], Is.EqualTo(156));
        Assert.That(PngEncoder.ReadUInt32BigEndian(compressed, compressed.Length - 4), Is.EqualTo(PngEncoder.Adler32(data)));
        Assert.That(decompressed, Is.EqualTo(data));
    }

    [Test]
    public void Encode_WritesPngSignatureChunksAndCompressedImageData()
    {
        RawBitmap bitmap = new RawBitmap(1, 1, ChartPixelFormat.Rgba32);
        bitmap.SetPixel(0, 0, new ChartColor(10, 20, 30, 40));

        byte[] png = PngEncoder.Encode(bitmap);
        byte[] idat = GetChunkData(png, "IDAT");
        byte[] scanlines = DecompressZlib(idat);

        Assert.That(new byte[] { png[0], png[1], png[2], png[3], png[4], png[5], png[6], png[7] }, Is.EqualTo(PngEncoder.Signature()));
        Assert.That(GetChunkType(png, 8), Is.EqualTo("IHDR"));
        Assert.That(GetChunkType(png, NextChunkOffset(png, 8)), Is.EqualTo("IDAT"));
        Assert.That(GetChunkType(png, NextChunkOffset(png, NextChunkOffset(png, 8))), Is.EqualTo("IEND"));
        Assert.That(PngEncoder.ReadUInt32BigEndian(GetChunkData(png, "IHDR"), 0), Is.EqualTo(1));
        Assert.That(scanlines, Is.EqualTo(new byte[] { 0, 10, 20, 30, 40 }));
    }

    [Test]
    public void Encode_WritesValidChunkCrcsAndIdatAdlerChecksum()
    {
        RawBitmap bitmap = CreateKnownTwoByTwoBitmap();

        byte[] png = PngEncoder.Encode(bitmap);
        byte[] idat = GetChunkData(png, "IDAT");
        byte[] scanlines = DecompressZlib(idat);

        AssertAllChunkCrcsValid(png);
        Assert.That(PngEncoder.ReadUInt32BigEndian(idat, idat.Length - 4), Is.EqualTo(PngEncoder.Adler32(scanlines)));
        Assert.That(UnfilterRgbaScanlines(scanlines, 2, 2), Is.EqualTo(KnownTwoByTwoRgbaRows()));
    }

    [Test]
    public void EncodedPng_IdatPayloadDecodesWithBuiltInZlibStream()
    {
        RawBitmap bitmap = CreateAdaptiveFilterBitmap();

        byte[] png = PngEncoder.Encode(bitmap);
        byte[] scanlines = DecompressZlib(GetChunkData(png, "IDAT"));

        AssertFilterRowsAreValid(scanlines, bitmap.Width, bitmap.Height);
        Assert.That(UnfilterRgbaScanlines(scanlines, bitmap.Width, bitmap.Height), Is.EqualTo(KnownAdaptiveRgbaRows()));
    }

    [Test]
    public void Encode_KnownTinyImagesDecodeToFilterNoneScanlines()
    {
        RawBitmap transparent = new RawBitmap(1, 1, ChartPixelFormat.Rgba32);
        transparent.SetPixel(0, 0, new ChartColor(0, 0, 0, 0));

        AssertEncodedTinyImage(transparent, new byte[] { 0, 0, 0, 0 });
        AssertEncodedTinyImage(CreateKnownTwoByTwoBitmap(), KnownTwoByTwoRgbaRows());
    }

    [Test]
    public void PngEncoder_ValidatesInputs()
    {
        Assert.Throws<System.ArgumentNullException>(() => PngEncoder.Encode(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => PngEncoder.CreateIhdrData(0, 1));
        Assert.Throws<System.ArgumentNullException>(() => PngEncoder.CreateRgbaScanlines(null!));
        Assert.Throws<System.ArgumentNullException>(() => PngEncoder.CreateFilteredScanlines(null!, PngEncoder.FilterNone));
        Assert.Throws<System.ArgumentException>(() => PngEncoder.CreateFilteredScanlines(new RawBitmap(1, 1, ChartPixelFormat.Rgba32), 5));
        Assert.Throws<System.ArgumentNullException>(() => PngEncoder.CreateAdaptiveFilteredScanlines(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => PngEncoder.ScoreFilteredRow(new byte[4], new byte[4], PngEncoder.FilterNone, 0));
        Assert.Throws<System.ArgumentException>(() => PngEncoder.ScoreFilteredRow(new byte[4], new byte[3], PngEncoder.FilterNone, 4));
        Assert.Throws<System.ArgumentNullException>(() => PngEncoder.CompressZlib(null!));
        Assert.Throws<System.ArgumentNullException>(() => PngEncoder.WriteChunk(null!, "IHDR", new byte[0]));
        Assert.Throws<System.ArgumentException>(() => PngEncoder.WriteChunk(new System.IO.MemoryStream(), "BAD", new byte[0]));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => PngEncoder.CalculateRgbaRowLength(int.MaxValue));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => PngEncoder.CalculateScanlineDataLength(int.MaxValue, 2));
    }

    static RawBitmap CreateKnownTwoByTwoBitmap()
    {
        RawBitmap bitmap = new RawBitmap(2, 2, ChartPixelFormat.Rgba32);
        bitmap.SetPixel(0, 0, new ChartColor(255, 0, 0, 255));
        bitmap.SetPixel(1, 0, new ChartColor(0, 255, 0, 128));
        bitmap.SetPixel(0, 1, new ChartColor(0, 0, 255, 64));
        bitmap.SetPixel(1, 1, new ChartColor(255, 255, 255, 0));
        return bitmap;
    }

    static RawBitmap CreateAdaptiveFilterBitmap()
    {
        RawBitmap bitmap = new RawBitmap(2, 2, ChartPixelFormat.Rgba32);
        bitmap.SetPixel(0, 0, new ChartColor(10, 20, 30, 40));
        bitmap.SetPixel(1, 0, new ChartColor(20, 40, 60, 80));
        bitmap.SetPixel(0, 1, new ChartColor(10, 20, 30, 40));
        bitmap.SetPixel(1, 1, new ChartColor(20, 40, 60, 80));
        return bitmap;
    }

    static byte[] KnownTwoByTwoRgbaRows()
    {
        return new byte[]
        {
            255, 0, 0, 255, 0, 255, 0, 128,
            0, 0, 255, 64, 255, 255, 255, 0
        };
    }

    static byte[] KnownTwoByTwoFilterNoneScanlines()
    {
        return new byte[]
        {
            0, 255, 0, 0, 255, 0, 255, 0, 128,
            0, 0, 0, 255, 64, 255, 255, 255, 0
        };
    }

    static byte[] KnownTwoByTwoFilterSubScanlines()
    {
        return new byte[]
        {
            1, 255, 0, 0, 255, 1, 255, 0, 129,
            1, 0, 0, 255, 64, 255, 255, 0, 192
        };
    }

    static byte[] KnownTwoByTwoFilterUpScanlines()
    {
        return new byte[]
        {
            2, 255, 0, 0, 255, 0, 255, 0, 128,
            2, 1, 0, 255, 65, 255, 0, 255, 128
        };
    }

    static byte[] KnownTwoByTwoFilterAverageScanlines()
    {
        return new byte[]
        {
            3, 255, 0, 0, 255, 129, 255, 0, 1,
            3, 129, 0, 255, 193, 255, 128, 128, 160
        };
    }

    static byte[] KnownTwoByTwoFilterPaethScanlines()
    {
        return new byte[]
        {
            4, 255, 0, 0, 255, 1, 255, 0, 129,
            4, 1, 0, 255, 65, 255, 0, 0, 192
        };
    }

    static byte[] KnownAdaptiveRgbaRows()
    {
        return new byte[]
        {
            10, 20, 30, 40, 20, 40, 60, 80,
            10, 20, 30, 40, 20, 40, 60, 80
        };
    }

    static void AssertEncodedTinyImage(RawBitmap bitmap, byte[] expectedRgbaRows)
    {
        byte[] png = PngEncoder.Encode(bitmap);
        byte[] ihdr = GetChunkData(png, "IHDR");
        byte[] idat = GetChunkData(png, "IDAT");
        byte[] scanlines = DecompressZlib(idat);

        Assert.That(PngEncoder.ReadUInt32BigEndian(ihdr, 0), Is.EqualTo(bitmap.Width));
        Assert.That(PngEncoder.ReadUInt32BigEndian(ihdr, 4), Is.EqualTo(bitmap.Height));
        Assert.That(UnfilterRgbaScanlines(scanlines, bitmap.Width, bitmap.Height), Is.EqualTo(expectedRgbaRows));
        AssertFilterRowsAreValid(scanlines, bitmap.Width, bitmap.Height);
    }

    static void AssertFilterRowsAreNone(byte[] scanlines, int width, int height)
    {
        int rowLength = width * 4 + 1;
        int y = 0;
        while (y < height)
        {
            Assert.That(scanlines[y * rowLength], Is.EqualTo(PngEncoder.FilterNone));
            y = y + 1;
        }
    }

    static void AssertFilterRowsAreValid(byte[] scanlines, int width, int height)
    {
        int rowLength = width * 4 + 1;
        int y = 0;
        while (y < height)
        {
            Assert.That(PngEncoder.IsFilterType(scanlines[y * rowLength]), Is.True);
            y = y + 1;
        }
    }

    static byte[] UnfilterRgbaScanlines(byte[] scanlines, int width, int height)
    {
        int bytesPerPixel = 4;
        int sourceRowLength = width * bytesPerPixel;
        int inputRowLength = sourceRowLength + 1;
        byte[] rgba = new byte[sourceRowLength * height];
        byte[] previousRow = new byte[sourceRowLength];
        byte[] currentRow = new byte[sourceRowLength];
        int y = 0;
        while (y < height)
        {
            int inputOffset = y * inputRowLength;
            byte filterType = scanlines[inputOffset];
            int index = 0;
            while (index < sourceRowLength)
            {
                currentRow[index] = UnfilterByte(scanlines[inputOffset + 1 + index], currentRow, previousRow, index, filterType, bytesPerPixel);
                index = index + 1;
            }

            System.Array.Copy(currentRow, 0, rgba, y * sourceRowLength, sourceRowLength);
            byte[] swap = previousRow;
            previousRow = currentRow;
            currentRow = swap;
            y = y + 1;
        }

        return rgba;
    }

    static byte UnfilterByte(byte filtered, byte[] currentRow, byte[] previousRow, int index, byte filterType, int bytesPerPixel)
    {
        int left = 0;
        int upLeft = 0;
        if (index >= bytesPerPixel)
        {
            left = currentRow[index - bytesPerPixel];
            upLeft = previousRow[index - bytesPerPixel];
        }

        int up = previousRow[index];
        int predictor = 0;
        if (filterType == PngEncoder.FilterSub)
        {
            predictor = left;
        }
        else if (filterType == PngEncoder.FilterUp)
        {
            predictor = up;
        }
        else if (filterType == PngEncoder.FilterAverage)
        {
            predictor = (left + up) / 2;
        }
        else if (filterType == PngEncoder.FilterPaeth)
        {
            predictor = PngEncoder.PaethPredictor(left, up, upLeft);
        }

        return (byte)((filtered + predictor) & 255);
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

    static string GetChunkType(byte[] png, int offset)
    {
        return System.Text.Encoding.ASCII.GetString(png, offset + 4, 4);
    }

    static int NextChunkOffset(byte[] png, int offset)
    {
        return offset + 12 + (int)PngEncoder.ReadUInt32BigEndian(png, offset);
    }

    static byte[] GetChunkData(byte[] png, string type)
    {
        int offset = 8;
        while (offset < png.Length)
        {
            uint length = PngEncoder.ReadUInt32BigEndian(png, offset);
            if (GetChunkType(png, offset) == type)
            {
                byte[] data = new byte[length];
                System.Array.Copy(png, offset + 8, data, 0, data.Length);
                return data;
            }

            offset = offset + 12 + (int)length;
        }

        throw new System.InvalidOperationException("PNG chunk was not found.");
    }

    static void AssertAllChunkCrcsValid(byte[] png)
    {
        int offset = 8;
        while (offset < png.Length)
        {
            uint length = PngEncoder.ReadUInt32BigEndian(png, offset);
            string type = GetChunkType(png, offset);
            byte[] crcBytes = new byte[4 + length];
            System.Array.Copy(png, offset + 4, crcBytes, 0, crcBytes.Length);
            uint expected = PngEncoder.Crc32(crcBytes);
            uint actual = PngEncoder.ReadUInt32BigEndian(png, offset + 8 + (int)length);
            Assert.That(actual, Is.EqualTo(expected), "CRC mismatch for chunk " + type);
            offset = offset + 12 + (int)length;
        }
    }
}
