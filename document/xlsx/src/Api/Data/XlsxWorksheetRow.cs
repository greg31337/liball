using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents worksheet row data used by the public workbook API.
/// </summary>
public class XlsxWorksheetRow
{
    /// <summary>
    /// Stores the row index value used by this model.
    /// </summary>
    public uint RowIndex;
    /// <summary>
    /// Contains spans.
    /// </summary>
    public string? Spans;
    /// <summary>
    /// Stores the height value used by this model.
    /// </summary>
    public double Height;
    /// <summary>
    /// Indicates whether height is present and should be written.
    /// </summary>
    public bool HasHeight;
    /// <summary>
    /// Stores the style value used by this model.
    /// </summary>
    public uint Style;
    /// <summary>
    /// Indicates whether style is present and should be written.
    /// </summary>
    public bool HasStyle;
    /// <summary>
    /// Stores the hidden value used by this model.
    /// </summary>
    public bool Hidden;
    /// <summary>
    /// Indicates whether hidden is present and should be written.
    /// </summary>
    public bool HasHidden;
    /// <summary>
    /// Stores the custom height value used by this model.
    /// </summary>
    public bool CustomHeight;
    /// <summary>
    /// Indicates whether custom height is present and should be written.
    /// </summary>
    public bool HasCustomHeight;
    /// <summary>
    /// Contains cells.
    /// </summary>
    public List<XlsxWorksheetCell> Cells;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new worksheet row with its default state.
    /// </summary>
    public XlsxWorksheetRow()
    {
        RowIndex = 0;
        Spans = "";
        Height = 0;
        HasHeight = false;
        Style = 0;
        HasStyle = false;
        Hidden = false;
        HasHidden = false;
        CustomHeight = false;
        HasCustomHeight = false;
        Cells = new List<XlsxWorksheetCell>();
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
