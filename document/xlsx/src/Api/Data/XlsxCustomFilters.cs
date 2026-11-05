using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents custom filters data used by the public workbook API.
/// </summary>
public class XlsxCustomFilters
{
    /// <summary>
    /// Stores the and value used by this model.
    /// </summary>
    public bool And;
    /// <summary>
    /// Indicates whether and is present and should be written.
    /// </summary>
    public bool HasAnd;
    /// <summary>
    /// Contains filters.
    /// </summary>
    public List<XlsxCustomFilter> Filters;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new custom filters with its default state.
    /// </summary>
    public XlsxCustomFilters()
    {
        And = false;
        HasAnd = false;
        Filters = new List<XlsxCustomFilter>();
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
