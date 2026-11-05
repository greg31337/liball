using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents color filter data used by the public workbook API.
/// </summary>
public class XlsxColorFilter
{
    /// <summary>
    /// Stores the dxf id value used by this model.
    /// </summary>
    public uint DxfId;
    /// <summary>
    /// Indicates whether dxf id is present and should be written.
    /// </summary>
    public bool HasDxfId;
    /// <summary>
    /// Stores the cell color value used by this model.
    /// </summary>
    public bool CellColor;
    /// <summary>
    /// Indicates whether cell color is present and should be written.
    /// </summary>
    public bool HasCellColor;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new color filter with its default state.
    /// </summary>
    public XlsxColorFilter()
    {
        DxfId = 0;
        HasDxfId = false;
        CellColor = false;
        HasCellColor = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
