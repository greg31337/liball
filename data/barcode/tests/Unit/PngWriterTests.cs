namespace barcode.Tests;

public class PngWriterTests
{
    [Test]
    public void DefaultPngIsDeterministicValidFilteredGrayscale()
    {
        BarcodeSymbol symbol = CreateSymbol(
            3,
            2,
            new byte[] { 1, 0, 1, 0, 1, 0 },
            CreateQuietZone(1, 2, 1, 1));
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ModuleScale = 3;
        options.Rotation = BarcodeRotation.Clockwise90;
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        PngWriteResult first = PngWriter.Write(plan, options);
        PngWriteResult second = PngWriter.Write(plan, options);
        ParsedPng parsed = ParsePng(first.Bytes);
        RasterCanvas gray = RasterCanvas.CreateGray8(plan, options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Bytes, Is.EqualTo(second.Bytes));
            Assert.That(first.ColorType, Is.EqualTo(0));
            Assert.That(first.BitDepth, Is.EqualTo(8));
            Assert.That(parsed.Width, Is.EqualTo(plan.Size.WidthPixels));
            Assert.That(parsed.Height, Is.EqualTo(plan.Size.HeightPixels));
            Assert.That(parsed.BitDepth, Is.EqualTo(8));
            Assert.That(parsed.ColorType, Is.EqualTo(0));
            Assert.That(parsed.ChunkTypes,
                Is.EqualTo(new string[] { "IHDR", "IDAT", "IEND" }));
            Assert.That(parsed.Pixels, Is.EqualTo(gray.Pixels));
            Assert.That(parsed.FilterTypes, Is.EqualTo(first.FilterTypes));
            Assert.That(parsed.FilterTypes.Any(value => value != 0), Is.True);
        });
    }

    [TestCase(0x000000FFu, 0xFFFFFFFFu, false, 0)]
    [TestCase(0xFF0000FFu, 0x0000FFFFu, false, 2)]
    [TestCase(0x000000FFu, 0xFFFFFFFFu, true, 4)]
    [TestCase(0xFF0000FFu, 0x0000FFFFu, true, 6)]
    public void WriterSelectsTheSmallestLosslessColorType(
        uint foreground,
        uint background,
        bool transparent,
        int expectedColorType)
    {
        BarcodeSymbol symbol = CreateSymbol(
            1,
            1,
            new byte[] { 1 },
            CreateQuietZone(0, 0, 1, 0));
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ForegroundRgba = foreground;
        options.BackgroundRgba = background;
        options.TransparentBackground = transparent;
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        PngWriteResult written = PngWriter.Write(plan, options);
        ParsedPng parsed = ParsePng(written.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(written.ColorType, Is.EqualTo(expectedColorType));
            Assert.That(parsed.ColorType, Is.EqualTo(expectedColorType));
            Assert.That(parsed.Pixels,
                Is.EqualTo(ExpectedTwoPixelColors(expectedColorType)));
        });
    }

    [Test]
    public void SemiTransparentForegroundIsCompositedOverBackground()
    {
        BarcodeSymbol symbol = CreateSymbol(
            1,
            1,
            new byte[] { 1 },
            CreateQuietZone(0, 0, 1, 0));
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ForegroundRgba = 0xFF000080u;
        options.BackgroundRgba = 0x0000FF80u;
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        ParsedPng parsed = ParsePng(
            PngWriter.Write(plan, options).Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.ColorType, Is.EqualTo(6));
            Assert.That(parsed.Pixels, Is.EqualTo(new byte[]
            {
                170, 0, 85, 192,
                0, 0, 255, 128
            }));
        });
    }

    [Test]
    public void PhysicalResolutionIsWrittenInPixelsPerMetre()
    {
        BarcodeSymbol symbol = CreateSymbol(
            2,
            2,
            new byte[] { 1, 0, 0, 1 },
            CreateQuietZone(1, 1, 1, 1));
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.DotsPerInch = 100.0;
        sizing.ModuleSizeMillimetres = 0.254;
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.Sizing = sizing;
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        ParsedPng parsed = ParsePng(
            PngWriter.Write(plan, options).Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.ChunkTypes, Is.EqualTo(new string[]
            {
                "IHDR", "pHYs", "IDAT", "IEND"
            }));
            Assert.That(parsed.PixelsPerMetreX, Is.EqualTo(3937));
            Assert.That(parsed.PixelsPerMetreY, Is.EqualTo(3937));
            Assert.That(parsed.PhysicalUnit, Is.EqualTo(1));
        });
    }

    [Test]
    public void BuiltInTextGeometryIsEncodedIntoPngPixels()
    {
        BarcodeSymbol symbol = CreateSymbol(
            11,
            1,
            new byte[11],
            CreateQuietZone(0, 0, 0, 0));
        BarcodeTextLayout layout = new BarcodeTextLayout();
        layout.ReservedHeight = 7.0;
        symbol.TextLayout = layout;
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ModuleScale = 2;
        options.TextMode = BarcodeTextMode.Custom;
        options.CustomText = "A";
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        ParsedPng parsed = ParsePng(
            PngWriter.Write(plan, options).Bytes);
        RasterCanvas expected = RasterCanvas.CreateGray8(plan, options);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.ColorType, Is.EqualTo(0));
            Assert.That(parsed.Pixels, Is.EqualTo(expected.Pixels));
            Assert.That(parsed.Pixels.Count(value => value == 0),
                Is.EqualTo(160));
        });
    }

    [Test]
    public void InvalidPlansAreRejectedBeforeWritingChunks()
    {
        BarcodeSymbol symbol = CreateSymbol(
            1,
            1,
            new byte[] { 1 },
            CreateQuietZone(0, 0, 0, 0));
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        RenderPlan invalid = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));
        invalid.Size.HeightPixels = 0;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => PngWriter.Write(null, options),
                Throws.TypeOf<BarcodeRenderingException>());
            Assert.That(
                () => PngWriter.Write(invalid, options),
                Throws.TypeOf<BarcodeRenderingException>());
        });
    }

    static byte[] ExpectedTwoPixelColors(int colorType)
    {
        switch (colorType)
        {
            case 0:
                return new byte[] { 0, 255 };
            case 2:
                return new byte[]
                {
                    255, 0, 0,
                    0, 0, 255
                };
            case 4:
                return new byte[]
                {
                    0, 255,
                    255, 0
                };
            case 6:
                return new byte[]
                {
                    255, 0, 0, 255,
                    0, 0, 255, 0
                };
            default:
                return new byte[0];
        }
    }

    static BarcodeRenderRequest CreateRequest(
        BarcodeSymbol symbol,
        BarcodeRenderOptions options)
    {
        BarcodeRenderRequest request = new BarcodeRenderRequest();
        request.Symbol = symbol;
        request.Options = options;
        return request;
    }

    static BarcodeSymbol CreateSymbol(
        int width,
        int height,
        byte[] modules,
        BarcodeQuietZone quietZone)
    {
        BarcodeSymbol symbol = new BarcodeSymbol();
        symbol.Dimension = BarcodeDimension.Matrix;
        symbol.WidthModules = width;
        symbol.HeightModules = height;
        symbol.Modules = modules;
        symbol.RequiredQuietZone = quietZone;
        return symbol;
    }

    static BarcodeQuietZone CreateQuietZone(
        int left,
        int top,
        int right,
        int bottom)
    {
        BarcodeQuietZone quietZone = new BarcodeQuietZone();
        quietZone.LeftModules = left;
        quietZone.TopModules = top;
        quietZone.RightModules = right;
        quietZone.BottomModules = bottom;
        return quietZone;
    }

    static ParsedPng ParsePng(byte[] source)
    {
        Assert.That(source.Take(8), Is.EqualTo(new byte[]
        {
            137, 80, 78, 71, 13, 10, 26, 10
        }));
        ParsedPng result = new ParsedPng();
        global::System.Collections.Generic.List<string> chunkTypes =
            new global::System.Collections.Generic.List<string>();
        global::System.Collections.Generic.List<byte> compressed =
            new global::System.Collections.Generic.List<byte>();
        int offset = 8;
        while (offset < source.Length)
        {
            int length = ReadBigEndianInteger(source, offset);
            string type = global::System.Text.Encoding.ASCII.GetString(
                source,
                offset + 4,
                4);
            byte[] data = source
                .Skip(offset + 8)
                .Take(length)
                .ToArray();
            uint observedCrc = ReadBigEndianUnsignedInteger(
                source,
                offset + 8 + length);
            uint expectedCrc = CalculateChunkCrc(type, data);
            Assert.That(observedCrc, Is.EqualTo(expectedCrc),
                "PNG chunk CRC must match for " + type + ".");
            chunkTypes.Add(type);
            if (type == "IHDR")
            {
                result.Width = ReadBigEndianInteger(data, 0);
                result.Height = ReadBigEndianInteger(data, 4);
                result.BitDepth = data[8];
                result.ColorType = data[9];
            }
            else if (type == "pHYs")
            {
                result.PixelsPerMetreX = ReadBigEndianInteger(data, 0);
                result.PixelsPerMetreY = ReadBigEndianInteger(data, 4);
                result.PhysicalUnit = data[8];
            }
            else if (type == "IDAT")
            {
                compressed.AddRange(data);
            }

            offset = offset + length + 12;
        }

        result.ChunkTypes = chunkTypes.ToArray();
        byte[] scanlines = Decompress(compressed.ToArray());
        int channels = ChannelCount(result.ColorType);
        Unfilter(result, scanlines, channels);
        return result;
    }

    static byte[] Decompress(byte[] source)
    {
        using (global::System.IO.MemoryStream input =
            new global::System.IO.MemoryStream(source))
        using (global::System.IO.Compression.ZLibStream decompressor =
            new global::System.IO.Compression.ZLibStream(
                input,
                global::System.IO.Compression.CompressionMode.Decompress))
        using (global::System.IO.MemoryStream output =
            new global::System.IO.MemoryStream())
        {
            decompressor.CopyTo(output);
            return output.ToArray();
        }
    }

    static void Unfilter(
        ParsedPng png,
        byte[] scanlines,
        int bytesPerPixel)
    {
        int rowBytes = png.Width * bytesPerPixel;
        Assert.That(scanlines, Has.Length.EqualTo(
            (rowBytes + 1) * png.Height));
        byte[] pixels = new byte[rowBytes * png.Height];
        byte[] filterTypes = new byte[png.Height];
        int row = 0;
        while (row < png.Height)
        {
            int scanlineOffset = row * (rowBytes + 1);
            int filter = scanlines[scanlineOffset];
            filterTypes[row] = (byte)filter;
            Assert.That(filter, Is.InRange(0, 4));
            int index = 0;
            while (index < rowBytes)
            {
                int left = index < bytesPerPixel
                    ? 0
                    : pixels[(row * rowBytes) + index - bytesPerPixel];
                int above = row == 0
                    ? 0
                    : pixels[((row - 1) * rowBytes) + index];
                int upperLeft = row == 0 || index < bytesPerPixel
                    ? 0
                    : pixels[((row - 1) * rowBytes)
                        + index
                        - bytesPerPixel];
                int predictor = Predictor(
                    filter,
                    left,
                    above,
                    upperLeft);
                pixels[(row * rowBytes) + index] = (byte)(
                    (scanlines[scanlineOffset + 1 + index]
                        + predictor) & 255);
                index = index + 1;
            }

            row = row + 1;
        }

        png.Pixels = pixels;
        png.FilterTypes = filterTypes;
    }

    static int Predictor(int filter, int left, int above, int upperLeft)
    {
        switch (filter)
        {
            case 0:
                return 0;
            case 1:
                return left;
            case 2:
                return above;
            case 3:
                return (left + above) / 2;
            case 4:
                return Paeth(left, above, upperLeft);
            default:
                return 0;
        }
    }

    static int Paeth(int left, int above, int upperLeft)
    {
        int estimate = left + above - upperLeft;
        int leftDistance = global::System.Math.Abs(estimate - left);
        int aboveDistance = global::System.Math.Abs(estimate - above);
        int upperLeftDistance = global::System.Math.Abs(
            estimate - upperLeft);
        if (leftDistance <= aboveDistance
            && leftDistance <= upperLeftDistance)
        {
            return left;
        }

        return aboveDistance <= upperLeftDistance
            ? above
            : upperLeft;
    }

    static int ChannelCount(int colorType)
    {
        switch (colorType)
        {
            case 0:
                return 1;
            case 2:
                return 3;
            case 4:
                return 2;
            case 6:
                return 4;
            default:
                Assert.Fail("Unsupported PNG color type " + colorType + ".");
                return 0;
        }
    }

    static int ReadBigEndianInteger(byte[] source, int offset)
    {
        return (source[offset] << 24)
            | (source[offset + 1] << 16)
            | (source[offset + 2] << 8)
            | source[offset + 3];
    }

    static uint ReadBigEndianUnsignedInteger(byte[] source, int offset)
    {
        return ((uint)source[offset] << 24)
            | ((uint)source[offset + 1] << 16)
            | ((uint)source[offset + 2] << 8)
            | source[offset + 3];
    }

    static uint CalculateChunkCrc(string type, byte[] data)
    {
        uint crc = 0xFFFFFFFFu;
        byte[] typeBytes = global::System.Text.Encoding.ASCII.GetBytes(type);
        int index = 0;
        while (index < typeBytes.Length)
        {
            crc = UpdateCrc(crc, typeBytes[index]);
            index = index + 1;
        }

        index = 0;
        while (index < data.Length)
        {
            crc = UpdateCrc(crc, data[index]);
            index = index + 1;
        }

        return crc ^ 0xFFFFFFFFu;
    }

    static uint UpdateCrc(uint crc, byte value)
    {
        uint current = crc ^ value;
        int bit = 0;
        while (bit < 8)
        {
            current = (current & 1u) == 0u
                ? current >> 1
                : (current >> 1) ^ 0xEDB88320u;
            bit = bit + 1;
        }

        return current;
    }
}

public class ParsedPng
{
    public int Width;
    public int Height;
    public int BitDepth;
    public int ColorType;
    public int PixelsPerMetreX;
    public int PixelsPerMetreY;
    public int PhysicalUnit;
    public string[] ChunkTypes = new string[0];
    public byte[] FilterTypes = new byte[0];
    public byte[] Pixels = new byte[0];
}
