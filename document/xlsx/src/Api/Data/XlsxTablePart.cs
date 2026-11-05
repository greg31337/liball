using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML table part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxTablePart
{
    /// <summary>
    /// Indicates whether the operation completed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics produced while the object was read, changed, validated, or written.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Stores the normalized absolute package path, beginning with <c>/</c>.
    /// </summary>
    public string PartPath;
    /// <summary>
    /// Stores the namespace uri value used by this model.
    /// </summary>
    public string NamespaceUri;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
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
    /// Stores the display name value used by this model.
    /// </summary>
    public string DisplayName;
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string Reference;
    /// <summary>
    /// Stores the table type value used by this model.
    /// </summary>
    public string TableType;
    /// <summary>
    /// Stores the comment value used by this model.
    /// </summary>
    public string Comment;
    /// <summary>
    /// Stores the number of header row items processed by the operation.
    /// </summary>
    public uint HeaderRowCount;
    /// <summary>
    /// Indicates whether header row count is present and should be written.
    /// </summary>
    public bool HasHeaderRowCount;
    /// <summary>
    /// Stores the number of totals row items processed by the operation.
    /// </summary>
    public uint TotalsRowCount;
    /// <summary>
    /// Indicates whether totals row count is present and should be written.
    /// </summary>
    public bool HasTotalsRowCount;
    /// <summary>
    /// Stores the totals row shown value used by this model.
    /// </summary>
    public bool TotalsRowShown;
    /// <summary>
    /// Indicates whether totals row shown is present and should be written.
    /// </summary>
    public bool HasTotalsRowShown;
    /// <summary>
    /// Stores the insert row value used by this model.
    /// </summary>
    public bool InsertRow;
    /// <summary>
    /// Indicates whether insert row is present and should be written.
    /// </summary>
    public bool HasInsertRow;
    /// <summary>
    /// Stores the insert row shift value used by this model.
    /// </summary>
    public bool InsertRowShift;
    /// <summary>
    /// Indicates whether insert row shift is present and should be written.
    /// </summary>
    public bool HasInsertRowShift;
    /// <summary>
    /// Stores the published value used by this model.
    /// </summary>
    public bool Published;
    /// <summary>
    /// Indicates whether published is present and should be written.
    /// </summary>
    public bool HasPublished;
    /// <summary>
    /// Stores the connection id value used by this model.
    /// </summary>
    public uint ConnectionId;
    /// <summary>
    /// Indicates whether connection id is present and should be written.
    /// </summary>
    public bool HasConnectionId;
    /// <summary>
    /// Stores the differential style identifier for the header row.
    /// </summary>
    public uint HeaderRowDxfId;
    /// <summary>
    /// Indicates whether the header-row differential style identifier is present.
    /// </summary>
    public bool HasHeaderRowDxfId;
    /// <summary>
    /// Stores the differential style identifier for table data cells.
    /// </summary>
    public uint DataDxfId;
    /// <summary>
    /// Indicates whether the data differential style identifier is present.
    /// </summary>
    public bool HasDataDxfId;
    /// <summary>
    /// Stores the differential style identifier for the totals row.
    /// </summary>
    public uint TotalsRowDxfId;
    /// <summary>
    /// Indicates whether the totals-row differential style identifier is present.
    /// </summary>
    public bool HasTotalsRowDxfId;
    /// <summary>
    /// Stores the differential border style identifier for the header row.
    /// </summary>
    public uint HeaderRowBorderDxfId;
    /// <summary>
    /// Indicates whether the header-row border differential style identifier is present.
    /// </summary>
    public bool HasHeaderRowBorderDxfId;
    /// <summary>
    /// Stores the differential border style identifier for the table.
    /// </summary>
    public uint TableBorderDxfId;
    /// <summary>
    /// Indicates whether the table border differential style identifier is present.
    /// </summary>
    public bool HasTableBorderDxfId;
    /// <summary>
    /// Stores the differential border style identifier for the totals row.
    /// </summary>
    public uint TotalsRowBorderDxfId;
    /// <summary>
    /// Indicates whether the totals-row border differential style identifier is present.
    /// </summary>
    public bool HasTotalsRowBorderDxfId;
    /// <summary>
    /// Stores the named cell style for the header row.
    /// </summary>
    public string HeaderRowCellStyle;
    /// <summary>
    /// Stores the named cell style for table data cells.
    /// </summary>
    public string DataCellStyle;
    /// <summary>
    /// Stores the named cell style for the totals row.
    /// </summary>
    public string TotalsRowCellStyle;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string AutoFilterRawXml;
    /// <summary>
    /// Stores the auto filter enabled value used by this model.
    /// </summary>
    public bool AutoFilterEnabled;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string SortStateRawXml;
    /// <summary>
    /// Stores the sort state enabled value used by this model.
    /// </summary>
    public bool SortStateEnabled;
    /// <summary>
    /// Stores the sort column name value used by this model.
    /// </summary>
    public string SortColumnName;
    /// <summary>
    /// Stores the sort descending value used by this model.
    /// </summary>
    public bool SortDescending;
    /// <summary>
    /// Stores the number of table column items processed by the operation.
    /// </summary>
    public uint TableColumnCount;
    /// <summary>
    /// Indicates whether table column count is present and should be written.
    /// </summary>
    public bool HasTableColumnCount;
    /// <summary>
    /// Contains columns.
    /// </summary>
    public List<XlsxTableColumn> Columns;
    /// <summary>
    /// Stores the style info value used by this model.
    /// </summary>
    public XlsxTableStyleInfo StyleInfo;
    /// <summary>
    /// Indicates whether style info is present and should be written.
    /// </summary>
    public bool HasStyleInfo;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string ExtensionListRawXml;
    /// <summary>
    /// Contains unknown root attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownRootAttributes;
    /// <summary>
    /// Contains table columns unknown attributes.
    /// </summary>
    public List<XlsxXmlAttribute> TableColumnsUnknownAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new table part with its default state.
    /// </summary>
    public XlsxTablePart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = "/xl/tables/table1.xml";
        NamespaceUri = XlsxNamespaces.Spreadsheet;
        RawXml = "";
        Id = 0;
        HasId = false;
        Name = "";
        DisplayName = "";
        Reference = "";
        TableType = "";
        Comment = "";
        HeaderRowCount = 0;
        HasHeaderRowCount = false;
        TotalsRowCount = 0;
        HasTotalsRowCount = false;
        TotalsRowShown = false;
        HasTotalsRowShown = false;
        InsertRow = false;
        HasInsertRow = false;
        InsertRowShift = false;
        HasInsertRowShift = false;
        Published = false;
        HasPublished = false;
        ConnectionId = 0;
        HasConnectionId = false;
        HeaderRowDxfId = 0;
        HasHeaderRowDxfId = false;
        DataDxfId = 0;
        HasDataDxfId = false;
        TotalsRowDxfId = 0;
        HasTotalsRowDxfId = false;
        HeaderRowBorderDxfId = 0;
        HasHeaderRowBorderDxfId = false;
        TableBorderDxfId = 0;
        HasTableBorderDxfId = false;
        TotalsRowBorderDxfId = 0;
        HasTotalsRowBorderDxfId = false;
        HeaderRowCellStyle = "";
        DataCellStyle = "";
        TotalsRowCellStyle = "";
        AutoFilterRawXml = "";
        AutoFilterEnabled = false;
        SortStateRawXml = "";
        SortStateEnabled = false;
        SortColumnName = "";
        SortDescending = false;
        TableColumnCount = 0;
        HasTableColumnCount = false;
        Columns = new List<XlsxTableColumn>();
        StyleInfo = new XlsxTableStyleInfo();
        HasStyleInfo = false;
        ExtensionListRawXml = "";
        UnknownRootAttributes = new List<XlsxXmlAttribute>();
        TableColumnsUnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
