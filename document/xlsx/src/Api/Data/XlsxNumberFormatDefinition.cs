using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents number format definition data used by the public workbook API.
/// </summary>
public class XlsxNumberFormatDefinition
{
    /// <summary>
    /// Stores the number format id value used by this model.
    /// </summary>
    public uint NumberFormatId;
    /// <summary>
    /// Indicates whether number format id is present and should be written.
    /// </summary>
    public bool HasNumberFormatId;
    /// <summary>
    /// Stores the format code value used by this model.
    /// </summary>
    public string? FormatCode;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new number format definition with its default state.
    /// </summary>
    public XlsxNumberFormatDefinition()
    {
        NumberFormatId = 0;
        HasNumberFormatId = false;
        FormatCode = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
