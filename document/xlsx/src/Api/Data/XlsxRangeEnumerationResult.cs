using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Contains ordered cells, rows, and typed values produced by range enumeration.
/// </summary>
public class XlsxRangeEnumerationResult
{
    /// <summary>
    /// Indicates whether enumeration completed without errors.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains enumeration diagnostics.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Stores the enumerated A1 range reference.
    /// </summary>
    public string Reference;
    /// <summary>
    /// Contains cells in row-major order.
    /// </summary>
    public List<XlsxRangeCellItem> Cells;
    /// <summary>
    /// Contains rows in ascending row order.
    /// </summary>
    public List<XlsxRangeRowItem> Rows;
    /// <summary>
    /// Contains typed values in row-major order.
    /// </summary>
    public List<XlsxCellValue> Values;

    /// <summary>
    /// Initializes an empty range enumeration result.
    /// </summary>
    public XlsxRangeEnumerationResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Reference = "";
        Cells = new List<XlsxRangeCellItem>();
        Rows = new List<XlsxRangeRowItem>();
        Values = new List<XlsxCellValue>();
    }

    internal XlsxDiagnostic AddError(string code, string message)
    {
        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, message);
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
        return diagnostic;
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
