using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents pivot table field reference data used by the public workbook API.
/// </summary>
public class XlsxPivotTableFieldReference
{
    /// <summary>
    /// Stores the field index value used by this model.
    /// </summary>
    public int FieldIndex;
    /// <summary>
    /// Indicates whether field index is present and should be written.
    /// </summary>
    public bool HasFieldIndex;
    /// <summary>
    /// Stores the item index value used by this model.
    /// </summary>
    public int ItemIndex;
    /// <summary>
    /// Indicates whether item index is present and should be written.
    /// </summary>
    public bool HasItemIndex;
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Initializes a new pivot table field reference with its default state.
    /// </summary>
    public XlsxPivotTableFieldReference()
    {
        FieldIndex = 0;
        HasFieldIndex = false;
        ItemIndex = 0;
        HasItemIndex = false;
        Name = "";
        RawXml = "";
    }
}
