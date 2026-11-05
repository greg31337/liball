using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents r1 c1 reference value data used by the public workbook API.
/// </summary>
public class XlsxR1C1ReferenceValue
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
    /// Indicates whether row is present and should be written.
    /// </summary>
    public bool HasRow;
    /// <summary>
    /// Indicates whether column is present and should be written.
    /// </summary>
    public bool HasColumn;
    /// <summary>
    /// Indicates whether relative row.
    /// </summary>
    public bool IsRelativeRow;
    /// <summary>
    /// Indicates whether relative column.
    /// </summary>
    public bool IsRelativeColumn;
    /// <summary>
    /// Initializes a new r1 c1 reference value with its default state.
    /// </summary>
    public XlsxR1C1ReferenceValue()
    {
        Text = "";
        Row = 0;
        Column = 0;
        HasRow = false;
        HasColumn = false;
        IsRelativeRow = true;
        IsRelativeColumn = true;
    }
}
