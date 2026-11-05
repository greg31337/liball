namespace barcode.Tests;

public class BmpWriterTests
{
    [Test]
    public void DefaultBmpUsesDeterministicPaletteAndBottomUpRows()
    {
        BarcodeSymbol symbol = CreateSymbol(
            3,
            2,
            new byte[] { 1, 0, 1, 0, 1, 0 });
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ModuleScale = 1;
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        BmpWriteResult first = BmpWriter.Write(plan, options);
        BmpWriteResult second = BmpWriter.Write(plan, options);
        ParsedBmp parsed = ParseBmp(first.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(first.Bytes, Is.EqualTo(second.Bytes));
            Assert.That(first.BitsPerPixel, Is.EqualTo(1));
            Assert.That(first.Stride, Is.EqualTo(4));
            Assert.That(first.PixelOffset, Is.EqualTo(62));
            Assert.That(first.PaletteEntries, Is.EqualTo(2));
            Assert.That(parsed.DibHeaderSize, Is.EqualTo(40));
            Assert.That(parsed.Compression, Is.EqualTo(0));
            Assert.That(parsed.Palette, Is.EqualTo(new byte[]
            {
                255, 255, 255, 255,
                0, 0, 0, 255
            }));
            Assert.That(first.Bytes.Skip(62), Is.EqualTo(new byte[]
            {
                0x40, 0, 0, 0,
                0xA0, 0, 0, 0
            }));
            Assert.That(parsed.Pixels, Is.EqualTo(new byte[]
            {
                0, 0, 0, 255,
                255, 255, 255, 255,
                0, 0, 0, 255,
                255, 255, 255, 255,
                0, 0, 0, 255,
                255, 255, 255, 255
            }));
        });
    }

    [Test]
    public void OpaqueColorsUseBgrPixelsWithZeroRowPadding()
    {
        BarcodeSymbol symbol = CreateSymbol(
            3,
            2,
            new byte[] { 1, 0, 1, 0, 1, 0 });
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ModuleScale = 1;
        options.ForegroundRgba = 0xFF4000FFu;
        options.BackgroundRgba = 0x0080FFFFu;
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        BmpWriteResult written = BmpWriter.Write(plan, options);
        ParsedBmp parsed = ParseBmp(written.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(written.BitsPerPixel, Is.EqualTo(24));
            Assert.That(written.Stride, Is.EqualTo(12));
            Assert.That(written.PixelOffset, Is.EqualTo(54));
            Assert.That(parsed.Compression, Is.EqualTo(0));
            Assert.That(parsed.Pixels, Is.EqualTo(new byte[]
            {
                255, 64, 0, 255,
                0, 128, 255, 255,
                255, 64, 0, 255,
                0, 128, 255, 255,
                255, 64, 0, 255,
                0, 128, 255, 255
            }));
            Assert.That(written.Bytes.Skip(63).Take(3),
                Is.EqualTo(new byte[] { 0, 0, 0 }));
            Assert.That(written.Bytes.Skip(75).Take(3),
                Is.EqualTo(new byte[] { 0, 0, 0 }));
        });
    }

    [Test]
    public void AlphaColorsUseV4BitfieldsAndStraightBgraPixels()
    {
        BarcodeSymbol symbol = CreateSymbol(
            1,
            1,
            new byte[] { 1 });
        BarcodeQuietZone quietZone = new BarcodeQuietZone();
        quietZone.RightModules = 1;
        symbol.RequiredQuietZone = quietZone;
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ModuleScale = 1;
        options.ForegroundRgba = 0xFF000080u;
        options.BackgroundRgba = 0x0000FF80u;
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        BmpWriteResult written = BmpWriter.Write(plan, options);
        ParsedBmp parsed = ParseBmp(written.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(written.BitsPerPixel, Is.EqualTo(32));
            Assert.That(written.Stride, Is.EqualTo(8));
            Assert.That(written.PixelOffset, Is.EqualTo(122));
            Assert.That(parsed.DibHeaderSize, Is.EqualTo(108));
            Assert.That(parsed.Compression, Is.EqualTo(3));
            Assert.That(parsed.RedMask, Is.EqualTo(0x00FF0000u));
            Assert.That(parsed.GreenMask, Is.EqualTo(0x0000FF00u));
            Assert.That(parsed.BlueMask, Is.EqualTo(0x000000FFu));
            Assert.That(parsed.AlphaMask, Is.EqualTo(0xFF000000u));
            Assert.That(parsed.ColorSpace, Is.EqualTo(0x73524742u));
            Assert.That(parsed.Pixels, Is.EqualTo(new byte[]
            {
                170, 0, 85, 192,
                0, 0, 255, 128
            }));
        });
    }

    [Test]
    public void TransparentBackgroundPreservesBackgroundRgbAtZeroAlpha()
    {
        BarcodeSymbol symbol = CreateSymbol(
            1,
            1,
            new byte[] { 1 });
        BarcodeQuietZone quietZone = new BarcodeQuietZone();
        quietZone.RightModules = 1;
        symbol.RequiredQuietZone = quietZone;
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ModuleScale = 1;
        options.ForegroundRgba = 0xFF000080u;
        options.BackgroundRgba = 0x0000FFFFu;
        options.TransparentBackground = true;
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        ParsedBmp parsed = ParseBmp(
            BmpWriter.Write(plan, options).Bytes);

        Assert.That(parsed.Pixels, Is.EqualTo(new byte[]
        {
            255, 0, 0, 128,
            0, 0, 255, 0
        }));
    }

    [Test]
    public void PhysicalResolutionAndRotatedPixelsAreSerialized()
    {
        BarcodeSymbol symbol = CreateSymbol(
            2,
            1,
            new byte[] { 1, 0 });
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.DotsPerInch = 100.0;
        sizing.ModuleSizeMillimetres = 0.254;
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.Sizing = sizing;
        options.Rotation = BarcodeRotation.Clockwise90;
        options.ForegroundRgba = 0x00FF00FFu;
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        ParsedBmp parsed = ParseBmp(
            BmpWriter.Write(plan, options).Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Width, Is.EqualTo(1));
            Assert.That(parsed.Height, Is.EqualTo(2));
            Assert.That(parsed.PixelsPerMetreX, Is.EqualTo(3937));
            Assert.That(parsed.PixelsPerMetreY, Is.EqualTo(3937));
            Assert.That(parsed.Pixels, Is.EqualTo(new byte[]
            {
                0, 255, 0, 255,
                255, 255, 255, 255
            }));
        });
    }

    [Test]
    public void BuiltInTextGeometryIsEncodedIntoBmpPixels()
    {
        BarcodeSymbol symbol = CreateSymbol(
            11,
            1,
            new byte[11]);
        BarcodeTextLayout layout = new BarcodeTextLayout();
        layout.ReservedHeight = 7.0;
        symbol.TextLayout = layout;
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ModuleScale = 2;
        options.TextMode = BarcodeTextMode.Custom;
        options.CustomText = "A";
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        BmpWriteResult written = BmpWriter.Write(plan, options);
        ParsedBmp parsed = ParseBmp(written.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(written.BitsPerPixel, Is.EqualTo(1));
            Assert.That(CountBlackPixels(parsed.Pixels), Is.EqualTo(160));
        });
    }

    [Test]
    public void InvalidPlansAreRejectedBeforeWritingHeaders()
    {
        BarcodeSymbol symbol = CreateSymbol(
            1,
            1,
            new byte[] { 1 });
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        RenderPlan invalid = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));
        invalid.Size.WidthPixels = 0;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => BmpWriter.Write(null, options),
                Throws.TypeOf<BarcodeRenderingException>());
            Assert.That(
                () => BmpWriter.Write(invalid, options),
                Throws.TypeOf<BarcodeRenderingException>());
        });
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
        byte[] modules)
    {
        BarcodeQuietZone quietZone = new BarcodeQuietZone();
        BarcodeSymbol symbol = new BarcodeSymbol();
        symbol.Dimension = BarcodeDimension.Matrix;
        symbol.WidthModules = width;
        symbol.HeightModules = height;
        symbol.Modules = modules;
        symbol.RequiredQuietZone = quietZone;
        return symbol;
    }

    static ParsedBmp ParseBmp(byte[] source)
    {
        Assert.That(source.Take(2), Is.EqualTo(new byte[] { 66, 77 }));
        ParsedBmp result = new ParsedBmp();
        result.FileSize = ReadInteger(source, 2);
        result.PixelOffset = ReadInteger(source, 10);
        result.DibHeaderSize = ReadInteger(source, 14);
        result.Width = ReadInteger(source, 18);
        result.Height = ReadInteger(source, 22);
        result.Planes = ReadShort(source, 26);
        result.BitsPerPixel = ReadShort(source, 28);
        result.Compression = ReadInteger(source, 30);
        result.ImageSize = ReadInteger(source, 34);
        result.PixelsPerMetreX = ReadInteger(source, 38);
        result.PixelsPerMetreY = ReadInteger(source, 42);
        result.ColorsUsed = ReadInteger(source, 46);
        if (result.DibHeaderSize == 108)
        {
            result.RedMask = ReadUnsignedInteger(source, 54);
            result.GreenMask = ReadUnsignedInteger(source, 58);
            result.BlueMask = ReadUnsignedInteger(source, 62);
            result.AlphaMask = ReadUnsignedInteger(source, 66);
            result.ColorSpace = ReadUnsignedInteger(source, 70);
        }

        Assert.Multiple(() =>
        {
            Assert.That(result.FileSize, Is.EqualTo(source.Length));
            Assert.That(result.Planes, Is.EqualTo(1));
            Assert.That(result.Height, Is.GreaterThan(0));
        });
        int stride = ((result.Width * result.BitsPerPixel + 31) / 32) * 4;
        Assert.That(result.ImageSize, Is.EqualTo(stride * result.Height));
        if (result.BitsPerPixel == 1)
        {
            result.Palette = ReadPalette(source, result.ColorsUsed);
        }

        result.Pixels = DecodePixels(source, result, stride);
        return result;
    }

    static byte[] ReadPalette(byte[] source, int count)
    {
        byte[] palette = new byte[count * 4];
        int index = 0;
        while (index < count)
        {
            int sourceOffset = 54 + (index * 4);
            int targetOffset = index * 4;
            palette[targetOffset] = source[sourceOffset + 2];
            palette[targetOffset + 1] = source[sourceOffset + 1];
            palette[targetOffset + 2] = source[sourceOffset];
            palette[targetOffset + 3] = 255;
            index = index + 1;
        }

        return palette;
    }

    static byte[] DecodePixels(
        byte[] source,
        ParsedBmp bmp,
        int stride)
    {
        byte[] pixels = new byte[bmp.Width * bmp.Height * 4];
        int row = 0;
        while (row < bmp.Height)
        {
            int sourceRow = bmp.Height - row - 1;
            int rowOffset = bmp.PixelOffset + (sourceRow * stride);
            int column = 0;
            while (column < bmp.Width)
            {
                int targetOffset = ((row * bmp.Width) + column) * 4;
                if (bmp.BitsPerPixel == 1)
                {
                    int index = (source[rowOffset + (column / 8)]
                        >> (7 - (column % 8))) & 1;
                    int paletteOffset = index * 4;
                    global::System.Array.Copy(
                        bmp.Palette,
                        paletteOffset,
                        pixels,
                        targetOffset,
                        4);
                }
                else
                {
                    int bytesPerPixel = bmp.BitsPerPixel / 8;
                    int sourceOffset = rowOffset
                        + (column * bytesPerPixel);
                    pixels[targetOffset] = source[sourceOffset + 2];
                    pixels[targetOffset + 1] = source[sourceOffset + 1];
                    pixels[targetOffset + 2] = source[sourceOffset];
                    pixels[targetOffset + 3] = bmp.BitsPerPixel == 32
                        ? source[sourceOffset + 3]
                        : (byte)255;
                }

                column = column + 1;
            }

            row = row + 1;
        }

        return pixels;
    }

    static int CountBlackPixels(byte[] pixels)
    {
        int count = 0;
        int offset = 0;
        while (offset < pixels.Length)
        {
            if (pixels[offset] == 0
                && pixels[offset + 1] == 0
                && pixels[offset + 2] == 0
                && pixels[offset + 3] == 255)
            {
                count = count + 1;
            }

            offset = offset + 4;
        }

        return count;
    }

    static int ReadShort(byte[] source, int offset)
    {
        return source[offset] | (source[offset + 1] << 8);
    }

    static int ReadInteger(byte[] source, int offset)
    {
        return source[offset]
            | (source[offset + 1] << 8)
            | (source[offset + 2] << 16)
            | (source[offset + 3] << 24);
    }

    static uint ReadUnsignedInteger(byte[] source, int offset)
    {
        return source[offset]
            | ((uint)source[offset + 1] << 8)
            | ((uint)source[offset + 2] << 16)
            | ((uint)source[offset + 3] << 24);
    }
}

public class ParsedBmp
{
    public int FileSize;
    public int PixelOffset;
    public int DibHeaderSize;
    public int Width;
    public int Height;
    public int Planes;
    public int BitsPerPixel;
    public int Compression;
    public int ImageSize;
    public int PixelsPerMetreX;
    public int PixelsPerMetreY;
    public int ColorsUsed;
    public uint RedMask;
    public uint GreenMask;
    public uint BlueMask;
    public uint AlphaMask;
    public uint ColorSpace;
    public byte[] Palette = new byte[0];
    public byte[] Pixels = new byte[0];
}
