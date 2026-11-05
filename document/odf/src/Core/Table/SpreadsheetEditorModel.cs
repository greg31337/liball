namespace odf;

internal static class SpreadsheetEditorModel
{
    internal static OdfTable[] GetSheets(OdfDocument document)
    {
        OdfNode spreadsheet = RequireSpreadsheet(document);
        int count = 0;
        int index = 0;
        while (index < document.Tables.Length)
        {
            OdfTable table = document.Tables[index];
            if (table.Node != null && table.Node.Parent == spreadsheet)
            {
                count = count + 1;
            }
            index = index + 1;
        }
        OdfTable[] sheets = new OdfTable[count];
        index = 0;
        int destination = 0;
        while (index < document.Tables.Length)
        {
            OdfTable table = document.Tables[index];
            if (table.Node != null && table.Node.Parent == spreadsheet)
            {
                sheets[destination] = table;
                destination = destination + 1;
            }
            index = index + 1;
        }
        return sheets;
    }

    internal static OdfTable? FindSheet(
        OdfDocument document,
        string name)
    {
        OdfTable[] sheets = GetSheets(document);
        OdfTable? result = null;
        int count = 0;
        int index = 0;
        while (index < sheets.Length)
        {
            if (sheets[index].Name == name)
            {
                result = sheets[index];
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
    }

    internal static OdfTable CreateSheet(
        OdfDocument document,
        string name,
        long rowCount,
        long columnCount)
    {
        OdfNode spreadsheet = RequireSpreadsheet(document);
        RequireNewSheetName(document, name);
        if (rowCount <= 0 || columnCount <= 0)
        {
            throw new OdfEditException(
                "A new sheet requires positive row and column counts.");
        }
        OdfNode table = CreateTableElement("table", spreadsheet);
        StyleDocumentModel.WriteAttribute(
            table,
            OdfNamespaces.Table,
            "name",
            OdfNamespaces.TablePrefix,
            name);
        OdfNode column = CreateTableElement("table-column", table);
        WriteRepeatCount(column, "number-columns-repeated", columnCount);
        StyleDocumentModel.AppendNode(table, column);
        OdfNode row = CreateTableElement("table-row", table);
        WriteRepeatCount(row, "number-rows-repeated", rowCount);
        OdfNode cell = CreateTableElement("table-cell", row);
        WriteRepeatCount(cell, "number-columns-repeated", columnCount);
        StyleDocumentModel.AppendNode(row, cell);
        StyleDocumentModel.AppendNode(table, row);
        InsertSheetNode(spreadsheet, table);
        ReloadTablesPreservingSpreadsheetData(document);
        OdfTable? created = FindSheet(document, name);
        if (created == null)
        {
            throw new OdfEditException("The new sheet could not be materialized.");
        }
        return created;
    }

    internal static void DeleteSheet(
        OdfDocument document,
        OdfTable sheet)
    {
        OdfNode spreadsheet = RequireSpreadsheet(document);
        RequireOwnedSheet(sheet, spreadsheet);
        OdfNode node = sheet.Node ?? new OdfNode();
        TableReferenceUpdater.DeleteSheetReferences(document, sheet.Name);
        RemoveOwnedScenarios(document, sheet);
        StyleDocumentModel.RemoveNodeFromParent(node);
        if (document.SpreadsheetSettings.ActiveTable == sheet.Name)
        {
            document.SpreadsheetSettings.ActiveTable = "";
        }
        ReloadTablesPreservingSpreadsheetData(document);
    }

    internal static void RenameSheet(
        OdfDocument document,
        OdfTable sheet,
        string newName)
    {
        OdfNode spreadsheet = RequireSpreadsheet(document);
        RequireOwnedSheet(sheet, spreadsheet);
        if (newName == null || newName.Length == 0)
        {
            throw new OdfEditException("A non-empty sheet name is required.");
        }
        OdfTable? conflict = FindSheet(document, newName);
        if (conflict != null && conflict != sheet)
        {
            throw new OdfEditException(
                $"A sheet named '{newName}' already exists.");
        }
        string oldName = sheet.Name;
        TableReferenceUpdater.RenameSheetReferences(
            document,
            oldName,
            newName);
        sheet.Name = newName;
        StyleDocumentModel.WriteAttribute(
            sheet.Node ?? new OdfNode(),
            OdfNamespaces.Table,
            "name",
            OdfNamespaces.TablePrefix,
            newName);
        if (document.SpreadsheetSettings.ActiveTable == oldName)
        {
            document.SpreadsheetSettings.ActiveTable = newName;
        }
        ReloadTablesPreservingSpreadsheetData(document);
    }

    internal static OdfTable MoveSheet(
        OdfDocument document,
        OdfTable sheet,
        long destinationIndex)
    {
        OdfNode spreadsheet = RequireSpreadsheet(document);
        RequireOwnedSheet(sheet, spreadsheet);
        OdfTable[] sheets = GetSheets(document);
        if (destinationIndex < 0 || destinationIndex >= sheets.Length)
        {
            throw new OdfEditException(
                "The destination sheet index is outside the document.");
        }
        int sourceIndex = FindSheetIndex(sheets, sheet);
        if (sourceIndex < 0)
        {
            throw new OdfEditException("The sheet is not part of the document.");
        }
        OdfNode[] ordered = SheetNodes(sheets);
        OdfNode moving = ordered[sourceIndex];
        if (sourceIndex < destinationIndex)
        {
            int index = sourceIndex;
            while (index < destinationIndex)
            {
                ordered[index] = ordered[index + 1];
                index = index + 1;
            }
        }
        else
        {
            int index = sourceIndex;
            while (index > destinationIndex)
            {
                ordered[index] = ordered[index - 1];
                index = index - 1;
            }
        }
        ordered[(int)destinationIndex] = moving;
        ReplaceSheetPositions(spreadsheet, ordered);
        string name = sheet.Name;
        ReloadTablesPreservingSpreadsheetData(document);
        OdfTable? moved = FindSheet(document, name);
        if (moved == null)
        {
            throw new OdfEditException("The moved sheet could not be materialized.");
        }
        return moved;
    }

    static OdfNode RequireSpreadsheet(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for spreadsheet editing.");
        }
        SpreadsheetDataState? state = document.SpreadsheetState;
        if (state != null && state.SpreadsheetNode != null)
        {
            return state.SpreadsheetNode;
        }
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfNode? root = document.Parts[partIndex].Root;
            OdfNode? spreadsheet = FindSpreadsheet(root);
            if (spreadsheet != null)
            {
                return spreadsheet;
            }
            partIndex = partIndex + 1;
        }
        throw new OdfEditException(
            "Spreadsheet editing requires an office:spreadsheet body.");
    }

    static OdfNode? FindSpreadsheet(OdfNode? node)
    {
        if (node == null)
        {
            return null;
        }
        if (node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Office
            && node.Name.LocalName == "spreadsheet")
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode? result = FindSpreadsheet(node.Children[index]);
            if (result != null)
            {
                return result;
            }
            index = index + 1;
        }
        return null;
    }

    static void RequireNewSheetName(OdfDocument document, string name)
    {
        if (name == null || name.Length == 0)
        {
            throw new OdfEditException("A non-empty sheet name is required.");
        }
        if (FindSheet(document, name) != null)
        {
            throw new OdfEditException(
                $"A sheet named '{name}' already exists.");
        }
    }

    static void RequireOwnedSheet(OdfTable sheet, OdfNode spreadsheet)
    {
        if (sheet == null || sheet.Node == null || sheet.Node.Parent != spreadsheet)
        {
            throw new OdfEditException(
                "The selected table is not a sheet in this document.");
        }
    }

    static OdfNode CreateTableElement(string localName, OdfNode parent)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = new OdfQualifiedName();
        result.Name.NamespaceName = OdfNamespaces.Table;
        result.Name.LocalName = localName;
        result.Name.Prefix = OdfNamespaces.TablePrefix;
        result.Location.PartPath = parent.Location.PartPath;
        result.WasEmptyElement = true;
        return result;
    }

    static void WriteRepeatCount(
        OdfNode node,
        string localName,
        long count)
    {
        if (count > 1)
        {
            StyleDocumentModel.WriteAttribute(
                node,
                OdfNamespaces.Table,
                localName,
                OdfNamespaces.TablePrefix,
                count.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    static void InsertSheetNode(OdfNode spreadsheet, OdfNode sheet)
    {
        int insertion = spreadsheet.Children.Length;
        int index = 0;
        while (index < spreadsheet.Children.Length)
        {
            OdfNode child = spreadsheet.Children[index];
            if (IsPostSheetFeature(child))
            {
                insertion = index;
                index = spreadsheet.Children.Length;
            }
            else
            {
                index = index + 1;
            }
        }
        InsertChild(spreadsheet, sheet, insertion);
    }

    static bool IsPostSheetFeature(OdfNode node)
    {
        if (node.Kind != OdfNodeKind.Element
            || node.Name == null
            || node.Name.NamespaceName != OdfNamespaces.Table)
        {
            return false;
        }
        string name = node.Name.LocalName;
        return name == "named-expressions"
            || name == "database-ranges"
            || name == "data-pilot-tables"
            || name == "consolidation"
            || name == "dde-links";
    }

    static void InsertChild(
        OdfNode parent,
        OdfNode child,
        int insertion)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        int index = 0;
        while (index < insertion)
        {
            children[index] = parent.Children[index];
            index = index + 1;
        }
        children[insertion] = child;
        index = insertion;
        while (index < parent.Children.Length)
        {
            children[index + 1] = parent.Children[index];
            index = index + 1;
        }
        parent.Children = children;
        child.Parent = parent;
        parent.WasEmptyElement = false;
    }

    static int FindSheetIndex(OdfTable[] sheets, OdfTable target)
    {
        int index = 0;
        while (index < sheets.Length)
        {
            if (sheets[index] == target
                || (target.Node != null && sheets[index].Node == target.Node))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static OdfNode[] SheetNodes(OdfTable[] sheets)
    {
        OdfNode[] nodes = new OdfNode[sheets.Length];
        int index = 0;
        while (index < sheets.Length)
        {
            nodes[index] = sheets[index].Node ?? new OdfNode();
            index = index + 1;
        }
        return nodes;
    }

    static void ReplaceSheetPositions(
        OdfNode spreadsheet,
        OdfNode[] ordered)
    {
        int sheetIndex = 0;
        int index = 0;
        while (index < spreadsheet.Children.Length)
        {
            OdfNode child = spreadsheet.Children[index];
            if (child.Kind == OdfNodeKind.Element
                && child.Name != null
                && child.Name.NamespaceName == OdfNamespaces.Table
                && child.Name.LocalName == "table")
            {
                spreadsheet.Children[index] = ordered[sheetIndex];
                ordered[sheetIndex].Parent = spreadsheet;
                sheetIndex = sheetIndex + 1;
            }
            index = index + 1;
        }
    }

    static void RemoveOwnedScenarios(OdfDocument document, OdfTable sheet)
    {
        int count = 0;
        int index = 0;
        while (index < document.Scenarios.Length)
        {
            if (document.Scenarios[index].Table != sheet)
            {
                count = count + 1;
            }
            index = index + 1;
        }
        OdfScenario[] scenarios = new OdfScenario[count];
        index = 0;
        int destination = 0;
        while (index < document.Scenarios.Length)
        {
            OdfScenario scenario = document.Scenarios[index];
            if (scenario.Table != sheet)
            {
                scenarios[destination] = scenario;
                destination = destination + 1;
            }
            index = index + 1;
        }
        document.Scenarios = scenarios;
    }

    static void ReloadTablesPreservingSpreadsheetData(OdfDocument document)
    {
        OdfSpreadsheetSettings settings = document.SpreadsheetSettings;
        OdfTableTemplate[] templates = document.TableTemplates;
        OdfNamedExpression[] named = document.NamedExpressions;
        OdfLabelRange[] labels = document.LabelRanges;
        OdfDatabaseRange[] databases = document.DatabaseRanges;
        OdfConsolidation[] consolidations = document.Consolidations;
        OdfScenario[] scenarios = document.Scenarios;
        OdfContentValidation[] validations = document.ContentValidations;
        OdfConditionalStyle[] conditional = document.ConditionalStyles;
        OdfPivotTable[] pivots = document.PivotTables;
        OdfDetectiveRange[] detectiveRanges = document.DetectiveRanges;
        OdfDetectiveOperation[] detectiveOperations =
            document.DetectiveOperations;
        OdfSpreadsheetChange[] changes = document.SpreadsheetChanges;
        bool trackChanges = document.TrackSpreadsheetChanges;
        TableDocumentModel.LoadTables(document);
        document.SpreadsheetSettings = settings;
        document.TableTemplates = templates;
        document.NamedExpressions = named;
        document.LabelRanges = labels;
        document.DatabaseRanges = databases;
        document.Consolidations = consolidations;
        document.Scenarios = scenarios;
        document.ContentValidations = validations;
        document.ConditionalStyles = conditional;
        document.PivotTables = pivots;
        document.DetectiveRanges = detectiveRanges;
        document.DetectiveOperations = detectiveOperations;
        document.SpreadsheetChanges = changes;
        document.TrackSpreadsheetChanges = trackChanges;
        RebindScenarioTables(document);
        RebindDetectiveCells(document);
    }

    static void RebindScenarioTables(OdfDocument document)
    {
        int index = 0;
        while (index < document.Scenarios.Length)
        {
            OdfScenario scenario = document.Scenarios[index];
            if (scenario.Table != null && scenario.Table.Node != null)
            {
                scenario.Table = FindTableByNode(
                    document,
                    scenario.Table.Node);
            }
            index = index + 1;
        }
    }

    static OdfTable? FindTableByNode(OdfDocument document, OdfNode node)
    {
        int index = 0;
        while (index < document.Tables.Length)
        {
            if (document.Tables[index].Node == node)
            {
                return document.Tables[index];
            }
            index = index + 1;
        }
        return null;
    }

    static void RebindDetectiveCells(OdfDocument document)
    {
        int index = 0;
        while (index < document.DetectiveRanges.Length)
        {
            OdfDetectiveRange item = document.DetectiveRanges[index];
            if (item.Cell != null && item.Cell.Node != null)
            {
                item.Cell = FindCellByNode(document, item.Cell.Node);
            }
            index = index + 1;
        }
        index = 0;
        while (index < document.DetectiveOperations.Length)
        {
            OdfDetectiveOperation item = document.DetectiveOperations[index];
            if (item.Cell != null && item.Cell.Node != null)
            {
                item.Cell = FindCellByNode(document, item.Cell.Node);
            }
            index = index + 1;
        }
    }

    static OdfCell? FindCellByNode(OdfDocument document, OdfNode node)
    {
        int tableIndex = 0;
        while (tableIndex < document.Tables.Length)
        {
            OdfTable table = document.Tables[tableIndex];
            int rowIndex = 0;
            while (rowIndex < table.Rows.Length)
            {
                OdfRow row = table.Rows[rowIndex];
                int cellIndex = 0;
                while (cellIndex < row.Cells.Length)
                {
                    if (row.Cells[cellIndex].Node == node)
                    {
                        return row.Cells[cellIndex];
                    }
                    cellIndex = cellIndex + 1;
                }
                rowIndex = rowIndex + 1;
            }
            tableIndex = tableIndex + 1;
        }
        return null;
    }
}
