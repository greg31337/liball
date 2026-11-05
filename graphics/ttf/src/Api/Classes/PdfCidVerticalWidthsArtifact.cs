namespace ttf;

/// <summary>Contains PDF vertical default and exception CID metrics.</summary>
/// <remarks>
/// <c>DW2</c> is represented by the default origin y and displacement y.
/// <c>W2</c> exceptions additionally contain origin x. Signed design metrics
/// are rounded to the nearest PDF 1000-unit integer with exact halves rounded
/// away from zero. The most frequent origin-y/displacement-y pair is selected
/// as the default; ties prefer PDF's intrinsic <c>[880 -1000]</c> pair.
/// </remarks>
public class PdfCidVerticalWidthsArtifact : IPdfCidVerticalWidthsArtifact
{
    int defaultOriginY;
    int defaultDisplacementY;
    PdfCidVerticalMetric[] metrics;
    PdfCidVerticalWidthEntry[] entries;

    internal PdfCidVerticalWidthsArtifact(
        int defaultOriginY,
        int defaultDisplacementY,
        PdfCidVerticalMetric[] metrics,
        PdfCidVerticalWidthEntry[] entries)
    {
        this.defaultOriginY = defaultOriginY;
        this.defaultDisplacementY = defaultDisplacementY;
        this.metrics = CopyMetrics(metrics);
        this.entries = CopyEntries(entries);
    }

    /// <summary>Gets the default <c>DW2</c> vertical origin.</summary>
    /// <returns>The default <c>v1y</c> value.</returns>
    public int GetDefaultOriginY()
    {
        return defaultOriginY;
    }

    /// <summary>Gets the default <c>DW2</c> vertical displacement.</summary>
    /// <returns>The default <c>w1y</c> value.</returns>
    public int GetDefaultDisplacementY()
    {
        return defaultDisplacementY;
    }

    /// <summary>Gets the number of allocated CID metrics.</summary>
    /// <returns>The CID count, including CID zero.</returns>
    public int GetCidCount()
    {
        return metrics.Length;
    }

    /// <summary>Gets one allocated CID's vertical metric.</summary>
    /// <param name="cid">The allocated document CID.</param>
    /// <returns>The vertical displacement and origin.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>cid</c> is not allocated.
    /// </exception>
    public PdfCidVerticalMetric GetMetric(int cid)
    {
        if (cid < 0 || cid >= metrics.Length)
        {
            throw new System.ArgumentOutOfRangeException("cid");
        }

        return metrics[cid];
    }

    /// <summary>Copies all vertical metrics in allocated CID order.</summary>
    /// <returns>A new array containing immutable metrics.</returns>
    public PdfCidVerticalMetric[] GetMetrics()
    {
        return CopyMetrics(metrics);
    }

    /// <summary>Gets the number of compact <c>W2</c> entries.</summary>
    /// <returns>The exception entry count.</returns>
    public int GetEntryCount()
    {
        return entries.Length;
    }

    /// <summary>Gets one compact <c>W2</c> entry.</summary>
    /// <param name="index">The zero-based entry index.</param>
    /// <returns>The immutable range or array entry.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the artifact.
    /// </exception>
    public PdfCidVerticalWidthEntry GetEntry(int index)
    {
        if (index < 0 || index >= entries.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return entries[index];
    }

    /// <summary>Copies the compact <c>W2</c> entries.</summary>
    /// <returns>A new array containing immutable entries.</returns>
    public PdfCidVerticalWidthEntry[] GetEntries()
    {
        return CopyEntries(entries);
    }

    static PdfCidVerticalWidthEntry[] CopyEntries(
        PdfCidVerticalWidthEntry[] values)
    {
        PdfCidVerticalWidthEntry[] copy =
            new PdfCidVerticalWidthEntry[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static PdfCidVerticalMetric[] CopyMetrics(
        PdfCidVerticalMetric[] values)
    {
        PdfCidVerticalMetric[] copy =
            new PdfCidVerticalMetric[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
