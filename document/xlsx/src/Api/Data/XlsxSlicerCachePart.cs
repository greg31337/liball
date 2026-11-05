using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML slicer cache part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxSlicerCachePart
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
    /// Stores preserved XML for a construct that is not fully represented by typed fields.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Stores the name value used by this model.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the source name value used by this model.
    /// </summary>
    public string SourceName;
    /// <summary>
    /// Indicates whether table slicer cache is present and should be written.
    /// </summary>
    public bool HasTableSlicerCache;
    /// <summary>
    /// Stores the table id value used by this model.
    /// </summary>
    public uint TableId;
    /// <summary>
    /// Indicates whether table id is present and should be written.
    /// </summary>
    public bool HasTableId;
    /// <summary>
    /// Stores the table column name value used by this model.
    /// </summary>
    public string TableColumnName;
    /// <summary>
    /// Contains pivot tables.
    /// </summary>
    public List<XlsxSlicerPivotTableReference> PivotTables;
    /// <summary>Contains typed slicer item selections.</summary>
    public List<XlsxSlicerSelectionItem> Selections;
    /// <summary>Stores the slicer cross-filter token.</summary>
    public string CrossFilter;
    /// <summary>Stores the slicer sort-order token.</summary>
    public string SortOrder;
    /// <summary>Stores preserved extension-list XML.</summary>
    public string ExtensionListRawXml;
    /// <summary>Contains unknown root attributes.</summary>
    public List<XlsxXmlAttribute> UnknownRootAttributes;
    /// <summary>Contains preserved child XML not represented by typed fields.</summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new slicer cache part with its default state.
    /// </summary>
    public XlsxSlicerCachePart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = "/xl/slicerCaches/slicerCache1.xml";
        RawXml = "";
        Name = "";
        SourceName = "";
        HasTableSlicerCache = false;
        TableId = 0;
        HasTableId = false;
        TableColumnName = "";
        PivotTables = new List<XlsxSlicerPivotTableReference>();
        Selections = new List<XlsxSlicerSelectionItem>();
        CrossFilter = "";
        SortOrder = "";
        ExtensionListRawXml = "";
        UnknownRootAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
