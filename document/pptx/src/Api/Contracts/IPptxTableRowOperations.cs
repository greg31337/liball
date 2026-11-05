using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxTableRowOperations"/>.
/// </summary>
public interface IPptxTableRowOperations
{
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int CellCount(PptxTableRow target);
    /// <summary>
    /// Returns the cell without changing the table row.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting table cell value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxTableCell GetCell(PptxTableRow target, int index);
    /// <summary>
    /// Returns the height without changing the table row.
    /// </summary>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    long GetHeight(PptxTableRow target);
    /// <summary>
    /// Updates the height and the underlying presentation data.
    /// </summary>
    /// <param name="height">The height in English Metric Units (EMUs), unless the method states another unit.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetHeight(PptxTableRow target, long height);
}
