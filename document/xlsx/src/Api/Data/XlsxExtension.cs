using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents extension data used by the public workbook API.
/// </summary>
public class XlsxExtension
{
    /// <summary>
    /// Stores the uri value used by this model.
    /// </summary>
    public string? Uri;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Stores the namespace uri value used by this model.
    /// </summary>
    public string NamespaceUri;
    /// <summary>
    /// Stores the prefix value used by this model.
    /// </summary>
    public string Prefix;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new extension with its default state.
    /// </summary>
    public XlsxExtension()
    {
        Uri = "";
        RawXml = "";
        NamespaceUri = "";
        Prefix = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
