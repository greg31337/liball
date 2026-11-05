using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents content type override data used by the public workbook API.
/// </summary>
public class XlsxContentTypeOverride
{
    /// <summary>
    /// Stores the part name value used by this model.
    /// </summary>
    public string PartName;
    /// <summary>
    /// Stores the content type value used by this model.
    /// </summary>
    public string? ContentType;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new content type override with its default state.
    /// </summary>
    public XlsxContentTypeOverride()
    {
        PartName = "";
        ContentType = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
