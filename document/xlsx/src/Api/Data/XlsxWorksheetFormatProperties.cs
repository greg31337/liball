using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents worksheet format properties data used by the public workbook API.
/// </summary>
public class XlsxWorksheetFormatProperties
{
    /// <summary>
    /// Stores the base column width value used by this model.
    /// </summary>
    public uint BaseColumnWidth;
    /// <summary>
    /// Indicates whether base column width is present and should be written.
    /// </summary>
    public bool HasBaseColumnWidth;
    /// <summary>
    /// Stores the default column width value used by this model.
    /// </summary>
    public double DefaultColumnWidth;
    /// <summary>
    /// Indicates whether default column width is present and should be written.
    /// </summary>
    public bool HasDefaultColumnWidth;
    /// <summary>
    /// Stores the default row height value used by this model.
    /// </summary>
    public double DefaultRowHeight;
    /// <summary>
    /// Indicates whether default row height is present and should be written.
    /// </summary>
    public bool HasDefaultRowHeight;
    /// <summary>
    /// Stores the custom height value used by this model.
    /// </summary>
    public bool CustomHeight;
    /// <summary>
    /// Indicates whether custom height is present and should be written.
    /// </summary>
    public bool HasCustomHeight;
    /// <summary>
    /// Stores the zero height value used by this model.
    /// </summary>
    public bool ZeroHeight;
    /// <summary>
    /// Indicates whether zero height is present and should be written.
    /// </summary>
    public bool HasZeroHeight;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Initializes a new worksheet format properties with its default state.
    /// </summary>
    public XlsxWorksheetFormatProperties()
    {
        BaseColumnWidth = 0;
        HasBaseColumnWidth = false;
        DefaultColumnWidth = 0;
        HasDefaultColumnWidth = false;
        DefaultRowHeight = 0;
        HasDefaultRowHeight = false;
        CustomHeight = false;
        HasCustomHeight = false;
        ZeroHeight = false;
        HasZeroHeight = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
    }
}
