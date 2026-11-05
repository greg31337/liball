using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a formula parse operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxFormulaParseResult
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
    /// Stores the formula value used by this model.
    /// </summary>
    public string Formula;
    /// <summary>
    /// Contains tokens.
    /// </summary>
    public List<XlsxFormulaToken> Tokens;
    /// <summary>
    /// Stores the root value used by this model.
    /// </summary>
    public XlsxFormulaNode Root;
    /// <summary>
    /// Indicates whether root is present and should be written.
    /// </summary>
    public bool HasRoot;
    /// <summary>
    /// Initializes a new formula parse result with its default state.
    /// </summary>
    public XlsxFormulaParseResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Formula = "";
        Tokens = new List<XlsxFormulaToken>();
        Root = new XlsxFormulaNode();
        HasRoot = false;
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
