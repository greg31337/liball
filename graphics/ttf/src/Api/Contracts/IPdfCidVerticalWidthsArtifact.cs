namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfCidVerticalWidthsArtifact"/>.</summary>
public interface IPdfCidVerticalWidthsArtifact
{
    /// <summary>Gets the default <c>DW2</c> vertical origin.</summary>
    /// <returns>The default <c>v1y</c> value.</returns>
    int GetDefaultOriginY();

    /// <summary>Gets the default <c>DW2</c> vertical displacement.</summary>
    /// <returns>The default <c>w1y</c> value.</returns>
    int GetDefaultDisplacementY();

    /// <summary>Gets the number of allocated CID metrics.</summary>
    /// <returns>The CID count, including CID zero.</returns>
    int GetCidCount();

    /// <summary>Gets one allocated CID's vertical metric.</summary>
    /// <param name="cid">The allocated document CID.</param>
    /// <returns>The vertical displacement and origin.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>cid</c> is not allocated.
    /// </exception>
    PdfCidVerticalMetric GetMetric(int cid);

    /// <summary>Copies all vertical metrics in allocated CID order.</summary>
    /// <returns>A new array containing immutable metrics.</returns>
    PdfCidVerticalMetric[] GetMetrics();

    /// <summary>Gets the number of compact <c>W2</c> entries.</summary>
    /// <returns>The exception entry count.</returns>
    int GetEntryCount();

    /// <summary>Gets one compact <c>W2</c> entry.</summary>
    /// <param name="index">The zero-based entry index.</param>
    /// <returns>The immutable range or array entry.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the artifact.
    /// </exception>
    PdfCidVerticalWidthEntry GetEntry(int index);

    /// <summary>Copies the compact <c>W2</c> entries.</summary>
    /// <returns>A new array containing immutable entries.</returns>
    PdfCidVerticalWidthEntry[] GetEntries();
}
