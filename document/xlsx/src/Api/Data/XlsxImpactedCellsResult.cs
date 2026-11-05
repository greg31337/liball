using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Contains cells transitively affected by one or more changed workbook cells.
/// </summary>
public class XlsxImpactedCellsResult
{
    /// <summary>
    /// Indicates whether impacted-cell analysis completed without errors.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains impacted-cell analysis diagnostics.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Contains the normalized changed cell references used as starting points.
    /// </summary>
    public List<string> StartingReferences;
    /// <summary>
    /// Contains direct dependent cells.
    /// </summary>
    public List<string> DirectDependents;
    /// <summary>
    /// Contains all transitively impacted cells, excluding starting cells.
    /// </summary>
    public List<string> ImpactedCells;

    /// <summary>
    /// Initializes an empty impacted-cell result.
    /// </summary>
    public XlsxImpactedCellsResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        StartingReferences = new List<string>();
        DirectDependents = new List<string>();
        ImpactedCells = new List<string>();
    }

    internal XlsxDiagnostic AddError(string code, string message)
    {
        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, message);
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
        return diagnostic;
    }
}
