using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a csv import operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxCsvImportResult
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
    /// Stores an A1-style cell or range reference.
    /// </summary>
    public string Reference;
    /// <summary>
    /// Stores the number of imported row items processed by the operation.
    /// </summary>
    public int ImportedRowCount;
    /// <summary>
    /// Stores the number of column items processed by the operation.
    /// </summary>
    public int ColumnCount;
    /// <summary>
    /// Stores the number of imported cell items processed by the operation.
    /// </summary>
    public int ImportedCellCount;
    /// <summary>
    /// Stores the number of created cell items processed by the operation.
    /// </summary>
    public int CreatedCellCount;
    /// <summary>
    /// Initializes a new csv import result with its default state.
    /// </summary>
    public XlsxCsvImportResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Reference = "";
        ImportedRowCount = 0;
        ColumnCount = 0;
        ImportedCellCount = 0;
        CreatedCellCount = 0;
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

    internal static void AddCellAccessDiagnostics(XlsxCsvImportResult result, XlsxWorksheetCellAccessResult cellResult)
    {
        foreach (XlsxDiagnostic diagnostic in cellResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddRangeDiagnostics(XlsxCsvImportResult result, XlsxA1CellReferenceResult referenceResult)
    {
        foreach (XlsxDiagnostic diagnostic in referenceResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddParseDiagnostics(XlsxCsvImportResult result, XlsxCsvParseResult parseResult)
    {
        foreach (XlsxDiagnostic diagnostic in parseResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}
