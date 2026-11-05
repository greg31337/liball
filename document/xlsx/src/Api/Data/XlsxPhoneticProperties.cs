using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents typed phonetic display properties for rich worksheet text.
/// </summary>
public class XlsxPhoneticProperties
{
    /// <summary>Stores the font identifier.</summary>
    public uint FontId;
    /// <summary>Indicates whether the font identifier is present.</summary>
    public bool HasFontId;
    /// <summary>Stores the phonetic text type.</summary>
    public string Type;
    /// <summary>Stores the phonetic alignment.</summary>
    public string Alignment;
    /// <summary>Contains unknown attributes.</summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>Initializes phonetic properties with their default state.</summary>
    public XlsxPhoneticProperties()
    {
        FontId = 0;
        HasFontId = false;
        Type = "";
        Alignment = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
