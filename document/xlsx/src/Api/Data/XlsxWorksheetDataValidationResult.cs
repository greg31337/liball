using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a worksheet data validation operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxWorksheetDataValidationResult
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
    /// Contains data validations.
    /// </summary>
    public XlsxDataValidations? DataValidations;
    /// <summary>
    /// Indicates whether data validations is present and should be written.
    /// </summary>
    public bool HasDataValidations;
    /// <summary>
    /// Stores the data validation value used by this model.
    /// </summary>
    public XlsxDataValidation? DataValidation;
    /// <summary>
    /// Indicates whether data validation is present and should be written.
    /// </summary>
    public bool HasDataValidation;
    /// <summary>
    /// Stores the number of added items processed by the operation.
    /// </summary>
    public int AddedCount;
    /// <summary>
    /// Stores the number of removed items processed by the operation.
    /// </summary>
    public int RemovedCount;
    /// <summary>
    /// Initializes a new worksheet data validation result with its default state.
    /// </summary>
    public XlsxWorksheetDataValidationResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        DataValidations = null;
        HasDataValidations = false;
        DataValidation = null;
        HasDataValidation = false;
        AddedCount = 0;
        RemovedCount = 0;
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

    internal static void AddRangeDiagnostics(XlsxWorksheetDataValidationResult result, XlsxA1RangeReferenceResult rangeResult)
    {
        foreach (XlsxDiagnostic diagnostic in rangeResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}
