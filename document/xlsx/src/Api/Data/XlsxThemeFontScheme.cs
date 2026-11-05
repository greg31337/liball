using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents theme font scheme data used by the public workbook API.
/// </summary>
public class XlsxThemeFontScheme
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
    /// Stores the major font value used by this model.
    /// </summary>
    public XlsxThemeFontSet MajorFont;
    /// <summary>
    /// Indicates whether major font is present and should be written.
    /// </summary>
    public bool HasMajorFont;
    /// <summary>
    /// Stores the minor font value used by this model.
    /// </summary>
    public XlsxThemeFontSet MinorFont;
    /// <summary>
    /// Indicates whether minor font is present and should be written.
    /// </summary>
    public bool HasMinorFont;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new theme font scheme with its default state.
    /// </summary>
    public XlsxThemeFontScheme()
    {
        Name = "";
        RawXml = "";
        MajorFont = new XlsxThemeFontSet();
        HasMajorFont = false;
        MinorFont = new XlsxThemeFontSet();
        HasMinorFont = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
