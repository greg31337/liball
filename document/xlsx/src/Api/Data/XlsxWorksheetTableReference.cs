using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents worksheet table reference data used by the public workbook API.
/// </summary>
public class XlsxWorksheetTableReference
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
    /// Initializes a new worksheet table reference with its default state.
    /// </summary>
    public XlsxWorksheetTableReference()
    {
        RelationshipId = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
