using System;
using System.Collections.Generic;
using System.Globalization;

namespace xlsx;

/// <summary>
/// Provides workbook-level projection, materialization, template, inventory, and dependency queries.
/// </summary>
public class XlsxWorkbookDataService : IXlsxWorkbookDataService
{
    /// <summary>Initializes a workbook data service.</summary>
    public XlsxWorkbookDataService()
    {
    }

    /// <summary>Projects a workbook range to typed row arrays.</summary>
    /// <param name="workbook">The workbook to inspect.</param><param name="sheetName">The worksheet name.</param><param name="reference">The A1 range to project.</param>
    /// <returns>The materialized range.</returns>
    public XlsxWorkbookRangeMaterialization ProjectRangeRows(XlsxWorkbook workbook, string sheetName, string reference)
    {
        XlsxWorkbookRangeMaterialization result = new XlsxWorkbookRangeMaterialization();
        if (workbook == null || workbook.Worksheets == null)
        {
            result.AddError("workbookData.workbookMissing", "Workbook worksheet data is required.");
            return result;
        }
        string normalizedSheetName = XlsxCellValueConverter.NormalizeText(sheetName);
        XlsxWorksheetPart? worksheet = FindDataWorksheet(workbook, normalizedSheetName);
        if (worksheet == null)
        {
            result.AddError("workbookData.sheetNotFound", "The requested worksheet was not found.");
            return result;
        }
        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(reference);
        foreach (XlsxDiagnostic diagnostic in range.Validation.Diagnostics) { result.AddDiagnostic(diagnostic); }
        if (!result.IsValid) { return result; }
        XlsxWorksheetPart.NormalizeRangeBounds(range);
        result.SheetName = normalizedSheetName;
        result.Reference = FormatMaterializedRange(range.First.Row, range.First.Column, range.Last.Row, range.Last.Column);
        result.ColumnCount = range.Last.Column - range.First.Column + 1;
        int row = range.First.Row;
        while (row <= range.Last.Row)
        {
            XlsxCellValue[] values = new XlsxCellValue[result.ColumnCount];
            int offset = 0;
            while (offset < result.ColumnCount)
            {
                values[offset] = ReadWorkbookCellValue(workbook, worksheet, row, range.First.Column + offset);
                offset = offset + 1;
            }
            result.Rows.Add(values);
            row = row + 1;
        }
        return result;
    }

    /// <summary>Detects whether the first populated row of a range is likely a header row.</summary>
    /// <param name="workbook">The workbook to inspect.</param><param name="sheetName">The worksheet name.</param><param name="reference">The A1 range to inspect.</param>
    /// <returns>The header detection result.</returns>
    public XlsxHeaderRowDetectionResult DetectHeaderRow(XlsxWorkbook workbook, string sheetName, string reference)
    {
        XlsxHeaderRowDetectionResult result = new XlsxHeaderRowDetectionResult();
        XlsxWorkbookRangeMaterialization materialized = ProjectRangeRows(workbook, sheetName, reference);
        foreach (XlsxDiagnostic diagnostic in materialized.Validation.Diagnostics) { result.AddDiagnostic(diagnostic); }
        result.SheetName = materialized.SheetName;
        result.Reference = materialized.Reference;
        if (!result.IsValid) { return result; }
        int candidateIndex = FindFirstPopulatedRow(materialized.Rows, 0);
        if (candidateIndex < 0) { return result; }
        int nextIndex = FindFirstPopulatedRow(materialized.Rows, candidateIndex + 1);
        XlsxCellValue[] candidate = materialized.Rows[candidateIndex];
        int populated = 0;
        int text = 0;
        bool unique = true;
        List<string> labels = new List<string>();
        foreach (XlsxCellValue value in candidate)
        {
            string label = GetCellDisplayText(value);
            result.Headers.Add(label);
            if (!IsBlankCellValue(value))
            {
                populated = populated + 1;
                if (IsTextCellValue(value)) { text = text + 1; }
                if (ContainsTextValue(labels, label)) { unique = false; }
                labels.Add(label);
            }
        }
        double confidence = 0;
        if (populated > 0 && text == populated) { confidence = confidence + 0.5; }
        if (populated > 1 && unique) { confidence = confidence + 0.25; }
        if (nextIndex >= 0 && RowContainsNonText(materialized.Rows[nextIndex])) { confidence = confidence + 0.25; }
        result.Confidence = confidence;
        result.HasHeaderRow = nextIndex >= 0 && confidence >= 0.75;
        if (result.HasHeaderRow)
        {
            XlsxA1RangeReferenceResult parsed = XlsxA1RangeReferenceConverter.Parse(materialized.Reference);
            if (parsed.IsValid) { result.HeaderRow = parsed.First.Row + candidateIndex; }
        }
        return result;
    }

    /// <summary>Materializes sheet-qualified selected ranges in request order.</summary>
    /// <param name="workbook">The workbook to inspect.</param><param name="selectedRanges">The sheet-qualified or first-sheet-relative A1 ranges.</param>
    /// <returns>The selected range materializations.</returns>
    public XlsxSelectedRangesMaterializationResult MaterializeSelectedRanges(XlsxWorkbook workbook, List<string> selectedRanges)
    {
        XlsxSelectedRangesMaterializationResult result = new XlsxSelectedRangesMaterializationResult();
        if (workbook == null || workbook.Worksheets == null || workbook.Worksheets.Count == 0)
        {
            result.AddError("workbookData.workbookMissing", "Workbook worksheet data is required.");
            return result;
        }
        if (selectedRanges == null || selectedRanges.Count == 0)
        {
            result.AddError("workbookData.selectedRangesMissing", "At least one selected range is required.");
            return result;
        }
        foreach (string selectedRange in selectedRanges)
        {
            XlsxReferenceResolutionResult parsed = XlsxWorkbookReferenceService.ParseA1Core(selectedRange);
            if (!parsed.IsValid || !parsed.HasA1Range || parsed.IsExternal || parsed.IsThreeDimensional)
            {
                foreach (XlsxDiagnostic diagnostic in parsed.Validation.Diagnostics) { result.AddDiagnostic(diagnostic); }
                if (parsed.IsExternal || parsed.IsThreeDimensional) { result.AddError("workbookData.selectedRangeUnsupported", "Selected ranges must target one worksheet in the current workbook."); }
                continue;
            }
            string selectedSheet = parsed.HasSheetName ? parsed.FirstSheetName : XlsxFormulaService.GetSheetName(workbook, 0);
            XlsxWorkbookRangeMaterialization materialized = ProjectRangeRows(workbook, selectedSheet, parsed.A1Range.Text);
            result.Ranges.Add(materialized);
            foreach (XlsxDiagnostic diagnostic in materialized.Validation.Diagnostics) { result.AddDiagnostic(diagnostic); }
        }
        return result;
    }

    /// <summary>Replaces literal template tokens in text cells and optionally formulas.</summary>
    /// <param name="workbook">The workbook to update.</param><param name="replacements">The literal token replacements.</param><param name="includeFormulas">Whether formula text is also updated.</param>
    /// <returns>The replacement counts and diagnostics.</returns>
    public XlsxTemplateReplacementResult ReplaceTemplateTokens(XlsxWorkbook workbook, List<XlsxTemplateReplacement> replacements, bool includeFormulas)
    {
        XlsxTemplateReplacementResult result = new XlsxTemplateReplacementResult();
        if (workbook == null || workbook.Worksheets == null)
        {
            result.AddError("templateReplacement.workbookMissing", "Workbook worksheet data is required.");
            return result;
        }
        ValidateTemplateReplacements(result, replacements);
        if (!result.IsValid) { return result; }
        foreach (XlsxWorksheetPart worksheet in workbook.Worksheets)
        {
            if (worksheet == null || worksheet.Rows == null) { continue; }
            foreach (XlsxWorksheetRow row in worksheet.Rows)
            {
                if (row == null || row.Cells == null) { continue; }
                foreach (XlsxWorksheetCell cell in row.Cells)
                {
                    if (cell == null) { continue; }
                    if (includeFormulas && (cell.HasFormula || XlsxCellValueConverter.NormalizeText(cell.Formula).Length > 0))
                    {
                        ReplaceFormulaTokens(cell, replacements, result);
                    }
                    else
                    {
                        ReplaceCellTokens(workbook, cell, replacements, result);
                    }
                }
            }
        }
        return result;
    }

    /// <summary>Inventories external links, connections, query tables, relationships, formula references, and hyperlinks.</summary>
    /// <param name="workbook">The workbook to inspect.</param>
    /// <returns>The external-content inventory.</returns>
    public XlsxExternalContentInventoryResult InventoryExternalContent(XlsxWorkbook workbook)
    {
        XlsxExternalContentInventoryResult result = new XlsxExternalContentInventoryResult();
        if (workbook == null)
        {
            result.AddError("externalContent.workbookMissing", "A workbook is required for external-content inventory.");
            return result;
        }
        AddExternalLinks(workbook, result);
        AddWorkbookExternalReferences(workbook, result);
        AddConnections(workbook, result);
        AddQueryTables(workbook, result);
        AddExternalRelationships(workbook.PackageRelationships, result);
        AddExternalRelationships(workbook.WorkbookRelationships, result);
        if (workbook.RelationshipParts != null)
        {
            foreach (XlsxRelationshipsPart relationships in workbook.RelationshipParts) { AddExternalRelationships(relationships, result); }
        }
        AddExternalHyperlinks(workbook, result);
        AddExternalFormulaReferences(workbook, result);
        return result;
    }

    /// <summary>Queries direct formula dependencies for one sheet-qualified cell.</summary>
    /// <param name="workbook">The workbook to inspect.</param><param name="qualifiedReference">The sheet-qualified A1 cell reference.</param>
    /// <returns>The dependency details.</returns>
    public XlsxFormulaDependencyQueryResult QueryFormulaDependencies(XlsxWorkbook workbook, string qualifiedReference)
    {
        XlsxFormulaDependencyQueryResult result = new XlsxFormulaDependencyQueryResult();
        XlsxReferenceResolutionResult parsed = XlsxWorkbookReferenceService.ParseA1Core(qualifiedReference);
        foreach (XlsxDiagnostic diagnostic in parsed.Validation.Diagnostics) { result.AddDiagnostic(diagnostic); }
        bool multipleCells = parsed.HasA1Range && (parsed.A1Range.First.Row != parsed.A1Range.Last.Row || parsed.A1Range.First.Column != parsed.A1Range.Last.Column);
        if (!parsed.HasSheetName || !parsed.HasA1Range || multipleCells)
        {
            result.AddError("formulaDependency.referenceInvalid", "Dependency queries require one sheet-qualified cell reference.");
            return result;
        }
        result.QualifiedReference = XlsxFormulaService.NormalizeQualifiedReference(qualifiedReference);
        XlsxWorkbookFormulaDependencyGraphResult graph = new XlsxFormulaService().BuildWorkbookDependencyGraph(workbook);
        foreach (XlsxDiagnostic diagnostic in graph.Validation.Diagnostics) { result.AddDiagnostic(diagnostic); }
        if (!result.IsValid) { return result; }
        XlsxFormulaDependencyNode? node = XlsxFormulaService.FindNode(graph.Nodes, result.QualifiedReference);
        if (node == null)
        {
            result.AddError("formulaDependency.cellNotFound", "The dependency graph does not contain the requested cell.");
            return result;
        }
        result.HasNode = true;
        result.Formula = node.Formula;
        CopyTextValues(node.Precedents, result.Precedents);
        CopyTextValues(node.Dependents, result.Dependents);
        CopyTextValues(node.ExternalPrecedents, result.ExternalPrecedents);
        CopyTextValues(node.Names, result.Names);
        return result;
    }

    /// <summary>Queries direct and transitive formula impacts from changed cells.</summary>
    /// <param name="workbook">The workbook to inspect.</param><param name="changedReferences">The sheet-qualified changed cells.</param>
    /// <returns>The impacted cells.</returns>
    public XlsxImpactedCellsResult QueryImpactedCells(XlsxWorkbook workbook, List<string> changedReferences)
    {
        XlsxFormulaService service = new XlsxFormulaService();
        XlsxWorkbookFormulaDependencyGraphResult graph = service.BuildWorkbookDependencyGraph(workbook);
        if (!graph.IsValid)
        {
            XlsxImpactedCellsResult invalid = new XlsxImpactedCellsResult();
            foreach (XlsxDiagnostic diagnostic in graph.Validation.Diagnostics) { invalid.Validation.AddDiagnostic(diagnostic); }
            invalid.IsValid = invalid.Validation.IsValid;
            return invalid;
        }
        return service.FindImpactedCells(graph, changedReferences);
    }

    internal static XlsxCellValue ReadWorkbookCellValue(XlsxWorkbook workbook, XlsxWorksheetPart worksheet, int row, int column)
    {
        XlsxCellValue value = XlsxRangeService.CreateCellItem(worksheet, row, column).Value;
        if (value.Kind == XlsxCellValueKind.SharedString && workbook.SharedStrings != null && value.SharedStringIndex < (uint)workbook.SharedStrings.Items.Count)
        {
            return XlsxCellValue.CreateText(workbook.SharedStrings.GetText((int)value.SharedStringIndex));
        }
        if (value.Kind == XlsxCellValueKind.FormulaCachedValue && value.HasTypedCachedValue && value.TypedCachedValue != null && value.TypedCachedValue.Kind == XlsxCellValueKind.SharedString && workbook.SharedStrings != null && value.TypedCachedValue.SharedStringIndex < (uint)workbook.SharedStrings.Items.Count)
        {
            value.TypedCachedValue = XlsxCellValue.CreateText(workbook.SharedStrings.GetText((int)value.TypedCachedValue.SharedStringIndex));
        }
        return value;
    }

    internal static XlsxWorksheetPart? FindDataWorksheet(XlsxWorkbook workbook, string sheetName)
    {
        if (workbook == null || workbook.Worksheets == null) { return null; }
        int index = 0;
        while (index < workbook.Worksheets.Count)
        {
            if (string.Equals(XlsxFormulaService.GetSheetName(workbook, index), sheetName, StringComparison.OrdinalIgnoreCase)) { return workbook.Worksheets[index]; }
            index = index + 1;
        }
        return null;
    }

    internal static string FormatMaterializedRange(int firstRow, int firstColumn, int lastRow, int lastColumn)
    {
        string first = XlsxA1CellReferenceConverter.FormatString(firstRow, firstColumn, false, false);
        string last = XlsxA1CellReferenceConverter.FormatString(lastRow, lastColumn, false, false);
        return first == last ? first : first + ":" + last;
    }

    internal static int FindFirstPopulatedRow(List<XlsxCellValue[]> rows, int startIndex)
    {
        int index = startIndex;
        while (index < rows.Count)
        {
            foreach (XlsxCellValue value in rows[index]) { if (!IsBlankCellValue(value)) { return index; } }
            index = index + 1;
        }
        return -1;
    }

    internal static bool IsBlankCellValue(XlsxCellValue value)
    {
        if (value == null || value.Kind == XlsxCellValueKind.Blank) { return true; }
        return IsTextCellValue(value) && XlsxCellValueConverter.NormalizeText(value.Text).Length == 0;
    }

    internal static bool IsTextCellValue(XlsxCellValue value)
    {
        return value != null && (value.Kind == XlsxCellValueKind.Text || value.Kind == XlsxCellValueKind.InlineString || value.Kind == XlsxCellValueKind.RichText);
    }

    internal static bool RowContainsNonText(XlsxCellValue[] row)
    {
        foreach (XlsxCellValue value in row) { if (!IsBlankCellValue(value) && !IsTextCellValue(value)) { return true; } }
        return false;
    }

    internal static string GetCellDisplayText(XlsxCellValue value)
    {
        if (value == null) { return ""; }
        if (value.Kind == XlsxCellValueKind.Text || value.Kind == XlsxCellValueKind.InlineString || value.Kind == XlsxCellValueKind.RichText) { return XlsxCellValueConverter.NormalizeText(value.Text); }
        if (value.Kind == XlsxCellValueKind.Number) { return value.HasExactText ? value.ExactText : value.Number.ToString(CultureInfo.InvariantCulture); }
        if (value.Kind == XlsxCellValueKind.Decimal) { return value.HasExactText ? value.ExactText : value.Decimal.ToString(CultureInfo.InvariantCulture); }
        if (value.Kind == XlsxCellValueKind.Boolean) { return value.Boolean ? "TRUE" : "FALSE"; }
        if (value.Kind == XlsxCellValueKind.DateTime) { return value.DateTime.ToString("O", CultureInfo.InvariantCulture); }
        if (value.Kind == XlsxCellValueKind.Duration) { return value.Duration.ToString(); }
        if (value.Kind == XlsxCellValueKind.Error) { return value.Text; }
        if (value.Kind == XlsxCellValueKind.FormulaCachedValue && value.TypedCachedValue != null) { return GetCellDisplayText(value.TypedCachedValue); }
        return XlsxCellValueConverter.NormalizeText(value.RawValue);
    }

    internal static bool ContainsTextValue(List<string> values, string value)
    {
        foreach (string item in values) { if (string.Equals(item, value, StringComparison.OrdinalIgnoreCase)) { return true; } }
        return false;
    }

    internal static void ValidateTemplateReplacements(XlsxTemplateReplacementResult result, List<XlsxTemplateReplacement> replacements)
    {
        if (replacements == null || replacements.Count == 0) { result.AddError("templateReplacement.replacementsMissing", "At least one template replacement is required."); return; }
        List<string> tokens = new List<string>();
        foreach (XlsxTemplateReplacement replacement in replacements)
        {
            if (replacement == null || replacement.Token == null || replacement.Token.Length == 0) { result.AddError("templateReplacement.tokenMissing", "Template replacement tokens must not be empty."); continue; }
            if (ContainsTemplateToken(tokens, replacement.Token)) { result.AddError("templateReplacement.tokenDuplicate", "Template replacement tokens must be unique."); continue; }
            tokens.Add(replacement.Token);
        }
    }

    internal static void ReplaceFormulaTokens(XlsxWorksheetCell cell, List<XlsxTemplateReplacement> replacements, XlsxTemplateReplacementResult result)
    {
        string original = XlsxWorksheetPart.GetFormulaText(cell);
        int count = 0;
        string replaced = ApplyTextReplacements(original, replacements);
        count = CountReplacementOccurrences(original, replacements);
        if (count == 0) { return; }
        cell.Formula = replaced;
        if (cell.FormulaInfo != null) { cell.FormulaInfo.Text = replaced; }
        result.ReplacedFormulaCount = result.ReplacedFormulaCount + 1;
        result.ReplacementCount = result.ReplacementCount + count;
    }

    internal static void ReplaceCellTokens(XlsxWorkbook workbook, XlsxWorksheetCell cell, List<XlsxTemplateReplacement> replacements, XlsxTemplateReplacementResult result)
    {
        XlsxCellValueResult converted = XlsxCellValueConverter.FromWorksheetCell(cell);
        if (!converted.IsValid || converted.Value == null || converted.Value.Kind == XlsxCellValueKind.RichText) { return; }
        XlsxCellValue value = converted.Value;
        string original = "";
        if (value.Kind == XlsxCellValueKind.SharedString && workbook.SharedStrings != null && value.SharedStringIndex < (uint)workbook.SharedStrings.Items.Count)
        {
            original = workbook.SharedStrings.GetText((int)value.SharedStringIndex);
        }
        else if (value.Kind == XlsxCellValueKind.Text || value.Kind == XlsxCellValueKind.InlineString)
        {
            original = value.Text;
        }
        else
        {
            return;
        }
        int count = CountReplacementOccurrences(original, replacements);
        if (count == 0) { return; }
        string replaced = ApplyTextReplacements(original, replacements);
        XlsxCellValueConverter.ApplyToWorksheetCell(XlsxCellValue.CreateText(replaced), cell);
        result.ReplacedCellCount = result.ReplacedCellCount + 1;
        result.ReplacementCount = result.ReplacementCount + count;
    }

    internal static string ApplyTextReplacements(string text, List<XlsxTemplateReplacement> replacements)
    {
        string result = XlsxCellValueConverter.NormalizeText(text);
        foreach (XlsxTemplateReplacement replacement in replacements) { result = result.Replace(replacement.Token, XlsxCellValueConverter.NormalizeText(replacement.Replacement), StringComparison.Ordinal); }
        return result;
    }

    internal static int CountReplacementOccurrences(string text, List<XlsxTemplateReplacement> replacements)
    {
        int total = 0;
        string current = XlsxCellValueConverter.NormalizeText(text);
        foreach (XlsxTemplateReplacement replacement in replacements)
        {
            int start = 0;
            while (start <= current.Length - replacement.Token.Length)
            {
                int found = current.IndexOf(replacement.Token, start, StringComparison.Ordinal);
                if (found < 0) { break; }
                total = total + 1;
                start = found + replacement.Token.Length;
            }
            current = current.Replace(replacement.Token, XlsxCellValueConverter.NormalizeText(replacement.Replacement), StringComparison.Ordinal);
        }
        return total;
    }

    internal static bool ContainsTemplateToken(List<string> tokens, string token)
    {
        foreach (string existing in tokens) { if (existing == token) { return true; } }
        return false;
    }

    internal static void AddExternalLinks(XlsxWorkbook workbook, XlsxExternalContentInventoryResult result)
    {
        if (workbook.ExternalLinks == null) { return; }
        foreach (XlsxExternalLinkPart link in workbook.ExternalLinks)
        {
            if (link == null) { continue; }
            AddInventoryItem(result, XlsxExternalContentKind.ExternalLink, link.PartPath, "", XlsxCellValueConverter.NormalizeText(link.RelationshipId), XlsxCellValueConverter.NormalizeText(link.Target));
        }
    }

    internal static void AddConnections(XlsxWorkbook workbook, XlsxExternalContentInventoryResult result)
    {
        if (workbook.Connections == null || workbook.Connections.Connections == null) { return; }
        foreach (XlsxWorkbookConnectionInfo connection in workbook.Connections.Connections)
        {
            if (connection == null) { continue; }
            string identifier = connection.HasId ? connection.Id.ToString(CultureInfo.InvariantCulture) : "";
            AddInventoryItem(result, XlsxExternalContentKind.Connection, workbook.Connections.PartPath, connection.Name, identifier, connection.Name);
        }
    }

    internal static void AddWorkbookExternalReferences(XlsxWorkbook workbook, XlsxExternalContentInventoryResult result)
    {
        if (workbook.WorkbookPart == null || workbook.WorkbookPart.ExternalReferences == null) { return; }
        foreach (XlsxWorkbookExternalReference reference in workbook.WorkbookPart.ExternalReferences)
        {
            if (reference == null) { continue; }
            string target = "";
            if (workbook.WorkbookRelationships != null)
            {
                XlsxRelationship? relationship = workbook.WorkbookRelationships.GetRelationship(reference.RelationshipId);
                if (relationship != null) { target = relationship.TargetPath.Length > 0 ? relationship.TargetPath : XlsxCellValueConverter.NormalizeText(relationship.Target); }
            }
            AddInventoryItem(result, XlsxExternalContentKind.ExternalLink, XlsxPackagePaths.Workbook, "", reference.RelationshipId, target);
        }
    }

    internal static void AddQueryTables(XlsxWorkbook workbook, XlsxExternalContentInventoryResult result)
    {
        if (workbook.QueryTables == null) { return; }
        foreach (XlsxQueryTablePart query in workbook.QueryTables)
        {
            if (query == null) { continue; }
            string connection = query.HasConnectionId ? query.ConnectionId.ToString(CultureInfo.InvariantCulture) : "";
            AddInventoryItem(result, XlsxExternalContentKind.QueryTable, query.PartPath, query.Name, "", connection);
        }
    }

    internal static void AddExternalRelationships(XlsxRelationshipsPart relationships, XlsxExternalContentInventoryResult result)
    {
        if (relationships == null || relationships.Relationships == null) { return; }
        foreach (XlsxRelationship relationship in relationships.Relationships)
        {
            if (relationship == null) { continue; }
            bool external = relationship.IsExternal || relationship.TargetMode == "External" || relationship.Type == XlsxRelationshipTypes.ExternalLinkPath;
            if (!external) { continue; }
            string externalTarget = XlsxCellValueConverter.NormalizeText(relationship.ExternalTarget);
            string target = externalTarget.Length > 0 ? externalTarget : XlsxCellValueConverter.NormalizeText(relationship.Target);
            AddInventoryItem(result, XlsxExternalContentKind.ExternalRelationship, relationships.SourcePartPath, XlsxCellValueConverter.NormalizeText(relationship.Type), XlsxCellValueConverter.NormalizeText(relationship.Id), target);
        }
    }

    internal static void AddExternalHyperlinks(XlsxWorkbook workbook, XlsxExternalContentInventoryResult result)
    {
        if (workbook.Worksheets == null) { return; }
        foreach (XlsxWorksheetPart worksheet in workbook.Worksheets)
        {
            if (worksheet == null || worksheet.Hyperlinks == null) { continue; }
            foreach (XlsxWorksheetHyperlink hyperlink in worksheet.Hyperlinks)
            {
                if (hyperlink == null) { continue; }
                string externalTarget = XlsxCellValueConverter.NormalizeText(hyperlink.ExternalTarget);
                string ordinaryTarget = XlsxCellValueConverter.NormalizeText(hyperlink.Target);
                if (!hyperlink.IsExternal && hyperlink.TargetMode != "External" && !XlsxPackagePath.LooksLikeExternalUri(externalTarget) && !XlsxPackagePath.LooksLikeExternalUri(ordinaryTarget)) { continue; }
                string target = externalTarget.Length > 0 ? externalTarget : ordinaryTarget;
                AddInventoryItem(result, XlsxExternalContentKind.ExternalHyperlink, worksheet.PartPath, XlsxCellValueConverter.NormalizeText(hyperlink.Reference), XlsxCellValueConverter.NormalizeText(hyperlink.RelationshipId), target);
            }
        }
    }

    internal static void AddExternalFormulaReferences(XlsxWorkbook workbook, XlsxExternalContentInventoryResult result)
    {
        XlsxWorkbookFormulaDependencyGraphResult graph = new XlsxFormulaService().BuildWorkbookDependencyGraph(workbook);
        foreach (XlsxDiagnostic diagnostic in graph.Validation.Diagnostics) { result.AddDiagnostic(diagnostic); }
        if (!graph.IsValid) { return; }
        foreach (XlsxFormulaDependencyNode node in graph.Nodes)
        {
            foreach (string external in node.ExternalPrecedents)
            {
                AddInventoryItem(result, XlsxExternalContentKind.ExternalFormulaReference, "", node.QualifiedReference, "", external);
            }
        }
    }

    internal static void AddInventoryItem(XlsxExternalContentInventoryResult result, XlsxExternalContentKind kind, string sourcePartPath, string sourceReference, string relationshipId, string target)
    {
        foreach (XlsxExternalContentItem existing in result.Items)
        {
            if (existing.Kind == kind && existing.SourcePartPath == sourcePartPath && existing.SourceReference == sourceReference && existing.RelationshipId == relationshipId && existing.Target == target) { return; }
        }
        XlsxExternalContentItem item = new XlsxExternalContentItem();
        item.Kind = kind;
        item.SourcePartPath = XlsxCellValueConverter.NormalizeText(sourcePartPath);
        item.SourceReference = XlsxCellValueConverter.NormalizeText(sourceReference);
        item.RelationshipId = XlsxCellValueConverter.NormalizeText(relationshipId);
        item.Target = XlsxCellValueConverter.NormalizeText(target);
        result.Items.Add(item);
    }

    internal static void CopyTextValues(List<string> source, List<string> target)
    {
        foreach (string value in source) { target.Add(value); }
    }
}
