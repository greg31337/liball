namespace png.Tests;

public class PngValidFixtures
{
    public static PngImage CreateImage(PngPixelFormat pixelFormat, int width, int height)
    {
        ValidateDimensions(width, height);

        PngImage image = Png.CreateImage(width, height, pixelFormat);
        FillPixelData(image.Pixels.Data, pixelFormat, width, height);
        if (IsIndexed(pixelFormat))
        {
            image.Metadata.Palette = CreatePalette(GetPaletteEntryCount(pixelFormat));
        }

        return image;
    }

    public static byte[] CreatePng(PngPixelFormat pixelFormat, int width, int height)
    {
        PngEncoderOptions options = new PngEncoderOptions();
        options.UseAutomaticColorType = false;
        options.UseAutomaticBitDepth = false;
        options.ColorType = GetColorType(pixelFormat);
        options.BitDepth = GetBitDepth(pixelFormat);
        return Png.EncodeToBytes(CreateImage(pixelFormat, width, height), options);
    }

    public static PngImage CreateRgba8Image(int width, int height)
    {
        return CreateImage(PngPixelFormat.Rgba8, width, height);
    }

    public static byte[] CreateRgba8Png(int width, int height)
    {
        return CreatePng(PngPixelFormat.Rgba8, width, height);
    }

    public static PngImage CreateRgb8Image(int width, int height)
    {
        return CreateImage(PngPixelFormat.Rgb8, width, height);
    }

    public static byte[] CreateRgb8Png(int width, int height)
    {
        return CreatePng(PngPixelFormat.Rgb8, width, height);
    }

    public static PngImage CreateGrayscale8Image(int width, int height)
    {
        return CreateImage(PngPixelFormat.Grayscale8, width, height);
    }

    public static byte[] CreateGrayscale8Png(int width, int height)
    {
        return CreatePng(PngPixelFormat.Grayscale8, width, height);
    }

    public static PngImage CreateIndexed8Image(int width, int height)
    {
        return CreateImage(PngPixelFormat.Indexed8, width, height);
    }

    public static byte[] CreateIndexed8Png(int width, int height)
    {
        return CreatePng(PngPixelFormat.Indexed8, width, height);
    }

    static void ValidateDimensions(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException("height");
        }
    }

    static void FillPixelData(byte[] data, PngPixelFormat pixelFormat, int width, int height)
    {
        int index = 0;
        while (index < data.Length)
        {
            data[index] = CreatePixelByte(index, pixelFormat, width, height);
            index += 1;
        }
    }

    static byte CreatePixelByte(int index, PngPixelFormat pixelFormat, int width, int height)
    {
        int value = index * 37
            + (int)pixelFormat * 11
            + width * 17
            + height * 29
            + 23;
        return (byte)(value & 255);
    }

    static bool IsIndexed(PngPixelFormat pixelFormat)
    {
        return pixelFormat == PngPixelFormat.Indexed1
            || pixelFormat == PngPixelFormat.Indexed2
            || pixelFormat == PngPixelFormat.Indexed4
            || pixelFormat == PngPixelFormat.Indexed8;
    }

    static int GetPaletteEntryCount(PngPixelFormat pixelFormat)
    {
        switch (pixelFormat)
        {
            case PngPixelFormat.Indexed1:
                return 2;
            case PngPixelFormat.Indexed2:
                return 4;
            case PngPixelFormat.Indexed4:
                return 16;
            case PngPixelFormat.Indexed8:
                return 256;
            default:
                throw new ArgumentOutOfRangeException("pixelFormat");
        }
    }

    static PngColorType GetColorType(PngPixelFormat pixelFormat)
    {
        switch (pixelFormat)
        {
            case PngPixelFormat.Grayscale1:
            case PngPixelFormat.Grayscale2:
            case PngPixelFormat.Grayscale4:
            case PngPixelFormat.Grayscale8:
            case PngPixelFormat.Grayscale16:
                return PngColorType.Grayscale;
            case PngPixelFormat.Indexed1:
            case PngPixelFormat.Indexed2:
            case PngPixelFormat.Indexed4:
            case PngPixelFormat.Indexed8:
                return PngColorType.IndexedColor;
            case PngPixelFormat.Rgb8:
            case PngPixelFormat.Rgb16:
                return PngColorType.Truecolor;
            case PngPixelFormat.GrayscaleAlpha8:
            case PngPixelFormat.GrayscaleAlpha16:
                return PngColorType.GrayscaleAlpha;
            case PngPixelFormat.Rgba8:
            case PngPixelFormat.Rgba16:
                return PngColorType.TruecolorAlpha;
            default:
                throw new ArgumentOutOfRangeException("pixelFormat");
        }
    }

    static PngBitDepth GetBitDepth(PngPixelFormat pixelFormat)
    {
        switch (pixelFormat)
        {
            case PngPixelFormat.Grayscale1:
            case PngPixelFormat.Indexed1:
                return PngBitDepth.One;
            case PngPixelFormat.Grayscale2:
            case PngPixelFormat.Indexed2:
                return PngBitDepth.Two;
            case PngPixelFormat.Grayscale4:
            case PngPixelFormat.Indexed4:
                return PngBitDepth.Four;
            case PngPixelFormat.Grayscale8:
            case PngPixelFormat.Indexed8:
            case PngPixelFormat.Rgb8:
            case PngPixelFormat.GrayscaleAlpha8:
            case PngPixelFormat.Rgba8:
                return PngBitDepth.Eight;
            case PngPixelFormat.Grayscale16:
            case PngPixelFormat.Rgb16:
            case PngPixelFormat.GrayscaleAlpha16:
            case PngPixelFormat.Rgba16:
                return PngBitDepth.Sixteen;
            default:
                throw new ArgumentOutOfRangeException("pixelFormat");
        }
    }

    static PngPalette CreatePalette(int entryCount)
    {
        PngPalette palette = new PngPalette();
        palette.Entries = new PngPaletteEntry[entryCount];
        int index = 0;
        while (index < entryCount)
        {
            PngPaletteEntry entry = new PngPaletteEntry();
            entry.Red = (byte)((index * 67 + entryCount * 13 + 7) & 255);
            entry.Green = (byte)((index * 29 + entryCount * 17 + 19) & 255);
            entry.Blue = (byte)((index * 11 + entryCount * 23 + 31) & 255);
            palette.Entries[index] = entry;
            index += 1;
        }

        return palette;
    }
}
