using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a workbook repair operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxWorkbookRepairResult
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
    /// Stores the workbook value used by this model.
    /// </summary>
    public XlsxWorkbook? Workbook;
    /// <summary>
    /// Contains the serialized bytes produced by the operation.
    /// </summary>
    public byte[] Bytes;
    /// <summary>
    /// Stores the number of repair items processed by the operation.
    /// </summary>
    public int RepairCount;
    /// <summary>
    /// Contains repairs.
    /// </summary>
    public List<string> Repairs;
    /// <summary>
    /// Initializes a new workbook repair result with its default state.
    /// </summary>
    public XlsxWorkbookRepairResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Workbook = null;
        Bytes = new byte[0];
        RepairCount = 0;
        Repairs = new List<string>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    /// <summary>
    /// Adds repair to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "code">The diagnostic code, which should remain stable across releases.</param>
    /// <param name = "message">The human-readable diagnostic message.</param>
    internal void AddRepair(string code, string message)
    {
        Repairs.Add(code);
        RepairCount = RepairCount + 1;
        AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Info, code, message));
    }

    internal void AddError(string code, string message)
    {
        AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, code, message));
    }
}
