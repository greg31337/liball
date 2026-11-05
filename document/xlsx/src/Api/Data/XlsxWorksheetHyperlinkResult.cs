using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a worksheet hyperlink operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxWorksheetHyperlinkResult
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
    /// Contains relationships.
    /// </summary>
    public XlsxRelationshipsPart Relationships;
    /// <summary>
    /// Indicates whether relationships is present and should be written.
    /// </summary>
    public bool HasRelationships;
    /// <summary>
    /// Stores the hyperlink value used by this model.
    /// </summary>
    public XlsxWorksheetHyperlink? Hyperlink;
    /// <summary>
    /// Indicates whether hyperlink is present and should be written.
    /// </summary>
    public bool HasHyperlink;
    /// <summary>
    /// Initializes a new worksheet hyperlink result with its default state.
    /// </summary>
    public XlsxWorksheetHyperlinkResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Relationships = new XlsxRelationshipsPart();
        HasRelationships = false;
        Hyperlink = null;
        HasHyperlink = false;
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
