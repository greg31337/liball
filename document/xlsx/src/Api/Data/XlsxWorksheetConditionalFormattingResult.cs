using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a worksheet conditional formatting operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxWorksheetConditionalFormattingResult
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
    /// Stores the conditional formatting value used by this model.
    /// </summary>
    public XlsxConditionalFormatting? ConditionalFormatting;
    /// <summary>
    /// Indicates whether conditional formatting is present and should be written.
    /// </summary>
    public bool HasConditionalFormatting;
    /// <summary>
    /// Stores the rule value used by this model.
    /// </summary>
    public XlsxConditionalFormatRule? Rule;
    /// <summary>
    /// Indicates whether rule is present and should be written.
    /// </summary>
    public bool HasRule;
    /// <summary>
    /// Stores the number of added items processed by the operation.
    /// </summary>
    public int AddedCount;
    /// <summary>
    /// Stores the number of removed items processed by the operation.
    /// </summary>
    public int RemovedCount;
    /// <summary>
    /// Initializes a new worksheet conditional formatting result with its default state.
    /// </summary>
    public XlsxWorksheetConditionalFormattingResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        ConditionalFormatting = null;
        HasConditionalFormatting = false;
        Rule = null;
        HasRule = false;
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

    internal static void AddRangeDiagnostics(XlsxWorksheetConditionalFormattingResult result, XlsxA1RangeReferenceResult rangeResult)
    {
        foreach (XlsxDiagnostic diagnostic in rangeResult.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}
