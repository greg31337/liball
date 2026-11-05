using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents drawing marker data used by the public workbook API.
/// </summary>
public class XlsxDrawingMarker
{
    /// <summary>
    /// Stores the column value used by this model.
    /// </summary>
    public uint Column;
    /// <summary>
    /// Stores the column offset emu value used by this model.
    /// </summary>
    public string ColumnOffsetEmu;
    /// <summary>
    /// Stores the row value used by this model.
    /// </summary>
    public uint Row;
    /// <summary>
    /// Stores the row offset emu value used by this model.
    /// </summary>
    public string RowOffsetEmu;
    /// <summary>
    /// Initializes a new drawing marker with its default state.
    /// </summary>
    public XlsxDrawingMarker()
    {
        Column = 0;
        ColumnOffsetEmu = "0";
        Row = 0;
        RowOffsetEmu = "0";
    }
}
