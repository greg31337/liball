namespace png;

/// <summary>
/// Removes unused trailing palette entries while preserving every referenced index.
/// </summary>
internal static class PngPaletteTrimmer
{
    /// <summary>
    /// Returns palette entries through the highest index referenced by the pixel buffer.
    /// </summary>
    /// <param name="indexedBuffer">The source indexed-color pixel buffer.</param>
    /// <param name="palette">The palette whose entry order matches the pixel indices.</param>
    /// <returns>The resulting metadata, model, or calculated value.</returns>
    public static PngPalette Trim(PngPixelBuffer indexedBuffer, PngPalette palette)
    {
        ArgumentNullException.ThrowIfNull(indexedBuffer);
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(palette.Entries);

        PngPixelBuffer indices = PngPaletteIndexPixelConverter.ToIndexed8(indexedBuffer);
        int entryCount = FindHighestIndex(indices) + 1;
        if (entryCount > palette.Entries.Length)
        {
            throw new ArgumentOutOfRangeException("palette");
        }

        return CopyEntries(palette, entryCount);
    }

    internal static int FindHighestIndex(PngPixelBuffer indices)
    {
        int highestIndex = 0;
        int rowIndex = 0;
        while (rowIndex < indices.Height)
        {
            int rowOffset = indices.Stride * rowIndex;
            int columnIndex = 0;
            while (columnIndex < indices.Width)
            {
                int paletteIndex = indices.Data[rowOffset + columnIndex];
                if (paletteIndex > highestIndex)
                {
                    highestIndex = paletteIndex;
                }

                columnIndex += 1;
            }

            rowIndex += 1;
        }

        return highestIndex;
    }

    internal static PngPalette CopyEntries(PngPalette palette, int entryCount)
    {
        PngPalette trimmed = new PngPalette();
        trimmed.Entries = new PngPaletteEntry[entryCount];

        int entryIndex = 0;
        while (entryIndex < entryCount)
        {
            PngPaletteEntry source = palette.Entries[entryIndex];
            ArgumentNullException.ThrowIfNull(source);

            PngPaletteEntry entry = new PngPaletteEntry();
            entry.Red = source.Red;
            entry.Green = source.Green;
            entry.Blue = source.Blue;
            trimmed.Entries[entryIndex] = entry;

            entryIndex += 1;
        }

        return trimmed;
    }
}
