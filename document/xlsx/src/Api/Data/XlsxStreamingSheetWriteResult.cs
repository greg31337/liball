using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a streaming sheet write operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxStreamingSheetWriteResult
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
    /// Stores the session value used by this model.
    /// </summary>
    public XlsxStreamingSheetWriteSession? Session;
    /// <summary>
    /// Contains shared strings.
    /// </summary>
    public XlsxSharedStringTablePart SharedStrings;
    /// <summary>
    /// Stores the number of row items processed by the operation.
    /// </summary>
    public int RowCount;
    /// <summary>
    /// Stores the number of cell items processed by the operation.
    /// </summary>
    public int CellCount;
    /// <summary>
    /// Initializes a new streaming sheet write result with its default state.
    /// </summary>
    public XlsxStreamingSheetWriteResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Session = null;
        SharedStrings = new XlsxSharedStringTablePart();
        RowCount = 0;
        CellCount = 0;
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
