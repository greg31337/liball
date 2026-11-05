using System;

namespace xlsx;

/// <summary>Reports detected resource counts, removals, and diagnostics from a security-policy evaluation.</summary>
public class XlsxSecurityPolicyResult
{
    /// <summary>Indicates whether the workbook satisfies the policy.</summary>
    public bool IsValid;
    /// <summary>Contains policy diagnostics.</summary>
    public XlsxValidationResult Validation;
    /// <summary>Stores the detected image count.</summary>
    public int ImageCount;
    /// <summary>Stores the detected comment count.</summary>
    public int CommentCount;
    /// <summary>Stores the detected formula count.</summary>
    public int FormulaCount;
    /// <summary>Stores the detected external-relationship count.</summary>
    public int ExternalRelationshipCount;
    /// <summary>Stores the number of macros removed.</summary>
    public int MacrosRemoved;
    /// <summary>Stores the number of OLE objects and embedded packages removed.</summary>
    public int OleObjectsRemoved;
    /// <summary>Stores the number of external image relationships removed.</summary>
    public int ExternalImagesRemoved;
    /// <summary>Stores the number of external links removed.</summary>
    public int ExternalLinksRemoved;
    /// <summary>Stores the number of connection parts removed.</summary>
    public int ConnectionsRemoved;
    /// <summary>Stores the number of query-table parts removed.</summary>
    public int QueryTablesRemoved;
    /// <summary>Stores the number of custom XML parts removed.</summary>
    public int CustomXmlPartsRemoved;
    /// <summary>Stores the number of unknown active-content parts removed.</summary>
    public int UnknownActivePartsRemoved;

    /// <summary>Initializes an empty successful result.</summary>
    public XlsxSecurityPolicyResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        ImageCount = 0;
        CommentCount = 0;
        FormulaCount = 0;
        ExternalRelationshipCount = 0;
        MacrosRemoved = 0;
        OleObjectsRemoved = 0;
        ExternalImagesRemoved = 0;
        ExternalLinksRemoved = 0;
        ConnectionsRemoved = 0;
        QueryTablesRemoved = 0;
        CustomXmlPartsRemoved = 0;
        UnknownActivePartsRemoved = 0;
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
