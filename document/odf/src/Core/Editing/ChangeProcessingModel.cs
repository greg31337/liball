using System;
using System.Collections.Generic;
using System.Text;

namespace odf;

internal class ChangeProjectionState
{
    internal bool ShowInsertions;
    internal bool ShowDeletions;
    internal List<string> HiddenInsertions = new List<string>();
    internal OdfChange[] Changes = [];
}

internal static class ChangeProcessingModel
{
    internal static bool AcceptTextChange(
        OdfDocument document,
        OdfChange change)
    {
        RequireTextChange(document, change);
        bool result = ApplyTextChange(change, true);
        if (result)
        {
            RefreshTextChanges(document);
        }
        return result;
    }

    internal static bool RejectTextChange(
        OdfDocument document,
        OdfChange change)
    {
        RequireTextChange(document, change);
        bool result = ApplyTextChange(change, false);
        if (result)
        {
            RefreshTextChanges(document);
        }
        return result;
    }

    internal static int AcceptAllTextChanges(OdfDocument document)
    {
        RequireTextDocument(document);
        return ApplyTextChangeSet(document, document.Changes, true);
    }

    internal static int RejectAllTextChanges(OdfDocument document)
    {
        RequireTextDocument(document);
        return ApplyTextChangeSet(document, document.Changes, false);
    }

    internal static int AcceptSelectedTextChanges(
        OdfDocument document,
        OdfChange[] changes)
    {
        RequireTextSelection(document, changes);
        return ApplyTextChangeSet(document, changes, true);
    }

    internal static int RejectSelectedTextChanges(
        OdfDocument document,
        OdfChange[] changes)
    {
        RequireTextSelection(document, changes);
        return ApplyTextChangeSet(document, changes, false);
    }

    internal static string ProjectVisibleText(
        OdfDocument document,
        bool showInsertions,
        bool showDeletions)
    {
        RequireTextDocument(document);
        ChangeProjectionState state = new ChangeProjectionState();
        state.ShowInsertions = showInsertions;
        state.ShowDeletions = showDeletions;
        state.Changes = document.Changes;
        StringBuilder result = new StringBuilder();
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfNode? body = document.Parts[partIndex].Body;
            if (body != null)
            {
                AppendProjectedText(result, body, state);
            }
            partIndex = partIndex + 1;
        }
        return TrimProjectedNewlines(result.ToString());
    }

    internal static OdfChange[] FilterTextChanges(
        OdfDocument document,
        string creator,
        string date,
        string comment,
        bool matchCase)
    {
        RequireTextDocument(document);
        List<OdfChange> result = new List<OdfChange>();
        int index = 0;
        while (index < document.Changes.Length)
        {
            OdfChange change = document.Changes[index];
            if (MatchesChangeFields(
                    change.Creator,
                    change.Date,
                    change.Comment,
                    creator,
                    date,
                    comment,
                    matchCase))
            {
                result.Add(change);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal static OdfSpreadsheetChange[] FilterSpreadsheetChanges(
        OdfDocument document,
        string creator,
        string date,
        string comment,
        bool matchCase)
    {
        if (document == null || document.SpreadsheetChanges == null)
        {
            throw new OdfDocumentException(
                "A document and spreadsheet changes are required.");
        }
        List<OdfSpreadsheetChange> result =
            new List<OdfSpreadsheetChange>();
        int index = 0;
        while (index < document.SpreadsheetChanges.Length)
        {
            OdfSpreadsheetChange change = document.SpreadsheetChanges[index];
            if (MatchesChangeFields(
                    change.Creator,
                    change.Date,
                    change.Comment,
                    creator,
                    date,
                    comment,
                    matchCase))
            {
                result.Add(change);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    internal static bool AcceptSpreadsheetChange(
        OdfDocument document,
        OdfSpreadsheetChange change)
    {
        RequireSpreadsheetChange(document, change);
        change.AcceptanceState = "accepted";
        change.RejectingChangeId = "";
        return true;
    }

    internal static bool RejectSpreadsheetChange(
        OdfDocument document,
        OdfSpreadsheetChange change,
        string rejectingChangeId)
    {
        RequireSpreadsheetChange(document, change);
        change.AcceptanceState = "rejected";
        change.RejectingChangeId = rejectingChangeId;
        return true;
    }

    internal static int AcceptAllSpreadsheetChanges(OdfDocument document)
    {
        RequireSpreadsheetDocument(document);
        return SetSpreadsheetChangeStates(
            document.SpreadsheetChanges,
            true,
            "");
    }

    internal static int RejectAllSpreadsheetChanges(
        OdfDocument document,
        string rejectingChangeId)
    {
        RequireSpreadsheetDocument(document);
        return SetSpreadsheetChangeStates(
            document.SpreadsheetChanges,
            false,
            rejectingChangeId);
    }

    internal static int AcceptSelectedSpreadsheetChanges(
        OdfDocument document,
        OdfSpreadsheetChange[] changes)
    {
        RequireSpreadsheetSelection(document, changes);
        return SetSpreadsheetChangeStates(changes, true, "");
    }

    internal static int RejectSelectedSpreadsheetChanges(
        OdfDocument document,
        OdfSpreadsheetChange[] changes,
        string rejectingChangeId)
    {
        RequireSpreadsheetSelection(document, changes);
        return SetSpreadsheetChangeStates(
            changes,
            false,
            rejectingChangeId);
    }

    static int SetSpreadsheetChangeStates(
        OdfSpreadsheetChange[] changes,
        bool accept,
        string rejectingChangeId)
    {
        int index = 0;
        while (index < changes.Length)
        {
            changes[index].AcceptanceState = accept ? "accepted" : "rejected";
            changes[index].RejectingChangeId = accept ? "" : rejectingChangeId;
            index = index + 1;
        }
        return changes.Length;
    }

    static int ApplyTextChangeSet(
        OdfDocument document,
        OdfChange[] changes,
        bool accept)
    {
        int result = 0;
        int index = changes.Length - 1;
        while (index >= 0)
        {
            OdfChange change = changes[index];
            if (ContainsTextChange(document.Changes, change)
                && ApplyTextChange(change, accept))
            {
                result = result + 1;
            }
            index = index - 1;
        }
        if (result > 0)
        {
            RefreshTextChanges(document);
        }
        return result;
    }

    static bool ApplyTextChange(OdfChange change, bool accept)
    {
        if (!change.IsBoundaryResolved || change.Node == null)
        {
            return false;
        }
        if (change.Kind == OdfChangeKind.Insertion)
        {
            if (change.StartNode == null || change.EndNode == null)
            {
                return false;
            }
            if (!accept
                && !RemoveContentBetween(change.StartNode, change.EndNode))
            {
                return false;
            }
            RemoveAttachedNode(change.StartNode);
            RemoveAttachedNode(change.EndNode);
        }
        else if (change.Kind == OdfChangeKind.Deletion)
        {
            if (change.PointNode == null || change.OperationNode == null)
            {
                return false;
            }
            if (!accept && !RestoreDeletedContent(change))
            {
                return false;
            }
            RemoveAttachedNode(change.PointNode);
        }
        else if (change.Kind == OdfChangeKind.FormatChange)
        {
            if (change.StartNode == null || change.EndNode == null)
            {
                return false;
            }
            RemoveAttachedNode(change.StartNode);
            RemoveAttachedNode(change.EndNode);
        }
        else
        {
            return false;
        }
        RemoveAttachedNode(change.Node);
        return true;
    }

    static bool RestoreDeletedContent(OdfChange change)
    {
        OdfNode point = change.PointNode ?? new OdfNode();
        OdfNode operation = change.OperationNode ?? new OdfNode();
        OdfNode? parent = point.Parent;
        if (parent == null)
        {
            return false;
        }
        List<OdfNode> content = new List<OdfNode>();
        int index = 0;
        while (index < operation.Children.Length)
        {
            OdfNode child = operation.Children[index];
            if (!IsOfficeChangeInfo(child))
            {
                content.Add(new OdfTreeEditor([]).CloneSubtree(child));
            }
            index = index + 1;
        }
        if (content.Count == 0)
        {
            return true;
        }
        if (IsTextBlock(parent) && ContainsOnlyTextBlocks(content))
        {
            RestoreDeletedBlocks(parent, point, content);
        }
        else
        {
            index = 0;
            while (index < content.Count)
            {
                InsertBeforeBoundary(parent, point, content[index]);
                index = index + 1;
            }
        }
        return true;
    }

    static void RestoreDeletedBlocks(
        OdfNode containingBlock,
        OdfNode point,
        List<OdfNode> blocks)
    {
        int pointIndex = ChildIndex(containingBlock, point);
        OdfNode first = blocks[0];
        if (blocks.Count == 1)
        {
            int childIndex = 0;
            while (childIndex < first.Children.Length)
            {
                InsertNodeAt(
                    containingBlock,
                    first.Children[childIndex],
                    pointIndex + childIndex);
                childIndex = childIndex + 1;
            }
            return;
        }
        OdfNode[] original = containingBlock.Children;
        OdfNode[] prefix = new OdfNode[pointIndex];
        Array.Copy(original, prefix, pointIndex);
        OdfNode[] firstChildren = first.Children;
        OdfNode[] merged = new OdfNode[prefix.Length + firstChildren.Length];
        Array.Copy(prefix, merged, prefix.Length);
        Array.Copy(firstChildren, 0, merged, prefix.Length, firstChildren.Length);
        SetChildren(containingBlock, merged);
        point.Parent = null;
        OdfNode[] suffix = new OdfNode[original.Length - pointIndex - 1];
        Array.Copy(
            original,
            pointIndex + 1,
            suffix,
            0,
            suffix.Length);
        OdfNode? outer = containingBlock.Parent;
        if (outer == null)
        {
            return;
        }
        int outerIndex = ChildIndex(outer, containingBlock) + 1;
        int blockIndex = 1;
        OdfNode last = containingBlock;
        while (blockIndex < blocks.Count)
        {
            OdfNode block = blocks[blockIndex];
            InsertNodeAt(outer, block, outerIndex);
            outerIndex = outerIndex + 1;
            last = block;
            blockIndex = blockIndex + 1;
        }
        AppendChildren(last, suffix);
    }

    static bool RemoveContentBetween(OdfNode start, OdfNode end)
    {
        List<OdfNode> startPath = NodePath(start);
        List<OdfNode> endPath = NodePath(end);
        int common = 0;
        while (common < startPath.Count
            && common < endPath.Count
            && startPath[common] == endPath[common])
        {
            common = common + 1;
        }
        if (common == 0 || common >= startPath.Count
            || common >= endPath.Count)
        {
            return false;
        }
        OdfNode ancestor = startPath[common - 1];
        OdfNode startBranch = startPath[common];
        OdfNode endBranch = endPath[common];
        int startIndex = ChildIndex(ancestor, startBranch);
        int endIndex = ChildIndex(ancestor, endBranch);
        if (startIndex < 0 || endIndex < 0 || startIndex > endIndex)
        {
            return false;
        }
        TrimAfterBoundary(startBranch, start);
        TrimBeforeBoundary(endBranch, end);
        int index = endIndex - 1;
        while (index > startIndex)
        {
            RemoveChildAt(ancestor, index);
            index = index - 1;
        }
        return true;
    }

    static void TrimAfterBoundary(OdfNode node, OdfNode boundary)
    {
        if (node == boundary)
        {
            return;
        }
        OdfNode child = DirectPathChild(node, boundary);
        int index = ChildIndex(node, child);
        TrimAfterBoundary(child, boundary);
        int remove = node.Children.Length - 1;
        while (remove > index)
        {
            RemoveChildAt(node, remove);
            remove = remove - 1;
        }
    }

    static void TrimBeforeBoundary(OdfNode node, OdfNode boundary)
    {
        if (node == boundary)
        {
            return;
        }
        OdfNode child = DirectPathChild(node, boundary);
        int index = ChildIndex(node, child);
        TrimBeforeBoundary(child, boundary);
        int remove = index - 1;
        while (remove >= 0)
        {
            RemoveChildAt(node, remove);
            remove = remove - 1;
        }
    }

    static OdfNode DirectPathChild(OdfNode ancestor, OdfNode descendant)
    {
        OdfNode current = descendant;
        while (current.Parent != null && current.Parent != ancestor)
        {
            current = current.Parent;
        }
        return current;
    }

    static List<OdfNode> NodePath(OdfNode node)
    {
        List<OdfNode> result = new List<OdfNode>();
        OdfNode? current = node;
        while (current != null)
        {
            result.Insert(0, current);
            current = current.Parent;
        }
        return result;
    }

    static void AppendProjectedText(
        StringBuilder result,
        OdfNode node,
        ChangeProjectionState state)
    {
        if (IsTextElement(node, "tracked-changes")
            || IsOfficeElement(node, "annotation"))
        {
            return;
        }
        if (IsTextBoundary(node, "change-start"))
        {
            string id = ReadChangeId(node);
            if (!state.ShowInsertions && IsInsertion(state.Changes, id))
            {
                state.HiddenInsertions.Add(id);
            }
            return;
        }
        if (IsTextBoundary(node, "change-end"))
        {
            RemoveHiddenInsertion(state.HiddenInsertions, ReadChangeId(node));
            return;
        }
        if (IsTextBoundary(node, "change"))
        {
            if (state.ShowDeletions && state.HiddenInsertions.Count == 0)
            {
                OdfChange? change = FindChange(state.Changes, ReadChangeId(node));
                if (change != null && change.Kind == OdfChangeKind.Deletion)
                {
                    result.Append(change.DeletedText);
                }
            }
            return;
        }
        if (node.Kind == OdfNodeKind.Text)
        {
            if (state.HiddenInsertions.Count == 0)
            {
                result.Append(node.Value);
            }
            return;
        }
        bool block = IsTextBlock(node);
        if (block && result.Length > 0
            && result[result.Length - 1] != '\n')
        {
            result.Append('\n');
        }
        if (IsTextElement(node, "s") && state.HiddenInsertions.Count == 0)
        {
            long count = ReadSpaceCount(node);
            result.Append(new string(' ', (int)count));
            return;
        }
        if (IsTextElement(node, "tab") && state.HiddenInsertions.Count == 0)
        {
            result.Append('\t');
            return;
        }
        if (IsTextElement(node, "line-break")
            && state.HiddenInsertions.Count == 0)
        {
            result.Append('\n');
            return;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            AppendProjectedText(result, node.Children[index], state);
            index = index + 1;
        }
    }

    static bool MatchesChangeFields(
        string actualCreator,
        string actualDate,
        string actualComment,
        string creator,
        string date,
        string comment,
        bool matchCase)
    {
        StringComparison comparison = matchCase
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;
        return (creator.Length == 0
                || actualCreator.IndexOf(creator, comparison) >= 0)
            && (date.Length == 0
                || actualDate.IndexOf(date, comparison) >= 0)
            && (comment.Length == 0
                || actualComment.IndexOf(comment, comparison) >= 0);
    }

    static void RefreshTextChanges(OdfDocument document)
    {
        TextDocumentModel.LoadTextBlocks(document);
        TextRangeModel.LoadTextRanges(document);
        AnnotationDocumentModel.LoadAnnotations(document);
        TextChangeDocumentModel.LoadChanges(document);
    }

    static void RequireTextDocument(OdfDocument document)
    {
        if (document == null || document.Changes == null)
        {
            throw new OdfDocumentException(
                "A document and tracked changes are required.");
        }
    }

    static void RequireTextChange(OdfDocument document, OdfChange change)
    {
        RequireTextDocument(document);
        if (change == null || !ContainsTextChange(document.Changes, change))
        {
            throw new OdfEditException(
                "The tracked change does not belong to the document.");
        }
    }

    static void RequireTextSelection(
        OdfDocument document,
        OdfChange[] changes)
    {
        RequireTextDocument(document);
        if (changes == null)
        {
            throw new OdfEditException("A tracked-change selection is required.");
        }
        int index = 0;
        while (index < changes.Length)
        {
            if (changes[index] == null
                || !ContainsTextChange(document.Changes, changes[index]))
            {
                throw new OdfEditException(
                    "Every selected tracked change must belong to the document.");
            }
            index = index + 1;
        }
    }

    static void RequireSpreadsheetChange(
        OdfDocument document,
        OdfSpreadsheetChange change)
    {
        if (document == null || document.SpreadsheetChanges == null
            || change == null
            || !ContainsSpreadsheetChange(document.SpreadsheetChanges, change))
        {
            throw new OdfEditException(
                "The spreadsheet change does not belong to the document.");
        }
    }

    static void RequireSpreadsheetDocument(OdfDocument document)
    {
        if (document == null || document.SpreadsheetChanges == null)
        {
            throw new OdfDocumentException(
                "A document and spreadsheet changes are required.");
        }
    }

    static void RequireSpreadsheetSelection(
        OdfDocument document,
        OdfSpreadsheetChange[] changes)
    {
        RequireSpreadsheetDocument(document);
        if (changes == null)
        {
            throw new OdfEditException(
                "A spreadsheet-change selection is required.");
        }
        int index = 0;
        while (index < changes.Length)
        {
            if (changes[index] == null
                || !ContainsSpreadsheetChange(
                    document.SpreadsheetChanges,
                    changes[index]))
            {
                throw new OdfEditException(
                    "Every selected spreadsheet change must belong to the document.");
            }
            index = index + 1;
        }
    }

    static bool ContainsTextChange(OdfChange[] changes, OdfChange target)
    {
        int index = 0;
        while (index < changes.Length)
        {
            if (changes[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool ContainsSpreadsheetChange(
        OdfSpreadsheetChange[] changes,
        OdfSpreadsheetChange target)
    {
        int index = 0;
        while (index < changes.Length)
        {
            if (changes[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool IsInsertion(OdfChange[] changes, string id)
    {
        OdfChange? change = FindChange(changes, id);
        return change != null && change.Kind == OdfChangeKind.Insertion;
    }

    static OdfChange? FindChange(OdfChange[] changes, string id)
    {
        int index = 0;
        while (index < changes.Length)
        {
            if (changes[index].Id == id)
            {
                return changes[index];
            }
            index = index + 1;
        }
        return null;
    }

    static void RemoveHiddenInsertion(List<string> hidden, string id)
    {
        int index = hidden.Count - 1;
        while (index >= 0)
        {
            if (hidden[index] == id)
            {
                hidden.RemoveAt(index);
                return;
            }
            index = index - 1;
        }
    }

    static string ReadChangeId(OdfNode node)
    {
        return StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Text,
            "change-id");
    }

    static long ReadSpaceCount(OdfNode node)
    {
        string value = StyleDocumentModel.ReadAttribute(
            node, OdfNamespaces.Text, "c");
        OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(value);
        if (!parsed.IsValid || parsed.IntegerValue < 1
            || parsed.IntegerValue > int.MaxValue)
        {
            return 1;
        }
        return parsed.IntegerValue;
    }

    static string TrimProjectedNewlines(string value)
    {
        int start = 0;
        int end = value.Length;
        while (start < end && value[start] == '\n')
        {
            start = start + 1;
        }
        while (end > start && value[end - 1] == '\n')
        {
            end = end - 1;
        }
        return value.Substring(start, end - start);
    }

    static bool ContainsOnlyTextBlocks(List<OdfNode> nodes)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (!IsTextBlock(nodes[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool IsTextBlock(OdfNode node)
    {
        return IsTextElement(node, "p") || IsTextElement(node, "h");
    }

    static bool IsTextBoundary(OdfNode node, string localName)
    {
        return IsTextElement(node, localName);
    }

    static bool IsTextElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Text
            && node.Name.LocalName == localName;
    }

    static bool IsOfficeElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Office
            && node.Name.LocalName == localName;
    }

    static bool IsOfficeChangeInfo(OdfNode node)
    {
        return IsOfficeElement(node, "change-info");
    }

    static void RemoveAttachedNode(OdfNode node)
    {
        if (node.Parent != null)
        {
            StyleDocumentModel.RemoveNodeFromParent(node);
        }
    }

    static void InsertBeforeBoundary(
        OdfNode parent,
        OdfNode boundary,
        OdfNode child)
    {
        InsertNodeAt(parent, child, ChildIndex(parent, boundary));
    }

    static void InsertNodeAt(
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

    static void AppendChildren(OdfNode parent, OdfNode[] appended)
    {
        OdfNode[] children =
            new OdfNode[parent.Children.Length + appended.Length];
        Array.Copy(parent.Children, children, parent.Children.Length);
        Array.Copy(
            appended,
            0,
            children,
            parent.Children.Length,
            appended.Length);
        SetChildren(parent, children);
    }

    static void SetChildren(OdfNode parent, OdfNode[] children)
    {
        parent.Children = children;
        int index = 0;
        while (index < children.Length)
        {
            children[index].Parent = parent;
            children[index].Location.PartPath = parent.Location.PartPath;
            index = index + 1;
        }
        parent.WasEmptyElement = children.Length == 0;
    }

    static void RemoveChildAt(OdfNode parent, int removal)
    {
        OdfNode removed = parent.Children[removal];
        OdfNode[] children = new OdfNode[parent.Children.Length - 1];
        Array.Copy(parent.Children, 0, children, 0, removal);
        Array.Copy(
            parent.Children,
            removal + 1,
            children,
            removal,
            parent.Children.Length - removal - 1);
        parent.Children = children;
        removed.Parent = null;
        parent.WasEmptyElement = children.Length == 0;
    }

    static int ChildIndex(OdfNode parent, OdfNode child)
    {
        int index = 0;
        while (index < parent.Children.Length)
        {
            if (parent.Children[index] == child)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }
}
