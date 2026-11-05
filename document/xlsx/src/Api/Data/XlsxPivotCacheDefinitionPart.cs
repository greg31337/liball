using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML pivot cache definition part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxPivotCacheDefinitionPart
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
    /// Stores the number of record items processed by the operation.
    /// </summary>
    public uint RecordCount;
    /// <summary>
    /// Indicates whether record count is present and should be written.
    /// </summary>
    public bool HasRecordCount;
    /// <summary>
    /// Stores the refreshed by value used by this model.
    /// </summary>
    public string RefreshedBy;
    /// <summary>
    /// Stores typed cache source information.
    /// </summary>
    public XlsxPivotCacheSource Source;
    /// <summary>
    /// Indicates whether typed cache source information is present.
    /// </summary>
    public bool HasSource;
    /// <summary>
    /// Contains typed cache fields.
    /// </summary>
    public List<XlsxPivotCacheField> Fields;
    /// <summary>
    /// Contains typed calculated cache items.
    /// </summary>
    public List<XlsxPivotCacheCalculatedItem> CalculatedItems;
    /// <summary>
    /// Stores typed refresh and version settings.
    /// </summary>
    public XlsxPivotCacheRefreshSettings RefreshSettings;
    /// <summary>
    /// Stores the pivot-cache records relationship identifier.
    /// </summary>
    public string RecordsRelationshipId;
    /// <summary>
    /// Stores the resolved pivot-cache records relationship.
    /// </summary>
    public XlsxRelationship? RecordsRelationship;
    /// <summary>
    /// Indicates whether the records relationship is resolved.
    /// </summary>
    public bool HasRecordsRelationship;
    /// <summary>
    /// Stores the resolved pivot-cache records target path.
    /// </summary>
    public string RecordsTargetPath;
    /// <summary>
    /// Stores preserved extension-list XML.
    /// </summary>
    public string ExtensionListRawXml;
    /// <summary>
    /// Contains preserved child XML not represented by typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Contains unknown root attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownRootAttributes;
    /// <summary>
    /// Initializes a new pivot cache definition part with its default state.
    /// </summary>
    public XlsxPivotCacheDefinitionPart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = "/xl/pivotCache/pivotCacheDefinition1.xml";
        RawXml = "";
        RecordCount = 0;
        HasRecordCount = false;
        RefreshedBy = "";
        Source = new XlsxPivotCacheSource();
        HasSource = false;
        Fields = new List<XlsxPivotCacheField>();
        CalculatedItems = new List<XlsxPivotCacheCalculatedItem>();
        RefreshSettings = new XlsxPivotCacheRefreshSettings();
        RecordsRelationshipId = "";
        RecordsRelationship = null;
        HasRecordsRelationship = false;
        RecordsTargetPath = "";
        ExtensionListRawXml = "";
        UnknownXml = new List<string>();
        UnknownRootAttributes = new List<XlsxXmlAttribute>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
