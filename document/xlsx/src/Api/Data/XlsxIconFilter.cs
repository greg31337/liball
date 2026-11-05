using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents icon filter data used by the public workbook API.
/// </summary>
public class XlsxIconFilter
{
    /// <summary>
    /// Stores the icon set value used by this model.
    /// </summary>
    public string IconSet;
    /// <summary>
    /// Stores the icon id value used by this model.
    /// </summary>
    public uint IconId;
    /// <summary>
    /// Indicates whether icon id is present and should be written.
    /// </summary>
    public bool HasIconId;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new icon filter with its default state.
    /// </summary>
    public XlsxIconFilter()
    {
        IconSet = "";
        IconId = 0;
        HasIconId = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
