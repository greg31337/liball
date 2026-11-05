using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a formula dependency graph operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxFormulaDependencyGraphResult
{
    /// <summary>
    /// Indicates whether the operation completed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics produced while the object was read, changed, validated, or written.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Contains nodes.
    /// </summary>
    public List<XlsxFormulaDependencyNode> Nodes;
    /// <summary>
    /// Initializes a new formula dependency graph result with its default state.
    /// </summary>
    public XlsxFormulaDependencyGraphResult()
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

    /// <summary>
    /// Returns node from the current model.
    /// </summary>
    /// <param name = "cellReference">An A1-style cell or range reference.</param>
    /// <returns>The formula dependency node produced by the operation.</returns>
    internal XlsxFormulaDependencyNode? GetNode(string cellReference)
    {
        foreach (XlsxFormulaDependencyNode node in Nodes)
        {
            if (node.CellReference == cellReference)
            {
                return node;
            }
        }

        return null;
    }
}
