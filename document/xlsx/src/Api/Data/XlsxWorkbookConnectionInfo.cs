using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents workbook connection info data used by the public workbook API.
/// </summary>
public class XlsxWorkbookConnectionInfo
{
    /// <summary>
    /// Stores the id value used by this model.
    /// </summary>
    public uint Id;
    /// <summary>
    /// Indicates whether id is present and should be written.
    /// </summary>
    public bool HasId;
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string Name;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new workbook connection info with its default state.
    /// </summary>
    public XlsxWorkbookConnectionInfo()
    {
        Id = 0;
        HasId = false;
        Name = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
