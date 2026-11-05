using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents pivot table item data used by the public workbook API.
/// </summary>
public class XlsxPivotTableItem
{
    /// <summary>
    /// Stores the type value used by this model.
    /// </summary>
    public string Type;
    /// <summary>
    /// Stores the index value used by this model.
    /// </summary>
    public int Index;
    /// <summary>
    /// Indicates whether index is present and should be written.
    /// </summary>
    public bool HasIndex;
    /// <summary>
    /// Stores the hidden value used by this model.
    /// </summary>
    public bool Hidden;
    /// <summary>
    /// Indicates whether hidden is present and should be written.
    /// </summary>
    public bool HasHidden;
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Initializes a new pivot table item with its default state.
    /// </summary>
    public XlsxPivotTableItem()
    {
        Type = "";
        Index = 0;
        HasIndex = false;
        Hidden = false;
        HasHidden = false;
        Name = "";
        RawXml = "";
    }
}
