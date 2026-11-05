using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Represents one row returned by range enumeration.
/// </summary>
public class XlsxRangeRowItem
{
    /// <summary>
    /// Stores the one-based worksheet row index.
    /// </summary>
    public int Row;
    /// <summary>
    /// Contains enumerated cells in column order.
    /// </summary>
    public List<XlsxRangeCellItem> Cells;

    /// <summary>
    /// Initializes an empty range row item.
    /// </summary>
    public XlsxRangeRowItem()
    {
        Row = 0;
        Cells = new List<XlsxRangeCellItem>();
    }
}
