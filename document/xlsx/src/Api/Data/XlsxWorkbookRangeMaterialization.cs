using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Contains a rectangular workbook range materialized as typed row arrays.
/// </summary>
public class XlsxWorkbookRangeMaterialization
{
    /// <summary>Indicates whether materialization completed without errors.</summary>
    public bool IsValid;
    /// <summary>Contains materialization diagnostics.</summary>
    public XlsxValidationResult Validation;
    /// <summary>Stores the worksheet name.</summary>
    public string SheetName;
    /// <summary>Stores the normalized A1 range.</summary>
    public string Reference;
    /// <summary>Stores the number of columns in every row.</summary>
    public int ColumnCount;
    /// <summary>Contains typed row arrays in worksheet order, including blank coordinates.</summary>
    public List<XlsxCellValue[]> Rows;

    /// <summary>Initializes an empty successful materialization.</summary>
    public XlsxWorkbookRangeMaterialization()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        SheetName = "";
        Reference = "";
        ColumnCount = 0;
        Rows = new List<XlsxCellValue[]>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    internal void AddError(string code, string message)
    {
        AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, message));
    }
}
