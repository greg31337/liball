using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Describes functions and references that affect whether a formula can be calculated safely.
/// </summary>
public class XlsxFormulaAnalysisResult
{
    /// <summary>
    /// Indicates whether the formula was parsed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains parse and analysis diagnostics.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Stores the analyzed formula text.
    /// </summary>
    public string Formula;
    /// <summary>
    /// Contains every distinct function name used by the formula.
    /// </summary>
    public List<string> Functions;
    /// <summary>
    /// Contains volatile function names used by the formula.
    /// </summary>
    public List<string> VolatileFunctions;
    /// <summary>
    /// Contains functions that may access external data or invoke active behavior.
    /// </summary>
    public List<string> UnsafeFunctions;
    /// <summary>
    /// Contains functions that the built-in calculation engine does not support.
    /// </summary>
    public List<string> UnsupportedFunctions;
    /// <summary>
    /// Contains function names encoded with a future-formula prefix.
    /// </summary>
    public List<string> FutureFunctions;
    /// <summary>
    /// Contains external-workbook references used by the formula.
    /// </summary>
    public List<string> ExternalReferences;
    /// <summary>
    /// Contains references to other worksheets in the current workbook.
    /// </summary>
    public List<string> CrossSheetReferences;
    /// <summary>
    /// Indicates whether at least one volatile function is present.
    /// </summary>
    public bool HasVolatileFunction;
    /// <summary>
    /// Indicates whether at least one unsafe function is present.
    /// </summary>
    public bool HasUnsafeFunction;
    /// <summary>
    /// Indicates whether at least one external-workbook reference is present.
    /// </summary>
    public bool HasExternalReference;
    /// <summary>
    /// Indicates whether future formula syntax is present.
    /// </summary>
    public bool HasFutureSyntax;

    /// <summary>
    /// Initializes an empty formula analysis result.
    /// </summary>
    public XlsxFormulaAnalysisResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Formula = "";
        Functions = new List<string>();
        VolatileFunctions = new List<string>();
        UnsafeFunctions = new List<string>();
        UnsupportedFunctions = new List<string>();
        FutureFunctions = new List<string>();
        ExternalReferences = new List<string>();
        CrossSheetReferences = new List<string>();
        HasVolatileFunction = false;
        HasUnsafeFunction = false;
        HasExternalReference = false;
        HasFutureSyntax = false;
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}
