using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents auto filter values data used by the public workbook API.
/// </summary>
public class XlsxAutoFilterValues
{
    /// <summary>
    /// Stores the blank value used by this model.
    /// </summary>
    public bool Blank;
    /// <summary>
    /// Indicates whether blank is present and should be written.
    /// </summary>
    public bool HasBlank;
    /// <summary>
    /// Stores the calendar type value used by this model.
    /// </summary>
    public string CalendarType;
    /// <summary>
    /// Contains values.
    /// </summary>
    public List<string> Values;
    /// <summary>
    /// Contains date group items.
    /// </summary>
    public List<XlsxDateGroupItem> DateGroupItems;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new auto filter values with its default state.
    /// </summary>
    public XlsxAutoFilterValues()
    {
        Blank = false;
        HasBlank = false;
        CalendarType = "";
        Values = new List<string>();
        DateGroupItems = new List<XlsxDateGroupItem>();
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
