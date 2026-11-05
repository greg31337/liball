using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a worksheet filter operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxWorksheetFilterResult
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
    /// Stores the auto filter value used by this model.
    /// </summary>
    public XlsxAutoFilter? AutoFilter;
    /// <summary>
    /// Indicates whether auto filter is present and should be written.
    /// </summary>
    public bool HasAutoFilter;
    /// <summary>
    /// Stores the filter column value used by this model.
    /// </summary>
    public XlsxAutoFilterColumn? FilterColumn;
    /// <summary>
    /// Indicates whether filter column is present and should be written.
    /// </summary>
    public bool HasFilterColumn;
    /// <summary>
    /// Stores the table value used by this model.
    /// </summary>
    public XlsxTablePart? Table;
    /// <summary>
    /// Indicates whether table is present and should be written.
    /// </summary>
    public bool HasTable;
    /// <summary>
    /// Stores the table column value used by this model.
    /// </summary>
    public XlsxTableColumn? TableColumn;
    /// <summary>
    /// Indicates whether table column is present and should be written.
    /// </summary>
    public bool HasTableColumn;
    /// <summary>
    /// Initializes a new worksheet filter result with its default state.
    /// </summary>
    public XlsxWorksheetFilterResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        AutoFilter = null;
        HasAutoFilter = false;
        FilterColumn = null;
        HasFilterColumn = false;
        Table = null;
        HasTable = false;
        TableColumn = null;
        HasTableColumn = false;
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

    internal static void AddRangeDiagnostics(XlsxWorksheetFilterResult result, XlsxA1RangeReferenceResult rangeResult)
    {
        foreach (XlsxDiagnostic diagnostic in rangeResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }

    internal static void AddTableDiagnostics(XlsxWorksheetFilterResult result, XlsxWorksheetTableResult tableResult)
    {
        foreach (XlsxDiagnostic diagnostic in tableResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }

        result.Table = tableResult.Table;
        result.HasTable = tableResult.HasTable;
        result.TableColumn = tableResult.Column;
        result.HasTableColumn = tableResult.HasColumn;
    }
}
