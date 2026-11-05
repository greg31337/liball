using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Contains multiple selected workbook ranges materialized in request order.
/// </summary>
public class XlsxSelectedRangesMaterializationResult
{
    /// <summary>Indicates whether every selected range was materialized without errors.</summary>
    public bool IsValid;
    /// <summary>Contains selection and materialization diagnostics.</summary>
    public XlsxValidationResult Validation;
    /// <summary>Contains materialized ranges in request order.</summary>
    public List<XlsxWorkbookRangeMaterialization> Ranges;

    /// <summary>Initializes an empty successful selected-range result.</summary>
    public XlsxSelectedRangesMaterializationResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Ranges = new List<XlsxWorkbookRangeMaterialization>();
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
