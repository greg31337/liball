using System;
using System.Collections.Generic;

namespace htmlgui;

internal class TableTemplateExpander
{
    internal bool IsTableControlName(string name)
    {
        switch (name)
        {
            case "table":
            case "data-table":
            case "grid":
            case "data-grid":
                return true;
            default:
                return false;
        }
    }

    internal DomNode ExpandTable(UiNode node, DomNode?[][] cellContents, DomNode?[] detailContents)
    {
        UiTable table = RequireTable(node);
        ValidateTable(node, table, cellContents, detailContents);
        bool interactive = table.IsInteractive || node.Name == "grid" || node.Name == "data-grid";
        List<DomAttribute> attributes = CreateRootAttributes(node, table, interactive);
        List<DomNode> children = new List<DomNode>();
        if (table.Caption != null && table.Caption.Length > 0)
        {
            children.Add(CreateCaption(node, table.Caption));
        }

        children.Add(CreateColumnGroup(node, table));
        int[] rowIndex = new int[] { 1 };
        children.Add(CreateTableHead(node, table, interactive, rowIndex));
        AddTableBodies(node, table, cellContents, detailContents, interactive, rowIndex, children);
        if (table.Summaries.Length > 0)
        {
            children.Add(CreateTableFoot(node, table, interactive, rowIndex));
        }

        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = GetNodeKey(node),
            Id = GetNodeId(node),
            Name = HtmlElements.Table,
            Attributes = attributes.ToArray(),
            Children = children.ToArray(),
            Version = GetNodeVersion(node)
        };
    }

    internal UiTable RequireTable(UiNode node)
    {
        UiTable? table = node.Table;
        if (table == null)
        {
            throw CreateRenderException(node, "A table control requires a table description.");
        }

        if (string.IsNullOrWhiteSpace(table.Key))
        {
            throw CreateRenderException(node, "Tables require a non-empty stable key.");
        }

        return table;
    }

    internal void ValidateTable(UiNode node, UiTable table, DomNode?[][] cellContents, DomNode?[] detailContents)
    {
        UiTableColumn[] columns = ValidateColumns(node, table);
        UiTableRow[] rows = ValidateRows(node, table, columns, cellContents, detailContents);
        ValidateSummaries(node, table, columns);
        ValidateFilters(node, table, columns);
        ValidateGroups(node, table, rows);
        ValidateSelection(node, table, rows);
        ValidateEditState(node, table, rows);
    }

    internal UiTableColumn[] ValidateColumns(UiNode node, UiTable table)
    {
        UiTableColumn[]? columns = table.Columns;
        if (columns == null)
        {
            throw CreateRenderException(node, "Table columns cannot be null.");
        }

        int index = 0;
        while (index < columns.Length)
        {
            UiTableColumn? column = columns[index];
            if (column == null || string.IsNullOrWhiteSpace(column.Key) || column.Header == null)
            {
                throw CreateRenderException(node, "Table columns require stable keys and headers.");
            }

            if (column.FieldKey != null && string.IsNullOrWhiteSpace(column.FieldKey))
            {
                throw CreateRenderException(node, "A supplied table field key cannot be empty.");
            }

            ValidateSortDirection(column.SortDirection);
            if (!column.IsSortable && column.SortDirection != UiSortDirection.None)
            {
                throw CreateRenderException(node, "A sorted column must permit sorting.");
            }

            if (column.Width != null)
            {
                new LayoutExpander().FormatOptionalLength(column.Width);
            }

            int earlierIndex = 0;
            while (earlierIndex < index)
            {
                if (columns[earlierIndex].Key == column.Key)
                {
                    throw CreateRenderException(node, "Table column keys must be unique.");
                }

                earlierIndex++;
            }

            index++;
        }

        return columns;
    }

    internal UiTableRow[] ValidateRows(UiNode node, UiTable table, UiTableColumn[] columns, DomNode?[][] cellContents, DomNode?[] detailContents)
    {
        UiTableRow[]? rows = table.Rows;
        if (rows == null)
        {
            throw CreateRenderException(node, "Table rows cannot be null.");
        }

        if (cellContents.Length != rows.Length || detailContents.Length != rows.Length)
        {
            throw CreateRenderException(node, "Expanded table content must align with table rows.");
        }

        int rowIndex = 0;
        while (rowIndex < rows.Length)
        {
            UiTableRow? row = rows[rowIndex];
            if (row == null || string.IsNullOrWhiteSpace(row.Key) || row.Cells == null)
            {
                throw CreateRenderException(node, "Table rows require stable keys and cell collections.");
            }

            if (cellContents[rowIndex] == null || cellContents[rowIndex].Length != row.Cells.Length)
            {
                throw CreateRenderException(node, "Expanded cell content must align with each table row.");
            }

            int earlierRowIndex = 0;
            while (earlierRowIndex < rowIndex)
            {
                if (rows[earlierRowIndex].Key == row.Key)
                {
                    throw CreateRenderException(node, "Table row keys must be unique.");
                }

                earlierRowIndex++;
            }

            ValidateRowCells(node, row, columns);
            if (row.IsExpanded && (!table.HasRowDetails || row.DetailContent == null))
            {
                throw CreateRenderException(node, "An expanded table row requires enabled detail content.");
            }

            if (row.DetailContent != null && !table.HasRowDetails)
            {
                throw CreateRenderException(node, "Row detail content requires table detail support.");
            }

            rowIndex++;
        }

        return rows;
    }

    internal void ValidateRowCells(UiNode node, UiTableRow row, UiTableColumn[] columns)
    {
        bool[] occupied = new bool[columns.Length];
        int cellIndex = 0;
        while (cellIndex < row.Cells.Length)
        {
            UiTableCell? cell = row.Cells[cellIndex];
            if (cell == null || string.IsNullOrWhiteSpace(cell.ColumnKey) || cell.Value == null)
            {
                throw CreateRenderException(node, "Table cells require column keys and portable values.");
            }

            int columnIndex = FindColumnIndex(columns, cell.ColumnKey);
            if (columnIndex < 0)
            {
                throw CreateRenderException(node, "Table cells must identify declared columns.");
            }

            int span = 1;
            if (cell.HasColumnSpan)
            {
                span = cell.ColumnSpan;
                if (span < 1)
                {
                    throw CreateRenderException(node, "Table column spans must be positive.");
                }
            }

            if (cell.HasRowSpan && cell.RowSpan < 1)
            {
                throw CreateRenderException(node, "Table row spans must be positive.");
            }

            if (columnIndex + span > columns.Length)
            {
                throw CreateRenderException(node, "A table cell span cannot exceed the declared columns.");
            }

            int occupiedIndex = columnIndex;
            while (occupiedIndex < columnIndex + span)
            {
                if (occupied[occupiedIndex])
                {
                    throw CreateRenderException(node, "Table cells cannot overlap or repeat a column.");
                }

                occupied[occupiedIndex] = true;
                occupiedIndex++;
            }

            ValueJsonCodec.ValidateValue(cell.Value, 64, 10000, 1048576, 1048576);
            cellIndex++;
        }
    }

    internal void ValidateSummaries(UiNode node, UiTable table, UiTableColumn[] columns)
    {
        UiTableSummary[]? summaries = table.Summaries;
        if (summaries == null)
        {
            throw CreateRenderException(node, "Table summaries cannot be null.");
        }

        int index = 0;
        while (index < summaries.Length)
        {
            UiTableSummary? summary = summaries[index];
            if (summary == null || string.IsNullOrWhiteSpace(summary.ColumnKey) || summary.Label == null || summary.Value == null || FindColumnIndex(columns, summary.ColumnKey) < 0)
            {
                throw CreateRenderException(node, "Table summaries require declared columns, labels, and portable values.");
            }

            int earlierIndex = 0;
            while (earlierIndex < index)
            {
                if (summaries[earlierIndex].ColumnKey == summary.ColumnKey)
                {
                    throw CreateRenderException(node, "Only one table summary is permitted per column.");
                }

                earlierIndex++;
            }

            ValueJsonCodec.ValidateValue(summary.Value, 64, 10000, 1048576, 1048576);
            index++;
        }
    }

    internal void ValidateFilters(UiNode node, UiTable table, UiTableColumn[] columns)
    {
        UiFilter[]? filters = table.Filters;
        if (filters == null)
        {
            throw CreateRenderException(node, "Table filters cannot be null.");
        }

        int index = 0;
        while (index < filters.Length)
        {
            UiFilter? filter = filters[index];
            if (filter == null || string.IsNullOrWhiteSpace(filter.Key) || string.IsNullOrWhiteSpace(filter.Operator) || filter.Value == null || FindFilterColumnIndex(columns, filter.Key) < 0)
            {
                throw CreateRenderException(node, "Table filters require declared columns, operators, and portable operands.");
            }

            ValueJsonCodec.ValidateValue(filter.Value, 64, 10000, 1048576, 1048576);
            index++;
        }
    }

    internal void ValidateGroups(UiNode node, UiTable table, UiTableRow[] rows)
    {
        UiGroup[]? groups = table.Groups;
        if (groups == null)
        {
            throw CreateRenderException(node, "Table groups cannot be null.");
        }

        int groupIndex = 0;
        while (groupIndex < groups.Length)
        {
            UiGroup? group = groups[groupIndex];
            if (group == null || string.IsNullOrWhiteSpace(group.Key) || group.Label == null || group.ItemKeys == null)
            {
                throw CreateRenderException(node, "Table groups require keys, labels, and row-key collections.");
            }

            int earlierGroupIndex = 0;
            while (earlierGroupIndex < groupIndex)
            {
                if (groups[earlierGroupIndex].Key == group.Key)
                {
                    throw CreateRenderException(node, "Table group keys must be unique.");
                }

                earlierGroupIndex++;
            }

            int keyIndex = 0;
            while (keyIndex < group.ItemKeys.Length)
            {
                string? rowKey = group.ItemKeys[keyIndex];
                if (string.IsNullOrWhiteSpace(rowKey) || FindRowIndex(rows, rowKey) < 0)
                {
                    throw CreateRenderException(node, "Table group row keys must identify table rows.");
                }

                int earlierKeyIndex = 0;
                while (earlierKeyIndex < keyIndex)
                {
                    if (group.ItemKeys[earlierKeyIndex] == rowKey)
                    {
                        throw CreateRenderException(node, "Table group row keys must be unique.");
                    }

                    earlierKeyIndex++;
                }

                int otherGroupIndex = 0;
                while (otherGroupIndex < groupIndex)
                {
                    if (ContainsKey(groups[otherGroupIndex].ItemKeys, rowKey))
                    {
                        throw CreateRenderException(node, "A table row cannot belong to more than one group.");
                    }

                    otherGroupIndex++;
                }

                keyIndex++;
            }

            groupIndex++;
        }
    }

    internal void ValidateSelection(UiNode node, UiTable table, UiTableRow[] rows)
    {
        UiSelection? selection = table.Selection;
        if (selection == null || selection.SelectedKeys == null)
        {
            throw CreateRenderException(node, "Tables require complete row-selection state.");
        }

        ValidateSelectionMode(selection.Mode);
        int selectedCount = 0;
        int rowIndex = 0;
        while (rowIndex < rows.Length)
        {
            if (rows[rowIndex].IsSelected || ContainsKey(selection.SelectedKeys, rows[rowIndex].Key))
            {
                selectedCount++;
            }

            rowIndex++;
        }

        int keyIndex = 0;
        while (keyIndex < selection.SelectedKeys.Length)
        {
            string? key = selection.SelectedKeys[keyIndex];
            if (string.IsNullOrWhiteSpace(key) || FindRowIndex(rows, key) < 0)
            {
                throw CreateRenderException(node, "Selected table keys must identify rows.");
            }

            int earlierIndex = 0;
            while (earlierIndex < keyIndex)
            {
                if (selection.SelectedKeys[earlierIndex] == key)
                {
                    throw CreateRenderException(node, "Selected table row keys must be unique.");
                }

                earlierIndex++;
            }

            keyIndex++;
        }

        if (selection.Mode == UiSelectionMode.None && selectedCount > 0)
        {
            throw CreateRenderException(node, "Selection-disabled tables cannot contain selected rows.");
        }

        if (selection.Mode == UiSelectionMode.Single && selectedCount > 1)
        {
            throw CreateRenderException(node, "Single-selection tables permit at most one selected row.");
        }

        if (selection.ActiveKey != null && FindRowIndex(rows, selection.ActiveKey) < 0)
        {
            throw CreateRenderException(node, "The active table key must identify a row.");
        }

        if (selection.AnchorKey != null && (selection.Mode != UiSelectionMode.Range || FindRowIndex(rows, selection.AnchorKey) < 0))
        {
            throw CreateRenderException(node, "A table range anchor must identify a row.");
        }
    }

    internal void ValidateEditState(UiNode node, UiTable table, UiTableRow[] rows)
    {
        UiEditState? edit = table.EditState;
        if (edit == null)
        {
            return;
        }

        int rowIndex = FindRowIndex(rows, edit.RowKey);
        if (rowIndex < 0 || string.IsNullOrWhiteSpace(edit.ColumnKey) || edit.OriginalValue == null || edit.CurrentValue == null)
        {
            throw CreateRenderException(node, "A table edit must identify a row, column, and portable values.");
        }

        int cellIndex = FindCellIndex(rows[rowIndex].Cells, edit.ColumnKey);
        if (cellIndex < 0 || !rows[rowIndex].Cells[cellIndex].IsEditable)
        {
            throw CreateRenderException(node, "A table edit must identify an editable cell.");
        }

        ValueJsonCodec.ValidateValue(edit.OriginalValue, 64, 10000, 1048576, 1048576);
        ValueJsonCodec.ValidateValue(edit.CurrentValue, 64, 10000, 1048576, 1048576);
    }

    internal List<DomAttribute> CreateRootAttributes(UiNode node, UiTable table, bool interactive)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = interactive ? "htmlgui-table htmlgui-data-grid" : "htmlgui-table" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "selection-mode", Value = GetSelectionModeToken(table.Selection.Mode) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "table-key", Value = table.Key });
        AddFilterAttributes(table, attributes);
        if (table.EditState != null)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "edit-column", Value = table.EditState.ColumnKey });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "edit-row", Value = table.EditState.RowKey });
        }

        if (interactive)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.ColumnCount, Value = InvariantFormatter.FormatInt32(table.Columns.Length) });
            attributes.Add(new DomAttribute { Name = AriaAttributes.RowCount, Value = InvariantFormatter.FormatInt32(CountRenderedRows(table)) });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Grid });
            if (table.Selection.Mode == UiSelectionMode.Multiple || table.Selection.Mode == UiSelectionMode.Range)
            {
                attributes.Add(new DomAttribute { Name = AriaAttributes.MultiSelectable, Value = "true" });
            }

            string tabIndex = "0";
            if (node.State != null && node.State.HasTabIndex)
            {
                tabIndex = InvariantFormatter.FormatInt32(node.State.TabIndex);
            }
            else if (node.State != null && !node.State.IsEnabled)
            {
                tabIndex = "-1";
            }

            attributes.Add(new DomAttribute { Name = HtmlAttributes.TabIndex, Value = tabIndex });
            if (table.Selection.ActiveKey != null)
            {
                attributes.Add(new DomAttribute { Name = AriaAttributes.ActiveDescendant, Value = CreateSyntheticId(node, "row", table.Selection.ActiveKey) });
            }
        }

        if ((node.State != null && node.State.IsBusy) || (table.EditState != null && table.EditState.IsPending))
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Busy, Value = "true" });
        }

        if (node.State != null && !node.State.IsEnabled)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Disabled, Value = "true" });
        }

        if (node.State != null && !node.State.IsVisible)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }

        SortAttributes(attributes);
        return attributes;
    }

    internal void AddFilterAttributes(UiTable table, List<DomAttribute> attributes)
    {
        int index = 0;
        while (index < table.Filters.Length)
        {
            UiFilter filter = table.Filters[index];
            string prefix = HtmlAttributes.DataPrefix + "filter-" + InvariantFormatter.FormatInt32(index) + "-";
            attributes.Add(new DomAttribute { Name = prefix + "ignore-case", Value = FormatBoolean(filter.IgnoreCase) });
            attributes.Add(new DomAttribute { Name = prefix + "key", Value = filter.Key });
            attributes.Add(new DomAttribute { Name = prefix + "operator", Value = filter.Operator });
            attributes.Add(new DomAttribute { Name = prefix + "value", Value = FormatValue(filter.Value) });
            index++;
        }
    }

    internal DomNode CreateCaption(UiNode node, string caption)
    {
        return CreateTextElement(node, "$caption", "caption", "caption", HtmlElements.Caption, caption, "htmlgui-table-caption");
    }

    internal DomNode CreateColumnGroup(UiNode node, UiTable table)
    {
        DomNode[] children = new DomNode[table.Columns.Length];
        int index = 0;
        while (index < table.Columns.Length)
        {
            UiTableColumn column = table.Columns[index];
            List<DomAttribute> attributes = CreateColumnAttributes(column, false);
            children[index] = new DomNode
            {
                Kind = DomNodeKind.Element,
                Key = column.Key,
                Id = CreateSyntheticId(node, "column", column.Key),
                Name = HtmlElements.Column,
                Attributes = attributes.ToArray(),
                Version = GetNodeVersion(node)
            };
            index++;
        }

        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$columns",
            Id = CreateSyntheticId(node, "columns", table.Key),
            Name = HtmlElements.ColumnGroup,
            Children = children,
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateTableHead(UiNode node, UiTable table, bool interactive, int[] rowIndex)
    {
        DomNode[] headers = new DomNode[table.Columns.Length];
        int index = 0;
        while (index < table.Columns.Length)
        {
            UiTableColumn column = table.Columns[index];
            List<DomAttribute> attributes = CreateColumnAttributes(column, true);
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Scope, Value = "col" });
            if (column.IsSortable)
            {
                attributes.Add(new DomAttribute { Name = AriaAttributes.Sort, Value = GetSortDirectionToken(column.SortDirection) });
            }

            if (column.IsReorderable)
            {
                attributes.Add(new DomAttribute { Name = "draggable", Value = "true" });
            }

            if (interactive)
            {
                attributes.Add(new DomAttribute { Name = AriaAttributes.ColumnIndex, Value = InvariantFormatter.FormatInt32(index + 1) });
                attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.ColumnHeader });
                if (column.IsSortable || column.IsResizable || column.IsReorderable)
                {
                    attributes.Add(new DomAttribute { Name = HtmlAttributes.TabIndex, Value = "-1" });
                }
            }

            SortAttributes(attributes);
            headers[index] = CreateCellElement(node, "$header-" + column.Key, "header", column.Key, HtmlElements.TableHeader, attributes, column.Header);
            index++;
        }

        List<DomAttribute> rowAttributes = new List<DomAttribute>();
        rowAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-table-header-row" });
        if (interactive)
        {
            rowAttributes.Add(new DomAttribute { Name = AriaAttributes.RowIndex, Value = InvariantFormatter.FormatInt32(rowIndex[0]) });
            rowAttributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Row });
        }

        SortAttributes(rowAttributes);
        rowIndex[0]++;
        DomNode row = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$header-row",
            Id = CreateSyntheticId(node, "header-row", table.Key),
            Name = HtmlElements.TableRow,
            Attributes = rowAttributes.ToArray(),
            Children = headers,
            Version = GetNodeVersion(node)
        };
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$head",
            Id = CreateSyntheticId(node, "head", table.Key),
            Name = HtmlElements.TableHead,
            Children = new DomNode[] { row },
            Version = GetNodeVersion(node)
        };
    }

    internal List<DomAttribute> CreateColumnAttributes(UiTableColumn column, bool header)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        string className = header ? "htmlgui-table-header" : "htmlgui-table-column";
        if (column.IsFrozen)
        {
            className += " htmlgui-table-frozen";
        }

        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = className });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "column-key", Value = column.Key });
        if (column.FieldKey != null)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "field-key", Value = column.FieldKey });
        }

        if (column.IsFrozen)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "frozen", Value = "true" });
        }

        if (column.IsResizable)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "resizable", Value = "true" });
        }

        if (column.IsReorderable)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "reorderable", Value = "true" });
        }

        if (column.IsSortable)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "sortable", Value = "true" });
        }

        if (column.IsHidden)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }

        string width = new LayoutExpander().FormatOptionalLength(column.Width);
        if (width.Length > 0)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Style, Value = CssProperties.Width + ":" + width + ";" });
        }

        SortAttributes(attributes);
        return attributes;
    }

    internal void AddTableBodies(UiNode node, UiTable table, DomNode?[][] cellContents, DomNode?[] detailContents, bool interactive, int[] rowIndex, List<DomNode> result)
    {
        bool[] grouped = new bool[table.Rows.Length];
        int groupIndex = 0;
        while (groupIndex < table.Groups.Length)
        {
            UiGroup group = table.Groups[groupIndex];
            List<DomNode> groupRows = new List<DomNode>();
            groupRows.Add(CreateGroupHeader(node, table, group, interactive, rowIndex));
            int keyIndex = 0;
            while (keyIndex < group.ItemKeys.Length)
            {
                int sourceIndex = FindRowIndex(table.Rows, group.ItemKeys[keyIndex]);
                grouped[sourceIndex] = true;
                AddRowAndDetail(node, table, sourceIndex, cellContents[sourceIndex], detailContents[sourceIndex], interactive, !group.IsExpanded, rowIndex, groupRows);
                keyIndex++;
            }

            result.Add(CreateBody(node, group.Key, "htmlgui-table-group", groupRows.ToArray(), interactive, group));
            groupIndex++;
        }

        List<DomNode> ungroupedRows = new List<DomNode>();
        int rowSourceIndex = 0;
        while (rowSourceIndex < table.Rows.Length)
        {
            if (!grouped[rowSourceIndex])
            {
                AddRowAndDetail(node, table, rowSourceIndex, cellContents[rowSourceIndex], detailContents[rowSourceIndex], interactive, false, rowIndex, ungroupedRows);
            }

            rowSourceIndex++;
        }

        if (ungroupedRows.Count > 0 || table.Groups.Length == 0)
        {
            result.Add(CreateBody(node, "$body", "htmlgui-table-body", ungroupedRows.ToArray(), interactive, null));
        }
    }

    internal DomNode CreateBody(UiNode node, string key, string className, DomNode[] rows, bool interactive, UiGroup? group)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = className });
        if (group != null)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "expanded", Value = FormatBoolean(group.IsExpanded) });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "group-key", Value = group.Key });
        }

        if (interactive)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.RowGroup });
        }

        SortAttributes(attributes);
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = key,
            Id = CreateSyntheticId(node, "body", key),
            Name = HtmlElements.TableBody,
            Attributes = attributes.ToArray(),
            Children = rows,
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateGroupHeader(UiNode node, UiTable table, UiGroup group, bool interactive, int[] rowIndex)
    {
        List<DomAttribute> cellAttributes = new List<DomAttribute>();
        cellAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-table-group-header" });
        cellAttributes.Add(new DomAttribute { Name = HtmlAttributes.ColumnSpan, Value = InvariantFormatter.FormatInt32(table.Columns.Length) });
        cellAttributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "group-key", Value = group.Key });
        cellAttributes.Add(new DomAttribute { Name = HtmlAttributes.Scope, Value = "rowgroup" });
        if (interactive)
        {
            cellAttributes.Add(new DomAttribute { Name = AriaAttributes.ColumnSpan, Value = InvariantFormatter.FormatInt32(table.Columns.Length) });
            cellAttributes.Add(new DomAttribute { Name = AriaAttributes.Expanded, Value = FormatBoolean(group.IsExpanded) });
            cellAttributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.RowHeader });
        }

        SortAttributes(cellAttributes);
        DomNode cell = CreateCellElement(node, "$group-cell", "group-cell", group.Key, HtmlElements.TableHeader, cellAttributes, group.Label);
        List<DomAttribute> rowAttributes = new List<DomAttribute>();
        rowAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-table-group-row" });
        if (interactive)
        {
            rowAttributes.Add(new DomAttribute { Name = AriaAttributes.RowIndex, Value = InvariantFormatter.FormatInt32(rowIndex[0]) });
            rowAttributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Row });
        }

        SortAttributes(rowAttributes);
        rowIndex[0]++;
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$group-" + group.Key,
            Id = CreateSyntheticId(node, "group-row", group.Key),
            Name = HtmlElements.TableRow,
            Attributes = rowAttributes.ToArray(),
            Children = new DomNode[] { cell },
            Version = GetNodeVersion(node)
        };
    }

    internal void AddRowAndDetail(UiNode node, UiTable table, int sourceIndex, DomNode?[] cellContents, DomNode? detailContent, bool interactive, bool hiddenByGroup, int[] rowIndex, List<DomNode> result)
    {
        UiTableRow row = table.Rows[sourceIndex];
        result.Add(CreateDataRow(node, table, row, cellContents, interactive, hiddenByGroup, rowIndex));
        if (row.DetailContent != null)
        {
            result.Add(CreateDetailRow(node, table, row, detailContent, interactive, hiddenByGroup, rowIndex));
        }
    }

    internal DomNode CreateDataRow(UiNode node, UiTable table, UiTableRow row, DomNode?[] cellContents, bool interactive, bool hiddenByGroup, int[] rowIndex)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        bool selected = row.IsSelected || ContainsKey(table.Selection.SelectedKeys, row.Key);
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-table-row" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "row-key", Value = row.Key });
        if (selected)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "selected", Value = "true" });
        }

        if (row.DetailContent != null)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Controls, Value = CreateSyntheticId(node, "detail-row", row.Key) });
            attributes.Add(new DomAttribute { Name = AriaAttributes.Expanded, Value = FormatBoolean(row.IsExpanded) });
        }

        if (row.IsDisabled || (node.State != null && !node.State.IsEnabled))
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Disabled, Value = "true" });
        }

        if (hiddenByGroup)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }

        if (table.Selection.ActiveKey == row.Key)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "active", Value = "true" });
        }

        if (interactive)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.RowIndex, Value = InvariantFormatter.FormatInt32(rowIndex[0]) });
            attributes.Add(new DomAttribute { Name = AriaAttributes.Selected, Value = FormatBoolean(selected) });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Row });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.TabIndex, Value = "-1" });
        }

        SortAttributes(attributes);
        DomNode[] cells = CreateRowCells(node, table, row, cellContents, interactive);
        rowIndex[0]++;
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = row.Key,
            Id = CreateSyntheticId(node, "row", row.Key),
            Name = HtmlElements.TableRow,
            Attributes = attributes.ToArray(),
            Children = cells,
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode[] CreateRowCells(UiNode node, UiTable table, UiTableRow row, DomNode?[] cellContents, bool interactive)
    {
        List<DomNode> result = new List<DomNode>();
        int columnIndex = 0;
        while (columnIndex < table.Columns.Length)
        {
            UiTableColumn column = table.Columns[columnIndex];
            int sourceIndex = FindCellIndex(row.Cells, column.Key);
            if (sourceIndex < 0)
            {
                result.Add(CreateEmptyCell(node, row, column, columnIndex, interactive));
                columnIndex++;
            }
            else
            {
                UiTableCell cell = row.Cells[sourceIndex];
                result.Add(CreateDataCell(node, table, row, cell, cellContents[sourceIndex], column, columnIndex, interactive));
                int span = 1;
                if (cell.HasColumnSpan)
                {
                    span = cell.ColumnSpan;
                }

                columnIndex += span;
            }
        }

        return result.ToArray();
    }

    internal DomNode CreateEmptyCell(UiNode node, UiTableRow row, UiTableColumn column, int columnIndex, bool interactive)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-table-cell htmlgui-table-cell-empty" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "column-key", Value = column.Key });
        if (column.IsHidden)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }

        if (interactive)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.ColumnIndex, Value = InvariantFormatter.FormatInt32(columnIndex + 1) });
            attributes.Add(new DomAttribute { Name = AriaAttributes.ReadOnly, Value = "true" });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.GridCell });
        }

        SortAttributes(attributes);
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$cell-" + column.Key,
            Id = CreateSyntheticId(node, "cell", row.Key + "\n" + column.Key),
            Name = HtmlElements.TableData,
            Attributes = attributes.ToArray(),
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateDataCell(UiNode node, UiTable table, UiTableRow row, UiTableCell cell, DomNode? content, UiTableColumn column, int columnIndex, bool interactive)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        string className = cell.IsHeader ? "htmlgui-table-cell htmlgui-table-row-header" : "htmlgui-table-cell";
        if (column.IsFrozen)
        {
            className += " htmlgui-table-frozen";
        }

        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = className });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "column-key", Value = cell.ColumnKey });
        if (cell.IsEditable)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "editable", Value = "true" });
        }

        if (column.IsFrozen)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "frozen", Value = "true" });
        }

        if (column.IsHidden)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }

        if (cell.HasColumnSpan)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.ColumnSpan, Value = InvariantFormatter.FormatInt32(cell.ColumnSpan) });
        }

        if (cell.HasRowSpan)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.RowSpan, Value = InvariantFormatter.FormatInt32(cell.RowSpan) });
        }

        if (interactive)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.ColumnIndex, Value = InvariantFormatter.FormatInt32(columnIndex + 1) });
            attributes.Add(new DomAttribute { Name = AriaAttributes.ReadOnly, Value = FormatBoolean(!cell.IsEditable) });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = cell.IsHeader ? AriaRoles.RowHeader : AriaRoles.GridCell });
            if (cell.HasColumnSpan)
            {
                attributes.Add(new DomAttribute { Name = AriaAttributes.ColumnSpan, Value = InvariantFormatter.FormatInt32(cell.ColumnSpan) });
            }

            if (cell.HasRowSpan)
            {
                attributes.Add(new DomAttribute { Name = AriaAttributes.RowSpan, Value = InvariantFormatter.FormatInt32(cell.RowSpan) });
            }
        }

        if (cell.IsHeader)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Scope, Value = "row" });
        }

        AddEditAttributes(table.EditState, row.Key, cell.ColumnKey, attributes);
        SortAttributes(attributes);
        DomNode[] children = CreateValueContent(node, "cell-value", row.Key + "\n" + cell.ColumnKey, cell.Value, content);
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = cell.ColumnKey,
            Id = CreateSyntheticId(node, "cell", row.Key + "\n" + cell.ColumnKey),
            Name = cell.IsHeader ? HtmlElements.TableHeader : HtmlElements.TableData,
            Attributes = attributes.ToArray(),
            Children = children,
            Version = GetNodeVersion(node)
        };
    }

    internal void AddEditAttributes(UiEditState? edit, string rowKey, string columnKey, List<DomAttribute> attributes)
    {
        if (edit == null || edit.RowKey != rowKey || edit.ColumnKey != columnKey)
        {
            return;
        }

        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "edit-current", Value = FormatValue(edit.CurrentValue) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "edit-dirty", Value = FormatBoolean(edit.IsDirty) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "edit-original", Value = FormatValue(edit.OriginalValue) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "editing", Value = "true" });
        if (edit.IsPending)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Busy, Value = "true" });
        }

        if (!edit.IsValid)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Invalid, Value = "true" });
        }
    }

    internal DomNode CreateDetailRow(UiNode node, UiTable table, UiTableRow row, DomNode? content, bool interactive, bool hiddenByGroup, int[] rowIndex)
    {
        List<DomAttribute> cellAttributes = new List<DomAttribute>();
        cellAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-table-detail-cell" });
        cellAttributes.Add(new DomAttribute { Name = HtmlAttributes.ColumnSpan, Value = InvariantFormatter.FormatInt32(table.Columns.Length) });
        if (interactive)
        {
            cellAttributes.Add(new DomAttribute { Name = AriaAttributes.ColumnSpan, Value = InvariantFormatter.FormatInt32(table.Columns.Length) });
            cellAttributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.GridCell });
        }

        SortAttributes(cellAttributes);
        DomNode[] detailChildren = Array.Empty<DomNode>();
        if (content != null)
        {
            detailChildren = new DomNode[] { content };
        }

        DomNode cell = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$detail-cell",
            Id = CreateSyntheticId(node, "detail-cell", row.Key),
            Name = HtmlElements.TableData,
            Attributes = cellAttributes.ToArray(),
            Children = detailChildren,
            Version = GetNodeVersion(node)
        };
        List<DomAttribute> rowAttributes = new List<DomAttribute>();
        rowAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-table-detail-row" });
        rowAttributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "detail-for", Value = row.Key });
        if (!row.IsExpanded || hiddenByGroup)
        {
            rowAttributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }

        if (interactive)
        {
            rowAttributes.Add(new DomAttribute { Name = AriaAttributes.RowIndex, Value = InvariantFormatter.FormatInt32(rowIndex[0]) });
            rowAttributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Row });
        }

        SortAttributes(rowAttributes);
        rowIndex[0]++;
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$detail-" + row.Key,
            Id = CreateSyntheticId(node, "detail-row", row.Key),
            Name = HtmlElements.TableRow,
            Attributes = rowAttributes.ToArray(),
            Children = new DomNode[] { cell },
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateTableFoot(UiNode node, UiTable table, bool interactive, int[] rowIndex)
    {
        DomNode[] cells = new DomNode[table.Columns.Length];
        int columnIndex = 0;
        while (columnIndex < table.Columns.Length)
        {
            UiTableColumn column = table.Columns[columnIndex];
            UiTableSummary? summary = FindSummary(table.Summaries, column.Key);
            List<DomAttribute> attributes = new List<DomAttribute>();
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-table-summary" });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "column-key", Value = column.Key });
            if (column.IsHidden)
            {
                attributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
            }

            if (interactive)
            {
                attributes.Add(new DomAttribute { Name = AriaAttributes.ColumnIndex, Value = InvariantFormatter.FormatInt32(columnIndex + 1) });
                attributes.Add(new DomAttribute { Name = AriaAttributes.ReadOnly, Value = "true" });
                attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.GridCell });
            }

            string text = string.Empty;
            if (summary != null)
            {
                attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "summary-label", Value = summary.Label });
                string value = FormatValue(summary.Value);
                text = value;
                if (summary.Label.Length > 0 && value.Length > 0)
                {
                    text = summary.Label + ": " + value;
                }
                else if (summary.Label.Length > 0)
                {
                    text = summary.Label;
                }
            }

            SortAttributes(attributes);
            cells[columnIndex] = CreateCellElement(node, "$summary-" + column.Key, "summary", column.Key, HtmlElements.TableData, attributes, text);
            columnIndex++;
        }

        List<DomAttribute> rowAttributes = new List<DomAttribute>();
        rowAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-table-summary-row" });
        if (interactive)
        {
            rowAttributes.Add(new DomAttribute { Name = AriaAttributes.RowIndex, Value = InvariantFormatter.FormatInt32(rowIndex[0]) });
            rowAttributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Row });
        }

        SortAttributes(rowAttributes);
        rowIndex[0]++;
        DomNode row = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$summary-row",
            Id = CreateSyntheticId(node, "summary-row", table.Key),
            Name = HtmlElements.TableRow,
            Attributes = rowAttributes.ToArray(),
            Children = cells,
            Version = GetNodeVersion(node)
        };
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$foot",
            Id = CreateSyntheticId(node, "foot", table.Key),
            Name = HtmlElements.TableFoot,
            Children = new DomNode[] { row },
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateCellElement(UiNode node, string key, string suffix, string identity, string name, List<DomAttribute> attributes, string text)
    {
        DomNode[] children = Array.Empty<DomNode>();
        if (text.Length > 0)
        {
            children = new DomNode[]
            {
                new DomNode
                {
                    Kind = DomNodeKind.Text,
                    Key = "$text",
                    Id = CreateSyntheticId(node, suffix + "-text", identity),
                    Text = text,
                    Version = GetNodeVersion(node)
                }
            };
        }

        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = key,
            Id = CreateSyntheticId(node, suffix, identity),
            Name = name,
            Attributes = attributes.ToArray(),
            Children = children,
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateTextElement(UiNode node, string key, string suffix, string identity, string name, string text, string className)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = className });
        return CreateCellElement(node, key, suffix, identity, name, attributes, text);
    }

    internal DomNode[] CreateValueContent(UiNode node, string suffix, string identity, UiValue value, DomNode? content)
    {
        if (content != null)
        {
            return new DomNode[] { content };
        }

        string text = FormatValue(value);
        if (text.Length == 0)
        {
            return Array.Empty<DomNode>();
        }

        return new DomNode[]
        {
            new DomNode
            {
                Kind = DomNodeKind.Text,
                Key = "$value",
                Id = CreateSyntheticId(node, suffix, identity),
                Text = text,
                Version = GetNodeVersion(node)
            }
        };
    }

    internal int CountRenderedRows(UiTable table)
    {
        int count = 1 + table.Groups.Length + table.Rows.Length;
        int rowIndex = 0;
        while (rowIndex < table.Rows.Length)
        {
            if (table.Rows[rowIndex].DetailContent != null)
            {
                count++;
            }

            rowIndex++;
        }

        if (table.Summaries.Length > 0)
        {
            count++;
        }

        return count;
    }

    internal int FindColumnIndex(UiTableColumn[] columns, string key)
    {
        int index = 0;
        while (index < columns.Length)
        {
            if (columns[index].Key == key)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal int FindFilterColumnIndex(UiTableColumn[] columns, string key)
    {
        int index = 0;
        while (index < columns.Length)
        {
            if (columns[index].Key == key || columns[index].FieldKey == key)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal int FindRowIndex(UiTableRow[] rows, string key)
    {
        int index = 0;
        while (index < rows.Length)
        {
            if (rows[index].Key == key)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal int FindCellIndex(UiTableCell[] cells, string key)
    {
        int index = 0;
        while (index < cells.Length)
        {
            if (cells[index].ColumnKey == key)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal UiTableSummary? FindSummary(UiTableSummary[] summaries, string key)
    {
        int index = 0;
        while (index < summaries.Length)
        {
            if (summaries[index].ColumnKey == key)
            {
                return summaries[index];
            }

            index++;
        }

        return null;
    }

    internal bool ContainsKey(string[] keys, string key)
    {
        int index = 0;
        while (index < keys.Length)
        {
            if (keys[index] == key)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal void ValidateSelectionMode(UiSelectionMode mode)
    {
        switch (mode)
        {
            case UiSelectionMode.None:
            case UiSelectionMode.Single:
            case UiSelectionMode.Multiple:
            case UiSelectionMode.Range:
                return;
            default:
                throw new ArgumentOutOfRangeException("selectionMode", mode, "The selection mode is not supported.");
        }
    }

    internal void ValidateSortDirection(UiSortDirection direction)
    {
        switch (direction)
        {
            case UiSortDirection.None:
            case UiSortDirection.Ascending:
            case UiSortDirection.Descending:
                return;
            default:
                throw new ArgumentOutOfRangeException("sortDirection", direction, "The sort direction is not supported.");
        }
    }

    internal string GetSelectionModeToken(UiSelectionMode mode)
    {
        ValidateSelectionMode(mode);
        switch (mode)
        {
            case UiSelectionMode.Single:
                return "single";
            case UiSelectionMode.Multiple:
                return "multiple";
            case UiSelectionMode.Range:
                return "range";
            default:
                return "none";
        }
    }

    internal string GetSortDirectionToken(UiSortDirection direction)
    {
        ValidateSortDirection(direction);
        switch (direction)
        {
            case UiSortDirection.Ascending:
                return "ascending";
            case UiSortDirection.Descending:
                return "descending";
            default:
                return "none";
        }
    }

    internal string FormatValue(UiValue value)
    {
        return new DocumentComposer().FormatMetadataValue(value);
    }

    internal string FormatBoolean(bool value)
    {
        if (value)
        {
            return "true";
        }

        return "false";
    }

    internal string CreateSyntheticId(UiNode node, string suffix, string identity)
    {
        string token = StableHash.HashParts(new string[] { GetNodeId(node), GetNodeKey(node), "table", suffix, identity });
        return "ui-table-" + token.Substring(0, 20);
    }

    internal string GetNodeKey(UiNode node)
    {
        if (node.Identity == null)
        {
            return string.Empty;
        }

        if (node.Identity.AuthorKey.Length > 0)
        {
            return node.Identity.AuthorKey;
        }

        return node.Identity.SiblingIdentity;
    }

    internal string GetNodeId(UiNode node)
    {
        if (node.Identity == null)
        {
            return string.Empty;
        }

        return node.Identity.StableId;
    }

    internal int GetNodeVersion(UiNode node)
    {
        if (node.State == null)
        {
            return 0;
        }

        return node.State.Version;
    }

    internal void SortAttributes(List<DomAttribute> attributes)
    {
        int index = 1;
        while (index < attributes.Count)
        {
            DomAttribute candidate = attributes[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && string.CompareOrdinal(attributes[insertionIndex].Name, candidate.Name) > 0)
            {
                attributes[insertionIndex + 1] = attributes[insertionIndex];
                insertionIndex--;
            }

            attributes[insertionIndex + 1] = candidate;
            index++;
        }
    }

    internal UiRenderException CreateRenderException(UiNode node, string message)
    {
        string nodeId = GetNodeId(node);
        if (nodeId.Length == 0)
        {
            nodeId = "unknown";
        }

        return new UiRenderException(message, nodeId, "dom");
    }
}
