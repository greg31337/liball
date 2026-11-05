using System;

namespace xlsx;

/// <summary>
/// Contains used, data, first-cell, and last-cell range queries for a worksheet.
/// </summary>
public class XlsxRangeQueryResult
{
    /// <summary>
    /// Indicates whether the query completed without errors.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains range-query diagnostics.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Indicates whether the worksheet has a used range.
    /// </summary>
    public bool HasUsedRange;
    /// <summary>
    /// Stores the used range including styled or preserved cells.
    /// </summary>
    public string UsedRange;
    /// <summary>
    /// Indicates whether the worksheet has a data range.
    /// </summary>
    public bool HasDataRange;
    /// <summary>
    /// Stores the range containing cells with values, formulas, or inline text.
    /// </summary>
    public string DataRange;
    /// <summary>
    /// Stores the first used cell reference in row-major order.
    /// </summary>
    public string FirstUsedCell;
    /// <summary>
    /// Stores the last used cell reference in row-major order.
    /// </summary>
    public string LastUsedCell;
    /// <summary>
    /// Stores the first data cell reference in row-major order.
    /// </summary>
    public string FirstDataCell;
    /// <summary>
    /// Stores the last data cell reference in row-major order.
    /// </summary>
    public string LastDataCell;

    /// <summary>
    /// Initializes an empty range query result.
    /// </summary>
    public XlsxRangeQueryResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        HasUsedRange = false;
        UsedRange = "";
        HasDataRange = false;
        DataRange = "";
        FirstUsedCell = "";
        LastUsedCell = "";
        FirstDataCell = "";
        LastDataCell = "";
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
