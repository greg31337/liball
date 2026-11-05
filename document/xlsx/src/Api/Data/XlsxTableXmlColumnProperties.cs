using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Represents the typed XML mapping attached to a table column.
/// </summary>
public class XlsxTableXmlColumnProperties
{
    /// <summary>
    /// Stores the workbook XML map identifier.
    /// </summary>
    public uint MapId;
    /// <summary>
    /// Indicates whether the XML map identifier is present.
    /// </summary>
    public bool HasMapId;
    /// <summary>
    /// Stores the XPath mapped to the table column.
    /// </summary>
    public string XPath;
    /// <summary>
    /// Stores the SpreadsheetML XML data-type token.
    /// </summary>
    public string XmlDataType;
    /// <summary>
    /// Indicates whether mapped values are denormalized.
    /// </summary>
    public bool Denormalized;
    /// <summary>
    /// Indicates whether the denormalized flag is present.
    /// </summary>
    public bool HasDenormalized;
    /// <summary>
    /// Stores preserved XML for the mapping extension list.
    /// </summary>
    public string ExtensionListRawXml;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML not represented by typed fields.
    /// </summary>
    public List<string> UnknownXml;

    /// <summary>
    /// Initializes an empty table XML column mapping.
    /// </summary>
    public XlsxTableXmlColumnProperties()
    {
        MapId = 0;
        HasMapId = false;
        XPath = "";
        XmlDataType = "";
        Denormalized = false;
        HasDenormalized = false;
        ExtensionListRawXml = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
