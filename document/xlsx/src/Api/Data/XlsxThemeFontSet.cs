using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents theme font set data used by the public workbook API.
/// </summary>
public class XlsxThemeFontSet
{
    /// <summary>
    /// Stores the latin typeface value used by this model.
    /// </summary>
    public string? LatinTypeface;
    /// <summary>
    /// Stores the east asian typeface value used by this model.
    /// </summary>
    public string? EastAsianTypeface;
    /// <summary>
    /// Stores the complex script typeface value used by this model.
    /// </summary>
    public string? ComplexScriptTypeface;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new theme font set with its default state.
    /// </summary>
    public XlsxThemeFontSet()
    {
        LatinTypeface = "";
        EastAsianTypeface = "";
        ComplexScriptTypeface = "";
        RawXml = "";
        UnknownXml = new List<string>();
    }
}
