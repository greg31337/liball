using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PixelFormatConversionTests
{
    static readonly PngPixelFormat[] Formats = new PngPixelFormat[]
    {
        PngPixelFormat.Grayscale1,
        PngPixelFormat.Grayscale2,
        PngPixelFormat.Grayscale4,
        PngPixelFormat.Grayscale8,
        PngPixelFormat.Grayscale16,
        PngPixelFormat.Indexed1,
        PngPixelFormat.Indexed2,
        PngPixelFormat.Indexed4,
        PngPixelFormat.Indexed8,
        PngPixelFormat.Rgb8,
        PngPixelFormat.Rgb16,
        PngPixelFormat.GrayscaleAlpha8,
        PngPixelFormat.GrayscaleAlpha16,
        PngPixelFormat.Rgba8,
        PngPixelFormat.Rgba16,
        PngPixelFormat.Bgr8,
        PngPixelFormat.Bgr16,
        PngPixelFormat.Bgra8,
        PngPixelFormat.Bgra16,
        PngPixelFormat.Argb8,
        PngPixelFormat.Argb16
    };

    [Test]
    public void ConvertPixelFormat_SupportsEveryDeclaredSourceAndTargetPair()
    {
        IPngTransformer transformer = new PngTransformer();
        PngPalette palette = CreatePalette(new byte[][] { new byte[] { 255, 255, 255 } });
        int sourceIndex = 0;
        while (sourceIndex < Formats.Length)
        {
            PngPixelBuffer source = CreateWhiteBuffer(Formats[sourceIndex]);
            int targetIndex = 0;
            while (targetIndex < Formats.Length)
            {
                PngPixelConversionOptions options = new PngPixelConversionOptions();
                options.TargetFormat = Formats[targetIndex];
                options.Palette = palette;
                PngPixelBuffer result = transformer.ConvertPixelFormat(source, options);
                PngPixelConversionOptions rgba16Options = new PngPixelConversionOptions();
                rgba16Options.TargetFormat = PngPixelFormat.Rgba16;
                rgba16Options.Palette = palette;
                PngPixelBuffer rgba16 = transformer.ConvertPixelFormat(result, rgba16Options);

                Assert.That(result.Format, Is.EqualTo(Formats[targetIndex]));
                Assert.That(result.Width, Is.EqualTo(1));
                Assert.That(result.Height, Is.EqualTo(1));
                Assert.That(rgba16.Data, Is.EqualTo(new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 }));
                targetIndex += 1;
            }

            sourceIndex += 1;
        }
    }

    [Test]
    public void ConvertPixelFormat_PreservesPackedDepthOrScalesSamplesAsRequested()
    {
        IPngTransformer transformer = new PngTransformer();
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Grayscale2, 4, 2, 2, new byte[] { 0x1B, 99, 0xE4, 88 });

        PngPixelBuffer preserved = Convert(transformer, source, PngPixelFormat.Grayscale2);
        PngPixelBuffer gray8 = Convert(transformer, source, PngPixelFormat.Grayscale8);
        PngPixelBuffer gray16 = Convert(transformer, source, PngPixelFormat.Grayscale16);
        source.Data[0] = 0;

        Assert.That(preserved.Stride, Is.EqualTo(1));
        Assert.That(preserved.Data, Is.EqualTo(new byte[] { 0x1B, 0xE4 }));
        Assert.That(gray8.Data, Is.EqualTo(new byte[] { 0, 85, 170, 255, 255, 170, 85, 0 }));
        Assert.That(new byte[]
        {
            gray16.Data[0], gray16.Data[1], gray16.Data[2], gray16.Data[3],
            gray16.Data[4], gray16.Data[5], gray16.Data[6], gray16.Data[7]
        }, Is.EqualTo(new byte[] { 0, 0, 0x55, 0x55, 0xAA, 0xAA, 0xFF, 0xFF }));
    }

    [Test]
    public void ConvertPixelFormat_ExpandsIndexedPaletteAndTransparency()
    {
        IPngTransformer transformer = new PngTransformer();
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Indexed2, 4, 1, 1, new byte[] { 0x1B });
        PngPixelConversionOptions options = new PngPixelConversionOptions();
        options.TargetFormat = PngPixelFormat.Rgba8;
        options.Palette = CreatePalette(new byte[][]
        {
            new byte[] { 10, 20, 30 },
            new byte[] { 40, 50, 60 },
            new byte[] { 70, 80, 90 },
            new byte[] { 100, 110, 120 }
        });
        options.IndexedTransparency = new PngIndexedTransparency();
        options.IndexedTransparency.Alpha = new byte[] { 255, 128, 0 };

        PngPixelBuffer result = transformer.ConvertPixelFormat(source, options);

        Assert.That(result.Data, Is.EqualTo(new byte[]
        {
            10, 20, 30, 255,
            40, 50, 60, 128,
            70, 80, 90, 0,
            100, 110, 120, 255
        }));
    }

    [Test]
    public void ConvertPixelFormat_MapsExplicitAlphaColorsIntoIndexedDepth()
    {
        IPngTransformer transformer = new PngTransformer();
        PngPixelBuffer source = CreateBuffer(PngPixelFormat.Rgba8, 4, 1, 16, new byte[]
        {
            10, 20, 30, 255,
            40, 50, 60, 128,
            70, 80, 90, 0,
            100, 110, 120, 255
        });
        PngPixelConversionOptions options = new PngPixelConversionOptions();
        options.TargetFormat = PngPixelFormat.Indexed2;
        options.Palette = CreatePalette(new byte[][]
        {
            new byte[] { 10, 20, 30 },
            new byte[] { 40, 50, 60 },
            new byte[] { 70, 80, 90 },
            new byte[] { 100, 110, 120 }
        });
        options.IndexedTransparency = new PngIndexedTransparency();
        options.IndexedTransparency.Alpha = new byte[] { 255, 128, 0 };

        PngPixelBuffer result = transformer.ConvertPixelFormat(source, options);

        Assert.That(result.Format, Is.EqualTo(PngPixelFormat.Indexed2));
        Assert.That(result.Stride, Is.EqualTo(1));
        Assert.That(result.Data, Is.EqualTo(new byte[] { 0x1B }));
    }

    [Test]
    public void ConvertPixelFormat_ExpandsGrayscaleAndTruecolorTransparencyAtCorrectDepth()
    {
        IPngTransformer transformer = new PngTransformer();
        PngPixelBuffer grayscale = CreateBuffer(PngPixelFormat.Grayscale2, 2, 1, 1, new byte[] { 0x60 });
        PngPixelConversionOptions grayscaleOptions = new PngPixelConversionOptions();
        grayscaleOptions.TargetFormat = PngPixelFormat.GrayscaleAlpha8;
        grayscaleOptions.GrayscaleTransparency = new PngGrayscaleTransparency();
        grayscaleOptions.GrayscaleTransparency.Gray = 2;
        PngPixelBuffer rgb = CreateBuffer(PngPixelFormat.Rgb8, 2, 1, 6, new byte[] { 10, 20, 30, 1, 2, 3 });
        PngPixelConversionOptions rgbOptions = new PngPixelConversionOptions();
        rgbOptions.TargetFormat = PngPixelFormat.Rgba16;
        rgbOptions.TruecolorTransparency = new PngTruecolorTransparency();
        rgbOptions.TruecolorTransparency.Red = 10;
        rgbOptions.TruecolorTransparency.Green = 20;
        rgbOptions.TruecolorTransparency.Blue = 30;

        PngPixelBuffer grayscaleResult = transformer.ConvertPixelFormat(grayscale, grayscaleOptions);
        PngPixelBuffer rgbResult = transformer.ConvertPixelFormat(rgb, rgbOptions);

        Assert.That(grayscaleResult.Data, Is.EqualTo(new byte[] { 85, 255, 170, 0 }));
        Assert.That(rgbResult.Data, Is.EqualTo(new byte[]
        {
            10, 10, 20, 20, 30, 30, 0, 0,
            1, 1, 2, 2, 3, 3, 255, 255
        }));
    }

    [Test]
    public void ConvertPixelFormat_RejectsInvalidBuffersOptionsAndPaletteMappings()
    {
        IPngTransformer transformer = new PngTransformer();
        PngPixelBuffer rgb = CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, new byte[] { 1, 2, 3 });
        PngPixelConversionOptions indexed = new PngPixelConversionOptions();
        indexed.TargetFormat = PngPixelFormat.Indexed1;
        PngPixelConversionOptions missingColor = new PngPixelConversionOptions();
        missingColor.TargetFormat = PngPixelFormat.Indexed1;
        missingColor.Palette = CreatePalette(new byte[][] { new byte[] { 9, 9, 9 } });
        PngPixelConversionOptions invalidTarget = new PngPixelConversionOptions();
        invalidTarget.TargetFormat = PngPixelFormat.Unknown;
        PngPixelBuffer invalidStride = CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 2, new byte[] { 1, 2, 3 });
        PngPixelBuffer nullData = CreateBuffer(PngPixelFormat.Rgb8, 1, 1, 3, null!);

        Assert.Throws<ArgumentNullException>(() => transformer.ConvertPixelFormat(null!, new PngPixelConversionOptions()));
        Assert.Throws<ArgumentNullException>(() => transformer.ConvertPixelFormat(rgb, null!));
        Assert.Throws<ArgumentNullException>(() => transformer.ConvertPixelFormat(nullData, new PngPixelConversionOptions()));
        Assert.Throws<ArgumentOutOfRangeException>(() => transformer.ConvertPixelFormat(invalidStride, new PngPixelConversionOptions()));
        Assert.Throws<ArgumentOutOfRangeException>(() => transformer.ConvertPixelFormat(rgb, invalidTarget));
        Assert.Throws<ArgumentNullException>(() => transformer.ConvertPixelFormat(rgb, indexed));
        Assert.Throws<InvalidOperationException>(() => transformer.ConvertPixelFormat(rgb, missingColor));
    }

    static PngPixelBuffer Convert(IPngTransformer transformer, PngPixelBuffer source, PngPixelFormat targetFormat)
    {
        PngPixelConversionOptions options = new PngPixelConversionOptions();
        options.TargetFormat = targetFormat;
        return transformer.ConvertPixelFormat(source, options);
    }

    static PngPixelBuffer CreateWhiteBuffer(PngPixelFormat format)
    {
        switch (format)
        {
            case PngPixelFormat.Grayscale1:
                return CreateBuffer(format, 1, 1, 1, new byte[] { 0x80 });
            case PngPixelFormat.Grayscale2:
                return CreateBuffer(format, 1, 1, 1, new byte[] { 0xC0 });
            case PngPixelFormat.Grayscale4:
                return CreateBuffer(format, 1, 1, 1, new byte[] { 0xF0 });
            case PngPixelFormat.Grayscale8:
                return CreateBuffer(format, 1, 1, 1, new byte[] { 255 });
            case PngPixelFormat.Grayscale16:
                return CreateBuffer(format, 1, 1, 2, Filled(2));
            case PngPixelFormat.Indexed1:
            case PngPixelFormat.Indexed2:
            case PngPixelFormat.Indexed4:
            case PngPixelFormat.Indexed8:
                return CreateBuffer(format, 1, 1, 1, new byte[] { 0 });
            case PngPixelFormat.Rgb8:
                return CreateBuffer(format, 1, 1, 3, Filled(3));
            case PngPixelFormat.Rgb16:
                return CreateBuffer(format, 1, 1, 6, Filled(6));
            case PngPixelFormat.GrayscaleAlpha8:
                return CreateBuffer(format, 1, 1, 2, Filled(2));
            case PngPixelFormat.GrayscaleAlpha16:
                return CreateBuffer(format, 1, 1, 4, Filled(4));
            case PngPixelFormat.Rgba8:
                return CreateBuffer(format, 1, 1, 4, Filled(4));
            case PngPixelFormat.Rgba16:
                return CreateBuffer(format, 1, 1, 8, Filled(8));
            case PngPixelFormat.Bgr8:
                return CreateBuffer(format, 1, 1, 3, Filled(3));
            case PngPixelFormat.Bgr16:
                return CreateBuffer(format, 1, 1, 6, Filled(6));
            case PngPixelFormat.Bgra8:
            case PngPixelFormat.Argb8:
                return CreateBuffer(format, 1, 1, 4, Filled(4));
            case PngPixelFormat.Bgra16:
            case PngPixelFormat.Argb16:
                return CreateBuffer(format, 1, 1, 8, Filled(8));
            default:
                throw new ArgumentOutOfRangeException("format");
        }
    }

    static byte[] Filled(int length)
    {
        byte[] data = new byte[length];
        int index = 0;
        while (index < length)
        {
            data[index] = 255;
            index += 1;
        }

        return data;
    }

    static PngPalette CreatePalette(byte[][] colors)
    {
        PngPalette palette = new PngPalette();
        palette.Entries = new PngPaletteEntry[colors.Length];
        int index = 0;
        while (index < colors.Length)
        {
            PngPaletteEntry entry = new PngPaletteEntry();
            entry.Red = colors[index][0];
            entry.Green = colors[index][1];
            entry.Blue = colors[index][2];
            palette.Entries[index] = entry;
            index += 1;
        }

        return palette;
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
