using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Reports worksheet features that overlap a candidate range.
/// </summary>
public class XlsxRangeOverlapResult
{
    /// <summary>
    /// Indicates whether overlap validation completed without malformed input.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains overlap validation diagnostics.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Indicates whether any tracked feature overlaps the candidate range.
    /// </summary>
    public bool HasOverlap;
    /// <summary>
    /// Contains overlapping merged-cell references.
    /// </summary>
    public List<string> MergeReferences;
    /// <summary>
    /// Contains overlapping table references.
    /// </summary>
    public List<string> TableReferences;
    /// <summary>
    /// Contains overlapping data-validation references.
    /// </summary>
    public List<string> ValidationReferences;
    /// <summary>
    /// Contains overlapping filter references.
    /// </summary>
    public List<string> FilterReferences;
    /// <summary>
    /// Contains overlapping pivot-table location references.
    /// </summary>
    public List<string> PivotReferences;

    /// <summary>
    /// Initializes an empty range overlap result.
    /// </summary>
    public XlsxRangeOverlapResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        HasOverlap = false;
        MergeReferences = new List<string>();
        TableReferences = new List<string>();
        ValidationReferences = new List<string>();
        FilterReferences = new List<string>();
        PivotReferences = new List<string>();
    }

    internal XlsxDiagnostic AddError(string code, string message)
    {
        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, message);
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
        return diagnostic;
    }
}
