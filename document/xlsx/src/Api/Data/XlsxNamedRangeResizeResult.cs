using System;

namespace xlsx;

/// <summary>
/// Contains the outcome of resizing a workbook or worksheet-scoped defined range.
/// </summary>
public class XlsxNamedRangeResizeResult
{
    /// <summary>
    /// Indicates whether the range was resized without errors.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains resize diagnostics.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Stores the resized defined name.
    /// </summary>
    public XlsxWorkbookDefinedName? DefinedName;
    /// <summary>
    /// Indicates whether a defined name was found.
    /// </summary>
    public bool HasDefinedName;
    /// <summary>
    /// Stores the previous defined-name reference.
    /// </summary>
    public string PreviousReference;
    /// <summary>
    /// Stores the resized defined-name reference.
    /// </summary>
    public string Reference;

    /// <summary>
    /// Initializes an empty named-range resize result.
    /// </summary>
    public XlsxNamedRangeResizeResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        DefinedName = null;
        HasDefinedName = false;
        PreviousReference = "";
        Reference = "";
    }

    internal XlsxDiagnostic AddError(string code, string message)
    {
        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, message);
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
        return diagnostic;
    }
}
