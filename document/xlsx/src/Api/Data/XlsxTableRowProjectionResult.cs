using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Contains typed row arrays projected from a worksheet table.
/// </summary>
public class XlsxTableRowProjectionResult
{
    /// <summary>
    /// Indicates whether projection completed without errors.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics produced by the projection.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Stores the projected table name.
    /// </summary>
    public string TableName;
    /// <summary>
    /// Stores the projected table reference.
    /// </summary>
    public string Reference;
    /// <summary>
    /// Stores the number of columns in each projected row.
    /// </summary>
    public int ColumnCount;
    /// <summary>
    /// Contains typed table rows in worksheet order.
    /// </summary>
    public List<XlsxCellValue[]> Rows;

    /// <summary>
    /// Initializes an empty table row projection result.
    /// </summary>
    public XlsxTableRowProjectionResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        TableName = "";
        Reference = "";
        ColumnCount = 0;
        Rows = new List<XlsxCellValue[]>();
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
