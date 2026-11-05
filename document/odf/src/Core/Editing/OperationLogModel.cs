using System;
using System.Collections.Generic;

namespace odf;

internal static class OperationLogModel
{
    internal static OdfOperation CreateInsertNodeOperation(
        OdfDocument document,
        OdfNode parent,
        int childIndex,
        OdfNode payload,
        long sequence)
    {
        if (parent == null || payload == null
            || payload.Kind != OdfNodeKind.Element
            || childIndex < 0 || childIndex > parent.Children.Length)
        {
            throw new OdfEditException(
                "An insertion requires an element payload and a valid child index.");
        }
        OdfOperation result = NewOperation(
            OdfOperationKind.InsertNode,
            StableNodeIdModel.FindNodePartPath(document, parent),
            sequence);
        result.ParentNodeId = StableNodeIdModel.EnsureStableNodeId(
            document, parent);
        result.TargetNodeId = NewDetachedNodeId(document);
        if (!PayloadStableIdsAvailable(
                document, payload, result.TargetNodeId))
        {
            throw new OdfEditException(
                "An inserted payload contains a duplicate stable node identifier.");
        }
        result.ChildIndex = childIndex;
        result.Payload = CloneNode(payload);
        OdfEditPrecondition parentCondition = NodeCondition(
            OdfEditPreconditionKind.NodeExists,
            result.ParentNodeId);
        OdfEditPrecondition targetCondition = NodeCondition(
            OdfEditPreconditionKind.NodeAbsent,
            result.TargetNodeId);
        result.Preconditions = [parentCondition, targetCondition];
        return result;
    }

    internal static OdfOperation CreateRemoveNodeOperation(
        OdfDocument document,
        OdfNode target,
        long sequence)
    {
        if (target == null)
        {
            throw new OdfEditException(
                "A removable operation target is required.");
        }
        OdfNode? parent = target.Parent;
        if (parent == null)
        {
            throw new OdfEditException(
                "A removable operation target must have a parent element.");
        }
        OdfOperation result = NewOperation(
            OdfOperationKind.RemoveNode,
            StableNodeIdModel.FindNodePartPath(document, target),
            sequence);
        result.TargetNodeId = StableNodeIdModel.EnsureStableNodeId(
            document, target);
        result.ParentNodeId = StableNodeIdModel.EnsureStableNodeId(
            document, parent);
        result.ChildIndex = FindChildIndex(parent, target);
        result.Payload = CloneNode(target);
        result.Preconditions = NodePositionConditions(result);
        return result;
    }

    internal static OdfOperation CreateMoveNodeOperation(
        OdfDocument document,
        OdfNode target,
        OdfNode destinationParent,
        int destinationIndex,
        long sequence)
    {
        if (target == null || destinationParent == null)
        {
            throw new OdfEditException(
                "A move requires source and destination elements.");
        }
        OdfNode? previousParent = target.Parent;
        int maximumIndex = destinationParent.Children.Length;
        if (previousParent == destinationParent)
        {
            maximumIndex = maximumIndex - 1;
        }
        if (previousParent == null || destinationIndex < 0
            || destinationIndex > maximumIndex)
        {
            throw new OdfEditException(
                "A move requires attached source and destination elements and a valid index.");
        }
        string sourcePart = StableNodeIdModel.FindNodePartPath(
            document, target);
        string destinationPart = StableNodeIdModel.FindNodePartPath(
            document, destinationParent);
        if (sourcePart != destinationPart)
        {
            throw new OdfEditException(
                "A replayable move cannot cross XML parts.");
        }
        OdfOperation result = NewOperation(
            OdfOperationKind.MoveNode, sourcePart, sequence);
        result.TargetNodeId = StableNodeIdModel.EnsureStableNodeId(
            document, target);
        result.ParentNodeId = StableNodeIdModel.EnsureStableNodeId(
            document, destinationParent);
        result.ChildIndex = destinationIndex;
        result.PreviousParentNodeId = StableNodeIdModel.EnsureStableNodeId(
            document, previousParent);
        result.PreviousChildIndex = FindChildIndex(previousParent, target);
        OdfEditPrecondition exists = NodeCondition(
            OdfEditPreconditionKind.NodeExists,
            result.TargetNodeId);
        OdfEditPrecondition parent = NodeCondition(
            OdfEditPreconditionKind.ParentEquals,
            result.TargetNodeId);
        parent.ParentNodeId = result.PreviousParentNodeId;
        OdfEditPrecondition position = NodeCondition(
            OdfEditPreconditionKind.ChildIndexEquals,
            result.TargetNodeId);
        position.ChildIndex = result.PreviousChildIndex;
        OdfEditPrecondition destination = NodeCondition(
            OdfEditPreconditionKind.NodeExists,
            result.ParentNodeId);
        result.Preconditions = [exists, parent, position, destination];
        return result;
    }

    internal static OdfOperation CreateSetAttributeOperation(
        OdfDocument document,
        OdfNode target,
        OdfQualifiedName attributeName,
        string value,
        long sequence)
    {
        RequireAttributeInput(attributeName, value);
        OdfOperation result = NewOperation(
            OdfOperationKind.SetAttribute,
            StableNodeIdModel.FindNodePartPath(document, target),
            sequence);
        result.TargetNodeId = StableNodeIdModel.EnsureStableNodeId(
            document, target);
        result.AttributeName = CopyName(attributeName);
        result.Value = value;
        CaptureAttributeValue(result, target, attributeName);
        result.Preconditions = AttributeConditions(result);
        return result;
    }

    internal static OdfOperation CreateRemoveAttributeOperation(
        OdfDocument document,
        OdfNode target,
        OdfQualifiedName attributeName,
        long sequence)
    {
        RequireAttributeInput(attributeName, "");
        OdfOperation result = NewOperation(
            OdfOperationKind.RemoveAttribute,
            StableNodeIdModel.FindNodePartPath(document, target),
            sequence);
        result.TargetNodeId = StableNodeIdModel.EnsureStableNodeId(
            document, target);
        result.AttributeName = CopyName(attributeName);
        CaptureAttributeValue(result, target, attributeName);
        if (!result.HadPreviousValue)
        {
            throw new OdfEditException(
                "A remove-attribute operation requires an existing attribute.");
        }
        result.Preconditions = AttributeConditions(result);
        return result;
    }

    internal static OdfOperation CreateSetTextOperation(
        OdfDocument document,
        OdfNode target,
        string value,
        long sequence)
    {
        if (value == null)
        {
            throw new OdfEditException("A text value is required.");
        }
        OdfOperation result = NewOperation(
            OdfOperationKind.SetText,
            StableNodeIdModel.FindNodePartPath(document, target),
            sequence);
        result.TargetNodeId = StableNodeIdModel.EnsureStableNodeId(
            document, target);
        OdfPart? part = StableNodeIdModel.FindOperationPart(
            document, result.PartPath);
        if (part == null)
        {
            throw new OdfEditException(
                "The text operation target does not belong to a document part.");
        }
        result.Value = value;
        result.HadPreviousValue = true;
        result.PreviousValue = new OdfTreeEditor(part.Nodes)
            .GetTextContent(target);
        OdfEditPrecondition exists = NodeCondition(
            OdfEditPreconditionKind.NodeExists,
            result.TargetNodeId);
        OdfEditPrecondition text = NodeCondition(
            OdfEditPreconditionKind.TextEquals,
            result.TargetNodeId);
        text.ExpectedValuePresent = true;
        text.ExpectedValue = result.PreviousValue;
        result.Preconditions = [exists, text];
        return result;
    }

    internal static OdfEditResult ReplayOperations(
        OdfDocument document,
        OdfOperation[] operations)
    {
        if (document == null || operations == null)
        {
            throw new OdfEditException(
                "A document and operation collection are required for replay.");
        }
        RequireOperationItems(operations);
        OdfOperation[] ordered = OrderOperations(operations);
        List<OdfOperation> rollback = new List<OdfOperation>();
        int index = 0;
        while (index < ordered.Length)
        {
            OdfOperation operation = ordered[index];
            if (operation == null || !ArePreconditionsSatisfied(
                    document, operation))
            {
                RollBackOperations(document, rollback);
                return EditResult(
                    OdfEditStatus.Conflict,
                    false,
                    0,
                    "An operation precondition was not satisfied.");
            }
            OdfOperation? inverse = CaptureRuntimeInverse(document, operation);
            if (inverse == null || !ApplyOperation(document, operation))
            {
                RollBackOperations(document, rollback);
                return EditResult(
                    OdfEditStatus.Failed,
                    false,
                    0,
                    "An operation could not be replayed.");
            }
            rollback.Add(inverse);
            index = index + 1;
        }
        if (ordered.Length == 0)
        {
            return EditResult(
                OdfEditStatus.NoChange,
                false,
                0,
                "No operations were supplied.");
        }
        return EditResult(
            OdfEditStatus.Success,
            true,
            ordered.Length,
            "The ordered operation log was replayed.");
    }

    internal static OdfOperation? InvertOperation(OdfOperation operation)
    {
        if (operation == null)
        {
            return null;
        }
        OdfOperation result = NewOperation(
            OdfOperationKind.Unknown,
            operation.PartPath,
            operation.Sequence);
        result.Id = operation.Id.Length == 0
            ? "" : operation.Id + "-inverse";
        result.TargetNodeId = operation.TargetNodeId;
        result.Author = operation.Author;
        result.SessionId = operation.SessionId;
        result.Date = operation.Date;
        if (operation.Kind == OdfOperationKind.InsertNode)
        {
            result.Kind = OdfOperationKind.RemoveNode;
            result.ParentNodeId = operation.ParentNodeId;
            result.ChildIndex = operation.ChildIndex;
            result.Payload = CloneOptionalNode(operation.Payload);
        }
        else if (operation.Kind == OdfOperationKind.RemoveNode
            && operation.Payload != null)
        {
            result.Kind = OdfOperationKind.InsertNode;
            result.ParentNodeId = operation.ParentNodeId;
            result.ChildIndex = operation.ChildIndex;
            result.Payload = CloneNode(operation.Payload);
        }
        else if (operation.Kind == OdfOperationKind.MoveNode
            && operation.PreviousParentNodeId.Length > 0
            && operation.PreviousChildIndex >= 0)
        {
            result.Kind = OdfOperationKind.MoveNode;
            result.ParentNodeId = operation.PreviousParentNodeId;
            result.ChildIndex = operation.PreviousChildIndex;
            result.PreviousParentNodeId = operation.ParentNodeId;
            result.PreviousChildIndex = operation.ChildIndex;
        }
        else if (operation.Kind == OdfOperationKind.SetAttribute
            && operation.AttributeName != null)
        {
            result.AttributeName = CopyName(operation.AttributeName);
            if (operation.HadPreviousValue)
            {
                result.Kind = OdfOperationKind.SetAttribute;
                result.Value = operation.PreviousValue;
                result.HadPreviousValue = true;
                result.PreviousValue = operation.Value;
            }
            else
            {
                result.Kind = OdfOperationKind.RemoveAttribute;
                result.HadPreviousValue = true;
                result.PreviousValue = operation.Value;
            }
        }
        else if (operation.Kind == OdfOperationKind.RemoveAttribute
            && operation.AttributeName != null
            && operation.HadPreviousValue)
        {
            result.Kind = OdfOperationKind.SetAttribute;
            result.AttributeName = CopyName(operation.AttributeName);
            result.Value = operation.PreviousValue;
            result.HadPreviousValue = false;
        }
        else if (operation.Kind == OdfOperationKind.SetText
            && operation.HadPreviousValue)
        {
            result.Kind = OdfOperationKind.SetText;
            result.Value = operation.PreviousValue;
            result.HadPreviousValue = true;
            result.PreviousValue = operation.Value;
        }
        else
        {
            return null;
        }
        return result;
    }

    internal static OdfOperation[] InvertOperations(OdfOperation[] operations)
    {
        if (operations == null)
        {
            throw new OdfEditException(
                "An operation collection is required for inversion.");
        }
        RequireOperationItems(operations);
        OdfOperation[] ordered = OrderOperations(operations);
        OdfOperation[] result = new OdfOperation[ordered.Length];
        int index = 0;
        while (index < ordered.Length)
        {
            OdfOperation? inverse = InvertOperation(
                ordered[ordered.Length - index - 1]);
            if (inverse == null)
            {
                throw new OdfEditException(
                    "The operation collection contains an operation that cannot be inverted.");
            }
            inverse.Sequence = index;
            result[index] = inverse;
            index = index + 1;
        }
        return result;
    }

    static bool ArePreconditionsSatisfied(
        OdfDocument document,
        OdfOperation operation)
    {
        int index = 0;
        while (index < operation.Preconditions.Length)
        {
            if (!IsPreconditionSatisfied(
                    document,
                    operation.PartPath,
                    operation.Preconditions[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool IsPreconditionSatisfied(
        OdfDocument document,
        string partPath,
        OdfEditPrecondition condition)
    {
        if (condition == null)
        {
            return false;
        }
        OdfNode? target = FindOperationNode(
            document, partPath, condition.TargetNodeId);
        if (condition.Kind == OdfEditPreconditionKind.NodeAbsent)
        {
            return target == null;
        }
        if (target == null)
        {
            return false;
        }
        if (condition.Kind == OdfEditPreconditionKind.NodeExists)
        {
            return true;
        }
        if (condition.Kind == OdfEditPreconditionKind.ParentEquals)
        {
            return target.Parent != null
                && StableNodeIdModel.ReadStableNodeId(target.Parent)
                    == condition.ParentNodeId;
        }
        if (condition.Kind == OdfEditPreconditionKind.ChildIndexEquals)
        {
            return target.Parent != null
                && FindChildIndex(target.Parent, target) == condition.ChildIndex;
        }
        OdfPart? part = StableNodeIdModel.FindOperationPart(
            document, partPath);
        if (part == null)
        {
            return false;
        }
        OdfTreeEditor editor = new OdfTreeEditor(part.Nodes);
        if (condition.Kind == OdfEditPreconditionKind.AttributeEquals
            && condition.AttributeName != null)
        {
            OdfAttribute? attribute = editor.FindAttribute(
                target, condition.AttributeName);
            if (!condition.ExpectedValuePresent)
            {
                return attribute == null;
            }
            return attribute != null
                && attribute.Value == condition.ExpectedValue;
        }
        if (condition.Kind == OdfEditPreconditionKind.TextEquals)
        {
            return editor.GetTextContent(target) == condition.ExpectedValue;
        }
        return false;
    }

    static bool ApplyOperation(
        OdfDocument document,
        OdfOperation operation)
    {
        if (!OdfNameRules.IsXmlId(operation.TargetNodeId))
        {
            return false;
        }
        OdfPart? part = StableNodeIdModel.FindOperationPart(
            document, operation.PartPath);
        if (part == null)
        {
            return false;
        }
        OdfTreeEditor editor = new OdfTreeEditor(part.Nodes);
        OdfNode? target = FindOperationNode(
            document, operation.PartPath, operation.TargetNodeId);
        if (operation.Kind == OdfOperationKind.InsertNode)
        {
            OdfNode? parent = FindOperationNode(
                document, operation.PartPath, operation.ParentNodeId);
            if (parent == null || operation.Payload == null
                || operation.ChildIndex < 0
                || operation.ChildIndex > parent.Children.Length
                || target != null
                || !PayloadStableIdsAvailable(
                    document, operation.Payload, operation.TargetNodeId))
            {
                return false;
            }
            OdfNode inserted = CloneNode(operation.Payload);
            StableNodeIdModel.WriteStableNodeId(
                inserted, operation.TargetNodeId);
            editor.InsertChild(parent, operation.ChildIndex, inserted);
            return true;
        }
        if (target == null)
        {
            return false;
        }
        if (operation.Kind == OdfOperationKind.RemoveNode)
        {
            if (target.Parent == null)
            {
                return false;
            }
            editor.RemoveNode(target);
            return true;
        }
        if (operation.Kind == OdfOperationKind.MoveNode)
        {
            OdfNode? parent = FindOperationNode(
                document, operation.PartPath, operation.ParentNodeId);
            int maximumIndex = parent == null
                ? -1 : parent.Children.Length;
            if (target.Parent == parent)
            {
                maximumIndex = maximumIndex - 1;
            }
            if (parent == null || operation.ChildIndex < 0
                || operation.ChildIndex > maximumIndex
                || ContainsNode(target, parent))
            {
                return false;
            }
            editor.ReparentNode(target, parent, operation.ChildIndex);
            return true;
        }
        if (operation.Kind == OdfOperationKind.SetAttribute
            && operation.AttributeName != null
            && IsReplayableAttributeName(operation.AttributeName))
        {
            editor.SetAttribute(
                target, operation.AttributeName, operation.Value);
            return true;
        }
        if (operation.Kind == OdfOperationKind.RemoveAttribute
            && operation.AttributeName != null
            && IsReplayableAttributeName(operation.AttributeName))
        {
            editor.RemoveAttribute(target, operation.AttributeName);
            return true;
        }
        if (operation.Kind == OdfOperationKind.SetText)
        {
            editor.SetTextContent(target, operation.Value);
            return true;
        }
        return false;
    }

    static OdfOperation? CaptureRuntimeInverse(
        OdfDocument document,
        OdfOperation operation)
    {
        OdfOperation snapshot = CopyOperation(operation);
        OdfNode? target = FindOperationNode(
            document, operation.PartPath, operation.TargetNodeId);
        if (operation.Kind == OdfOperationKind.InsertNode)
        {
            return InvertOperation(snapshot);
        }
        if (target == null)
        {
            return null;
        }
        if (operation.Kind == OdfOperationKind.RemoveNode)
        {
            if (target.Parent == null)
            {
                return null;
            }
            snapshot.ParentNodeId = StableNodeIdModel.EnsureStableNodeId(
                document, target.Parent);
            snapshot.ChildIndex = FindChildIndex(target.Parent, target);
            snapshot.Payload = CloneNode(target);
        }
        else if (operation.Kind == OdfOperationKind.MoveNode)
        {
            if (target.Parent == null)
            {
                return null;
            }
            snapshot.PreviousParentNodeId =
                StableNodeIdModel.EnsureStableNodeId(document, target.Parent);
            snapshot.PreviousChildIndex = FindChildIndex(target.Parent, target);
        }
        else if (operation.Kind == OdfOperationKind.SetAttribute
            || operation.Kind == OdfOperationKind.RemoveAttribute)
        {
            if (operation.AttributeName == null)
            {
                return null;
            }
            CaptureAttributeValue(snapshot, target, operation.AttributeName);
        }
        else if (operation.Kind == OdfOperationKind.SetText)
        {
            OdfPart? part = StableNodeIdModel.FindOperationPart(
                document, operation.PartPath);
            if (part == null)
            {
                return null;
            }
            snapshot.HadPreviousValue = true;
            snapshot.PreviousValue = new OdfTreeEditor(part.Nodes)
                .GetTextContent(target);
        }
        return InvertOperation(snapshot);
    }

    static void RollBackOperations(
        OdfDocument document,
        List<OdfOperation> rollback)
    {
        int index = rollback.Count - 1;
        while (index >= 0)
        {
            ApplyOperation(document, rollback[index]);
            index = index - 1;
        }
    }

    static OdfOperation[] OrderOperations(OdfOperation[] operations)
    {
        OdfOperation[] result = new OdfOperation[operations.Length];
        Array.Copy(operations, result, operations.Length);
        int index = 1;
        while (index < result.Length)
        {
            OdfOperation current = result[index];
            int insertion = index;
            while (insertion > 0
                && result[insertion - 1].Sequence > current.Sequence)
            {
                result[insertion] = result[insertion - 1];
                insertion = insertion - 1;
            }
            result[insertion] = current;
            index = index + 1;
        }
        return result;
    }

    static OdfOperation NewOperation(
        OdfOperationKind kind,
        string partPath,
        long sequence)
    {
        OdfOperation result = new OdfOperation();
        result.Id = "odf-operation-" + Guid.NewGuid().ToString("N");
        result.Kind = kind;
        result.PartPath = partPath;
        result.Sequence = sequence;
        return result;
    }

    static OdfEditPrecondition NodeCondition(
        OdfEditPreconditionKind kind,
        string targetNodeId)
    {
        OdfEditPrecondition result = new OdfEditPrecondition();
        result.Kind = kind;
        result.TargetNodeId = targetNodeId;
        return result;
    }

    static OdfEditPrecondition[] NodePositionConditions(
        OdfOperation operation)
    {
        OdfEditPrecondition exists = NodeCondition(
            OdfEditPreconditionKind.NodeExists,
            operation.TargetNodeId);
        OdfEditPrecondition parent = NodeCondition(
            OdfEditPreconditionKind.ParentEquals,
            operation.TargetNodeId);
        parent.ParentNodeId = operation.ParentNodeId;
        OdfEditPrecondition index = NodeCondition(
            OdfEditPreconditionKind.ChildIndexEquals,
            operation.TargetNodeId);
        index.ChildIndex = operation.ChildIndex;
        return [exists, parent, index];
    }

    static OdfEditPrecondition[] AttributeConditions(OdfOperation operation)
    {
        OdfEditPrecondition exists = NodeCondition(
            OdfEditPreconditionKind.NodeExists,
            operation.TargetNodeId);
        OdfEditPrecondition value = NodeCondition(
            OdfEditPreconditionKind.AttributeEquals,
            operation.TargetNodeId);
        value.AttributeName = CopyOptionalName(operation.AttributeName);
        value.ExpectedValuePresent = operation.HadPreviousValue;
        value.ExpectedValue = operation.PreviousValue;
        return [exists, value];
    }

    static void CaptureAttributeValue(
        OdfOperation operation,
        OdfNode target,
        OdfQualifiedName attributeName)
    {
        OdfAttribute? attribute = FindAttribute(target, attributeName);
        operation.HadPreviousValue = attribute != null;
        operation.PreviousValue = attribute == null ? "" : attribute.Value;
    }

    static OdfAttribute? FindAttribute(
        OdfNode node,
        OdfQualifiedName name)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == name.NamespaceName
                && attribute.Name.LocalName == name.LocalName)
            {
                return attribute;
            }
            index = index + 1;
        }
        return null;
    }

    static OdfNode? FindOperationNode(
        OdfDocument document,
        string partPath,
        string stableId)
    {
        OdfNode? result = StableNodeIdModel.FindStableNode(
            document, stableId);
        if (result == null)
        {
            return null;
        }
        return StableNodeIdModel.FindNodePartPath(document, result) == partPath
            ? result : null;
    }

    static string NewDetachedNodeId(OdfDocument document)
    {
        string result = "odf-node-" + Guid.NewGuid().ToString("N");
        while (StableNodeIdModel.FindStableNode(document, result) != null)
        {
            result = "odf-node-" + Guid.NewGuid().ToString("N");
        }
        return result;
    }

    static bool ContainsNode(OdfNode ancestor, OdfNode candidate)
    {
        OdfNode? current = candidate;
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

    static void RequireOperationItems(OdfOperation[] operations)
    {
        int index = 0;
        while (index < operations.Length)
        {
            if (operations[index] == null)
            {
                throw new OdfEditException(
                    "An operation collection cannot contain a null operation.");
            }
            index = index + 1;
        }
    }

    static int FindChildIndex(OdfNode parent, OdfNode child)
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

    static void RequireAttributeInput(
        OdfQualifiedName attributeName,
        string value)
    {
        if (!IsReplayableAttributeName(attributeName) || value == null)
        {
            throw new OdfEditException(
                "An attribute operation requires a qualified name and value.");
        }
    }

    static bool IsReplayableAttributeName(OdfQualifiedName attributeName)
    {
        return OdfNameRules.IsQualifiedNameValue(attributeName)
            && !(attributeName.NamespaceName == OdfNamespaces.Collaboration
                && attributeName.LocalName == "node-id");
    }

    static bool PayloadStableIdsAvailable(
        OdfDocument document,
        OdfNode payload,
        string rootStableId)
    {
        List<string> found = new List<string>();
        return PayloadStableIdsAvailableInTree(
            document, payload, payload, rootStableId, found);
    }

    static bool PayloadStableIdsAvailableInTree(
        OdfDocument document,
        OdfNode node,
        OdfNode root,
        string rootStableId,
        List<string> found)
    {
        string stableId = node == root
            ? rootStableId : StableNodeIdModel.ReadStableNodeId(node);
        if (stableId.Length > 0)
        {
            if (!OdfNameRules.IsXmlId(stableId)
                || StableNodeIdModel.FindStableNode(document, stableId) != null
                || ContainsString(found, stableId))
            {
                return false;
            }
            found.Add(stableId);
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            if (!PayloadStableIdsAvailableInTree(
                    document,
                    node.Children[index],
                    root,
                    rootStableId,
                    found))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool ContainsString(List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static OdfNode CloneNode(OdfNode node)
    {
        return new OdfTreeEditor([]).CloneSubtree(node);
    }

    static OdfNode? CloneOptionalNode(OdfNode? node)
    {
        return node == null ? null : CloneNode(node);
    }

    static OdfQualifiedName CopyName(OdfQualifiedName name)
    {
        OdfQualifiedName result = new OdfQualifiedName();
        result.NamespaceName = name.NamespaceName;
        result.LocalName = name.LocalName;
        result.Prefix = name.Prefix;
        return result;
    }

    static OdfQualifiedName? CopyOptionalName(OdfQualifiedName? name)
    {
        return name == null ? null : CopyName(name);
    }

    static OdfOperation CopyOperation(OdfOperation source)
    {
        OdfOperation result = new OdfOperation();
        result.Id = source.Id;
        result.Sequence = source.Sequence;
        result.Kind = source.Kind;
        result.PartPath = source.PartPath;
        result.TargetNodeId = source.TargetNodeId;
        result.ParentNodeId = source.ParentNodeId;
        result.ChildIndex = source.ChildIndex;
        result.PreviousParentNodeId = source.PreviousParentNodeId;
        result.PreviousChildIndex = source.PreviousChildIndex;
        result.AttributeName = CopyOptionalName(source.AttributeName);
        result.Value = source.Value;
        result.HadPreviousValue = source.HadPreviousValue;
        result.PreviousValue = source.PreviousValue;
        result.Author = source.Author;
        result.SessionId = source.SessionId;
        result.Date = source.Date;
        result.Preconditions = source.Preconditions;
        result.Payload = CloneOptionalNode(source.Payload);
        result.ExtensionNode = CloneOptionalNode(source.ExtensionNode);
        return result;
    }

    static OdfEditResult EditResult(
        OdfEditStatus status,
        bool changed,
        int affected,
        string message)
    {
        OdfEditResult result = new OdfEditResult();
        result.Status = status;
        result.Changed = changed;
        result.AffectedCount = affected;
        result.Message = message;
        return result;
    }
}
