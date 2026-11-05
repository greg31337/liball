using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a pivot table create operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxPivotTableCreateResult
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
    /// Stores the pivot table definition value used by this model.
    /// </summary>
    public XlsxPivotTableDefinitionPart PivotTableDefinition;
    /// <summary>
    /// Indicates whether pivot table definition is present and should be written.
    /// </summary>
    public bool HasPivotTableDefinition;
    /// <summary>
    /// Stores the pivot cache definition value used by this model.
    /// </summary>
    public XlsxPivotCacheDefinitionPart PivotCacheDefinition;
    /// <summary>
    /// Indicates whether pivot cache definition is present and should be written.
    /// </summary>
    public bool HasPivotCacheDefinition;
    /// <summary>
    /// Stores the pivot cache records xml value used by this model.
    /// </summary>
    public string PivotCacheRecordsXml;
    /// <summary>
    /// Stores the pivot cache records path value used by this model.
    /// </summary>
    public string PivotCacheRecordsPath;
    /// <summary>
    /// Contains pivot table relationships.
    /// </summary>
    public XlsxRelationshipsPart PivotTableRelationships;
    /// <summary>
    /// Indicates whether pivot table relationships is present and should be written.
    /// </summary>
    public bool HasPivotTableRelationships;
    /// <summary>
    /// Contains pivot cache relationships.
    /// </summary>
    public XlsxRelationshipsPart PivotCacheRelationships;
    /// <summary>
    /// Indicates whether pivot cache relationships is present and should be written.
    /// </summary>
    public bool HasPivotCacheRelationships;
    /// <summary>
    /// Initializes a new pivot table create result with its default state.
    /// </summary>
    public XlsxPivotTableCreateResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PivotTableDefinition = new XlsxPivotTableDefinitionPart();
        HasPivotTableDefinition = false;
        PivotCacheDefinition = new XlsxPivotCacheDefinitionPart();
        HasPivotCacheDefinition = false;
        PivotCacheRecordsXml = "";
        PivotCacheRecordsPath = "";
        PivotTableRelationships = new XlsxRelationshipsPart();
        HasPivotTableRelationships = false;
        PivotCacheRelationships = new XlsxRelationshipsPart();
        HasPivotCacheRelationships = false;
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    internal XlsxDiagnostic AddError(string code, string message)
    {
        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, message);
        AddDiagnostic(diagnostic);
        return diagnostic;
    }
}
