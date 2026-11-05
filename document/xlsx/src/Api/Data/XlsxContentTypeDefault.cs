using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents content type default data used by the public workbook API.
/// </summary>
public class XlsxContentTypeDefault
{
    /// <summary>
    /// Stores the extension value used by this model.
    /// </summary>
    public string? Extension;
    /// <summary>
    /// Stores the content type value used by this model.
    /// </summary>
    public string? ContentType;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new content type default with its default state.
    /// </summary>
    public XlsxContentTypeDefault()
    {
        Extension = "";
        ContentType = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
