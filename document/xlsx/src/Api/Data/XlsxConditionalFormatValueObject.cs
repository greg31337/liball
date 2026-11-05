using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents conditional format value object data used by the public workbook API.
/// </summary>
public class XlsxConditionalFormatValueObject
{
    /// <summary>
    /// Stores the type value used by this model.
    /// </summary>
    public string Type;
    /// <summary>
    /// Stores the value value used by this model.
    /// </summary>
    public string Value;
    /// <summary>
    /// Stores the greater than or equal value used by this model.
    /// </summary>
    public bool GreaterThanOrEqual;
    /// <summary>
    /// Indicates whether greater than or equal is present and should be written.
    /// </summary>
    public bool HasGreaterThanOrEqual;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new conditional format value object with its default state.
    /// </summary>
    public XlsxConditionalFormatValueObject()
    {
        Type = "";
        Value = "";
        GreaterThanOrEqual = false;
        HasGreaterThanOrEqual = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
