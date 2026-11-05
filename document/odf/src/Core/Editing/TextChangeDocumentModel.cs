using System;
using System.Collections.Generic;
using System.Text;

namespace odf;

internal class TextChangeDocumentState
{
    internal OdfNode[] TrackedChangesNodes = [];
    internal OdfChange[] OriginalChanges = [];
    internal bool OriginalTrackChanges;
}

internal class TextChangeBoundary
{
    internal string Id = "";
    internal string PartPath = "";
    internal int Kind;
    internal int Order;
    internal OdfNode? Node;
    internal OdfTextBlock? Block;
}

internal static class TextChangeDocumentModel
{
    internal static void LoadChanges(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for tracked-change materialization.");
        }
        List<OdfChange> changes = new List<OdfChange>();
        List<OdfNode> trackedNodes = new List<OdfNode>();
        List<TextChangeBoundary> boundaries =
            new List<TextChangeBoundary>();
        int order = 0;
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfPart part = document.Parts[partIndex];
            if (part.Body != null)
            {
                CollectChangeDefinitions(
                    document,
                    part.Body,
                    part.Path,
                    changes,
                    trackedNodes);
                order = CollectChangeBoundaries(
                    document,
                    part.Body,
                    part.Path,
                    boundaries,
                    order);
            }
            partIndex = partIndex + 1;
        }
        document.Changes = changes.ToArray();
        document.TrackTextChanges = ReadTrackingSetting(trackedNodes);
        ResolveChangeBoundaries(document, boundaries);
        TextChangeDocumentState state = new TextChangeDocumentState();
        state.TrackedChangesNodes = trackedNodes.ToArray();
        state.OriginalChanges = CopyChanges(document.Changes);
        state.OriginalTrackChanges = document.TrackTextChanges;
        document.TextChangeState = state;
    }

    internal static void SynchronizeChanges(OdfDocument document)
    {
        if (document == null || document.Changes == null)
        {
            throw new OdfDocumentException(
                "A document and tracked-change collection are required for writing.");
        }
        TextChangeDocumentState state = document.TextChangeState
            ?? new TextChangeDocumentState();
        RemoveDeletedChanges(
            document, state.OriginalChanges, document.Changes);
        List<OdfNode> trackedNodes = new List<OdfNode>();
        int trackedIndex = 0;
        while (trackedIndex < state.TrackedChangesNodes.Length)
        {
            OdfNode tracked = state.TrackedChangesNodes[trackedIndex];
            if (tracked.Parent != null)
            {
                trackedNodes.Add(tracked);
            }
            trackedIndex = trackedIndex + 1;
        }
        int index = 0;
        while (index < document.Changes.Length)
        {
            OdfChange change = document.Changes[index];
            if (change == null)
            {
                throw new OdfDocumentException(
                    "The tracked-change collection contains a null change.");
            }
            SynchronizeChange(document, change, trackedNodes);
            index = index + 1;
        }
        SynchronizeTrackingSetting(
            document, state, trackedNodes);
        RefreshChangeBoundaries(document);
        SnapshotChanges(document.Changes);
        state.TrackedChangesNodes = trackedNodes.ToArray();
        state.OriginalChanges = CopyChanges(document.Changes);
        state.OriginalTrackChanges = document.TrackTextChanges;
        document.TextChangeState = state;
    }

    static void CollectChangeDefinitions(
        OdfDocument document,
        OdfNode node,
        string partPath,
        List<OdfChange> changes,
        List<OdfNode> trackedNodes)
    {
        if (IsTextElement(node, "tracked-changes"))
        {
            trackedNodes.Add(node);
            int childIndex = 0;
            while (childIndex < node.Children.Length)
            {
                OdfNode child = node.Children[childIndex];
                if (IsTextElement(child, "changed-region"))
                {
                    changes.Add(CreateChange(document, child, partPath));
                }
                childIndex = childIndex + 1;
            }
            return;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            CollectChangeDefinitions(
                document,
                node.Children[index],
                partPath,
                changes,
                trackedNodes);
            index = index + 1;
        }
    }

    static OdfChange CreateChange(
        OdfDocument document,
        OdfNode region,
        string partPath)
    {
        OdfChange result = new OdfChange();
        result.Node = region;
        result.PartPath = partPath;
        result.XmlId = Read(region, OdfNamespaces.Xml, "id");
        if (result.XmlId.Length == 0)
        {
            result.XmlId = region.XmlId;
        }
        result.Id = Read(region, OdfNamespaces.Text, "id");
        if (result.Id.Length == 0)
        {
            result.Id = result.XmlId;
        }
        OdfNode? operation = FindOperation(region);
        result.OperationNode = operation;
        if (operation != null)
        {
            result.OperationName = CopyName(operation.Name);
            result.Kind = ChangeKind(operation);
            result.ChangeInfoNode = FindDirectChild(
                operation, OdfNamespaces.Office, "change-info");
            LoadChangeInfo(result);
            if (result.Kind == OdfChangeKind.Deletion)
            {
                result.DeletedTextBlocks = ContainedDeletionBlocks(
                    document.TextBlocks, operation, result.ChangeInfoNode);
                result.DeletedText = JoinBlockText(
                    result.DeletedTextBlocks);
            }
        }
        SnapshotChange(result);
        return result;
    }

    static OdfNode? FindOperation(OdfNode region)
    {
        int index = 0;
        while (index < region.Children.Length)
        {
            OdfNode child = region.Children[index];
            if (child.Kind == OdfNodeKind.Element)
            {
                return child;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfChangeKind ChangeKind(OdfNode operation)
    {
        if (IsTextElement(operation, "insertion"))
        {
            return OdfChangeKind.Insertion;
        }
        if (IsTextElement(operation, "deletion"))
        {
            return OdfChangeKind.Deletion;
        }
        if (IsTextElement(operation, "format-change"))
        {
            return OdfChangeKind.FormatChange;
        }
        return OdfChangeKind.Unknown;
    }

    static void LoadChangeInfo(OdfChange change)
    {
        OdfNode? info = change.ChangeInfoNode;
        if (info == null)
        {
            return;
        }
        change.Creator = DirectChildText(
            info, OdfNamespaces.DublinCore, "creator");
        change.Date = DirectChildText(
            info, OdfNamespaces.DublinCore, "date");
        change.IsDateValid = IsDate(change.Date);
        StringBuilder comment = new StringBuilder();
        int index = 0;
        while (index < info.Children.Length)
        {
            OdfNode child = info.Children[index];
            if (IsTextElement(child, "p"))
            {
                if (comment.Length > 0)
                {
                    comment.Append('\n');
                }
                AppendElementText(comment, child);
            }
            index = index + 1;
        }
        change.Comment = comment.ToString();
    }

    static int CollectChangeBoundaries(
        OdfDocument document,
        OdfNode node,
        string partPath,
        List<TextChangeBoundary> boundaries,
        int order)
    {
        if (IsTextElement(node, "tracked-changes"))
        {
            return order;
        }
        int kind = BoundaryKind(node);
        if (kind > 0)
        {
            TextChangeBoundary boundary = new TextChangeBoundary();
            boundary.Id = Read(
                node, OdfNamespaces.Text, "change-id");
            boundary.PartPath = partPath;
            boundary.Kind = kind;
            boundary.Order = order;
            boundary.Node = node;
            boundary.Block = FindContainingBlock(
                document.TextBlocks, node);
            boundaries.Add(boundary);
            order = order + 1;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            order = CollectChangeBoundaries(
                document,
                node.Children[index],
                partPath,
                boundaries,
                order);
            index = index + 1;
        }
        return order;
    }

    static int BoundaryKind(OdfNode node)
    {
        if (IsTextElement(node, "change"))
        {
            return 1;
        }
        if (IsTextElement(node, "change-start"))
        {
            return 2;
        }
        if (IsTextElement(node, "change-end"))
        {
            return 3;
        }
        return 0;
    }

    static void ResolveChangeBoundaries(
        OdfDocument document,
        List<TextChangeBoundary> boundaries)
    {
        int index = 0;
        while (index < document.Changes.Length)
        {
            ResolveOneChangeBoundary(
                document.Changes[index], boundaries);
            index = index + 1;
        }
    }

    static void ResolveOneChangeBoundary(
        OdfChange change,
        List<TextChangeBoundary> boundaries)
    {
        change.PointNode = null;
        change.StartNode = null;
        change.EndNode = null;
        change.StartBlock = null;
        change.EndBlock = null;
        change.ChangedText = "";
        change.IsPoint = false;
        change.IsRange = false;
        change.IsBoundaryResolved = false;
        change.IsReversed = false;
        change.HasDuplicateBoundary = false;
        if (change.Id.Length == 0)
        {
            return;
        }
        int pointCount = 0;
        int startCount = 0;
        int endCount = 0;
        TextChangeBoundary? point = null;
        TextChangeBoundary? start = null;
        TextChangeBoundary? end = null;
        int index = 0;
        while (index < boundaries.Count)
        {
            TextChangeBoundary boundary = boundaries[index];
            if (boundary.Id == change.Id
                && boundary.PartPath == change.PartPath)
            {
                if (boundary.Kind == 1)
                {
                    pointCount = pointCount + 1;
                    if (point == null)
                    {
                        point = boundary;
                    }
                }
                else if (boundary.Kind == 2)
                {
                    startCount = startCount + 1;
                    if (start == null)
                    {
                        start = boundary;
                    }
                }
                else
                {
                    endCount = endCount + 1;
                    if (end == null)
                    {
                        end = boundary;
                    }
                }
            }
            index = index + 1;
        }
        change.IsPoint = pointCount > 0;
        change.IsRange = startCount > 0 || endCount > 0;
        change.HasDuplicateBoundary = pointCount > 1
            || startCount > 1 || endCount > 1
            || pointCount > 0 && (startCount > 0 || endCount > 0);
        if (point != null)
        {
            change.PointNode = point.Node;
            change.StartNode = point.Node;
            change.StartBlock = point.Block;
        }
        if (start != null)
        {
            change.StartNode = start.Node;
            change.StartBlock = start.Block;
        }
        if (end != null)
        {
            change.EndNode = end.Node;
            change.EndBlock = end.Block;
        }
        if (!change.HasDuplicateBoundary && pointCount == 1
            && startCount == 0 && endCount == 0)
        {
            change.IsBoundaryResolved = true;
        }
        else if (!change.HasDuplicateBoundary && startCount == 1
            && endCount == 1 && start != null && end != null)
        {
            change.IsReversed = end.Order < start.Order;
            change.IsBoundaryResolved = !change.IsReversed;
            if (change.IsBoundaryResolved
                && start.Node != null && end.Node != null)
            {
                change.ChangedText = ExtractChangedText(
                    RootNode(start.Node), start.Node, end.Node);
            }
        }
    }

    static void SynchronizeChange(
        OdfDocument document,
        OdfChange change,
        List<OdfNode> trackedNodes)
    {
        RequireChangeFields(change);
        bool created = change.Node == null;
        if (created && change.Id.Length == 0)
        {
            throw new OdfEditException(
                "A tracked change requires a non-empty identifier.");
        }
        OdfNode region;
        if (created)
        {
            OdfNode tracked = SelectTrackedChangesNode(
                document, change.PartPath, trackedNodes);
            region = CreateTextElement(
                "changed-region", tracked.Location.PartPath);
            StyleDocumentModel.AppendNode(tracked, region);
            change.Node = region;
            change.PartPath = PartPathForNode(document, tracked);
            if (change.XmlId.Length == 0)
            {
                change.XmlId = change.Id;
            }
        }
        else
        {
            region = change.Node ?? new OdfNode();
        }
        SynchronizeIdentity(document, change, region, created);
        if (!created && change.Kind == OdfChangeKind.Unknown
            && change.OperationNode == null)
        {
            SnapshotChange(change);
            return;
        }
        OdfNode operation = EnsureOperation(change, region);
        change.OperationNode = operation;
        change.OperationName = CopyName(operation.Name);
        if (!created && change.Kind == OdfChangeKind.Unknown
            && change.ChangeInfoNode == null
            && change.Creator.Length == 0 && change.Date.Length == 0
            && change.Comment.Length == 0)
        {
            SnapshotChange(change);
            return;
        }
        OdfNode info = EnsureChangeInfo(change, operation);
        SynchronizeChangeInfo(change, info, created);
        if (change.Kind == OdfChangeKind.Deletion)
        {
            SynchronizeDeletionText(
                document, change, operation, info, created);
        }
        else
        {
            change.DeletedText = "";
            change.DeletedTextBlocks = [];
        }
        SnapshotChange(change);
    }

    static void SynchronizeIdentity(
        OdfDocument document,
        OdfChange change,
        OdfNode region,
        bool created)
    {
        if (!created && change.Id != change.OriginalId)
        {
            RenameBoundaryReferences(
                document, change.PartPath, change.OriginalId, change.Id);
        }
        string originalTextId = Read(
            region, OdfNamespaces.Text, "id");
        if (!created && change.Id != change.OriginalId
            && change.OriginalId == change.OriginalXmlId
            && change.XmlId == change.OriginalXmlId)
        {
            change.XmlId = change.Id;
        }
        if (created || change.XmlId.Length > 0
            || change.OriginalXmlId.Length > 0)
        {
            WriteOptional(region, OdfNamespaces.Xml, "id",
                OdfNamespaces.XmlPrefix, change.XmlId);
            region.XmlId = change.XmlId;
        }
        if (originalTextId.Length > 0 || change.Id != change.XmlId)
        {
            WriteOptional(region, OdfNamespaces.Text, "id",
                OdfNamespaces.TextPrefix, change.Id);
        }
        else
        {
            StyleDocumentModel.RemoveAttribute(
                region, OdfNamespaces.Text, "id");
        }
    }

    static OdfNode EnsureOperation(OdfChange change, OdfNode region)
    {
        OdfNode? operation = change.OperationNode;
        bool compatible = operation != null
            && (change.Kind == OdfChangeKind.Unknown
                || ChangeKind(operation) == change.Kind);
        if (compatible && operation != null)
        {
            return operation;
        }
        OdfNode replacement = CreateOperationElement(
            change, region.Location.PartPath);
        if (operation == null)
        {
            StyleDocumentModel.AppendNode(region, replacement);
            return replacement;
        }
        OdfNode? info = FindDirectChild(
            operation, OdfNamespaces.Office, "change-info");
        if (info != null)
        {
            StyleDocumentModel.RemoveNodeFromParent(info);
            StyleDocumentModel.AppendNode(replacement, info);
        }
        ReplaceChildNode(region, operation, replacement);
        return replacement;
    }

    static OdfNode CreateOperationElement(
        OdfChange change,
        string partPath)
    {
        if (change.Kind == OdfChangeKind.Insertion)
        {
            return CreateTextElement("insertion", partPath);
        }
        if (change.Kind == OdfChangeKind.Deletion)
        {
            return CreateTextElement("deletion", partPath);
        }
        if (change.Kind == OdfChangeKind.FormatChange)
        {
            return CreateTextElement("format-change", partPath);
        }
        OdfQualifiedName? name = change.OperationName;
        if (name == null || name.LocalName.Length == 0)
        {
            throw new OdfEditException(
                "A new unknown change requires an operation name.");
        }
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = CopyName(name);
        result.Location.PartPath = partPath;
        return result;
    }

    static OdfNode EnsureChangeInfo(
        OdfChange change,
        OdfNode operation)
    {
        OdfNode? info = FindDirectChild(
            operation, OdfNamespaces.Office, "change-info");
        if (info == null)
        {
            info = CreateOfficeElement(
                "change-info", operation.Location.PartPath);
            InsertChildNode(operation, 0, info);
        }
        change.ChangeInfoNode = info;
        return info;
    }

    static void SynchronizeChangeInfo(
        OdfChange change,
        OdfNode info,
        bool created)
    {
        OdfNode creator = EnsureQualifiedChild(
            info,
            OdfNamespaces.DublinCore,
            "creator",
            OdfNamespaces.DublinCorePrefix,
            0);
        OdfNode date = EnsureQualifiedChild(
            info,
            OdfNamespaces.DublinCore,
            "date",
            OdfNamespaces.DublinCorePrefix,
            1);
        if (created || change.Creator != change.OriginalCreator)
        {
            ReplaceElementText(creator, change.Creator);
        }
        if (created || change.Date != change.OriginalDate)
        {
            ReplaceElementText(date, change.Date);
        }
        if (created || change.Comment != change.OriginalComment)
        {
            ReplaceCommentParagraphs(info, change.Comment);
        }
        change.IsDateValid = IsDate(change.Date);
    }

    static void SynchronizeDeletionText(
        OdfDocument document,
        OdfChange change,
        OdfNode operation,
        OdfNode info,
        bool created)
    {
        if (!created && change.DeletedText == change.OriginalDeletedText)
        {
            return;
        }
        RemoveDeletionContent(operation, info);
        if (change.DeletedText.Length == 0)
        {
            change.DeletedTextBlocks = [];
            return;
        }
        OdfNode paragraph = CreateTextElement(
            "p", operation.Location.PartPath);
        ReplaceElementText(paragraph, change.DeletedText);
        StyleDocumentModel.AppendNode(operation, paragraph);
        OdfTextBlock block = TextDocumentModel.CreateTextBlock(
            paragraph,
            operation.Location.PartPath,
            document.LoadOptions.SecurityLimits.MaximumXmlTextLength);
        change.DeletedTextBlocks = [block];
    }

    static void RemoveDeletionContent(OdfNode operation, OdfNode info)
    {
        List<OdfNode> retained = new List<OdfNode>();
        int index = 0;
        while (index < operation.Children.Length)
        {
            OdfNode child = operation.Children[index];
            if (child == info)
            {
                retained.Add(child);
            }
            else
            {
                child.Parent = null;
            }
            index = index + 1;
        }
        operation.Children = retained.ToArray();
    }

    static void ReplaceCommentParagraphs(OdfNode info, string comment)
    {
        List<OdfNode> retained = new List<OdfNode>();
        int index = 0;
        while (index < info.Children.Length)
        {
            OdfNode child = info.Children[index];
            if (IsTextElement(child, "p"))
            {
                child.Parent = null;
            }
            else
            {
                retained.Add(child);
            }
            index = index + 1;
        }
        info.Children = retained.ToArray();
        if (comment.Length > 0)
        {
            OdfNode paragraph = CreateTextElement(
                "p", info.Location.PartPath);
            ReplaceElementText(paragraph, comment);
            StyleDocumentModel.AppendNode(info, paragraph);
        }
    }

    static OdfNode EnsureQualifiedChild(
        OdfNode parent,
        string namespaceName,
        string localName,
        string prefix,
        int insertion)
    {
        OdfNode? child = FindDirectChild(
            parent, namespaceName, localName);
        if (child != null)
        {
            return child;
        }
        child = CreateElement(
            namespaceName, localName, prefix, parent.Location.PartPath);
        if (insertion > parent.Children.Length)
        {
            insertion = parent.Children.Length;
        }
        InsertChildNode(parent, insertion, child);
        return child;
    }

    static void RemoveDeletedChanges(
        OdfDocument document,
        OdfChange[] original,
        OdfChange[] current)
    {
        int index = 0;
        while (index < original.Length)
        {
            OdfChange change = original[index];
            if (!ContainsChange(current, change))
            {
                if (change.Node != null)
                {
                    StyleDocumentModel.RemoveNodeFromParent(change.Node);
                }
                if (!ContainsChangeId(current, change.Id, change.PartPath))
                {
                    RemoveBoundaryReferences(
                        document, change.PartPath, change.Id);
                }
            }
            index = index + 1;
        }
    }

    static void SynchronizeTrackingSetting(
        OdfDocument document,
        TextChangeDocumentState state,
        List<OdfNode> trackedNodes)
    {
        if (trackedNodes.Count == 0 && document.TrackTextChanges)
        {
            SelectTrackedChangesNode(document, "", trackedNodes);
        }
        if (document.TrackTextChanges == state.OriginalTrackChanges)
        {
            return;
        }
        int index = 0;
        while (index < trackedNodes.Count)
        {
            StyleDocumentModel.WriteAttribute(
                trackedNodes[index],
                OdfNamespaces.Text,
                "track-changes",
                OdfNamespaces.TextPrefix,
                document.TrackTextChanges ? "true" : "false");
            index = index + 1;
        }
    }

    static OdfNode SelectTrackedChangesNode(
        OdfDocument document,
        string requestedPart,
        List<OdfNode> trackedNodes)
    {
        int index = 0;
        while (index < trackedNodes.Count)
        {
            OdfNode candidate = trackedNodes[index];
            if (requestedPart.Length == 0
                || candidate.Location.PartPath == requestedPart)
            {
                return candidate;
            }
            index = index + 1;
        }
        OdfNode textRoot = FindTextDocumentRoot(document, requestedPart);
        OdfNode tracked = CreateTextElement(
            "tracked-changes", textRoot.Location.PartPath);
        InsertChildNode(textRoot, 0, tracked);
        trackedNodes.Add(tracked);
        return tracked;
    }

    static OdfNode FindTextDocumentRoot(
        OdfDocument document,
        string requestedPart)
    {
        int index = 0;
        while (index < document.Parts.Length)
        {
            OdfPart part = document.Parts[index];
            if (part.Body != null
                && (requestedPart.Length == 0
                    || part.Path == requestedPart))
            {
                OdfNode? text = FindDescendantTextRoot(part.Body);
                if (text != null)
                {
                    return text;
                }
            }
            index = index + 1;
        }
        throw new OdfEditException(
            "A tracked change requires an office:text document body.");
    }

    static string PartPathForNode(OdfDocument document, OdfNode node)
    {
        int index = 0;
        while (index < document.Parts.Length)
        {
            OdfPart part = document.Parts[index];
            if (part.Body != null
                && IsDescendantOrSelf(node, part.Body))
            {
                return part.Path;
            }
            index = index + 1;
        }
        return node.Location.PartPath;
    }

    static OdfNode? FindDescendantTextRoot(OdfNode node)
    {
        if (IsOfficeElement(node, "text"))
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode? result = FindDescendantTextRoot(node.Children[index]);
            if (result != null)
            {
                return result;
            }
            index = index + 1;
        }
        return null;
    }

    static void RefreshChangeBoundaries(OdfDocument document)
    {
        List<TextChangeBoundary> boundaries =
            new List<TextChangeBoundary>();
        int order = 0;
        int partIndex = 0;
        while (partIndex < document.Parts.Length)
        {
            OdfPart part = document.Parts[partIndex];
            if (part.Body != null)
            {
                order = CollectChangeBoundaries(
                    document,
                    part.Body,
                    part.Path,
                    boundaries,
                    order);
            }
            partIndex = partIndex + 1;
        }
        ResolveChangeBoundaries(document, boundaries);
    }

    static void RenameBoundaryReferences(
        OdfDocument document,
        string partPath,
        string oldId,
        string newId)
    {
        if (oldId.Length == 0 || oldId == newId)
        {
            return;
        }
        int index = 0;
        while (index < document.Parts.Length)
        {
            OdfPart part = document.Parts[index];
            if (part.Body != null && part.Path == partPath)
            {
                RewriteBoundaryReferences(
                    part.Body, oldId, newId, false);
            }
            index = index + 1;
        }
    }

    static void RewriteBoundaryReferences(
        OdfNode node,
        string oldId,
        string newId,
        bool remove)
    {
        if (IsTextElement(node, "tracked-changes"))
        {
            return;
        }
        int kind = BoundaryKind(node);
        if (kind > 0
            && Read(node, OdfNamespaces.Text, "change-id") == oldId)
        {
            if (remove)
            {
                StyleDocumentModel.RemoveNodeFromParent(node);
                return;
            }
            StyleDocumentModel.WriteAttribute(
                node,
                OdfNamespaces.Text,
                "change-id",
                OdfNamespaces.TextPrefix,
                newId);
        }
        OdfNode[] children = CopyNodes(node.Children);
        int index = 0;
        while (index < children.Length)
        {
            RewriteBoundaryReferences(
                children[index], oldId, newId, remove);
            index = index + 1;
        }
    }

    static void RemoveBoundaryReferences(
        OdfDocument document,
        string partPath,
        string id)
    {
        int index = 0;
        while (index < document.Parts.Length)
        {
            OdfPart part = document.Parts[index];
            if (part.Body != null && part.Path == partPath)
            {
                RewriteBoundaryReferences(part.Body, id, "", true);
            }
            index = index + 1;
        }
    }

    static bool ReadTrackingSetting(List<OdfNode> trackedNodes)
    {
        int index = 0;
        while (index < trackedNodes.Count)
        {
            string value = Read(
                trackedNodes[index],
                OdfNamespaces.Text,
                "track-changes");
            if (value == "true" || value == "1")
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static OdfTextBlock[] ContainedDeletionBlocks(
        OdfTextBlock[] blocks,
        OdfNode operation,
        OdfNode? info)
    {
        List<OdfTextBlock> result = new List<OdfTextBlock>();
        int index = 0;
        while (index < blocks.Length)
        {
            OdfTextBlock block = blocks[index];
            if (block.Node != null
                && IsDescendantOrSelf(block.Node, operation)
                && (info == null
                    || !IsDescendantOrSelf(block.Node, info)))
            {
                result.Add(block);
            }
            index = index + 1;
        }
        return result.ToArray();
    }

    static string JoinBlockText(OdfTextBlock[] blocks)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < blocks.Length)
        {
            if (index > 0)
            {
                result.Append('\n');
            }
            result.Append(blocks[index].Text);
            index = index + 1;
        }
        return result.ToString();
    }

    static string ExtractChangedText(
        OdfNode root,
        OdfNode start,
        OdfNode end)
    {
        List<OdfNode> nodes = new List<OdfNode>();
        CollectNodes(root, nodes);
        int startIndex = NodeListIndex(nodes, start);
        int endIndex = NodeListIndex(nodes, end);
        if (startIndex < 0 || endIndex <= startIndex)
        {
            return "";
        }
        StringBuilder result = new StringBuilder();
        int index = startIndex + 1;
        while (index < endIndex)
        {
            OdfNode node = nodes[index];
            if (!IsExcludedChangeText(node))
            {
                AppendChangedNode(result, node);
            }
            index = index + 1;
        }
        while (result.Length > 0
            && result[result.Length - 1] == '\n')
        {
            result.Length = result.Length - 1;
        }
        return result.ToString();
    }

    static void CollectNodes(OdfNode node, List<OdfNode> nodes)
    {
        nodes.Add(node);
        int index = 0;
        while (index < node.Children.Length)
        {
            CollectNodes(node.Children[index], nodes);
            index = index + 1;
        }
    }

    static int NodeListIndex(List<OdfNode> nodes, OdfNode target)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (nodes[index] == target)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static bool IsExcludedChangeText(OdfNode node)
    {
        OdfNode? current = node;
        while (current != null)
        {
            if (IsTextElement(current, "tracked-changes")
                || IsOfficeElement(current, "annotation"))
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }

    static void AppendChangedNode(StringBuilder result, OdfNode node)
    {
        if (node.Kind == OdfNodeKind.Text)
        {
            result.Append(node.Value);
            return;
        }
        if (node.Kind != OdfNodeKind.Element || node.Name == null
            || node.Name.NamespaceName != OdfNamespaces.Text)
        {
            return;
        }
        if ((node.Name.LocalName == "p" || node.Name.LocalName == "h")
            && result.Length > 0 && result[result.Length - 1] != '\n')
        {
            result.Append('\n');
        }
        else if (node.Name.LocalName == "tab")
        {
            result.Append('\t');
        }
        else if (node.Name.LocalName == "line-break")
        {
            result.Append('\n');
        }
        else if (node.Name.LocalName == "s")
        {
            AppendChangeSpaces(result, node);
        }
    }

    static void AppendChangeSpaces(StringBuilder result, OdfNode node)
    {
        string lexical = Read(node, OdfNamespaces.Text, "c");
        int count = 1;
        OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(lexical);
        if (lexical.Length > 0 && parsed.IsValid
            && parsed.IntegerValue > 0
            && parsed.IntegerValue <= int.MaxValue)
        {
            count = (int)parsed.IntegerValue;
        }
        result.Append(new string(' ', count));
    }

    static OdfNode RootNode(OdfNode node)
    {
        OdfNode result = node;
        while (result.Parent != null)
        {
            result = result.Parent;
        }
        return result;
    }

    static OdfTextBlock? FindContainingBlock(
        OdfTextBlock[] blocks,
        OdfNode node)
    {
        int index = 0;
        while (index < blocks.Length)
        {
            OdfNode? blockNode = blocks[index].Node;
            if (blockNode != null
                && IsDescendantOrSelf(node, blockNode))
            {
                return blocks[index];
            }
            index = index + 1;
        }
        return null;
    }

    static bool IsDescendantOrSelf(OdfNode node, OdfNode ancestor)
    {
        OdfNode? current = node;
        while (current != null)
        {
            if (current == ancestor)
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }

    static string DirectChildText(
        OdfNode parent,
        string namespaceName,
        string localName)
    {
        OdfNode? child = FindDirectChild(
            parent, namespaceName, localName);
        if (child == null)
        {
            return "";
        }
        StringBuilder result = new StringBuilder();
        AppendElementText(result, child);
        return result.ToString();
    }

    static void AppendElementText(StringBuilder result, OdfNode node)
    {
        if (node.Kind == OdfNodeKind.Text)
        {
            result.Append(node.Value);
            return;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            AppendElementText(result, node.Children[index]);
            index = index + 1;
        }
    }

    static void ReplaceElementText(OdfNode node, string value)
    {
        int index = 0;
        while (index < node.Children.Length)
        {
            node.Children[index].Parent = null;
            index = index + 1;
        }
        if (value.Length == 0)
        {
            node.Children = [];
            return;
        }
        OdfNode text = new OdfNode();
        text.Kind = OdfNodeKind.Text;
        text.Value = value;
        text.Parent = node;
        text.Location.PartPath = node.Location.PartPath;
        node.Children = [text];
        node.WasEmptyElement = false;
    }

    static OdfNode? FindDirectChild(
        OdfNode parent,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < parent.Children.Length)
        {
            OdfNode child = parent.Children[index];
            if (child.Kind == OdfNodeKind.Element && child.Name != null
                && child.Name.NamespaceName == namespaceName
                && child.Name.LocalName == localName)
            {
                return child;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode CreateTextElement(string localName, string partPath)
    {
        return CreateElement(
            OdfNamespaces.Text,
            localName,
            OdfNamespaces.TextPrefix,
            partPath);
    }

    static OdfNode CreateOfficeElement(string localName, string partPath)
    {
        return CreateElement(
            OdfNamespaces.Office,
            localName,
            OdfNamespaces.OfficePrefix,
            partPath);
    }

    static OdfNode CreateElement(
        string namespaceName,
        string localName,
        string prefix,
        string partPath)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = StyleDocumentModel.QualifiedName(
            namespaceName, localName, prefix);
        result.Location.PartPath = partPath;
        return result;
    }

    static void InsertChildNode(
        OdfNode parent,
        int index,
        OdfNode child)
    {
        OdfNode[] children = new OdfNode[parent.Children.Length + 1];
        Array.Copy(parent.Children, 0, children, 0, index);
        children[index] = child;
        Array.Copy(parent.Children, index, children, index + 1,
            parent.Children.Length - index);
        parent.Children = children;
        child.Parent = parent;
        child.Location.PartPath = parent.Location.PartPath;
        parent.WasEmptyElement = false;
    }

    static void ReplaceChildNode(
        OdfNode parent,
        OdfNode oldChild,
        OdfNode newChild)
    {
        int index = 0;
        while (index < parent.Children.Length
            && parent.Children[index] != oldChild)
        {
            index = index + 1;
        }
        if (index == parent.Children.Length)
        {
            throw new OdfEditException(
                "The tracked-change operation is detached from its region.");
        }
        parent.Children[index] = newChild;
        oldChild.Parent = null;
        newChild.Parent = parent;
        newChild.Location.PartPath = parent.Location.PartPath;
    }

    static string Read(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return StyleDocumentModel.ReadAttribute(
            node, namespaceName, localName);
    }

    static void WriteOptional(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(
                node, namespaceName, localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                node, namespaceName, localName, prefix, value);
        }
    }

    static bool IsTextElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Text
            && node.Name.LocalName == localName;
    }

    static bool IsOfficeElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Office
            && node.Name.LocalName == localName;
    }

    static OdfQualifiedName? CopyName(OdfQualifiedName? source)
    {
        if (source == null)
        {
            return null;
        }
        OdfQualifiedName result = new OdfQualifiedName();
        result.NamespaceName = source.NamespaceName;
        result.LocalName = source.LocalName;
        result.Prefix = source.Prefix;
        return result;
    }

    static OdfNode[] CopyNodes(OdfNode[] values)
    {
        OdfNode[] result = new OdfNode[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }

    static OdfChange[] CopyChanges(OdfChange[] values)
    {
        OdfChange[] result = new OdfChange[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }

    static bool ContainsChange(OdfChange[] values, OdfChange target)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool ContainsChangeId(
        OdfChange[] values,
        string id,
        string partPath)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Id == id
                && values[index].PartPath == partPath)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void SnapshotChanges(OdfChange[] changes)
    {
        int index = 0;
        while (index < changes.Length)
        {
            SnapshotChange(changes[index]);
            index = index + 1;
        }
    }

    static void SnapshotChange(OdfChange change)
    {
        change.OriginalId = change.Id;
        change.OriginalXmlId = change.XmlId;
        change.OriginalCreator = change.Creator;
        change.OriginalDate = change.Date;
        change.OriginalComment = change.Comment;
        change.OriginalDeletedText = change.DeletedText;
    }

    static bool IsDate(string value)
    {
        return value.Length > 0
            && (OdfLexicalParser.ParseDate(value).IsValid
                || OdfLexicalParser.ParseDateTime(value).IsValid);
    }

    static void RequireChangeFields(OdfChange change)
    {
        if (change.Id == null || change.XmlId == null
            || change.Creator == null || change.Date == null
            || change.Comment == null || change.DeletedText == null
            || change.ChangedText == null || change.PartPath == null
            || change.DeletedTextBlocks == null)
        {
            throw new OdfEditException(
                "Tracked-change fields and collections cannot be null.");
        }
        if (change.Kind != OdfChangeKind.Unknown
            && change.Kind != OdfChangeKind.Insertion
            && change.Kind != OdfChangeKind.Deletion
            && change.Kind != OdfChangeKind.FormatChange)
        {
            throw new OdfEditException(
                "The tracked-change kind is not recognized.");
        }
    }
}
