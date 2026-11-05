using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a csv export operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxCsvExportResult
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
    /// Stores the csv value used by this model.
    /// </summary>
    public string Csv;
    /// <summary>
    /// Indicates whether csv is present and should be written.
    /// </summary>
    public bool HasCsv;
    /// <summary>
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string Reference;
    /// <summary>
    /// Stores the number of row items processed by the operation.
    /// </summary>
    public int RowCount;
    /// <summary>
    /// Stores the number of column items processed by the operation.
    /// </summary>
    public int ColumnCount;
    /// <summary>
    /// Stores the number of exported row items processed by the operation.
    /// </summary>
    public int ExportedRowCount;
    /// <summary>
    /// Stores the number of exported cell items processed by the operation.
    /// </summary>
    public int ExportedCellCount;
    /// <summary>
    /// Initializes a new csv export result with its default state.
    /// </summary>
    public XlsxCsvExportResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Csv = "";
        HasCsv = false;
        Reference = "";
        RowCount = 0;
        ColumnCount = 0;
        ExportedRowCount = 0;
        ExportedCellCount = 0;
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

    internal static void AddRangeDiagnostics(XlsxCsvExportResult result, XlsxA1RangeReferenceResult rangeResult)
    {
        foreach (XlsxDiagnostic diagnostic in rangeResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}
