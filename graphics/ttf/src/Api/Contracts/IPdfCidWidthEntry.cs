namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfCidWidthEntry"/>.</summary>
public interface IPdfCidWidthEntry
{
    /// <summary>Gets the PDF width-entry representation.</summary>
    /// <returns>Range or array.</returns>
    PdfCidWidthEntryKind GetKind();

    /// <summary>Gets the first CID represented by the entry.</summary>
    /// <returns>The inclusive first CID.</returns>
    int GetFirstCid();

    /// <summary>Gets the last CID represented by the entry.</summary>
    /// <returns>The inclusive last CID.</returns>
    int GetLastCid();

    /// <summary>Gets the common width of a range entry.</summary>
    /// <returns>The range width in PDF 1000-unit space.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// This is an array entry.
    /// </exception>
    int GetRangeWidth();

    /// <summary>Copies the widths of an array entry.</summary>
    /// <returns>A new array, or an empty array for a range entry.</returns>
    int[] CopyArrayWidths();
}
