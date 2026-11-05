using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents worksheet column data used by the public workbook API.
/// </summary>
public class XlsxWorksheetColumn
{
    /// <summary>
    /// Stores the min value used by this model.
    /// </summary>
    public uint Min;
    /// <summary>
    /// Stores the max value used by this model.
    /// </summary>
    public uint Max;
    /// <summary>
    /// Stores the width value used by this model.
    /// </summary>
    public double Width;
    /// <summary>
    /// Indicates whether width is present and should be written.
    /// </summary>
    public bool HasWidth;
    /// <summary>
    /// Stores the style value used by this model.
    /// </summary>
    public uint Style;
    /// <summary>
    /// Indicates whether style is present and should be written.
    /// </summary>
    public bool HasStyle;
    /// <summary>
    /// Stores the hidden value used by this model.
    /// </summary>
    public bool Hidden;
    /// <summary>
    /// Indicates whether hidden is present and should be written.
    /// </summary>
    public bool HasHidden;
    /// <summary>
    /// Stores the best fit value used by this model.
    /// </summary>
    public bool BestFit;
    /// <summary>
    /// Indicates whether best fit is present and should be written.
    /// </summary>
    public bool HasBestFit;
    /// <summary>
    /// Stores the custom width value used by this model.
    /// </summary>
    public bool CustomWidth;
    /// <summary>
    /// Indicates whether custom width is present and should be written.
    /// </summary>
    public bool HasCustomWidth;
    /// <summary>
    /// Stores the outline level value used by this model.
    /// </summary>
    public uint OutlineLevel;
    /// <summary>
    /// Indicates whether outline level is present and should be written.
    /// </summary>
    public bool HasOutlineLevel;
    /// <summary>
    /// Stores the collapsed value used by this model.
    /// </summary>
    public bool Collapsed;
    /// <summary>
    /// Indicates whether collapsed is present and should be written.
    /// </summary>
    public bool HasCollapsed;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new worksheet column with its default state.
    /// </summary>
    public XlsxWorksheetColumn()
    {
        Min = 0;
        Max = 0;
        Width = 0;
        HasWidth = false;
        Style = 0;
        HasStyle = false;
        Hidden = false;
        HasHidden = false;
        BestFit = false;
        HasBestFit = false;
        CustomWidth = false;
        HasCustomWidth = false;
        OutlineLevel = 0;
        HasOutlineLevel = false;
        Collapsed = false;
        HasCollapsed = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
