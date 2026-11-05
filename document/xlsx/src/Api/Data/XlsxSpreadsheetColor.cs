using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents spreadsheet color data used by the public workbook API.
/// </summary>
public class XlsxSpreadsheetColor
{
    /// <summary>
    /// Stores the rgb value used by this model.
    /// </summary>
    public string Rgb;
    /// <summary>
    /// Indicates whether rgb is present and should be written.
    /// </summary>
    public bool HasRgb;
    /// <summary>
    /// Indicates whether auto.
    /// </summary>
    public bool IsAuto;
    /// <summary>
    /// Indicates whether auto is present and should be written.
    /// </summary>
    public bool HasAuto;
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
    /// Initializes a new spreadsheet color with its default state.
    /// </summary>
    public XlsxSpreadsheetColor()
    {
        Rgb = "";
        HasRgb = false;
        IsAuto = false;
        HasAuto = false;
        Indexed = 0;
        HasIndexed = false;
        Theme = 0;
        HasTheme = false;
        Tint = 0;
        HasTint = false;
    }
}
