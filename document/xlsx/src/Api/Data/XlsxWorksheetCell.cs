using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents worksheet cell data used by the public workbook API.
/// </summary>
public class XlsxWorksheetCell
{
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string? Reference;
    /// <summary>
    /// Stores the type value used by this model.
    /// </summary>
    public string? Type;
    /// <summary>
    /// Stores the style value used by this model.
    /// </summary>
    public uint Style;
    /// <summary>
    /// Indicates whether style is present and should be written.
    /// </summary>
    public bool HasStyle;
    /// <summary>
    /// Stores the value value used by this model.
    /// </summary>
    public string Value;
    /// <summary>
    /// Stores the formula value used by this model.
    /// </summary>
    public string Formula;
    /// <summary>
    /// Stores the formula info value used by this model.
    /// </summary>
    public XlsxCellFormula FormulaInfo;
    /// <summary>
    /// Indicates whether formula is present and should be written.
    /// </summary>
    public bool HasFormula;
    /// <summary>
    /// Stores the inline string text value used by this model.
    /// </summary>
    public string InlineStringText;
    /// <summary>
    /// Stores the inline string value used by this model.
    /// </summary>
    public XlsxInlineString InlineString;
    /// <summary>
    /// Indicates whether inline string is present and should be written.
    /// </summary>
    public bool HasInlineString;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new worksheet cell with its default state.
    /// </summary>
    public XlsxWorksheetCell()
    {
        Reference = "";
        Type = "";
        Style = 0;
        HasStyle = false;
        Value = "";
        Formula = "";
        FormulaInfo = new XlsxCellFormula();
        HasFormula = false;
        InlineStringText = "";
        InlineString = new XlsxInlineString();
        HasInlineString = false;
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
