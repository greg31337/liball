using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Provides workbook-aware formula analysis, dependency, impact, and calculation operations.
/// </summary>
public class XlsxFormulaService : IXlsxFormulaService
{
    /// <summary>
    /// Initializes a formula service.
    /// </summary>
    public XlsxFormulaService()
    {
    }

    /// <summary>
    /// Analyzes a formula for volatile, unsafe, unsupported, future, and external constructs.
    /// </summary>
    /// <param name="formula">The formula text to analyze.</param>
    /// <returns>The formula analysis result.</returns>
    public XlsxFormulaAnalysisResult AnalyzeFormula(string formula)
    {
        return AnalyzeFormulaCore(formula);
    }

    /// <summary>
    /// Builds a dependency graph across every worksheet in a workbook.
    /// </summary>
    /// <param name="workbook">The workbook to inspect.</param>
    /// <returns>The workbook-wide dependency graph.</returns>
    public XlsxWorkbookFormulaDependencyGraphResult BuildWorkbookDependencyGraph(XlsxWorkbook workbook)
    {
        XlsxWorkbookFormulaDependencyGraphResult result = new XlsxWorkbookFormulaDependencyGraphResult();
        if (workbook == null || workbook.Worksheets == null || workbook.WorkbookPart == null || workbook.WorkbookPart.Sheets == null)
        {
            result.AddError("formulaDependency.workbookMissing", "Workbook worksheet and sheet metadata are required.");
            return result;
        }

        Dictionary<string, XlsxFormulaDependencyNode> nodes = new Dictionary<string, XlsxFormulaDependencyNode>(StringComparer.OrdinalIgnoreCase);
        int sheetIndex = 0;
        while (sheetIndex < workbook.Worksheets.Count)
        {
            XlsxWorksheetPart worksheet = workbook.Worksheets[sheetIndex];
            string sheetName = GetSheetName(workbook, sheetIndex);
            AddWorksheetNodes(result, nodes, worksheet, sheetName);
            sheetIndex = sheetIndex + 1;
        }

        AddWorkbookDependentLinks(nodes);
        foreach (KeyValuePair<string, XlsxFormulaDependencyNode> item in nodes)
        {
            result.Nodes.Add(item.Value);
        }

        return result;
    }

    /// <summary>
    /// Finds direct and transitive dependents of changed cells.
    /// </summary>
    /// <param name="graph">The workbook-wide dependency graph.</param>
    /// <param name="changedReferences">Sheet-qualified changed cell references.</param>
    /// <returns>The impacted-cell result.</returns>
    public XlsxImpactedCellsResult FindImpactedCells(XlsxWorkbookFormulaDependencyGraphResult graph, List<string> changedReferences)
    {
        XlsxImpactedCellsResult result = new XlsxImpactedCellsResult();
        if (graph == null || graph.Nodes == null)
        {
            result.AddError("formulaImpact.graphMissing", "A workbook dependency graph is required.");
            return result;
        }
        if (changedReferences == null || changedReferences.Count == 0)
        {
            result.AddError("formulaImpact.referencesMissing", "At least one changed cell reference is required.");
            return result;
        }

        List<string> queue = new List<string>();
        foreach (string changed in changedReferences)
        {
            string normalized = NormalizeQualifiedReference(changed);
            AddUnique(result.StartingReferences, normalized);
            AddUnique(queue, normalized);
        }

        int queueIndex = 0;
        while (queueIndex < queue.Count)
        {
            string current = queue[queueIndex];
            XlsxFormulaDependencyNode? node = FindNode(graph.Nodes, current);
            if (node != null)
            {
                foreach (string dependent in node.Dependents)
                {
                    if (queueIndex < result.StartingReferences.Count)
                    {
                        AddUnique(result.DirectDependents, dependent);
                    }
                    if (!ContainsText(result.StartingReferences, dependent) && !ContainsText(result.ImpactedCells, dependent))
                    {
                        result.ImpactedCells.Add(dependent);
                        queue.Add(dependent);
                    }
                }
            }
            queueIndex = queueIndex + 1;
        }

        return result;
    }

    /// <summary>
    /// Calculates a formula with workbook and worksheet context.
    /// </summary>
    /// <param name="workbook">The workbook containing referenced cells.</param>
    /// <param name="sheetName">The worksheet used for unqualified references.</param>
    /// <param name="formula">The formula text to calculate.</param>
    /// <param name="options">Calculation safety and behavior options.</param>
    /// <returns>The calculated value and explicit capability report.</returns>
    public XlsxFormulaCalculationResult CalculateFormula(XlsxWorkbook workbook, string sheetName, string formula, XlsxFormulaCalculationOptions options)
    {
        if (options == null)
        {
            options = new XlsxFormulaCalculationOptions();
        }
        XlsxFormulaCalculationEngine engine = XlsxFormulaCalculationEngine.CreateForWorkbook(workbook, sheetName, options);
        return engine.EvaluateFormula(formula);
    }

    /// <summary>
    /// Recalculates supported formulas throughout a workbook.
    /// </summary>
    /// <param name="workbook">The workbook to calculate.</param>
    /// <param name="options">Calculation safety and behavior options.</param>
    /// <returns>The workbook calculation result.</returns>
    public XlsxFormulaCalculationResult CalculateWorkbook(XlsxWorkbook workbook, XlsxFormulaCalculationOptions options)
    {
        XlsxFormulaCalculationResult result = new XlsxFormulaCalculationResult();
        if (workbook == null || workbook.Worksheets == null)
        {
            result.AddError("formulaCalculation.workbookMissing", "A workbook with worksheets is required.");
            result.Value = XlsxFormulaCalculationValue.CreateError("#REF!");
            return result;
        }
        if (options == null)
        {
            options = new XlsxFormulaCalculationOptions();
        }
        if (options.MaximumPasses <= 0)
        {
            result.AddError("formulaCalculation.maximumPassesRange", "Maximum calculation passes must be positive.");
            return result;
        }

        int pass = 0;
        int formulaCount = 0;
        while (pass < options.MaximumPasses)
        {
            formulaCount = 0;
            int sheetIndex = 0;
            while (sheetIndex < workbook.Worksheets.Count)
            {
                XlsxWorksheetPart worksheet = workbook.Worksheets[sheetIndex];
                string sheetName = GetSheetName(workbook, sheetIndex);
                XlsxFormulaCalculationEngine engine = XlsxFormulaCalculationEngine.CreateForWorkbook(workbook, sheetName, options);
                if (worksheet != null && worksheet.Rows != null)
                {
                    foreach (XlsxWorksheetRow row in worksheet.Rows)
                    {
                        if (row != null && row.Cells != null)
                        {
                            foreach (XlsxWorksheetCell cell in row.Cells)
                            {
                                if (XlsxWorksheetPart.IsCalculableFormulaCell(cell))
                                {
                                    formulaCount = formulaCount + 1;
                                    XlsxFormulaCalculationResult calculation = engine.EvaluateFormula(XlsxWorksheetPart.GetFormulaText(cell));
                                    MergeCalculationReport(result, calculation);
                                    bool preserve = calculation.UnsupportedFunctions.Count > 0 && options.PreserveCachedValuesForUnsupported;
                                    if (!preserve && calculation.IsValid && calculation.Value != null)
                                    {
                                        XlsxWorksheetPart.ApplyFormulaCalculationValueToCell(cell, calculation.Value);
                                    }
                                    else if (preserve && pass == 0)
                                    {
                                        result.PreservedFormulaCount = result.PreservedFormulaCount + 1;
                                    }
                                    if (!calculation.IsValid && !options.ContinueAfterError)
                                    {
                                        result.CalculatedFormulaCount = formulaCount - result.PreservedFormulaCount;
                                        return result;
                                    }
                                }
                            }
                        }
                    }
                }
                sheetIndex = sheetIndex + 1;
            }
            pass = pass + 1;
        }

        result.CalculatedFormulaCount = formulaCount - result.PreservedFormulaCount;
        return result;
    }

    internal static XlsxFormulaAnalysisResult AnalyzeFormulaCore(string formula)
    {
        XlsxFormulaAnalysisResult result = new XlsxFormulaAnalysisResult();
        result.Formula = XlsxCellValueConverter.NormalizeText(formula);
        XlsxFormulaParseResult parsed = new XlsxFormulaParser().Parse(formula);
        foreach (XlsxDiagnostic diagnostic in parsed.Validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
        if (parsed.HasRoot && parsed.Root != null)
        {
            AnalyzeNode(result, parsed.Root);
        }
        return result;
    }

    internal static void AnalyzeNode(XlsxFormulaAnalysisResult result, XlsxFormulaNode node)
    {
        if (node.Kind == XlsxFormulaNodeKind.FunctionCall)
        {
            string originalName = XlsxCellValueConverter.NormalizeText(node.Operator).ToUpperInvariant();
            string normalizedName = NormalizeFutureFunctionName(originalName);
            AddUnique(result.Functions, originalName);
            if (normalizedName != originalName)
            {
                AddUnique(result.FutureFunctions, originalName);
                result.HasFutureSyntax = true;
            }
            if (IsVolatileFunction(normalizedName))
            {
                AddUnique(result.VolatileFunctions, originalName);
                result.HasVolatileFunction = true;
            }
            if (IsUnsafeFunction(normalizedName))
            {
                AddUnique(result.UnsafeFunctions, originalName);
                result.HasUnsafeFunction = true;
            }
            if (!IsSupportedFunction(normalizedName))
            {
                AddUnique(result.UnsupportedFunctions, originalName);
            }
        }
        else if (node.Kind == XlsxFormulaNodeKind.ExternalReference || IsExternalReferenceText(node.Text))
        {
            AddUnique(result.ExternalReferences, node.Text);
            result.HasExternalReference = true;
        }
        else if (node.Kind == XlsxFormulaNodeKind.Reference && node.Text.IndexOf('!') >= 0)
        {
            AddUnique(result.CrossSheetReferences, node.Text);
        }

        foreach (XlsxFormulaNode child in node.Children)
        {
            AnalyzeNode(result, child);
        }
    }

    internal static string NormalizeFutureFunctionName(string name)
    {
        string normalized = XlsxCellValueConverter.NormalizeText(name).ToUpperInvariant();
        if (normalized.StartsWith("_XLFN.", StringComparison.Ordinal) || normalized.StartsWith("_XLWS.", StringComparison.Ordinal) || normalized.StartsWith("_XLPM.", StringComparison.Ordinal))
        {
            return normalized.Substring(normalized.IndexOf('.') + 1);
        }
        return normalized;
    }

    internal static bool IsSupportedFunction(string name)
    {
        return name == "SUM" || name == "AVERAGE" || name == "COUNT" || name == "MIN" || name == "MAX" ||
            name == "MEDIAN" || name == "STDEV.S" || name == "VAR.S" || name == "PERCENTILE.INC" || name == "LARGE" || name == "SMALL" ||
            name == "PMT" || name == "FV" || name == "PV" || name == "NPV" || name == "LET" || name == "LAMBDA" || name == "IF" || name == "IFERROR" ||
            name == "AND" || name == "OR" || name == "NOT" || name == "LEN" || name == "LEFT" || name == "RIGHT" || name == "MID" || name == "UPPER" ||
            name == "LOWER" || name == "TRIM" || name == "CONCAT" || name == "TEXTJOIN" || name == "DATE" || name == "YEAR" || name == "MONTH" ||
            name == "DAY" || name == "TIME" || name == "ABS" || name == "ROUND" || name == "INT" || name == "SQRT" || name == "POWER" || name == "MOD" ||
            name == "INDEX" || name == "MATCH" || name == "VLOOKUP" || name == "XLOOKUP" || name == "SEQUENCE" || name == "FILTER" || name == "TRANSPOSE";
    }

    internal static bool IsVolatileFunction(string name)
    {
        return name == "NOW" || name == "TODAY" || name == "RAND" || name == "RANDBETWEEN" || name == "OFFSET" || name == "INDIRECT" || name == "CELL" || name == "INFO";
    }

    internal static bool IsUnsafeFunction(string name)
    {
        return name == "WEBSERVICE" || name == "FILTERXML" || name == "RTD" || name == "DDE" || name == "CALL" || name == "REGISTER.ID" || name == "HYPERLINK";
    }

    internal static bool IsExternalReferenceText(string text)
    {
        string normalized = XlsxCellValueConverter.NormalizeText(text);
        int open = normalized.IndexOf('[');
        int close = normalized.IndexOf(']');
        return open >= 0 && close > open;
    }

    internal static void AddWorksheetNodes(XlsxWorkbookFormulaDependencyGraphResult result, Dictionary<string, XlsxFormulaDependencyNode> nodes, XlsxWorksheetPart worksheet, string sheetName)
    {
        if (worksheet == null || worksheet.Rows == null)
        {
            result.AddError("formulaDependency.worksheetMissing", "Workbook worksheet data must not be null.");
            return;
        }
        foreach (XlsxWorksheetRow row in worksheet.Rows)
        {
            if (row != null && row.Cells != null)
            {
                foreach (XlsxWorksheetCell cell in row.Cells)
                {
                    if (cell != null && cell.Reference != null && cell.Reference.Length > 0 && XlsxWorksheetPart.IsCalculableFormulaCell(cell))
                    {
                        string qualified = QualifyReference(sheetName, cell.Reference);
                        XlsxFormulaDependencyNode node = GetOrCreateNode(nodes, sheetName, cell.Reference, qualified);
                        node.Formula = XlsxWorksheetPart.GetFormulaText(cell);
                        XlsxFormulaParseResult parsed = new XlsxFormulaParser().Parse(node.Formula);
                        foreach (XlsxDiagnostic diagnostic in parsed.Validation.Diagnostics)
                        {
                            result.AddDiagnostic(diagnostic);
                        }
                        if (parsed.HasRoot && parsed.Root != null)
                        {
                            AddNodeDependencies(nodes, node, sheetName, parsed.Root);
                        }
                    }
                }
            }
        }
    }

    internal static void AddNodeDependencies(Dictionary<string, XlsxFormulaDependencyNode> nodes, XlsxFormulaDependencyNode dependencyNode, string currentSheet, XlsxFormulaNode formulaNode)
    {
        if (formulaNode.Kind == XlsxFormulaNodeKind.Range)
        {
            AddRangeDependencies(nodes, dependencyNode, currentSheet, formulaNode);
            return;
        }
        if (formulaNode.Kind == XlsxFormulaNodeKind.Reference)
        {
            AddReferenceDependency(nodes, dependencyNode, currentSheet, formulaNode.Text);
        }
        else if (formulaNode.Kind == XlsxFormulaNodeKind.ExternalReference)
        {
            AddUnique(dependencyNode.ExternalPrecedents, formulaNode.Text);
        }
        else if (formulaNode.Kind == XlsxFormulaNodeKind.Name || formulaNode.Kind == XlsxFormulaNodeKind.StructuredReference)
        {
            AddUnique(dependencyNode.Names, formulaNode.Text);
        }
        foreach (XlsxFormulaNode child in formulaNode.Children)
        {
            AddNodeDependencies(nodes, dependencyNode, currentSheet, child);
        }
    }

    internal static void AddReferenceDependency(Dictionary<string, XlsxFormulaDependencyNode> nodes, XlsxFormulaDependencyNode dependencyNode, string currentSheet, string text)
    {
        XlsxReferenceResolutionResult parsed = XlsxWorkbookReferenceService.ParseA1Core(text);
        if (parsed.IsExternal)
        {
            AddUnique(dependencyNode.ExternalPrecedents, text);
            return;
        }
        if (!parsed.IsValid || !parsed.HasA1Range)
        {
            AddUnique(dependencyNode.Names, text);
            return;
        }
        string sheetName = parsed.HasSheetName ? parsed.FirstSheetName : currentSheet;
        string cellReference = XlsxA1CellReferenceConverter.FormatString(parsed.A1Range.First.Row, parsed.A1Range.First.Column, false, false);
        string qualified = QualifyReference(sheetName, cellReference);
        AddUnique(dependencyNode.Precedents, qualified);
        GetOrCreateNode(nodes, sheetName, cellReference, qualified);
    }

    internal static void AddRangeDependencies(Dictionary<string, XlsxFormulaDependencyNode> nodes, XlsxFormulaDependencyNode dependencyNode, string currentSheet, XlsxFormulaNode rangeNode)
    {
        XlsxReferenceResolutionResult parsed = XlsxWorkbookReferenceService.ParseA1Core(rangeNode.Text);
        if (!parsed.IsValid || !parsed.HasA1Range)
        {
            if (rangeNode.Children.Count >= 2)
            {
                string combined = rangeNode.Children[0].Text + ":" + XlsxFormulaCalculationEngine.GetFormulaReferenceTarget(rangeNode.Children[1].Text);
                parsed = XlsxWorkbookReferenceService.ParseA1Core(combined);
            }
        }
        if (parsed.IsExternal)
        {
            AddUnique(dependencyNode.ExternalPrecedents, rangeNode.Text);
            return;
        }
        if (!parsed.IsValid || !parsed.HasA1Range)
        {
            AddUnique(dependencyNode.Names, rangeNode.Text);
            return;
        }
        string sheetName = parsed.HasSheetName ? parsed.FirstSheetName : currentSheet;
        int minRow = Math.Min(parsed.A1Range.First.Row, parsed.A1Range.Last.Row);
        int maxRow = Math.Max(parsed.A1Range.First.Row, parsed.A1Range.Last.Row);
        int minColumn = Math.Min(parsed.A1Range.First.Column, parsed.A1Range.Last.Column);
        int maxColumn = Math.Max(parsed.A1Range.First.Column, parsed.A1Range.Last.Column);
        int count = (maxRow - minRow + 1) * (maxColumn - minColumn + 1);
        if (count > 10000)
        {
            AddUnique(dependencyNode.ExternalPrecedents, rangeNode.Text);
            return;
        }
        int row = minRow;
        while (row <= maxRow)
        {
            int column = minColumn;
            while (column <= maxColumn)
            {
                string cellReference = XlsxA1CellReferenceConverter.FormatString(row, column, false, false);
                string qualified = QualifyReference(sheetName, cellReference);
                AddUnique(dependencyNode.Precedents, qualified);
                GetOrCreateNode(nodes, sheetName, cellReference, qualified);
                column = column + 1;
            }
            row = row + 1;
        }
    }

    internal static void AddWorkbookDependentLinks(Dictionary<string, XlsxFormulaDependencyNode> nodes)
    {
        foreach (KeyValuePair<string, XlsxFormulaDependencyNode> item in nodes)
        {
            foreach (string precedent in item.Value.Precedents)
            {
                if (nodes.ContainsKey(precedent))
                {
                    AddUnique(nodes[precedent].Dependents, item.Value.QualifiedReference);
                }
            }
        }
    }

    internal static XlsxFormulaDependencyNode GetOrCreateNode(Dictionary<string, XlsxFormulaDependencyNode> nodes, string sheetName, string cellReference, string qualified)
    {
        if (nodes.ContainsKey(qualified))
        {
            return nodes[qualified];
        }
        XlsxFormulaDependencyNode node = new XlsxFormulaDependencyNode();
        node.SheetName = sheetName;
        node.CellReference = cellReference;
        node.QualifiedReference = qualified;
        nodes.Add(qualified, node);
        return node;
    }

    internal static XlsxFormulaDependencyNode? FindNode(List<XlsxFormulaDependencyNode> nodes, string qualifiedReference)
    {
        foreach (XlsxFormulaDependencyNode node in nodes)
        {
            if (string.Equals(node.QualifiedReference, qualifiedReference, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }
        }
        return null;
    }

    internal static string GetSheetName(XlsxWorkbook workbook, int index)
    {
        if (workbook != null && workbook.WorkbookPart != null && workbook.WorkbookPart.Sheets != null && index >= 0 && index < workbook.WorkbookPart.Sheets.Count)
        {
            string name = XlsxCellValueConverter.NormalizeText(workbook.WorkbookPart.Sheets[index].Name);
            if (name.Length > 0)
            {
                return name;
            }
        }
        return "Sheet" + (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string QualifyReference(string sheetName, string cellReference)
    {
        string escaped = XlsxCellValueConverter.NormalizeText(sheetName).Replace("'", "''", StringComparison.Ordinal);
        bool quote = escaped.IndexOf(' ') >= 0 || escaped.IndexOf('!') >= 0 || escaped.IndexOf('\'') >= 0;
        if (quote)
        {
            escaped = "'" + escaped + "'";
        }
        return escaped + "!" + XlsxCellValueConverter.NormalizeText(cellReference).Replace("$", "", StringComparison.Ordinal).ToUpperInvariant();
    }

    internal static string NormalizeQualifiedReference(string reference)
    {
        XlsxReferenceResolutionResult parsed = XlsxWorkbookReferenceService.ParseA1Core(reference);
        if (parsed.IsValid && parsed.HasA1Range && parsed.HasSheetName)
        {
            string cellReference = XlsxA1CellReferenceConverter.FormatString(parsed.A1Range.First.Row, parsed.A1Range.First.Column, false, false);
            return QualifyReference(parsed.FirstSheetName, cellReference);
        }
        return XlsxCellValueConverter.NormalizeText(reference);
    }

    internal static void AddUnique(List<string> values, string value)
    {
        if (value == null || value.Length == 0 || ContainsText(values, value))
        {
            return;
        }
        values.Add(value);
    }

    internal static bool ContainsText(List<string> values, string value)
    {
        foreach (string item in values)
        {
            if (string.Equals(item, value, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    internal static void MergeCalculationReport(XlsxFormulaCalculationResult target, XlsxFormulaCalculationResult source)
    {
        foreach (XlsxDiagnostic diagnostic in source.Validation.Diagnostics)
        {
            target.AddDiagnostic(diagnostic);
        }
        foreach (string item in source.UnsupportedFunctions)
        {
            AddUnique(target.UnsupportedFunctions, item);
        }
        foreach (string item in source.FutureFunctions)
        {
            AddUnique(target.FutureFunctions, item);
        }
        foreach (string item in source.VolatileFunctions)
        {
            AddUnique(target.VolatileFunctions, item);
        }
        foreach (string item in source.UnsafeFunctions)
        {
            AddUnique(target.UnsafeFunctions, item);
        }
        foreach (string item in source.ExternalReferences)
        {
            AddUnique(target.ExternalReferences, item);
        }
        target.Formula = source.Formula;
        target.Value = source.Value;
    }
}
