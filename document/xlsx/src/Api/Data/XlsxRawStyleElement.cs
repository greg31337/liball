using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents raw style element data used by the public workbook API.
/// </summary>
public class XlsxRawStyleElement
{
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new raw style element with its default state.
    /// </summary>
    public XlsxRawStyleElement()
    {
        RawXml = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
