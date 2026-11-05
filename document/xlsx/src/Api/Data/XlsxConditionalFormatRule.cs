using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents conditional format rule data used by the public workbook API.
/// </summary>
public class XlsxConditionalFormatRule
{
    /// <summary>
    /// Stores the type value used by this model.
    /// </summary>
    public string Type;
    /// <summary>
    /// Stores the priority value used by this model.
    /// </summary>
    public uint Priority;
    /// <summary>
    /// Indicates whether priority is present and should be written.
    /// </summary>
    public bool HasPriority;
    /// <summary>
    /// Stores the dxf id value used by this model.
    /// </summary>
    public uint DxfId;
    /// <summary>
    /// Indicates whether dxf id is present and should be written.
    /// </summary>
    public bool HasDxfId;
    /// <summary>
    /// Stores the stop if true value used by this model.
    /// </summary>
    public bool StopIfTrue;
    /// <summary>
    /// Indicates whether stop if true is present and should be written.
    /// </summary>
    public bool HasStopIfTrue;
    /// <summary>
    /// Stores the above average value used by this model.
    /// </summary>
    public bool AboveAverage;
    /// <summary>
    /// Indicates whether above average is present and should be written.
    /// </summary>
    public bool HasAboveAverage;
    /// <summary>
    /// Stores the percent value used by this model.
    /// </summary>
    public bool Percent;
    /// <summary>
    /// Indicates whether percent is present and should be written.
    /// </summary>
    public bool HasPercent;
    /// <summary>
    /// Stores the bottom value used by this model.
    /// </summary>
    public bool Bottom;
    /// <summary>
    /// Indicates whether bottom is present and should be written.
    /// </summary>
    public bool HasBottom;
    /// <summary>
    /// Stores the equal average value used by this model.
    /// </summary>
    public bool EqualAverage;
    /// <summary>
    /// Indicates whether equal average is present and should be written.
    /// </summary>
    public bool HasEqualAverage;
    /// <summary>
    /// Stores the operator value used by this model.
    /// </summary>
    public string Operator;
    /// <summary>
    /// Stores the text value used by this model.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores the time period value used by this model.
    /// </summary>
    public string TimePeriod;
    /// <summary>
    /// Stores the rank value used by this model.
    /// </summary>
    public uint Rank;
    /// <summary>
    /// Indicates whether rank is present and should be written.
    /// </summary>
    public bool HasRank;
    /// <summary>
    /// Stores the std dev value used by this model.
    /// </summary>
    public uint StdDev;
    /// <summary>
    /// Indicates whether std dev is present and should be written.
    /// </summary>
    public bool HasStdDev;
    /// <summary>
    /// Stores the id value used by this model.
    /// </summary>
    public string Id;
    /// <summary>
    /// Contains formulas.
    /// </summary>
    public List<string> Formulas;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public List<string> FormulaRawXml;
    /// <summary>
    /// Stores the color scale value used by this model.
    /// </summary>
    public XlsxConditionalFormatColorScale ColorScale;
    /// <summary>
    /// Indicates whether color scale is present and should be written.
    /// </summary>
    public bool HasColorScale;
    /// <summary>
    /// Stores the data bar value used by this model.
    /// </summary>
    public XlsxConditionalFormatDataBar DataBar;
    /// <summary>
    /// Indicates whether data bar is present and should be written.
    /// </summary>
    public bool HasDataBar;
    /// <summary>
    /// Stores the icon set value used by this model.
    /// </summary>
    public XlsxConditionalFormatIconSet IconSet;
    /// <summary>
    /// Indicates whether icon set is present and should be written.
    /// </summary>
    public bool HasIconSet;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string DifferentialStyleRawXml;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string ExtensionListRawXml;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new conditional format rule with its default state.
    /// </summary>
    public XlsxConditionalFormatRule()
    {
        Type = "";
        Priority = 0;
        HasPriority = false;
        DxfId = 0;
        HasDxfId = false;
        StopIfTrue = false;
        HasStopIfTrue = false;
        AboveAverage = false;
        HasAboveAverage = false;
        Percent = false;
        HasPercent = false;
        Bottom = false;
        HasBottom = false;
        EqualAverage = false;
        HasEqualAverage = false;
        Operator = "";
        Text = "";
        TimePeriod = "";
        Rank = 0;
        HasRank = false;
        StdDev = 0;
        HasStdDev = false;
        Id = "";
        Formulas = new List<string>();
        FormulaRawXml = new List<string>();
        ColorScale = new XlsxConditionalFormatColorScale();
        HasColorScale = false;
        DataBar = new XlsxConditionalFormatDataBar();
        HasDataBar = false;
        IconSet = new XlsxConditionalFormatIconSet();
        HasIconSet = false;
        DifferentialStyleRawXml = "";
        ExtensionListRawXml = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
