namespace png;

/// <summary>
/// Expands indexed pixels through a palette into 8-bit RGB pixels.
/// </summary>
internal static class PngPaletteExpander
{
    /// <summary>
    /// Looks up each palette index and returns a new 8-bit RGB buffer.
    /// </summary>
    /// <param name="indexedBuffer">The source indexed-color pixel buffer.</param>
    /// <param name="palette">The palette whose entry order matches the pixel indices.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer ExpandToRgb8(PngPixelBuffer indexedBuffer, PngPalette palette)
    {
        ArgumentNullException.ThrowIfNull(indexedBuffer);
        ArgumentNullException.ThrowIfNull(palette);

        ValidatePalette(palette);
        PngPixelBuffer indices = PngPaletteIndexPixelConverter.ToIndexed8(indexedBuffer);

        long stride = (long)indices.Width * 3;
        long byteCount = stride * indices.Height;
        if (stride > int.MaxValue || byteCount > int.MaxValue)
        {
            throw new OverflowException();
        }

        byte[] rgb = new byte[(int)byteCount];
        int rowIndex = 0;
        while (rowIndex < indices.Height)
        {
            ExpandRow(indices, rowIndex, palette, rgb, (int)stride);
            rowIndex += 1;
        }

        PngPixelBuffer result = new PngPixelBuffer();
        result.Format = PngPixelFormat.Rgb8;
        result.Width = indices.Width;
        result.Height = indices.Height;
        result.Stride = (int)stride;
        result.Data = rgb;
        return result;
    }

    /// <summary>
    /// Looks up each palette index and returns a new 8-bit RGB buffer.
    /// </summary>
    /// <param name="indexedBuffer">The source indexed-color pixel buffer.</param>
    /// <param name="plteChunk">The plte chunk value.</param>
    /// <returns>A newly allocated pixel buffer containing the result.</returns>
    public static PngPixelBuffer ExpandToRgb8(PngPixelBuffer indexedBuffer, PngChunk plteChunk)
    {
        ArgumentNullException.ThrowIfNull(plteChunk);

        PngPalette palette = PngPlteParser.Parse(plteChunk);
        return ExpandToRgb8(indexedBuffer, palette);
    }

    internal static void ExpandRow(PngPixelBuffer indices, int rowIndex, PngPalette palette, byte[] rgb, int rgbStride)
    {
        int sourceOffset = indices.Stride * rowIndex;
        int destinationOffset = rgbStride * rowIndex;
        int x = 0;
        while (x < indices.Width)
        {
            int paletteIndex = indices.Data[sourceOffset + x];
            if (paletteIndex >= palette.Entries.Length)
            {
                throw new ArgumentOutOfRangeException("palette");
            }

            PngPaletteEntry entry = palette.Entries[paletteIndex];
            rgb[destinationOffset] = entry.Red;
            rgb[destinationOffset + 1] = entry.Green;
            rgb[destinationOffset + 2] = entry.Blue;
            destinationOffset += 3;
            x += 1;
        }
    }

    internal static void ValidatePalette(PngPalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette.Entries);

        if (palette.Entries.Length < 1 || palette.Entries.Length > 256)
        {
            throw new ArgumentOutOfRangeException("palette");
        }

        int index = 0;
        while (index < palette.Entries.Length)
        {
            if (palette.Entries[index] == null)
            {
                throw new ArgumentNullException("Entries");
            }

            index += 1;
        }
    }
}
