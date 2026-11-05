using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents xml attribute data used by the public workbook API.
/// </summary>
public class XlsxXmlAttribute
{
    /// <summary>
    /// Stores the namespace uri value used by this model.
    /// </summary>
    public string NamespaceUri;
    /// <summary>
    /// Stores the local name value used by this model.
    /// </summary>
    public string LocalName;
    /// <summary>
    /// Stores the prefix value used by this model.
    /// </summary>
    public string Prefix;
    /// <summary>
    /// Stores the value value used by this model.
    /// </summary>
    public string Value;
    /// <summary>
    /// Initializes a new xml attribute with its default state.
    /// </summary>
    public XlsxXmlAttribute()
    {
        NamespaceUri = "";
        LocalName = "";
        Prefix = "";
        Value = "";
    }
}
