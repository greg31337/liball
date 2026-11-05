using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents streaming workbook sheet info data used by the public workbook API.
/// </summary>
public class XlsxStreamingWorkbookSheetInfo
{
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the sheet id value used by this model.
    /// </summary>
    public uint SheetId;
    /// <summary>
    /// Stores the relationship id value used by this model.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// Stores the normalized absolute package path, beginning with <c>/</c>.
    /// </summary>
    public string PartPath;
    /// <summary>
    /// Stores the number of row items processed by the operation.
    /// </summary>
    public int RowCount;
    /// <summary>
    /// Stores the number of cell items processed by the operation.
    /// </summary>
    public int CellCount;
    /// <summary>
    /// Initializes a new streaming workbook sheet info with its default state.
    /// </summary>
    public XlsxStreamingWorkbookSheetInfo()
    {
        Name = "";
        SheetId = 0;
        RelationshipId = "";
        PartPath = "";
        RowCount = 0;
        CellCount = 0;
    }
}
