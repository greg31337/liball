using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Defines formula analysis, workbook dependency, impact, and calculation operations.
/// </summary>
public interface IXlsxFormulaService
{
    /// <summary>
    /// Analyzes a formula for volatile, unsafe, unsupported, future, and external constructs.
    /// </summary>
    /// <param name="formula">The formula text to analyze.</param>
    /// <returns>The formula analysis result.</returns>
    XlsxFormulaAnalysisResult AnalyzeFormula(string formula);
    /// <summary>
    /// Builds a dependency graph across every worksheet in a workbook.
    /// </summary>
    /// <param name="workbook">The workbook to inspect.</param>
    /// <returns>The workbook-wide dependency graph.</returns>
    XlsxWorkbookFormulaDependencyGraphResult BuildWorkbookDependencyGraph(XlsxWorkbook workbook);
    /// <summary>
    /// Finds direct and transitive dependents of changed cells.
    /// </summary>
    /// <param name="graph">The workbook-wide dependency graph.</param>
    /// <param name="changedReferences">Sheet-qualified changed cell references.</param>
    /// <returns>The impacted-cell result.</returns>
    XlsxImpactedCellsResult FindImpactedCells(XlsxWorkbookFormulaDependencyGraphResult graph, List<string> changedReferences);
    /// <summary>
    /// Calculates a formula with workbook and worksheet context.
    /// </summary>
    /// <param name="workbook">The workbook containing referenced cells.</param>
    /// <param name="sheetName">The worksheet used for unqualified references.</param>
    /// <param name="formula">The formula text to calculate.</param>
    /// <param name="options">Calculation safety and behavior options.</param>
    /// <returns>The calculated value and explicit capability report.</returns>
    XlsxFormulaCalculationResult CalculateFormula(XlsxWorkbook workbook, string sheetName, string formula, XlsxFormulaCalculationOptions options);
    /// <summary>
    /// Recalculates supported formulas throughout a workbook.
    /// </summary>
    /// <param name="workbook">The workbook to calculate.</param>
    /// <param name="options">Calculation safety and behavior options.</param>
    /// <returns>The workbook calculation result.</returns>
    XlsxFormulaCalculationResult CalculateWorkbook(XlsxWorkbook workbook, XlsxFormulaCalculationOptions options);
}
