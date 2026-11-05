using System;

namespace xlsx;

/// <summary>
/// Contains serialized pivot-cache records and validation diagnostics.
/// </summary>
public class XlsxPivotCacheRecordsWriteResult
{
    /// <summary>Indicates whether serialization completed without errors.</summary>
    public bool IsValid;
    /// <summary>Contains validation diagnostics.</summary>
    public XlsxValidationResult Validation;
    /// <summary>Stores serialized XML.</summary>
    public string Xml;

    /// <summary>Initializes an empty pivot-cache records write result.</summary>
    public XlsxPivotCacheRecordsWriteResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Xml = "";
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
