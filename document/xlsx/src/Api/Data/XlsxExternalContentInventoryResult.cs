using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Contains an inventory of externally sourced and externally targeted workbook content.
/// </summary>
public class XlsxExternalContentInventoryResult
{
    /// <summary>Indicates whether inventory completed without errors.</summary>
    public bool IsValid;
    /// <summary>Contains inventory diagnostics.</summary>
    public XlsxValidationResult Validation;
    /// <summary>Contains unique external-content items.</summary>
    public List<XlsxExternalContentItem> Items;

    /// <summary>Initializes an empty successful inventory.</summary>
    public XlsxExternalContentInventoryResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Items = new List<XlsxExternalContentItem>();
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
