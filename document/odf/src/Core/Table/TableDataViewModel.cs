using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace odf;

internal static class TableDataViewModel
{
    internal static void LoadDataViews(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for table-view materialization.");
        }
        document.TableTemplates = LoadTemplates(document);
        int index = 0;
        while (index < document.Tables.Length)
        {
            InitializeTableView(document.Tables[index]);
            index = index + 1;
        }
        ResolveDataViews(document);
    }

    internal static void SynchronizeDataViews(
        OdfDocument document,
        OdfSaveOptions options)
    {
        if (document == null || options == null)
        {
            throw new OdfDocumentException(
                "A document and save options are required for table-view synchronization.");
        }
        int templateIndex = 0;
        while (templateIndex < document.TableTemplates.Length)
        {
            SynchronizeTemplate(document.TableTemplates[templateIndex]);
            templateIndex = templateIndex + 1;
        }
        int tableIndex = 0;
        while (tableIndex < document.Tables.Length)
        {
            SynchronizeTable(
                document,
                document.Tables[tableIndex],
                options);
            tableIndex = tableIndex + 1;
        }
        ResolveDataViews(document);
    }

    internal static void InitializeRowView(OdfRow row)
    {
        OdfNode? node = row.Node;
        if (node == null)
        {
            return;
        }
        row.StyleName = ReadTableAttribute(node, "style-name");
        row.DefaultCellStyleName = ReadTableAttribute(
            node,
            "default-cell-style-name");
        string visibility = ReadTableAttribute(node, "visibility");
        row.Visibility = visibility.Length == 0 ? "visible" : visibility;
        row.IsVisible = row.Visibility == "visible";
        row.OriginalStyleName = row.StyleName;
        row.OriginalDefaultCellStyleName = row.DefaultCellStyleName;
        row.OriginalVisibility = row.Visibility;
        row.OriginalIsVisible = row.IsVisible;
    }

    internal static void InitializeColumnView(OdfColumn column)
    {
        OdfNode? node = column.Node;
        if (node == null)
        {
            return;
        }
        column.StyleName = ReadTableAttribute(node, "style-name");
        column.DefaultCellStyleName = ReadTableAttribute(
            node,
            "default-cell-style-name");
        string visibility = ReadTableAttribute(node, "visibility");
        column.Visibility = visibility.Length == 0 ? "visible" : visibility;
        column.IsVisible = column.Visibility == "visible";
        column.OriginalStyleName = column.StyleName;
        column.OriginalDefaultCellStyleName = column.DefaultCellStyleName;
        column.OriginalVisibility = column.Visibility;
        column.OriginalIsVisible = column.IsVisible;
    }

    internal static void InitializeCellView(OdfCell cell)
    {
        OdfNode? node = cell.Node;
        if (node == null)
        {
            return;
        }
        cell.StyleName = ReadTableAttribute(node, "style-name");
        cell.ValidationName = ReadTableAttribute(
            node,
            "content-validation-name");
        string protect = ReadTableAttribute(node, "protect");
        string protectedValue = ReadTableAttribute(node, "protected");
        cell.IsProtected = ParseBoolean(protect, false)
            || ParseBoolean(protectedValue, false);
        cell.OriginalStyleName = cell.StyleName;
        cell.OriginalValidationName = cell.ValidationName;
        cell.OriginalIsProtected = cell.IsProtected;
    }

    static OdfTableTemplate[] LoadTemplates(OdfDocument document)
    {
        List<OdfTableTemplate> result = new List<OdfTableTemplate>();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfPart part = document.Parts[partIndex];
            OdfNode[] nodes = new OdfTreeEditor(part.Nodes).TraverseAllNodes();
            int nodeIndex = 0;
            while (nodeIndex < nodes.Length)
            {
                OdfNode node = nodes[nodeIndex];
                if (IsTableElement(node, "table-template"))
                {
                    result.Add(CreateTemplate(node, part.Path));
                }
                nodeIndex = nodeIndex + 1;
            }
            partIndex = partIndex + 1;
        }
        return result.ToArray();
    }

    static OdfTableTemplate CreateTemplate(OdfNode node, string partPath)
    {
        OdfTableTemplate result = new OdfTableTemplate();
        result.Name = ReadTableAttribute(node, "name");
        result.FirstRowStartColumn = ReadTableAttribute(
            node,
            "first-row-start-column");
        result.FirstRowEndColumn = ReadTableAttribute(
            node,
            "first-row-end-column");
        result.LastRowStartColumn = ReadTableAttribute(
            node,
            "last-row-start-column");
        result.LastRowEndColumn = ReadTableAttribute(
            node,
            "last-row-end-column");
        ReadTemplateRegion(node, "first-row", result, 0);
        ReadTemplateRegion(node, "last-row", result, 1);
        ReadTemplateRegion(node, "first-column", result, 2);
        ReadTemplateRegion(node, "last-column", result, 3);
        ReadTemplateRegion(node, "body", result, 4);
        ReadTemplateRegion(node, "even-rows", result, 5);
        ReadTemplateRegion(node, "odd-rows", result, 6);
        ReadTemplateRegion(node, "even-columns", result, 7);
        ReadTemplateRegion(node, "odd-columns", result, 8);
        OdfNode? background = FindTableChild(node, "background");
        result.BackgroundStyleName = background == null
            ? ""
            : ReadTableAttribute(background, "style-name");
        result.PartPath = partPath;
        result.Node = node;
        result.OriginalSignature = TemplateSignature(result);
        return result;
    }

    static void ReadTemplateRegion(
        OdfNode template,
        string localName,
        OdfTableTemplate result,
        int region)
    {
        OdfNode? node = FindTableChild(template, localName);
        string styleName = node == null
            ? ""
            : ReadTableAttribute(node, "style-name");
        string paragraphStyleName = node == null
            ? ""
            : ReadTableAttribute(node, "paragraph-style-name");
        SetTemplateRegionFields(
            result,
            region,
            styleName,
            paragraphStyleName);
    }

    static void SetTemplateRegionFields(
        OdfTableTemplate template,
        int region,
        string styleName,
        string paragraphStyleName)
    {
        if (region == 0)
        {
            template.FirstRowStyleName = styleName;
            template.FirstRowParagraphStyleName = paragraphStyleName;
        }
        else if (region == 1)
        {
            template.LastRowStyleName = styleName;
            template.LastRowParagraphStyleName = paragraphStyleName;
        }
        else if (region == 2)
        {
            template.FirstColumnStyleName = styleName;
            template.FirstColumnParagraphStyleName = paragraphStyleName;
        }
        else if (region == 3)
        {
            template.LastColumnStyleName = styleName;
            template.LastColumnParagraphStyleName = paragraphStyleName;
        }
        else if (region == 4)
        {
            template.BodyStyleName = styleName;
            template.BodyParagraphStyleName = paragraphStyleName;
        }
        else if (region == 5)
        {
            template.EvenRowsStyleName = styleName;
            template.EvenRowsParagraphStyleName = paragraphStyleName;
        }
        else if (region == 6)
        {
            template.OddRowsStyleName = styleName;
            template.OddRowsParagraphStyleName = paragraphStyleName;
        }
        else if (region == 7)
        {
            template.EvenColumnsStyleName = styleName;
            template.EvenColumnsParagraphStyleName = paragraphStyleName;
        }
        else if (region == 8)
        {
            template.OddColumnsStyleName = styleName;
            template.OddColumnsParagraphStyleName = paragraphStyleName;
        }
    }

    static void InitializeTableView(OdfTable table)
    {
        OdfNode? node = table.Node;
        if (node == null)
        {
            return;
        }
        table.Name = ReadTableAttribute(node, "name");
        table.StyleName = ReadTableAttribute(node, "style-name");
        table.TemplateName = ReadTableAttribute(node, "template-name");
        table.UseFirstRowStyles = ParseBoolean(
            ReadTableAttribute(node, "use-first-row-styles"),
            false);
        table.UseLastRowStyles = ParseBoolean(
            ReadTableAttribute(node, "use-last-row-styles"),
            false);
        table.UseFirstColumnStyles = ParseBoolean(
            ReadTableAttribute(node, "use-first-column-styles"),
            false);
        table.UseLastColumnStyles = ParseBoolean(
            ReadTableAttribute(node, "use-last-column-styles"),
            false);
        table.UseBandingRowStyles = ParseBoolean(
            ReadTableAttribute(node, "use-banding-rows-styles"),
            false);
        table.UseBandingColumnStyles = ParseBoolean(
            ReadTableAttribute(node, "use-banding-columns-styles"),
            false);
        table.IsProtected = ParseBoolean(
            ReadTableAttribute(node, "protected"),
            false);
        table.ProtectionKey = ReadTableAttribute(node, "protection-key");
        table.ProtectionKeyDigestAlgorithm = ReadTableAttribute(
            node,
            "protection-key-digest-algorithm");
        table.IsPrinted = ParseBoolean(
            ReadTableAttribute(node, "print"),
            true);
        table.PrintRangeText = ReadTableAttribute(node, "print-ranges");
        table.PrintRanges = ParsePrintRanges(table, table.PrintRangeText);
        table.Title = ReadChildText(node, "title");
        table.Description = ReadChildText(node, "desc");
        InitializeTableSource(table);
        InitializeTitleRanges(table);
        int rowIndex = 0;
        while (rowIndex < table.Rows.Length)
        {
            InitializeRowView(table.Rows[rowIndex]);
            int cellIndex = 0;
            while (cellIndex < table.Rows[rowIndex].Cells.Length)
            {
                InitializeCellView(table.Rows[rowIndex].Cells[cellIndex]);
                cellIndex = cellIndex + 1;
            }
            rowIndex = rowIndex + 1;
        }
        int columnIndex = 0;
        while (columnIndex < table.Columns.Length)
        {
            InitializeColumnView(table.Columns[columnIndex]);
            columnIndex = columnIndex + 1;
        }
        SnapshotTable(table);
    }

    static void InitializeTableSource(OdfTable table)
    {
        if (table.Source == null)
        {
            table.Source = new OdfTableSource();
        }
        OdfTableSource source = table.Source;
        OdfNode? node = table.Node == null
            ? null
            : FindTableChild(table.Node, "table-source");
        if (node != null)
        {
            source.HasSource = true;
            source.Node = node;
            source.Href = StyleDocumentModel.ReadAttribute(
                node,
                OdfNamespaces.XLink,
                "href");
            string type = StyleDocumentModel.ReadAttribute(
                node,
                OdfNamespaces.XLink,
                "type");
            source.XLinkType = type.Length == 0 ? "simple" : type;
            source.XLinkActuate = StyleDocumentModel.ReadAttribute(
                node,
                OdfNamespaces.XLink,
                "actuate");
            source.TableName = ReadTableAttribute(node, "table-name");
            string mode = ReadTableAttribute(node, "mode");
            source.Mode = mode.Length == 0 ? "copy-all" : mode;
            source.FilterName = ReadTableAttribute(node, "filter-name");
            source.FilterOptions = ReadTableAttribute(
                node,
                "filter-options");
            source.RefreshDelay = ReadTableAttribute(node, "refresh-delay");
        }
        SnapshotSource(source);
    }

    static void InitializeTitleRanges(OdfTable table)
    {
        table.PrintTitleRowStart = 0;
        table.PrintTitleRowCount = 0;
        int rowIndex = 0;
        while (rowIndex < table.Rows.Length)
        {
            OdfRow row = table.Rows[rowIndex];
            if (row.IsHeader)
            {
                if (table.PrintTitleRowCount == 0)
                {
                    table.PrintTitleRowStart = row.StartIndex;
                }
                table.PrintTitleRowCount = AddSaturated(
                    table.PrintTitleRowCount,
                    row.RepeatCount);
            }
            rowIndex = rowIndex + 1;
        }
        table.PrintTitleColumnStart = 0;
        table.PrintTitleColumnCount = 0;
        int columnIndex = 0;
        while (columnIndex < table.Columns.Length)
        {
            OdfColumn column = table.Columns[columnIndex];
            if (column.IsHeader)
            {
                if (table.PrintTitleColumnCount == 0)
                {
                    table.PrintTitleColumnStart = column.StartIndex;
                }
                table.PrintTitleColumnCount = AddSaturated(
                    table.PrintTitleColumnCount,
                    column.RepeatCount);
            }
            columnIndex = columnIndex + 1;
        }
    }

    static OdfCellRange[] ParsePrintRanges(OdfTable owner, string lexical)
    {
        if (lexical.Length == 0)
        {
            return [];
        }
        OdfCellRangeListValue parsed = OdfCellAddressParser.ParseRangeList(
            lexical);
        if (!parsed.IsValid)
        {
            return [];
        }
        OdfCellRange[] result = new OdfCellRange[parsed.Ranges.Length];
        int index = 0;
        while (index < parsed.Ranges.Length)
        {
            result[index] = ConvertRange(owner, parsed.Ranges[index]);
            index = index + 1;
        }
        return result;
    }

    static OdfCellRange ConvertRange(
        OdfTable owner,
        OdfCellRangeValue source)
    {
        OdfCellRange result = new OdfCellRange();
        result.LexicalValue = source.LexicalValue;
        result.StartTableName = source.Start.HasSheet
            ? source.Start.SheetName
            : owner.Name;
        result.EndTableName = source.HasEnd && source.End.HasSheet
            ? source.End.SheetName
            : result.StartTableName;
        result.HasColumns = source.Start.HasColumn;
        result.HasRows = source.Start.HasRow;
        result.HasEnd = source.HasEnd;
        result.StartColumnIndex = source.Start.HasColumn
            ? ToZeroBasedIndex(source.Start.ColumnIndex)
            : 0;
        result.StartRowIndex = source.Start.HasRow
            ? ToZeroBasedIndex(source.Start.Row)
            : 0;
        OdfCellAddressValue end = source.HasEnd
            ? source.End
            : source.Start;
        result.EndColumnIndex = end.HasColumn
            ? ToZeroBasedIndex(end.ColumnIndex)
            : result.StartColumnIndex;
        result.EndRowIndex = end.HasRow
            ? ToZeroBasedIndex(end.Row)
            : result.StartRowIndex;
        result.IsValid = source.IsValid;
        result.Table = result.StartTableName == owner.Name ? owner : null;
        SnapshotRange(result);
        return result;
    }

    static long ToZeroBasedIndex(BigInteger value)
    {
        if (value <= 1)
        {
            return 0;
        }
        BigInteger zeroBased = value - 1;
        return zeroBased > long.MaxValue
            ? long.MaxValue
            : (long)zeroBased;
    }

    static void ResolveDataViews(OdfDocument document)
    {
        int tableIndex = 0;
        while (tableIndex < document.Tables.Length)
        {
            OdfTable table = document.Tables[tableIndex];
            table.Style = FindStyle(
                document,
                table.StyleName,
                OdfStyleFamily.Table,
                table.PartPath);
            table.DefaultStyle = FindDefaultStyle(
                document,
                OdfStyleFamily.Table);
            table.Template = FindTemplate(document, table.TemplateName);
            int rowIndex = 0;
            while (rowIndex < table.Rows.Length)
            {
                ResolveRowView(document, table.Rows[rowIndex]);
                int cellIndex = 0;
                while (cellIndex < table.Rows[rowIndex].Cells.Length)
                {
                    ResolveCellView(
                        document,
                        table.Rows[rowIndex].Cells[cellIndex]);
                    cellIndex = cellIndex + 1;
                }
                rowIndex = rowIndex + 1;
            }
            int columnIndex = 0;
            while (columnIndex < table.Columns.Length)
            {
                ResolveColumnView(document, table.Columns[columnIndex]);
                columnIndex = columnIndex + 1;
            }
            ResolveEffectiveCellStyles(table);
            ResolvePrintRangeTables(document, table.PrintRanges);
            tableIndex = tableIndex + 1;
        }
    }

    static void ResolveRowView(OdfDocument document, OdfRow row)
    {
        string partPath = row.Table == null ? "" : row.Table.PartPath;
        row.Style = FindStyle(
            document,
            row.StyleName,
            OdfStyleFamily.TableRow,
            partPath);
        row.DefaultStyle = FindDefaultStyle(
            document,
            OdfStyleFamily.TableRow);
        row.DefaultCellStyle = FindStyle(
            document,
            row.DefaultCellStyleName,
            OdfStyleFamily.TableCell,
            partPath);
    }

    static void ResolveColumnView(OdfDocument document, OdfColumn column)
    {
        string partPath = column.Table == null ? "" : column.Table.PartPath;
        column.Style = FindStyle(
            document,
            column.StyleName,
            OdfStyleFamily.TableColumn,
            partPath);
        column.DefaultStyle = FindDefaultStyle(
            document,
            OdfStyleFamily.TableColumn);
        column.DefaultCellStyle = FindStyle(
            document,
            column.DefaultCellStyleName,
            OdfStyleFamily.TableCell,
            partPath);
    }

    static void ResolveCellView(OdfDocument document, OdfCell cell)
    {
        OdfTable? table = cell.Row == null ? null : cell.Row.Table;
        string partPath = table == null ? "" : table.PartPath;
        cell.Style = FindStyle(
            document,
            cell.StyleName,
            OdfStyleFamily.TableCell,
            partPath);
        cell.DefaultStyle = FindDefaultStyle(
            document,
            OdfStyleFamily.TableCell);
        cell.Validation = SpreadsheetFormattingModel.FindValidation(
            document,
            cell.ValidationName);
    }

    static void ResolveEffectiveCellStyles(OdfTable table)
    {
        int rowIndex = 0;
        while (rowIndex < table.Rows.Length)
        {
            OdfRow row = table.Rows[rowIndex];
            int cellIndex = 0;
            while (cellIndex < row.Cells.Length)
            {
                OdfCell cell = row.Cells[cellIndex];
                OdfColumn? column = TableDocumentModel.GetColumn(
                    table,
                    cell.StartColumnIndex);
                if (cell.Style != null)
                {
                    cell.EffectiveStyle = cell.Style;
                }
                else if (row.DefaultCellStyle != null)
                {
                    cell.EffectiveStyle = row.DefaultCellStyle;
                }
                else if (column != null && column.DefaultCellStyle != null)
                {
                    cell.EffectiveStyle = column.DefaultCellStyle;
                }
                else
                {
                    cell.EffectiveStyle = cell.DefaultStyle;
                }
                cellIndex = cellIndex + 1;
            }
            rowIndex = rowIndex + 1;
        }
    }

    static OdfStyle? FindStyle(
        OdfDocument document,
        string name,
        OdfStyleFamily family,
        string partPath)
    {
        if (name.Length == 0)
        {
            return null;
        }
        OdfStyle? common = null;
        OdfStyle? any = null;
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            if (!style.IsDefault
                && style.Name == name
                && style.Family == family)
            {
                if (style.PartPath == partPath)
                {
                    return style;
                }
                if (common == null && style.IsCommon)
                {
                    common = style;
                }
                if (any == null)
                {
                    any = style;
                }
            }
            index = index + 1;
        }
        return common ?? any;
    }

    static OdfStyle? FindDefaultStyle(
        OdfDocument document,
        OdfStyleFamily family)
    {
        int index = 0;
        while (index < document.Styles.Length)
        {
            OdfStyle style = document.Styles[index];
            if (style.IsDefault && style.Family == family)
            {
                return style;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfTableTemplate? FindTemplate(
        OdfDocument document,
        string name)
    {
        if (name.Length == 0)
        {
            return null;
        }
        OdfTableTemplate? result = null;
        int count = 0;
        int index = 0;
        while (index < document.TableTemplates.Length)
        {
            if (document.TableTemplates[index].Name == name)
            {
                result = document.TableTemplates[index];
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
    }

    static void ResolvePrintRangeTables(
        OdfDocument document,
        OdfCellRange[] ranges)
    {
        int index = 0;
        while (index < ranges.Length)
        {
            ranges[index].Table = TableDocumentModel.FindTable(
                document,
                ranges[index].StartTableName);
            index = index + 1;
        }
    }

    static void SynchronizeTable(
        OdfDocument document,
        OdfTable table,
        OdfSaveOptions options)
    {
        OdfNode? node = table.Node;
        if (node == null)
        {
            return;
        }
        WriteChangedTableAttribute(
            node,
            "name",
            table.Name,
            table.OriginalName);
        WriteChangedTableAttribute(
            node,
            "style-name",
            table.StyleName,
            table.OriginalStyleName);
        WriteChangedTableAttribute(
            node,
            "template-name",
            table.TemplateName,
            table.OriginalTemplateName);
        WriteChangedBooleanAttribute(node, "use-first-row-styles", table.UseFirstRowStyles, table.OriginalUseFirstRowStyles);
        WriteChangedBooleanAttribute(node, "use-last-row-styles", table.UseLastRowStyles, table.OriginalUseLastRowStyles);
        WriteChangedBooleanAttribute(node, "use-first-column-styles", table.UseFirstColumnStyles, table.OriginalUseFirstColumnStyles);
        WriteChangedBooleanAttribute(node, "use-last-column-styles", table.UseLastColumnStyles, table.OriginalUseLastColumnStyles);
        WriteChangedBooleanAttribute(node, "use-banding-rows-styles", table.UseBandingRowStyles, table.OriginalUseBandingRowStyles);
        WriteChangedBooleanAttribute(node, "use-banding-columns-styles", table.UseBandingColumnStyles, table.OriginalUseBandingColumnStyles);
        WriteChangedBooleanAttribute(node, "protected", table.IsProtected, table.OriginalIsProtected);
        WriteChangedTableAttribute(node, "protection-key", table.ProtectionKey, table.OriginalProtectionKey);
        WriteProtectionDigest(
            document,
            node,
            table.ProtectionKeyDigestAlgorithm,
            table.OriginalProtectionKeyDigestAlgorithm,
            options);
        WriteChangedBooleanAttribute(node, "print", table.IsPrinted, table.OriginalIsPrinted);
        string printRanges = SelectPrintRangeText(table);
        WriteChangedTableAttribute(node, "print-ranges", printRanges, table.OriginalPrintRangeText);
        table.PrintRangeText = printRanges;
        if (table.Title != table.OriginalTitle)
        {
            WriteChildText(node, "title", table.Title);
        }
        if (table.Description != table.OriginalDescription)
        {
            WriteChildText(node, "desc", table.Description);
        }
        SynchronizeTableSource(table, node);
        int rowIndex = 0;
        while (rowIndex < table.Rows.Length)
        {
            SynchronizeRow(table.Rows[rowIndex]);
            int cellIndex = 0;
            while (cellIndex < table.Rows[rowIndex].Cells.Length)
            {
                SynchronizeCell(table.Rows[rowIndex].Cells[cellIndex]);
                cellIndex = cellIndex + 1;
            }
            rowIndex = rowIndex + 1;
        }
        int columnIndex = 0;
        while (columnIndex < table.Columns.Length)
        {
            SynchronizeColumn(table.Columns[columnIndex]);
            columnIndex = columnIndex + 1;
        }
        table.PrintRanges = ParsePrintRanges(table, table.PrintRangeText);
        SnapshotTable(table);
    }

    static void SynchronizeTableSource(OdfTable table, OdfNode tableNode)
    {
        if (table.Source == null)
        {
            throw new OdfDocumentException(
                "A table source description is required for writing.");
        }
        OdfTableSource source = table.Source;
        if (source.Node == null)
        {
            source.Node = FindTableChild(tableNode, "table-source");
        }
        PromoteChangedSource(source);
        if (!source.HasSource)
        {
            if (source.Node != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(source.Node);
                source.Node = null;
            }
            SnapshotSource(source);
            return;
        }
        bool isNew = source.Node == null;
        if (isNew || SourceChanged(source))
        {
            ValidateSource(source);
        }
        OdfNode node = source.Node ?? CreateTableElement(
            "table-source",
            tableNode.Location.PartPath);
        if (isNew)
        {
            InsertTableSource(tableNode, node);
            source.Node = node;
        }
        WriteChangedQualifiedAttribute(
            node,
            OdfNamespaces.XLink,
            "href",
            OdfNamespaces.XLinkPrefix,
            source.Href,
            source.OriginalHref,
            isNew,
            false);
        WriteChangedQualifiedAttribute(
            node,
            OdfNamespaces.XLink,
            "type",
            OdfNamespaces.XLinkPrefix,
            source.XLinkType,
            source.OriginalXLinkType,
            isNew,
            false);
        WriteChangedQualifiedAttribute(
            node,
            OdfNamespaces.XLink,
            "actuate",
            OdfNamespaces.XLinkPrefix,
            source.XLinkActuate,
            source.OriginalXLinkActuate,
            isNew,
            true);
        WriteChangedSourceAttribute(
            node,
            "table-name",
            source.TableName,
            source.OriginalTableName,
            isNew,
            "");
        WriteChangedSourceAttribute(
            node,
            "mode",
            source.Mode,
            source.OriginalMode,
            isNew,
            "copy-all");
        WriteChangedSourceAttribute(
            node,
            "filter-name",
            source.FilterName,
            source.OriginalFilterName,
            isNew,
            "");
        WriteChangedSourceAttribute(
            node,
            "filter-options",
            source.FilterOptions,
            source.OriginalFilterOptions,
            isNew,
            "");
        WriteChangedSourceAttribute(
            node,
            "refresh-delay",
            source.RefreshDelay,
            source.OriginalRefreshDelay,
            isNew,
            "");
        SnapshotSource(source);
    }

    static void PromoteChangedSource(OdfTableSource source)
    {
        if (!source.OriginalHasSource
            && !source.HasSource
            && SourceChanged(source))
        {
            source.HasSource = true;
        }
    }

    static bool SourceChanged(OdfTableSource source)
    {
        return source.Href != source.OriginalHref
            || source.XLinkType != source.OriginalXLinkType
            || source.XLinkActuate != source.OriginalXLinkActuate
            || source.TableName != source.OriginalTableName
            || source.Mode != source.OriginalMode
            || source.FilterName != source.OriginalFilterName
            || source.FilterOptions != source.OriginalFilterOptions
            || source.RefreshDelay != source.OriginalRefreshDelay;
    }

    static void ValidateSource(OdfTableSource source)
    {
        if (source.Href.Length == 0)
        {
            throw new OdfDocumentException(
                "A linked table source requires an href value.");
        }
        if (source.XLinkType != "simple")
        {
            throw new OdfDocumentException(
                "A linked table source must use the simple XLink type.");
        }
        if (source.XLinkActuate.Length > 0
            && source.XLinkActuate != "onRequest")
        {
            throw new OdfDocumentException(
                "A linked table source can only actuate on request.");
        }
        if (source.Mode != "copy-all"
            && source.Mode != "copy-results-only")
        {
            throw new OdfDocumentException(
                "A linked table source mode is not recognized.");
        }
        if (source.RefreshDelay.Length > 0
            && !OdfLexicalParser.ParseDuration(
                source.RefreshDelay).IsValid)
        {
            throw new OdfDocumentException(
                "A linked table source refresh delay must be a duration.");
        }
    }

    static void InsertTableSource(OdfNode table, OdfNode source)
    {
        int insertion = table.Children.Length;
        int index = 0;
        while (index < table.Children.Length)
        {
            OdfNode child = table.Children[index];
            bool metadata = IsTableElement(child, "title")
                || IsTableElement(child, "desc");
            if (child.Kind == OdfNodeKind.Element && !metadata)
            {
                insertion = index;
                index = table.Children.Length;
            }
            else
            {
                index = index + 1;
            }
        }
        OdfNode[] children = new OdfNode[table.Children.Length + 1];
        Array.Copy(table.Children, 0, children, 0, insertion);
        children[insertion] = source;
        Array.Copy(
            table.Children,
            insertion,
            children,
            insertion + 1,
            table.Children.Length - insertion);
        table.Children = children;
        source.Parent = table;
        source.Location.PartPath = table.Location.PartPath;
        table.WasEmptyElement = false;
    }

    static void WriteChangedSourceAttribute(
        OdfNode node,
        string localName,
        string value,
        string original,
        bool isNew,
        string defaultValue)
    {
        if (!isNew && value == original)
        {
            return;
        }
        string lexical = value == defaultValue ? "" : value;
        WriteOptionalTableAttribute(node, localName, lexical);
    }

    static void WriteChangedQualifiedAttribute(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value,
        string original,
        bool isNew,
        bool optional)
    {
        if (!isNew && value == original)
        {
            return;
        }
        if (optional && value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                namespaceName,
                localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                node,
                namespaceName,
                localName,
                prefix,
                value);
        }
    }

    internal static void WriteProtectionDigest(
        OdfDocument document,
        OdfNode node,
        string value,
        string original,
        OdfSaveOptions options)
    {
        if (IsLegacyTarget(options.TargetVersion))
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                OdfNamespaces.Table,
                "protection-key-digest-algorithm");
            return;
        }
        bool legacyDocument = options.TargetVersion == OdfVersion.Unknown
            && document.VersionInfo != null
            && (document.VersionInfo.Version == OdfVersion.Version10
                || document.VersionInfo.Version == OdfVersion.Version11);
        if (legacyDocument && value != original && value.Length > 0)
        {
            throw new OdfDocumentException(
                "Protection-key digest algorithms require OpenDocument 1.2 or later.");
        }
        if (legacyDocument && value == original)
        {
            return;
        }
        if (value != original
            || value.Length > 0
                && !HasTableAttribute(
                    node,
                    "protection-key-digest-algorithm"))
        {
            WriteOptionalTableAttribute(
                node,
                "protection-key-digest-algorithm",
                value);
        }
    }

    static bool IsLegacyTarget(OdfVersion version)
    {
        return version == OdfVersion.Version10
            || version == OdfVersion.Version11;
    }

    static bool HasTableAttribute(OdfNode node, string localName)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == OdfNamespaces.Table
                && attribute.Name.LocalName == localName)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static string SelectPrintRangeText(OdfTable table)
    {
        bool changed = table.PrintRanges.Length
            != table.OriginalPrintRangeCount;
        int index = 0;
        while (index < table.PrintRanges.Length)
        {
            if (RangeChanged(table.PrintRanges[index]))
            {
                changed = true;
            }
            index = index + 1;
        }
        if (!changed)
        {
            return table.PrintRangeText;
        }
        string[] values = new string[table.PrintRanges.Length];
        index = 0;
        while (index < table.PrintRanges.Length)
        {
            OdfCellRange range = table.PrintRanges[index];
            if (range.LexicalValue == range.OriginalLexicalValue
                && RangeCoordinatesChanged(range))
            {
                range.LexicalValue = FormatPublicRange(table, range);
            }
            values[index] = range.LexicalValue;
            index = index + 1;
        }
        return string.Join(" ", values);
    }

    static bool RangeChanged(OdfCellRange range)
    {
        return range.LexicalValue != range.OriginalLexicalValue
            || RangeCoordinatesChanged(range);
    }

    static bool RangeCoordinatesChanged(OdfCellRange range)
    {
        return range.StartTableName != range.OriginalStartTableName
            || range.EndTableName != range.OriginalEndTableName
            || range.StartColumnIndex != range.OriginalStartColumnIndex
            || range.EndColumnIndex != range.OriginalEndColumnIndex
            || range.StartRowIndex != range.OriginalStartRowIndex
            || range.EndRowIndex != range.OriginalEndRowIndex
            || range.HasColumns != range.OriginalHasColumns
            || range.HasRows != range.OriginalHasRows
            || range.HasEnd != range.OriginalHasEnd;
    }

    static string FormatPublicRange(OdfTable owner, OdfCellRange range)
    {
        OdfCellRangeValue value = new OdfCellRangeValue();
        string startTableName = range.Table == owner
            ? owner.Name
            : range.StartTableName;
        string endTableName = range.Table == owner
            ? owner.Name
            : range.EndTableName;
        value.Start = CreateRangeEndpoint(
            owner,
            startTableName,
            range.StartColumnIndex,
            range.StartRowIndex,
            range.HasColumns,
            range.HasRows);
        value.HasEnd = range.HasEnd;
        if (range.HasEnd)
        {
            value.End = CreateRangeEndpoint(
                owner,
                endTableName,
                range.EndColumnIndex,
                range.EndRowIndex,
                range.HasColumns,
                range.HasRows);
        }
        value.IsValid = range.IsValid && (range.HasColumns || range.HasRows);
        string result = OdfCellAddressFormatter.FormatCellRange(value);
        if (result.Length == 0)
        {
            throw new OdfEditException(
                "A print range requires valid row or column coordinates.");
        }
        return result;
    }

    static OdfCellAddressValue CreateRangeEndpoint(
        OdfTable owner,
        string tableName,
        long columnIndex,
        long rowIndex,
        bool hasColumns,
        bool hasRows)
    {
        if (columnIndex < 0 || rowIndex < 0)
        {
            throw new OdfEditException(
                "Print-range row and column indexes cannot be negative.");
        }
        OdfCellAddressValue result = new OdfCellAddressValue();
        result.HasSheet = tableName.Length > 0 && tableName != owner.Name;
        result.SheetName = result.HasSheet ? tableName : "";
        result.HasColumn = hasColumns;
        result.ColumnIndex = new BigInteger(columnIndex) + 1;
        result.HasRow = hasRows;
        result.Row = new BigInteger(rowIndex) + 1;
        result.IsValid = hasColumns || hasRows;
        return result;
    }

    static void SnapshotRange(OdfCellRange range)
    {
        range.OriginalLexicalValue = range.LexicalValue;
        range.OriginalStartTableName = range.StartTableName;
        range.OriginalEndTableName = range.EndTableName;
        range.OriginalStartColumnIndex = range.StartColumnIndex;
        range.OriginalEndColumnIndex = range.EndColumnIndex;
        range.OriginalStartRowIndex = range.StartRowIndex;
        range.OriginalEndRowIndex = range.EndRowIndex;
        range.OriginalHasColumns = range.HasColumns;
        range.OriginalHasRows = range.HasRows;
        range.OriginalHasEnd = range.HasEnd;
    }

    static void SynchronizeRow(OdfRow row)
    {
        OdfNode? node = row.Node;
        if (node == null)
        {
            return;
        }
        WriteChangedTableAttribute(node, "style-name", row.StyleName, row.OriginalStyleName);
        WriteChangedTableAttribute(node, "default-cell-style-name", row.DefaultCellStyleName, row.OriginalDefaultCellStyleName);
        if (row.Visibility == row.OriginalVisibility
            && row.IsVisible != row.OriginalIsVisible)
        {
            row.Visibility = row.IsVisible ? "visible" : "collapse";
        }
        WriteChangedTableAttribute(node, "visibility", row.Visibility, row.OriginalVisibility);
        row.IsVisible = row.Visibility.Length == 0
            || row.Visibility == "visible";
        row.OriginalStyleName = row.StyleName;
        row.OriginalDefaultCellStyleName = row.DefaultCellStyleName;
        row.OriginalVisibility = row.Visibility;
        row.OriginalIsVisible = row.IsVisible;
    }

    static void SynchronizeColumn(OdfColumn column)
    {
        OdfNode? node = column.Node;
        if (node == null)
        {
            return;
        }
        WriteChangedTableAttribute(node, "style-name", column.StyleName, column.OriginalStyleName);
        WriteChangedTableAttribute(node, "default-cell-style-name", column.DefaultCellStyleName, column.OriginalDefaultCellStyleName);
        if (column.Visibility == column.OriginalVisibility
            && column.IsVisible != column.OriginalIsVisible)
        {
            column.Visibility = column.IsVisible ? "visible" : "collapse";
        }
        WriteChangedTableAttribute(node, "visibility", column.Visibility, column.OriginalVisibility);
        column.IsVisible = column.Visibility.Length == 0
            || column.Visibility == "visible";
        column.OriginalStyleName = column.StyleName;
        column.OriginalDefaultCellStyleName = column.DefaultCellStyleName;
        column.OriginalVisibility = column.Visibility;
        column.OriginalIsVisible = column.IsVisible;
    }

    static void SynchronizeCell(OdfCell cell)
    {
        OdfNode? node = cell.Node;
        if (node == null)
        {
            return;
        }
        WriteChangedTableAttribute(node, "style-name", cell.StyleName, cell.OriginalStyleName);
        WriteChangedTableAttribute(
            node,
            "content-validation-name",
            cell.ValidationName,
            cell.OriginalValidationName);
        if (cell.IsProtected != cell.OriginalIsProtected)
        {
            StyleDocumentModel.RemoveAttribute(node, OdfNamespaces.Table, "protect");
            StyleDocumentModel.WriteAttribute(
                node,
                OdfNamespaces.Table,
                "protected",
                OdfNamespaces.TablePrefix,
                cell.IsProtected ? "true" : "false");
        }
        cell.OriginalStyleName = cell.StyleName;
        cell.OriginalValidationName = cell.ValidationName;
        cell.OriginalIsProtected = cell.IsProtected;
    }

    static void SynchronizeTemplate(OdfTableTemplate template)
    {
        OdfNode? node = template.Node;
        if (node == null || TemplateSignature(template) == template.OriginalSignature)
        {
            return;
        }
        WriteRequiredTableAttribute(node, "name", template.Name);
        WriteOptionalTableAttribute(node, "first-row-start-column", template.FirstRowStartColumn);
        WriteOptionalTableAttribute(node, "first-row-end-column", template.FirstRowEndColumn);
        WriteOptionalTableAttribute(node, "last-row-start-column", template.LastRowStartColumn);
        WriteOptionalTableAttribute(node, "last-row-end-column", template.LastRowEndColumn);
        WriteTemplateRegion(node, "first-row", template.FirstRowStyleName, template.FirstRowParagraphStyleName);
        WriteTemplateRegion(node, "last-row", template.LastRowStyleName, template.LastRowParagraphStyleName);
        WriteTemplateRegion(node, "first-column", template.FirstColumnStyleName, template.FirstColumnParagraphStyleName);
        WriteTemplateRegion(node, "last-column", template.LastColumnStyleName, template.LastColumnParagraphStyleName);
        WriteTemplateRegion(node, "body", template.BodyStyleName, template.BodyParagraphStyleName);
        WriteTemplateRegion(node, "even-rows", template.EvenRowsStyleName, template.EvenRowsParagraphStyleName);
        WriteTemplateRegion(node, "odd-rows", template.OddRowsStyleName, template.OddRowsParagraphStyleName);
        WriteTemplateRegion(node, "even-columns", template.EvenColumnsStyleName, template.EvenColumnsParagraphStyleName);
        WriteTemplateRegion(node, "odd-columns", template.OddColumnsStyleName, template.OddColumnsParagraphStyleName);
        WriteTemplateRegion(node, "background", template.BackgroundStyleName, "");
        template.OriginalSignature = TemplateSignature(template);
    }

    static void WriteTemplateRegion(
        OdfNode template,
        string localName,
        string styleName,
        string paragraphStyleName)
    {
        OdfNode? node = FindTableChild(template, localName);
        if (styleName.Length == 0)
        {
            if (localName == "body")
            {
                throw new OdfEditException(
                    "A table template requires a body style.");
            }
            if (node != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
            }
            return;
        }
        if (node == null)
        {
            node = CreateTableElement(localName, template.Location.PartPath);
            InsertTemplateRegion(template, node);
        }
        WriteRequiredTableAttribute(node, "style-name", styleName);
        WriteOptionalTableAttribute(
            node,
            "paragraph-style-name",
            paragraphStyleName);
    }

    static string TemplateSignature(OdfTableTemplate template)
    {
        StringBuilder result = new StringBuilder();
        AppendSignature(result, template.Name);
        AppendSignature(result, template.FirstRowStartColumn);
        AppendSignature(result, template.FirstRowEndColumn);
        AppendSignature(result, template.LastRowStartColumn);
        AppendSignature(result, template.LastRowEndColumn);
        AppendSignature(result, template.FirstRowStyleName);
        AppendSignature(result, template.FirstRowParagraphStyleName);
        AppendSignature(result, template.LastRowStyleName);
        AppendSignature(result, template.LastRowParagraphStyleName);
        AppendSignature(result, template.FirstColumnStyleName);
        AppendSignature(result, template.FirstColumnParagraphStyleName);
        AppendSignature(result, template.LastColumnStyleName);
        AppendSignature(result, template.LastColumnParagraphStyleName);
        AppendSignature(result, template.BodyStyleName);
        AppendSignature(result, template.BodyParagraphStyleName);
        AppendSignature(result, template.EvenRowsStyleName);
        AppendSignature(result, template.EvenRowsParagraphStyleName);
        AppendSignature(result, template.OddRowsStyleName);
        AppendSignature(result, template.OddRowsParagraphStyleName);
        AppendSignature(result, template.EvenColumnsStyleName);
        AppendSignature(result, template.EvenColumnsParagraphStyleName);
        AppendSignature(result, template.OddColumnsStyleName);
        AppendSignature(result, template.OddColumnsParagraphStyleName);
        AppendSignature(result, template.BackgroundStyleName);
        return result.ToString();
    }

    static void AppendSignature(StringBuilder result, string value)
    {
        result.Append(value.Length);
        result.Append(':');
        result.Append(value);
        result.Append(';');
    }

    static void SnapshotTable(OdfTable table)
    {
        table.OriginalName = table.Name;
        table.OriginalStyleName = table.StyleName;
        table.OriginalTemplateName = table.TemplateName;
        table.OriginalUseFirstRowStyles = table.UseFirstRowStyles;
        table.OriginalUseLastRowStyles = table.UseLastRowStyles;
        table.OriginalUseFirstColumnStyles = table.UseFirstColumnStyles;
        table.OriginalUseLastColumnStyles = table.UseLastColumnStyles;
        table.OriginalUseBandingRowStyles = table.UseBandingRowStyles;
        table.OriginalUseBandingColumnStyles = table.UseBandingColumnStyles;
        table.OriginalIsProtected = table.IsProtected;
        table.OriginalProtectionKey = table.ProtectionKey;
        table.OriginalProtectionKeyDigestAlgorithm =
            table.ProtectionKeyDigestAlgorithm;
        table.OriginalIsPrinted = table.IsPrinted;
        table.OriginalPrintRangeText = table.PrintRangeText;
        table.OriginalPrintRangeCount = table.PrintRanges.Length;
        table.OriginalTitle = table.Title;
        table.OriginalDescription = table.Description;
        SnapshotSource(table.Source);
    }

    static void SnapshotSource(OdfTableSource source)
    {
        source.OriginalHasSource = source.HasSource;
        source.OriginalHref = source.Href;
        source.OriginalXLinkType = source.XLinkType;
        source.OriginalXLinkActuate = source.XLinkActuate;
        source.OriginalTableName = source.TableName;
        source.OriginalMode = source.Mode;
        source.OriginalFilterName = source.FilterName;
        source.OriginalFilterOptions = source.FilterOptions;
        source.OriginalRefreshDelay = source.RefreshDelay;
    }

    static string ReadChildText(OdfNode parent, string localName)
    {
        OdfNode? node = FindTableChild(parent, localName);
        if (node == null)
        {
            return "";
        }
        StringBuilder result = new StringBuilder();
        AppendNodeText(result, node);
        return result.ToString();
    }

    static void AppendNodeText(StringBuilder result, OdfNode node)
    {
        if (node.Kind == OdfNodeKind.Text)
        {
            result.Append(node.Value);
            return;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            AppendNodeText(result, node.Children[index]);
            index = index + 1;
        }
    }

    static void WriteChildText(
        OdfNode parent,
        string localName,
        string value)
    {
        OdfNode? node = FindTableChild(parent, localName);
        if (value.Length == 0)
        {
            if (node != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
            }
            return;
        }
        if (node == null)
        {
            node = CreateTableElement(localName, parent.Location.PartPath);
            InsertTableMetadataChild(parent, node);
        }
        OdfNode text = new OdfNode();
        text.Kind = OdfNodeKind.Text;
        text.Value = value;
        text.Parent = node;
        text.Location.PartPath = node.Location.PartPath;
        node.Children = [text];
        node.WasEmptyElement = false;
    }

    static void InsertTableMetadataChild(OdfNode parent, OdfNode node)
    {
        int rank = node.Name != null && node.Name.LocalName == "title" ? 0 : 1;
        int insertion = 0;
        while (insertion < parent.Children.Length
            && TableMetadataRank(parent.Children[insertion]) <= rank)
        {
            insertion = insertion + 1;
        }
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        Array.Copy(parent.Children, 0, children, 0, insertion);
        children[insertion] = node;
        Array.Copy(
            parent.Children,
            insertion,
            children,
            insertion + 1,
            parent.Children.Length - insertion);
        parent.Children = children;
        node.Parent = parent;
        parent.WasEmptyElement = false;
    }

    static int TableMetadataRank(OdfNode node)
    {
        if (IsTableElement(node, "title"))
        {
            return 0;
        }
        if (IsTableElement(node, "desc"))
        {
            return 1;
        }
        return 2;
    }

    static void InsertTemplateRegion(OdfNode template, OdfNode node)
    {
        int rank = TemplateRegionRank(node);
        int insertion = 0;
        while (insertion < template.Children.Length
            && TemplateRegionRank(template.Children[insertion]) <= rank)
        {
            insertion = insertion + 1;
        }
        OdfNode[] children = new OdfNode[template.Children.Length + 1];
        Array.Copy(template.Children, 0, children, 0, insertion);
        children[insertion] = node;
        Array.Copy(
            template.Children,
            insertion,
            children,
            insertion + 1,
            template.Children.Length - insertion);
        template.Children = children;
        node.Parent = template;
        template.WasEmptyElement = false;
    }

    static int TemplateRegionRank(OdfNode node)
    {
        if (IsTableElement(node, "first-row")) return 0;
        if (IsTableElement(node, "last-row")) return 1;
        if (IsTableElement(node, "first-column")) return 2;
        if (IsTableElement(node, "last-column")) return 3;
        if (IsTableElement(node, "body")) return 4;
        if (IsTableElement(node, "even-rows")) return 5;
        if (IsTableElement(node, "odd-rows")) return 6;
        if (IsTableElement(node, "even-columns")) return 7;
        if (IsTableElement(node, "odd-columns")) return 8;
        if (IsTableElement(node, "background")) return 9;
        return 10;
    }

    static OdfNode CreateTableElement(string localName, string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = new OdfQualifiedName();
        result.Name.NamespaceName = OdfNamespaces.Table;
        result.Name.LocalName = localName;
        result.Name.Prefix = OdfNamespaces.TablePrefix;
        result.Location.PartPath = partPath;
        result.WasEmptyElement = true;
        return result;
    }

    static OdfNode? FindTableChild(OdfNode parent, string localName)
    {
        int index = 0;
        while (index < parent.Children.Length)
        {
            if (IsTableElement(parent.Children[index], localName))
            {
                return parent.Children[index];
            }
            index = index + 1;
        }
        return null;
    }

    static string ReadTableAttribute(OdfNode node, string localName)
    {
        return StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Table,
            localName);
    }

    static void WriteChangedTableAttribute(
        OdfNode node,
        string localName,
        string value,
        string original)
    {
        if (value != original)
        {
            WriteOptionalTableAttribute(node, localName, value);
        }
    }

    static void WriteChangedBooleanAttribute(
        OdfNode node,
        string localName,
        bool value,
        bool original)
    {
        if (value != original)
        {
            WriteRequiredTableAttribute(
                node,
                localName,
                value ? "true" : "false");
        }
    }

    static void WriteRequiredTableAttribute(
        OdfNode node,
        string localName,
        string value)
    {
        StyleDocumentModel.WriteAttribute(
            node,
            OdfNamespaces.Table,
            localName,
            OdfNamespaces.TablePrefix,
            value);
    }

    static void WriteOptionalTableAttribute(
        OdfNode node,
        string localName,
        string value)
    {
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                OdfNamespaces.Table,
                localName);
        }
        else
        {
            WriteRequiredTableAttribute(node, localName, value);
        }
    }

    static bool ParseBoolean(string value, bool defaultValue)
    {
        if (value.Length == 0)
        {
            return defaultValue;
        }
        OdfLexicalValue parsed = OdfLexicalParser.ParseBoolean(value);
        return parsed.IsValid ? parsed.BooleanValue : defaultValue;
    }

    static long AddSaturated(long value, long count)
    {
        return count > long.MaxValue - value
            ? long.MaxValue
            : value + count;
    }

    static bool IsTableElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Table
            && node.Name.LocalName == localName;
    }
}
