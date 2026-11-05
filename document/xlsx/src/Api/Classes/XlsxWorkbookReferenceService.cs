using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides workbook-aware parsing and resolution for cell references, defined names, print names, and table references.
/// </summary>
public class XlsxWorkbookReferenceService : IXlsxWorkbookReferenceService
{
    /// <summary>
    /// Initializes a workbook reference service.
    /// </summary>
    public XlsxWorkbookReferenceService()
    {
    }

    /// <summary>
    /// Parses a quoted, sheet-qualified, three-dimensional, or workbook-qualified A1 reference.
    /// </summary>
    /// <param name="text">The reference text to parse.</param>
    /// <returns>The parsed reference and validation diagnostics.</returns>
    public XlsxReferenceResolutionResult ParseA1Reference(string text)
    {
        return ParseA1Core(text);
    }

    /// <summary>
    /// Parses an optionally qualified R1C1 reference and resolves relative coordinates from a base cell.
    /// </summary>
    /// <param name="text">The reference text to parse.</param>
    /// <param name="baseRow">The one-based base row for relative coordinates.</param>
    /// <param name="baseColumn">The one-based base column for relative coordinates.</param>
    /// <returns>The parsed reference and resolved A1 coordinates.</returns>
    public XlsxReferenceResolutionResult ParseR1C1Reference(string text, int baseRow, int baseColumn)
    {
        XlsxReferenceResolutionResult result = new XlsxReferenceResolutionResult();
        result.OriginalText = NormalizeInput(text);
        if (!ValidateBaseCell(result, baseRow, baseColumn))
        {
            return result;
        }

        string body = ParseQualifier(result, result.OriginalText);
        if (!result.IsValid)
        {
            return result;
        }

        int separator = body.IndexOf(':');
        string firstText = body;
        string lastText = "";
        if (separator >= 0)
        {
            firstText = body.Substring(0, separator);
            lastText = body.Substring(separator + 1);
            if (lastText.IndexOf(':') >= 0)
            {
                result.AddError("reference.r1c1RangeInvalid", "R1C1 range must contain at most two endpoints.");
                return result;
            }
        }

        XlsxR1C1ReferenceResult first = XlsxR1C1ReferenceConverter.Parse(firstText);
        CopyDiagnostics(result, first.Validation);
        if (!result.IsValid)
        {
            return result;
        }

        result.FirstR1C1 = first.Reference;
        result.HasR1C1Reference = true;
        XlsxCellReferenceValue firstA1 = ResolveR1C1Endpoint(result, first.Reference, baseRow, baseColumn);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxCellReferenceValue lastA1 = firstA1;
        string normalizedBody = first.Text;
        if (separator >= 0)
        {
            XlsxR1C1ReferenceResult last = XlsxR1C1ReferenceConverter.Parse(lastText);
            CopyDiagnostics(result, last.Validation);
            if (!result.IsValid)
            {
                return result;
            }

            result.LastR1C1 = last.Reference;
            result.HasLastR1C1 = true;
            lastA1 = ResolveR1C1Endpoint(result, last.Reference, baseRow, baseColumn);
            normalizedBody = normalizedBody + ":" + last.Text;
        }
        else
        {
            result.LastR1C1 = first.Reference;
        }

        SetA1Range(result, firstA1, lastA1, separator >= 0);
        result.NormalizedText = BuildQualifier(result) + normalizedBody;
        return result;
    }

    /// <summary>
    /// Resolves a local or workbook-scoped defined name, with local scope taking precedence.
    /// </summary>
    /// <param name="workbook">The workbook containing the name.</param>
    /// <param name="name">The defined name, optionally qualified by a worksheet or workbook.</param>
    /// <param name="localSheetId">The zero-based local sheet scope used when the name is unqualified.</param>
    /// <returns>The resolved name and validation diagnostics.</returns>
    public XlsxReferenceResolutionResult ResolveDefinedName(XlsxWorkbook workbook, string name, int localSheetId)
    {
        return ResolveDefinedNameCore(workbook, name, localSheetId);
    }

    /// <summary>
    /// Resolves the built-in print-area name for a worksheet scope.
    /// </summary>
    /// <param name="workbook">The workbook containing the print name.</param>
    /// <param name="localSheetId">The zero-based worksheet scope.</param>
    /// <returns>The resolved print name and validation diagnostics.</returns>
    public XlsxReferenceResolutionResult ResolvePrintArea(XlsxWorkbook workbook, int localSheetId)
    {
        XlsxReferenceResolutionResult result = ResolveDefinedNameCore(workbook, "_xlnm.Print_Area", localSheetId);
        result.IsPrintArea = result.HasDefinedName;
        return result;
    }

    /// <summary>
    /// Resolves the built-in print-titles name for a worksheet scope.
    /// </summary>
    /// <param name="workbook">The workbook containing the print name.</param>
    /// <param name="localSheetId">The zero-based worksheet scope.</param>
    /// <returns>The resolved print name and validation diagnostics.</returns>
    public XlsxReferenceResolutionResult ResolvePrintTitles(XlsxWorkbook workbook, int localSheetId)
    {
        XlsxReferenceResolutionResult result = ResolveDefinedNameCore(workbook, "_xlnm.Print_Titles", localSheetId);
        result.IsPrintTitles = result.HasDefinedName;
        return result;
    }

    /// <summary>
    /// Resolves a table structured reference across the workbook.
    /// </summary>
    /// <param name="workbook">The workbook containing the table.</param>
    /// <param name="text">The structured reference text.</param>
    /// <returns>The resolved table and A1 range.</returns>
    public XlsxReferenceResolutionResult ResolveTableReference(XlsxWorkbook workbook, string text)
    {
        XlsxReferenceResolutionResult result = ParseStructuredReference(text);
        if (!result.IsValid)
        {
            return result;
        }
        if (workbook == null || workbook.Worksheets == null)
        {
            result.AddError("reference.workbookMissing", "Workbook and worksheet data are required to resolve a table reference.");
            return result;
        }
        if (result.IsExternal)
        {
            return result;
        }

        XlsxWorksheetPart? worksheet = null;
        string sheetName = result.FirstSheetName;
        if (result.HasSheetName)
        {
            worksheet = FindWorksheet(workbook, result.FirstSheetName);
            if (worksheet == null)
            {
                result.AddError("reference.sheetNotFound", "Structured reference worksheet was not found in the workbook.");
                return result;
            }
        }
        else
        {
            foreach (XlsxWorksheetPart candidate in workbook.Worksheets)
            {
                if (FindTable(candidate, result.TableName) != null)
                {
                    worksheet = candidate;
                    sheetName = FindWorksheetName(workbook, candidate);
                    break;
                }
            }
        }

        if (worksheet == null)
        {
            result.AddError("reference.tableNotFound", "Structured reference table was not found in the workbook.");
            return result;
        }

        ResolveStructuredReference(result, worksheet, sheetName);
        return result;
    }

    /// <summary>
    /// Detects whether a name or reference contains an external-workbook qualifier.
    /// </summary>
    /// <param name="text">The name or reference text to inspect.</param>
    /// <returns><c>true</c> when an external-workbook qualifier is present; otherwise, <c>false</c>.</returns>
    public bool IsExternalName(string text)
    {
        return HasExternalQualifier(text);
    }

    internal static XlsxReferenceResolutionResult ParseA1Core(string text)
    {
        XlsxReferenceResolutionResult result = new XlsxReferenceResolutionResult();
        result.OriginalText = NormalizeInput(text);
        string body = ParseQualifier(result, result.OriginalText);
        if (!result.IsValid)
        {
            return result;
        }

        XlsxA1RangeReferenceResult range = XlsxA1RangeReferenceConverter.Parse(body);
        CopyDiagnostics(result, range.Validation);
        if (result.IsValid)
        {
            result.A1Range = range.Range;
            result.HasA1Range = true;
            result.NormalizedText = BuildQualifier(result) + range.Text;
        }
        return result;
    }

    internal static XlsxReferenceResolutionResult ResolveDefinedNameCore(XlsxWorkbook workbook, string name, int localSheetId)
    {
        XlsxReferenceResolutionResult result = new XlsxReferenceResolutionResult();
        result.OriginalText = NormalizeInput(name);
        if (workbook == null || workbook.WorkbookPart == null || workbook.WorkbookPart.DefinedNames == null)
        {
            result.AddError("reference.workbookMissing", "Workbook defined-name data is required for name resolution.");
            return result;
        }
        if (localSheetId < 0)
        {
            result.AddError("reference.localSheetIdRange", "Defined-name local sheet id must not be negative.");
            return result;
        }

        string nameText = ParseQualifier(result, result.OriginalText);
        if (!result.HasWorkbookName && HasExternalQualifier(nameText))
        {
            string externalText = nameText;
            if (externalText.StartsWith("'", StringComparison.Ordinal))
            {
                externalText = externalText.Substring(1);
            }
            int close = externalText.IndexOf(']');
            result.WorkbookName = externalText.Substring(1, close - 1);
            result.HasWorkbookName = true;
            result.IsExternal = true;
            nameText = externalText.Substring(close + 1);
            if (nameText.EndsWith("'", StringComparison.Ordinal))
            {
                nameText = nameText.Substring(0, nameText.Length - 1);
            }
        }
        result.Name = nameText;
        if (!result.IsValid)
        {
            return result;
        }
        if (nameText.Length == 0)
        {
            result.AddError("reference.nameMissing", "Defined name must not be empty.");
            return result;
        }
        if (result.IsExternal)
        {
            result.NormalizedText = result.OriginalText;
            return result;
        }

        int scope = localSheetId;
        if (result.HasSheetName)
        {
            scope = FindSheetIndex(workbook, result.FirstSheetName);
            if (scope < 0)
            {
                result.AddError("reference.sheetNotFound", "Defined-name worksheet was not found in the workbook.");
                return result;
            }
        }

        XlsxWorkbookDefinedName? resolved = FindDefinedName(workbook.WorkbookPart.DefinedNames, nameText, scope, true);
        if (resolved == null)
        {
            resolved = FindDefinedName(workbook.WorkbookPart.DefinedNames, nameText, scope, false);
        }
        if (resolved == null)
        {
            result.AddError("reference.nameNotFound", "Defined name was not found in the requested workbook scope.");
            return result;
        }

        result.DefinedName = resolved;
        result.HasDefinedName = true;
        result.NormalizedText = resolved.Text;
        result.IsPrintArea = string.Equals(resolved.Name, "_xlnm.Print_Area", StringComparison.OrdinalIgnoreCase);
        result.IsPrintTitles = string.Equals(resolved.Name, "_xlnm.Print_Titles", StringComparison.OrdinalIgnoreCase);
        XlsxReferenceResolutionResult parsed = ParseA1Core(resolved.Text);
        if (parsed.IsValid)
        {
            result.A1Range = parsed.A1Range;
            result.HasA1Range = parsed.HasA1Range;
            result.WorkbookName = parsed.WorkbookName;
            result.HasWorkbookName = parsed.HasWorkbookName;
            result.FirstSheetName = parsed.FirstSheetName;
            result.LastSheetName = parsed.LastSheetName;
            result.HasSheetName = parsed.HasSheetName;
            result.IsThreeDimensional = parsed.IsThreeDimensional;
            result.IsExternal = parsed.IsExternal;
        }
        return result;
    }

    internal static XlsxWorkbookDefinedName? FindDefinedName(List<XlsxWorkbookDefinedName> names, string name, int localSheetId, bool local)
    {
        foreach (XlsxWorkbookDefinedName candidate in names)
        {
            if (candidate == null || !string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (local && candidate.HasLocalSheetId && candidate.LocalSheetId == localSheetId)
            {
                return candidate;
            }
            if (!local && !candidate.HasLocalSheetId)
            {
                return candidate;
            }
        }
        return null;
    }

    internal static string ParseQualifier(XlsxReferenceResolutionResult result, string text)
    {
        if (text == null || text.Length == 0)
        {
            result.AddError("reference.empty", "Reference text must not be empty.");
            return "";
        }

        int separator = FindQualifierSeparator(text);
        if (separator < 0)
        {
            return text;
        }
        if (separator == 0 || separator == text.Length - 1)
        {
            result.AddError("reference.qualifierInvalid", "Reference qualifier and target must not be empty.");
            return "";
        }

        string qualifier = text.Substring(0, separator);
        string body = text.Substring(separator + 1);
        if (qualifier.StartsWith("'", StringComparison.Ordinal))
        {
            if (!qualifier.EndsWith("'", StringComparison.Ordinal) || qualifier.Length < 2)
            {
                result.AddError("reference.quoteInvalid", "Quoted worksheet qualifier is not terminated.");
                return "";
            }
            qualifier = qualifier.Substring(1, qualifier.Length - 2).Replace("''", "'");
        }

        int workbookOpen = qualifier.IndexOf('[');
        if (workbookOpen >= 0)
        {
            int close = qualifier.IndexOf(']', workbookOpen + 1);
            if (close <= workbookOpen + 1)
            {
                result.AddError("reference.workbookQualifierInvalid", "Workbook qualifier must contain a name inside square brackets.");
                return "";
            }
            result.WorkbookName = qualifier.Substring(workbookOpen + 1, close - workbookOpen - 1);
            result.HasWorkbookName = true;
            result.IsExternal = true;
            qualifier = qualifier.Substring(close + 1);
        }

        if (qualifier.Length == 0)
        {
            result.AddError("reference.sheetNameMissing", "Worksheet qualifier must not be empty.");
            return "";
        }

        int rangeSeparator = qualifier.IndexOf(':');
        if (rangeSeparator >= 0)
        {
            if (rangeSeparator == 0 || rangeSeparator == qualifier.Length - 1 || qualifier.IndexOf(':', rangeSeparator + 1) >= 0)
            {
                result.AddError("reference.threeDimensionalInvalid", "Three-dimensional reference must contain two worksheet names.");
                return "";
            }
            result.FirstSheetName = qualifier.Substring(0, rangeSeparator);
            result.LastSheetName = qualifier.Substring(rangeSeparator + 1);
            result.IsThreeDimensional = true;
        }
        else
        {
            result.FirstSheetName = qualifier;
            result.LastSheetName = qualifier;
        }
        result.HasSheetName = true;
        return body;
    }

    internal static int FindQualifierSeparator(string text)
    {
        bool quoted = false;
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character == '\'')
            {
                if (quoted && index + 1 < text.Length && text[index + 1] == '\'')
                {
                    index = index + 2;
                    continue;
                }
                quoted = !quoted;
            }
            else if (character == '!' && !quoted)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal static string BuildQualifier(XlsxReferenceResolutionResult result)
    {
        if (!result.HasSheetName)
        {
            return "";
        }
        string sheet = result.FirstSheetName;
        if (result.IsThreeDimensional)
        {
            sheet = sheet + ":" + result.LastSheetName;
        }
        if (result.HasWorkbookName)
        {
            sheet = "[" + result.WorkbookName + "]" + sheet;
        }
        return XlsxWorkbookPart.QuoteSheetNameForFormula(sheet) + "!";
    }

    internal static bool ValidateBaseCell(XlsxReferenceResolutionResult result, int baseRow, int baseColumn)
    {
        if (baseRow < 1 || baseRow > XlsxXmlValueParser.MaxSpreadsheetRows)
        {
            result.AddError("reference.baseRowRange", "R1C1 base row must be within worksheet limits.");
        }
        if (baseColumn < 1 || baseColumn > XlsxXmlValueParser.MaxSpreadsheetColumns)
        {
            result.AddError("reference.baseColumnRange", "R1C1 base column must be within worksheet limits.");
        }
        return result.IsValid;
    }

    internal static XlsxCellReferenceValue ResolveR1C1Endpoint(XlsxReferenceResolutionResult result, XlsxR1C1ReferenceValue reference, int baseRow, int baseColumn)
    {
        XlsxCellReferenceValue value = new XlsxCellReferenceValue();
        int row = reference.IsRelativeRow ? baseRow + (reference.HasRow ? reference.Row : 0) : reference.Row;
        int column = reference.IsRelativeColumn ? baseColumn + (reference.HasColumn ? reference.Column : 0) : reference.Column;
        if (row < 1 || row > XlsxXmlValueParser.MaxSpreadsheetRows)
        {
            result.AddError("reference.resolvedRowRange", "Resolved R1C1 row is outside worksheet limits.");
        }
        if (column < 1 || column > XlsxXmlValueParser.MaxSpreadsheetColumns)
        {
            result.AddError("reference.resolvedColumnRange", "Resolved R1C1 column is outside worksheet limits.");
        }
        value.Row = row;
        value.Column = column;
        value.IsAbsoluteRow = !reference.IsRelativeRow;
        value.IsAbsoluteColumn = !reference.IsRelativeColumn;
        value.Text = XlsxA1CellReferenceConverter.FormatString(row, column, value.IsAbsoluteRow, value.IsAbsoluteColumn);
        return value;
    }

    internal static void SetA1Range(XlsxReferenceResolutionResult result, XlsxCellReferenceValue first, XlsxCellReferenceValue last, bool hasLast)
    {
        XlsxA1RangeReferenceValue range = new XlsxA1RangeReferenceValue();
        range.First = first;
        range.Last = last;
        range.HasLast = hasLast;
        range.Text = XlsxA1RangeReferenceConverter.FormatString(range);
        result.A1Range = range;
        result.HasA1Range = result.IsValid && range.Text.Length > 0;
    }

    internal static XlsxReferenceResolutionResult ParseStructuredReference(string text)
    {
        XlsxReferenceResolutionResult result = new XlsxReferenceResolutionResult();
        result.OriginalText = NormalizeInput(text);
        string body = ParseQualifier(result, result.OriginalText);
        if (!result.IsValid)
        {
            return result;
        }
        int bracket = body.IndexOf('[');
        if (bracket <= 0 || !body.EndsWith("]", StringComparison.Ordinal))
        {
            result.AddError("reference.structuredInvalid", "Structured reference must contain a table name followed by bracketed selectors.");
            return result;
        }

        result.IsStructuredReference = true;
        result.TableName = body.Substring(0, bracket);
        string selectors = body.Substring(bracket);
        ReadStructuredSelectors(result, selectors);
        return result;
    }

    internal static void ReadStructuredSelectors(XlsxReferenceResolutionResult result, string text)
    {
        if (text.Length < 2 || text[0] != '[' || text[text.Length - 1] != ']')
        {
            result.AddError("reference.structuredInvalid", "Structured reference selectors must be enclosed in square brackets.");
            return;
        }
        string inner = text.Substring(1, text.Length - 2);
        List<string> tokens = new List<string>();
        if (inner.IndexOf('[') < 0)
        {
            tokens.Add(inner);
        }
        else
        {
            int index = 0;
            while (index < inner.Length)
            {
                if (inner[index] != '[')
                {
                    index = index + 1;
                    continue;
                }
                int close = inner.IndexOf(']', index + 1);
                if (close < 0)
                {
                    result.AddError("reference.structuredInvalid", "Structured reference contains an unterminated selector.");
                    return;
                }
                tokens.Add(inner.Substring(index + 1, close - index - 1));
                index = close + 1;
            }
        }

        foreach (string rawToken in tokens)
        {
            string token = rawToken.Trim();
            if (token.StartsWith("#", StringComparison.Ordinal))
            {
                result.StructuredItem = token;
            }
            else if (token.StartsWith("@", StringComparison.Ordinal))
            {
                result.StructuredItem = "#This Row";
                AddStructuredColumn(result, token.Substring(1));
            }
            else if (token.Length > 0)
            {
                AddStructuredColumn(result, token);
            }
        }
        if (result.StructuredItem.Length == 0)
        {
            result.StructuredItem = "#Data";
        }
    }

    internal static void AddStructuredColumn(XlsxReferenceResolutionResult result, string name)
    {
        if (result.FirstColumnName.Length == 0)
        {
            result.FirstColumnName = name;
            result.LastColumnName = name;
        }
        else
        {
            result.LastColumnName = name;
        }
    }

    internal static void ResolveStructuredReference(XlsxReferenceResolutionResult result, XlsxWorksheetPart worksheet, string sheetName)
    {
        XlsxTablePart? table = FindTable(worksheet, result.TableName);
        if (table == null)
        {
            result.AddError("reference.tableNotFound", "Structured reference table was not found on the worksheet.");
            return;
        }
        XlsxA1RangeReferenceResult tableRange = XlsxA1RangeReferenceConverter.Parse(table.Reference);
        CopyDiagnostics(result, tableRange.Validation);
        if (!result.IsValid)
        {
            return;
        }

        int firstRow = tableRange.First.Row;
        int lastRow = tableRange.Last.Row;
        int firstColumn = tableRange.First.Column;
        int lastColumn = tableRange.Last.Column;
        int headerRows = table.HasHeaderRowCount ? (int)table.HeaderRowCount : 1;
        int totalsRows = table.HasTotalsRowCount ? (int)table.TotalsRowCount : (table.HasTotalsRowShown && table.TotalsRowShown ? 1 : 0);
        if (string.Equals(result.StructuredItem, "#Headers", StringComparison.OrdinalIgnoreCase))
        {
            if (headerRows <= 0)
            {
                result.AddError("reference.tableHeadersMissing", "Structured reference requests headers from a table without a header row.");
                return;
            }
            lastRow = firstRow + headerRows - 1;
        }
        else if (string.Equals(result.StructuredItem, "#Totals", StringComparison.OrdinalIgnoreCase))
        {
            if (totalsRows <= 0)
            {
                result.AddError("reference.tableTotalsMissing", "Structured reference requests totals from a table without a totals row.");
                return;
            }
            firstRow = lastRow - totalsRows + 1;
        }
        else if (!string.Equals(result.StructuredItem, "#All", StringComparison.OrdinalIgnoreCase))
        {
            firstRow = firstRow + headerRows;
            lastRow = lastRow - totalsRows;
            if (firstRow > lastRow)
            {
                result.AddError("reference.tableDataMissing", "Structured reference requests data from a table without data rows.");
                return;
            }
        }

        if (result.FirstColumnName.Length > 0)
        {
            int firstIndex = FindTableColumnIndex(table, result.FirstColumnName);
            int lastIndex = FindTableColumnIndex(table, result.LastColumnName);
            if (firstIndex < 0 || lastIndex < 0)
            {
                result.AddError("reference.tableColumnNotFound", "Structured reference column was not found in the table.");
                return;
            }
            firstColumn = tableRange.First.Column + firstIndex;
            lastColumn = tableRange.First.Column + lastIndex;
            if (firstColumn > lastColumn)
            {
                int temporary = firstColumn;
                firstColumn = lastColumn;
                lastColumn = temporary;
            }
        }

        XlsxCellReferenceValue first = CreateA1Cell(firstRow, firstColumn);
        XlsxCellReferenceValue last = CreateA1Cell(lastRow, lastColumn);
        SetA1Range(result, first, last, firstRow != lastRow || firstColumn != lastColumn);
        result.Table = table;
        result.HasTable = true;
        result.Worksheet = worksheet;
        result.HasWorksheet = true;
        if (!result.HasSheetName && sheetName != null && sheetName.Length > 0)
        {
            result.FirstSheetName = sheetName;
            result.LastSheetName = sheetName;
            result.HasSheetName = true;
        }
        result.NormalizedText = BuildQualifier(result) + result.A1Range.Text;
    }

    internal static XlsxCellReferenceValue CreateA1Cell(int row, int column)
    {
        XlsxCellReferenceValue value = new XlsxCellReferenceValue();
        value.Row = row;
        value.Column = column;
        value.Text = XlsxA1CellReferenceConverter.FormatString(row, column, false, false);
        return value;
    }

    internal static XlsxTablePart? FindTable(XlsxWorksheetPart worksheet, string tableName)
    {
        if (worksheet == null || worksheet.Tables == null)
        {
            return null;
        }
        foreach (XlsxTablePart table in worksheet.Tables)
        {
            if (table != null && (string.Equals(table.Name, tableName, StringComparison.OrdinalIgnoreCase) || string.Equals(table.DisplayName, tableName, StringComparison.OrdinalIgnoreCase)))
            {
                return table;
            }
        }
        return null;
    }

    internal static int FindTableColumnIndex(XlsxTablePart table, string columnName)
    {
        if (table.Columns == null)
        {
            return -1;
        }
        int index = 0;
        foreach (XlsxTableColumn column in table.Columns)
        {
            if (column != null && string.Equals(column.Name, columnName, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal static XlsxWorksheetPart? FindWorksheet(XlsxWorkbook workbook, string sheetName)
    {
        if (workbook.WorkbookPart == null || workbook.WorkbookPart.Sheets == null || workbook.WorkbookRelationships == null)
        {
            return null;
        }
        foreach (XlsxWorkbookSheet sheet in workbook.WorkbookPart.Sheets)
        {
            if (sheet == null || !string.Equals(sheet.Name, sheetName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            XlsxRelationship? relationship = workbook.WorkbookRelationships.GetRelationship(sheet.RelationshipId);
            if (relationship == null)
            {
                return null;
            }
            XlsxPackagePathResult target = XlsxPackagePath.ResolveRelationshipTarget(XlsxPackagePaths.Workbook, relationship.Target, false);
            foreach (XlsxWorksheetPart worksheet in workbook.Worksheets)
            {
                if (worksheet != null && target.Validation.IsValid && worksheet.PartPath == target.Path)
                {
                    return worksheet;
                }
            }
        }
        return null;
    }

    internal static string FindWorksheetName(XlsxWorkbook workbook, XlsxWorksheetPart worksheet)
    {
        if (workbook.WorkbookPart == null || workbook.WorkbookPart.Sheets == null || workbook.WorkbookRelationships == null)
        {
            return "";
        }
        foreach (XlsxWorkbookSheet sheet in workbook.WorkbookPart.Sheets)
        {
            if (sheet == null)
            {
                continue;
            }
            XlsxRelationship? relationship = workbook.WorkbookRelationships.GetRelationship(sheet.RelationshipId);
            if (relationship != null)
            {
                XlsxPackagePathResult target = XlsxPackagePath.ResolveRelationshipTarget(XlsxPackagePaths.Workbook, relationship.Target, false);
                if (target.Validation.IsValid && worksheet.PartPath == target.Path)
                {
                    return sheet.Name == null ? "" : sheet.Name;
                }
            }
        }
        return "";
    }

    internal static int FindSheetIndex(XlsxWorkbook workbook, string sheetName)
    {
        int index = 0;
        foreach (XlsxWorkbookSheet sheet in workbook.WorkbookPart.Sheets)
        {
            if (sheet != null && string.Equals(sheet.Name, sheetName, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal static string NormalizeInput(string text)
    {
        if (text == null)
        {
            return "";
        }
        string value = text.Trim();
        if (value.StartsWith("=", StringComparison.Ordinal))
        {
            value = value.Substring(1);
        }
        return value;
    }

    internal static bool HasExternalQualifier(string text)
    {
        string value = NormalizeInput(text);
        if (value.StartsWith("'", StringComparison.Ordinal))
        {
            value = value.Substring(1);
        }
        int open = value.IndexOf('[');
        int close = value.IndexOf(']');
        if (open < 0 || close <= open + 1)
        {
            return false;
        }
        if (open == 0)
        {
            return true;
        }
        int qualifierEnd = value.IndexOf('!');
        return qualifierEnd > open;
    }

    internal static void CopyDiagnostics(XlsxReferenceResolutionResult result, XlsxValidationResult validation)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            result.AddDiagnostic(diagnostic);
        }
    }
}
