using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a data table import operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxDataTableImportResult
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
    /// Stores the number of header row items processed by the operation.
    /// </summary>
    public int HeaderRowCount;
    /// <summary>
    /// Stores the number of data row items processed by the operation.
    /// </summary>
    public int DataRowCount;
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
    /// Stores the table value used by this model.
    /// </summary>
    public XlsxTablePart? Table;
    /// <summary>
    /// Indicates whether table is present and should be written.
    /// </summary>
    public bool HasTable;
    /// <summary>
    /// Initializes a new data table import result with its default state.
    /// </summary>
    public XlsxDataTableImportResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Reference = "";
        HeaderRowCount = 0;
        DataRowCount = 0;
        ColumnCount = 0;
        ImportedCellCount = 0;
        CreatedCellCount = 0;
        Table = null;
        HasTable = false;
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

    internal static void AddCellAccessDiagnostics(XlsxDataTableImportResult result, XlsxWorksheetCellAccessResult cellResult)
    {
        foreach (XlsxDiagnostic diagnostic in cellResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddRangeDiagnostics(XlsxDataTableImportResult result, XlsxA1CellReferenceResult referenceResult)
    {
        foreach (XlsxDiagnostic diagnostic in referenceResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddTableDiagnostics(XlsxDataTableImportResult result, XlsxWorksheetTableResult tableResult)
    {
        foreach (XlsxDiagnostic diagnostic in tableResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}
