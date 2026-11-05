using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents conditional format color data used by the public workbook API.
/// </summary>
public class XlsxConditionalFormatColor
{
    /// <summary>
    /// Stores the rgb value used by this model.
    /// </summary>
    public string Rgb;
    /// <summary>
    /// Stores the indexed value used by this model.
    /// </summary>
    public uint Indexed;
    /// <summary>
    /// Indicates whether indexed is present and should be written.
    /// </summary>
    public bool HasIndexed;
    /// <summary>
    /// Stores the theme value used by this model.
    /// </summary>
    public uint Theme;
    /// <summary>
    /// Indicates whether theme is present and should be written.
    /// </summary>
    public bool HasTheme;
    /// <summary>
    /// Stores the tint value used by this model.
    /// </summary>
    public double Tint;
    /// <summary>
    /// Indicates whether tint is present and should be written.
    /// </summary>
    public bool HasTint;
    /// <summary>
    /// Stores the auto value used by this model.
    /// </summary>
    public bool Auto;
    /// <summary>
    /// Indicates whether auto is present and should be written.
    /// </summary>
    public bool HasAuto;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new conditional format color with its default state.
    /// </summary>
    public XlsxConditionalFormatColor()
    {
        Rgb = "";
        Indexed = 0;
        HasIndexed = false;
        Theme = 0;
        HasTheme = false;
        Tint = 0;
        HasTint = false;
        Auto = false;
        HasAuto = false;
        RawXml = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
