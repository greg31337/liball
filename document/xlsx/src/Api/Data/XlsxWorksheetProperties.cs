using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents worksheet properties data used by the public workbook API.
/// </summary>
public class XlsxWorksheetProperties
{
    /// <summary>
    /// Stores the code name value used by this model.
    /// </summary>
    public string? CodeName;
    /// <summary>
    /// Stores the enable format conditions calculation value used by this model.
    /// </summary>
    public bool EnableFormatConditionsCalculation;
    /// <summary>
    /// Indicates whether enable format conditions calculation is present and should be written.
    /// </summary>
    public bool HasEnableFormatConditionsCalculation;
    /// <summary>
    /// Stores the worksheet tab color.
    /// </summary>
    public XlsxSpreadsheetColor TabColor;
    /// <summary>
    /// Indicates whether tab color is present and should be written.
    /// </summary>
    public bool HasTabColor;
    /// <summary>
    /// Stores worksheet outline properties.
    /// </summary>
    public XlsxWorksheetOutlineProperties OutlineProperties;
    /// <summary>
    /// Indicates whether outline properties is present and should be written.
    /// </summary>
    public bool HasOutlineProperties;
    /// <summary>
    /// Stores worksheet page-setup properties.
    /// </summary>
    public XlsxWorksheetPageSetupProperties PageSetupProperties;
    /// <summary>
    /// Indicates whether page-setup properties is present and should be written.
    /// </summary>
    public bool HasPageSetupProperties;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Contains unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownAttributes;
    /// <summary>
    /// Contains preserved sheet-property child XML not represented by typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new worksheet properties with its default state.
    /// </summary>
    public XlsxWorksheetProperties()
    {
        CodeName = "";
        EnableFormatConditionsCalculation = false;
        HasEnableFormatConditionsCalculation = false;
        TabColor = new XlsxSpreadsheetColor();
        HasTabColor = false;
        OutlineProperties = new XlsxWorksheetOutlineProperties();
        HasOutlineProperties = false;
        PageSetupProperties = new XlsxWorksheetPageSetupProperties();
        HasPageSetupProperties = false;
        RawXml = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
