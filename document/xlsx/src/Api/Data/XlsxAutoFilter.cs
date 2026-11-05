using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents auto filter data used by the public workbook API.
/// </summary>
public class XlsxAutoFilter
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
    /// Stores the namespace uri value used by this model.
    /// </summary>
    public string NamespaceUri;
    /// <summary>
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string Reference;
    /// <summary>
    /// Contains filter columns.
    /// </summary>
    public List<XlsxAutoFilterColumn> FilterColumns;
    /// <summary>
    /// Stores the sort state value used by this model.
    /// </summary>
    public XlsxSortState SortState;
    /// <summary>
    /// Indicates whether sort state is present and should be written.
    /// </summary>
    public bool HasSortState;
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
    /// Initializes a new auto filter with its default state.
    /// </summary>
    public XlsxAutoFilter()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        NamespaceUri = XlsxNamespaces.Spreadsheet;
        RawXml = "";
        Reference = "";
        FilterColumns = new List<XlsxAutoFilterColumn>();
        SortState = new XlsxSortState();
        HasSortState = false;
        ExtensionListRawXml = "";
        UnknownAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
