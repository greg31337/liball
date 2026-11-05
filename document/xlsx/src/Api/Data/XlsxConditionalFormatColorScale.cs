using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents conditional format color scale data used by the public workbook API.
/// </summary>
public class XlsxConditionalFormatColorScale
{
    /// <summary>
    /// Contains values.
    /// </summary>
    public List<XlsxConditionalFormatValueObject> Values;
    /// <summary>
    /// Contains colors.
    /// </summary>
    public List<XlsxConditionalFormatColor> Colors;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new conditional format color scale with its default state.
    /// </summary>
    public XlsxConditionalFormatColorScale()
    {
        Values = new List<XlsxConditionalFormatValueObject>();
        Colors = new List<XlsxConditionalFormatColor>();
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
