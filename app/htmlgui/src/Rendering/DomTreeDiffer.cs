using System;
using System.Collections.Generic;
using System.Text;

namespace htmlgui;

internal class DomTreeDiffState
{
    internal List<DomMutation> Mutations;

    internal DomTreeDiffState()
    {
        Mutations = new List<DomMutation>();
    }
}

internal class DomStyleDeclaration
{
    internal string Name = string.Empty;
    internal string Value = string.Empty;
}

internal class DomTreeDiffer
{
    internal const int MaximumValueDepth = 64;
    internal const int MaximumValueCollectionCount = 10000;
    internal const int MaximumValueStringLength = 1048576;
    internal const int MaximumValueBinaryLength = 1048576;

    internal DomMutation[] CreateMutations(DomSnapshot? before, DomSnapshot? after)
    {
        if (before == null)
        {
            throw new ArgumentNullException("before");
        }

        if (after == null)
        {
            throw new ArgumentNullException("after");
        }

        if (!string.IsNullOrEmpty(before.ContentHash) && string.Equals(before.ContentHash, after.ContentHash, StringComparison.Ordinal))
        {
            return Array.Empty<DomMutation>();
        }

        DomTreeDiffState state = new DomTreeDiffState();
        DomNode[] beforeRoots = before.Roots ?? Array.Empty<DomNode>();
        DomNode[] afterRoots = after.Roots ?? Array.Empty<DomNode>();
        if (SiblingKeysRequireFallback(beforeRoots, afterRoots))
        {
            AddFullDocumentFallback(beforeRoots, afterRoots, state);
        }
        else
        {
            DiffSiblingNodes(beforeRoots, afterRoots, null, state);
        }

        return CoalesceMutations(state.Mutations);
    }

    internal void DiffSiblingNodes(DomNode[] before, DomNode[] after, string? parentId, DomTreeDiffState state)
    {
        List<DomNode> working = new List<DomNode>();
        int removalIndex = before.Length - 1;
        while (removalIndex >= 0)
        {
            DomNode beforeNode = RequireNode(before[removalIndex], "before");
            if (!ContainsNodeKey(after, beforeNode.Key))
            {
                AddRemoveMutation(state, beforeNode, parentId, removalIndex);
            }

            removalIndex--;
        }

        int beforeIndex = 0;
        while (beforeIndex < before.Length)
        {
            DomNode beforeNode = RequireNode(before[beforeIndex], "before");
            if (ContainsNodeKey(after, beforeNode.Key))
            {
                working.Add(beforeNode);
            }

            beforeIndex++;
        }

        int afterIndex = 0;
        while (afterIndex < after.Length)
        {
            DomNode afterNode = RequireNode(after[afterIndex], "after");
            int workingIndex = FindNodeKeyIndexInList(working, afterNode.Key);
            if (workingIndex < 0)
            {
                string? referenceId = GetReferenceIdentifier(working, afterIndex);
                AddInsertMutation(state, afterNode, parentId, referenceId, afterIndex);
                working.Insert(afterIndex, afterNode);
            }
            else if (workingIndex != afterIndex)
            {
                DomNode moved = working[workingIndex];
                working.RemoveAt(workingIndex);
                working.Insert(afterIndex, moved);
                string? referenceId = GetReferenceIdentifier(working, afterIndex + 1);
                AddMoveMutation(state, moved, parentId, referenceId, workingIndex, afterIndex);
            }

            afterIndex++;
        }

        int matchedIndex = 0;
        while (matchedIndex < after.Length)
        {
            DomNode afterNode = RequireNode(after[matchedIndex], "after");
            int originalIndex = FindNodeKeyIndexInArray(before, afterNode.Key);
            if (originalIndex >= 0)
            {
                DomNode beforeNode = RequireNode(before[originalIndex], "before");
                DiffMatchedNodes(beforeNode, afterNode, parentId, matchedIndex, state);
            }

            matchedIndex++;
        }
    }

    internal void DiffMatchedNodes(DomNode before, DomNode after, string? parentId, int index, DomTreeDiffState state)
    {
        if (NodesRequireReplacement(before, after))
        {
            AddReplaceMutation(state, before, after, parentId, index);
            return;
        }

        if (CanBailOutSubtree(before, after))
        {
            return;
        }

        DomNode[] beforeChildren = before.Children ?? Array.Empty<DomNode>();
        DomNode[] afterChildren = after.Children ?? Array.Empty<DomNode>();
        if (SiblingKeysRequireFallback(beforeChildren, afterChildren))
        {
            AddReplaceMutation(state, before, after, parentId, index);
            return;
        }

        if (IsTextBearingNode(after))
        {
            if (!string.Equals(before.Text, after.Text, StringComparison.Ordinal))
            {
                AddTextMutation(state, before, after.Text);
            }

            return;
        }

        DiffAttributes(before, after, state);
        DiffProperties(before, after, state);
        DiffEventSubscriptions(before, after, state);
        DiffSiblingNodes(beforeChildren, afterChildren, after.Id, state);
    }

    internal bool CanBailOutSubtree(DomNode before, DomNode after)
    {
        bool beforeHasHash = !string.IsNullOrEmpty(before.SubtreeHash);
        bool afterHasHash = !string.IsNullOrEmpty(after.SubtreeHash);
        if (beforeHasHash || afterHasHash)
        {
            return beforeHasHash && afterHasHash && string.Equals(before.SubtreeHash, after.SubtreeHash, StringComparison.Ordinal);
        }

        return before.Version > 0 && before.Version == after.Version;
    }

    internal bool SiblingKeysRequireFallback(DomNode[] before, DomNode[] after)
    {
        if (HasInvalidSiblingKeys(before) || HasInvalidSiblingKeys(after))
        {
            return true;
        }

        int beforeIndex = 0;
        while (beforeIndex < before.Length)
        {
            DomNode? beforeNode = before[beforeIndex];
            if (beforeNode != null)
            {
                int afterIndex = 0;
                while (afterIndex < after.Length)
                {
                    DomNode? afterNode = after[afterIndex];
                    if (afterNode != null
                        && beforeNode.Id.Length > 0
                        && string.Equals(beforeNode.Id, afterNode.Id, StringComparison.Ordinal)
                        && !string.Equals(beforeNode.Key, afterNode.Key, StringComparison.Ordinal))
                    {
                        return true;
                    }

                    afterIndex++;
                }
            }

            beforeIndex++;
        }

        return false;
    }

    internal bool HasInvalidSiblingKeys(DomNode[] nodes)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            DomNode? node = nodes[index];
            if (node != null)
            {
                if (string.IsNullOrWhiteSpace(node.Key) || IsPositionalFallbackKey(node, index))
                {
                    return true;
                }

                int earlierIndex = 0;
                while (earlierIndex < index)
                {
                    DomNode? earlier = nodes[earlierIndex];
                    if (earlier != null && string.Equals(earlier.Key, node.Key, StringComparison.Ordinal))
                    {
                        return true;
                    }

                    earlierIndex++;
                }
            }

            index++;
        }

        return false;
    }

    internal bool IsPositionalFallbackKey(DomNode node, int siblingIndex)
    {
        string expected = "$" + GetDiffNodeKindToken(node.Kind) + "-" + InvariantFormatter.FormatInt32(siblingIndex);
        return string.Equals(node.Key, expected, StringComparison.Ordinal);
    }

    internal string GetDiffNodeKindToken(DomNodeKind kind)
    {
        switch (kind)
        {
            case DomNodeKind.Element:
                return "element";
            case DomNodeKind.Text:
                return "text";
            case DomNodeKind.Comment:
                return "comment";
            case DomNodeKind.Fragment:
                return "fragment";
            case DomNodeKind.RawContent:
                return "raw-content";
            default:
                return "unknown-" + InvariantFormatter.FormatInt32((int)kind);
        }
    }

    internal void DiffAttributes(DomNode before, DomNode after, DomTreeDiffState state)
    {
        DomAttribute[] beforeAttributes = CloneDomAttributes(before.Attributes);
        DomAttribute[] afterAttributes = CloneDomAttributes(after.Attributes);
        SortDomAttributes(beforeAttributes);
        SortDomAttributes(afterAttributes);
        int beforeIndex = 0;
        while (beforeIndex < beforeAttributes.Length)
        {
            DomAttribute attribute = beforeAttributes[beforeIndex];
            if (!IsSpecialAttribute(before, attribute) && FindDomAttributeIndex(afterAttributes, attribute.NamespaceUri, attribute.Name) < 0)
            {
                AddRemoveAttributeMutation(state, before.Id, attribute);
            }

            beforeIndex++;
        }

        int afterIndex = 0;
        while (afterIndex < afterAttributes.Length)
        {
            DomAttribute attribute = afterAttributes[afterIndex];
            if (!IsSpecialAttribute(after, attribute))
            {
                int matchingIndex = FindDomAttributeIndex(beforeAttributes, attribute.NamespaceUri, attribute.Name);
                if (matchingIndex < 0 || !DomAttributesEqual(beforeAttributes[matchingIndex], attribute))
                {
                    AddSetAttributeMutation(state, after.Id, attribute);
                }
            }

            afterIndex++;
        }

        DiffClassAttribute(before, after, beforeAttributes, afterAttributes, state);
        DiffStyleAttribute(before, after, beforeAttributes, afterAttributes, state);
    }

    internal void DiffClassAttribute(DomNode before, DomNode after, DomAttribute[] beforeAttributes, DomAttribute[] afterAttributes, DomTreeDiffState state)
    {
        DomAttribute? beforeClass = FindHtmlAttribute(beforeAttributes, HtmlAttributes.Class);
        DomAttribute? afterClass = FindHtmlAttribute(afterAttributes, HtmlAttributes.Class);
        List<string> beforeTokens = ParseClassTokens(beforeClass == null ? string.Empty : beforeClass.Value);
        List<string> afterTokens = ParseClassTokens(afterClass == null ? string.Empty : afterClass.Value);
        int beforeIndex = 0;
        while (beforeIndex < beforeTokens.Count)
        {
            string token = beforeTokens[beforeIndex];
            if (!ContainsOrdinalString(afterTokens, token))
            {
                AddClassMutation(state, UiMutationKind.RemoveClass, before.Id, token);
            }

            beforeIndex++;
        }

        int afterIndex = 0;
        while (afterIndex < afterTokens.Count)
        {
            string token = afterTokens[afterIndex];
            if (!ContainsOrdinalString(beforeTokens, token))
            {
                AddClassMutation(state, UiMutationKind.AddClass, after.Id, token);
            }

            afterIndex++;
        }
    }

    internal void DiffStyleAttribute(DomNode before, DomNode after, DomAttribute[] beforeAttributes, DomAttribute[] afterAttributes, DomTreeDiffState state)
    {
        DomAttribute? beforeStyle = FindHtmlAttribute(beforeAttributes, HtmlAttributes.Style);
        DomAttribute? afterStyle = FindHtmlAttribute(afterAttributes, HtmlAttributes.Style);
        List<DomStyleDeclaration> beforeDeclarations = ParseStyleDeclarations(beforeStyle == null ? string.Empty : beforeStyle.Value);
        List<DomStyleDeclaration> afterDeclarations = ParseStyleDeclarations(afterStyle == null ? string.Empty : afterStyle.Value);
        int beforeIndex = 0;
        while (beforeIndex < beforeDeclarations.Count)
        {
            DomStyleDeclaration declaration = beforeDeclarations[beforeIndex];
            if (FindStyleDeclarationIndex(afterDeclarations, declaration.Name) < 0)
            {
                AddStyleMutation(state, UiMutationKind.RemoveStyle, before.Id, declaration.Name, null);
            }

            beforeIndex++;
        }

        int afterIndex = 0;
        while (afterIndex < afterDeclarations.Count)
        {
            DomStyleDeclaration declaration = afterDeclarations[afterIndex];
            int matchingIndex = FindStyleDeclarationIndex(beforeDeclarations, declaration.Name);
            if (matchingIndex < 0 || !string.Equals(beforeDeclarations[matchingIndex].Value, declaration.Value, StringComparison.Ordinal))
            {
                AddStyleMutation(state, UiMutationKind.SetStyle, after.Id, declaration.Name, declaration.Value);
            }

            afterIndex++;
        }
    }

    internal void DiffProperties(DomNode before, DomNode after, DomTreeDiffState state)
    {
        List<DomProperty> beforeProperties = CreateEffectiveProperties(before);
        List<DomProperty> afterProperties = CreateEffectiveProperties(after);
        SortDomProperties(beforeProperties);
        SortDomProperties(afterProperties);
        int beforeIndex = 0;
        while (beforeIndex < beforeProperties.Count)
        {
            DomProperty property = beforeProperties[beforeIndex];
            if (FindDomPropertyIndex(afterProperties, property.Name) < 0)
            {
                AddRemovePropertyMutation(state, before.Id, property.Name);
            }

            beforeIndex++;
        }

        int afterIndex = 0;
        while (afterIndex < afterProperties.Count)
        {
            DomProperty property = afterProperties[afterIndex];
            int matchingIndex = FindDomPropertyIndex(beforeProperties, property.Name);
            if (matchingIndex < 0 || !DomPropertyValuesEqual(beforeProperties[matchingIndex], property))
            {
                AddSetPropertyMutation(state, after.Id, property);
            }

            afterIndex++;
        }
    }

    internal void DiffEventSubscriptions(DomNode before, DomNode after, DomTreeDiffState state)
    {
        DomEventSubscription[] beforeSubscriptions = CloneDomSubscriptions(before.EventSubscriptions);
        DomEventSubscription[] afterSubscriptions = CloneDomSubscriptions(after.EventSubscriptions);
        SortDomSubscriptions(beforeSubscriptions);
        SortDomSubscriptions(afterSubscriptions);
        int beforeIndex = 0;
        while (beforeIndex < beforeSubscriptions.Length)
        {
            DomEventSubscription subscription = beforeSubscriptions[beforeIndex];
            int matchingIndex = FindDomSubscriptionIndex(afterSubscriptions, subscription.Id);
            if (matchingIndex < 0 || !DomSubscriptionsEqual(subscription, afterSubscriptions[matchingIndex]))
            {
                AddEventMutation(state, UiMutationKind.UnsubscribeEvent, before.Id, subscription);
            }

            beforeIndex++;
        }

        int afterIndex = 0;
        while (afterIndex < afterSubscriptions.Length)
        {
            DomEventSubscription subscription = afterSubscriptions[afterIndex];
            int matchingIndex = FindDomSubscriptionIndex(beforeSubscriptions, subscription.Id);
            if (matchingIndex < 0 || !DomSubscriptionsEqual(beforeSubscriptions[matchingIndex], subscription))
            {
                AddEventMutation(state, UiMutationKind.SubscribeEvent, after.Id, subscription);
            }

            afterIndex++;
        }
    }

    internal bool NodesRequireReplacement(DomNode before, DomNode after)
    {
        if (before.Kind != after.Kind || !string.Equals(before.Id, after.Id, StringComparison.Ordinal))
        {
            return true;
        }

        if (!string.Equals(before.Name, after.Name, StringComparison.Ordinal) || !string.Equals(before.NamespaceUri, after.NamespaceUri, StringComparison.Ordinal))
        {
            return true;
        }

        if (before.Kind == DomNodeKind.RawContent && !string.Equals(before.RawContentPolicyId, after.RawContentPolicyId, StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    internal bool IsTextBearingNode(DomNode node)
    {
        return node.Kind == DomNodeKind.Text || node.Kind == DomNodeKind.Comment || node.Kind == DomNodeKind.RawContent;
    }

    internal bool ContainsNodeKey(DomNode[] nodes, string key)
    {
        return FindNodeKeyIndexInArray(nodes, key) >= 0;
    }

    internal int FindNodeKeyIndexInArray(DomNode[] nodes, string key)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            DomNode node = RequireNode(nodes[index], "nodes");
            if (string.Equals(node.Key, key, StringComparison.Ordinal))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal int FindNodeKeyIndexInList(List<DomNode> nodes, string key)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (string.Equals(nodes[index].Key, key, StringComparison.Ordinal))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal string? GetReferenceIdentifier(List<DomNode> nodes, int index)
    {
        if (index >= 0 && index < nodes.Count)
        {
            return nodes[index].Id;
        }

        return null;
    }

    internal DomNode RequireNode(DomNode? node, string parameterName)
    {
        if (node == null)
        {
            throw new ArgumentException("DOM snapshot node collections cannot contain null.", parameterName);
        }

        return node;
    }

    internal void AddFullDocumentFallback(DomNode[] before, DomNode[] after, DomTreeDiffState state)
    {
        int beforeIndex = before.Length - 1;
        while (beforeIndex >= 0)
        {
            DomNode beforeNode = RequireNode(before[beforeIndex], "before");
            AddRemoveMutation(state, beforeNode, null, beforeIndex);
            beforeIndex--;
        }

        int afterIndex = 0;
        while (afterIndex < after.Length)
        {
            DomNode afterNode = RequireNode(after[afterIndex], "after");
            AddInsertMutation(state, afterNode, null, null, afterIndex);
            afterIndex++;
        }
    }

    internal DomMutation[] CoalesceMutations(List<DomMutation> mutations)
    {
        List<DomMutation> result = new List<DomMutation>();
        int index = 0;
        while (index < mutations.Count)
        {
            DomMutation mutation = mutations[index];
            int existingIndex = FindCoalescingMutationIndex(result, mutation);
            if (existingIndex >= 0)
            {
                result[existingIndex] = mutation;
            }
            else
            {
                result.Add(mutation);
            }

            index++;
        }

        int sequence = 0;
        while (sequence < result.Count)
        {
            result[sequence].Sequence = sequence;
            sequence++;
        }

        return result.ToArray();
    }

    internal int FindCoalescingMutationIndex(List<DomMutation> mutations, DomMutation candidate)
    {
        int index = mutations.Count - 1;
        while (index >= 0)
        {
            DomMutation existing = mutations[index];
            if (string.Equals(existing.TargetId, candidate.TargetId, StringComparison.Ordinal))
            {
                if (MutationsShareCoalescingSlot(existing, candidate))
                {
                    return index;
                }

                if (IsStructuralMutation(existing) || IsStructuralMutation(candidate))
                {
                    return -1;
                }
            }

            index--;
        }

        return -1;
    }

    internal bool MutationsShareCoalescingSlot(DomMutation existing, DomMutation candidate)
    {
        switch (candidate.Kind)
        {
            case UiMutationKind.SetText:
                return existing.Kind == UiMutationKind.SetText;
            case UiMutationKind.SetAttribute:
            case UiMutationKind.RemoveAttribute:
                return (existing.Kind == UiMutationKind.SetAttribute || existing.Kind == UiMutationKind.RemoveAttribute)
                    && AttributeMutationsShareIdentity(existing, candidate);
            case UiMutationKind.SetProperty:
            case UiMutationKind.RemoveProperty:
                return (existing.Kind == UiMutationKind.SetProperty || existing.Kind == UiMutationKind.RemoveProperty)
                    && string.Equals(existing.Name, candidate.Name, StringComparison.Ordinal);
            case UiMutationKind.AddClass:
            case UiMutationKind.RemoveClass:
                return (existing.Kind == UiMutationKind.AddClass || existing.Kind == UiMutationKind.RemoveClass)
                    && string.Equals(existing.Name, candidate.Name, StringComparison.Ordinal);
            case UiMutationKind.SetStyle:
            case UiMutationKind.RemoveStyle:
                return (existing.Kind == UiMutationKind.SetStyle || existing.Kind == UiMutationKind.RemoveStyle)
                    && string.Equals(existing.Name, candidate.Name, StringComparison.Ordinal);
            case UiMutationKind.SubscribeEvent:
                return existing.Kind == UiMutationKind.SubscribeEvent && string.Equals(existing.Name, candidate.Name, StringComparison.Ordinal);
            case UiMutationKind.UnsubscribeEvent:
                return existing.Kind == UiMutationKind.UnsubscribeEvent && string.Equals(existing.Name, candidate.Name, StringComparison.Ordinal);
            case UiMutationKind.Replace:
                return existing.Kind == UiMutationKind.Replace;
            default:
                return false;
        }
    }

    internal bool AttributeMutationsShareIdentity(DomMutation left, DomMutation right)
    {
        string? leftNamespace = left.Attribute == null ? null : left.Attribute.NamespaceUri;
        string? rightNamespace = right.Attribute == null ? null : right.Attribute.NamespaceUri;
        return string.Equals(leftNamespace, rightNamespace, StringComparison.Ordinal)
            && string.Equals(left.Name, right.Name, StringComparison.Ordinal);
    }

    internal bool IsStructuralMutation(DomMutation mutation)
    {
        return mutation.Kind == UiMutationKind.Insert
            || mutation.Kind == UiMutationKind.Remove
            || mutation.Kind == UiMutationKind.Move
            || mutation.Kind == UiMutationKind.Replace;
    }

    internal void AddRemoveMutation(DomTreeDiffState state, DomNode node, string? parentId, int index)
    {
        DomMutation mutation = new DomMutation
        {
            Kind = UiMutationKind.Remove,
            TargetId = node.Id,
            ParentId = parentId,
            HasFromIndex = true,
            FromIndex = index
        };
        AddMutation(state, mutation);
    }

    internal void AddInsertMutation(DomTreeDiffState state, DomNode node, string? parentId, string? referenceId, int index)
    {
        DomMutation mutation = new DomMutation
        {
            Kind = UiMutationKind.Insert,
            TargetId = node.Id,
            ParentId = parentId,
            ReferenceId = referenceId,
            Node = CloneDomNode(node),
            HasIndex = true,
            Index = index
        };
        AddMutation(state, mutation);
    }

    internal void AddMoveMutation(DomTreeDiffState state, DomNode node, string? parentId, string? referenceId, int fromIndex, int toIndex)
    {
        DomMutation mutation = new DomMutation
        {
            Kind = UiMutationKind.Move,
            TargetId = node.Id,
            ParentId = parentId,
            ReferenceId = referenceId,
            HasFromIndex = true,
            FromIndex = fromIndex,
            HasToIndex = true,
            ToIndex = toIndex
        };
        AddMutation(state, mutation);
    }

    internal void AddReplaceMutation(DomTreeDiffState state, DomNode before, DomNode after, string? parentId, int index)
    {
        DomMutation mutation = new DomMutation
        {
            Kind = UiMutationKind.Replace,
            TargetId = before.Id,
            ParentId = parentId,
            Node = CloneDomNode(after),
            HasIndex = true,
            Index = index
        };
        AddMutation(state, mutation);
    }

    internal void AddTextMutation(DomTreeDiffState state, DomNode before, string? text)
    {
        DomMutation mutation = new DomMutation
        {
            Kind = UiMutationKind.SetText,
            TargetId = before.Id,
            Value = text ?? string.Empty
        };
        AddMutation(state, mutation);
    }

    internal void AddRemoveAttributeMutation(DomTreeDiffState state, string targetId, DomAttribute attribute)
    {
        DomMutation mutation = new DomMutation
        {
            Kind = UiMutationKind.RemoveAttribute,
            TargetId = targetId,
            Attribute = CloneDomAttributeValue(attribute),
            Name = attribute.Name
        };
        AddMutation(state, mutation);
    }

    internal void AddSetAttributeMutation(DomTreeDiffState state, string targetId, DomAttribute attribute)
    {
        DomMutation mutation = new DomMutation
        {
            Kind = UiMutationKind.SetAttribute,
            TargetId = targetId,
            Attribute = CloneDomAttributeValue(attribute),
            Name = attribute.Name,
            Value = attribute.Value
        };
        AddMutation(state, mutation);
    }

    internal void AddClassMutation(DomTreeDiffState state, UiMutationKind kind, string targetId, string token)
    {
        DomMutation mutation = new DomMutation
        {
            Kind = kind,
            TargetId = targetId,
            Name = token,
            Value = token
        };
        AddMutation(state, mutation);
    }

    internal void AddStyleMutation(DomTreeDiffState state, UiMutationKind kind, string targetId, string name, string? value)
    {
        DomMutation mutation = new DomMutation
        {
            Kind = kind,
            TargetId = targetId,
            Name = name,
            Value = value
        };
        AddMutation(state, mutation);
    }

    internal void AddRemovePropertyMutation(DomTreeDiffState state, string targetId, string name)
    {
        DomMutation mutation = new DomMutation
        {
            Kind = UiMutationKind.RemoveProperty,
            TargetId = targetId,
            Name = name
        };
        AddMutation(state, mutation);
    }

    internal void AddSetPropertyMutation(DomTreeDiffState state, string targetId, DomProperty property)
    {
        DomProperty detached = CloneDomPropertyValue(property);
        DomMutation mutation = new DomMutation
        {
            Kind = UiMutationKind.SetProperty,
            TargetId = targetId,
            Property = detached,
            Name = detached.Name,
            Value = ValueJsonCodec.SerializeValue(detached.Value, MaximumValueDepth, MaximumValueCollectionCount, MaximumValueStringLength, MaximumValueBinaryLength)
        };
        AddMutation(state, mutation);
    }

    internal void AddEventMutation(DomTreeDiffState state, UiMutationKind kind, string targetId, DomEventSubscription subscription)
    {
        DomEventSubscription detached = CloneDomSubscriptionValue(subscription);
        DomMutation mutation = new DomMutation
        {
            Kind = kind,
            TargetId = targetId,
            EventSubscription = detached,
            Name = detached.Id
        };
        AddMutation(state, mutation);
    }

    internal bool IsSpecialAttribute(DomNode node, DomAttribute attribute)
    {
        if (attribute.NamespaceUri != null)
        {
            return false;
        }

        if (string.Equals(attribute.Name, HtmlAttributes.Class, StringComparison.Ordinal) || string.Equals(attribute.Name, HtmlAttributes.Style, StringComparison.Ordinal))
        {
            return true;
        }

        return IsReflectedPropertyAttribute(node, attribute.Name);
    }

    internal bool IsReflectedPropertyAttribute(DomNode node, string name)
    {
        if (node.NamespaceUri != null)
        {
            return false;
        }

        if (string.Equals(node.Name, HtmlElements.Input, StringComparison.Ordinal))
        {
            return string.Equals(name, HtmlAttributes.Value, StringComparison.Ordinal) || string.Equals(name, HtmlAttributes.Checked, StringComparison.Ordinal);
        }

        if (string.Equals(node.Name, HtmlElements.Option, StringComparison.Ordinal))
        {
            return string.Equals(name, HtmlAttributes.Selected, StringComparison.Ordinal);
        }

        if (string.Equals(node.Name, HtmlElements.Details, StringComparison.Ordinal) || string.Equals(node.Name, HtmlElements.Dialog, StringComparison.Ordinal))
        {
            return string.Equals(name, HtmlAttributes.Open, StringComparison.Ordinal);
        }

        if (string.Equals(node.Name, HtmlElements.Audio, StringComparison.Ordinal) || string.Equals(node.Name, HtmlElements.Video, StringComparison.Ordinal))
        {
            return string.Equals(name, "autoplay", StringComparison.Ordinal)
                || string.Equals(name, "controls", StringComparison.Ordinal)
                || string.Equals(name, "loop", StringComparison.Ordinal)
                || string.Equals(name, "muted", StringComparison.Ordinal);
        }

        return false;
    }

    internal List<DomProperty> CreateEffectiveProperties(DomNode node)
    {
        DomProperty[] cloned = CloneDomProperties(node.Properties);
        List<DomProperty> result = new List<DomProperty>(cloned);
        if (node.NamespaceUri != null)
        {
            return result;
        }

        if (string.Equals(node.Name, HtmlElements.Input, StringComparison.Ordinal))
        {
            AddEffectiveStringProperty(result, node.Attributes, HtmlAttributes.Value);
            AddEffectiveBooleanProperty(result, node.Attributes, HtmlAttributes.Checked);
        }
        else if (string.Equals(node.Name, HtmlElements.Option, StringComparison.Ordinal))
        {
            AddEffectiveBooleanProperty(result, node.Attributes, HtmlAttributes.Selected);
        }
        else if (string.Equals(node.Name, HtmlElements.Details, StringComparison.Ordinal) || string.Equals(node.Name, HtmlElements.Dialog, StringComparison.Ordinal))
        {
            AddEffectiveBooleanProperty(result, node.Attributes, HtmlAttributes.Open);
        }
        else if (string.Equals(node.Name, HtmlElements.Audio, StringComparison.Ordinal) || string.Equals(node.Name, HtmlElements.Video, StringComparison.Ordinal))
        {
            AddEffectiveBooleanProperty(result, node.Attributes, "autoplay");
            AddEffectiveBooleanProperty(result, node.Attributes, "controls");
            AddEffectiveBooleanProperty(result, node.Attributes, "loop");
            AddEffectiveBooleanProperty(result, node.Attributes, "muted");
        }

        return result;
    }

    internal void AddEffectiveStringProperty(List<DomProperty> properties, DomAttribute[]? attributes, string name)
    {
        if (FindDomPropertyIndex(properties, name) >= 0)
        {
            return;
        }

        DomAttribute? attribute = FindHtmlAttribute(attributes ?? Array.Empty<DomAttribute>(), name);
        string value = attribute == null ? string.Empty : attribute.Value;
        properties.Add(new DomProperty
        {
            Name = name,
            Value = new UiValue { Kind = UiValueKind.String, StringValue = value }
        });
    }

    internal void AddEffectiveBooleanProperty(List<DomProperty> properties, DomAttribute[]? attributes, string name)
    {
        if (FindDomPropertyIndex(properties, name) >= 0)
        {
            return;
        }

        bool value = FindHtmlAttribute(attributes ?? Array.Empty<DomAttribute>(), name) != null;
        properties.Add(new DomProperty
        {
            Name = name,
            Value = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = value }
        });
    }

    internal DomAttribute? FindHtmlAttribute(DomAttribute[] attributes, string name)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            DomAttribute? attribute = attributes[index];
            if (attribute == null)
            {
                throw new ArgumentException("DOM attribute collections cannot contain null.", "attributes");
            }

            if (attribute.NamespaceUri == null && string.Equals(attribute.Name, name, StringComparison.Ordinal))
            {
                return attribute;
            }

            index++;
        }

        return null;
    }

    internal int FindDomAttributeIndex(DomAttribute[] attributes, string? namespaceUri, string name)
    {
        int index = 0;
        while (index < attributes.Length)
        {
            DomAttribute attribute = attributes[index];
            if (string.Equals(attribute.NamespaceUri, namespaceUri, StringComparison.Ordinal) && string.Equals(attribute.Name, name, StringComparison.Ordinal))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal int FindDomPropertyIndex(List<DomProperty> properties, string name)
    {
        int index = 0;
        while (index < properties.Count)
        {
            if (string.Equals(properties[index].Name, name, StringComparison.Ordinal))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal int FindDomSubscriptionIndex(DomEventSubscription[] subscriptions, string id)
    {
        int index = 0;
        while (index < subscriptions.Length)
        {
            if (string.Equals(subscriptions[index].Id, id, StringComparison.Ordinal))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal bool DomAttributesEqual(DomAttribute left, DomAttribute right)
    {
        return string.Equals(left.NamespaceUri, right.NamespaceUri, StringComparison.Ordinal)
            && string.Equals(left.Prefix, right.Prefix, StringComparison.Ordinal)
            && string.Equals(left.Name, right.Name, StringComparison.Ordinal)
            && (left.IsBoolean || string.Equals(left.Value, right.Value, StringComparison.Ordinal))
            && left.IsBoolean == right.IsBoolean;
    }

    internal bool DomPropertyValuesEqual(DomProperty left, DomProperty right)
    {
        return ValueJsonCodec.ValuesEqual(left.Value, right.Value, MaximumValueDepth, MaximumValueCollectionCount, MaximumValueStringLength, MaximumValueBinaryLength);
    }

    internal bool DomSubscriptionsEqual(DomEventSubscription left, DomEventSubscription right)
    {
        return string.Equals(left.Id, right.Id, StringComparison.Ordinal)
            && string.Equals(left.HandlerId, right.HandlerId, StringComparison.Ordinal)
            && left.Kind == right.Kind
            && string.Equals(left.EventName, right.EventName, StringComparison.Ordinal)
            && left.Phase == right.Phase
            && left.IsDelegated == right.IsDelegated
            && left.PreventDefault == right.PreventDefault
            && left.StopPropagation == right.StopPropagation
            && left.Once == right.Once
            && left.Passive == right.Passive
            && left.HasDebounceMilliseconds == right.HasDebounceMilliseconds
            && (!left.HasDebounceMilliseconds || left.DebounceMilliseconds == right.DebounceMilliseconds)
            && left.HasThrottleMilliseconds == right.HasThrottleMilliseconds
            && (!left.HasThrottleMilliseconds || left.ThrottleMilliseconds == right.ThrottleMilliseconds);
    }

    internal void SortDomAttributes(DomAttribute[] attributes)
    {
        int index = 1;
        while (index < attributes.Length)
        {
            DomAttribute candidate = attributes[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && CompareDomAttributes(attributes[insertionIndex], candidate) > 0)
            {
                attributes[insertionIndex + 1] = attributes[insertionIndex];
                insertionIndex--;
            }

            attributes[insertionIndex + 1] = candidate;
            index++;
        }
    }

    internal int CompareDomAttributes(DomAttribute left, DomAttribute right)
    {
        int namespaceResult = string.CompareOrdinal(left.NamespaceUri ?? string.Empty, right.NamespaceUri ?? string.Empty);
        if (namespaceResult != 0)
        {
            return namespaceResult;
        }

        int nameResult = string.CompareOrdinal(left.Name, right.Name);
        if (nameResult != 0)
        {
            return nameResult;
        }

        return string.CompareOrdinal(left.Prefix ?? string.Empty, right.Prefix ?? string.Empty);
    }

    internal void SortDomProperties(List<DomProperty> properties)
    {
        int index = 1;
        while (index < properties.Count)
        {
            DomProperty candidate = properties[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && string.CompareOrdinal(properties[insertionIndex].Name, candidate.Name) > 0)
            {
                properties[insertionIndex + 1] = properties[insertionIndex];
                insertionIndex--;
            }

            properties[insertionIndex + 1] = candidate;
            index++;
        }
    }

    internal void SortDomSubscriptions(DomEventSubscription[] subscriptions)
    {
        int index = 1;
        while (index < subscriptions.Length)
        {
            DomEventSubscription candidate = subscriptions[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && string.CompareOrdinal(subscriptions[insertionIndex].Id, candidate.Id) > 0)
            {
                subscriptions[insertionIndex + 1] = subscriptions[insertionIndex];
                insertionIndex--;
            }

            subscriptions[insertionIndex + 1] = candidate;
            index++;
        }
    }

    internal List<string> ParseClassTokens(string value)
    {
        List<string> result = new List<string>();
        StringBuilder token = new StringBuilder();
        int index = 0;
        while (index <= value.Length)
        {
            bool atEnd = index == value.Length;
            char character = atEnd ? ' ' : value[index];
            if (atEnd || IsHtmlWhiteSpace(character))
            {
                if (token.Length > 0)
                {
                    string candidate = token.ToString();
                    if (!ContainsOrdinalString(result, candidate))
                    {
                        result.Add(candidate);
                    }

                    token.Clear();
                }
            }
            else
            {
                token.Append(character);
            }

            index++;
        }

        result.Sort(StringComparer.Ordinal);
        return result;
    }

    internal bool IsHtmlWhiteSpace(char value)
    {
        return value == ' ' || value == '\t' || value == '\n' || value == '\f' || value == '\r';
    }

    internal bool ContainsOrdinalString(List<string> values, string candidate)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (string.Equals(values[index], candidate, StringComparison.Ordinal))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal List<DomStyleDeclaration> ParseStyleDeclarations(string value)
    {
        List<DomStyleDeclaration> result = new List<DomStyleDeclaration>();
        StringBuilder segment = new StringBuilder();
        char quote = '\0';
        bool escaped = false;
        int parenthesisDepth = 0;
        int bracketDepth = 0;
        int braceDepth = 0;
        int index = 0;
        while (index <= value.Length)
        {
            bool atEnd = index == value.Length;
            char character = atEnd ? ';' : value[index];
            if (!atEnd && escaped)
            {
                segment.Append(character);
                escaped = false;
            }
            else if (!atEnd && quote != '\0')
            {
                segment.Append(character);
                if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == quote)
                {
                    quote = '\0';
                }
            }
            else if (!atEnd && (character == '\'' || character == '"'))
            {
                quote = character;
                segment.Append(character);
            }
            else if (!atEnd && character == '(')
            {
                parenthesisDepth++;
                segment.Append(character);
            }
            else if (!atEnd && character == ')' && parenthesisDepth > 0)
            {
                parenthesisDepth--;
                segment.Append(character);
            }
            else if (!atEnd && character == '[')
            {
                bracketDepth++;
                segment.Append(character);
            }
            else if (!atEnd && character == ']' && bracketDepth > 0)
            {
                bracketDepth--;
                segment.Append(character);
            }
            else if (!atEnd && character == '{')
            {
                braceDepth++;
                segment.Append(character);
            }
            else if (!atEnd && character == '}' && braceDepth > 0)
            {
                braceDepth--;
                segment.Append(character);
            }
            else if (character == ';' && parenthesisDepth == 0 && bracketDepth == 0 && braceDepth == 0)
            {
                AddStyleDeclarationSegment(result, segment.ToString());
                segment.Clear();
            }
            else
            {
                segment.Append(character);
            }

            index++;
        }

        SortStyleDeclarations(result);
        return result;
    }

    internal void AddStyleDeclarationSegment(List<DomStyleDeclaration> declarations, string segment)
    {
        int separatorIndex = FindStyleValueSeparator(segment);
        if (separatorIndex < 0)
        {
            return;
        }

        string name = segment.Substring(0, separatorIndex).Trim();
        if (name.Length == 0)
        {
            return;
        }

        string value = segment.Substring(separatorIndex + 1).Trim();
        int existingIndex = FindStyleDeclarationIndex(declarations, name);
        DomStyleDeclaration declaration = new DomStyleDeclaration { Name = name, Value = value };
        if (existingIndex >= 0)
        {
            declarations[existingIndex] = declaration;
        }
        else
        {
            declarations.Add(declaration);
        }
    }

    internal int FindStyleValueSeparator(string segment)
    {
        char quote = '\0';
        bool escaped = false;
        int parenthesisDepth = 0;
        int bracketDepth = 0;
        int braceDepth = 0;
        int index = 0;
        while (index < segment.Length)
        {
            char character = segment[index];
            if (escaped)
            {
                escaped = false;
            }
            else if (quote != '\0')
            {
                if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == quote)
                {
                    quote = '\0';
                }
            }
            else if (character == '\'' || character == '"')
            {
                quote = character;
            }
            else if (character == '(')
            {
                parenthesisDepth++;
            }
            else if (character == ')' && parenthesisDepth > 0)
            {
                parenthesisDepth--;
            }
            else if (character == '[')
            {
                bracketDepth++;
            }
            else if (character == ']' && bracketDepth > 0)
            {
                bracketDepth--;
            }
            else if (character == '{')
            {
                braceDepth++;
            }
            else if (character == '}' && braceDepth > 0)
            {
                braceDepth--;
            }
            else if (character == ':' && parenthesisDepth == 0 && bracketDepth == 0 && braceDepth == 0)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal int FindStyleDeclarationIndex(List<DomStyleDeclaration> declarations, string name)
    {
        int index = 0;
        while (index < declarations.Count)
        {
            if (string.Equals(declarations[index].Name, name, StringComparison.Ordinal))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal void SortStyleDeclarations(List<DomStyleDeclaration> declarations)
    {
        int index = 1;
        while (index < declarations.Count)
        {
            DomStyleDeclaration candidate = declarations[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && string.CompareOrdinal(declarations[insertionIndex].Name, candidate.Name) > 0)
            {
                declarations[insertionIndex + 1] = declarations[insertionIndex];
                insertionIndex--;
            }

            declarations[insertionIndex + 1] = candidate;
            index++;
        }
    }

    internal DomAttribute CloneDomAttributeValue(DomAttribute source)
    {
        return new DomAttribute
        {
            NamespaceUri = source.NamespaceUri,
            Prefix = source.Prefix,
            Name = source.Name,
            Value = source.Value,
            IsBoolean = source.IsBoolean
        };
    }

    internal DomProperty CloneDomPropertyValue(DomProperty source)
    {
        return new DomProperty
        {
            Name = source.Name,
            Value = ValueJsonCodec.CloneValue(source.Value, MaximumValueDepth, MaximumValueCollectionCount, MaximumValueStringLength, MaximumValueBinaryLength)
        };
    }

    internal DomEventSubscription CloneDomSubscriptionValue(DomEventSubscription source)
    {
        return new DomEventSubscription
        {
            Id = source.Id,
            HandlerId = source.HandlerId,
            Kind = source.Kind,
            EventName = source.EventName,
            Phase = source.Phase,
            IsDelegated = source.IsDelegated,
            PreventDefault = source.PreventDefault,
            StopPropagation = source.StopPropagation,
            Once = source.Once,
            Passive = source.Passive,
            HasDebounceMilliseconds = source.HasDebounceMilliseconds,
            DebounceMilliseconds = source.DebounceMilliseconds,
            HasThrottleMilliseconds = source.HasThrottleMilliseconds,
            ThrottleMilliseconds = source.ThrottleMilliseconds
        };
    }

    internal void AddMutation(DomTreeDiffState state, DomMutation mutation)
    {
        mutation.Sequence = state.Mutations.Count;
        state.Mutations.Add(mutation);
    }

    internal DomNode CloneDomNode(DomNode source)
    {
        DomNode result = new DomNode
        {
            Sequence = source.Sequence,
            Kind = source.Kind,
            Key = source.Key,
            Id = source.Id,
            NamespaceUri = source.NamespaceUri,
            Name = source.Name,
            Text = source.Text,
            RawContentPolicyId = source.RawContentPolicyId,
            Attributes = CloneDomAttributes(source.Attributes),
            Properties = CloneDomProperties(source.Properties),
            EventSubscriptions = CloneDomSubscriptions(source.EventSubscriptions),
            StyleScope = source.StyleScope,
            HydrationMarker = source.HydrationMarker,
            Version = source.Version,
            SubtreeHash = source.SubtreeHash
        };
        DomNode[] children = source.Children ?? Array.Empty<DomNode>();
        result.Children = new DomNode[children.Length];
        int index = 0;
        while (index < children.Length)
        {
            result.Children[index] = CloneDomNode(RequireNode(children[index], "source"));
            index++;
        }

        return result;
    }

    internal DomAttribute[] CloneDomAttributes(DomAttribute[]? source)
    {
        DomAttribute[] attributes = source ?? Array.Empty<DomAttribute>();
        DomAttribute[] result = new DomAttribute[attributes.Length];
        int index = 0;
        while (index < attributes.Length)
        {
            DomAttribute? attribute = attributes[index];
            if (attribute == null)
            {
                throw new ArgumentException("DOM attribute collections cannot contain null.", "source");
            }

            result[index] = new DomAttribute
            {
                NamespaceUri = attribute.NamespaceUri,
                Prefix = attribute.Prefix,
                Name = attribute.Name,
                Value = attribute.Value,
                IsBoolean = attribute.IsBoolean
            };
            index++;
        }

        return result;
    }

    internal DomProperty[] CloneDomProperties(DomProperty[]? source)
    {
        DomProperty[] properties = source ?? Array.Empty<DomProperty>();
        DomProperty[] result = new DomProperty[properties.Length];
        int index = 0;
        while (index < properties.Length)
        {
            DomProperty? property = properties[index];
            if (property == null)
            {
                throw new ArgumentException("DOM property collections cannot contain null.", "source");
            }

            result[index] = new DomProperty
            {
                Name = property.Name,
                Value = ValueJsonCodec.CloneValue(property.Value, MaximumValueDepth, MaximumValueCollectionCount, MaximumValueStringLength, MaximumValueBinaryLength)
            };
            index++;
        }

        return result;
    }

    internal DomEventSubscription[] CloneDomSubscriptions(DomEventSubscription[]? source)
    {
        DomEventSubscription[] subscriptions = source ?? Array.Empty<DomEventSubscription>();
        DomEventSubscription[] result = new DomEventSubscription[subscriptions.Length];
        int index = 0;
        while (index < subscriptions.Length)
        {
            DomEventSubscription? subscription = subscriptions[index];
            if (subscription == null)
            {
                throw new ArgumentException("DOM event subscription collections cannot contain null.", "source");
            }

            result[index] = new DomEventSubscription
            {
                Id = subscription.Id,
                HandlerId = subscription.HandlerId,
                Kind = subscription.Kind,
                EventName = subscription.EventName,
                Phase = subscription.Phase,
                IsDelegated = subscription.IsDelegated,
                PreventDefault = subscription.PreventDefault,
                StopPropagation = subscription.StopPropagation,
                Once = subscription.Once,
                Passive = subscription.Passive,
                HasDebounceMilliseconds = subscription.HasDebounceMilliseconds,
                DebounceMilliseconds = subscription.DebounceMilliseconds,
                HasThrottleMilliseconds = subscription.HasThrottleMilliseconds,
                ThrottleMilliseconds = subscription.ThrottleMilliseconds
            };
            index++;
        }

        return result;
    }
}
