namespace ttf;

/// <summary>Contains one range or array entry for a PDF <c>W</c> value.</summary>
public class PdfCidWidthEntry : IPdfCidWidthEntry
{
    PdfCidWidthEntryKind kind;
    int firstCid;
    int lastCid;
    int rangeWidth;
    int[] arrayWidths;

    internal PdfCidWidthEntry(
        PdfCidWidthEntryKind kind,
        int firstCid,
        int lastCid,
        int rangeWidth,
        int[] arrayWidths)
    {
        this.kind = kind;
        this.firstCid = firstCid;
        this.lastCid = lastCid;
        this.rangeWidth = rangeWidth;
        this.arrayWidths = CopyIntegers(arrayWidths);
    }

    /// <summary>Gets the PDF width-entry representation.</summary>
    /// <returns>Range or array.</returns>
    public PdfCidWidthEntryKind GetKind()
    {
        return kind;
    }

    /// <summary>Gets the first CID represented by the entry.</summary>
    /// <returns>The inclusive first CID.</returns>
    public int GetFirstCid()
    {
        return firstCid;
    }

    /// <summary>Gets the last CID represented by the entry.</summary>
    /// <returns>The inclusive last CID.</returns>
    public int GetLastCid()
    {
        return lastCid;
    }

    /// <summary>Gets the common width of a range entry.</summary>
    /// <returns>The range width in PDF 1000-unit space.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// This is an array entry.
    /// </exception>
    public int GetRangeWidth()
    {
        if (kind != PdfCidWidthEntryKind.Range)
        {
            throw new System.InvalidOperationException(
                "An array width entry does not have one range width.");
        }

        return rangeWidth;
    }

    /// <summary>Copies the widths of an array entry.</summary>
    /// <returns>A new array, or an empty array for a range entry.</returns>
    public int[] CopyArrayWidths()
    {
        return CopyIntegers(arrayWidths);
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
