using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Contains the outcome, validation diagnostics, and data produced by a formula calculation operation.
/// </summary>
/// <remarks>
/// Result data may be incomplete when <c>IsValid</c> is false. Diagnostics contain stable codes suitable for tests and logging.
/// </remarks>
public class XlsxFormulaCalculationResult
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
    /// Stores the value value used by this model.
    /// </summary>
    public XlsxFormulaCalculationValue? Value;
    /// <summary>
    /// Stores the formula value used by this model.
    /// </summary>
    public string Formula;
    /// <summary>
    /// Stores the number of calculated formula items processed by the operation.
    /// </summary>
    public int CalculatedFormulaCount;
    /// <summary>
    /// Stores the number of calculated spill cell items processed by the operation.
    /// </summary>
    public int CalculatedSpillCellCount;
    /// <summary>
    /// Stores the number of formulas whose cached values were preserved because calculation was unsupported or blocked.
    /// </summary>
    public int PreservedFormulaCount;
    /// <summary>
    /// Contains unsupported function names encountered during calculation.
    /// </summary>
    public List<string> UnsupportedFunctions;
    /// <summary>
    /// Contains future-prefixed function names encountered during calculation.
    /// </summary>
    public List<string> FutureFunctions;
    /// <summary>
    /// Contains volatile function names encountered during calculation.
    /// </summary>
    public List<string> VolatileFunctions;
    /// <summary>
    /// Contains unsafe function names encountered during calculation.
    /// </summary>
    public List<string> UnsafeFunctions;
    /// <summary>
    /// Contains external-workbook references encountered during calculation.
    /// </summary>
    public List<string> ExternalReferences;
    /// <summary>
    /// Initializes a new formula calculation result with its default state.
    /// </summary>
    public XlsxFormulaCalculationResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Value = XlsxFormulaCalculationValue.CreateBlank();
        Formula = "";
        CalculatedFormulaCount = 0;
        CalculatedSpillCellCount = 0;
        PreservedFormulaCount = 0;
        UnsupportedFunctions = new List<string>();
        FutureFunctions = new List<string>();
        VolatileFunctions = new List<string>();
        UnsafeFunctions = new List<string>();
        ExternalReferences = new List<string>();
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
