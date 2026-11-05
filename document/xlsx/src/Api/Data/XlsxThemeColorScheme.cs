using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents theme color scheme data used by the public workbook API.
/// </summary>
public class XlsxThemeColorScheme
{
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string? Name;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Contains colors.
    /// </summary>
    public List<XlsxThemeColor> Colors;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new theme color scheme with its default state.
    /// </summary>
    public XlsxThemeColorScheme()
    {
        Name = "";
        RawXml = "";
        Colors = new List<XlsxThemeColor>();
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
