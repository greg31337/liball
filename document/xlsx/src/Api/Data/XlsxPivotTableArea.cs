using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents pivot table area data used by the public workbook API.
/// </summary>
public class XlsxPivotTableArea
{
    /// <summary>
    /// Stores the type value used by this model.
    /// </summary>
    public string Type;
    /// <summary>
    /// Stores the field index value used by this model.
    /// </summary>
    public int FieldIndex;
    /// <summary>
    /// Indicates whether field index is present and should be written.
    /// </summary>
    public bool HasFieldIndex;
    /// <summary>
    /// Contains axis.
    /// </summary>
    public string Axis;
    /// <summary>
    /// Contains references.
    /// </summary>
    public List<XlsxPivotTableAreaReference> References;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Initializes a new pivot table area with its default state.
    /// </summary>
    public XlsxPivotTableArea()
    {
        Type = "";
        FieldIndex = 0;
        HasFieldIndex = false;
        Axis = "";
        References = new List<XlsxPivotTableAreaReference>();
        RawXml = "";
    }
}
