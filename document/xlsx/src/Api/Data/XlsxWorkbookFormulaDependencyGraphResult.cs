using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Contains a workbook-wide formula dependency graph using sheet-qualified cell references.
/// </summary>
public class XlsxWorkbookFormulaDependencyGraphResult
{
    /// <summary>
    /// Indicates whether the graph was built without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains graph construction diagnostics.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Contains formula and precedent nodes from every worksheet.
    /// </summary>
    public List<XlsxFormulaDependencyNode> Nodes;

    /// <summary>
    /// Initializes an empty workbook dependency graph result.
    /// </summary>
    public XlsxWorkbookFormulaDependencyGraphResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Nodes = new List<XlsxFormulaDependencyNode>();
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

    internal XlsxFormulaDependencyNode? GetNode(string qualifiedReference)
    {
        foreach (XlsxFormulaDependencyNode node in Nodes)
        {
            if (string.Equals(node.QualifiedReference, qualifiedReference, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }
        }

        return null;
    }
}
