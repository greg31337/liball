using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML slicer timeline part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxSlicerTimelinePart
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
    /// Stores the root element name value used by this model.
    /// </summary>
    public string RootElementName;
    /// <summary>Stores the namespace used by the part.</summary>
    public string NamespaceUri;
    /// <summary>
    /// Contains items.
    /// </summary>
    public List<XlsxSlicerTimelineItem> Items;
    /// <summary>Contains bound pivot tables for a timeline cache.</summary>
    public List<XlsxSlicerPivotTableReference> PivotTables;
    /// <summary>Stores timeline selection and filter state.</summary>
    public XlsxTimelineState State;
    /// <summary>Indicates whether timeline state is present.</summary>
    public bool HasState;
    /// <summary>Stores preserved extension-list XML.</summary>
    public string ExtensionListRawXml;
    /// <summary>Contains unknown root attributes.</summary>
    public List<XlsxXmlAttribute> UnknownRootAttributes;
    /// <summary>Contains preserved child XML not represented by typed fields.</summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new slicer timeline part with its default state.
    /// </summary>
    public XlsxSlicerTimelinePart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = "";
        RawXml = "";
        Name = "";
        SourceName = "";
        RootElementName = "";
        NamespaceUri = XlsxNamespaces.Office2013Spreadsheet;
        Items = new List<XlsxSlicerTimelineItem>();
        PivotTables = new List<XlsxSlicerPivotTableReference>();
        State = new XlsxTimelineState();
        HasState = false;
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
