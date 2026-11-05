using System;
using System.Collections.Generic;

namespace odf;

internal class PivotTableState
{
    internal OdfNode? SpreadsheetNode;
    internal OdfPivotTable[] OriginalTables = [];
}

internal static class PivotTableModel
{
    internal static void LoadPivotTables(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for pivot-table materialization.");
        }
        PivotTableState state = new PivotTableState();
        List<OdfPivotTable> tables = new List<OdfPivotTable>();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfNode? root = document.Parts[partIndex].Root;
            if (root != null)
            {
                CollectPivotTables(root, state, tables);
            }
            partIndex = partIndex + 1;
        }
        document.PivotTables = tables.ToArray();
        state.OriginalTables = document.PivotTables;
        document.PivotTableState = state;
    }

    internal static void SynchronizePivotTables(OdfDocument document)
    {
        if (document == null || document.PivotTables == null)
        {
            throw new OdfDocumentException(
                "A document and pivot-table collection are required for writing.");
        }
        PivotTableState? state = document.PivotTableState;
        if (state == null)
        {
            LoadPivotTables(document);
            state = document.PivotTableState;
        }
        if (state == null)
        {
            return;
        }
        RemoveDeletedPivotTables(
            state.OriginalTables,
            document.PivotTables);
        OdfNode? container = FindDirectChild(
            state.SpreadsheetNode,
            "data-pilot-tables");
        int index = 0;
        while (index < document.PivotTables.Length)
        {
            OdfPivotTable pivot = document.PivotTables[index];
            if (pivot == null
                || pivot.Name.Length == 0
                || pivot.TargetRangeAddress.Length == 0
                || pivot.Fields == null
                || pivot.FilterRules == null
                || pivot.Fields.Length == 0)
            {
                throw new OdfEditException(
                    "Each pivot table requires a name, output range, and at least one field.");
            }
            OdfNode? node = pivot.Node;
            if (node == null)
            {
                if (state.SpreadsheetNode == null)
                {
                    throw new OdfEditException(
                        "A spreadsheet body is required to add a pivot table.");
                }
                if (container == null)
                {
                    container = CreateTableElement(
                        "data-pilot-tables",
                        state.SpreadsheetNode.Location.PartPath);
                    InsertPivotContainer(state.SpreadsheetNode, container);
                }
                node = CreateTableElement(
                    "data-pilot-table",
                    container.Location.PartPath);
                StyleDocumentModel.AppendNode(container, node);
                pivot.Node = node;
            }
            SynchronizePivotTable(pivot, node);
            index = index + 1;
        }
        if (container != null && container.Children.Length == 0)
        {
            StyleDocumentModel.RemoveNodeFromParent(container);
        }
        state.OriginalTables = document.PivotTables;
    }

    static void CollectPivotTables(
        OdfNode node,
        PivotTableState state,
        List<OdfPivotTable> tables)
    {
        if (IsOfficeElement(node, "spreadsheet")
            && state.SpreadsheetNode == null)
        {
            state.SpreadsheetNode = node;
        }
        if (IsTableElement(node, "data-pilot-table"))
        {
            tables.Add(CreatePivotTable(node));
            return;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            CollectPivotTables(node.Children[index], state, tables);
            index = index + 1;
        }
    }

    static OdfPivotTable CreatePivotTable(OdfNode node)
    {
        OdfPivotTable result = new OdfPivotTable();
        result.Node = node;
        result.Name = ReadTableAttribute(node, "name");
        result.ApplicationData = ReadTableAttribute(node, "application-data");
        result.GrandTotal = ReadDefaultToken(node, "grand-total", "both");
        result.IgnoreEmptyRows = ReadBoolean(
            node,
            "ignore-empty-rows",
            false);
        result.IdentifyCategories = ReadBoolean(
            node,
            "identify-categories",
            false);
        result.TargetRangeAddress = ReadTableAttribute(
            node,
            "target-range-address");
        result.ButtonRangeAddresses = ReadTableAttribute(node, "buttons");
        result.ShowFilterButton = ReadBoolean(
            node,
            "show-filter-button",
            true);
        result.DrillDownOnDoubleClick = ReadBoolean(
            node,
            "drill-down-on-double-click",
            true);
        result.SourceNode = FindSourceNode(node);
        if (result.SourceNode != null && result.SourceNode.Name != null)
        {
            string sourceLocalName = result.SourceNode.Name.LocalName;
            result.SourceKind = SourceKind(sourceLocalName);
            if (sourceLocalName == "source-cell-range")
            {
                result.SourceRangeAddress = ReadTableAttribute(
                    result.SourceNode,
                    "cell-range-address");
                result.SourceName = ReadTableAttribute(
                    result.SourceNode,
                    "name");
                OdfNode? filter = FindDirectChild(
                    result.SourceNode,
                    "filter");
                if (filter != null)
                {
                    List<OdfFilterRule> filters =
                        new List<OdfFilterRule>();
                    CollectFilterRules(filter, "", 0, filters);
                    result.FilterRules = filters.ToArray();
                }
            }
        }
        List<OdfPivotField> fields = new List<OdfPivotField>();
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (IsTableElement(child, "data-pilot-field"))
            {
                fields.Add(CreatePivotField(child));
            }
            index = index + 1;
        }
        result.Fields = fields.ToArray();
        return result;
    }

    static OdfPivotField CreatePivotField(OdfNode node)
    {
        OdfPivotField result = new OdfPivotField();
        result.Node = node;
        result.SourceFieldName = ReadTableAttribute(
            node,
            "source-field-name");
        result.Orientation = ReadDefaultToken(
            node,
            "orientation",
            "hidden");
        result.SelectedPage = ReadTableAttribute(node, "selected-page");
        result.IsDataLayoutField = ReadBoolean(
            node,
            "is-data-layout-field",
            false);
        result.Function = ReadDefaultToken(node, "function", "auto");
        result.UsedHierarchy = ReadInteger(node, "used-hierarchy", 0);
        OdfNode? level = FindDirectChild(node, "data-pilot-level");
        if (level != null)
        {
            result.Level = CreatePivotLevel(level);
        }
        return result;
    }

    static OdfPivotLevel CreatePivotLevel(OdfNode node)
    {
        OdfPivotLevel result = new OdfPivotLevel();
        result.Node = node;
        result.ShowEmpty = ReadBoolean(node, "show-empty", false);
        OdfNode? subtotals = FindDirectChild(
            node,
            "data-pilot-subtotals");
        if (subtotals != null)
        {
            List<OdfPivotSubtotal> items =
                new List<OdfPivotSubtotal>();
            int index = 0;
            while (index < subtotals.Children.Length)
            {
                OdfNode child = subtotals.Children[index];
                if (IsTableElement(child, "data-pilot-subtotal"))
                {
                    OdfPivotSubtotal subtotal = new OdfPivotSubtotal();
                    subtotal.Node = child;
                    subtotal.Function = ReadDefaultToken(
                        child,
                        "function",
                        "auto");
                    items.Add(subtotal);
                }
                index = index + 1;
            }
            result.Subtotals = items.ToArray();
        }
        OdfNode? members = FindDirectChild(node, "data-pilot-members");
        if (members != null)
        {
            List<OdfPivotMember> items = new List<OdfPivotMember>();
            int index = 0;
            while (index < members.Children.Length)
            {
                OdfNode child = members.Children[index];
                if (IsTableElement(child, "data-pilot-member"))
                {
                    OdfPivotMember member = new OdfPivotMember();
                    member.Node = child;
                    member.Name = ReadTableAttribute(child, "name");
                    member.Display = ReadBoolean(child, "display", true);
                    member.ShowDetails = ReadBoolean(
                        child,
                        "show-details",
                        true);
                    items.Add(member);
                }
                index = index + 1;
            }
            result.Members = items.ToArray();
        }
        ReadLevelDisplayInfo(result, node);
        ReadLevelSortInfo(result, node);
        ReadLevelLayoutInfo(result, node);
        return result;
    }

    static void ReadLevelDisplayInfo(OdfPivotLevel level, OdfNode node)
    {
        OdfNode? display = FindDirectChild(
            node,
            "data-pilot-display-info");
        if (display == null)
        {
            return;
        }
        level.HasDisplayInfo = true;
        level.DisplayInfoEnabled = ReadBoolean(display, "enabled", false);
        level.DisplayDataField = ReadTableAttribute(display, "data-field");
        level.DisplayMemberCount = ReadInteger(display, "member-count", 0);
        level.DisplayMemberMode = ReadDefaultToken(
            display,
            "display-member-mode",
            "from-top");
    }

    static void ReadLevelSortInfo(OdfPivotLevel level, OdfNode node)
    {
        OdfNode? sort = FindDirectChild(node, "data-pilot-sort-info");
        if (sort == null)
        {
            return;
        }
        level.HasSortInfo = true;
        level.SortMode = ReadDefaultToken(sort, "sort-mode", "none");
        level.SortDataField = ReadTableAttribute(sort, "data-field");
        level.SortOrder = ReadDefaultToken(sort, "order", "ascending");
    }

    static void ReadLevelLayoutInfo(OdfPivotLevel level, OdfNode node)
    {
        OdfNode? layout = FindDirectChild(
            node,
            "data-pilot-layout-info");
        if (layout == null)
        {
            return;
        }
        level.HasLayoutInfo = true;
        level.LayoutMode = ReadDefaultToken(
            layout,
            "layout-mode",
            "tabular-layout");
        level.AddEmptyLines = ReadBoolean(
            layout,
            "add-empty-lines",
            false);
    }

    static void CollectFilterRules(
        OdfNode node,
        string connector,
        int depth,
        List<OdfFilterRule> result)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (IsTableElement(child, "filter-condition"))
            {
                OdfFilterRule rule = new OdfFilterRule();
                rule.Node = child;
                rule.FieldNumber = ReadInteger(child, "field-number", 0);
                rule.Operator = ReadDefaultToken(child, "operator", "=");
                rule.Value = ReadTableAttribute(child, "value");
                rule.DataType = ReadDefaultToken(
                    child,
                    "data-type",
                    "automatic");
                rule.CaseSensitive = ReadBoolean(
                    child,
                    "case-sensitive",
                    false);
                rule.Connector = connector;
                rule.GroupDepth = depth;
                result.Add(rule);
            }
            else if (IsTableElement(child, "filter-and"))
            {
                CollectFilterRules(child, "and", depth + 1, result);
            }
            else if (IsTableElement(child, "filter-or"))
            {
                CollectFilterRules(child, "or", depth + 1, result);
            }
            index = index + 1;
        }
    }

    static void SynchronizePivotTable(OdfPivotTable pivot, OdfNode node)
    {
        WriteRequired(node, "name", pivot.Name);
        WriteOptional(node, "application-data", pivot.ApplicationData);
        WriteDefaultToken(node, "grand-total", pivot.GrandTotal, "both");
        WriteBoolean(node, "ignore-empty-rows", pivot.IgnoreEmptyRows, false);
        WriteBoolean(
            node,
            "identify-categories",
            pivot.IdentifyCategories,
            false);
        WriteRequired(
            node,
            "target-range-address",
            pivot.TargetRangeAddress);
        WriteOptional(node, "buttons", pivot.ButtonRangeAddresses);
        WriteBoolean(
            node,
            "show-filter-button",
            pivot.ShowFilterButton,
            true);
        WriteBoolean(
            node,
            "drill-down-on-double-click",
            pivot.DrillDownOnDoubleClick,
            true);
        SynchronizeSource(pivot, node);
        RemoveMissingDirectNodes(
            node,
            "data-pilot-field",
            FieldNodes(pivot.Fields));
        int index = 0;
        while (index < pivot.Fields.Length)
        {
            OdfPivotField field = pivot.Fields[index];
            if (field == null || field.SourceFieldName.Length == 0)
            {
                throw new OdfEditException(
                    "Each pivot field requires a source field name.");
            }
            OdfNode? fieldNode = field.Node;
            if (fieldNode == null)
            {
                fieldNode = CreateTableElement(
                    "data-pilot-field",
                    node.Location.PartPath);
                StyleDocumentModel.AppendNode(node, fieldNode);
                field.Node = fieldNode;
            }
            SynchronizeField(field, fieldNode);
            index = index + 1;
        }
    }

    static void SynchronizeSource(OdfPivotTable pivot, OdfNode table)
    {
        string kind = pivot.SourceKind;
        if (kind.Length == 0
            && (pivot.SourceRangeAddress.Length > 0
                || pivot.SourceName.Length > 0))
        {
            kind = "cell-range";
            pivot.SourceKind = kind;
        }
        if (kind != "cell-range")
        {
            if (pivot.FilterRules.Length > 0)
            {
                throw new OdfEditException(
                    "Pivot filters require a cell-range source.");
            }
            return;
        }
        if (pivot.SourceRangeAddress.Length == 0
            && pivot.SourceName.Length == 0)
        {
            throw new OdfEditException(
                "A cell-range pivot source requires a range address or name.");
        }
        OdfNode? source = pivot.SourceNode;
        if (source == null || !IsTableElement(source, "source-cell-range"))
        {
            if (source != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(source);
            }
            source = CreateTableElement(
                "source-cell-range",
                table.Location.PartPath);
            InsertTableChildAt(table, source, 0);
            pivot.SourceNode = source;
        }
        WriteOptional(source, "name", pivot.SourceName);
        WriteOptional(
            source,
            "cell-range-address",
            pivot.SourceRangeAddress);
        SynchronizeFilter(pivot.FilterRules, source);
    }

    static void SynchronizeField(OdfPivotField field, OdfNode node)
    {
        WriteRequired(node, "source-field-name", field.SourceFieldName);
        WriteRequired(node, "orientation", field.Orientation);
        if (field.Orientation == "page")
        {
            WriteRequired(node, "selected-page", field.SelectedPage);
        }
        else
        {
            WriteOptional(node, "selected-page", "");
        }
        WriteBoolean(
            node,
            "is-data-layout-field",
            field.IsDataLayoutField,
            false);
        WriteDefaultToken(node, "function", field.Function, "auto");
        WriteInteger(node, "used-hierarchy", field.UsedHierarchy, 0);
        OdfNode? oldLevel = FindDirectChild(node, "data-pilot-level");
        if (field.Level == null)
        {
            if (oldLevel != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(oldLevel);
            }
            return;
        }
        OdfNode? levelNode = field.Level.Node;
        if (levelNode == null)
        {
            levelNode = CreateTableElement(
                "data-pilot-level",
                node.Location.PartPath);
            InsertTableChildAt(node, levelNode, 0);
            field.Level.Node = levelNode;
        }
        SynchronizeLevel(field.Level, levelNode);
    }

    static void SynchronizeLevel(OdfPivotLevel level, OdfNode node)
    {
        if (level.Subtotals == null || level.Members == null)
        {
            throw new OdfEditException(
                "Pivot level subtotal and member collections are required.");
        }
        WriteBoolean(node, "show-empty", level.ShowEmpty, false);
        SynchronizeSubtotals(level, node);
        SynchronizeMembers(level, node);
        SynchronizeDisplayInfo(level, node);
        SynchronizeSortInfo(level, node);
        SynchronizeLayoutInfo(level, node);
    }

    static void SynchronizeSubtotals(OdfPivotLevel level, OdfNode node)
    {
        OdfNode? container = FindDirectChild(
            node,
            "data-pilot-subtotals");
        if (level.Subtotals.Length == 0)
        {
            if (container != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(container);
            }
            return;
        }
        if (container == null)
        {
            container = CreateTableElement(
                "data-pilot-subtotals",
                node.Location.PartPath);
            InsertLevelChild(node, container, 1);
        }
        RemoveMissingDirectNodes(
            container,
            "data-pilot-subtotal",
            SubtotalNodes(level.Subtotals));
        int index = 0;
        while (index < level.Subtotals.Length)
        {
            OdfPivotSubtotal subtotal = level.Subtotals[index];
            if (subtotal == null)
            {
                throw new OdfEditException(
                    "Pivot subtotal entries cannot be null.");
            }
            OdfNode? subtotalNode = subtotal.Node;
            if (subtotalNode == null)
            {
                subtotalNode = CreateTableElement(
                    "data-pilot-subtotal",
                    node.Location.PartPath);
                StyleDocumentModel.AppendNode(container, subtotalNode);
                subtotal.Node = subtotalNode;
            }
            WriteRequired(subtotalNode, "function", subtotal.Function);
            index = index + 1;
        }
    }

    static void SynchronizeMembers(OdfPivotLevel level, OdfNode node)
    {
        OdfNode? container = FindDirectChild(
            node,
            "data-pilot-members");
        if (level.Members.Length == 0)
        {
            if (container != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(container);
            }
            return;
        }
        if (container == null)
        {
            container = CreateTableElement(
                "data-pilot-members",
                node.Location.PartPath);
            InsertLevelChild(node, container, 2);
        }
        RemoveMissingDirectNodes(
            container,
            "data-pilot-member",
            MemberNodes(level.Members));
        int index = 0;
        while (index < level.Members.Length)
        {
            OdfPivotMember member = level.Members[index];
            if (member == null || member.Name.Length == 0)
            {
                throw new OdfEditException(
                    "Each pivot member requires a name.");
            }
            OdfNode? memberNode = member.Node;
            if (memberNode == null)
            {
                memberNode = CreateTableElement(
                    "data-pilot-member",
                    node.Location.PartPath);
                StyleDocumentModel.AppendNode(container, memberNode);
                member.Node = memberNode;
            }
            WriteRequired(memberNode, "name", member.Name);
            WriteBoolean(memberNode, "display", member.Display, true);
            WriteBoolean(
                memberNode,
                "show-details",
                member.ShowDetails,
                true);
            index = index + 1;
        }
    }

    static void SynchronizeDisplayInfo(OdfPivotLevel level, OdfNode node)
    {
        OdfNode? info = FindDirectChild(
            node,
            "data-pilot-display-info");
        if (!level.HasDisplayInfo)
        {
            if (info != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(info);
            }
            return;
        }
        if (level.DisplayMemberCount < 0)
        {
            throw new OdfEditException(
                "A pivot display member count cannot be negative.");
        }
        if (info == null)
        {
            info = CreateTableElement(
                "data-pilot-display-info",
                node.Location.PartPath);
            InsertLevelChild(node, info, 3);
        }
        WriteRequired(
            info,
            "enabled",
            level.DisplayInfoEnabled ? "true" : "false");
        WriteRequired(info, "data-field", level.DisplayDataField);
        WriteRequired(
            info,
            "member-count",
            level.DisplayMemberCount.ToString());
        WriteRequired(
            info,
            "display-member-mode",
            level.DisplayMemberMode);
    }

    static void SynchronizeSortInfo(OdfPivotLevel level, OdfNode node)
    {
        OdfNode? info = FindDirectChild(node, "data-pilot-sort-info");
        if (!level.HasSortInfo)
        {
            if (info != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(info);
            }
            return;
        }
        if (info == null)
        {
            info = CreateTableElement(
                "data-pilot-sort-info",
                node.Location.PartPath);
            InsertLevelChild(node, info, 4);
        }
        WriteRequired(info, "sort-mode", level.SortMode);
        if (level.SortMode == "data")
        {
            WriteRequired(info, "data-field", level.SortDataField);
        }
        else
        {
            WriteOptional(info, "data-field", "");
        }
        WriteRequired(info, "order", level.SortOrder);
    }

    static void SynchronizeLayoutInfo(OdfPivotLevel level, OdfNode node)
    {
        OdfNode? info = FindDirectChild(node, "data-pilot-layout-info");
        if (!level.HasLayoutInfo)
        {
            if (info != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(info);
            }
            return;
        }
        if (info == null)
        {
            info = CreateTableElement(
                "data-pilot-layout-info",
                node.Location.PartPath);
            InsertLevelChild(node, info, 5);
        }
        WriteRequired(info, "layout-mode", level.LayoutMode);
        WriteRequired(
            info,
            "add-empty-lines",
            level.AddEmptyLines ? "true" : "false");
    }

    static void SynchronizeFilter(
        OdfFilterRule[] rules,
        OdfNode source)
    {
        OdfNode? filter = FindDirectChild(source, "filter");
        if (rules.Length == 0)
        {
            if (filter != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(filter);
            }
            return;
        }
        if (filter == null)
        {
            filter = CreateTableElement("filter", source.Location.PartPath);
            StyleDocumentModel.AppendNode(source, filter);
        }
        RemoveMissingDescendantNodes(
            filter,
            "filter-condition",
            FilterNodes(rules));
        OdfNode conditionParent = EnsureFilterParent(filter, rules.Length);
        int index = 0;
        while (index < rules.Length)
        {
            OdfFilterRule rule = rules[index];
            if (rule == null || rule.FieldNumber < 0)
            {
                throw new OdfEditException(
                    "Pivot filter field numbers cannot be negative.");
            }
            OdfNode? condition = rule.Node;
            if (condition == null)
            {
                condition = CreateTableElement(
                    "filter-condition",
                    source.Location.PartPath);
                StyleDocumentModel.AppendNode(conditionParent, condition);
                rule.Node = condition;
            }
            WriteRequired(
                condition,
                "field-number",
                rule.FieldNumber.ToString());
            WriteRequired(condition, "operator", rule.Operator);
            WriteRequired(condition, "value", rule.Value);
            WriteDefaultToken(
                condition,
                "data-type",
                rule.DataType,
                "automatic");
            WriteBoolean(
                condition,
                "case-sensitive",
                rule.CaseSensitive,
                false);
            index = index + 1;
        }
    }

    static OdfNode EnsureFilterParent(OdfNode filter, int ruleCount)
    {
        if (ruleCount == 1)
        {
            return filter;
        }
        OdfNode? group = FindDirectChild(filter, "filter-and");
        if (group == null)
        {
            group = FindDirectChild(filter, "filter-or");
        }
        if (group == null)
        {
            group = CreateTableElement(
                "filter-and",
                filter.Location.PartPath);
            List<OdfNode> directConditions = new List<OdfNode>();
            int index = 0;
            while (index < filter.Children.Length)
            {
                OdfNode child = filter.Children[index];
                if (IsTableElement(child, "filter-condition"))
                {
                    directConditions.Add(child);
                }
                index = index + 1;
            }
            index = 0;
            while (index < directConditions.Count)
            {
                StyleDocumentModel.RemoveNodeFromParent(
                    directConditions[index]);
                StyleDocumentModel.AppendNode(group, directConditions[index]);
                index = index + 1;
            }
            StyleDocumentModel.AppendNode(filter, group);
        }
        return group;
    }

    static void RemoveDeletedPivotTables(
        OdfPivotTable[] original,
        OdfPivotTable[] current)
    {
        int index = 0;
        while (index < original.Length)
        {
            OdfNode? node = original[index].Node;
            if (node != null && !ContainsPivotNode(current, node))
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
            }
            index = index + 1;
        }
    }

    static bool ContainsPivotNode(OdfPivotTable[] pivots, OdfNode node)
    {
        int index = 0;
        while (index < pivots.Length)
        {
            OdfPivotTable pivot = pivots[index];
            if (pivot != null && pivot.Node == node)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void RemoveMissingDirectNodes(
        OdfNode parent,
        string localName,
        OdfNode[] retained)
    {
        List<OdfNode> removed = new List<OdfNode>();
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode child = parent.Children[index];
            if (IsTableElement(child, localName)
                && !ContainsNode(retained, child))
            {
                removed.Add(child);
            }
            index = index + 1;
        }
        index = 0;
        while (index < removed.Count)
        {
            StyleDocumentModel.RemoveNodeFromParent(removed[index]);
            index = index + 1;
        }
    }

    static void RemoveMissingDescendantNodes(
        OdfNode parent,
        string localName,
        OdfNode[] retained)
    {
        List<OdfNode> removed = new List<OdfNode>();
        CollectMissingDescendants(parent, localName, retained, removed);
        int index = 0;
        while (index < removed.Count)
        {
            StyleDocumentModel.RemoveNodeFromParent(removed[index]);
            index = index + 1;
        }
    }

    static void CollectMissingDescendants(
        OdfNode parent,
        string localName,
        OdfNode[] retained,
        List<OdfNode> removed)
    {
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode child = parent.Children[index];
            if (IsTableElement(child, localName)
                && !ContainsNode(retained, child))
            {
                removed.Add(child);
            }
            else
            {
                CollectMissingDescendants(
                    child,
                    localName,
                    retained,
                    removed);
            }
            index = index + 1;
        }
    }

    static bool ContainsNode(OdfNode[] nodes, OdfNode node)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            if (nodes[index] == node)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static OdfNode[] FieldNodes(OdfPivotField[] fields)
    {
        OdfNode[] result = new OdfNode[fields.Length];
        int index = 0;
        while (index < fields.Length)
        {
            result[index] = fields[index].Node ?? new OdfNode();
            index = index + 1;
        }
        return result;
    }

    static OdfNode[] SubtotalNodes(OdfPivotSubtotal[] subtotals)
    {
        OdfNode[] result = new OdfNode[subtotals.Length];
        int index = 0;
        while (index < subtotals.Length)
        {
            result[index] = subtotals[index].Node ?? new OdfNode();
            index = index + 1;
        }
        return result;
    }

    static OdfNode[] MemberNodes(OdfPivotMember[] members)
    {
        OdfNode[] result = new OdfNode[members.Length];
        int index = 0;
        while (index < members.Length)
        {
            result[index] = members[index].Node ?? new OdfNode();
            index = index + 1;
        }
        return result;
    }

    static OdfNode[] FilterNodes(OdfFilterRule[] filters)
    {
        OdfNode[] result = new OdfNode[filters.Length];
        int index = 0;
        while (index < filters.Length)
        {
            result[index] = filters[index].Node ?? new OdfNode();
            index = index + 1;
        }
        return result;
    }

    static OdfNode? FindSourceNode(OdfNode table)
    {
        int index = 0;
        while (index < table.Children.Length)
        {
            OdfNode child = table.Children[index];
            if (child.Name != null
                && child.Name.NamespaceName == OdfNamespaces.Table
                && IsSourceLocalName(child.Name.LocalName))
            {
                return child;
            }
            index = index + 1;
        }
        return null;
    }

    static bool IsSourceLocalName(string localName)
    {
        return localName == "source-cell-range"
            || localName == "database-source-sql"
            || localName == "database-source-table"
            || localName == "database-source-query"
            || localName == "source-service";
    }

    static string SourceKind(string localName)
    {
        if (localName == "source-cell-range")
        {
            return "cell-range";
        }
        if (localName == "database-source-sql")
        {
            return "sql";
        }
        if (localName == "database-source-table")
        {
            return "table";
        }
        if (localName == "database-source-query")
        {
            return "query";
        }
        if (localName == "source-service")
        {
            return "service";
        }
        return localName;
    }

    static void InsertPivotContainer(OdfNode spreadsheet, OdfNode container)
    {
        int insertion = spreadsheet.Children.Length;
        int index = 0;
        while (index < spreadsheet.Children.Length)
        {
            OdfNode child = spreadsheet.Children[index];
            if (IsTableElement(child, "consolidation")
                || IsTableElement(child, "dde-links"))
            {
                insertion = index;
                index = spreadsheet.Children.Length;
            }
            else
            {
                index = index + 1;
            }
        }
        InsertTableChildAt(spreadsheet, container, insertion);
    }

    static void InsertLevelChild(OdfNode level, OdfNode child, int rank)
    {
        int insertion = level.Children.Length;
        int index = 0;
        while (index < level.Children.Length)
        {
            if (LevelChildRank(level.Children[index]) > rank)
            {
                insertion = index;
                index = level.Children.Length;
            }
            else
            {
                index = index + 1;
            }
        }
        InsertTableChildAt(level, child, insertion);
    }

    static int LevelChildRank(OdfNode node)
    {
        if (IsTableElement(node, "data-pilot-subtotals"))
        {
            return 1;
        }
        if (IsTableElement(node, "data-pilot-members"))
        {
            return 2;
        }
        if (IsTableElement(node, "data-pilot-display-info"))
        {
            return 3;
        }
        if (IsTableElement(node, "data-pilot-sort-info"))
        {
            return 4;
        }
        if (IsTableElement(node, "data-pilot-layout-info"))
        {
            return 5;
        }
        return 0;
    }

    static void InsertTableChildAt(
        OdfNode parent,
        OdfNode child,
        int insertion)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        Array.Copy(parent.Children, 0, children, 0, insertion);
        children[insertion] = child;
        Array.Copy(
            parent.Children,
            insertion,
            children,
            insertion + 1,
            parent.Children.Length - insertion);
        parent.Children = children;
        child.Parent = parent;
        child.Location.PartPath = parent.Location.PartPath;
        parent.WasEmptyElement = false;
    }

    static OdfNode? FindDirectChild(OdfNode? parent, string localName)
    {
        if (parent == null)
        {
            return null;
        }
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode child = parent.Children[index];
            if (IsTableElement(child, localName))
            {
                return child;
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

    static string ReadDefaultToken(
        OdfNode node,
        string localName,
        string defaultValue)
    {
        string value = ReadTableAttribute(node, localName);
        return value.Length == 0 ? defaultValue : value;
    }

    static bool ReadBoolean(
        OdfNode node,
        string localName,
        bool defaultValue)
    {
        string value = ReadTableAttribute(node, localName);
        if (value == "true" || value == "1")
        {
            return true;
        }
        if (value == "false" || value == "0")
        {
            return false;
        }
        return defaultValue;
    }

    static long ReadInteger(
        OdfNode node,
        string localName,
        long defaultValue)
    {
        string value = ReadTableAttribute(node, localName);
        OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(value);
        return parsed.IsValid ? parsed.IntegerValue : defaultValue;
    }

    static void WriteRequired(
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

    static void WriteOptional(
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
            WriteRequired(node, localName, value);
        }
    }

    static void WriteDefaultToken(
        OdfNode node,
        string localName,
        string value,
        string defaultValue)
    {
        if (value.Length == 0 || value == defaultValue)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                OdfNamespaces.Table,
                localName);
        }
        else
        {
            WriteRequired(node, localName, value);
        }
    }

    static void WriteBoolean(
        OdfNode node,
        string localName,
        bool value,
        bool defaultValue)
    {
        if (value == defaultValue)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                OdfNamespaces.Table,
                localName);
        }
        else
        {
            WriteRequired(node, localName, value ? "true" : "false");
        }
    }

    static void WriteInteger(
        OdfNode node,
        string localName,
        long value,
        long defaultValue)
    {
        if (value == defaultValue)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                OdfNamespaces.Table,
                localName);
        }
        else
        {
            WriteRequired(node, localName, value.ToString());
        }
    }

    static OdfNode CreateTableElement(string localName, string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(
            OdfNamespaces.Table,
            localName,
            OdfNamespaces.TablePrefix);
        result.Location.PartPath = partPath;
        result.WasEmptyElement = true;
        return result;
    }

    static bool IsOfficeElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Office
            && node.Name.LocalName == localName;
    }

    static bool IsTableElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Table
            && node.Name.LocalName == localName;
    }
}
