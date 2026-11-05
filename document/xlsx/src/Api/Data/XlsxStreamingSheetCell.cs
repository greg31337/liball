using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents streaming sheet cell data used by the public workbook API.
/// </summary>
public class XlsxStreamingSheetCell
{
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string Reference;
    /// <summary>
    /// Stores the cell value used by this model.
    /// </summary>
    public XlsxWorksheetCell Cell;
    /// <summary>
    /// Stores the value value used by this model.
    /// </summary>
    public XlsxCellValue Value;
    /// <summary>
    /// Stores the format code value used by this model.
    /// </summary>
    public string FormatCode;
    /// <summary>
    /// Stores the formatted value value used by this model.
    /// </summary>
    public string FormattedValue;
    /// <summary>
    /// Initializes a new streaming sheet cell with its default state.
    /// </summary>
    public XlsxStreamingSheetCell()
    {
        Reference = "";
        Cell = new XlsxWorksheetCell();
        Value = new XlsxCellValue();
        FormatCode = "General";
        FormattedValue = "";
    }
}
