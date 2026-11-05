using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents streaming sheet row data used by the public workbook API.
/// </summary>
public class XlsxStreamingSheetRow
{
    /// <summary>
    /// Stores the row value used by this model.
    /// </summary>
    public XlsxWorksheetRow Row;
    /// <summary>
    /// Stores the row index value used by this model.
    /// </summary>
    public uint RowIndex;
    /// <summary>
    /// Contains cells.
    /// </summary>
    public List<XlsxStreamingSheetCell> Cells;
    /// <summary>
    /// Initializes a new streaming sheet row with its default state.
    /// </summary>
    public XlsxStreamingSheetRow()
    {
        Row = new XlsxWorksheetRow();
        RowIndex = 0;
        Cells = new List<XlsxStreamingSheetCell>();
    }

    /// <summary>
    /// Returns cell from the current model.
    /// </summary>
    /// <param name = "reference">An A1-style cell or range reference.</param>
    /// <returns>The streaming sheet cell produced by the operation.</returns>
    internal XlsxStreamingSheetCell? GetCell(string reference)
    {
        foreach (XlsxStreamingSheetCell cell in Cells)
        {
            if (cell.Reference == reference)
            {
                return cell;
            }
        }

        return null;
    }
}
