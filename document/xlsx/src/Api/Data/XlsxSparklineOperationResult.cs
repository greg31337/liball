using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a sparkline operation operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxSparklineOperationResult
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
    /// Stores the group value used by this model.
    /// </summary>
    public XlsxSparklineGroup? Group;
    /// <summary>
    /// Indicates whether group is present and should be written.
    /// </summary>
    public bool HasGroup;
    /// <summary>
    /// Stores the sparkline value used by this model.
    /// </summary>
    public XlsxSparkline? Sparkline;
    /// <summary>
    /// Indicates whether sparkline is present and should be written.
    /// </summary>
    public bool HasSparkline;
    /// <summary>
    /// Initializes a new sparkline operation result with its default state.
    /// </summary>
    public XlsxSparklineOperationResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Group = null;
        HasGroup = false;
        Sparkline = null;
        HasSparkline = false;
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
