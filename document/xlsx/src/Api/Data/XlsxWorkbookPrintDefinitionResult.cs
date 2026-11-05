using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a workbook print definition operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxWorkbookPrintDefinitionResult
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
    /// Stores the defined name value used by this model.
    /// </summary>
    public XlsxWorkbookDefinedName? DefinedName;
    /// <summary>
    /// Indicates whether defined name is present and should be written.
    /// </summary>
    public bool HasDefinedName;
    /// <summary>
    /// Stores the created defined name value used by this model.
    /// </summary>
    public bool CreatedDefinedName;
    /// <summary>
    /// Stores the removed defined name value used by this model.
    /// </summary>
    public bool RemovedDefinedName;
    /// <summary>
    /// Initializes a new workbook print definition result with its default state.
    /// </summary>
    public XlsxWorkbookPrintDefinitionResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        DefinedName = null;
        HasDefinedName = false;
        CreatedDefinedName = false;
        RemovedDefinedName = false;
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
