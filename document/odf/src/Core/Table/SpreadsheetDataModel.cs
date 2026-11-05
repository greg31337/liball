using System;
using System.Collections.Generic;

namespace odf;

internal class SpreadsheetDataState
{
    internal OdfNode? SpreadsheetNode;
    internal OdfNamedExpression[] NamedExpressions = [];
    internal OdfLabelRange[] LabelRanges = [];
    internal OdfDatabaseRange[] DatabaseRanges = [];
    internal OdfConsolidation[] Consolidations = [];
    internal OdfScenario[] Scenarios = [];
}

internal static class SpreadsheetDataModel
{
    internal static void LoadSpreadsheetData(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for spreadsheet-data materialization.");
        }
        SpreadsheetDataState state = new SpreadsheetDataState();
        List<OdfNamedExpression> named =
            new List<OdfNamedExpression>();
        List<OdfLabelRange> labels = new List<OdfLabelRange>();
        List<OdfDatabaseRange> databases =
            new List<OdfDatabaseRange>();
        List<OdfConsolidation> consolidations =
            new List<OdfConsolidation>();
        List<OdfScenario> scenarios = new List<OdfScenario>();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfNode? root = document.Parts[partIndex].Root;
            if (root != null)
            {
                CollectSpreadsheetData(
                    root,
                    document,
                    state,
                    named,
                    labels,
                    databases,
                    consolidations,
                    scenarios);
            }
            partIndex = partIndex + 1;
        }
        document.NamedExpressions = named.ToArray();
        document.LabelRanges = labels.ToArray();
        document.DatabaseRanges = databases.ToArray();
        document.Consolidations = consolidations.ToArray();
        document.Scenarios = scenarios.ToArray();
        state.NamedExpressions = document.NamedExpressions;
        state.LabelRanges = document.LabelRanges;
        state.DatabaseRanges = document.DatabaseRanges;
        state.Consolidations = document.Consolidations;
        state.Scenarios = document.Scenarios;
        document.SpreadsheetState = state;
    }

    internal static void SynchronizeSpreadsheetData(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for spreadsheet-data synchronization.");
        }
        SpreadsheetDataState? state = document.SpreadsheetState;
        if (state == null)
        {
            LoadSpreadsheetData(document);
            state = document.SpreadsheetState;
        }
        if (state == null)
        {
            return;
        }
        RemoveDiscardedNamed(state.NamedExpressions, document.NamedExpressions);
        RemoveDiscardedLabels(state.LabelRanges, document.LabelRanges);
        RemoveDiscardedDatabases(state.DatabaseRanges, document.DatabaseRanges);
        RemoveDiscardedConsolidations(
            state.Consolidations,
            document.Consolidations);
        RemoveDiscardedScenarios(state.Scenarios, document.Scenarios);
        OdfNode? spreadsheet = state.SpreadsheetNode;
        SynchronizeNamedExpressions(document.NamedExpressions, spreadsheet);
        SynchronizeLabelRanges(document.LabelRanges, spreadsheet);
        SynchronizeDatabaseRanges(document.DatabaseRanges, spreadsheet);
        SynchronizeConsolidations(document.Consolidations, spreadsheet);
        SynchronizeScenarios(document.Scenarios);
        state.NamedExpressions = document.NamedExpressions;
        state.LabelRanges = document.LabelRanges;
        state.DatabaseRanges = document.DatabaseRanges;
        state.Consolidations = document.Consolidations;
        state.Scenarios = document.Scenarios;
    }

    static void CollectSpreadsheetData(
        OdfNode node,
        OdfDocument document,
        SpreadsheetDataState state,
        List<OdfNamedExpression> named,
        List<OdfLabelRange> labels,
        List<OdfDatabaseRange> databases,
        List<OdfConsolidation> consolidations,
        List<OdfScenario> scenarios)
    {
        if (IsOfficeElement(node, "spreadsheet")
            && state.SpreadsheetNode == null)
        {
            state.SpreadsheetNode = node;
        }
        if (IsTableElement(node, "named-range")
            || IsTableElement(node, "named-expression"))
        {
            named.Add(CreateNamedExpression(node));
        }
        else if (IsTableElement(node, "label-range"))
        {
            labels.Add(CreateLabelRange(node));
        }
        else if (IsTableElement(node, "database-range"))
        {
            databases.Add(CreateDatabaseRange(node));
        }
        else if (IsTableElement(node, "consolidation"))
        {
            consolidations.Add(CreateConsolidation(node));
        }
        else if (IsTableElement(node, "scenario"))
        {
            scenarios.Add(CreateScenario(node, document));
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            CollectSpreadsheetData(
                node.Children[index],
                document,
                state,
                named,
                labels,
                databases,
                consolidations,
                scenarios);
            index = index + 1;
        }
    }

    static OdfNamedExpression CreateNamedExpression(OdfNode node)
    {
        OdfNamedExpression result = new OdfNamedExpression();
        result.Node = node;
        result.IsRange = IsTableElement(node, "named-range");
        result.Name = ReadTableAttribute(node, "name");
        result.Expression = ReadTableAttribute(node, "expression");
        result.BaseCellAddress = ReadTableAttribute(
            node,
            "base-cell-address");
        result.RangeAddress = ReadTableAttribute(
            node,
            "cell-range-address");
        result.RangeUsableAs = ReadTableAttribute(
            node,
            "range-usable-as");
        return result;
    }

    static OdfLabelRange CreateLabelRange(OdfNode node)
    {
        OdfLabelRange result = new OdfLabelRange();
        result.Node = node;
        result.LabelRangeAddress = ReadTableAttribute(
            node,
            "label-cell-range-address");
        result.DataRangeAddress = ReadTableAttribute(
            node,
            "data-cell-range-address");
        result.Orientation = ReadTableAttribute(node, "orientation");
        return result;
    }

    static OdfDatabaseRange CreateDatabaseRange(OdfNode node)
    {
        OdfDatabaseRange result = new OdfDatabaseRange();
        result.Node = node;
        result.Name = ReadTableAttribute(node, "name");
        result.TargetRangeAddress = ReadTableAttribute(
            node,
            "target-range-address");
        result.ContainsHeader = ReadBoolean(node, "contains-header", true);
        result.DisplayFilterButtons = ReadBoolean(
            node,
            "display-filter-buttons",
            false);
        result.KeepStyles = ReadBoolean(
            node,
            "on-update-keep-styles",
            true);
        result.KeepSize = ReadBoolean(
            node,
            "on-update-keep-size",
            false);
        result.HasPersistentData = ReadBoolean(
            node,
            "has-persistent-data",
            true);
        OdfNode? sort = FindFirstDescendant(node, "sort");
        if (sort != null)
        {
            result.SortCaseSensitive = ReadBoolean(
                sort,
                "case-sensitive",
                false);
            result.SortTargetRangeAddress = ReadTableAttribute(
                sort,
                "target-range-address");
            result.SortRules = CreateSortRules(sort);
        }
        OdfNode? filter = FindFirstDescendant(node, "filter");
        if (filter != null)
        {
            List<OdfFilterRule> filters = new List<OdfFilterRule>();
            CollectFilterRules(filter, "", 0, filters);
            result.FilterRules = filters.ToArray();
        }
        OdfNode? subtotals = FindFirstDescendant(
            node,
            "subtotal-rules");
        if (subtotals != null)
        {
            result.SubtotalRules = CreateSubtotalRules(subtotals);
        }
        return result;
    }

    static OdfSortRule[] CreateSortRules(OdfNode sort)
    {
        List<OdfSortRule> result = new List<OdfSortRule>();
        int index = 0;
        while (index < sort.Children.Length)
        {
            OdfNode child = sort.Children[index];
            if (IsTableElement(child, "sort-by"))
            {
                OdfSortRule rule = new OdfSortRule();
                rule.Node = child;
                rule.FieldNumber = ReadNonNegativeInteger(
                    child,
                    "field-number");
                rule.DataType = ReadDefaultToken(
                    child,
                    "data-type",
                    "automatic");
                rule.Order = ReadDefaultToken(
                    child,
                    "order",
                    "ascending");
                result.Add(rule);
            }
            index = index + 1;
        }
        return result.ToArray();
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
                rule.FieldNumber = ReadNonNegativeInteger(
                    child,
                    "field-number");
                rule.Operator = ReadDefaultToken(
                    child,
                    "operator",
                    "=");
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

    static OdfSubtotalRule[] CreateSubtotalRules(OdfNode subtotals)
    {
        List<OdfSubtotalRule> result = new List<OdfSubtotalRule>();
        int index = 0;
        while (index < subtotals.Children.Length)
        {
            OdfNode child = subtotals.Children[index];
            if (IsTableElement(child, "subtotal-rule"))
            {
                OdfSubtotalRule rule = new OdfSubtotalRule();
                rule.Node = child;
                rule.GroupByFieldNumber = ReadNonNegativeInteger(
                    child,
                    "group-by-field-number");
                rule.Fields = CreateSubtotalFields(child);
                result.Add(rule);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfSubtotalField[] CreateSubtotalFields(OdfNode ruleNode)
    {
        List<OdfSubtotalField> result = new List<OdfSubtotalField>();
        int index = 0;
        while (index < ruleNode.Children.Length)
        {
            OdfNode child = ruleNode.Children[index];
            if (IsTableElement(child, "subtotal-field"))
            {
                OdfSubtotalField field = new OdfSubtotalField();
                field.Node = child;
                field.FieldNumber = ReadNonNegativeInteger(
                    child,
                    "field-number");
                field.Function = ReadDefaultToken(
                    child,
                    "function",
                    "sum");
                result.Add(field);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfConsolidation CreateConsolidation(OdfNode node)
    {
        OdfConsolidation result = new OdfConsolidation();
        result.Node = node;
        result.Function = ReadDefaultToken(node, "function", "sum");
        result.SourceRangeAddresses = ReadTableAttribute(
            node,
            "source-cell-range-addresses");
        result.TargetCellAddress = ReadTableAttribute(
            node,
            "target-cell-address");
        result.UseLabel = ReadDefaultToken(node, "use-label", "none");
        return result;
    }

    static OdfScenario CreateScenario(
        OdfNode node,
        OdfDocument document)
    {
        OdfScenario result = new OdfScenario();
        result.Node = node;
        result.Table = FindContainingTable(node, document);
        result.RangeAddresses = ReadTableAttribute(node, "scenario-ranges");
        result.Comment = ReadTableAttribute(node, "comment");
        result.CopyBack = ReadBoolean(node, "copy-back", false);
        result.CopyStyles = ReadBoolean(node, "copy-styles", false);
        result.CopyFormulas = ReadBoolean(node, "copy-formulas", false);
        result.IsActive = ReadBoolean(node, "is-active", false);
        result.DisplayBorder = ReadBoolean(node, "display-border", true);
        result.BorderColor = ReadTableAttribute(node, "border-color");
        result.IsProtected = ReadBoolean(node, "protected", false);
        result.ProtectionKey = ReadTableAttribute(node, "protection-key");
        return result;
    }

    static void SynchronizeNamedExpressions(
        OdfNamedExpression[] expressions,
        OdfNode? spreadsheet)
    {
        if (expressions.Length == 0)
        {
            return;
        }
        OdfNode parent = RequireFeatureParent(
            spreadsheet,
            "named-expressions");
        int index = 0;
        while (index < expressions.Length)
        {
            OdfNamedExpression item = expressions[index];
            OdfNode node = EnsureFeatureNode(
                item.Node,
                parent,
                item.IsRange ? "named-range" : "named-expression");
            item.Node = node;
            SetTableElementName(
                node,
                item.IsRange ? "named-range" : "named-expression");
            WriteRequired(node, "name", item.Name);
            WriteOptional(node, "expression", item.Expression);
            WriteOptional(node, "base-cell-address", item.BaseCellAddress);
            WriteOptional(node, "cell-range-address", item.RangeAddress);
            WriteOptional(node, "range-usable-as", item.RangeUsableAs);
            index = index + 1;
        }
    }

    static void SynchronizeLabelRanges(
        OdfLabelRange[] ranges,
        OdfNode? spreadsheet)
    {
        if (ranges.Length == 0)
        {
            return;
        }
        OdfNode parent = RequireFeatureParent(spreadsheet, "label-ranges");
        int index = 0;
        while (index < ranges.Length)
        {
            OdfLabelRange item = ranges[index];
            OdfNode node = EnsureFeatureNode(
                item.Node,
                parent,
                "label-range");
            item.Node = node;
            WriteRequired(
                node,
                "label-cell-range-address",
                item.LabelRangeAddress);
            WriteRequired(
                node,
                "data-cell-range-address",
                item.DataRangeAddress);
            WriteRequired(node, "orientation", item.Orientation);
            index = index + 1;
        }
    }

    static void SynchronizeDatabaseRanges(
        OdfDatabaseRange[] ranges,
        OdfNode? spreadsheet)
    {
        if (ranges.Length == 0)
        {
            return;
        }
        OdfNode parent = RequireFeatureParent(
            spreadsheet,
            "database-ranges");
        int index = 0;
        while (index < ranges.Length)
        {
            SynchronizeDatabaseRange(ranges[index], parent);
            index = index + 1;
        }
    }

    static void SynchronizeDatabaseRange(
        OdfDatabaseRange range,
        OdfNode parent)
    {
        OdfNode node = EnsureFeatureNode(
            range.Node,
            parent,
            "database-range");
        range.Node = node;
        WriteRequired(node, "name", range.Name);
        WriteRequired(node, "target-range-address", range.TargetRangeAddress);
        WriteBoolean(node, "contains-header", range.ContainsHeader, true);
        WriteBoolean(
            node,
            "display-filter-buttons",
            range.DisplayFilterButtons,
            false);
        WriteBoolean(node, "on-update-keep-styles", range.KeepStyles, true);
        WriteBoolean(node, "on-update-keep-size", range.KeepSize, false);
        WriteBoolean(
            node,
            "has-persistent-data",
            range.HasPersistentData,
            true);
        SynchronizeSort(range, node);
        SynchronizeFilter(range, node);
        SynchronizeSubtotals(range, node);
    }

    static void SynchronizeSort(OdfDatabaseRange range, OdfNode database)
    {
        OdfNode? sort = FindFirstDescendant(database, "sort");
        if (range.SortRules.Length == 0
            && range.SortTargetRangeAddress.Length == 0
            && !range.SortCaseSensitive)
        {
            RemoveIfAttached(sort);
            return;
        }
        if (sort == null)
        {
            sort = CreateTableElement("sort");
            StyleDocumentModel.AppendNode(database, sort);
        }
        WriteBoolean(sort, "case-sensitive", range.SortCaseSensitive, false);
        WriteOptional(
            sort,
            "target-range-address",
            range.SortTargetRangeAddress);
        SynchronizeSortRules(range.SortRules, sort);
    }

    static void SynchronizeSortRules(
        OdfSortRule[] rules,
        OdfNode sort)
    {
        RemoveMissingChildNodes(sort, "sort-by", SortNodes(rules));
        int index = 0;
        while (index < rules.Length)
        {
            OdfSortRule rule = rules[index];
            OdfNode node = EnsureFeatureNode(rule.Node, sort, "sort-by");
            rule.Node = node;
            WriteInteger(node, "field-number", rule.FieldNumber);
            WriteOptionalDefault(node, "data-type", rule.DataType, "automatic");
            WriteOptionalDefault(node, "order", rule.Order, "ascending");
            index = index + 1;
        }
    }

    static void SynchronizeFilter(
        OdfDatabaseRange range,
        OdfNode database)
    {
        OdfNode? filter = FindFirstDescendant(database, "filter");
        if (range.FilterRules.Length == 0)
        {
            RemoveIfAttached(filter);
            return;
        }
        if (filter == null)
        {
            filter = CreateTableElement("filter");
            StyleDocumentModel.AppendNode(database, filter);
        }
        RemoveMissingDescendantNodes(
            filter,
            "filter-condition",
            FilterNodes(range.FilterRules));
        int index = 0;
        while (index < range.FilterRules.Length)
        {
            OdfFilterRule rule = range.FilterRules[index];
            OdfNode node = EnsureFeatureNode(
                rule.Node,
                filter,
                "filter-condition");
            rule.Node = node;
            WriteInteger(node, "field-number", rule.FieldNumber);
            WriteRequired(node, "operator", rule.Operator);
            WriteRequired(node, "value", rule.Value);
            WriteOptionalDefault(node, "data-type", rule.DataType, "automatic");
            WriteBoolean(node, "case-sensitive", rule.CaseSensitive, false);
            index = index + 1;
        }
    }

    static void SynchronizeSubtotals(
        OdfDatabaseRange range,
        OdfNode database)
    {
        OdfNode? subtotals = FindFirstDescendant(
            database,
            "subtotal-rules");
        if (range.SubtotalRules.Length == 0)
        {
            RemoveIfAttached(subtotals);
            return;
        }
        if (subtotals == null)
        {
            subtotals = CreateTableElement("subtotal-rules");
            StyleDocumentModel.AppendNode(database, subtotals);
        }
        RemoveMissingChildNodes(
            subtotals,
            "subtotal-rule",
            SubtotalNodes(range.SubtotalRules));
        int index = 0;
        while (index < range.SubtotalRules.Length)
        {
            OdfSubtotalRule rule = range.SubtotalRules[index];
            OdfNode node = EnsureFeatureNode(
                rule.Node,
                subtotals,
                "subtotal-rule");
            rule.Node = node;
            WriteInteger(
                node,
                "group-by-field-number",
                rule.GroupByFieldNumber);
            SynchronizeSubtotalFields(rule.Fields, node);
            index = index + 1;
        }
    }

    static void SynchronizeSubtotalFields(
        OdfSubtotalField[] fields,
        OdfNode rule)
    {
        RemoveMissingChildNodes(rule, "subtotal-field", SubtotalFieldNodes(fields));
        int index = 0;
        while (index < fields.Length)
        {
            OdfSubtotalField field = fields[index];
            OdfNode node = EnsureFeatureNode(
                field.Node,
                rule,
                "subtotal-field");
            field.Node = node;
            WriteInteger(node, "field-number", field.FieldNumber);
            WriteOptionalDefault(node, "function", field.Function, "sum");
            index = index + 1;
        }
    }

    static void SynchronizeConsolidations(
        OdfConsolidation[] items,
        OdfNode? spreadsheet)
    {
        if (items.Length == 0)
        {
            return;
        }
        OdfNode parent = RequireSpreadsheet(spreadsheet);
        int index = 0;
        while (index < items.Length)
        {
            OdfConsolidation item = items[index];
            OdfNode node = EnsureFeatureNode(
                item.Node,
                parent,
                "consolidation");
            item.Node = node;
            WriteOptionalDefault(node, "function", item.Function, "sum");
            WriteRequired(
                node,
                "source-cell-range-addresses",
                item.SourceRangeAddresses);
            WriteRequired(
                node,
                "target-cell-address",
                item.TargetCellAddress);
            WriteOptionalDefault(node, "use-label", item.UseLabel, "none");
            index = index + 1;
        }
    }

    static void SynchronizeScenarios(OdfScenario[] items)
    {
        int index = 0;
        while (index < items.Length)
        {
            OdfScenario item = items[index];
            OdfNode tableNode = item.Table == null
                ? new OdfNode()
                : item.Table.Node ?? new OdfNode();
            if (item.Node == null
                && (item.Table == null || item.Table.Node == null))
            {
                throw new OdfEditException(
                    "A new scenario requires a containing table.");
            }
            OdfNode node = EnsureFeatureNode(
                item.Node,
                tableNode,
                "scenario");
            item.Node = node;
            WriteRequired(node, "scenario-ranges", item.RangeAddresses);
            WriteOptional(node, "comment", item.Comment);
            WriteBoolean(node, "copy-back", item.CopyBack, false);
            WriteBoolean(node, "copy-styles", item.CopyStyles, false);
            WriteBoolean(node, "copy-formulas", item.CopyFormulas, false);
            WriteBoolean(node, "is-active", item.IsActive, false);
            WriteBoolean(node, "display-border", item.DisplayBorder, true);
            WriteOptional(node, "border-color", item.BorderColor);
            WriteBoolean(node, "protected", item.IsProtected, false);
            WriteOptional(node, "protection-key", item.ProtectionKey);
            index = index + 1;
        }
    }

    static void RemoveDiscardedNamed(
        OdfNamedExpression[] original,
        OdfNamedExpression[] current)
    {
        int index = 0;
        while (index < original.Length)
        {
            if (!ContainsNamed(current, original[index]))
            {
                RemoveIfAttached(original[index].Node);
            }
            index = index + 1;
        }
    }

    static void RemoveDiscardedLabels(
        OdfLabelRange[] original,
        OdfLabelRange[] current)
    {
        int index = 0;
        while (index < original.Length)
        {
            if (!ContainsLabel(current, original[index]))
            {
                RemoveIfAttached(original[index].Node);
            }
            index = index + 1;
        }
    }

    static void RemoveDiscardedDatabases(
        OdfDatabaseRange[] original,
        OdfDatabaseRange[] current)
    {
        int index = 0;
        while (index < original.Length)
        {
            if (!ContainsDatabase(current, original[index]))
            {
                RemoveIfAttached(original[index].Node);
            }
            index = index + 1;
        }
    }

    static void RemoveDiscardedConsolidations(
        OdfConsolidation[] original,
        OdfConsolidation[] current)
    {
        int index = 0;
        while (index < original.Length)
        {
            if (!ContainsConsolidation(current, original[index]))
            {
                RemoveIfAttached(original[index].Node);
            }
            index = index + 1;
        }
    }

    static void RemoveDiscardedScenarios(
        OdfScenario[] original,
        OdfScenario[] current)
    {
        int index = 0;
        while (index < original.Length)
        {
            if (!ContainsScenario(current, original[index]))
            {
                RemoveIfAttached(original[index].Node);
            }
            index = index + 1;
        }
    }

    static bool ContainsNamed(
        OdfNamedExpression[] items,
        OdfNamedExpression target)
    {
        int index = 0;
        while (index < items.Length)
        {
            if (items[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool ContainsLabel(OdfLabelRange[] items, OdfLabelRange target)
    {
        int index = 0;
        while (index < items.Length)
        {
            if (items[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool ContainsDatabase(
        OdfDatabaseRange[] items,
        OdfDatabaseRange target)
    {
        int index = 0;
        while (index < items.Length)
        {
            if (items[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool ContainsConsolidation(
        OdfConsolidation[] items,
        OdfConsolidation target)
    {
        int index = 0;
        while (index < items.Length)
        {
            if (items[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool ContainsScenario(OdfScenario[] items, OdfScenario target)
    {
        int index = 0;
        while (index < items.Length)
        {
            if (items[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static OdfNode[] SortNodes(OdfSortRule[] items)
    {
        OdfNode[] result = new OdfNode[items.Length];
        int index = 0;
        while (index < items.Length)
        {
            result[index] = items[index].Node ?? new OdfNode();
            index = index + 1;
        }
        return result;
    }

    static OdfNode[] FilterNodes(OdfFilterRule[] items)
    {
        OdfNode[] result = new OdfNode[items.Length];
        int index = 0;
        while (index < items.Length)
        {
            result[index] = items[index].Node ?? new OdfNode();
            index = index + 1;
        }
        return result;
    }

    static OdfNode[] SubtotalNodes(OdfSubtotalRule[] items)
    {
        OdfNode[] result = new OdfNode[items.Length];
        int index = 0;
        while (index < items.Length)
        {
            result[index] = items[index].Node ?? new OdfNode();
            index = index + 1;
        }
        return result;
    }

    static OdfNode[] SubtotalFieldNodes(OdfSubtotalField[] items)
    {
        OdfNode[] result = new OdfNode[items.Length];
        int index = 0;
        while (index < items.Length)
        {
            result[index] = items[index].Node ?? new OdfNode();
            index = index + 1;
        }
        return result;
    }

    static void RemoveMissingChildNodes(
        OdfNode parent,
        string localName,
        OdfNode[] retained)
    {
        int index = parent.Children.Length - 1;
        while (index >= 0)
        {
            OdfNode child = parent.Children[index];
            if (IsTableElement(child, localName)
                && !ContainsNode(retained, child))
            {
                StyleDocumentModel.RemoveNodeFromParent(child);
            }
            index = index - 1;
        }
    }

    static void RemoveMissingDescendantNodes(
        OdfNode parent,
        string localName,
        OdfNode[] retained)
    {
        int index = parent.Children.Length - 1;
        while (index >= 0)
        {
            OdfNode child = parent.Children[index];
            if (IsTableElement(child, localName)
                && !ContainsNode(retained, child))
            {
                StyleDocumentModel.RemoveNodeFromParent(child);
            }
            else
            {
                RemoveMissingDescendantNodes(child, localName, retained);
            }
            index = index - 1;
        }
    }

    static bool ContainsNode(OdfNode[] nodes, OdfNode target)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            if (nodes[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static OdfNode RequireFeatureParent(
        OdfNode? spreadsheet,
        string containerName)
    {
        OdfNode parent = RequireSpreadsheet(spreadsheet);
        OdfNode? container = FindDirectChild(parent, containerName);
        if (container == null)
        {
            container = CreateTableElement(containerName);
            StyleDocumentModel.AppendNode(parent, container);
        }
        return container;
    }

    static OdfNode RequireSpreadsheet(OdfNode? spreadsheet)
    {
        if (spreadsheet == null)
        {
            throw new OdfEditException(
                "Spreadsheet declarations require an office:spreadsheet element.");
        }
        return spreadsheet;
    }

    static OdfNode EnsureFeatureNode(
        OdfNode? node,
        OdfNode parent,
        string localName)
    {
        if (node != null)
        {
            return node;
        }
        OdfNode result = CreateTableElement(localName);
        StyleDocumentModel.AppendNode(parent, result);
        return result;
    }

    static OdfNode CreateTableElement(string localName)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = new OdfQualifiedName();
        result.Name.NamespaceName = OdfNamespaces.Table;
        result.Name.LocalName = localName;
        result.Name.Prefix = OdfNamespaces.TablePrefix;
        result.WasEmptyElement = true;
        return result;
    }

    static void SetTableElementName(OdfNode node, string localName)
    {
        if (node.Name == null)
        {
            node.Name = new OdfQualifiedName();
        }
        node.Name.NamespaceName = OdfNamespaces.Table;
        node.Name.LocalName = localName;
        node.Name.Prefix = OdfNamespaces.TablePrefix;
    }

    static OdfNode? FindFirstDescendant(OdfNode node, string localName)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (IsTableElement(child, localName))
            {
                return child;
            }
            OdfNode? nested = FindFirstDescendant(child, localName);
            if (nested != null)
            {
                return nested;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode? FindDirectChild(OdfNode node, string localName)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            if (IsTableElement(node.Children[index], localName))
            {
                return node.Children[index];
            }
            index = index + 1;
        }
        return null;
    }

    static OdfTable? FindContainingTable(
        OdfNode node,
        OdfDocument document)
    {
        OdfNode? current = node.Parent;
        while (current != null)
        {
            if (IsTableElement(current, "table"))
            {
                int index = 0;
                while (index < document.Tables.Length)
                {
                    if (document.Tables[index].Node == current)
                    {
                        return document.Tables[index];
                    }
                    index = index + 1;
                }
                return null;
            }
            current = current.Parent;
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
        if (value.Length == 0)
        {
            return defaultValue;
        }
        OdfLexicalValue parsed = OdfLexicalParser.ParseBoolean(value);
        return parsed.IsValid ? parsed.BooleanValue : defaultValue;
    }

    static long ReadNonNegativeInteger(OdfNode node, string localName)
    {
        string value = ReadTableAttribute(node, localName);
        OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(value);
        return parsed.IsValid && parsed.IntegerValue >= 0
            ? parsed.IntegerValue
            : 0;
    }

    static void WriteRequired(
        OdfNode node,
        string localName,
        string value)
    {
        if (value.Length == 0)
        {
            throw new OdfEditException(
                "The spreadsheet declaration attribute " + localName
                + " requires a value.");
        }
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
            StyleDocumentModel.WriteAttribute(
                node,
                OdfNamespaces.Table,
                localName,
                OdfNamespaces.TablePrefix,
                value);
        }
    }

    static void WriteOptionalDefault(
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
            StyleDocumentModel.WriteAttribute(
                node,
                OdfNamespaces.Table,
                localName,
                OdfNamespaces.TablePrefix,
                value);
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
            StyleDocumentModel.WriteAttribute(
                node,
                OdfNamespaces.Table,
                localName,
                OdfNamespaces.TablePrefix,
                value ? "true" : "false");
        }
    }

    static void WriteInteger(OdfNode node, string localName, long value)
    {
        if (value < 0)
        {
            throw new OdfEditException(
                "Spreadsheet field numbers cannot be negative.");
        }
        StyleDocumentModel.WriteAttribute(
            node,
            OdfNamespaces.Table,
            localName,
            OdfNamespaces.TablePrefix,
            value.ToString());
    }

    static void RemoveIfAttached(OdfNode? node)
    {
        if (node != null && node.Parent != null)
        {
            StyleDocumentModel.RemoveNodeFromParent(node);
        }
    }

    static bool IsTableElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Table
            && node.Name.LocalName == localName;
    }

    static bool IsOfficeElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Office
            && node.Name.LocalName == localName;
    }
}
