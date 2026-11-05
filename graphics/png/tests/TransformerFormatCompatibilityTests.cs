using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class TransformerFormatCompatibilityTests
{
    [Test]
    public void AlphaOperations_PreserveReorderedChannelContracts()
    {
        IPngTransformer transformer = new PngTransformer();
        PngPixelBuffer bgr = CreateBuffer(PngPixelFormat.Bgr8, 1, 1, 3, new byte[] { 30, 20, 10 });
        PngPixelBuffer bgra = CreateBuffer(PngPixelFormat.Bgra8, 1, 1, 4, new byte[] { 30, 20, 10, 128 });
        PngPixelBuffer argb = CreateBuffer(PngPixelFormat.Argb8, 1, 1, 4, new byte[] { 255, 10, 20, 30 });
        PngBackgroundColor background = new PngBackgroundColor();
        background.Red = 100;
        background.Green = 110;
        background.Blue = 120;

        PngPixelBuffer added = transformer.AddOpaqueAlpha(bgr);
        PngPixelBuffer strippedBgra = transformer.StripAlpha(bgra);
        PngPixelBuffer strippedArgb = transformer.StripAlpha(argb);
        PngPixelBuffer inverted = transformer.InvertAlpha(argb);
        PngPixelBuffer composited = transformer.CompositeOverBackground(bgra, background);
        PngPixelBuffer premultiplied = transformer.PremultiplyAlpha(bgra);

        AssertBuffer(added, PngPixelFormat.Bgra8, 4, new byte[] { 30, 20, 10, 255 });
        AssertBuffer(strippedBgra, PngPixelFormat.Bgr8, 3, new byte[] { 30, 20, 10 });
        AssertBuffer(strippedArgb, PngPixelFormat.Rgb8, 3, new byte[] { 10, 20, 30 });
        AssertBuffer(inverted, PngPixelFormat.Argb8, 4, new byte[] { 0, 10, 20, 30 });
        AssertBuffer(composited, PngPixelFormat.Bgr8, 3, new byte[]
        {
            Composite(30, 120, 128), Composite(20, 110, 128), Composite(10, 100, 128)
        });
        AssertBuffer(premultiplied, PngPixelFormat.Bgra8, 4, new byte[] { 15, 10, 5, 128 });
        Assert.That(transformer.IsOpaque(argb), Is.True);
    }

    [Test]
    public void GeometryResize_SupportsEveryReorderedFormat()
    {
        IPngTransformer transformer = new PngTransformer();
        PngPixelFormat[] formats = new PngPixelFormat[]
        {
            PngPixelFormat.Bgr8,
            PngPixelFormat.Bgr16,
            PngPixelFormat.Bgra8,
            PngPixelFormat.Bgra16,
            PngPixelFormat.Argb8,
            PngPixelFormat.Argb16
        };
        int index = 0;
        while (index < formats.Length)
        {
            PngPixelFormat format = formats[index];
            int bytesPerPixel = PngSizeCalculator.GetPixelFormatBitsPerPixel(format) / 8;
            int sampleBytes = bytesPerPixel == 3 || bytesPerPixel == 4 ? 1 : 2;
            byte[] data = CreateEndpointPixels(bytesPerPixel);
            PngPixelBuffer source = CreateBuffer(format, 2, 1, bytesPerPixel * 2, data);

            PngPixelBuffer nearest = transformer.ResizeNearestNeighbor(source, 4, 1);
            PngPixelBuffer bilinear = transformer.ResizeBilinear(source, 3, 1);
            PngPixelBuffer cropped = transformer.Crop(source, 1, 0, 1, 1);

            Assert.That(nearest.Format, Is.EqualTo(format));
            Assert.That(nearest.Stride, Is.EqualTo(bytesPerPixel * 4));
            Assert.That(nearest.Data, Is.EqualTo(RepeatEndpointPixels(bytesPerPixel)));
            Assert.That(bilinear.Format, Is.EqualTo(format));
            Assert.That(bilinear.Data, Is.EqualTo(CreateBilinearEndpointPixels(bytesPerPixel, sampleBytes)));
            Assert.That(cropped.Format, Is.EqualTo(format));
            Assert.That(cropped.Data, Is.EqualTo(Filled(bytesPerPixel, 255)));
            index += 1;
        }
    }

    [Test]
    public void ResizeBilinear_InterpolatesPackedGrayscaleSamples()
    {
        IPngTransformer transformer = new PngTransformer();
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale2, 2, 1, 1, new byte[] { 0x30 });

        PngPixelBuffer result = transformer.ResizeBilinear(source, 3, 1);

        Assert.That(result.Format, Is.EqualTo(PngPixelFormat.Grayscale2));
        Assert.That(result.Width, Is.EqualTo(3));
        Assert.That(result.Height, Is.EqualTo(1));
        Assert.That(result.Stride, Is.EqualTo(1));
        Assert.That(result.Data, Is.EqualTo(new byte[] { 0x2C }));
    }

    [Test]
    public void ExpandGrayscaleToRgb_PreservesAlphaAtEightAndSixteenBits()
    {
        IPngTransformer transformer = new PngTransformer();
        PngPixelBuffer grayAlpha8 = CreateBuffer(PngPixelFormat.GrayscaleAlpha8, 1, 1, 2, new byte[] { 10, 20 });
        PngPixelBuffer grayAlpha16 = CreateBuffer(PngPixelFormat.GrayscaleAlpha16, 1, 1, 4, new byte[] { 0x12, 0x12, 0xAB, 0xAB });

        PngPixelBuffer rgba8 = transformer.ExpandGrayscaleToRgb(grayAlpha8);
        PngPixelBuffer rgba16 = transformer.ExpandGrayscaleToRgb(grayAlpha16);
        PngPixelBuffer reduced = transformer.ExpandGrayscaleToRgb8(grayAlpha16);

        AssertBuffer(rgba8, PngPixelFormat.Rgba8, 4, new byte[] { 10, 10, 10, 20 });
        AssertBuffer(rgba16, PngPixelFormat.Rgba16, 8, new byte[] { 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0xAB, 0xAB });
        AssertBuffer(reduced, PngPixelFormat.Rgba8, 4, new byte[] { 0x12, 0x12, 0x12, 0xAB });
    }

    [Test]
    public void GeneratePalette_SupportsPackedReorderedAndLosslessSixteenBitColors()
    {
        IPngTransformer transformer = new PngTransformer();
        PngPixelBuffer packed = CreateBuffer(PngPixelFormat.Grayscale1, 2, 1, 1, new byte[] { 0x40 });
        PngPixelBuffer bgr = CreateBuffer(PngPixelFormat.Bgr8, 1, 1, 3, new byte[] { 30, 20, 10 });
        PngPixelBuffer rgba16 = CreateBuffer(PngPixelFormat.Rgba16, 1, 1, 8, new byte[] { 10, 10, 20, 20, 30, 30, 40, 41 });
        PngPixelBuffer lossy16 = CreateBuffer(PngPixelFormat.Rgb16, 1, 1, 6, new byte[] { 0, 1, 0, 0, 0, 0 });

        PngPalette packedPalette = transformer.GeneratePalette(packed);
        PngPalette bgrPalette = transformer.GeneratePalette(bgr);
        PngPalette rgba16Palette = transformer.GeneratePalette(rgba16);

        AssertPaletteEntry(packedPalette, 0, 0, 0, 0);
        AssertPaletteEntry(packedPalette, 1, 255, 255, 255);
        AssertPaletteEntry(bgrPalette, 0, 10, 20, 30);
        AssertPaletteEntry(rgba16Palette, 0, 10, 20, 30);
        Assert.Throws<InvalidDataException>(() => transformer.GeneratePalette(lossy16));
    }

    [Test]
    public void SignificantBitsScaling_SupportsPackedAndReorderedSamples()
    {
        IPngTransformer transformer = new PngTransformer();
        PngSignificantBits grayBits = new PngSignificantBits();
        grayBits.Gray = 2;
        PngPixelBuffer packed = CreateBuffer(PngPixelFormat.Grayscale4, 1, 1, 1, new byte[] { 0xA0 });
        PngSignificantBits colorBits = new PngSignificantBits();
        colorBits.Red = 2;
        colorBits.Green = 3;
        colorBits.Blue = 4;
        PngPixelBuffer bgr = CreateBuffer(PngPixelFormat.Bgr8, 1, 1, 3, new byte[] { 0xC0, 0xA0, 0x80 });

        PngPixelBuffer downPacked = transformer.DownshiftToSignificantBits(packed, grayBits);
        PngPixelBuffer upPacked = transformer.UpscaleFromSignificantBits(downPacked, grayBits);
        PngPixelBuffer downBgr = transformer.DownshiftToSignificantBits(bgr, colorBits);

        AssertBuffer(downPacked, PngPixelFormat.Grayscale4, 1, new byte[] { 0x20 });
        AssertBuffer(upPacked, PngPixelFormat.Grayscale4, 1, new byte[] { 0xA0 });
        AssertBuffer(downBgr, PngPixelFormat.Bgr8, 3, new byte[] { 12, 5, 2 });
    }

    [Test]
    public void GammaCorrection_SupportsPackedAndReorderedSamples()
    {
        IPngTransformer transformer = new PngTransformer();
        byte[] table = transformer.BuildIdentity8BitGammaTable();
        table[10] = 110;
        table[20] = 120;
        table[30] = 130;
        table[170] = 42;
        PngPixelBuffer bgra = CreateBuffer(PngPixelFormat.Bgra8, 1, 1, 4, new byte[] { 30, 20, 10, 40 });
        PngPixelBuffer packed = CreateBuffer(PngPixelFormat.Grayscale2, 1, 1, 1, new byte[] { 0x80 });

        PngPixelBuffer correctedBgra = transformer.Apply8BitGammaTable(bgra, table);
        PngPixelBuffer correctedPacked = transformer.Apply8BitGammaTable(packed, table);

        AssertBuffer(correctedBgra, PngPixelFormat.Bgra8, 4, new byte[] { 130, 120, 110, 40 });
        AssertBuffer(correctedPacked, PngPixelFormat.Grayscale8, 1, new byte[] { 42 });
    }

    [Test]
    public void ColorAndTransparencyTransforms_ReadReorderedChannelsCorrectly()
    {
        IPngTransformer transformer = new PngTransformer();
        PngPixelBuffer bgr = CreateBuffer(PngPixelFormat.Bgr8, 1, 1, 3, new byte[] { 30, 20, 10 });
        PngPixelBuffer argbGray = CreateBuffer(PngPixelFormat.Argb8, 1, 1, 4, new byte[] { 40, 50, 50, 50 });
        PngTruecolorTransparency transparency = new PngTruecolorTransparency();
        transparency.Red = 10;
        transparency.Green = 20;
        transparency.Blue = 30;

        PngPixelBuffer gray = transformer.ConvertRgbToGrayscale(bgr);
        PngPixelBuffer grayAlpha = transformer.ConvertRgbToGrayscaleIfPossible(argbGray);
        PngPixelBuffer transparent = transformer.ExpandTruecolorTransparency(bgr, transparency);

        AssertBuffer(gray, PngPixelFormat.Grayscale8, 1, new byte[] { transformer.CalculateLuminance8(10, 20, 30) });
        AssertBuffer(grayAlpha, PngPixelFormat.GrayscaleAlpha8, 2, new byte[] { 50, 40 });
        AssertBuffer(transparent, PngPixelFormat.Bgra8, 4, new byte[] { 30, 20, 10, 0 });
        Assert.That(transformer.IsGrayscale(argbGray), Is.True);
    }

    [Test]
    public void ReorderedSixteenBitTransforms_PreserveSamplesAndContracts()
    {
        IPngTransformer transformer = new PngTransformer();
        PngPixelBuffer bgr = CreateBuffer(PngPixelFormat.Bgr16, 1, 1, 6, new byte[] { 0x30, 0, 0x20, 0, 0x10, 0 });
        PngPixelBuffer bgra = CreateBuffer(PngPixelFormat.Bgra16, 1, 1, 8, new byte[] { 0x30, 0, 0x20, 0, 0x10, 0, 0xFF, 0xFF });
        ushort[] table = transformer.BuildIdentity16BitGammaTable();
        table[0x1000] = 0x1111;
        table[0x2000] = 0x2222;
        table[0x3000] = 0x3333;
        PngTruecolorTransparency transparency = new PngTruecolorTransparency();
        transparency.Red = 0x1000;
        transparency.Green = 0x2000;
        transparency.Blue = 0x3000;

        PngPixelBuffer added = transformer.AddOpaqueAlpha(bgr);
        PngPixelBuffer corrected = transformer.Apply16BitGammaTable(bgra, table);
        PngPixelBuffer inverted = transformer.InvertAlpha(bgra);
        PngPixelBuffer expanded = transformer.ExpandTruecolorTransparency(bgr, transparency);

        AssertBuffer(added, PngPixelFormat.Bgra16, 8, new byte[] { 0x30, 0, 0x20, 0, 0x10, 0, 0xFF, 0xFF });
        AssertBuffer(corrected, PngPixelFormat.Bgra16, 8, new byte[] { 0x33, 0x33, 0x22, 0x22, 0x11, 0x11, 0xFF, 0xFF });
        AssertBuffer(inverted, PngPixelFormat.Bgra16, 8, new byte[] { 0x30, 0, 0x20, 0, 0x10, 0, 0, 0 });
        AssertBuffer(expanded, PngPixelFormat.Bgra16, 8, new byte[] { 0x30, 0, 0x20, 0, 0x10, 0, 0, 0 });
    }

    static byte Composite(int foreground, int background, int alpha)
    {
        return (byte)((foreground * alpha + background * (255 - alpha) + 127) / 255);
    }

    static byte[] CreateEndpointPixels(int bytesPerPixel)
    {
        byte[] data = new byte[bytesPerPixel * 2];
        int index = bytesPerPixel;
        while (index < data.Length)
        {
            data[index] = 255;
            index += 1;
        }

        return data;
    }

    static byte[] RepeatEndpointPixels(int bytesPerPixel)
    {
        byte[] data = new byte[bytesPerPixel * 4];
        int index = bytesPerPixel * 2;
        while (index < data.Length)
        {
            data[index] = 255;
            index += 1;
        }

        return data;
    }

    static byte[] CreateBilinearEndpointPixels(int bytesPerPixel, int sampleBytes)
    {
        byte[] data = new byte[bytesPerPixel * 3];
        int middleOffset = bytesPerPixel;
        int sampleOffset = 0;
        while (sampleOffset < bytesPerPixel)
        {
            data[middleOffset + sampleOffset] = sampleBytes == 1 ? (byte)128 : (byte)0x80;
            if (sampleBytes == 2)
            {
                data[middleOffset + sampleOffset + 1] = 0;
            }

            sampleOffset += sampleBytes;
        }

        int index = bytesPerPixel * 2;
        while (index < data.Length)
        {
            data[index] = 255;
            index += 1;
        }

        return data;
    }

    static byte[] Filled(int length, byte value)
    {
        byte[] data = new byte[length];
        int index = 0;
        while (index < length)
        {
            data[index] = value;
            index += 1;
        }

        return data;
    }

    static void AssertPaletteEntry(PngPalette palette, int index, byte red, byte green, byte blue)
    {
        Assert.That(palette.Entries[index].Red, Is.EqualTo(red));
        Assert.That(palette.Entries[index].Green, Is.EqualTo(green));
        Assert.That(palette.Entries[index].Blue, Is.EqualTo(blue));
    }

    static void AssertBuffer(PngPixelBuffer buffer, PngPixelFormat format, int stride, byte[] data)
    {
        Assert.That(buffer.Format, Is.EqualTo(format));
        Assert.That(buffer.Width, Is.EqualTo(1));
        Assert.That(buffer.Height, Is.EqualTo(1));
        Assert.That(buffer.Stride, Is.EqualTo(stride));
        Assert.That(buffer.Data, Is.EqualTo(data));
    }

    static PngPixelBuffer CreateBuffer(PngPixelFormat format, int width, int height, int stride, byte[] data)
    {
        PngPixelBuffer buffer = new PngPixelBuffer();
        buffer.Format = format;
        buffer.Width = width;
        buffer.Height = height;
        buffer.Stride = stride;
        buffer.Data = data;
        return buffer;
    }
}
