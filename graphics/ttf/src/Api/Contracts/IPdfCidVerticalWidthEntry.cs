namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfCidVerticalWidthEntry"/>.</summary>
public interface IPdfCidVerticalWidthEntry
{
    /// <summary>Gets the PDF vertical width-entry representation.</summary>
    /// <returns>Range or array.</returns>
    PdfCidWidthEntryKind GetKind();

    /// <summary>Gets the first CID represented by the entry.</summary>
    /// <returns>The inclusive first CID.</returns>
    int GetFirstCid();

    /// <summary>Gets the last CID represented by the entry.</summary>
    /// <returns>The inclusive last CID.</returns>
    int GetLastCid();

    /// <summary>Gets the common metric of a range entry.</summary>
    /// <returns>The range's vertical displacement and origin.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// This is an array entry.
    /// </exception>
    PdfCidVerticalMetric? GetRangeMetric();

    /// <summary>Gets the number of metrics in an array entry.</summary>
    /// <returns>The array metric count, or zero for a range entry.</returns>
    int GetArrayMetricCount();

    /// <summary>Gets one metric from an array entry.</summary>
    /// <param name="index">The zero-based metric index.</param>
    /// <returns>The vertical displacement and origin.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// This is a range entry.
    /// </exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the array entry.
    /// </exception>
    PdfCidVerticalMetric GetArrayMetric(int index);

    /// <summary>Copies the metrics of an array entry.</summary>
    /// <returns>A new array, or an empty array for a range entry.</returns>
    PdfCidVerticalMetric[] GetArrayMetrics();
}
