using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents table style info data used by the public workbook API.
/// </summary>
public class XlsxTableStyleInfo
{
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the show first column value used by this model.
    /// </summary>
    public bool ShowFirstColumn;
    /// <summary>
    /// Indicates whether show first column is present and should be written.
    /// </summary>
    public bool HasShowFirstColumn;
    /// <summary>
    /// Stores the show last column value used by this model.
    /// </summary>
    public bool ShowLastColumn;
    /// <summary>
    /// Indicates whether show last column is present and should be written.
    /// </summary>
    public bool HasShowLastColumn;
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
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new table style info with its default state.
    /// </summary>
    public XlsxTableStyleInfo()
    {
        Name = "";
        ShowFirstColumn = false;
        HasShowFirstColumn = false;
        ShowLastColumn = false;
        HasShowLastColumn = false;
        ShowRowStripes = false;
        HasShowRowStripes = false;
        ShowColumnStripes = false;
        HasShowColumnStripes = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
