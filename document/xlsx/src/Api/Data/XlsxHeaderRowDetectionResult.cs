using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Describes deterministic header-row detection for a workbook range.
/// </summary>
public class XlsxHeaderRowDetectionResult
{
    /// <summary>Indicates whether detection completed without errors.</summary>
    public bool IsValid;
    /// <summary>Contains detection diagnostics.</summary>
    public XlsxValidationResult Validation;
    /// <summary>Stores the worksheet name.</summary>
    public string SheetName;
    /// <summary>Stores the inspected range.</summary>
    public string Reference;
    /// <summary>Indicates whether a likely header row was found.</summary>
    public bool HasHeaderRow;
    /// <summary>Stores the one-based worksheet row index of the candidate header.</summary>
    public int HeaderRow;
    /// <summary>Stores a score from zero through one for the candidate header.</summary>
    public double Confidence;
    /// <summary>Contains normalized header labels in column order.</summary>
    public List<string> Headers;

    /// <summary>Initializes an empty successful detection result.</summary>
    public XlsxHeaderRowDetectionResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        SheetName = "";
        Reference = "";
        HasHeaderRow = false;
        HeaderRow = 0;
        Confidence = 0;
        Headers = new List<string>();
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
