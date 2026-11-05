using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a worksheet column edit operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxWorksheetColumnEditResult
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
    /// Stores the number of affected cell items processed by the operation.
    /// </summary>
    public int AffectedCellCount;
    /// <summary>
    /// Stores the number of moved cell items processed by the operation.
    /// </summary>
    public int MovedCellCount;
    /// <summary>
    /// Stores the number of removed cell items processed by the operation.
    /// </summary>
    public int RemovedCellCount;
    /// <summary>
    /// Stores the number of updated cell reference items processed by the operation.
    /// </summary>
    public int UpdatedCellReferenceCount;
    /// <summary>
    /// Stores the number of affected column items processed by the operation.
    /// </summary>
    public int AffectedColumnCount;
    /// <summary>
    /// Stores the number of moved column items processed by the operation.
    /// </summary>
    public int MovedColumnCount;
    /// <summary>
    /// Stores the number of removed column items processed by the operation.
    /// </summary>
    public int RemovedColumnCount;
    /// <summary>
    /// Stores the number of formula reference update items processed by the operation.
    /// </summary>
    public int FormulaReferenceUpdateCount;
    /// <summary>
    /// Stores the number of deleted formula reference items processed by the operation.
    /// </summary>
    public int DeletedFormulaReferenceCount;
    /// <summary>
    /// Stores the number of structural reference update items processed by the operation.
    /// </summary>
    public int StructuralReferenceUpdateCount;
    /// <summary>
    /// Stores the number of removed structural reference items processed by the operation.
    /// </summary>
    public int RemovedStructuralReferenceCount;
    /// <summary>
    /// Initializes a new worksheet column edit result with its default state.
    /// </summary>
    public XlsxWorksheetColumnEditResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        AffectedCellCount = 0;
        MovedCellCount = 0;
        RemovedCellCount = 0;
        UpdatedCellReferenceCount = 0;
        AffectedColumnCount = 0;
        MovedColumnCount = 0;
        RemovedColumnCount = 0;
        FormulaReferenceUpdateCount = 0;
        DeletedFormulaReferenceCount = 0;
        StructuralReferenceUpdateCount = 0;
        RemovedStructuralReferenceCount = 0;
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
