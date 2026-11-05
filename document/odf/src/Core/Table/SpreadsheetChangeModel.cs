using System;
using System.Collections.Generic;
using System.Text;

namespace odf;

internal class SpreadsheetChangeState
{
    internal OdfNode? SpreadsheetNode;
    internal OdfNode? TrackedChangesNode;
    internal bool OriginalTrackChanges;
    internal OdfDetectiveRange[] OriginalRanges = [];
    internal OdfDetectiveOperation[] OriginalOperations = [];
    internal OdfSpreadsheetChange[] OriginalChanges = [];
}

internal static class SpreadsheetChangeModel
{
    internal static void LoadSpreadsheetChanges(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for spreadsheet-change materialization.");
        }
        SpreadsheetChangeState state = new SpreadsheetChangeState();
        List<OdfDetectiveRange> ranges =
            new List<OdfDetectiveRange>();
        List<OdfDetectiveOperation> operations =
            new List<OdfDetectiveOperation>();
        List<OdfSpreadsheetChange> changes =
            new List<OdfSpreadsheetChange>();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfNode? root = document.Parts[partIndex].Root;
            if (root != null)
            {
                CollectChanges(
                    root,
                    document,
                    state,
                    ranges,
                    operations,
                    changes);
            }
            partIndex = partIndex + 1;
        }
        document.DetectiveRanges = ranges.ToArray();
        document.DetectiveOperations = operations.ToArray();
        document.SpreadsheetChanges = changes.ToArray();
        document.TrackSpreadsheetChanges = state.TrackedChangesNode != null
            && ReadBoolean(
                state.TrackedChangesNode,
                "track-changes",
                false);
        state.OriginalTrackChanges = document.TrackSpreadsheetChanges;
        state.OriginalRanges = document.DetectiveRanges;
        state.OriginalOperations = document.DetectiveOperations;
        state.OriginalChanges = document.SpreadsheetChanges;
        document.SpreadsheetChangeState = state;
    }

    internal static void SynchronizeSpreadsheetChanges(OdfDocument document)
    {
        if (document == null
            || document.DetectiveRanges == null
            || document.DetectiveOperations == null
            || document.SpreadsheetChanges == null)
        {
            throw new OdfDocumentException(
                "Spreadsheet detective and change collections are required for writing.");
        }
        SpreadsheetChangeState? state = document.SpreadsheetChangeState;
        if (state == null)
        {
            LoadSpreadsheetChanges(document);
            state = document.SpreadsheetChangeState;
        }
        if (state == null)
        {
            return;
        }
        RemoveDeletedRanges(state.OriginalRanges, document.DetectiveRanges);
        RemoveDeletedOperations(
            state.OriginalOperations,
            document.DetectiveOperations);
        RemoveDeletedChanges(
            state.OriginalChanges,
            document.SpreadsheetChanges);
        SynchronizeDetectiveRanges(document.DetectiveRanges);
        SynchronizeDetectiveOperations(document.DetectiveOperations);
        SynchronizeTrackedChanges(document, state);
        state.OriginalTrackChanges = document.TrackSpreadsheetChanges;
        state.OriginalRanges = document.DetectiveRanges;
        state.OriginalOperations = document.DetectiveOperations;
        state.OriginalChanges = document.SpreadsheetChanges;
    }

    static void CollectChanges(
        OdfNode node,
        OdfDocument document,
        SpreadsheetChangeState state,
        List<OdfDetectiveRange> ranges,
        List<OdfDetectiveOperation> operations,
        List<OdfSpreadsheetChange> changes)
    {
        if (IsOfficeElement(node, "spreadsheet")
            && state.SpreadsheetNode == null)
        {
            state.SpreadsheetNode = node;
        }
        if (IsTableElement(node, "tracked-changes"))
        {
            if (state.TrackedChangesNode == null)
            {
                state.TrackedChangesNode = node;
            }
            CollectTrackedChangeChildren(node, changes);
            return;
        }
        if (IsTableElement(node, "highlighted-range"))
        {
            ranges.Add(CreateDetectiveRange(node, document));
        }
        else if (IsTableElement(node, "operation"))
        {
            operations.Add(CreateDetectiveOperation(node, document));
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            CollectChanges(
                node.Children[index],
                document,
                state,
                ranges,
                operations,
                changes);
            index = index + 1;
        }
    }

    static void CollectTrackedChangeChildren(
        OdfNode tracked,
        List<OdfSpreadsheetChange> changes)
    {
        int index = 0;
        while (index < tracked.Children.Length)
        {
            OdfNode child = tracked.Children[index];
            if (IsTrackedChangeNode(child))
            {
                changes.Add(CreateSpreadsheetChange(child));
            }
            index = index + 1;
        }
    }

    static OdfDetectiveRange CreateDetectiveRange(
        OdfNode node,
        OdfDocument document)
    {
        OdfDetectiveRange result = new OdfDetectiveRange();
        result.Node = node;
        result.RangeAddress = ReadTableAttribute(
            node,
            "cell-range-address");
        result.Direction = ReadDefaultToken(
            node,
            "direction",
            "from-same-table");
        result.ContainsError = ReadBoolean(node, "contains-error", false);
        result.MarkedInvalid = ReadBoolean(node, "marked-invalid", false);
        result.Cell = FindOwningCell(document, node);
        return result;
    }

    static OdfDetectiveOperation CreateDetectiveOperation(
        OdfNode node,
        OdfDocument document)
    {
        OdfDetectiveOperation result = new OdfDetectiveOperation();
        result.Node = node;
        result.Name = ReadDefaultToken(
            node,
            "name",
            "trace-precedents");
        result.Index = ReadInteger(node, "index", 0);
        result.Cell = FindOwningCell(document, node);
        return result;
    }

    static OdfSpreadsheetChange CreateSpreadsheetChange(OdfNode node)
    {
        OdfSpreadsheetChange result = new OdfSpreadsheetChange();
        result.Node = node;
        result.Kind = ChangeKind(node);
        result.Id = ReadTableAttribute(node, "id");
        result.AcceptanceState = ReadDefaultToken(
            node,
            "acceptance-state",
            "pending");
        result.RejectingChangeId = ReadTableAttribute(
            node,
            "rejecting-change-id");
        ReadChangeInfo(result, node);
        if (result.Kind == "insertion" || result.Kind == "deletion")
        {
            result.Axis = ReadTableAttribute(node, "type");
            result.Position = ReadInteger(node, "position", 0);
            result.Count = ReadInteger(node, "count", 1);
            result.TableIndex = ReadInteger(node, "table", 0);
            result.MultiDeletionSpanned = ReadInteger(
                node,
                "multi-deletion-spanned",
                0);
        }
        else if (result.Kind == "cell-content")
        {
            OdfNode? address = FindDirectChild(node, "cell-address");
            if (address != null)
            {
                result.CellColumn = ReadInteger(address, "column", 0);
                result.CellRow = ReadInteger(address, "row", 0);
                result.CellTable = ReadInteger(address, "table", 0);
            }
        }
        else if (result.Kind == "movement")
        {
            OdfNode? source = FindDirectChild(
                node,
                "source-range-address");
            OdfNode? target = FindDirectChild(
                node,
                "target-range-address");
            ReadMovementRange(result, source, true);
            ReadMovementRange(result, target, false);
        }
        LoadChangeStructures(result, node);
        return result;
    }

    static void LoadChangeStructures(
        OdfSpreadsheetChange change,
        OdfNode node)
    {
        change.Dependencies = LoadChangeDependencies(
            FindDirectChild(node, "dependencies"));
        change.Deletions = LoadChangeDeletions(
            FindDirectChild(node, "deletions"));
        change.CutOffs = LoadChangeCutOffs(
            FindDirectChild(node, "cut-offs"));
        OdfNode? previous = FindDirectChild(node, "previous");
        if (previous != null)
        {
            change.PreviousId = ReadTableAttribute(previous, "id");
            OdfNode? cell = FindDirectChild(
                previous, "change-track-table-cell");
            if (cell == null)
            {
                cell = FindDirectChild(previous, "table-cell");
            }
            if (cell != null)
            {
                change.PreviousCell = CreateChangeCell(cell);
            }
        }
    }

    static OdfSpreadsheetChangeDependency[] LoadChangeDependencies(
        OdfNode? container)
    {
        if (container == null)
        {
            return [];
        }
        List<OdfSpreadsheetChangeDependency> result =
            new List<OdfSpreadsheetChangeDependency>();
        int index = 0;
        while (index < container.Children.Length)
        {
            OdfNode child = container.Children[index];
            if (IsTableElement(child, "dependency"))
            {
                OdfSpreadsheetChangeDependency dependency =
                    new OdfSpreadsheetChangeDependency();
                dependency.Id = ReadTableAttribute(child, "id");
                dependency.Node = child;
                result.Add(dependency);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfSpreadsheetChangeDeletion[] LoadChangeDeletions(
        OdfNode? container)
    {
        if (container == null)
        {
            return [];
        }
        List<OdfSpreadsheetChangeDeletion> result =
            new List<OdfSpreadsheetChangeDeletion>();
        int index = 0;
        while (index < container.Children.Length)
        {
            OdfNode child = container.Children[index];
            if (IsTableElement(child, "cell-content-deletion")
                || IsTableElement(child, "change-deletion"))
            {
                OdfSpreadsheetChangeDeletion deletion =
                    new OdfSpreadsheetChangeDeletion();
                deletion.Kind = IsTableElement(child, "change-deletion")
                    ? "change" : "cell-content";
                deletion.Id = ReadTableAttribute(child, "id");
                deletion.Node = child;
                OdfNode? address = FindDirectChild(child, "cell-address");
                if (address != null)
                {
                    deletion.HasCellAddress = true;
                    deletion.CellColumn = ReadInteger(address, "column", 0);
                    deletion.CellRow = ReadInteger(address, "row", 0);
                    deletion.CellTable = ReadInteger(address, "table", 0);
                }
                OdfNode? cell = FindDirectChild(
                    child, "change-track-table-cell");
                if (cell != null)
                {
                    deletion.Cell = CreateChangeCell(cell);
                }
                result.Add(deletion);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfSpreadsheetChangeCutOff[] LoadChangeCutOffs(
        OdfNode? container)
    {
        if (container == null)
        {
            return [];
        }
        List<OdfSpreadsheetChangeCutOff> result =
            new List<OdfSpreadsheetChangeCutOff>();
        int index = 0;
        while (index < container.Children.Length)
        {
            OdfNode child = container.Children[index];
            if (IsTableElement(child, "insertion-cut-off")
                || IsTableElement(child, "movement-cut-off"))
            {
                OdfSpreadsheetChangeCutOff cutOff =
                    new OdfSpreadsheetChangeCutOff();
                cutOff.Kind = IsTableElement(child, "insertion-cut-off")
                    ? "insertion" : "movement";
                cutOff.Id = ReadTableAttribute(child, "id");
                string position = ReadTableAttribute(child, "position");
                string start = ReadTableAttribute(child, "start-position");
                string end = ReadTableAttribute(child, "end-position");
                cutOff.HasPosition = position.Length > 0;
                cutOff.Position = ReadInteger(child, "position", 0);
                cutOff.HasRange = start.Length > 0 && end.Length > 0;
                cutOff.StartPosition = ReadInteger(
                    child, "start-position", 0);
                cutOff.EndPosition = ReadInteger(child, "end-position", 0);
                cutOff.Node = child;
                result.Add(cutOff);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static OdfSpreadsheetChangeCell CreateChangeCell(OdfNode node)
    {
        OdfSpreadsheetChangeCell result = new OdfSpreadsheetChangeCell();
        result.Node = node;
        result.CellAddress = ReadTableAttribute(node, "cell-address");
        result.MatrixCovered = ReadBoolean(node, "matrix-covered", false);
        result.Formula = ReadTableAttribute(node, "formula");
        result.MatrixColumnsSpanned = ReadInteger(
            node, "number-matrix-columns-spanned", 0);
        result.MatrixRowsSpanned = ReadInteger(
            node, "number-matrix-rows-spanned", 0);
        result.ValueType = ReadOfficeAttribute(node, "value-type");
        result.Value = ReadChangeCellValue(node, result.ValueType);
        result.Currency = ReadOfficeAttribute(node, "currency");
        result.Text = ExtractText(node);
        return result;
    }

    static string ReadChangeCellValue(OdfNode node, string valueType)
    {
        if (valueType == "date")
        {
            return ReadOfficeAttribute(node, "date-value");
        }
        if (valueType == "time")
        {
            return ReadOfficeAttribute(node, "time-value");
        }
        if (valueType == "boolean")
        {
            return ReadOfficeAttribute(node, "boolean-value");
        }
        if (valueType == "string" || valueType == "error")
        {
            return ReadOfficeAttribute(node, "string-value");
        }
        return ReadOfficeAttribute(node, "value");
    }

    static void ReadChangeInfo(
        OdfSpreadsheetChange change,
        OdfNode node)
    {
        OdfNode? info = FindDirectOfficeChild(node, "change-info");
        if (info == null)
        {
            return;
        }
        OdfNode? creator = FindDirectQualifiedChild(
            info,
            OdfNamespaces.DublinCore,
            "creator");
        OdfNode? date = FindDirectQualifiedChild(
            info,
            OdfNamespaces.DublinCore,
            "date");
        change.Creator = ExtractText(creator);
        change.Date = ExtractText(date);
        OdfNode? comment = FindDirectQualifiedChild(
            info,
            OdfNamespaces.Text,
            "p");
        change.Comment = ExtractText(comment);
    }

    static void ReadMovementRange(
        OdfSpreadsheetChange change,
        OdfNode? node,
        bool source)
    {
        if (node == null)
        {
            return;
        }
        long startColumn = ReadInteger(
            node,
            "start-column",
            ReadInteger(node, "column", 0));
        long startRow = ReadInteger(
            node,
            "start-row",
            ReadInteger(node, "row", 0));
        long startTable = ReadInteger(
            node,
            "start-table",
            ReadInteger(node, "table", 0));
        long endColumn = ReadInteger(node, "end-column", startColumn);
        long endRow = ReadInteger(node, "end-row", startRow);
        long endTable = ReadInteger(node, "end-table", startTable);
        if (source)
        {
            change.SourceStartColumn = startColumn;
            change.SourceStartRow = startRow;
            change.SourceStartTable = startTable;
            change.SourceEndColumn = endColumn;
            change.SourceEndRow = endRow;
            change.SourceEndTable = endTable;
        }
        else
        {
            change.TargetStartColumn = startColumn;
            change.TargetStartRow = startRow;
            change.TargetStartTable = startTable;
            change.TargetEndColumn = endColumn;
            change.TargetEndRow = endRow;
            change.TargetEndTable = endTable;
        }
    }

    static void SynchronizeDetectiveRanges(OdfDetectiveRange[] ranges)
    {
        int index = 0;
        while (index < ranges.Length)
        {
            OdfDetectiveRange range = ranges[index];
            if (range == null || range.Cell == null || range.Cell.Node == null)
            {
                throw new OdfEditException(
                    "Each detective range requires an attached owner cell.");
            }
            OdfNode detective = EnsureDetective(range.Cell.Node);
            OdfNode? node = range.Node;
            if (node == null)
            {
                node = CreateTableElement(
                    "highlighted-range",
                    detective.Location.PartPath);
                InsertDetectiveChild(detective, node, false);
                range.Node = node;
            }
            else if (node.Parent != detective)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
                InsertDetectiveChild(detective, node, false);
            }
            if (range.MarkedInvalid)
            {
                WriteOptional(node, "cell-range-address", "");
                WriteOptional(node, "direction", "");
                WriteOptional(node, "contains-error", "");
                WriteRequired(node, "marked-invalid", "true");
            }
            else
            {
                if (range.RangeAddress.Length == 0)
                {
                    throw new OdfEditException(
                        "A valid detective range requires a cell range address.");
                }
                WriteRequired(
                    node,
                    "cell-range-address",
                    range.RangeAddress);
                WriteRequired(node, "direction", range.Direction);
                WriteBoolean(
                    node,
                    "contains-error",
                    range.ContainsError,
                    false);
                WriteOptional(node, "marked-invalid", "");
            }
            index = index + 1;
        }
    }

    static void SynchronizeDetectiveOperations(
        OdfDetectiveOperation[] operations)
    {
        int index = 0;
        while (index < operations.Length)
        {
            OdfDetectiveOperation operation = operations[index];
            if (operation == null
                || operation.Cell == null
                || operation.Cell.Node == null
                || operation.Name.Length == 0
                || operation.Index < 0)
            {
                throw new OdfEditException(
                    "Each detective operation requires an attached cell, name, and non-negative index.");
            }
            OdfNode detective = EnsureDetective(operation.Cell.Node);
            OdfNode? node = operation.Node;
            if (node == null)
            {
                node = CreateTableElement(
                    "operation",
                    detective.Location.PartPath);
                InsertDetectiveChild(detective, node, true);
                operation.Node = node;
            }
            else if (node.Parent != detective)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
                InsertDetectiveChild(detective, node, true);
            }
            WriteRequired(node, "name", operation.Name);
            WriteRequired(node, "index", operation.Index.ToString());
            index = index + 1;
        }
    }

    static void SynchronizeTrackedChanges(
        OdfDocument document,
        SpreadsheetChangeState state)
    {
        OdfNode? tracked = state.TrackedChangesNode;
        if (document.SpreadsheetChanges.Length == 0
            && !document.TrackSpreadsheetChanges)
        {
            if (tracked != null)
            {
                StyleDocumentModel.RemoveNodeFromParent(tracked);
            }
            state.TrackedChangesNode = null;
            return;
        }
        if (tracked == null)
        {
            if (state.SpreadsheetNode == null)
            {
                throw new OdfEditException(
                    "A spreadsheet body is required for tracked changes.");
            }
            tracked = CreateTableElement(
                "tracked-changes",
                state.SpreadsheetNode.Location.PartPath);
            InsertNodeAt(state.SpreadsheetNode, tracked, 0);
            state.TrackedChangesNode = tracked;
        }
        WriteBoolean(
            tracked,
            "track-changes",
            document.TrackSpreadsheetChanges,
            false);
        int index = 0;
        while (index < document.SpreadsheetChanges.Length)
        {
            OdfSpreadsheetChange change = document.SpreadsheetChanges[index];
            if (change == null
                || change.Id.Length == 0
                || change.Kind.Length == 0
                || change.Creator.Length == 0
                || change.Date.Length == 0)
            {
                throw new OdfEditException(
                    "Each spreadsheet change requires an id, kind, creator, and date.");
            }
            string localName = ChangeLocalName(change.Kind);
            if (localName.Length == 0)
            {
                throw new OdfEditException(
                    "The spreadsheet change kind is not recognized.");
            }
            OdfNode? node = change.Node;
            if (node == null)
            {
                node = CreateTableElement(
                    localName,
                    tracked.Location.PartPath);
                StyleDocumentModel.AppendNode(tracked, node);
                change.Node = node;
            }
            else if (node.Name != null)
            {
                node.Name.LocalName = localName;
            }
            WriteRequired(node, "id", change.Id);
            WriteDefaultToken(
                node,
                "acceptance-state",
                change.AcceptanceState,
                "pending");
            WriteOptional(
                node,
                "rejecting-change-id",
                change.RejectingChangeId);
            SynchronizeChangeKind(change, node);
            index = index + 1;
        }
    }

    static void SynchronizeChangeKind(
        OdfSpreadsheetChange change,
        OdfNode node)
    {
        if (change.Kind == "insertion" || change.Kind == "deletion")
        {
            if (change.Axis.Length == 0
                || change.Position < 0
                || change.TableIndex < 0
                || change.Kind == "insertion" && change.Count <= 0)
            {
                throw new OdfEditException(
                    "Insertion and deletion changes require valid axis, position, table, and count values.");
            }
            WriteRequired(node, "type", change.Axis);
            WriteRequired(node, "position", change.Position.ToString());
            WriteInteger(node, "table", change.TableIndex, 0);
            if (change.Kind == "insertion")
            {
                WriteInteger(node, "count", change.Count, 1);
            }
            else
            {
                WriteInteger(
                    node,
                    "multi-deletion-spanned",
                    change.MultiDeletionSpanned,
                    0);
            }
            SynchronizeChangeInfo(change, node, 1);
        }
        else if (change.Kind == "cell-content")
        {
            OdfNode address = EnsureChangeChild(node, "cell-address", 1);
            WriteRequired(address, "column", change.CellColumn.ToString());
            WriteRequired(address, "row", change.CellRow.ToString());
            WriteRequired(address, "table", change.CellTable.ToString());
            SynchronizeChangeInfo(change, node, 2);
        }
        else if (change.Kind == "movement")
        {
            OdfNode source = EnsureChangeChild(
                node,
                "source-range-address",
                1);
            OdfNode target = EnsureChangeChild(
                node,
                "target-range-address",
                2);
            WriteMovementRange(change, source, true);
            WriteMovementRange(change, target, false);
            SynchronizeChangeInfo(change, node, 3);
        }
        SynchronizeChangeStructures(change, node);
    }

    static void SynchronizeChangeStructures(
        OdfSpreadsheetChange change,
        OdfNode node)
    {
        SynchronizeChangeDependencies(change, node);
        SynchronizeChangeDeletions(change, node);
        SynchronizeChangeCutOffs(change, node);
        if (change.Kind == "cell-content")
        {
            OdfNode previous = EnsureChangeChild(node, "previous", 6);
            WriteOptional(previous, "id", change.PreviousId);
            OdfSpreadsheetChangeCell cell = change.PreviousCell
                ?? new OdfSpreadsheetChangeCell();
            OdfNode cellNode = EnsurePreviousChangeCell(previous);
            SynchronizeChangeCell(cell, cellNode);
            change.PreviousCell = cell;
        }
    }

    static void SynchronizeChangeDependencies(
        OdfSpreadsheetChange change,
        OdfNode node)
    {
        OdfNode? container = FindDirectChild(node, "dependencies");
        if (change.Dependencies.Length == 0)
        {
            RemoveOptionalChangeContainer(container);
            return;
        }
        if (container == null)
        {
            container = CreateTableElement(
                "dependencies", node.Location.PartPath);
            InsertChangeChild(node, container, 4);
        }
        OdfNode[] children = new OdfNode[change.Dependencies.Length];
        int index = 0;
        while (index < change.Dependencies.Length)
        {
            OdfSpreadsheetChangeDependency dependency =
                change.Dependencies[index];
            if (dependency == null || dependency.Id.Length == 0)
            {
                throw new OdfEditException(
                    "Each spreadsheet change dependency requires an id.");
            }
            OdfNode child = dependency.Node
                ?? CreateTableElement("dependency", node.Location.PartPath);
            WriteRequired(child, "id", dependency.Id);
            dependency.Node = child;
            children[index] = child;
            index = index + 1;
        }
        ReplaceChildren(container, children);
    }

    static void SynchronizeChangeDeletions(
        OdfSpreadsheetChange change,
        OdfNode node)
    {
        OdfNode? container = FindDirectChild(node, "deletions");
        if (change.Deletions.Length == 0)
        {
            RemoveOptionalChangeContainer(container);
            return;
        }
        if (container == null)
        {
            container = CreateTableElement("deletions", node.Location.PartPath);
            InsertChangeChild(node, container, 5);
        }
        OdfNode[] children = new OdfNode[change.Deletions.Length];
        int index = 0;
        while (index < change.Deletions.Length)
        {
            OdfSpreadsheetChangeDeletion deletion = change.Deletions[index];
            if (deletion == null
                || deletion.Kind != "cell-content"
                    && deletion.Kind != "change")
            {
                throw new OdfEditException(
                    "A spreadsheet deletion requires a recognized kind.");
            }
            string localName = deletion.Kind == "change"
                ? "change-deletion" : "cell-content-deletion";
            OdfNode child = deletion.Node
                ?? CreateTableElement(localName, node.Location.PartPath);
            if (child.Name != null)
            {
                child.Name.LocalName = localName;
            }
            WriteOptional(child, "id", deletion.Id);
            SynchronizeDeletionDetails(deletion, child);
            deletion.Node = child;
            children[index] = child;
            index = index + 1;
        }
        ReplaceChildren(container, children);
    }

    static void SynchronizeDeletionDetails(
        OdfSpreadsheetChangeDeletion deletion,
        OdfNode node)
    {
        if (deletion.Kind == "change")
        {
            ReplaceChildren(node, []);
            return;
        }
        List<OdfNode> children = new List<OdfNode>();
        if (deletion.HasCellAddress)
        {
            OdfNode address = FindDirectChild(node, "cell-address")
                ?? CreateTableElement(
                    "cell-address", node.Location.PartPath);
            WriteRequired(address, "column", deletion.CellColumn.ToString());
            WriteRequired(address, "row", deletion.CellRow.ToString());
            WriteRequired(address, "table", deletion.CellTable.ToString());
            children.Add(address);
        }
        if (deletion.Cell != null)
        {
            OdfNode cell = deletion.Cell.Node
                ?? CreateTableElement(
                    "change-track-table-cell", node.Location.PartPath);
            SynchronizeChangeCell(deletion.Cell, cell);
            deletion.Cell.Node = cell;
            children.Add(cell);
        }
        ReplaceChildren(node, children.ToArray());
    }

    static void SynchronizeChangeCutOffs(
        OdfSpreadsheetChange change,
        OdfNode node)
    {
        OdfNode? container = FindDirectChild(node, "cut-offs");
        if (change.CutOffs.Length == 0)
        {
            RemoveOptionalChangeContainer(container);
            return;
        }
        if (change.Kind != "deletion")
        {
            throw new OdfEditException(
                "Only spreadsheet deletions can contain cut-offs.");
        }
        if (container == null)
        {
            container = CreateTableElement("cut-offs", node.Location.PartPath);
            InsertChangeChild(node, container, 6);
        }
        OdfNode[] children = new OdfNode[change.CutOffs.Length];
        int index = 0;
        while (index < change.CutOffs.Length)
        {
            OdfSpreadsheetChangeCutOff cutOff = change.CutOffs[index];
            if (cutOff == null
                || cutOff.Kind != "insertion" && cutOff.Kind != "movement")
            {
                throw new OdfEditException(
                    "A spreadsheet cut-off requires a recognized kind.");
            }
            string localName = cutOff.Kind == "insertion"
                ? "insertion-cut-off" : "movement-cut-off";
            OdfNode child = cutOff.Node
                ?? CreateTableElement(localName, node.Location.PartPath);
            if (child.Name != null)
            {
                child.Name.LocalName = localName;
            }
            if (cutOff.Kind == "insertion")
            {
                if (cutOff.Id.Length == 0)
                {
                    throw new OdfEditException(
                        "An insertion cut-off requires an id.");
                }
                WriteRequired(child, "id", cutOff.Id);
                WriteRequired(child, "position", cutOff.Position.ToString());
                WriteOptional(child, "start-position", "");
                WriteOptional(child, "end-position", "");
            }
            else
            {
                WriteOptional(child, "id", "");
                SynchronizeMovementCutOff(cutOff, child);
            }
            cutOff.Node = child;
            children[index] = child;
            index = index + 1;
        }
        ReplaceChildren(container, children);
    }

    static void SynchronizeMovementCutOff(
        OdfSpreadsheetChangeCutOff cutOff,
        OdfNode node)
    {
        if (cutOff.HasRange)
        {
            WriteOptional(node, "position", "");
            WriteRequired(
                node, "start-position", cutOff.StartPosition.ToString());
            WriteRequired(node, "end-position", cutOff.EndPosition.ToString());
        }
        else
        {
            WriteRequired(node, "position", cutOff.Position.ToString());
            WriteOptional(node, "start-position", "");
            WriteOptional(node, "end-position", "");
            cutOff.HasPosition = true;
        }
    }

    static void SynchronizeChangeCell(
        OdfSpreadsheetChangeCell cell,
        OdfNode node)
    {
        if (node.Name != null)
        {
            node.Name.LocalName = "change-track-table-cell";
        }
        WriteOptional(node, "cell-address", cell.CellAddress);
        WriteBoolean(node, "matrix-covered", cell.MatrixCovered, false);
        WriteOptional(node, "formula", cell.Formula);
        WriteInteger(
            node,
            "number-matrix-columns-spanned",
            cell.MatrixColumnsSpanned,
            0);
        WriteInteger(
            node,
            "number-matrix-rows-spanned",
            cell.MatrixRowsSpanned,
            0);
        WriteOfficeOptional(node, "value-type", cell.ValueType);
        WriteChangeCellValue(node, cell.ValueType, cell.Value);
        WriteOfficeOptional(node, "currency", cell.Currency);
        if (cell.Text.Length == 0)
        {
            ReplaceChildren(node, []);
        }
        else
        {
            OdfNode paragraph = CreateQualifiedTextElement(
                OdfNamespaces.Text,
                "p",
                OdfNamespaces.TextPrefix,
                cell.Text,
                node.Location.PartPath);
            ReplaceChildren(node, [paragraph]);
        }
        cell.Node = node;
    }

    static void WriteChangeCellValue(
        OdfNode node,
        string valueType,
        string value)
    {
        string attribute = "value";
        if (valueType == "date")
        {
            attribute = "date-value";
        }
        else if (valueType == "time")
        {
            attribute = "time-value";
        }
        else if (valueType == "boolean")
        {
            attribute = "boolean-value";
        }
        else if (valueType == "string" || valueType == "error")
        {
            attribute = "string-value";
        }
        WriteOfficeOptional(node, "value", "");
        WriteOfficeOptional(node, "date-value", "");
        WriteOfficeOptional(node, "time-value", "");
        WriteOfficeOptional(node, "boolean-value", "");
        WriteOfficeOptional(node, "string-value", "");
        WriteOfficeOptional(node, attribute, value);
    }

    static OdfNode EnsurePreviousChangeCell(OdfNode previous)
    {
        OdfNode? cell = FindDirectChild(
            previous, "change-track-table-cell");
        if (cell == null)
        {
            cell = FindDirectChild(previous, "table-cell");
        }
        if (cell == null)
        {
            cell = CreateTableElement(
                "change-track-table-cell", previous.Location.PartPath);
            StyleDocumentModel.AppendNode(previous, cell);
        }
        if (cell.Name != null)
        {
            cell.Name.LocalName = "change-track-table-cell";
        }
        return cell;
    }

    static void RemoveOptionalChangeContainer(OdfNode? container)
    {
        if (container != null)
        {
            StyleDocumentModel.RemoveNodeFromParent(container);
        }
    }

    static void SynchronizeChangeInfo(
        OdfSpreadsheetChange change,
        OdfNode node,
        int rank)
    {
        OdfNode? info = FindDirectOfficeChild(node, "change-info");
        if (info == null)
        {
            info = CreateQualifiedElement(
                OdfNamespaces.Office,
                "change-info",
                OdfNamespaces.OfficePrefix,
                node.Location.PartPath);
            InsertChangeChild(node, info, rank);
        }
        OdfNode creator = CreateQualifiedTextElement(
            OdfNamespaces.DublinCore,
            "creator",
            OdfNamespaces.DublinCorePrefix,
            change.Creator,
            info.Location.PartPath);
        OdfNode date = CreateQualifiedTextElement(
            OdfNamespaces.DublinCore,
            "date",
            OdfNamespaces.DublinCorePrefix,
            change.Date,
            info.Location.PartPath);
        if (change.Comment.Length == 0)
        {
            ReplaceChildren(info, [creator, date]);
        }
        else
        {
            OdfNode comment = CreateQualifiedTextElement(
                OdfNamespaces.Text,
                "p",
                OdfNamespaces.TextPrefix,
                change.Comment,
                info.Location.PartPath);
            ReplaceChildren(info, [creator, date, comment]);
        }
    }

    static void WriteMovementRange(
        OdfSpreadsheetChange change,
        OdfNode node,
        bool source)
    {
        long startColumn = source
            ? change.SourceStartColumn
            : change.TargetStartColumn;
        long startRow = source
            ? change.SourceStartRow
            : change.TargetStartRow;
        long startTable = source
            ? change.SourceStartTable
            : change.TargetStartTable;
        long endColumn = source
            ? change.SourceEndColumn
            : change.TargetEndColumn;
        long endRow = source
            ? change.SourceEndRow
            : change.TargetEndRow;
        long endTable = source
            ? change.SourceEndTable
            : change.TargetEndTable;
        WriteRequired(node, "start-column", startColumn.ToString());
        WriteRequired(node, "start-row", startRow.ToString());
        WriteRequired(node, "start-table", startTable.ToString());
        WriteRequired(node, "end-column", endColumn.ToString());
        WriteRequired(node, "end-row", endRow.ToString());
        WriteRequired(node, "end-table", endTable.ToString());
        WriteOptional(node, "column", "");
        WriteOptional(node, "row", "");
        WriteOptional(node, "table", "");
    }

    static void RemoveDeletedRanges(
        OdfDetectiveRange[] original,
        OdfDetectiveRange[] current)
    {
        int index = 0;
        while (index < original.Length)
        {
            OdfNode? node = original[index].Node;
            if (node != null && !ContainsRangeNode(current, node))
            {
                RemoveDetectiveNode(node);
            }
            index = index + 1;
        }
    }

    static bool ContainsRangeNode(
        OdfDetectiveRange[] ranges,
        OdfNode node)
    {
        int index = 0;
        while (index < ranges.Length)
        {
            OdfDetectiveRange range = ranges[index];
            if (range != null && range.Node == node)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void RemoveDeletedOperations(
        OdfDetectiveOperation[] original,
        OdfDetectiveOperation[] current)
    {
        int index = 0;
        while (index < original.Length)
        {
            OdfNode? node = original[index].Node;
            if (node != null && !ContainsOperationNode(current, node))
            {
                RemoveDetectiveNode(node);
            }
            index = index + 1;
        }
    }

    static bool ContainsOperationNode(
        OdfDetectiveOperation[] operations,
        OdfNode node)
    {
        int index = 0;
        while (index < operations.Length)
        {
            OdfDetectiveOperation operation = operations[index];
            if (operation != null && operation.Node == node)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void RemoveDeletedChanges(
        OdfSpreadsheetChange[] original,
        OdfSpreadsheetChange[] current)
    {
        int index = 0;
        while (index < original.Length)
        {
            OdfNode? node = original[index].Node;
            if (node != null && !ContainsChangeNode(current, node))
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
            }
            index = index + 1;
        }
    }

    static bool ContainsChangeNode(
        OdfSpreadsheetChange[] changes,
        OdfNode node)
    {
        int index = 0;
        while (index < changes.Length)
        {
            OdfSpreadsheetChange change = changes[index];
            if (change != null && change.Node == node)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void RemoveDetectiveNode(OdfNode node)
    {
        OdfNode? detective = node.Parent;
        StyleDocumentModel.RemoveNodeFromParent(node);
        if (detective != null && detective.Children.Length == 0)
        {
            StyleDocumentModel.RemoveNodeFromParent(detective);
        }
    }

    static OdfNode EnsureDetective(OdfNode cell)
    {
        OdfNode? detective = FindDirectChild(cell, "detective");
        if (detective != null)
        {
            return detective;
        }
        detective = CreateTableElement(
            "detective",
            cell.Location.PartPath);
        int insertion = cell.Children.Length;
        int index = 0;
        while (index < cell.Children.Length)
        {
            OdfNode child = cell.Children[index];
            if (child.Kind == OdfNodeKind.Text
                || child.Name != null
                    && child.Name.NamespaceName == OdfNamespaces.Text)
            {
                insertion = index;
                index = cell.Children.Length;
            }
            else
            {
                index = index + 1;
            }
        }
        InsertNodeAt(cell, detective, insertion);
        return detective;
    }

    static void InsertDetectiveChild(
        OdfNode detective,
        OdfNode child,
        bool operation)
    {
        int insertion = detective.Children.Length;
        if (!operation)
        {
            int index = 0;
            while (index < detective.Children.Length)
            {
                if (IsTableElement(detective.Children[index], "operation"))
                {
                    insertion = index;
                    index = detective.Children.Length;
                }
                else
                {
                    index = index + 1;
                }
            }
        }
        InsertNodeAt(detective, child, insertion);
    }

    static OdfNode EnsureChangeChild(
        OdfNode parent,
        string localName,
        int rank)
    {
        OdfNode? node = FindDirectChild(parent, localName);
        if (node != null)
        {
            return node;
        }
        node = CreateTableElement(localName, parent.Location.PartPath);
        InsertChangeChild(parent, node, rank);
        return node;
    }

    static void InsertChangeChild(OdfNode parent, OdfNode child, int rank)
    {
        int insertion = parent.Children.Length;
        int index = 0;
        while (index < parent.Children.Length)
        {
            if (ChangeChildRank(parent.Children[index]) > rank)
            {
                insertion = index;
                index = parent.Children.Length;
            }
            else
            {
                index = index + 1;
            }
        }
        InsertNodeAt(parent, child, insertion);
    }

    static int ChangeChildRank(OdfNode node)
    {
        if (IsTableElement(node, "cell-address")
            || IsTableElement(node, "source-range-address"))
        {
            return 1;
        }
        if (IsTableElement(node, "target-range-address"))
        {
            return 2;
        }
        if (node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Office
            && node.Name.LocalName == "change-info")
        {
            return 3;
        }
        if (IsTableElement(node, "dependencies"))
        {
            return 4;
        }
        if (IsTableElement(node, "deletions"))
        {
            return 5;
        }
        if (IsTableElement(node, "previous"))
        {
            return 6;
        }
        return 10;
    }

    static void ReplaceChildren(OdfNode parent, OdfNode[] children)
    {
        int index = 0;
        while (index < parent.Children.Length)
        {
            parent.Children[index].Parent = null;
            index = index + 1;
        }
        parent.Children = children;
        index = 0;
        while (index < children.Length)
        {
            children[index].Parent = parent;
            children[index].Location.PartPath = parent.Location.PartPath;
            index = index + 1;
        }
        parent.WasEmptyElement = children.Length == 0;
    }

    static void InsertNodeAt(OdfNode parent, OdfNode child, int insertion)
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

    static OdfCell? FindOwningCell(OdfDocument document, OdfNode node)
    {
        OdfNode? current = node.Parent;
        while (current != null && !IsCellElement(current))
        {
            current = current.Parent;
        }
        if (current == null)
        {
            return null;
        }
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
                    if (row.Cells[cellIndex].Node == current)
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

    static string ExtractText(OdfNode? node)
    {
        if (node == null)
        {
            return "";
        }
        StringBuilder result = new StringBuilder();
        AppendText(result, node);
        return result.ToString();
    }

    static void AppendText(StringBuilder result, OdfNode node)
    {
        if (node.Kind == OdfNodeKind.Text)
        {
            result.Append(node.Value);
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            AppendText(result, node.Children[index]);
            index = index + 1;
        }
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

    static OdfNode? FindDirectOfficeChild(
        OdfNode parent,
        string localName)
    {
        return FindDirectQualifiedChild(
            parent,
            OdfNamespaces.Office,
            localName);
    }

    static OdfNode? FindDirectQualifiedChild(
        OdfNode parent,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode child = parent.Children[index];
            if (child.Kind == OdfNodeKind.Element
                && child.Name != null
                && child.Name.NamespaceName == namespaceName
                && child.Name.LocalName == localName)
            {
                return child;
            }
            index = index + 1;
        }
        return null;
    }

    static string ChangeKind(OdfNode node)
    {
        if (node.Name == null)
        {
            return "";
        }
        if (node.Name.LocalName == "cell-content-change")
        {
            return "cell-content";
        }
        return node.Name.LocalName;
    }

    static string ChangeLocalName(string kind)
    {
        if (kind == "cell-content")
        {
            return "cell-content-change";
        }
        if (kind == "insertion"
            || kind == "deletion"
            || kind == "movement")
        {
            return kind;
        }
        return "";
    }

    static bool IsTrackedChangeNode(OdfNode node)
    {
        if (node.Kind != OdfNodeKind.Element
            || node.Name == null
            || node.Name.NamespaceName != OdfNamespaces.Table)
        {
            return false;
        }
        string localName = node.Name.LocalName;
        return localName == "cell-content-change"
            || localName == "insertion"
            || localName == "deletion"
            || localName == "movement";
    }

    static bool IsCellElement(OdfNode node)
    {
        return IsTableElement(node, "table-cell")
            || IsTableElement(node, "covered-table-cell");
    }

    static string ReadTableAttribute(OdfNode node, string localName)
    {
        return StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Table,
            localName);
    }

    static string ReadOfficeAttribute(OdfNode node, string localName)
    {
        return StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Office,
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
        OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(
            ReadTableAttribute(node, localName));
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

    static void WriteOfficeOptional(
        OdfNode node,
        string localName,
        string value)
    {
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                OdfNamespaces.Office,
                localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                node,
                OdfNamespaces.Office,
                localName,
                OdfNamespaces.OfficePrefix,
                value);
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
        return CreateQualifiedElement(
            OdfNamespaces.Table,
            localName,
            OdfNamespaces.TablePrefix,
            partPath);
    }

    static OdfNode CreateQualifiedTextElement(
        string namespaceName,
        string localName,
        string prefix,
        string value,
        string partPath)
    {
        OdfNode result = CreateQualifiedElement(
            namespaceName,
            localName,
            prefix,
            partPath);
        OdfNode text = new OdfNode();
        text.Kind = OdfNodeKind.Text;
        text.Value = value;
        text.Location.PartPath = partPath;
        text.Parent = result;
        result.Children = [text];
        result.WasEmptyElement = false;
        return result;
    }

    static OdfNode CreateQualifiedElement(
        string namespaceName,
        string localName,
        string prefix,
        string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(
            namespaceName,
            localName,
            prefix);
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
