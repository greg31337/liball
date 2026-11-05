using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents theme color data used by the public workbook API.
/// </summary>
public class XlsxThemeColor
{
    /// <summary>
    /// Stores the slot value used by this model.
    /// </summary>
    public string Slot;
    /// <summary>
    /// Stores the kind value used by this model.
    /// </summary>
    public string Kind;
    /// <summary>
    /// Stores the value value used by this model.
    /// </summary>
    public string? Value;
    /// <summary>
    /// Stores the last color value used by this model.
    /// </summary>
    public string? LastColor;
    /// <summary>
    /// Stores the tint value used by this model.
    /// </summary>
    public uint Tint;
    /// <summary>
    /// Indicates whether tint is present and should be written.
    /// </summary>
    public bool HasTint;
    /// <summary>
    /// Stores the shade value used by this model.
    /// </summary>
    public uint Shade;
    /// <summary>
    /// Indicates whether shade is present and should be written.
    /// </summary>
    public bool HasShade;
    /// <summary>
    /// Stores the luminance modulation value used by this model.
    /// </summary>
    public uint LuminanceModulation;
    /// <summary>
    /// Indicates whether luminance modulation is present and should be written.
    /// </summary>
    public bool HasLuminanceModulation;
    /// <summary>
    /// Stores the luminance offset value used by this model.
    /// </summary>
    public uint LuminanceOffset;
    /// <summary>
    /// Indicates whether luminance offset is present and should be written.
    /// </summary>
    public bool HasLuminanceOffset;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new theme color with its default state.
    /// </summary>
    public XlsxThemeColor()
    {
        Slot = "";
        Kind = "";
        Value = "";
        LastColor = "";
        Tint = 0;
        HasTint = false;
        Shade = 0;
        HasShade = false;
        LuminanceModulation = 0;
        HasLuminanceModulation = false;
        LuminanceOffset = 0;
        HasLuminanceOffset = false;
        RawXml = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
