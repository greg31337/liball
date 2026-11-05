using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents shared string rich text run data used by the public workbook API.
/// </summary>
public class XlsxSharedStringRichTextRun
{
    /// <summary>
    /// Stores the text value used by this model.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string PropertiesRawXml;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new shared string rich text run with its default state.
    /// </summary>
    public XlsxSharedStringRichTextRun()
    {
        Text = "";
        RawXml = "";
        PropertiesRawXml = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
