using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents slicer pivot table reference data used by the public workbook API.
/// </summary>
public class XlsxSlicerPivotTableReference
{
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the tab id value used by this model.
    /// </summary>
    public uint TabId;
    /// <summary>
    /// Indicates whether tab id is present and should be written.
    /// </summary>
    public bool HasTabId;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Initializes a new slicer pivot table reference with its default state.
    /// </summary>
    public XlsxSlicerPivotTableReference()
    {
        Name = "";
        TabId = 0;
        HasTabId = false;
        RawXml = "";
    }
}
