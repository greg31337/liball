using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents styles table styles data used by the public workbook API.
/// </summary>
public class XlsxStylesTableStyles
{
    /// <summary>
    /// Stores the number of  items processed by the operation.
    /// </summary>
    public uint Count;
    /// <summary>
    /// Indicates whether count is present and should be written.
    /// </summary>
    public bool HasCount;
    /// <summary>
    /// Stores the default table style value used by this model.
    /// </summary>
    public string? DefaultTableStyle;
    /// <summary>
    /// Stores the default pivot style value used by this model.
    /// </summary>
    public string? DefaultPivotStyle;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>Contains typed custom table and pivot styles.</summary>
    public List<XlsxTableStyleDefinition> Styles;
    /// <summary>Contains typed slicer styles.</summary>
    public List<XlsxTableStyleDefinition> SlicerStyles;
    /// <summary>
    /// Initializes a new styles table styles with its default state.
    /// </summary>
    public XlsxStylesTableStyles()
    {
        Count = 0;
        HasCount = false;
        DefaultTableStyle = "";
        DefaultPivotStyle = "";
        RawXml = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
        Styles = new List<XlsxTableStyleDefinition>();
        SlicerStyles = new List<XlsxTableStyleDefinition>();
    }
}
