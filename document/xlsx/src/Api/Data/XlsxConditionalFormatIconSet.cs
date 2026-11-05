using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents conditional format icon set data used by the public workbook API.
/// </summary>
public class XlsxConditionalFormatIconSet
{
    /// <summary>
    /// Stores the icon set value used by this model.
    /// </summary>
    public string IconSet;
    /// <summary>
    /// Stores the show value value used by this model.
    /// </summary>
    public bool ShowValue;
    /// <summary>
    /// Indicates whether show value is present and should be written.
    /// </summary>
    public bool HasShowValue;
    /// <summary>
    /// Stores the percent value used by this model.
    /// </summary>
    public bool Percent;
    /// <summary>
    /// Indicates whether percent is present and should be written.
    /// </summary>
    public bool HasPercent;
    /// <summary>
    /// Stores the reverse value used by this model.
    /// </summary>
    public bool Reverse;
    /// <summary>
    /// Indicates whether reverse is present and should be written.
    /// </summary>
    public bool HasReverse;
    /// <summary>
    /// Stores the custom value used by this model.
    /// </summary>
    public bool Custom;
    /// <summary>
    /// Indicates whether custom is present and should be written.
    /// </summary>
    public bool HasCustom;
    /// <summary>
    /// Contains values.
    /// </summary>
    public List<XlsxConditionalFormatValueObject> Values;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new conditional format icon set with its default state.
    /// </summary>
    public XlsxConditionalFormatIconSet()
    {
        IconSet = "";
        ShowValue = false;
        HasShowValue = false;
        Percent = false;
        HasPercent = false;
        Reverse = false;
        HasReverse = false;
        Custom = false;
        HasCustom = false;
        Values = new List<XlsxConditionalFormatValueObject>();
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
