using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents pivot table location data used by the public workbook API.
/// </summary>
public class XlsxPivotTableLocation
{
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string Reference;
    /// <summary>
    /// Stores the first header row value used by this model.
    /// </summary>
    public uint FirstHeaderRow;
    /// <summary>
    /// Indicates whether first header row is present and should be written.
    /// </summary>
    public bool HasFirstHeaderRow;
    /// <summary>
    /// Stores the first data row value used by this model.
    /// </summary>
    public uint FirstDataRow;
    /// <summary>
    /// Indicates whether first data row is present and should be written.
    /// </summary>
    public bool HasFirstDataRow;
    /// <summary>
    /// Stores the first data column value used by this model.
    /// </summary>
    public uint FirstDataColumn;
    /// <summary>
    /// Indicates whether first data column is present and should be written.
    /// </summary>
    public bool HasFirstDataColumn;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Initializes a new pivot table location with its default state.
    /// </summary>
    public XlsxPivotTableLocation()
    {
        Reference = "";
        FirstHeaderRow = 0;
        HasFirstHeaderRow = false;
        FirstDataRow = 0;
        HasFirstDataRow = false;
        FirstDataColumn = 0;
        HasFirstDataColumn = false;
        RawXml = "";
    }
}
