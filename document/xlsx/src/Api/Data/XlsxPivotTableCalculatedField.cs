using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents pivot table calculated field data used by the public workbook API.
/// </summary>
public class XlsxPivotTableCalculatedField
{
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the formula value used by this model.
    /// </summary>
    public string Formula;
    /// <summary>
    /// Stores the field index value used by this model.
    /// </summary>
    public int FieldIndex;
    /// <summary>
    /// Indicates whether field index is present and should be written.
    /// </summary>
    public bool HasFieldIndex;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Initializes a new pivot table calculated field with its default state.
    /// </summary>
    public XlsxPivotTableCalculatedField()
    {
        Name = "";
        Formula = "";
        FieldIndex = 0;
        HasFieldIndex = false;
        RawXml = "";
    }
}
