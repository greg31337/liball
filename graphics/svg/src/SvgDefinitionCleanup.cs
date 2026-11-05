namespace svg;

internal static class SvgDefinitionCleanup
{
    internal static void RemoveUnusedDefinitions(SvgProcessContext context)
    {
        if (HasScriptSemantics(context.Document)) return;
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document);
        System.Collections.Generic.List<SvgNode> roots = new System.Collections.Generic.List<SvgNode>();
        System.Collections.Generic.Dictionary<SvgNode, SvgNode> ownerByNode = new System.Collections.Generic.Dictionary<SvgNode, SvgNode>();
        System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<SvgNode>> ownersById = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<SvgNode>>(System.StringComparer.Ordinal);
        System.Collections.Generic.Dictionary<SvgNode, System.Collections.Generic.List<SvgNode>> dependencies = new System.Collections.Generic.Dictionary<SvgNode, System.Collections.Generic.List<SvgNode>>();
        System.Collections.Generic.HashSet<SvgNode> live = new System.Collections.Generic.HashSet<SvgNode>();
        int index = 0;
        while (index < nodes.Count)
        {
            SvgNode node = nodes[index];
            if (DefinitionRoot(node) && !NestedDefinitionRoot(node) && !ProtectedDefinitionSubtree(node))
            {
                roots.Add(node);
                dependencies.Add(node, new System.Collections.Generic.List<SvgNode>());
                IndexDefinition(node, node, ownerByNode, ownersById);
            }
            index = index + 1;
        }
        System.Collections.Generic.List<string> ownedIds = StringKeySnapshot(ownersById);
        index = 0;
        while (index < ownedIds.Count)
        {
            string ownedId = ownedIds[index];
            if (!SimpleId(ownedId))
            {
                System.Collections.Generic.List<SvgNode> protectedOwners = ownersById[ownedId];
                int protectedIndex = 0;
                while (protectedIndex < protectedOwners.Count)
                {
                    live.Add(protectedOwners[protectedIndex]);
                    protectedIndex = protectedIndex + 1;
                }
            }
            index = index + 1;
        }
        index = 0;
        while (index < nodes.Count)
        {
            SvgNode source = nodes[index];
            System.Collections.Generic.HashSet<string> references = ReferencesFromNode(source);
            System.Collections.Generic.List<string> ids = SetSnapshot(references);
            int referenceIndex = 0;
            while (referenceIndex < ids.Count)
            {
                string id = ids[referenceIndex];
                if (ownersById.ContainsKey(id))
                {
                    System.Collections.Generic.List<SvgNode> targets = ownersById[id];
                    SvgNode? sourceRoot = ownerByNode.ContainsKey(source) ? ownerByNode[source] : null;
                    int targetIndex = 0;
                    while (targetIndex < targets.Count)
                    {
                        SvgNode target = targets[targetIndex];
                        if (sourceRoot == null) live.Add(target);
                        else if (!object.ReferenceEquals(sourceRoot, target)) AddUnique(dependencies[sourceRoot], target);
                        targetIndex = targetIndex + 1;
                    }
                }
                referenceIndex = referenceIndex + 1;
            }
            index = index + 1;
        }
        MarkDependencies(live, dependencies);
        index = roots.Count - 1;
        while (index >= 0 && !context.IsFailed())
        {
            SvgNode root = roots[index];
            if (!live.Contains(root) && root.Parent != null) RemoveNode(context, root);
            index = index - 1;
        }
    }

    internal static void DeduplicateDefinitions(SvgProcessContext context)
    {
        if (HasScriptSemantics(context.Document)) return;
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document);
        System.Collections.Generic.Dictionary<string, int> idCounts = CountIds(nodes);
        System.Collections.Generic.List<SvgNode> canonical = new System.Collections.Generic.List<SvgNode>();
        int index = 0;
        while (index < nodes.Count && !context.IsFailed())
        {
            SvgNode node = nodes[index];
            if (DefinitionResource(node) && !HasDescendantId(node))
            {
                string id = Id(node);
                SvgNode? match = id.Length == 0 || !SimpleId(id) || !idCounts.ContainsKey(id) || idCounts[id] != 1 ? null : EquivalentEarlier(node, canonical);
                if (match != null && CanRewriteAllReferences(context.Document, id))
                {
                    RewriteReferences(context, id, Id(match));
                    if (!context.IsFailed() && node.Parent != null) RemoveNode(context, node);
                }
                else if (id.Length != 0 && SimpleId(id) && idCounts.ContainsKey(id) && idCounts[id] == 1) canonical.Add(node);
            }
            index = index + 1;
        }
        if (!context.IsFailed()) DeduplicateStyles(context, nodes);
    }

    static bool DefinitionRoot(SvgNode node)
    {
        if (node.Kind != SvgNodeKind.Element || node.Parent == null) return false;
        if (node.NamespaceUri != SvgNamespaces.Svg || node.ElementKind == SvgElementKind.Unknown) return false;
        SvgElementKind kind = node.ElementKind;
        if (ReferencedDefinitionKind(kind) || kind == SvgElementKind.Symbol) return true;
        if (node.Parent.ElementKind != SvgElementKind.Defs) return false;
        return kind != SvgElementKind.Style && kind != SvgElementKind.Script && kind != SvgElementKind.Metadata && kind != SvgElementKind.Title && kind != SvgElementKind.Desc && kind != SvgElementKind.Animate && kind != SvgElementKind.AnimateMotion && kind != SvgElementKind.AnimateTransform && kind != SvgElementKind.Set && kind != SvgElementKind.Discard;
    }

    static bool NestedDefinitionRoot(SvgNode node)
    {
        SvgNode? current = node.Parent;
        while (current != null)
        {
            if (DefinitionRoot(current)) return true;
            current = current.Parent;
        }
        return false;
    }

    static bool DefinitionResource(SvgNode node)
    {
        if (node.Kind != SvgNodeKind.Element || node.Parent == null || node.NamespaceUri != SvgNamespaces.Svg) return false;
        SvgElementKind kind = node.ElementKind;
        if (kind == SvgElementKind.LinearGradient || kind == SvgElementKind.RadialGradient || kind == SvgElementKind.Pattern || kind == SvgElementKind.Marker || kind == SvgElementKind.ClipPath || kind == SvgElementKind.Mask || kind == SvgElementKind.Filter) return true;
        return kind == SvgElementKind.Image && node.Parent.ElementKind == SvgElementKind.Defs;
    }

    static bool ReferencedDefinitionKind(SvgElementKind kind)
    {
        return kind == SvgElementKind.LinearGradient || kind == SvgElementKind.RadialGradient || kind == SvgElementKind.Pattern || kind == SvgElementKind.Marker || kind == SvgElementKind.ClipPath || kind == SvgElementKind.Mask || kind == SvgElementKind.Filter;
    }

    static bool ProtectedDefinitionSubtree(SvgNode node)
    {
        System.Collections.Generic.List<SvgNode> pending = new System.Collections.Generic.List<SvgNode>();
        pending.Add(node);
        while (pending.Count != 0)
        {
            SvgNode current = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            if (current.Kind == SvgNodeKind.Element)
            {
                SvgElementKind kind = current.ElementKind;
                if (kind == SvgElementKind.Unknown || kind == SvgElementKind.Style || kind == SvgElementKind.Script || kind == SvgElementKind.Metadata || kind == SvgElementKind.Title || kind == SvgElementKind.Desc || kind == SvgElementKind.Animate || kind == SvgElementKind.AnimateMotion || kind == SvgElementKind.AnimateTransform || kind == SvgElementKind.Set || kind == SvgElementKind.Discard) return true;
                int attributeIndex = 0;
                while (attributeIndex < current.Attributes.Count)
                {
                    SvgAttribute attribute = current.Attributes[attributeIndex];
                    if (AccessibilityAttribute(attribute) || EventAttribute(attribute)) return true;
                    attributeIndex = attributeIndex + 1;
                }
            }
            int childIndex = current.Children.Count - 1;
            while (childIndex >= 0)
            {
                pending.Add(current.Children[childIndex]);
                childIndex = childIndex - 1;
            }
        }
        return false;
    }

    static void IndexDefinition(SvgNode node, SvgNode root, System.Collections.Generic.Dictionary<SvgNode, SvgNode> ownerByNode, System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<SvgNode>> ownersById)
    {
        System.Collections.Generic.List<SvgNode> pending = new System.Collections.Generic.List<SvgNode>();
        pending.Add(node);
        while (pending.Count != 0)
        {
            SvgNode current = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            ownerByNode[current] = root;
            string id = Id(current);
            if (id.Length != 0)
            {
                if (!ownersById.ContainsKey(id)) ownersById.Add(id, new System.Collections.Generic.List<SvgNode>());
                AddUnique(ownersById[id], root);
            }
            int index = current.Children.Count - 1;
            while (index >= 0)
            {
                pending.Add(current.Children[index]);
                index = index - 1;
            }
        }
    }

    static void MarkDependencies(System.Collections.Generic.HashSet<SvgNode> live, System.Collections.Generic.Dictionary<SvgNode, System.Collections.Generic.List<SvgNode>> dependencies)
    {
        System.Collections.Generic.List<SvgNode> pending = new System.Collections.Generic.List<SvgNode>();
        System.Collections.Generic.List<SvgNode> initial = NodeSetSnapshot(live);
        int index = 0;
        while (index < initial.Count)
        {
            pending.Add(initial[index]);
            index = index + 1;
        }
        while (pending.Count != 0)
        {
            SvgNode current = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            System.Collections.Generic.List<SvgNode> targets = dependencies[current];
            index = 0;
            while (index < targets.Count)
            {
                SvgNode target = targets[index];
                if (live.Add(target)) pending.Add(target);
                index = index + 1;
            }
        }
    }

    static SvgNode? EquivalentEarlier(SvgNode node, System.Collections.Generic.List<SvgNode> candidates)
    {
        int index = 0;
        while (index < candidates.Count)
        {
            SvgNode candidate = candidates[index];
            if (candidate.Parent != null && candidate.ElementKind == node.ElementKind && EquivalentNode(candidate, node, true)) return candidate;
            index = index + 1;
        }
        return null;
    }

    static bool EquivalentNode(SvgNode first, SvgNode second, bool ignoreId)
    {
        if (first.Kind != second.Kind || first.NamespaceUri != second.NamespaceUri || first.LocalName != second.LocalName || first.Value != second.Value) return false;
        int firstCount = ComparableAttributeCount(first, ignoreId);
        int secondCount = ComparableAttributeCount(second, ignoreId);
        if (firstCount != secondCount || first.Children.Count != second.Children.Count) return false;
        bool[] matched = new bool[second.Attributes.Count];
        int firstIndex = 0;
        while (firstIndex < first.Attributes.Count)
        {
            SvgAttribute firstAttribute = first.Attributes[firstIndex];
            if (!ignoreId || !IdAttribute(firstAttribute))
            {
                bool found = false;
                int secondIndex = 0;
                while (secondIndex < second.Attributes.Count)
                {
                    SvgAttribute secondAttribute = second.Attributes[secondIndex];
                    if (!matched[secondIndex] && (!ignoreId || !IdAttribute(secondAttribute)) && firstAttribute.NamespaceUri == secondAttribute.NamespaceUri && firstAttribute.LocalName == secondAttribute.LocalName && firstAttribute.Value == secondAttribute.Value)
                    {
                        matched[secondIndex] = true;
                        found = true;
                        break;
                    }
                    secondIndex = secondIndex + 1;
                }
                if (!found) return false;
            }
            firstIndex = firstIndex + 1;
        }
        int childIndex = 0;
        while (childIndex < first.Children.Count)
        {
            if (!EquivalentNode(first.Children[childIndex], second.Children[childIndex], false)) return false;
            childIndex = childIndex + 1;
        }
        return true;
    }

    static int ComparableAttributeCount(SvgNode node, bool ignoreId)
    {
        int result = 0;
        int index = 0;
        while (index < node.Attributes.Count)
        {
            if (!ignoreId || !IdAttribute(node.Attributes[index])) result = result + 1;
            index = index + 1;
        }
        return result;
    }

    static bool HasDescendantId(SvgNode node)
    {
        System.Collections.Generic.List<SvgNode> pending = new System.Collections.Generic.List<SvgNode>();
        int index = node.Children.Count - 1;
        while (index >= 0)
        {
            pending.Add(node.Children[index]);
            index = index - 1;
        }
        while (pending.Count != 0)
        {
            SvgNode current = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            if (Id(current).Length != 0) return true;
            index = current.Children.Count - 1;
            while (index >= 0)
            {
                pending.Add(current.Children[index]);
                index = index - 1;
            }
        }
        return false;
    }

    static bool CanRewriteAllReferences(SvgDocument document, string id)
    {
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(document);
        int nodeIndex = 0;
        while (nodeIndex < nodes.Count)
        {
            SvgNode node = nodes[nodeIndex];
            int attributeIndex = 0;
            while (attributeIndex < node.Attributes.Count)
            {
                SvgAttribute attribute = node.Attributes[attributeIndex];
                if (!IdAttribute(attribute) && UnrewritableAttributeReference(attribute, id)) return false;
                attributeIndex = attributeIndex + 1;
            }
            if (node.Kind == SvgNodeKind.Element && node.ElementKind == SvgElementKind.Style && CssHasNonUrlIdReference(new SvgNodeApi().GetTextContent(node), id)) return false;
            if ((node.Kind == SvgNodeKind.Text || node.Kind == SvgNodeKind.CData) && (node.Parent == null || node.Parent.ElementKind != SvgElementKind.Style) && ContainsHashReference(node.Value, id)) return false;
            nodeIndex = nodeIndex + 1;
        }
        return true;
    }

    static bool UnrewritableAttributeReference(SvgAttribute attribute, string id)
    {
        string value = attribute.Value;
        bool href = HrefAttribute(attribute) && LocalHref(value, id);
        if (ContainsHashReference(value, id) && !href && CssHasNonUrlIdReference(value, id)) return true;
        if (ContainsWord(value, id) && !IdReferenceAttribute(attribute) && !TimingAttribute(attribute) && !href) return true;
        return false;
    }

    static void RewriteReferences(SvgProcessContext context, string oldId, string newId)
    {
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document);
        int nodeIndex = 0;
        while (nodeIndex < nodes.Count && !context.IsFailed())
        {
            SvgNode node = nodes[nodeIndex];
            int attributeIndex = 0;
            while (attributeIndex < node.Attributes.Count && !context.IsFailed())
            {
                SvgAttribute attribute = node.Attributes[attributeIndex];
                if (!IdAttribute(attribute))
                {
                    string replacement = RewriteAttributeValue(attribute, oldId, newId);
                    if (replacement != attribute.Value) ReplaceAttribute(context, node, attribute, replacement);
                }
                attributeIndex = attributeIndex + 1;
            }
            if (node.Kind == SvgNodeKind.Text || node.Kind == SvgNodeKind.CData)
            {
                SvgNode? parent = node.Parent;
                if (parent != null && parent.ElementKind == SvgElementKind.Style)
                {
                    string replacement = ReplaceUrlReferences(node.Value, oldId, newId);
                    if (replacement != node.Value)
                    {
                        SvgMutationResult mutation = context.Mutations.SetTextContent(node, replacement);
                        if (!mutation.Success) context.Fail(mutation.Message);
                    }
                }
            }
            nodeIndex = nodeIndex + 1;
        }
    }

    static string RewriteAttributeValue(SvgAttribute attribute, string oldId, string newId)
    {
        string result = attribute.Value;
        if (HrefAttribute(attribute) && LocalHref(result, oldId)) result = PreserveOuterWhitespace(result, "#" + newId);
        if (IdReferenceAttribute(attribute)) result = ReplaceWords(result, oldId, newId);
        if (TimingAttribute(attribute)) result = ReplaceTimingReferences(result, oldId, newId);
        return ReplaceUrlReferences(result, oldId, newId);
    }

    static void DeduplicateStyles(SvgProcessContext context, System.Collections.Generic.List<SvgNode> nodes)
    {
        int index = 0;
        while (index < nodes.Count && !context.IsFailed())
        {
            SvgNode node = nodes[index];
            if (StyleCandidate(node))
            {
                SvgNode? previous = PreviousSignificantSibling(node);
                if (previous != null && StyleCandidate(previous) && EquivalentNode(previous, node, false)) RemoveNode(context, node);
            }
            index = index + 1;
        }
    }

    static bool StyleCandidate(SvgNode node)
    {
        if (node.Kind != SvgNodeKind.Element || node.ElementKind != SvgElementKind.Style || node.Parent == null) return false;
        if (Id(node).Length != 0) return false;
        int attributeIndex = 0;
        while (attributeIndex < node.Attributes.Count)
        {
            if (AccessibilityAttribute(node.Attributes[attributeIndex]) || EventAttribute(node.Attributes[attributeIndex])) return false;
            attributeIndex = attributeIndex + 1;
        }
        return new SvgNodeApi().GetTextContent(node).IndexOf("@import", System.StringComparison.OrdinalIgnoreCase) < 0;
    }

    static SvgNode? PreviousSignificantSibling(SvgNode node)
    {
        SvgNode? parent = node.Parent;
        if (parent == null) return null;
        int index = 0;
        while (index < parent.Children.Count && !object.ReferenceEquals(parent.Children[index], node)) index = index + 1;
        index = index - 1;
        while (index >= 0)
        {
            SvgNode current = parent.Children[index];
            if (current.Kind == SvgNodeKind.Comment || current.Kind == SvgNodeKind.Text && current.Value.Trim().Length == 0) index = index - 1;
            else return current;
        }
        return null;
    }

    static bool HasScriptSemantics(SvgDocument document)
    {
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(document);
        int nodeIndex = 0;
        while (nodeIndex < nodes.Count)
        {
            SvgNode node = nodes[nodeIndex];
            if (node.Kind == SvgNodeKind.Element && node.ElementKind == SvgElementKind.Script) return true;
            int attributeIndex = 0;
            while (attributeIndex < node.Attributes.Count)
            {
                if (EventAttribute(node.Attributes[attributeIndex])) return true;
                attributeIndex = attributeIndex + 1;
            }
            nodeIndex = nodeIndex + 1;
        }
        return false;
    }

    static System.Collections.Generic.HashSet<string> ReferencesFromNode(SvgNode node)
    {
        System.Collections.Generic.HashSet<string> result = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        int attributeIndex = 0;
        while (attributeIndex < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[attributeIndex];
            if (!IdAttribute(attribute))
            {
                AddHashReferences(attribute.Value, result);
                if (HrefAttribute(attribute)) AddHrefReference(attribute.Value, result);
                AddUrlReferences(attribute.Value, result);
                if (IdReferenceAttribute(attribute)) AddWords(attribute.Value, result);
                if (TimingAttribute(attribute)) AddTimingReferences(attribute.Value, result);
            }
            attributeIndex = attributeIndex + 1;
        }
        if (node.Kind == SvgNodeKind.Element && node.ElementKind == SvgElementKind.Style)
        {
            string css = new SvgNodeApi().GetTextContent(node);
            AddHashReferences(css, result);
            AddUrlReferences(css, result);
        }
        return result;
    }

    static void AddHrefReference(string value, System.Collections.Generic.HashSet<string> result)
    {
        string trimmed = value.Trim();
        if (trimmed.Length > 1 && trimmed[0] == '#') result.Add(trimmed.Substring(1));
    }

    static void AddUrlReferences(string value, System.Collections.Generic.HashSet<string> result)
    {
        int index = 0;
        while (index < value.Length)
        {
            int open = IndexOfUrlOpen(value, index);
            if (open < 0) return;
            int close = value.IndexOf(')', open + 4);
            if (close < 0) return;
            int start = open + 4;
            int end = close;
            while (start < end && char.IsWhiteSpace(value[start])) start = start + 1;
            while (end > start && char.IsWhiteSpace(value[end - 1])) end = end - 1;
            if (end - start >= 2 && (value[start] == '\'' && value[end - 1] == '\'' || value[start] == '"' && value[end - 1] == '"'))
            {
                start = start + 1;
                end = end - 1;
            }
            if (end - start > 1 && value[start] == '#') result.Add(value.Substring(start + 1, end - start - 1));
            index = close + 1;
        }
    }

    static void AddHashReferences(string value, System.Collections.Generic.HashSet<string> result)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '#')
            {
                int start = index + 1;
                index = start;
                while (index < value.Length && IdCharacter(value[index])) index = index + 1;
                if (index > start) result.Add(value.Substring(start, index - start));
            }
            else index = index + 1;
        }
    }

    static void AddWords(string value, System.Collections.Generic.HashSet<string> result)
    {
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length && char.IsWhiteSpace(value[index])) index = index + 1;
            int start = index;
            while (index < value.Length && !char.IsWhiteSpace(value[index])) index = index + 1;
            if (index > start) result.Add(value.Substring(start, index - start));
        }
    }

    static void AddTimingReferences(string value, System.Collections.Generic.HashSet<string> result)
    {
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length && (char.IsWhiteSpace(value[index]) || value[index] == ';')) index = index + 1;
            int start = index;
            while (index < value.Length && value[index] != '.' && value[index] != ';' && !char.IsWhiteSpace(value[index])) index = index + 1;
            if (index > start && index < value.Length && value[index] == '.') result.Add(value.Substring(start, index - start));
            while (index < value.Length && value[index] != ';') index = index + 1;
        }
    }

    static string ReplaceUrlReferences(string value, string oldId, string newId)
    {
        System.Text.StringBuilder? builder = null;
        int copied = 0;
        int index = 0;
        while (index < value.Length)
        {
            int open = IndexOfUrlOpen(value, index);
            if (open < 0) break;
            int close = value.IndexOf(')', open + 4);
            if (close < 0) break;
            int hash = LocalUrlHash(value, open + 4, close, oldId);
            if (hash >= 0)
            {
                if (builder == null) builder = new System.Text.StringBuilder(value.Length + newId.Length);
                builder.Append(value, copied, hash - copied + 1);
                builder.Append(newId);
                copied = hash + oldId.Length + 1;
            }
            index = close + 1;
        }
        if (builder == null) return value;
        builder.Append(value, copied, value.Length - copied);
        return builder.ToString();
    }

    static bool CssHasNonUrlIdReference(string value, string id)
    {
        if (value.IndexOf('\\') >= 0) return true;
        int index = 0;
        while (index < value.Length)
        {
            int open = IndexOfUrlOpen(value, index);
            int hash = IndexOfHashReference(value, id, index);
            if (hash < 0) return false;
            if (open < 0 || hash < open) return true;
            int close = value.IndexOf(')', open + 4);
            if (close < 0 || hash > close) return true;
            index = close + 1;
        }
        return false;
    }

    static int IndexOfUrlOpen(string value, int start)
    {
        return value.IndexOf("url(", start, System.StringComparison.OrdinalIgnoreCase);
    }

    static int LocalUrlHash(string value, int start, int end, string id)
    {
        while (start < end && char.IsWhiteSpace(value[start])) start = start + 1;
        while (end > start && char.IsWhiteSpace(value[end - 1])) end = end - 1;
        if (end - start >= 2 && (value[start] == '\'' && value[end - 1] == '\'' || value[start] == '"' && value[end - 1] == '"'))
        {
            start = start + 1;
            end = end - 1;
        }
        return end - start == id.Length + 1 && value[start] == '#' && string.CompareOrdinal(value, start + 1, id, 0, id.Length) == 0 ? start : -1;
    }

    static bool ContainsHashReference(string value, string id)
    {
        return IndexOfHashReference(value, id, 0) >= 0;
    }

    static int IndexOfHashReference(string value, string id, int start)
    {
        string needle = "#" + id;
        int index = value.IndexOf(needle, start, System.StringComparison.Ordinal);
        while (index >= 0)
        {
            int end = index + needle.Length;
            if (end == value.Length || !IdCharacter(value[end])) return index;
            index = value.IndexOf(needle, end, System.StringComparison.Ordinal);
        }
        return -1;
    }

    static string ReplaceWords(string value, string oldId, string newId)
    {
        System.Text.StringBuilder? builder = null;
        int copied = 0;
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length && char.IsWhiteSpace(value[index])) index = index + 1;
            int start = index;
            while (index < value.Length && !char.IsWhiteSpace(value[index])) index = index + 1;
            if (index - start == oldId.Length && string.CompareOrdinal(value, start, oldId, 0, oldId.Length) == 0)
            {
                if (builder == null) builder = new System.Text.StringBuilder(value.Length + newId.Length);
                builder.Append(value, copied, start - copied);
                builder.Append(newId);
                copied = index;
            }
        }
        if (builder == null) return value;
        builder.Append(value, copied, value.Length - copied);
        return builder.ToString();
    }

    static string ReplaceTimingReferences(string value, string oldId, string newId)
    {
        System.Text.StringBuilder? builder = null;
        int copied = 0;
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length && (char.IsWhiteSpace(value[index]) || value[index] == ';')) index = index + 1;
            int start = index;
            while (index < value.Length && value[index] != '.' && value[index] != ';' && !char.IsWhiteSpace(value[index])) index = index + 1;
            if (index - start == oldId.Length && index < value.Length && value[index] == '.' && string.CompareOrdinal(value, start, oldId, 0, oldId.Length) == 0)
            {
                if (builder == null) builder = new System.Text.StringBuilder(value.Length + newId.Length);
                builder.Append(value, copied, start - copied);
                builder.Append(newId);
                copied = index;
            }
            while (index < value.Length && value[index] != ';') index = index + 1;
        }
        if (builder == null) return value;
        builder.Append(value, copied, value.Length - copied);
        return builder.ToString();
    }

    static string PreserveOuterWhitespace(string value, string replacement)
    {
        int start = 0;
        int end = value.Length;
        while (start < end && char.IsWhiteSpace(value[start])) start = start + 1;
        while (end > start && char.IsWhiteSpace(value[end - 1])) end = end - 1;
        return value.Substring(0, start) + replacement + value.Substring(end);
    }

    static bool LocalHref(string value, string id)
    {
        string trimmed = value.Trim();
        return trimmed.Length == id.Length + 1 && trimmed[0] == '#' && string.CompareOrdinal(trimmed, 1, id, 0, id.Length) == 0;
    }

    static bool ContainsWord(string value, string word)
    {
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length && char.IsWhiteSpace(value[index])) index = index + 1;
            int start = index;
            while (index < value.Length && !char.IsWhiteSpace(value[index])) index = index + 1;
            if (index - start == word.Length && string.CompareOrdinal(value, start, word, 0, word.Length) == 0) return true;
        }
        return false;
    }

    static bool IdCharacter(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_' || value == '-' || value == '.' || value >= 128;
    }

    static bool SimpleId(string value)
    {
        if (value.Length == 0) return false;
        int index = 0;
        while (index < value.Length)
        {
            if (!IdCharacter(value[index])) return false;
            index = index + 1;
        }
        return true;
    }

    static bool IdAttribute(SvgAttribute attribute)
    {
        return attribute.NamespaceUri.Length == 0 && attribute.LocalName == SvgAttributeNames.Id;
    }

    static bool HrefAttribute(SvgAttribute attribute)
    {
        return attribute.LocalName == SvgAttributeNames.Href && (attribute.NamespaceUri.Length == 0 || attribute.NamespaceUri == SvgNamespaces.XLink);
    }

    static bool IdReferenceAttribute(SvgAttribute attribute)
    {
        if (attribute.NamespaceUri.Length != 0) return false;
        string name = attribute.LocalName;
        return name == "aria-labelledby" || name == "aria-describedby" || name == "aria-controls" || name == "aria-owns" || name == "aria-details" || name == "aria-errormessage";
    }

    static bool TimingAttribute(SvgAttribute attribute)
    {
        return attribute.NamespaceUri.Length == 0 && (attribute.LocalName == "begin" || attribute.LocalName == "end");
    }

    static bool AccessibilityAttribute(SvgAttribute attribute)
    {
        if (attribute.NamespaceUri.Length != 0) return false;
        return attribute.LocalName == "role" || attribute.LocalName == SvgAttributeNames.TabIndex || attribute.LocalName.StartsWith("aria-", System.StringComparison.OrdinalIgnoreCase);
    }

    static bool EventAttribute(SvgAttribute attribute)
    {
        return attribute.LocalName.StartsWith("on", System.StringComparison.OrdinalIgnoreCase);
    }

    static string Id(SvgNode node)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (IdAttribute(attribute)) return attribute.Value;
            index = index + 1;
        }
        return "";
    }

    static System.Collections.Generic.Dictionary<string, int> CountIds(System.Collections.Generic.List<SvgNode> nodes)
    {
        System.Collections.Generic.Dictionary<string, int> result = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.Ordinal);
        int index = 0;
        while (index < nodes.Count)
        {
            string id = Id(nodes[index]);
            if (id.Length != 0)
            {
                if (result.ContainsKey(id)) result[id] = result[id] + 1;
                else result.Add(id, 1);
            }
            index = index + 1;
        }
        return result;
    }

    static void ReplaceAttribute(SvgProcessContext context, SvgNode node, SvgAttribute attribute, string value)
    {
        SvgAttribute replacement = context.Documents.CreateAttribute(attribute.QualifiedName, attribute.Prefix, attribute.LocalName, attribute.NamespaceUri, value);
        SvgMutationResult mutation = context.Mutations.ReplaceAttribute(node, attribute, replacement);
        if (!mutation.Success) context.Fail(mutation.Message);
    }

    static void RemoveNode(SvgProcessContext context, SvgNode node)
    {
        SvgMutationResult mutation = context.Mutations.RemoveChild(node.Parent, node);
        if (!mutation.Success) context.Fail(mutation.Message);
    }

    static void AddUnique(System.Collections.Generic.List<SvgNode> values, SvgNode value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (object.ReferenceEquals(values[index], value)) return;
            index = index + 1;
        }
        values.Add(value);
    }

    static System.Collections.Generic.List<string> SetSnapshot(System.Collections.Generic.HashSet<string> values)
    {
        System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();
        foreach (string value in values) result.Add(value);
        result.Sort(System.StringComparer.Ordinal);
        return result;
    }

    static System.Collections.Generic.List<string> StringKeySnapshot(System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<SvgNode>> values)
    {
        System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();
        foreach (string value in values.Keys) result.Add(value);
        result.Sort(System.StringComparer.Ordinal);
        return result;
    }

    static System.Collections.Generic.List<SvgNode> NodeSetSnapshot(System.Collections.Generic.HashSet<SvgNode> values)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        foreach (SvgNode value in values) result.Add(value);
        return result;
    }

    static System.Collections.Generic.List<SvgNode> CollectNodes(SvgDocument document)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        System.Collections.Generic.List<SvgNode> pending = new System.Collections.Generic.List<SvgNode>();
        pending.Add(document.DocumentNode);
        while (pending.Count != 0)
        {
            SvgNode node = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            result.Add(node);
            int index = node.Children.Count - 1;
            while (index >= 0)
            {
                pending.Add(node.Children[index]);
                index = index - 1;
            }
        }
        return result;
    }
}
