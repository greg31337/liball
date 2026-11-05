using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a formula reference update operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxFormulaReferenceUpdateResult
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
    /// Stores the original formula value used by this model.
    /// </summary>
    public string OriginalFormula;
    /// <summary>
    /// Stores the formula value used by this model.
    /// </summary>
    public string Formula;
    /// <summary>
    /// Stores the number of updated reference items processed by the operation.
    /// </summary>
    public int UpdatedReferenceCount;
    /// <summary>
    /// Stores the number of deleted reference items processed by the operation.
    /// </summary>
    public int DeletedReferenceCount;
    /// <summary>
    /// Stores the number of renamed sheet items processed by the operation.
    /// </summary>
    public int RenamedSheetCount;
    /// <summary>
    /// Initializes a new formula reference update result with its default state.
    /// </summary>
    public XlsxFormulaReferenceUpdateResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        OriginalFormula = "";
        Formula = "";
        UpdatedReferenceCount = 0;
        DeletedReferenceCount = 0;
        RenamedSheetCount = 0;
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
