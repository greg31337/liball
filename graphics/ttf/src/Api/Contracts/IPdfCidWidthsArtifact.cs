namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfCidWidthsArtifact"/>.</summary>
public interface IPdfCidWidthsArtifact
{
    /// <summary>Gets the default width for the PDF <c>DW</c> entry.</summary>
    /// <returns>The most frequent allocated CID width.</returns>
    int GetDefaultWidth();

    /// <summary>Gets the number of allocated CID widths.</summary>
    /// <returns>The CID count, including CID zero.</returns>
    int GetCidCount();

    /// <summary>Gets one allocated CID's horizontal width.</summary>
    /// <param name="cid">The allocated document CID.</param>
    /// <returns>The converted width in PDF 1000-unit space.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>cid</c> is not allocated.
    /// </exception>
    int GetWidth(int cid);

    /// <summary>Copies all widths indexed by allocated CID.</summary>
    /// <returns>A new array in CID order.</returns>
    int[] CopyWidths();

    /// <summary>Gets the number of compact <c>W</c> entries.</summary>
    /// <returns>The exception entry count.</returns>
    int GetEntryCount();

    /// <summary>Gets one compact <c>W</c> entry.</summary>
    /// <param name="index">The zero-based entry index.</param>
    /// <returns>The immutable range or array entry.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the artifact.
    /// </exception>
    PdfCidWidthEntry GetEntry(int index);

    /// <summary>Copies the compact <c>W</c> entries.</summary>
    /// <returns>A new array containing immutable entries.</returns>
    PdfCidWidthEntry[] GetEntries();
}
