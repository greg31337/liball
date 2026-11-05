using System.Collections.Generic;

namespace yaml;

/// <summary>
/// Represents a mutable scalar, sequence, mapping, alias, or empty YAML node.
/// </summary>
public class YamlNode
{
    internal string TagPresentation;
    internal string ParsedTag;
    /// <summary>
    /// The data shape represented by this node.
    /// </summary>
    public YamlNodeKind Kind;
    /// <summary>
    /// The scalar presentation style read from or requested for this value.
    /// </summary>
    public YamlScalarStyle ScalarStyle;
    /// <summary>
    /// The block or flow presentation style read from or requested for this collection.
    /// </summary>
    public YamlCollectionStyle CollectionStyle;
    /// <summary>
    /// The YAML tag, using its expanded form when schema processing has resolved it.
    /// </summary>
    public string Tag;
    /// <summary>
    /// The decoded scalar text; it is empty for collections, aliases, and empty nodes.
    /// </summary>
    public string Value;
    /// <summary>
    /// The anchor name declared on this node or event, without the ampersand marker.
    /// </summary>
    public string Anchor;
    /// <summary>
    /// The referenced anchor name, without the asterisk marker.
    /// </summary>
    public string AliasName;
    /// <summary>
    /// The resolved node referenced by an alias, or null when it is unresolved.
    /// </summary>
    public YamlNode? AliasTarget;
    /// <summary>
    /// The ordered child nodes of a sequence.
    /// </summary>
    public List<YamlNode> Children;
    /// <summary>
    /// The ordered key and value pairs of a mapping.
    /// </summary>
    public List<YamlPair> Pairs;
    /// <summary>
    /// Comments attached before this item, without changing their order.
    /// </summary>
    public List<string> LeadingComments;
    /// <summary>
    /// Comments attached after or inline with this item, in source order.
    /// </summary>
    public List<string> TrailingComments;
    /// <summary>
    /// Blank-line markers preserved before this item.
    /// </summary>
    public List<string> LeadingBlankLines;
    /// <summary>
    /// Blank-line markers preserved after this item.
    /// </summary>
    public List<string> TrailingBlankLines;
    /// <summary>
    /// Ordered comment and blank-line metadata attached to this node.
    /// </summary>
    public List<YamlTrivia> Trivia;
    /// <summary>
    /// The source range occupied by this item.
    /// </summary>
    public YamlSourceSpan Span;

    /// <summary>
    /// Initializes a new YAML node with empty or default state.
    /// </summary>
    public YamlNode()
    {
        Kind = YamlNodeKind.Empty;
        TagPresentation = "";
        ParsedTag = "";
        ScalarStyle = YamlScalarStyle.Any;
        CollectionStyle = YamlCollectionStyle.Any;
        Tag = "";
        Value = "";
        Anchor = "";
        AliasName = "";
        AliasTarget = null;
        Children = new List<YamlNode>();
        Pairs = new List<YamlPair>();
        LeadingComments = new List<string>();
        TrailingComments = new List<string>();
        LeadingBlankLines = new List<string>();
        TrailingBlankLines = new List<string>();
        Trivia = new List<YamlTrivia>();
        Span = YamlSourceSpan.Empty();
    }

    /// <summary>
    /// Creates an empty node associated with the supplied source range.
    /// </summary>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode Empty(YamlSourceSpan? span)
    {
        YamlNode node = new YamlNode();
        node.Kind = YamlNodeKind.Empty;
        node.Span = span == null ? YamlSourceSpan.Empty() : span;
        return node;
    }

    /// <summary>
    /// Creates a scalar node with the requested text and presentation style.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <param name="style">The presentation style assigned to the value.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode Scalar(string? value, YamlScalarStyle style, YamlSourceSpan? span)
    {
        YamlNode node = new YamlNode();
        node.Kind = YamlNodeKind.Scalar;
        node.Value = value == null ? "" : value;
        node.ScalarStyle = style;
        node.Span = span == null ? YamlSourceSpan.Empty() : span;
        return node;
    }

    /// <summary>
    /// Creates an empty sequence node with the requested collection style.
    /// </summary>
    /// <param name="style">The presentation style assigned to the value.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode Sequence(YamlCollectionStyle style, YamlSourceSpan? span)
    {
        YamlNode node = new YamlNode();
        node.Kind = YamlNodeKind.Sequence;
        node.CollectionStyle = style;
        node.Span = span == null ? YamlSourceSpan.Empty() : span;
        return node;
    }

    /// <summary>
    /// Creates an empty mapping node with the requested collection style.
    /// </summary>
    /// <param name="style">The presentation style assigned to the value.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode Mapping(YamlCollectionStyle style, YamlSourceSpan? span)
    {
        YamlNode node = new YamlNode();
        node.Kind = YamlNodeKind.Mapping;
        node.CollectionStyle = style;
        node.Span = span == null ? YamlSourceSpan.Empty() : span;
        return node;
    }

    /// <summary>
    /// Creates an alias node for the supplied anchor name.
    /// </summary>
    /// <param name="aliasName">The anchor name referenced by the alias, without the asterisk marker.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode Alias(string? aliasName, YamlSourceSpan? span)
    {
        YamlNode node = new YamlNode();
        node.Kind = YamlNodeKind.Alias;
        node.AliasName = aliasName == null ? "" : aliasName;
        node.Span = span == null ? YamlSourceSpan.Empty() : span;
        return node;
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="child">The sequence child node to process.</param>
    internal void AddChild(YamlNode? child)
    {
        if (child == null)
        {
            return;
        }

        Children.Add(child);
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="pair">The mapping pair to process.</param>
    internal void AddPair(YamlPair? pair)
    {
        if (pair == null)
        {
            return;
        }

        Pairs.Add(pair);
    }

    /// <summary>
    /// Converts this node graph to dictionaries, lists, scalar CLR values, or null.
    /// </summary>
    /// <returns>The loaded or converted value; null is a valid YAML result.</returns>
    internal object? ToJsonValue()
    {
        return YamlJsonConverter.ToJsonValue(this);
    }

    /// <summary>
    /// Creates a deep graph copy while preserving alias relationships and presentation metadata.
    /// </summary>
    /// <returns>The resulting YAML node.</returns>
    internal YamlNode Clone()
    {
        return CloneGraph(new Dictionary<YamlNode, YamlNode>());
    }

    internal YamlNode CloneGraph(Dictionary<YamlNode, YamlNode> visited)
    {
        if (visited == null)
        {
            visited = new Dictionary<YamlNode, YamlNode>();
        }

        if (visited.ContainsKey(this))
        {
            return visited[this];
        }

        YamlNode clone = new YamlNode();
        visited[this] = clone;
        CopyShallowTo(clone);

        int childIndex = 0;
        while (childIndex < Children.Count)
        {
            clone.Children.Add(Children[childIndex].CloneGraph(visited));
            childIndex = childIndex + 1;
        }

        int pairIndex = 0;
        while (pairIndex < Pairs.Count)
        {
            clone.Pairs.Add(Pairs[pairIndex].CloneGraph(visited));
            pairIndex = pairIndex + 1;
        }

        if (AliasTarget != null)
        {
            clone.AliasTarget = AliasTarget.CloneGraph(visited);
        }

        return clone;
    }

    internal void CopyShallowTo(YamlNode target)
    {
        if (target == null)
        {
            return;
        }

        target.Kind = Kind;
        target.ScalarStyle = ScalarStyle;
        target.CollectionStyle = CollectionStyle;
        target.Tag = Tag;
        target.TagPresentation = TagPresentation;
        target.ParsedTag = ParsedTag;
        target.Value = Value;
        target.Anchor = Anchor;
        target.AliasName = AliasName;
        target.AliasTarget = null;
        target.Children = new List<YamlNode>();
        target.Pairs = new List<YamlPair>();
        target.LeadingComments = CopyStringList(LeadingComments);
        target.TrailingComments = CopyStringList(TrailingComments);
        target.LeadingBlankLines = CopyStringList(LeadingBlankLines);
        target.TrailingBlankLines = CopyStringList(TrailingBlankLines);
        target.Trivia = CopyTriviaList(Trivia);
        target.Span = Span == null ? YamlSourceSpan.Empty() : Span.Copy();
    }

    internal static List<string> CopyStringList(List<string> values)
    {
        List<string> copy = new List<string>();
        if (values == null)
        {
            return copy;
        }

        int index = 0;
        while (index < values.Count)
        {
            copy.Add(values[index]);
            index = index + 1;
        }

        return copy;
    }

    internal static List<YamlTrivia> CopyTriviaList(List<YamlTrivia> values)
    {
        List<YamlTrivia> copy = new List<YamlTrivia>();
        if (values == null)
        {
            return copy;
        }

        int index = 0;
        while (index < values.Count)
        {
            if (values[index] != null)
            {
                copy.Add(values[index].Copy());
            }

            index = index + 1;
        }

        return copy;
    }

    /// <summary>
    /// Renames matching anchor or alias references and returns the number of changes.
    /// </summary>
    /// <param name="newAliasName">The replacement alias name without the asterisk marker.</param>
    /// <returns>True when this node is an alias and its name was assigned; otherwise false.</returns>
    internal bool RenameAlias(string? newAliasName)
    {
        if (Kind != YamlNodeKind.Alias)
        {
            return false;
        }

        AliasName = newAliasName == null ? "" : newAliasName;
        return true;
    }

    /// <summary>
    /// Changes matching alias targets and returns the number of changes.
    /// </summary>
    /// <param name="target">The node that the alias should reference.</param>
    /// <returns>True when this node is an alias and the non-null target was assigned; otherwise false.</returns>
    internal bool RetargetAlias(YamlNode? target)
    {
        if (Kind != YamlNodeKind.Alias || target == null)
        {
            return false;
        }

        AliasTarget = target;
        if (target.Anchor != "")
        {
            AliasName = target.Anchor;
        }

        return true;
    }

    /// <summary>
    /// Renames matching anchor or alias references and returns the number of changes.
    /// </summary>
    /// <param name="oldName">The anchor or alias name to replace.</param>
    /// <param name="newName">The replacement anchor or alias name.</param>
    /// <returns>The number of matching, changed, or stored items.</returns>
    internal int RenameAnchor(string oldName, string newName)
    {
        return RenameAnchorReferences(oldName, newName, true, false, new Dictionary<YamlNode, YamlNode>());
    }

    /// <summary>
    /// Renames matching anchor or alias references and returns the number of changes.
    /// </summary>
    /// <param name="oldName">The anchor or alias name to replace.</param>
    /// <param name="newName">The replacement anchor or alias name.</param>
    /// <returns>The number of matching, changed, or stored items.</returns>
    internal int RenameAliases(string oldName, string newName)
    {
        return RenameAnchorReferences(oldName, newName, false, true, new Dictionary<YamlNode, YamlNode>());
    }

    /// <summary>
    /// Renames matching anchor or alias references and returns the number of changes.
    /// </summary>
    /// <param name="oldName">The anchor or alias name to replace.</param>
    /// <param name="newName">The replacement anchor or alias name.</param>
    /// <returns>The number of matching, changed, or stored items.</returns>
    internal int RenameAnchorAndAliases(string oldName, string newName)
    {
        return RenameAnchorReferences(oldName, newName, true, true, new Dictionary<YamlNode, YamlNode>());
    }

    internal int RenameAnchorReferences(string oldName, string newName, bool renameAnchors, bool renameAliases, Dictionary<YamlNode, YamlNode> visited)
    {
        if (oldName == "" || visited == null)
        {
            return 0;
        }

        if (visited.ContainsKey(this))
        {
            return 0;
        }

        visited[this] = this;
        int count = 0;
        string replacement = newName == null ? "" : newName;
        if (renameAnchors && Anchor == oldName)
        {
            Anchor = replacement;
            count = count + 1;
        }

        if (renameAliases && Kind == YamlNodeKind.Alias && AliasName == oldName)
        {
            AliasName = replacement;
            count = count + 1;
        }

        count = count + RenameReferencesInChildren(oldName, replacement, renameAnchors, renameAliases, visited);
        return count;
    }

    internal int RenameReferencesInChildren(string oldName, string newName, bool renameAnchors, bool renameAliases, Dictionary<YamlNode, YamlNode> visited)
    {
        int count = 0;
        int childIndex = 0;
        while (childIndex < Children.Count)
        {
            count = count + Children[childIndex].RenameAnchorReferences(oldName, newName, renameAnchors, renameAliases, visited);
            childIndex = childIndex + 1;
        }

        int pairIndex = 0;
        while (pairIndex < Pairs.Count)
        {
            YamlPair pair = Pairs[pairIndex];
            if (pair.Key != null)
            {
                count = count + pair.Key.RenameAnchorReferences(oldName, newName, renameAnchors, renameAliases, visited);
            }

            if (pair.Value != null)
            {
                count = count + pair.Value.RenameAnchorReferences(oldName, newName, renameAnchors, renameAliases, visited);
            }

            pairIndex = pairIndex + 1;
        }

        if (AliasTarget != null)
        {
            count = count + AliasTarget.RenameAnchorReferences(oldName, newName, renameAnchors, renameAliases, visited);
        }

        return count;
    }

    /// <summary>
    /// Changes matching alias targets and returns the number of changes.
    /// </summary>
    /// <param name="aliasName">The anchor name referenced by the alias, without the asterisk marker.</param>
    /// <param name="target">The target node or CLR type associated with the operation.</param>
    /// <returns>The number of matching, changed, or stored items.</returns>
    internal int RetargetAliases(string aliasName, YamlNode? target)
    {
        return RetargetAliasesByName(aliasName, target, new Dictionary<YamlNode, YamlNode>());
    }

    /// <summary>
    /// Changes matching alias targets and returns the number of changes.
    /// </summary>
    /// <param name="oldTarget">The alias target to replace.</param>
    /// <param name="newTarget">The replacement alias target.</param>
    /// <returns>The number of matching, changed, or stored items.</returns>
    internal int RetargetAliasesByTarget(YamlNode? oldTarget, YamlNode? newTarget)
    {
        return RetargetAliasesByTargetRecursive(oldTarget, newTarget, new Dictionary<YamlNode, YamlNode>());
    }

    internal int RetargetAliasesByName(string aliasName, YamlNode? target, Dictionary<YamlNode, YamlNode> visited)
    {
        if (aliasName == "" || target == null || visited == null)
        {
            return 0;
        }

        if (visited.ContainsKey(this))
        {
            return 0;
        }

        visited[this] = this;
        int count = 0;
        if (Kind == YamlNodeKind.Alias && AliasName == aliasName)
        {
            RetargetAlias(target);
            count = count + 1;
        }

        count = count + RetargetAliasesByNameInChildren(aliasName, target, visited);
        return count;
    }

    internal int RetargetAliasesByNameInChildren(string aliasName, YamlNode target, Dictionary<YamlNode, YamlNode> visited)
    {
        int count = 0;
        int childIndex = 0;
        while (childIndex < Children.Count)
        {
            count = count + Children[childIndex].RetargetAliasesByName(aliasName, target, visited);
            childIndex = childIndex + 1;
        }

        int pairIndex = 0;
        while (pairIndex < Pairs.Count)
        {
            YamlPair pair = Pairs[pairIndex];
            if (pair.Key != null)
            {
                count = count + pair.Key.RetargetAliasesByName(aliasName, target, visited);
            }

            if (pair.Value != null)
            {
                count = count + pair.Value.RetargetAliasesByName(aliasName, target, visited);
            }

            pairIndex = pairIndex + 1;
        }

        if (AliasTarget != null)
        {
            count = count + AliasTarget.RetargetAliasesByName(aliasName, target, visited);
        }

        return count;
    }

    internal int RetargetAliasesByTargetRecursive(YamlNode? oldTarget, YamlNode? newTarget, Dictionary<YamlNode, YamlNode> visited)
    {
        if (oldTarget == null || newTarget == null || visited == null)
        {
            return 0;
        }

        if (visited.ContainsKey(this))
        {
            return 0;
        }

        visited[this] = this;
        int count = 0;
        if (Kind == YamlNodeKind.Alias && AliasTarget == oldTarget)
        {
            RetargetAlias(newTarget);
            count = count + 1;
        }

        count = count + RetargetAliasesByTargetInChildren(oldTarget, newTarget, visited);
        return count;
    }

    internal int RetargetAliasesByTargetInChildren(YamlNode oldTarget, YamlNode newTarget, Dictionary<YamlNode, YamlNode> visited)
    {
        int count = 0;
        int childIndex = 0;
        while (childIndex < Children.Count)
        {
            count = count + Children[childIndex].RetargetAliasesByTargetRecursive(oldTarget, newTarget, visited);
            childIndex = childIndex + 1;
        }

        int pairIndex = 0;
        while (pairIndex < Pairs.Count)
        {
            YamlPair pair = Pairs[pairIndex];
            if (pair.Key != null)
            {
                count = count + pair.Key.RetargetAliasesByTargetRecursive(oldTarget, newTarget, visited);
            }

            if (pair.Value != null)
            {
                count = count + pair.Value.RetargetAliasesByTargetRecursive(oldTarget, newTarget, visited);
            }

            pairIndex = pairIndex + 1;
        }

        if (AliasTarget != null)
        {
            count = count + AliasTarget.RetargetAliasesByTargetRecursive(oldTarget, newTarget, visited);
        }

        return count;
    }

    /// <summary>
    /// Copies missing pairs from merge-key sources into reachable mappings and removes applied merge pairs.
    /// </summary>
    /// <returns>The number of matching, changed, or stored items.</returns>
    internal int ApplyMergeKeys()
    {
        return ApplyMergeKeysRecursive(new Dictionary<YamlNode, YamlNode>());
    }

    internal int ApplyMergeKeysRecursive(Dictionary<YamlNode, YamlNode> visited)
    {
        if (visited == null)
        {
            visited = new Dictionary<YamlNode, YamlNode>();
        }

        if (visited.ContainsKey(this))
        {
            return 0;
        }

        visited[this] = this;
        int count = 0;
        if (Kind == YamlNodeKind.Mapping)
        {
            count = count + ApplyMergeKeysToMapping(visited);
        }

        count = count + ApplyMergeKeysInChildren(visited);
        return count;
    }

    internal int ApplyMergeKeysInChildren(Dictionary<YamlNode, YamlNode> visited)
    {
        int count = 0;
        int childIndex = 0;
        while (childIndex < Children.Count)
        {
            count = count + Children[childIndex].ApplyMergeKeysRecursive(visited);
            childIndex = childIndex + 1;
        }

        int pairIndex = 0;
        while (pairIndex < Pairs.Count)
        {
            YamlPair pair = Pairs[pairIndex];
            if (pair.Key != null)
            {
                count = count + pair.Key.ApplyMergeKeysRecursive(visited);
            }

            if (pair.Value != null)
            {
                count = count + pair.Value.ApplyMergeKeysRecursive(visited);
            }

            pairIndex = pairIndex + 1;
        }

        if (AliasTarget != null)
        {
            count = count + AliasTarget.ApplyMergeKeysRecursive(visited);
        }

        return count;
    }

    internal int ApplyMergeKeysToMapping(Dictionary<YamlNode, YamlNode> visited)
    {
        int count = 0;
        int index = 0;
        while (index < Pairs.Count)
        {
            YamlPair pair = Pairs[index];
            if (!IsMergeKeyPair(pair))
            {
                index = index + 1;
                continue;
            }

            List<YamlPair> mergePairs = new List<YamlPair>();
            CollectMergePairs(pair.Value, mergePairs, new Dictionary<YamlNode, YamlNode>(), visited);
            Pairs.RemoveAt(index);
            count = count + 1;

            Dictionary<YamlNode, YamlNode> cloneVisited = new Dictionary<YamlNode, YamlNode>();
            int mergeIndex = 0;
            int insertIndex = index;
            while (mergeIndex < mergePairs.Count)
            {
                YamlPair mergePair = mergePairs[mergeIndex];
                if (!ContainsEquivalentMappingKey(mergePair.Key))
                {
                    Pairs.Insert(insertIndex, mergePair.CloneGraph(cloneVisited));
                    insertIndex = insertIndex + 1;
                    count = count + 1;
                }

                mergeIndex = mergeIndex + 1;
            }

            index = insertIndex;
        }

        return count;
    }

    internal static bool IsMergeKeyPair(YamlPair pair)
    {
        if (pair == null || pair.Key == null)
        {
            return false;
        }

        return IsMergeKeyNode(pair.Key);
    }

    internal static bool IsMergeKeyNode(YamlNode node)
    {
        if (node == null || node.Kind != YamlNodeKind.Scalar)
        {
            return false;
        }

        if (node.Tag == YamlScalarResolver.MergeTag)
        {
            return true;
        }

        return node.Tag == ""
            && node.Value == "<<"
            && (node.ScalarStyle == YamlScalarStyle.Plain || node.ScalarStyle == YamlScalarStyle.Any);
    }

    internal static void CollectMergePairs(YamlNode? value, List<YamlPair> mergePairs, Dictionary<YamlNode, YamlNode> mergeVisited, Dictionary<YamlNode, YamlNode> processVisited)
    {
        if (value == null || mergePairs == null || mergeVisited == null)
        {
            return;
        }

        YamlNode? source = ResolveMergeSource(value);
        if (source == null)
        {
            return;
        }

        if (mergeVisited.ContainsKey(source))
        {
            return;
        }

        mergeVisited[source] = source;
        if (source.Kind == YamlNodeKind.Mapping)
        {
            source.ApplyMergeKeysRecursive(processVisited);
            int index = 0;
            while (index < source.Pairs.Count)
            {
                if (!IsMergeKeyPair(source.Pairs[index]))
                {
                    mergePairs.Add(source.Pairs[index]);
                }

                index = index + 1;
            }

            return;
        }

        if (source.Kind == YamlNodeKind.Sequence)
        {
            int index = 0;
            while (index < source.Children.Count)
            {
                CollectMergePairs(source.Children[index], mergePairs, mergeVisited, processVisited);
                index = index + 1;
            }
        }
    }

    internal static YamlNode? ResolveMergeSource(YamlNode? node)
    {
        if (node == null)
        {
            return null;
        }

        if (node.Kind == YamlNodeKind.Alias && node.AliasTarget != null)
        {
            return node.AliasTarget;
        }

        return node;
    }

    internal bool ContainsEquivalentMappingKey(YamlNode? key)
    {
        if (key == null || Kind != YamlNodeKind.Mapping)
        {
            return false;
        }

        if (key.Kind == YamlNodeKind.Scalar)
        {
            return FindMappingPairIndex(key.Value) >= 0;
        }

        int index = 0;
        while (index < Pairs.Count)
        {
            if (Pairs[index].Key == key)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>
    /// Follows a path from this node and returns the selected node, or null when it cannot be reached.
    /// </summary>
    /// <param name="pathText">The path text to parse and follow.</param>
    /// <returns>The resulting YAML node.</returns>
    internal YamlNode? FindPath(string? pathText)
    {
        return FindParsedPath(YamlPathParser.Parse(pathText));
    }

    /// <summary>
    /// Follows a path from this node and returns the selected node, or null when it cannot be reached.
    /// </summary>
    /// <param name="path">The parsed path to follow.</param>
    /// <returns>The resulting YAML node.</returns>
    internal YamlNode? FindParsedPath(YamlPath? path)
    {
        if (path == null || !path.Valid)
        {
            return null;
        }

        YamlNode current = this;
        int index = 0;
        while (index < path.Segments.Count)
        {
            YamlPathSegment segment = path.Segments[index];
            if (segment.Kind == YamlPathSegmentKind.MappingKey)
            {
                YamlNode? value = current.FindMappingValue(segment.Key);
                if (value == null)
                {
                    return null;
                }

                current = value;
            }
            else if (segment.Kind == YamlPathSegmentKind.SequenceIndex)
            {
                YamlNode? value = current.GetSequenceItem(segment.Index);
                if (value == null)
                {
                    return null;
                }

                current = value;
            }
            else
            {
                return null;
            }

            index = index + 1;
        }

        return current;
    }

    internal YamlNode? FindPathParent(YamlPath? path)
    {
        if (path == null || !path.Valid || path.Segments.Count == 0)
        {
            return null;
        }

        YamlPath parentPath = new YamlPath();
        int index = 0;
        while (index + 1 < path.Segments.Count)
        {
            parentPath.Segments.Add(path.Segments[index].Copy());
            index = index + 1;
        }

        return FindParsedPath(parentPath);
    }

    /// <summary>
    /// Replaces the node selected by a path without adding missing mapping keys.
    /// </summary>
    /// <param name="pathText">The path text to parse and follow.</param>
    /// <param name="value">The value to process.</param>
    /// <returns>True when the path selected an existing mapping value or sequence item and it was replaced; otherwise false.</returns>
    internal bool ReplacePath(string? pathText, YamlNode? value)
    {
        return ReplaceParsedPath(YamlPathParser.Parse(pathText), value);
    }

    /// <summary>
    /// Replaces the node selected by a path without adding missing mapping keys.
    /// </summary>
    /// <param name="path">The parsed path to follow.</param>
    /// <param name="value">The value to process.</param>
    /// <returns>True when the valid path selected an existing mapping value or sequence item and it was replaced; otherwise false.</returns>
    internal bool ReplaceParsedPath(YamlPath? path, YamlNode? value)
    {
        if (path == null || !path.Valid || path.Segments.Count == 0 || value == null)
        {
            return false;
        }

        YamlNode? parent = FindPathParent(path);
        if (parent == null)
        {
            return false;
        }

        YamlPathSegment last = path.Segments[path.Segments.Count - 1];
        if (last.Kind == YamlPathSegmentKind.MappingKey)
        {
            return parent.UpdateMappingValue(last.Key, value);
        }

        if (last.Kind == YamlPathSegmentKind.SequenceIndex)
        {
            return parent.ReplaceSequenceItem(last.Index, value);
        }

        return false;
    }

    /// <summary>
    /// Replaces the existing node selected by a path; this method has the same behavior as <see cref="ReplacePath(string, YamlNode)"/>.
    /// </summary>
    /// <param name="pathText">The path text to parse and follow.</param>
    /// <param name="value">The value to process.</param>
    /// <returns>True when the path selected an existing mapping value or sequence item and it was replaced; otherwise false.</returns>
    internal bool UpdatePath(string? pathText, YamlNode? value)
    {
        return ReplacePath(pathText, value);
    }

    /// <summary>
    /// Replaces the existing node selected by a path; this method has the same behavior as <see cref="ReplaceParsedPath(YamlPath, YamlNode)"/>.
    /// </summary>
    /// <param name="path">The parsed path to follow.</param>
    /// <param name="value">The value to process.</param>
    /// <returns>True when the path selected an existing mapping value or sequence item and it was replaced; otherwise false.</returns>
    internal bool UpdateParsedPath(YamlPath? path, YamlNode? value)
    {
        return ReplaceParsedPath(path, value);
    }

    /// <summary>
    /// Removes the sequence item or mapping pair selected by a path.
    /// </summary>
    /// <param name="pathText">The path text to parse and follow.</param>
    /// <returns>True when the path selected an existing sequence item or mapping pair and it was removed; otherwise false.</returns>
    internal bool RemovePath(string? pathText)
    {
        return RemoveParsedPath(YamlPathParser.Parse(pathText));
    }

    /// <summary>
    /// Removes the sequence item or mapping pair selected by a path.
    /// </summary>
    /// <param name="path">The parsed path to follow.</param>
    /// <returns>True when the valid path selected an existing sequence item or mapping pair and it was removed; otherwise false.</returns>
    internal bool RemoveParsedPath(YamlPath? path)
    {
        if (path == null || !path.Valid || path.Segments.Count == 0)
        {
            return false;
        }

        YamlNode? parent = FindPathParent(path);
        if (parent == null)
        {
            return false;
        }

        YamlPathSegment last = path.Segments[path.Segments.Count - 1];
        if (last.Kind == YamlPathSegmentKind.MappingKey)
        {
            return parent.RemoveMappingKey(last.Key);
        }

        if (last.Kind == YamlPathSegmentKind.SequenceIndex)
        {
            return parent.RemoveSequenceItem(last.Index);
        }

        return false;
    }

    /// <summary>
    /// Returns the requested stored value, or null when it is unavailable.
    /// </summary>
    /// <param name="index">The zero-based source or sequence index.</param>
    /// <returns>The selected child, or null when this node is not a sequence or the index is outside its bounds.</returns>
    internal YamlNode? GetSequenceItem(int index)
    {
        if (Kind != YamlNodeKind.Sequence || index < 0 || index >= Children.Count)
        {
            return null;
        }

        return Children[index];
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="child">The sequence child node to process.</param>
    /// <returns>True when this node is a sequence and the non-null child was appended; otherwise false.</returns>
    internal bool AddSequenceItem(YamlNode? child)
    {
        if (Kind != YamlNodeKind.Sequence || child == null)
        {
            return false;
        }

        Children.Add(child);
        return true;
    }

    /// <summary>
    /// Inserts the supplied value at the requested zero-based position.
    /// </summary>
    /// <param name="index">The zero-based source or sequence index.</param>
    /// <param name="child">The sequence child node to process.</param>
    /// <returns>True when this node is a sequence and the child was inserted at a valid position; otherwise false.</returns>
    internal bool InsertSequenceItem(int index, YamlNode? child)
    {
        if (Kind != YamlNodeKind.Sequence || child == null || index < 0 || index > Children.Count)
        {
            return false;
        }

        Children.Insert(index, child);
        return true;
    }

    /// <summary>
    /// Updates an existing value and reports whether a change was made.
    /// </summary>
    /// <param name="index">The zero-based source or sequence index.</param>
    /// <param name="child">The sequence child node to process.</param>
    /// <returns>True when an existing sequence item was replaced; otherwise false.</returns>
    internal bool UpdateSequenceItem(int index, YamlNode? child)
    {
        return ReplaceSequenceItem(index, child);
    }

    /// <summary>
    /// Replaces an existing value and reports whether a change was made.
    /// </summary>
    /// <param name="index">The zero-based source or sequence index.</param>
    /// <param name="child">The sequence child node to process.</param>
    /// <returns>True when this node is a sequence and an existing item was replaced with a non-null child; otherwise false.</returns>
    internal bool ReplaceSequenceItem(int index, YamlNode? child)
    {
        if (Kind != YamlNodeKind.Sequence || child == null || index < 0 || index >= Children.Count)
        {
            return false;
        }

        Children[index] = child;
        return true;
    }

    /// <summary>
    /// Removes the matching value and reports whether it was found.
    /// </summary>
    /// <param name="index">The zero-based source or sequence index.</param>
    /// <returns>True when this node is a sequence and an item existed at the supplied index; otherwise false.</returns>
    internal bool RemoveSequenceItem(int index)
    {
        if (Kind != YamlNodeKind.Sequence || index < 0 || index >= Children.Count)
        {
            return false;
        }

        Children.RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Finds the zero-based position of the first scalar mapping key whose text matches exactly.
    /// </summary>
    /// <param name="key">The mapping key or path key.</param>
    /// <returns>The resulting YAML pair.</returns>
    internal YamlPair? FindMappingPair(string key)
    {
        int index = FindMappingPairIndex(key);
        if (index < 0)
        {
            return null;
        }

        return Pairs[index];
    }

    /// <summary>
    /// Finds the first matching value, or returns null when no match exists.
    /// </summary>
    /// <param name="key">The mapping key or path key.</param>
    /// <returns>The pair index, or <c>-1</c> when this node is not a mapping or the key was not found.</returns>
    internal int FindMappingPairIndex(string key)
    {
        if (Kind != YamlNodeKind.Mapping)
        {
            return -1;
        }

        int index = 0;
        while (index < Pairs.Count)
        {
            YamlNode? pairKey = Pairs[index].Key;
            if (pairKey != null && pairKey.Kind == YamlNodeKind.Scalar && pairKey.Value == key)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    /// <summary>
    /// Finds the first matching value, or returns null when no match exists.
    /// </summary>
    /// <param name="key">The mapping key or path key.</param>
    /// <returns>The resulting YAML node.</returns>
    internal YamlNode? FindMappingValue(string key)
    {
        YamlPair? pair = FindMappingPair(key);
        if (pair == null)
        {
            return null;
        }

        return pair.Value;
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="pair">The mapping pair to process.</param>
    /// <returns>True when this node is a mapping and the non-null pair was appended; otherwise false.</returns>
    internal bool AddMappingPair(YamlPair? pair)
    {
        if (Kind != YamlNodeKind.Mapping || pair == null)
        {
            return false;
        }

        pair.RefreshComplexKey();
        Pairs.Add(pair);
        return true;
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="key">The mapping key or path key.</param>
    /// <param name="value">The value to process.</param>
    /// <returns>True when a pair containing the scalar key and supplied value was appended to a mapping; otherwise false.</returns>
    internal bool AddMappingValue(string? key, YamlNode? value)
    {
        return AddMappingPair(YamlNodeFactory.Pair(key, value));
    }

    /// <summary>
    /// Inserts the supplied value at the requested zero-based position.
    /// </summary>
    /// <param name="index">The zero-based source or sequence index.</param>
    /// <param name="pair">The mapping pair to process.</param>
    /// <returns>True when this node is a mapping and the pair was inserted at a valid position; otherwise false.</returns>
    internal bool InsertMappingPair(int index, YamlPair? pair)
    {
        if (Kind != YamlNodeKind.Mapping || pair == null || index < 0 || index > Pairs.Count)
        {
            return false;
        }

        pair.RefreshComplexKey();
        Pairs.Insert(index, pair);
        return true;
    }

    /// <summary>
    /// Updates an existing value and reports whether a change was made.
    /// </summary>
    /// <param name="key">The mapping key or path key.</param>
    /// <param name="value">The value to process.</param>
    /// <returns>True when the first matching scalar key existed and its value was replaced; otherwise false.</returns>
    internal bool UpdateMappingValue(string key, YamlNode? value)
    {
        if (Kind != YamlNodeKind.Mapping || value == null)
        {
            return false;
        }

        int index = FindMappingPairIndex(key);
        if (index < 0)
        {
            return false;
        }

        Pairs[index].Value = value;
        Pairs[index].RefreshComplexKey();
        Pairs[index].RefreshSpanFromNodes();
        return true;
    }

    /// <summary>
    /// Replaces an existing value and reports whether a change was made.
    /// </summary>
    /// <param name="key">The mapping key or path key.</param>
    /// <param name="pair">The mapping pair to process.</param>
    /// <returns>True when the first matching scalar key existed and its entire pair was replaced; otherwise false.</returns>
    internal bool ReplaceMappingPair(string key, YamlPair? pair)
    {
        if (Kind != YamlNodeKind.Mapping || pair == null)
        {
            return false;
        }

        int index = FindMappingPairIndex(key);
        if (index < 0)
        {
            return false;
        }

        pair.RefreshComplexKey();
        Pairs[index] = pair;
        return true;
    }

    /// <summary>
    /// Removes the matching value and reports whether it was found.
    /// </summary>
    /// <param name="key">The mapping key or path key.</param>
    /// <returns>True when the first matching scalar key was removed; otherwise false.</returns>
    internal bool RemoveMappingKey(string key)
    {
        int index = FindMappingPairIndex(key);
        if (index < 0)
        {
            return false;
        }

        Pairs.RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="comment">The comment text, with or without a leading number sign.</param>
    internal void AddLeadingComment(string? comment)
    {
        AddTrivia(YamlTrivia.Create(YamlTriviaKind.Comment, YamlTriviaPlacement.Before, comment, YamlSourceSpan.Empty()));
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="comment">The comment text, with or without a leading number sign.</param>
    internal void AddTrailingComment(string? comment)
    {
        AddTrivia(YamlTrivia.Create(YamlTriviaKind.Comment, YamlTriviaPlacement.Inline, comment, YamlSourceSpan.Empty()));
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="blankLine">The preserved blank-line marker.</param>
    internal void AddLeadingBlankLine(string? blankLine)
    {
        AddTrivia(YamlTrivia.Create(YamlTriviaKind.BlankLine, YamlTriviaPlacement.Before, blankLine, YamlSourceSpan.Empty()));
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="blankLine">The preserved blank-line marker.</param>
    internal void AddTrailingBlankLine(string? blankLine)
    {
        AddTrivia(YamlTrivia.Create(YamlTriviaKind.BlankLine, YamlTriviaPlacement.Following, blankLine, YamlSourceSpan.Empty()));
    }

    internal void AddTrivia(YamlTrivia? trivia)
    {
        if (trivia == null)
        {
            return;
        }

        YamlTrivia copy = trivia.Copy();
        Trivia.Add(copy);
        if (copy.Kind == YamlTriviaKind.BlankLine)
        {
            if (copy.Placement == YamlTriviaPlacement.Before)
            {
                LeadingBlankLines.Add(copy.Text);
            }
            else
            {
                TrailingBlankLines.Add(copy.Text);
            }
        }
        else if (copy.Kind == YamlTriviaKind.Comment)
        {
            if (copy.Placement == YamlTriviaPlacement.Before)
            {
                LeadingComments.Add(copy.Text);
            }
            else
            {
                TrailingComments.Add(copy.Text);
            }
        }
    }

    internal bool RemoveTrivia(int index)
    {
        if (index < 0 || index >= Trivia.Count)
        {
            return false;
        }

        YamlTrivia trivia = Trivia[index];
        Trivia.RemoveAt(index);
        if (trivia.Kind == YamlTriviaKind.BlankLine)
        {
            if (trivia.Placement == YamlTriviaPlacement.Before)
            {
                YamlTrivia.RemoveFirstText(LeadingBlankLines, trivia.Text);
            }
            else
            {
                YamlTrivia.RemoveFirstText(TrailingBlankLines, trivia.Text);
            }
        }
        else if (trivia.Kind == YamlTriviaKind.Comment)
        {
            if (trivia.Placement == YamlTriviaPlacement.Before)
            {
                YamlTrivia.RemoveFirstText(LeadingComments, trivia.Text);
            }
            else
            {
                YamlTrivia.RemoveFirstText(TrailingComments, trivia.Text);
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether this node contains scalar text.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool IsScalar()
    {
        return Kind == YamlNodeKind.Scalar;
    }

    /// <summary>
    /// Determines whether this node is an ordered sequence.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool IsSequence()
    {
        return Kind == YamlNodeKind.Sequence;
    }

    /// <summary>
    /// Determines whether this node is an ordered mapping.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool IsMapping()
    {
        return Kind == YamlNodeKind.Mapping;
    }

    /// <summary>
    /// Determines whether this node is an alias reference.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool IsAlias()
    {
        return Kind == YamlNodeKind.Alias;
    }
}
