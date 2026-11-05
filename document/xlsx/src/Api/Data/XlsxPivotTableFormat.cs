using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents pivot table format data used by the public workbook API.
/// </summary>
public class XlsxPivotTableFormat
{
    /// <summary>
    /// Stores the action value used by this model.
    /// </summary>
    public string Action;
    /// <summary>
    /// Stores the differential format id value used by this model.
    /// </summary>
    public uint DifferentialFormatId;
    /// <summary>
    /// Indicates whether differential format id is present and should be written.
    /// </summary>
    public bool HasDifferentialFormatId;
    /// <summary>
    /// Stores the area value used by this model.
    /// </summary>
    public XlsxPivotTableArea Area;
    /// <summary>
    /// Indicates whether area is present and should be written.
    /// </summary>
    public bool HasArea;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Initializes a new pivot table format with its default state.
    /// </summary>
    public XlsxPivotTableFormat()
    {
        Action = "";
        DifferentialFormatId = 0;
        HasDifferentialFormatId = false;
        Area = new XlsxPivotTableArea();
        HasArea = false;
        RawXml = "";
    }
}
