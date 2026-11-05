namespace ttf;

/// <summary>Contains PDF horizontal default and exception CID widths.</summary>
/// <remarks>
/// Design-unit advances are multiplied by 1000 and rounded to the nearest
/// integer. Exact halves round upward. The most frequent converted width is
/// selected as <c>DW</c>; ties prefer PDF's intrinsic default of 1000 and then
/// the smaller width. Only widths differing from <c>DW</c> appear in entries.
/// </remarks>
public class PdfCidWidthsArtifact : IPdfCidWidthsArtifact
{
    int defaultWidth;
    int[] widths;
    PdfCidWidthEntry[] entries;

    internal PdfCidWidthsArtifact(
        int defaultWidth,
        int[] widths,
        PdfCidWidthEntry[] entries)
    {
        this.defaultWidth = defaultWidth;
        this.widths = CopyIntegers(widths);
        this.entries = CopyEntries(entries);
    }

    /// <summary>Gets the default width for the PDF <c>DW</c> entry.</summary>
    /// <returns>The most frequent allocated CID width.</returns>
    public int GetDefaultWidth()
    {
        return defaultWidth;
    }

    /// <summary>Gets the number of allocated CID widths.</summary>
    /// <returns>The CID count, including CID zero.</returns>
    public int GetCidCount()
    {
        return widths.Length;
    }

    /// <summary>Gets one allocated CID's horizontal width.</summary>
    /// <param name="cid">The allocated document CID.</param>
    /// <returns>The converted width in PDF 1000-unit space.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>cid</c> is not allocated.
    /// </exception>
    public int GetWidth(int cid)
    {
        if (cid < 0 || cid >= widths.Length)
        {
            throw new System.ArgumentOutOfRangeException("cid");
        }

        return widths[cid];
    }

    /// <summary>Copies all widths indexed by allocated CID.</summary>
    /// <returns>A new array in CID order.</returns>
    public int[] CopyWidths()
    {
        return CopyIntegers(widths);
    }

    /// <summary>Gets the number of compact <c>W</c> entries.</summary>
    /// <returns>The exception entry count.</returns>
    public int GetEntryCount()
    {
        return entries.Length;
    }

    /// <summary>Gets one compact <c>W</c> entry.</summary>
    /// <param name="index">The zero-based entry index.</param>
    /// <returns>The immutable range or array entry.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the artifact.
    /// </exception>
    public PdfCidWidthEntry GetEntry(int index)
    {
        if (index < 0 || index >= entries.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return entries[index];
    }

    /// <summary>Copies the compact <c>W</c> entries.</summary>
    /// <returns>A new array containing immutable entries.</returns>
    public PdfCidWidthEntry[] GetEntries()
    {
        return CopyEntries(entries);
    }

    static PdfCidWidthEntry[] CopyEntries(PdfCidWidthEntry[] values)
    {
        PdfCidWidthEntry[] copy = new PdfCidWidthEntry[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
