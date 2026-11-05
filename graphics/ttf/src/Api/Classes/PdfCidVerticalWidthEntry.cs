namespace ttf;

/// <summary>Contains one range or array entry for a PDF <c>W2</c> value.</summary>
public class PdfCidVerticalWidthEntry : IPdfCidVerticalWidthEntry
{
    PdfCidWidthEntryKind kind;
    int firstCid;
    int lastCid;
    PdfCidVerticalMetric? rangeMetric;
    PdfCidVerticalMetric[] arrayMetrics;

    internal PdfCidVerticalWidthEntry(
        PdfCidWidthEntryKind kind,
        int firstCid,
        int lastCid,
        PdfCidVerticalMetric? rangeMetric,
        PdfCidVerticalMetric[] arrayMetrics)
    {
        this.kind = kind;
        this.firstCid = firstCid;
        this.lastCid = lastCid;
        this.rangeMetric = rangeMetric;
        this.arrayMetrics = CopyMetrics(arrayMetrics);
    }

    /// <summary>Gets the PDF vertical width-entry representation.</summary>
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

    /// <summary>Gets the common metric of a range entry.</summary>
    /// <returns>The range's vertical displacement and origin.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// This is an array entry.
    /// </exception>
    public PdfCidVerticalMetric? GetRangeMetric()
    {
        if (kind != PdfCidWidthEntryKind.Range)
        {
            throw new System.InvalidOperationException(
                "An array vertical width entry has no range metric.");
        }

        return rangeMetric;
    }

    /// <summary>Gets the number of metrics in an array entry.</summary>
    /// <returns>The array metric count, or zero for a range entry.</returns>
    public int GetArrayMetricCount()
    {
        return arrayMetrics.Length;
    }

    /// <summary>Gets one metric from an array entry.</summary>
    /// <param name="index">The zero-based metric index.</param>
    /// <returns>The vertical displacement and origin.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// This is a range entry.
    /// </exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the array entry.
    /// </exception>
    public PdfCidVerticalMetric GetArrayMetric(int index)
    {
        if (kind != PdfCidWidthEntryKind.Array)
        {
            throw new System.InvalidOperationException(
                "A range vertical width entry has no metric array.");
        }

        if (index < 0 || index >= arrayMetrics.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return arrayMetrics[index];
    }

    /// <summary>Copies the metrics of an array entry.</summary>
    /// <returns>A new array, or an empty array for a range entry.</returns>
    public PdfCidVerticalMetric[] GetArrayMetrics()
    {
        return CopyMetrics(arrayMetrics);
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
