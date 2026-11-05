using System.Collections.Generic;
using System.IO;

namespace png;

/// <summary>
/// Builds an exact palette for images containing no more than 256 distinct colors.
/// </summary>
internal static class PngPaletteGenerator
{
    /// <summary>
    /// Collects distinct source colors in encounter order and returns an exact palette of at most 256 entries.
    /// </summary>
    /// <param name="buffer">The source pixel buffer.</param>
    /// <returns>The resulting metadata, model, or calculated value.</returns>
    public static PngPalette Generate(PngPixelBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(buffer.Data);

        if (PngGrayscaleExpander.IsLowBitGrayscaleFormat(buffer.Format))
        {
            return Generate(PngGrayscaleExpander.ExpandLowBitToGrayscale8(buffer));
        }

        if (PngReorderedPixelTransformer.IsReordered(buffer.Format))
        {
            return Generate(PngReorderedPixelTransformer.Normalize(buffer));
        }

        if (buffer.Format == PngPixelFormat.GrayscaleAlpha16 || buffer.Format == PngPixelFormat.Rgba16)
        {
            return Generate(PngAlphaTransformer.StripAlpha(buffer));
        }

        if (Is16BitFormat(buffer.Format))
        {
            if (!PngSampleTransformer.CanReduce16BitTo8Bit(buffer))
            {
                throw new InvalidDataException("Palette generation requires 16-bit colors to be losslessly representable at 8 bits.");
            }

            return Generate(PngSampleTransformer.Reduce16BitTo8BitIfLossless(buffer));
        }

        int bytesPerPixel = GetBytesPerPixel(buffer.Format);
        int rowBytes = CalculateRowBytes(buffer.Width, bytesPerPixel);
        ValidateBuffer(buffer, rowBytes);

        List<PngPaletteEntry> entries = new List<PngPaletteEntry>();
        int rowIndex = 0;
        while (rowIndex < buffer.Height)
        {
            AddRowColors(buffer, rowIndex, bytesPerPixel, entries);
            rowIndex += 1;
        }

        PngPalette palette = new PngPalette();
        palette.Entries = entries.ToArray();
        return palette;
    }

    internal static void AddRowColors(PngPixelBuffer buffer, int rowIndex, int bytesPerPixel, List<PngPaletteEntry> entries)
    {
        int sourceOffset = buffer.Stride * rowIndex;
        int pixelIndex = 0;
        while (pixelIndex < buffer.Width)
        {
            int pixelOffset = sourceOffset + pixelIndex * bytesPerPixel;
            byte red = ReadRed(buffer, pixelOffset);
            byte green = ReadGreen(buffer, pixelOffset);
            byte blue = ReadBlue(buffer, pixelOffset);
            AddColor(entries, red, green, blue);
            pixelIndex += 1;
        }
    }

    internal static void AddColor(List<PngPaletteEntry> entries, byte red, byte green, byte blue)
    {
        if (HasColor(entries, red, green, blue))
        {
            return;
        }

        if (entries.Count >= PngPlteValidator.MaximumEntryCount)
        {
            throw new InvalidDataException("Palette generation found more than 256 colors.");
        }

        PngPaletteEntry entry = new PngPaletteEntry();
        entry.Red = red;
        entry.Green = green;
        entry.Blue = blue;
        entries.Add(entry);
    }

    internal static bool HasColor(List<PngPaletteEntry> entries, byte red, byte green, byte blue)
    {
        int index = 0;
        while (index < entries.Count)
        {
            PngPaletteEntry entry = entries[index];
            if (entry.Red == red && entry.Green == green && entry.Blue == blue)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal static byte ReadRed(PngPixelBuffer buffer, int offset)
    {
        return buffer.Data[offset];
    }

    internal static byte ReadGreen(PngPixelBuffer buffer, int offset)
    {
        if (IsGrayscale(buffer.Format))
        {
            return buffer.Data[offset];
        }

        return buffer.Data[offset + 1];
    }

    internal static byte ReadBlue(PngPixelBuffer buffer, int offset)
    {
        if (IsGrayscale(buffer.Format))
        {
            return buffer.Data[offset];
        }

        return buffer.Data[offset + 2];
    }

    internal static bool IsGrayscale(PngPixelFormat format)
    {
        return format == PngPixelFormat.Grayscale8 || format == PngPixelFormat.GrayscaleAlpha8;
    }

    internal static bool Is16BitFormat(PngPixelFormat format)
    {
        return format == PngPixelFormat.Grayscale16
            || format == PngPixelFormat.GrayscaleAlpha16
            || format == PngPixelFormat.Rgb16
            || format == PngPixelFormat.Rgba16;
    }

    internal static int GetBytesPerPixel(PngPixelFormat format)
    {
        switch (format)
        {
            case PngPixelFormat.Grayscale8:
                return 1;
            case PngPixelFormat.GrayscaleAlpha8:
                return 2;
            case PngPixelFormat.Rgb8:
                return 3;
            case PngPixelFormat.Rgba8:
                return 4;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static int CalculateRowBytes(int width, int bytesPerPixel)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("Width");
        }

        long rowBytes = (long)width * bytesPerPixel;
        if (rowBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        return (int)rowBytes;
    }

    internal static void ValidateBuffer(PngPixelBuffer buffer, int rowBytes)
    {
        if (buffer.Height <= 0)
        {
            throw new ArgumentOutOfRangeException("Height");
        }

        if (buffer.Stride < rowBytes)
        {
            throw new ArgumentOutOfRangeException("Stride");
        }

        long requiredBytes = CalculateRequiredBytes(buffer.Stride, buffer.Height, rowBytes);
        if (requiredBytes > buffer.Data.Length)
        {
            throw new ArgumentOutOfRangeException("Data");
        }
    }

    internal static long CalculateRequiredBytes(int stride, int height, int rowBytes)
    {
        if (height == 1)
        {
            return rowBytes;
        }

        return (long)stride * (height - 1) + rowBytes;
    }
}
