using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents table column data used by the public workbook API.
/// </summary>
public class XlsxTableColumn
{
    /// <summary>
    /// Stores the id value used by this model.
    /// </summary>
    public uint Id;
    /// <summary>
    /// Indicates whether id is present and should be written.
    /// </summary>
    public bool HasId;
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the unique name value used by this model.
    /// </summary>
    public string UniqueName;
    /// <summary>
    /// Stores the totals row function value used by this model.
    /// </summary>
    public string TotalsRowFunction;
    /// <summary>
    /// Stores the typed totals row function.
    /// </summary>
    public XlsxTableTotalFunction TotalFunction;
    /// <summary>
    /// Indicates whether a typed totals row function is present.
    /// </summary>
    public bool HasTotalFunction;
    /// <summary>
    /// Stores the totals row label value used by this model.
    /// </summary>
    public string TotalsRowLabel;
    /// <summary>
    /// Stores the query table field id value used by this model.
    /// </summary>
    public uint QueryTableFieldId;
    /// <summary>
    /// Indicates whether query table field id is present and should be written.
    /// </summary>
    public bool HasQueryTableFieldId;
    /// <summary>
    /// Stores the header row dxf id value used by this model.
    /// </summary>
    public uint HeaderRowDxfId;
    /// <summary>
    /// Indicates whether header row dxf id is present and should be written.
    /// </summary>
    public bool HasHeaderRowDxfId;
    /// <summary>
    /// Stores the data dxf id value used by this model.
    /// </summary>
    public uint DataDxfId;
    /// <summary>
    /// Indicates whether data dxf id is present and should be written.
    /// </summary>
    public bool HasDataDxfId;
    /// <summary>
    /// Stores the totals row dxf id value used by this model.
    /// </summary>
    public uint TotalsRowDxfId;
    /// <summary>
    /// Indicates whether totals row dxf id is present and should be written.
    /// </summary>
    public bool HasTotalsRowDxfId;
    /// <summary>
    /// Stores the header row cell style value used by this model.
    /// </summary>
    public string HeaderRowCellStyle;
    /// <summary>
    /// Stores the data cell style value used by this model.
    /// </summary>
    public string DataCellStyle;
    /// <summary>
    /// Stores the totals row cell style value used by this model.
    /// </summary>
    public string TotalsRowCellStyle;
    /// <summary>
    /// Stores the calculated column formula value used by this model.
    /// </summary>
    public string CalculatedColumnFormula;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string CalculatedColumnFormulaRawXml;
    /// <summary>
    /// Stores the totals row formula value used by this model.
    /// </summary>
    public string TotalsRowFormula;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string TotalsRowFormulaRawXml;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string XmlColumnPropertiesRawXml;
    /// <summary>
    /// Stores the typed XML column mapping.
    /// </summary>
    public XlsxTableXmlColumnProperties XmlColumnProperties;
    /// <summary>
    /// Indicates whether typed XML column mapping properties are present.
    /// </summary>
    public bool HasXmlColumnProperties;
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
    /// Initializes a new table column with its default state.
    /// </summary>
    public XlsxTableColumn()
    {
        Id = 0;
        HasId = false;
        Name = "";
        UniqueName = "";
        TotalsRowFunction = "";
        TotalFunction = XlsxTableTotalFunction.None;
        HasTotalFunction = false;
        TotalsRowLabel = "";
        QueryTableFieldId = 0;
        HasQueryTableFieldId = false;
        HeaderRowDxfId = 0;
        HasHeaderRowDxfId = false;
        DataDxfId = 0;
        HasDataDxfId = false;
        TotalsRowDxfId = 0;
        HasTotalsRowDxfId = false;
        HeaderRowCellStyle = "";
        DataCellStyle = "";
        TotalsRowCellStyle = "";
        CalculatedColumnFormula = "";
        CalculatedColumnFormulaRawXml = "";
        TotalsRowFormula = "";
        TotalsRowFormulaRawXml = "";
        XmlColumnPropertiesRawXml = "";
        XmlColumnProperties = new XlsxTableXmlColumnProperties();
        HasXmlColumnProperties = false;
        ExtensionListRawXml = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }
}
