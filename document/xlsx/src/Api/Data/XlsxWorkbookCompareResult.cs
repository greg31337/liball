using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a workbook compare operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxWorkbookCompareResult
{
    /// <summary>
    /// Indicates whether the operation completed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Stores the are equal value used by this model.
    /// </summary>
    public bool AreEqual;
    /// <summary>
    /// Contains diagnostics produced while the object was read, changed, validated, or written.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Contains differences.
    /// </summary>
    public List<XlsxWorkbookDifference> Differences;
    /// <summary>
    /// Stores the number of difference items processed by the operation.
    /// </summary>
    public int DifferenceCount;
    /// <summary>
    /// Initializes a new workbook compare result with its default state.
    /// </summary>
    public XlsxWorkbookCompareResult()
    {
        IsValid = true;
        AreEqual = true;
        Validation = new XlsxValidationResult();
        Differences = new List<XlsxWorkbookDifference>();
        DifferenceCount = 0;
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

    /// <summary>
    /// Adds difference to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "code">The diagnostic code, which should remain stable across releases.</param>
    /// <param name = "path">The file-system path to read or write.</param>
    /// <param name = "message">The human-readable diagnostic message.</param>
    /// <param name = "leftValue">The left value value used by the operation.</param>
    /// <param name = "rightValue">The right value value used by the operation.</param>
    /// <returns>The workbook difference produced by the operation.</returns>
    internal XlsxWorkbookDifference AddDifference(string code, string path, string message, string leftValue, string rightValue)
    {
        XlsxWorkbookDifference difference = new XlsxWorkbookDifference();
        difference.Code = XlsxCellValueConverter.NormalizeText(code);
        difference.Path = XlsxCellValueConverter.NormalizeText(path);
        difference.Message = XlsxCellValueConverter.NormalizeText(message);
        difference.LeftValue = XlsxCellValueConverter.NormalizeText(leftValue);
        difference.RightValue = XlsxCellValueConverter.NormalizeText(rightValue);
        Differences.Add(difference);
        DifferenceCount = Differences.Count;
        AreEqual = false;
        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Warning, difference.Code, difference.Message);
        diagnostic.SetLocation(difference.Path, "", "");
        Validation.AddDiagnostic(diagnostic);
        return difference;
    }
}
