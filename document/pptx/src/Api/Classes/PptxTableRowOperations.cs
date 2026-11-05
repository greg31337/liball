using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxTableRow values.
/// </summary>
public class PptxTableRowOperations : IPptxTableRowOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxTableRowOperations()
    {
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int CellCount(PptxTableRow target)
    {
        return target.CellCount();
    }

    /// <summary>
    /// Returns the cell without changing the table row.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting table cell value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxTableCell GetCell(PptxTableRow target, int index)
    {
        return target.GetCell(index);
    }

    /// <summary>
    /// Returns the height without changing the table row.
    /// </summary>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public long GetHeight(PptxTableRow target)
    {
        return target.GetHeight();
    }

    /// <summary>
    /// Updates the height and the underlying presentation data.
    /// </summary>
    /// <param name="height">The height in English Metric Units (EMUs), unless the method states another unit.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetHeight(PptxTableRow target, long height)
    {
        target.SetHeight(height);
    }
}
