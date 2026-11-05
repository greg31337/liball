using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a sheet drawing relationship operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxSheetDrawingRelationshipResult
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
    /// Stores the drawing value used by this model.
    /// </summary>
    public XlsxSheetDrawingReference? Drawing;
    /// <summary>
    /// Indicates whether drawing is present and should be written.
    /// </summary>
    public bool HasDrawing;
    /// <summary>
    /// Stores the relationship value used by this model.
    /// </summary>
    public XlsxRelationship? Relationship;
    /// <summary>
    /// Indicates whether relationship is present and should be written.
    /// </summary>
    public bool HasRelationship;
    /// <summary>
    /// Initializes a new sheet drawing relationship result with its default state.
    /// </summary>
    public XlsxSheetDrawingRelationshipResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Drawing = null;
        HasDrawing = false;
        Relationship = null;
        HasRelationship = false;
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
