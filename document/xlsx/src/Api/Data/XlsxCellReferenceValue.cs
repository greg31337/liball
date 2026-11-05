using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents cell reference value data used by the public workbook API.
/// </summary>
public class XlsxCellReferenceValue
{
    /// <summary>
    /// Stores the text value used by this model.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores the row value used by this model.
    /// </summary>
    public int Row;
    /// <summary>
    /// Stores the column value used by this model.
    /// </summary>
    public int Column;
    /// <summary>
    /// Indicates whether absolute row.
    /// </summary>
    public bool IsAbsoluteRow;
    /// <summary>
    /// Indicates whether absolute column.
    /// </summary>
    public bool IsAbsoluteColumn;
    /// <summary>
    /// Initializes a new cell reference value with its default state.
    /// </summary>
    public XlsxCellReferenceValue()
    {
        Text = "";
        Row = 0;
        Column = 0;
        IsAbsoluteRow = false;
        IsAbsoluteColumn = false;
    }
}
