using System;

namespace xlsx;

/// <summary>
/// Contains the resolved pivot-cache records relationship and diagnostics.
/// </summary>
public class XlsxPivotCacheRelationshipResult
{
    /// <summary>Indicates whether resolution completed without errors.</summary>
    public bool IsValid;
    /// <summary>Contains resolution diagnostics.</summary>
    public XlsxValidationResult Validation;
    /// <summary>Stores the resolved records relationship.</summary>
    public XlsxRelationship? Relationship;
    /// <summary>Indicates whether the records relationship is present.</summary>
    public bool HasRelationship;

    /// <summary>Initializes an empty pivot-cache relationship result.</summary>
    public XlsxPivotCacheRelationshipResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Relationship = null;
        HasRelationship = false;
    }

    internal XlsxDiagnostic AddError(string code, string message)
    {
        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, message);
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
        return diagnostic;
    }
}
