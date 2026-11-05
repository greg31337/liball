using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents pivot table style info data used by the public workbook API.
/// </summary>
public class XlsxPivotTableStyleInfo
{
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string Name;
    /// <summary>
    /// Contains show row headers.
    /// </summary>
    public bool ShowRowHeaders;
    /// <summary>
    /// Indicates whether show row headers is present and should be written.
    /// </summary>
    public bool HasShowRowHeaders;
    /// <summary>
    /// Contains show column headers.
    /// </summary>
    public bool ShowColumnHeaders;
    /// <summary>
    /// Indicates whether show column headers is present and should be written.
    /// </summary>
    public bool HasShowColumnHeaders;
    /// <summary>
    /// Contains show row stripes.
    /// </summary>
    public bool ShowRowStripes;
    /// <summary>
    /// Indicates whether show row stripes is present and should be written.
    /// </summary>
    public bool HasShowRowStripes;
    /// <summary>
    /// Contains show column stripes.
    /// </summary>
    public bool ShowColumnStripes;
    /// <summary>
    /// Indicates whether show column stripes is present and should be written.
    /// </summary>
    public bool HasShowColumnStripes;
    /// <summary>
    /// Stores the show last column value used by this model.
    /// </summary>
    public bool ShowLastColumn;
    /// <summary>
    /// Indicates whether show last column is present and should be written.
    /// </summary>
    public bool HasShowLastColumn;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Initializes a new pivot table style info with its default state.
    /// </summary>
    public XlsxPivotTableStyleInfo()
    {
        Name = "";
        ShowRowHeaders = false;
        HasShowRowHeaders = false;
        ShowColumnHeaders = false;
        HasShowColumnHeaders = false;
        ShowRowStripes = false;
        HasShowRowStripes = false;
        ShowColumnStripes = false;
        HasShowColumnStripes = false;
        ShowLastColumn = false;
        HasShowLastColumn = false;
        RawXml = "";
    }
}
