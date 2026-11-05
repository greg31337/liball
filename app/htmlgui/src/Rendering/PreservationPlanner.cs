using System;
using System.Collections.Generic;

namespace htmlgui;

internal class PreservationPlanner
{
    internal const int MaximumValueDepth = 64;
    internal const int MaximumValueCollectionCount = 10000;
    internal const int MaximumValueStringLength = 1048576;
    internal const int MaximumValueBinaryLength = 1048576;

    internal UiPreservationState CreatePreservationPlan(DomSnapshot? before, DomSnapshot? after, DomMutation[]? mutations, UiPreservationState? current)
    {
        if (before == null)
        {
            throw new ArgumentNullException("before");
        }

        if (after == null)
        {
            throw new ArgumentNullException("after");
        }

        if (mutations == null)
        {
            throw new ArgumentNullException("mutations");
        }

        if (current == null)
        {
            throw new ArgumentNullException("current");
        }

        ValidatePreservationMutations(mutations);
        UiPreservationState result = CopyPreservationState(current);
        string? focusedId = current.FocusedNodeId;
        DomNode? beforeFocus = FindSnapshotNode(before, focusedId);
        DomNode? afterFocus = FindSnapshotNode(after, focusedId);
        bool focusRemoved = IsNodeRemovedByPatch(before, mutations, focusedId);
        result.PreserveFocus = focusedId != null && beforeFocus != null && afterFocus != null && !focusRemoved;
        if (!result.PreserveFocus)
        {
            result.FocusedNodeId = null;
        }

        bool compatibleSelection = result.PreserveFocus && beforeFocus != null && afterFocus != null && AreSelectionHostsCompatible(beforeFocus, afterFocus);
        bool validSelection = current.HasSelection && current.SelectionStart >= 0 && current.SelectionEnd >= current.SelectionStart;
        result.PreserveSelection = compatibleSelection && validSelection;
        result.HasSelection = result.PreserveSelection;
        if (!result.PreserveSelection)
        {
            result.SelectionStart = 0;
            result.SelectionEnd = 0;
        }

        bool compositionReplaced = IsNodeReplacedByPatch(before, mutations, focusedId);
        bool compositionValueChanged = focusedId != null
            && (PatchControlsNamedState(mutations, focusedId, new string[] { HtmlAttributes.Value, HtmlAttributes.Checked, HtmlAttributes.Selected })
                || (beforeFocus != null && afterFocus != null && NamedDomStateChanged(beforeFocus, afterFocus, new string[] { HtmlAttributes.Value, HtmlAttributes.Checked, HtmlAttributes.Selected })));
        result.PreserveComposition = result.PreserveSelection && current.IsComposing && !compositionReplaced && !compositionValueChanged;
        result.IsComposing = result.PreserveComposition;
        result.ScrollOffsets = FilterScrollOffsets(after, current.ScrollOffsets);
        result.PreserveScroll = result.ScrollOffsets.Length > 0;
        result.ScrollAnchorNodeId = CanPreserveScrollAnchor(before, after, current.ScrollAnchorNodeId) ? current.ScrollAnchorNodeId : null;
        result.PreserveScrollAnchor = result.ScrollAnchorNodeId != null;
        result.MediaPlaybackStates = FilterMediaPlaybackStates(before, after, mutations, current.MediaPlaybackStates);
        result.PreserveMediaPlayback = result.MediaPlaybackStates.Length > 0;
        result.OpenOverlayIds = FilterOpenOverlayIdentifiers(before, after, mutations, current.OpenOverlayIds);
        result.PreserveOpenOverlays = result.OpenOverlayIds.Length > 0;
        result.UserEditedFieldValues = FilterUserEditedFieldValues(before, after, mutations, current.UserEditedFieldValues);
        result.PreserveUserEditedFields = result.UserEditedFieldValues.Length > 0;
        return result;
    }

    internal void ValidatePreservationMutations(DomMutation[] mutations)
    {
        int index = 0;
        while (index < mutations.Length)
        {
            DomMutation? mutation = mutations[index];
            if (mutation == null)
            {
                throw new ArgumentException("The patch mutation collection cannot contain null.", "mutations");
            }

            switch (mutation.Kind)
            {
                case UiMutationKind.Insert:
                case UiMutationKind.Remove:
                case UiMutationKind.Move:
                case UiMutationKind.Replace:
                case UiMutationKind.SetText:
                case UiMutationKind.SetAttribute:
                case UiMutationKind.RemoveAttribute:
                case UiMutationKind.SetProperty:
                case UiMutationKind.RemoveProperty:
                case UiMutationKind.AddClass:
                case UiMutationKind.RemoveClass:
                case UiMutationKind.SetStyle:
                case UiMutationKind.RemoveStyle:
                case UiMutationKind.SubscribeEvent:
                case UiMutationKind.UnsubscribeEvent:
                    break;
                default:
                    throw new ArgumentOutOfRangeException("mutations", mutation.Kind, "The patch mutation kind is not supported.");
            }

            if ((mutation.Kind == UiMutationKind.Remove || mutation.Kind == UiMutationKind.Move || mutation.Kind == UiMutationKind.Replace) && string.IsNullOrWhiteSpace(mutation.TargetId))
            {
                throw new ArgumentException("Structural patch mutations require a target identifier.", "mutations");
            }

            index++;
        }
    }

    internal bool IsNodeRemovedByPatch(DomSnapshot before, DomMutation[] mutations, string? nodeId)
    {
        int index = 0;
        while (nodeId != null && index < mutations.Length)
        {
            DomMutation mutation = mutations[index];
            if (mutation.Kind == UiMutationKind.Remove && MutationContainsNode(before, mutation.TargetId, nodeId))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal bool IsNodeReplacedByPatch(DomSnapshot before, DomMutation[] mutations, string? nodeId)
    {
        int index = 0;
        while (nodeId != null && index < mutations.Length)
        {
            DomMutation mutation = mutations[index];
            if (mutation.Kind == UiMutationKind.Replace && MutationContainsNode(before, mutation.TargetId, nodeId))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal bool MutationContainsNode(DomSnapshot snapshot, string targetId, string nodeId)
    {
        DomNode? target = FindSnapshotNode(snapshot, targetId);
        if (target == null)
        {
            return false;
        }

        return FindDescendantNode(target, nodeId, 0) != null;
    }

    internal DomNode? FindSnapshotNode(DomSnapshot snapshot, string? nodeId)
    {
        DomNode[]? roots = snapshot.Roots;
        int index = 0;
        while (nodeId != null && roots != null && index < roots.Length)
        {
            DomNode? root = roots[index];
            if (root != null)
            {
                DomNode? found = FindDescendantNode(root, nodeId, 0);
                if (found != null)
                {
                    return found;
                }
            }

            index++;
        }

        return null;
    }

    internal DomNode? FindDescendantNode(DomNode node, string nodeId, int depth)
    {
        if (node.Id == nodeId)
        {
            return node;
        }

        if (depth > 1024)
        {
            return null;
        }

        DomNode[]? children = node.Children;
        int index = 0;
        while (children != null && index < children.Length)
        {
            DomNode? child = children[index];
            if (child != null)
            {
                DomNode? found = FindDescendantNode(child, nodeId, depth + 1);
                if (found != null)
                {
                    return found;
                }
            }

            index++;
        }

        return null;
    }

    internal bool AreSelectionHostsCompatible(DomNode before, DomNode after)
    {
        if (before.Kind != DomNodeKind.Element || after.Kind != DomNodeKind.Element || before.Name != after.Name)
        {
            return false;
        }

        if (before.Name == HtmlElements.TextArea)
        {
            return true;
        }

        if (before.Name == HtmlElements.Input)
        {
            string beforeType = GetDomAttributeValue(before, HtmlAttributes.Type) ?? HtmlInputTypes.Text;
            string afterType = GetDomAttributeValue(after, HtmlAttributes.Type) ?? HtmlInputTypes.Text;
            return beforeType == afterType && SupportsTextSelection(beforeType);
        }

        string? beforeEditable = GetDomAttributeValue(before, "contenteditable");
        string? afterEditable = GetDomAttributeValue(after, "contenteditable");
        return beforeEditable == "true" && afterEditable == "true";
    }

    internal bool SupportsTextSelection(string inputType)
    {
        switch (inputType)
        {
            case HtmlInputTypes.Text:
            case HtmlInputTypes.Search:
            case HtmlInputTypes.Email:
            case HtmlInputTypes.Url:
            case HtmlInputTypes.Telephone:
            case HtmlInputTypes.Password:
                return true;
            default:
                return false;
        }
    }

    internal string? GetDomAttributeValue(DomNode node, string name)
    {
        DomAttribute[]? attributes = node.Attributes;
        int index = 0;
        while (attributes != null && index < attributes.Length)
        {
            DomAttribute? attribute = attributes[index];
            if (attribute != null && attribute.Name == name)
            {
                return attribute.Value;
            }

            index++;
        }

        return null;
    }

    internal UiValueEntry[] FilterScrollOffsets(DomSnapshot after, UiValueEntry[]? offsets)
    {
        if (offsets == null)
        {
            return Array.Empty<UiValueEntry>();
        }

        List<UiValueEntry> result = new List<UiValueEntry>();
        int index = 0;
        while (index < offsets.Length)
        {
            UiValueEntry? entry = offsets[index];
            if (entry != null && FindSnapshotNode(after, entry.Key) != null)
            {
                result.Add(CopyPreservationEntry(entry));
            }

            index++;
        }

        return result.ToArray();
    }

    internal bool CanPreserveScrollAnchor(DomSnapshot before, DomSnapshot after, string? nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return false;
        }

        return FindSnapshotNode(before, nodeId) != null && FindSnapshotNode(after, nodeId) != null;
    }

    internal UiValueEntry[] FilterMediaPlaybackStates(DomSnapshot before, DomSnapshot after, DomMutation[] mutations, UiValueEntry[]? states)
    {
        if (states == null)
        {
            return Array.Empty<UiValueEntry>();
        }

        List<UiValueEntry> result = new List<UiValueEntry>();
        int index = 0;
        while (index < states.Length)
        {
            UiValueEntry? entry = states[index];
            if (entry != null && !ContainsPreservationEntry(result, entry.Key))
            {
                DomNode? beforeNode = FindSnapshotNode(before, entry.Key);
                DomNode? afterNode = FindSnapshotNode(after, entry.Key);
                if (beforeNode != null
                    && afterNode != null
                    && AreMediaHostsCompatible(beforeNode, afterNode)
                    && !PatchControlsNamedState(mutations, entry.Key, new string[] { "currentTime", "paused", "volume", "muted", "playbackRate", "defaultPlaybackRate" })
                    && !NamedDomStateChanged(beforeNode, afterNode, new string[] { "currentTime", "paused", "volume", "muted", "playbackRate", "defaultPlaybackRate" }))
                {
                    result.Add(CopyPreservationEntry(entry));
                }
            }

            index++;
        }

        return result.ToArray();
    }

    internal UiValueEntry[] FilterUserEditedFieldValues(DomSnapshot before, DomSnapshot after, DomMutation[] mutations, UiValueEntry[]? values)
    {
        if (values == null)
        {
            return Array.Empty<UiValueEntry>();
        }

        List<UiValueEntry> result = new List<UiValueEntry>();
        int index = 0;
        while (index < values.Length)
        {
            UiValueEntry? entry = values[index];
            if (entry != null && !ContainsPreservationEntry(result, entry.Key))
            {
                DomNode? beforeNode = FindSnapshotNode(before, entry.Key);
                DomNode? afterNode = FindSnapshotNode(after, entry.Key);
                if (beforeNode != null
                    && afterNode != null
                    && AreControlledFieldHostsCompatible(beforeNode, afterNode)
                    && !PatchControlsNamedState(mutations, entry.Key, new string[] { HtmlAttributes.Value, HtmlAttributes.Checked, HtmlAttributes.Selected, "selectedIndex" })
                    && !NamedDomStateChanged(beforeNode, afterNode, new string[] { HtmlAttributes.Value, HtmlAttributes.Checked, HtmlAttributes.Selected, "selectedIndex" }))
                {
                    result.Add(CopyPreservationEntry(entry));
                }
            }

            index++;
        }

        return result.ToArray();
    }

    internal string[] FilterOpenOverlayIdentifiers(DomSnapshot before, DomSnapshot after, DomMutation[] mutations, string[]? identifiers)
    {
        if (identifiers == null)
        {
            return Array.Empty<string>();
        }

        List<string> result = new List<string>();
        int index = 0;
        while (index < identifiers.Length)
        {
            string? identifier = identifiers[index];
            if (!string.IsNullOrWhiteSpace(identifier) && !ContainsPreservationIdentifier(result, identifier))
            {
                DomNode? beforeNode = FindSnapshotNode(before, identifier);
                DomNode? afterNode = FindSnapshotNode(after, identifier);
                bool removedKnownNode = beforeNode != null && afterNode == null;
                bool incompatibleKnownNode = beforeNode != null && afterNode != null && (beforeNode.Kind != afterNode.Kind || !string.Equals(beforeNode.Name, afterNode.Name, StringComparison.Ordinal));
                bool controlledByPatch = PatchControlsNamedState(mutations, identifier, new string[] { HtmlAttributes.Open });
                bool modelStateChanged = beforeNode != null && afterNode != null && NamedDomStateChanged(beforeNode, afterNode, new string[] { HtmlAttributes.Open });
                if (!removedKnownNode && !incompatibleKnownNode && !controlledByPatch && !modelStateChanged)
                {
                    result.Add(identifier);
                }
            }

            index++;
        }

        return result.ToArray();
    }

    internal bool AreMediaHostsCompatible(DomNode before, DomNode after)
    {
        if (before.Kind != DomNodeKind.Element || after.Kind != DomNodeKind.Element || !string.Equals(before.Name, after.Name, StringComparison.Ordinal))
        {
            return false;
        }

        bool isMedia = string.Equals(before.Name, HtmlElements.Audio, StringComparison.Ordinal) || string.Equals(before.Name, HtmlElements.Video, StringComparison.Ordinal);
        if (!isMedia)
        {
            return false;
        }

        return string.Equals(GetDomAttributeValue(before, HtmlAttributes.Source), GetDomAttributeValue(after, HtmlAttributes.Source), StringComparison.Ordinal);
    }

    internal bool AreControlledFieldHostsCompatible(DomNode before, DomNode after)
    {
        if (before.Kind != DomNodeKind.Element || after.Kind != DomNodeKind.Element || !string.Equals(before.Name, after.Name, StringComparison.Ordinal))
        {
            return false;
        }

        if (string.Equals(before.Name, HtmlElements.Input, StringComparison.Ordinal))
        {
            string beforeType = GetDomAttributeValue(before, HtmlAttributes.Type) ?? HtmlInputTypes.Text;
            string afterType = GetDomAttributeValue(after, HtmlAttributes.Type) ?? HtmlInputTypes.Text;
            return string.Equals(beforeType, afterType, StringComparison.Ordinal);
        }

        return string.Equals(before.Name, HtmlElements.TextArea, StringComparison.Ordinal) || string.Equals(before.Name, HtmlElements.Select, StringComparison.Ordinal);
    }

    internal bool PatchControlsNamedState(DomMutation[] mutations, string targetId, string[] names)
    {
        int index = 0;
        while (index < mutations.Length)
        {
            DomMutation mutation = mutations[index];
            if (string.Equals(mutation.TargetId, targetId, StringComparison.Ordinal)
                && (mutation.Kind == UiMutationKind.SetProperty
                    || mutation.Kind == UiMutationKind.RemoveProperty
                    || mutation.Kind == UiMutationKind.SetAttribute
                    || mutation.Kind == UiMutationKind.RemoveAttribute)
                && ArrayContainsPreservationIdentifier(names, mutation.Name))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal bool NamedDomStateChanged(DomNode before, DomNode after, string[] names)
    {
        DomTreeDiffer differ = new DomTreeDiffer();
        List<DomProperty> beforeProperties = differ.CreateEffectiveProperties(before);
        List<DomProperty> afterProperties = differ.CreateEffectiveProperties(after);
        int nameIndex = 0;
        while (nameIndex < names.Length)
        {
            string name = names[nameIndex];
            int beforeIndex = differ.FindDomPropertyIndex(beforeProperties, name);
            int afterIndex = differ.FindDomPropertyIndex(afterProperties, name);
            if (beforeIndex != afterIndex && (beforeIndex < 0 || afterIndex < 0))
            {
                return true;
            }

            if (beforeIndex >= 0 && afterIndex >= 0 && !differ.DomPropertyValuesEqual(beforeProperties[beforeIndex], afterProperties[afterIndex]))
            {
                return true;
            }

            nameIndex++;
        }

        return false;
    }

    internal bool ContainsPreservationEntry(List<UiValueEntry> entries, string key)
    {
        int index = 0;
        while (index < entries.Count)
        {
            if (string.Equals(entries[index].Key, key, StringComparison.Ordinal))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal bool ContainsPreservationIdentifier(List<string> identifiers, string candidate)
    {
        int index = 0;
        while (index < identifiers.Count)
        {
            if (string.Equals(identifiers[index], candidate, StringComparison.Ordinal))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal bool ArrayContainsPreservationIdentifier(string[] identifiers, string? candidate)
    {
        int index = 0;
        while (candidate != null && index < identifiers.Length)
        {
            if (string.Equals(identifiers[index], candidate, StringComparison.Ordinal))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal UiValueEntry CopyPreservationEntry(UiValueEntry source)
    {
        return new UiValueEntry
        {
            Key = source.Key,
            Value = ValueJsonCodec.CloneValue(source.Value, MaximumValueDepth, MaximumValueCollectionCount, MaximumValueStringLength, MaximumValueBinaryLength)
        };
    }

    internal string[] CopyStringArray(string[]? values)
    {
        if (values == null)
        {
            return Array.Empty<string>();
        }

        string[] result = new string[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = values[index];
            index++;
        }

        return result;
    }

    internal UiPreservationState CopyPreservationState(UiPreservationState state)
    {
        return new UiPreservationState
        {
            PreserveFocus = state.PreserveFocus,
            PreserveSelection = state.PreserveSelection,
            PreserveComposition = state.PreserveComposition,
            PreserveScroll = state.PreserveScroll,
            FocusedNodeId = state.FocusedNodeId,
            HasSelection = state.HasSelection,
            SelectionStart = state.SelectionStart,
            SelectionEnd = state.SelectionEnd,
            IsComposing = state.IsComposing,
            ScrollOffsets = FilterScrollOffsetsWithoutSnapshot(state.ScrollOffsets),
            PreserveScrollAnchor = state.PreserveScrollAnchor,
            ScrollAnchorNodeId = state.ScrollAnchorNodeId,
            PreserveMediaPlayback = state.PreserveMediaPlayback,
            MediaPlaybackStates = CopyPreservationEntries(state.MediaPlaybackStates),
            PreserveOpenOverlays = state.PreserveOpenOverlays,
            OpenOverlayIds = CopyStringArray(state.OpenOverlayIds),
            PreserveUserEditedFields = state.PreserveUserEditedFields,
            UserEditedFieldValues = CopyPreservationEntries(state.UserEditedFieldValues)
        };
    }

    internal UiValueEntry[] FilterScrollOffsetsWithoutSnapshot(UiValueEntry[]? offsets)
    {
        if (offsets == null)
        {
            return Array.Empty<UiValueEntry>();
        }

        List<UiValueEntry> result = new List<UiValueEntry>();
        int index = 0;
        while (index < offsets.Length)
        {
            UiValueEntry? entry = offsets[index];
            if (entry != null)
            {
                result.Add(CopyPreservationEntry(entry));
            }

            index++;
        }

        return result.ToArray();
    }

    internal UiValueEntry[] CopyPreservationEntries(UiValueEntry[]? entries)
    {
        if (entries == null)
        {
            return Array.Empty<UiValueEntry>();
        }

        List<UiValueEntry> result = new List<UiValueEntry>();
        int index = 0;
        while (index < entries.Length)
        {
            UiValueEntry? entry = entries[index];
            if (entry != null)
            {
                result.Add(CopyPreservationEntry(entry));
            }

            index++;
        }

        return result.ToArray();
    }
}
