using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents theme format scheme data used by the public workbook API.
/// </summary>
public class XlsxThemeFormatScheme
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
    /// Stores the number of fill style items processed by the operation.
    /// </summary>
    public int FillStyleCount;
    /// <summary>
    /// Stores the number of line style items processed by the operation.
    /// </summary>
    public int LineStyleCount;
    /// <summary>
    /// Stores the number of effect style items processed by the operation.
    /// </summary>
    public int EffectStyleCount;
    /// <summary>
    /// Stores the number of background fill style items processed by the operation.
    /// </summary>
    public int BackgroundFillStyleCount;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new theme format scheme with its default state.
    /// </summary>
    public XlsxThemeFormatScheme()
    {
        Name = "";
        RawXml = "";
        FillStyleCount = 0;
        LineStyleCount = 0;
        EffectStyleCount = 0;
        BackgroundFillStyleCount = 0;
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
