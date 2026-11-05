using System;

namespace xlsx;

/// <summary>
/// Reports workbook template replacements applied to cells and formulas.
/// </summary>
public class XlsxTemplateReplacementResult
{
    /// <summary>Indicates whether replacement completed without errors.</summary>
    public bool IsValid;
    /// <summary>Contains replacement diagnostics.</summary>
    public XlsxValidationResult Validation;
    /// <summary>Stores the number of cells whose text changed.</summary>
    public int ReplacedCellCount;
    /// <summary>Stores the number of formulas whose text changed.</summary>
    public int ReplacedFormulaCount;
    /// <summary>Stores the total number of literal token occurrences replaced.</summary>
    public int ReplacementCount;

    /// <summary>Initializes an empty successful replacement result.</summary>
    public XlsxTemplateReplacementResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        ReplacedCellCount = 0;
        ReplacedFormulaCount = 0;
        ReplacementCount = 0;
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
