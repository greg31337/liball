using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents worksheet merge cell data used by the public workbook API.
/// </summary>
public class XlsxWorksheetMergeCell
{
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string? Reference;
    /// <summary>
    /// Stores the first row value used by this model.
    /// </summary>
    public int FirstRow;
    /// <summary>
    /// Stores the first column value used by this model.
    /// </summary>
    public int FirstColumn;
    /// <summary>
    /// Stores the last row value used by this model.
    /// </summary>
    public int LastRow;
    /// <summary>
    /// Stores the last column value used by this model.
    /// </summary>
    public int LastColumn;
    /// <summary>
    /// Indicates whether range is present and should be written.
    /// </summary>
    public bool HasRange;
    /// <summary>
    /// Initializes a new worksheet merge cell with its default state.
    /// </summary>
    public XlsxWorksheetMergeCell()
    {
        Reference = "";
        FirstRow = 0;
        FirstColumn = 0;
        LastRow = 0;
        LastColumn = 0;
        HasRange = false;
    }
}
