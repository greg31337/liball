using System;
using System.Collections.Generic;

namespace htmlgui;

internal class RenderTreeAnnotationResult
{
    internal bool Success;
    internal DomNode[] Roots = Array.Empty<DomNode>();
    internal UiDiagnostic[] Diagnostics = Array.Empty<UiDiagnostic>();
    internal string ContentHash = string.Empty;
}

internal class RenderTreeAnnotationState
{
    internal string RootSeed;
    internal int NextSequence;
    internal DiagnosticSink Diagnostics;
    internal List<DomNode> Ancestors;

    internal RenderTreeAnnotationState(string rootSeed, int maximumDiagnostics)
    {
        RootSeed = rootSeed;
        NextSequence = 0;
        Diagnostics = new DiagnosticSink(maximumDiagnostics);
        Ancestors = new List<DomNode>();
    }
}

internal class RenderTreeAnnotator
{
    internal RenderTreeAnnotationResult AnnotateTree(DomNode[]? roots, string? rootSeed, int maximumDiagnostics)
    {
        if (roots == null)
        {
            throw new ArgumentNullException("roots");
        }

        string seed = Guard.ArgumentNotWhiteSpace(rootSeed, "rootSeed");
        RenderTreeAnnotationState state = new RenderTreeAnnotationState(seed, maximumDiagnostics);
        DomNode[] annotatedRoots = AnnotateSiblings(roots, string.Empty, "/roots", state);
        List<string> treeParts = new List<string>();
        treeParts.Add("render-tree-v1");
        int rootIndex = 0;
        while (rootIndex < annotatedRoots.Length)
        {
            treeParts.Add(annotatedRoots[rootIndex].SubtreeHash ?? string.Empty);
            rootIndex++;
        }

        RenderTreeAnnotationResult result = new RenderTreeAnnotationResult();
        result.Success = !state.Diagnostics.HasErrors();
        result.Roots = annotatedRoots;
        result.Diagnostics = CreatePublicDiagnostics(state.Diagnostics.CreateSnapshot());
        result.ContentHash = StableHash.HashParts(treeParts.ToArray());
        return result;
    }

    internal DomNode[] AnnotateSiblings(DomNode[] source, string parentId, string location, RenderTreeAnnotationState state)
    {
        DomNode[] result = new DomNode[source.Length];
        List<string> keys = new List<string>();
        int index = 0;
        while (index < source.Length)
        {
            DomNode node = source[index];
            string nodeLocation = location + "/" + InvariantFormatter.FormatInt32(index);
            if (node == null)
            {
                state.Diagnostics.Add(DiagnosticLevel.Error, "render.null-node", "Render tree node collections cannot contain null.", nodeLocation);
                node = new DomNode();
            }

            DomNode annotated = AnnotateNode(node, parentId, index, nodeLocation, state);
            int earlierIndex = 0;
            while (earlierIndex < keys.Count)
            {
                if (string.Equals(keys[earlierIndex], annotated.Key, StringComparison.Ordinal))
                {
                    state.Diagnostics.Add(DiagnosticLevel.Error, "render.duplicate-key", "Sibling render keys must be unique.", nodeLocation + "/key");
                    break;
                }

                earlierIndex++;
            }

            keys.Add(annotated.Key);
            result[index] = annotated;
            index++;
        }

        return result;
    }

    internal DomNode AnnotateNode(DomNode source, string parentId, int siblingIndex, string location, RenderTreeAnnotationState state)
    {
        string key = source.Key;
        if (string.IsNullOrWhiteSpace(key))
        {
            key = "$" + GetNodeKindToken(source.Kind) + "-" + InvariantFormatter.FormatInt32(siblingIndex);
        }

        string id = source.Id;
        if (string.IsNullOrWhiteSpace(id))
        {
            if (parentId.Length == 0)
            {
                id = StableId.CreateRoot(state.RootSeed, GetNodeKindToken(source.Kind), key);
            }
            else
            {
                id = StableId.CreateChild(state.RootSeed, parentId, GetNodeKindToken(source.Kind), key);
            }
        }

        DomNode result = new DomNode();
        result.Sequence = state.NextSequence;
        state.NextSequence++;
        result.Kind = source.Kind;
        result.Key = key;
        result.Id = id;
        result.NamespaceUri = source.NamespaceUri;
        result.Name = source.Name;
        result.Text = source.Text;
        result.RawContentPolicyId = source.RawContentPolicyId;
        result.Attributes = CloneAttributes(source.Attributes);
        result.Properties = CloneProperties(source.Properties);
        result.EventSubscriptions = CloneEventSubscriptions(source.EventSubscriptions);
        result.StyleScope = source.StyleScope;
        result.HydrationMarker = source.HydrationMarker;
        result.Version = source.Version;
        if (source.Version < 0)
        {
            state.Diagnostics.Add(DiagnosticLevel.Error, "render.invalid-version", "Render tree versions cannot be negative.", location + "/version");
        }

        if (ContainsAncestor(source, state.Ancestors))
        {
            state.Diagnostics.Add(DiagnosticLevel.Error, "render.cycle", "The render tree cannot contain a cycle.", location);
            result.Children = Array.Empty<DomNode>();
        }
        else
        {
            state.Ancestors.Add(source);
            result.Children = AnnotateSiblings(source.Children ?? Array.Empty<DomNode>(), id, location + "/children", state);
            state.Ancestors.RemoveAt(state.Ancestors.Count - 1);
        }

        result.SubtreeHash = CreateNodeHash(result);
        return result;
    }

    internal bool ContainsAncestor(DomNode node, List<DomNode> ancestors)
    {
        int index = 0;
        while (index < ancestors.Count)
        {
            if (object.ReferenceEquals(ancestors[index], node))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal DomAttribute[] CloneAttributes(DomAttribute[]? source)
    {
        DomAttribute[] values = source ?? Array.Empty<DomAttribute>();
        DomAttribute[] result = new DomAttribute[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            DomAttribute value = values[index] ?? new DomAttribute();
            result[index] = new DomAttribute
            {
                NamespaceUri = value.NamespaceUri,
                Prefix = value.Prefix,
                Name = value.Name,
                Value = value.Value,
                IsBoolean = value.IsBoolean
            };
            index++;
        }

        SortClonedAttributes(result);
        return result;
    }

    internal DomProperty[] CloneProperties(DomProperty[]? source)
    {
        DomProperty[] values = source ?? Array.Empty<DomProperty>();
        DomProperty[] result = new DomProperty[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            DomProperty value = values[index] ?? new DomProperty();
            result[index] = new DomProperty
            {
                Name = value.Name,
                Value = CreateCanonicalValue(value.Value)
            };
            index++;
        }

        SortClonedProperties(result);
        return result;
    }

    internal UiValue CreateCanonicalValue(UiValue? value)
    {
        string json = ValueJsonCodec.SerializeValue(value, 64, 10000, 1048576, 1048576);
        return ValueJsonCodec.DeserializeValue(json, 64, 10000, 1048576, 1048576);
    }

    internal void SortClonedAttributes(DomAttribute[] values)
    {
        int index = 1;
        while (index < values.Length)
        {
            DomAttribute candidate = values[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && CompareAttributes(values[insertionIndex], candidate) > 0)
            {
                values[insertionIndex + 1] = values[insertionIndex];
                insertionIndex--;
            }

            values[insertionIndex + 1] = candidate;
            index++;
        }
    }

    internal int CompareAttributes(DomAttribute left, DomAttribute right)
    {
        int comparison = string.CompareOrdinal(left.NamespaceUri ?? string.Empty, right.NamespaceUri ?? string.Empty);
        if (comparison == 0)
        {
            comparison = string.CompareOrdinal(left.Prefix ?? string.Empty, right.Prefix ?? string.Empty);
        }

        if (comparison == 0)
        {
            comparison = string.CompareOrdinal(left.Name, right.Name);
        }

        if (comparison == 0)
        {
            comparison = left.IsBoolean.CompareTo(right.IsBoolean);
        }

        if (comparison == 0)
        {
            comparison = string.CompareOrdinal(left.Value, right.Value);
        }

        return comparison;
    }

    internal void SortClonedProperties(DomProperty[] values)
    {
        int index = 1;
        while (index < values.Length)
        {
            DomProperty candidate = values[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && string.CompareOrdinal(values[insertionIndex].Name, candidate.Name) > 0)
            {
                values[insertionIndex + 1] = values[insertionIndex];
                insertionIndex--;
            }

            values[insertionIndex + 1] = candidate;
            index++;
        }
    }

    internal DomEventSubscription[] CloneEventSubscriptions(DomEventSubscription[]? source)
    {
        DomEventSubscription[] values = source ?? Array.Empty<DomEventSubscription>();
        DomEventSubscription[] result = new DomEventSubscription[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            DomEventSubscription value = values[index] ?? new DomEventSubscription();
            result[index] = new DomEventSubscription
            {
                Id = value.Id,
                HandlerId = value.HandlerId,
                Kind = value.Kind,
                EventName = value.EventName,
                Phase = value.Phase,
                IsDelegated = value.IsDelegated,
                PreventDefault = value.PreventDefault,
                StopPropagation = value.StopPropagation,
                Once = value.Once,
                Passive = value.Passive,
                HasDebounceMilliseconds = value.HasDebounceMilliseconds,
                DebounceMilliseconds = value.DebounceMilliseconds,
                HasThrottleMilliseconds = value.HasThrottleMilliseconds,
                ThrottleMilliseconds = value.ThrottleMilliseconds
            };
            index++;
        }

        return result;
    }

    internal string CreateNodeHash(DomNode node)
    {
        List<string> parts = new List<string>();
        parts.Add("render-node-v1");
        parts.Add(GetNodeKindToken(node.Kind));
        parts.Add(node.Key);
        parts.Add(node.Id);
        AddOptionalText(parts, node.NamespaceUri);
        parts.Add(node.Name);
        AddOptionalText(parts, node.Text);
        AddOptionalText(parts, node.RawContentPolicyId);
        AddOptionalText(parts, node.StyleScope);
        AddOptionalText(parts, node.HydrationMarker);
        parts.Add(InvariantFormatter.FormatInt32(node.Version));
        AddAttributeParts(parts, node.Attributes);
        AddPropertyParts(parts, node.Properties);
        AddEventParts(parts, node.EventSubscriptions);
        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            parts.Add(node.Children[childIndex].SubtreeHash ?? string.Empty);
            childIndex++;
        }

        return StableHash.HashParts(parts.ToArray());
    }

    internal void AddOptionalText(List<string> parts, string? value)
    {
        if (value == null)
        {
            parts.Add("0");
        }
        else
        {
            parts.Add("1");
            parts.Add(value);
        }
    }

    internal void AddAttributeParts(List<string> parts, DomAttribute[] attributes)
    {
        string[] values = new string[attributes.Length];
        int index = 0;
        while (index < attributes.Length)
        {
            DomAttribute attribute = attributes[index];
            values[index] = StableHash.HashParts(new string[]
            {
                attribute.NamespaceUri ?? string.Empty,
                attribute.Prefix ?? string.Empty,
                attribute.Name,
                attribute.Value,
                InvariantFormatter.FormatBoolean(attribute.IsBoolean)
            });
            index++;
        }

        parts.Add(InvariantFormatter.FormatInt32(values.Length));
        index = 0;
        while (index < values.Length)
        {
            parts.Add(values[index]);
            index++;
        }
    }

    internal void AddPropertyParts(List<string> parts, DomProperty[] properties)
    {
        string[] values = new string[properties.Length];
        int index = 0;
        while (index < properties.Length)
        {
            DomProperty property = properties[index];
            string json = ValueJsonCodec.SerializeValue(property.Value, 64, 10000, 1048576, 1048576);
            values[index] = StableHash.HashParts(new string[] { property.Name, json });
            index++;
        }

        SortStrings(values);
        parts.Add(InvariantFormatter.FormatInt32(values.Length));
        index = 0;
        while (index < values.Length)
        {
            parts.Add(values[index]);
            index++;
        }
    }

    internal void AddEventParts(List<string> parts, DomEventSubscription[] events)
    {
        string[] values = new string[events.Length];
        int index = 0;
        while (index < events.Length)
        {
            DomEventSubscription subscription = events[index];
            values[index] = StableHash.HashParts(new string[]
            {
                subscription.Id,
                subscription.HandlerId,
                InvariantFormatter.FormatInt32((int)subscription.Kind),
                subscription.EventName,
                InvariantFormatter.FormatInt32((int)subscription.Phase),
                InvariantFormatter.FormatBoolean(subscription.IsDelegated),
                InvariantFormatter.FormatBoolean(subscription.PreventDefault),
                InvariantFormatter.FormatBoolean(subscription.StopPropagation),
                InvariantFormatter.FormatBoolean(subscription.Once),
                InvariantFormatter.FormatBoolean(subscription.Passive),
                InvariantFormatter.FormatBoolean(subscription.HasDebounceMilliseconds),
                InvariantFormatter.FormatInt32(subscription.DebounceMilliseconds),
                InvariantFormatter.FormatBoolean(subscription.HasThrottleMilliseconds),
                InvariantFormatter.FormatInt32(subscription.ThrottleMilliseconds)
            });
            index++;
        }

        parts.Add(InvariantFormatter.FormatInt32(values.Length));
        index = 0;
        while (index < values.Length)
        {
            parts.Add(values[index]);
            index++;
        }
    }

    internal void SortStrings(string[] values)
    {
        int index = 1;
        while (index < values.Length)
        {
            string candidate = values[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && string.CompareOrdinal(values[insertionIndex], candidate) > 0)
            {
                values[insertionIndex + 1] = values[insertionIndex];
                insertionIndex--;
            }

            values[insertionIndex + 1] = candidate;
            index++;
        }
    }

    internal string GetNodeKindToken(DomNodeKind kind)
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

    internal UiDiagnostic[] CreatePublicDiagnostics(DiagnosticEntry[] entries)
    {
        UiDiagnostic[] result = new UiDiagnostic[entries.Length];
        int index = 0;
        while (index < entries.Length)
        {
            DiagnosticEntry entry = entries[index];
            result[index] = new UiDiagnostic
            {
                Severity = (UiDiagnosticSeverity)(int)entry.Level,
                Code = entry.Code,
                Message = entry.Message,
                Location = entry.Location
            };
            index++;
        }

        return result;
    }
}
