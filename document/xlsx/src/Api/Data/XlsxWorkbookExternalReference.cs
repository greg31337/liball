using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents workbook external reference data used by the public workbook API.
/// </summary>
public class XlsxWorkbookExternalReference
{
    /// <summary>
    /// Stores the relationship id value used by this model.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new workbook external reference with its default state.
    /// </summary>
    public XlsxWorkbookExternalReference()
    {
        RelationshipId = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
