using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Contains the explicit dependency details for one sheet-qualified cell.
/// </summary>
public class XlsxFormulaDependencyQueryResult
{
    /// <summary>Indicates whether the query completed without errors.</summary>
    public bool IsValid;
    /// <summary>Contains dependency-query diagnostics.</summary>
    public XlsxValidationResult Validation;
    /// <summary>Stores the normalized sheet-qualified cell reference.</summary>
    public string QualifiedReference;
    /// <summary>Indicates whether the dependency graph contains the requested cell.</summary>
    public bool HasNode;
    /// <summary>Stores the cell formula when present.</summary>
    public string Formula;
    /// <summary>Contains direct precedent references.</summary>
    public List<string> Precedents;
    /// <summary>Contains direct dependent references.</summary>
    public List<string> Dependents;
    /// <summary>Contains external precedents.</summary>
    public List<string> ExternalPrecedents;
    /// <summary>Contains defined or structured names used by the formula.</summary>
    public List<string> Names;

    /// <summary>Initializes an empty successful dependency query.</summary>
    public XlsxFormulaDependencyQueryResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        QualifiedReference = "";
        HasNode = false;
        Formula = "";
        Precedents = new List<string>();
        Dependents = new List<string>();
        ExternalPrecedents = new List<string>();
        Names = new List<string>();
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
