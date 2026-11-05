using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents conditional format data bar data used by the public workbook API.
/// </summary>
public class XlsxConditionalFormatDataBar
{
    /// <summary>
    /// Stores the min length value used by this model.
    /// </summary>
    public uint MinLength;
    /// <summary>
    /// Indicates whether min length is present and should be written.
    /// </summary>
    public bool HasMinLength;
    /// <summary>
    /// Stores the max length value used by this model.
    /// </summary>
    public uint MaxLength;
    /// <summary>
    /// Indicates whether max length is present and should be written.
    /// </summary>
    public bool HasMaxLength;
    /// <summary>
    /// Stores the show value value used by this model.
    /// </summary>
    public bool ShowValue;
    /// <summary>
    /// Indicates whether show value is present and should be written.
    /// </summary>
    public bool HasShowValue;
    /// <summary>
    /// Contains values.
    /// </summary>
    public List<XlsxConditionalFormatValueObject> Values;
    /// <summary>
    /// Contains colors.
    /// </summary>
    public List<XlsxConditionalFormatColor> Colors;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new conditional format data bar with its default state.
    /// </summary>
    public XlsxConditionalFormatDataBar()
    {
        MinLength = 0;
        HasMinLength = false;
        MaxLength = 0;
        HasMaxLength = false;
        ShowValue = false;
        HasShowValue = false;
        Values = new List<XlsxConditionalFormatValueObject>();
        Colors = new List<XlsxConditionalFormatColor>();
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
