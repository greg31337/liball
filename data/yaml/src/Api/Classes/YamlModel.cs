using System.Collections.Generic;

namespace yaml;

/// <summary>
/// Provides instance-based construction, conversion, query, and transformation operations for YAML data objects.
/// </summary>
public class YamlModel : IYamlModel
{
    /// <summary>
    /// Initializes a stateless YAML model service.
    /// </summary>
    public YamlModel()
    {
    }

    /// <summary>Creates an empty operation result.</summary>
    /// <returns>The created operation result.</returns>
    public YamlResult CreateResult()
    {
        return new YamlResult();
    }

    /// <summary>Creates a tag directive.</summary>
    /// <param name="handle">The tag handle including exclamation-mark delimiters.</param>
    /// <param name="prefix">The prefix expanded by the tag handle.</param>
    /// <param name="span">The source span; pass null to use an empty span.</param>
    /// <returns>The created tag directive.</returns>
    public YamlTagDirective CreateTagDirective(string? handle, string? prefix, YamlSourceSpan? span)
    {
        return YamlTagDirective.Create(handle, prefix, span);
    }

    /// <summary>Creates an empty node.</summary>
    /// <param name="span">The source span; pass null to use an empty span.</param>
    /// <returns>The created node.</returns>
    public YamlNode CreateEmptyNode(YamlSourceSpan? span)
    {
        return YamlNode.Empty(span);
    }

    /// <summary>Creates a scalar node.</summary>
    /// <param name="value">The scalar text.</param>
    /// <param name="style">The scalar presentation style.</param>
    /// <param name="span">The source span; pass null to use an empty span.</param>
    /// <returns>The created node.</returns>
    public YamlNode CreateScalarNode(string? value, YamlScalarStyle style, YamlSourceSpan? span)
    {
        return YamlNode.Scalar(value, style, span);
    }

    /// <summary>Creates a sequence node.</summary>
    /// <param name="style">The collection presentation style.</param>
    /// <param name="span">The source span; pass null to use an empty span.</param>
    /// <returns>The created node.</returns>
    public YamlNode CreateSequenceNode(YamlCollectionStyle style, YamlSourceSpan? span)
    {
        return YamlNode.Sequence(style, span);
    }

    /// <summary>Creates a mapping node.</summary>
    /// <param name="style">The collection presentation style.</param>
    /// <param name="span">The source span; pass null to use an empty span.</param>
    /// <returns>The created node.</returns>
    public YamlNode CreateMappingNode(YamlCollectionStyle style, YamlSourceSpan? span)
    {
        return YamlNode.Mapping(style, span);
    }

    /// <summary>Creates an alias node.</summary>
    /// <param name="aliasName">The referenced anchor name.</param>
    /// <param name="span">The source span; pass null to use an empty span.</param>
    /// <returns>The created node.</returns>
    public YamlNode CreateAliasNode(string? aliasName, YamlSourceSpan? span)
    {
        return YamlNode.Alias(aliasName, span);
    }

    /// <summary>Creates a mapping pair.</summary>
    /// <param name="key">The key node.</param>
    /// <param name="value">The value node.</param>
    /// <param name="span">The source span; pass null to derive it from the nodes.</param>
    /// <returns>The created pair.</returns>
    public YamlPair CreatePair(YamlNode? key, YamlNode? value, YamlSourceSpan? span)
    {
        return YamlPair.Create(key, value, span);
    }

    /// <summary>Converts a supported CLR value to a YAML node graph.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The created node graph.</returns>
    public YamlNode FromJsonValue(object? value)
    {
        return YamlJsonConverter.FromJsonValue(value);
    }

    /// <summary>Converts a YAML node graph to JSON-compatible CLR values.</summary>
    /// <param name="node">The node graph to convert.</param>
    /// <returns>The converted value.</returns>
    public object? ToJsonValue(YamlNode? node)
    {
        return YamlJsonConverter.ToJsonValue(node);
    }

    /// <summary>Parses a YAML model path.</summary>
    /// <param name="text">The path text.</param>
    /// <returns>The parsed path and any validation error.</returns>
    public YamlPath ParsePath(string? text)
    {
        return YamlPathParser.Parse(text);
    }

    /// <summary>Finds a node by path.</summary>
    /// <param name="node">The root node.</param>
    /// <param name="path">The path text.</param>
    /// <returns>The matching node, or null.</returns>
    public YamlNode? FindPath(YamlNode? node, string? path)
    {
        if (node == null)
        {
            return null;
        }

        return node.FindPath(path);
    }

    /// <summary>Replaces a node selected by path.</summary>
    /// <param name="node">The root node.</param>
    /// <param name="path">The path text.</param>
    /// <param name="value">The replacement node.</param>
    /// <returns>True when a node was replaced.</returns>
    public bool ReplacePath(YamlNode? node, string? path, YamlNode? value)
    {
        if (node == null)
        {
            return false;
        }

        return node.ReplacePath(path, value);
    }

    /// <summary>Removes a node selected by path.</summary>
    /// <param name="node">The root node.</param>
    /// <param name="path">The path text.</param>
    /// <returns>True when a node was removed.</returns>
    public bool RemovePath(YamlNode? node, string? path)
    {
        if (node == null)
        {
            return false;
        }

        return node.RemovePath(path);
    }

    /// <summary>Returns the number of items in a sequence node.</summary>
    /// <param name="node">The sequence node.</param>
    /// <returns>The item count, or zero for another node kind or null.</returns>
    public int SequenceItemCount(YamlNode? node)
    {
        if (node == null || node.Kind != YamlNodeKind.Sequence)
        {
            return 0;
        }

        return node.Children.Count;
    }

    /// <summary>Returns a sequence item by zero-based index.</summary>
    /// <param name="node">The sequence node.</param>
    /// <param name="index">The zero-based item index.</param>
    /// <returns>The matching item, or null.</returns>
    public YamlNode? GetSequenceItem(YamlNode? node, int index)
    {
        return node == null ? null : node.GetSequenceItem(index);
    }

    /// <summary>Appends an item to a sequence.</summary>
    /// <param name="node">The sequence node.</param>
    /// <param name="item">The item to append.</param>
    /// <returns>True when the item was appended.</returns>
    public bool AddSequenceItem(YamlNode? node, YamlNode? item)
    {
        return node != null && node.AddSequenceItem(item);
    }

    /// <summary>Inserts an item into a sequence.</summary>
    /// <param name="node">The sequence node.</param>
    /// <param name="index">The zero-based insertion index.</param>
    /// <param name="item">The item to insert.</param>
    /// <returns>True when the item was inserted.</returns>
    public bool InsertSequenceItem(YamlNode? node, int index, YamlNode? item)
    {
        return node != null && node.InsertSequenceItem(index, item);
    }

    /// <summary>Replaces an indexed sequence item.</summary>
    /// <param name="node">The sequence node.</param>
    /// <param name="index">The zero-based item index.</param>
    /// <param name="item">The replacement item.</param>
    /// <returns>True when the item was replaced.</returns>
    public bool SetSequenceItem(YamlNode? node, int index, YamlNode? item)
    {
        return node != null && node.ReplaceSequenceItem(index, item);
    }

    /// <summary>Removes an indexed sequence item.</summary>
    /// <param name="node">The sequence node.</param>
    /// <param name="index">The zero-based item index.</param>
    /// <returns>True when the item was removed.</returns>
    public bool RemoveSequenceItem(YamlNode? node, int index)
    {
        return node != null && node.RemoveSequenceItem(index);
    }

    /// <summary>Returns the number of ordered pairs in a mapping node.</summary>
    /// <param name="node">The mapping node.</param>
    /// <returns>The pair count, or zero for another node kind or null.</returns>
    public int MappingPairCount(YamlNode? node)
    {
        if (node == null || node.Kind != YamlNodeKind.Mapping)
        {
            return 0;
        }

        return node.Pairs.Count;
    }

    /// <summary>Returns an ordered mapping pair by zero-based index.</summary>
    /// <param name="node">The mapping node.</param>
    /// <param name="index">The zero-based pair index.</param>
    /// <returns>The matching pair, or null.</returns>
    public YamlPair? GetMappingPair(YamlNode? node, int index)
    {
        if (node == null || node.Kind != YamlNodeKind.Mapping || index < 0 || index >= node.Pairs.Count)
        {
            return null;
        }

        return node.Pairs[index];
    }

    /// <summary>Finds the first pair with a matching scalar key.</summary>
    /// <param name="node">The mapping node.</param>
    /// <param name="key">The scalar key text.</param>
    /// <returns>The matching pair, or null.</returns>
    public YamlPair? FindMappingPair(YamlNode? node, string? key)
    {
        return node == null ? null : node.FindMappingPair(key == null ? "" : key);
    }

    /// <summary>Finds the first value with a matching scalar key.</summary>
    /// <param name="node">The mapping node.</param>
    /// <param name="key">The scalar key text.</param>
    /// <returns>The matching value, or null.</returns>
    public YamlNode? FindMappingValue(YamlNode? node, string? key)
    {
        return node == null ? null : node.FindMappingValue(key == null ? "" : key);
    }

    /// <summary>Appends an ordered pair to a mapping.</summary>
    /// <param name="node">The mapping node.</param>
    /// <param name="pair">The pair to append.</param>
    /// <returns>True when the pair was appended.</returns>
    public bool AddMappingPair(YamlNode? node, YamlPair? pair)
    {
        return node != null && node.AddMappingPair(pair);
    }

    /// <summary>Inserts an ordered pair into a mapping.</summary>
    /// <param name="node">The mapping node.</param>
    /// <param name="index">The zero-based insertion index.</param>
    /// <param name="pair">The pair to insert.</param>
    /// <returns>True when the pair was inserted.</returns>
    public bool InsertMappingPair(YamlNode? node, int index, YamlPair? pair)
    {
        return node != null && node.InsertMappingPair(index, pair);
    }

    /// <summary>Replaces an ordered pair by zero-based index.</summary>
    /// <param name="node">The mapping node.</param>
    /// <param name="index">The zero-based pair index.</param>
    /// <param name="pair">The replacement pair.</param>
    /// <returns>True when the pair was replaced.</returns>
    public bool SetMappingPair(YamlNode? node, int index, YamlPair? pair)
    {
        if (node == null || node.Kind != YamlNodeKind.Mapping || pair == null || index < 0 || index >= node.Pairs.Count)
        {
            return false;
        }

        pair.RefreshComplexKey();
        node.Pairs[index] = pair;
        return true;
    }

    /// <summary>Removes an ordered pair by zero-based index.</summary>
    /// <param name="node">The mapping node.</param>
    /// <param name="index">The zero-based pair index.</param>
    /// <returns>True when the pair was removed.</returns>
    public bool RemoveMappingPair(YamlNode? node, int index)
    {
        if (node == null || node.Kind != YamlNodeKind.Mapping || index < 0 || index >= node.Pairs.Count)
        {
            return false;
        }

        node.Pairs.RemoveAt(index);
        return true;
    }

    /// <summary>Appends a scalar-keyed value to a mapping.</summary>
    /// <param name="node">The mapping node.</param>
    /// <param name="key">The scalar key text.</param>
    /// <param name="value">The value to append.</param>
    /// <returns>True when the value was appended.</returns>
    public bool AddMappingValue(YamlNode? node, string? key, YamlNode? value)
    {
        return node != null && node.AddMappingValue(key, value);
    }

    /// <summary>Updates the first value with a matching scalar key.</summary>
    /// <param name="node">The mapping node.</param>
    /// <param name="key">The scalar key text.</param>
    /// <param name="value">The replacement value.</param>
    /// <returns>True when the value was updated.</returns>
    public bool SetMappingValue(YamlNode? node, string? key, YamlNode? value)
    {
        return node != null && node.UpdateMappingValue(key == null ? "" : key, value);
    }

    /// <summary>Removes the first pair with a matching scalar key.</summary>
    /// <param name="node">The mapping node.</param>
    /// <param name="key">The scalar key text.</param>
    /// <returns>True when the pair was removed.</returns>
    public bool RemoveMappingValue(YamlNode? node, string? key)
    {
        return node != null && node.RemoveMappingKey(key == null ? "" : key);
    }

    /// <summary>Finds a node using an already parsed path.</summary>
    /// <param name="node">The root node.</param>
    /// <param name="path">The parsed path.</param>
    /// <returns>The matching node, or null.</returns>
    public YamlNode? FindParsedPath(YamlNode? node, YamlPath? path)
    {
        return node == null ? null : node.FindParsedPath(path);
    }

    /// <summary>Replaces a node using an already parsed path.</summary>
    /// <param name="node">The root node.</param>
    /// <param name="path">The parsed path.</param>
    /// <param name="value">The replacement node.</param>
    /// <returns>True when a node was replaced.</returns>
    public bool ReplaceParsedPath(YamlNode? node, YamlPath? path, YamlNode? value)
    {
        return node != null && node.ReplaceParsedPath(path, value);
    }

    /// <summary>Removes a node using an already parsed path.</summary>
    /// <param name="node">The root node.</param>
    /// <param name="path">The parsed path.</param>
    /// <returns>True when a node was removed.</returns>
    public bool RemoveParsedPath(YamlNode? node, YamlPath? path)
    {
        return node != null && node.RemoveParsedPath(path);
    }

    /// <summary>Changes the presentation style of a scalar node.</summary>
    /// <param name="node">The scalar node.</param>
    /// <param name="style">The requested scalar style.</param>
    /// <returns>True when the style was assigned.</returns>
    public bool SetScalarStyle(YamlNode? node, YamlScalarStyle style)
    {
        if (node == null || node.Kind != YamlNodeKind.Scalar)
        {
            return false;
        }

        node.ScalarStyle = style;
        return true;
    }

    /// <summary>Changes the presentation style of a sequence or mapping node.</summary>
    /// <param name="node">The collection node.</param>
    /// <param name="style">The requested collection style.</param>
    /// <returns>True when the style was assigned.</returns>
    public bool SetCollectionStyle(YamlNode? node, YamlCollectionStyle style)
    {
        if (node == null || node.Kind != YamlNodeKind.Sequence && node.Kind != YamlNodeKind.Mapping)
        {
            return false;
        }

        node.CollectionStyle = style;
        return true;
    }

    /// <summary>Assigns or clears a node tag.</summary>
    /// <param name="node">The node to update.</param>
    /// <param name="tag">The tag text, or null to clear it.</param>
    /// <returns>True when a node was updated.</returns>
    public bool SetTag(YamlNode? node, string? tag)
    {
        if (node == null)
        {
            return false;
        }

        node.Tag = tag == null ? "" : tag;
        node.TagPresentation = "";
        node.ParsedTag = "";
        return true;
    }

    /// <summary>Finds a document anchor by name.</summary>
    /// <param name="document">The document whose anchor table should be inspected.</param>
    /// <param name="name">The anchor name.</param>
    /// <returns>The anchored node, or null.</returns>
    public YamlNode? FindAnchor(YamlDocument? document, string? name)
    {
        if (document == null || name == null || name == "")
        {
            return null;
        }

        return document.Anchors.Find(name);
    }

    /// <summary>Rebuilds a document anchor table from its reachable node graph.</summary>
    /// <param name="document">The document to update.</param>
    /// <returns>The number of registered anchors.</returns>
    public int RebuildAnchorTable(YamlDocument? document)
    {
        if (document == null)
        {
            return 0;
        }

        document.Anchors.Clear();
        CollectAnchors(document.Root, document.Anchors, new Dictionary<YamlNode, YamlNode>());
        return document.Anchors.Count();
    }

    /// <summary>Renames an anchor and every alias that references it.</summary>
    /// <param name="document">The document to update.</param>
    /// <param name="oldName">The current anchor name.</param>
    /// <param name="newName">The replacement anchor name.</param>
    /// <returns>True when the anchor was renamed or already had the requested name.</returns>
    public bool RenameAnchor(YamlDocument? document, string? oldName, string? newName)
    {
        string current = oldName == null ? "" : oldName;
        string replacement = newName == null ? "" : newName;
        if (document == null || !IsValidAnchorName(current) || !IsValidAnchorName(replacement))
        {
            return false;
        }

        RebuildAnchorTable(document);
        YamlNode? target = document.Anchors.Find(current);
        if (target == null)
        {
            return false;
        }

        YamlNode? conflict = document.Anchors.Find(replacement);
        if (conflict != null && conflict != target)
        {
            return false;
        }

        if (current == replacement)
        {
            return true;
        }

        target.Anchor = replacement;
        if (document.Root != null)
        {
            document.Root.RenameAliases(current, replacement);
        }

        RebuildAnchorTable(document);
        ResolveAliases(document);
        return true;
    }

    /// <summary>Renames every matching alias to an existing document anchor.</summary>
    /// <param name="document">The document to update.</param>
    /// <param name="oldName">The alias name to replace.</param>
    /// <param name="newName">The existing anchor name to reference.</param>
    /// <returns>The number of aliases changed.</returns>
    public int RenameAliases(YamlDocument? document, string? oldName, string? newName)
    {
        string current = oldName == null ? "" : oldName;
        string replacement = newName == null ? "" : newName;
        if (document == null || document.Root == null || current == "" || !IsValidAnchorName(replacement))
        {
            return 0;
        }

        RebuildAnchorTable(document);
        YamlNode? target = document.Anchors.Find(replacement);
        if (target == null)
        {
            return 0;
        }

        return document.Root.RetargetAliases(current, target);
    }

    /// <summary>Renames one alias to an existing document anchor.</summary>
    /// <param name="document">The document containing the alias and target.</param>
    /// <param name="alias">The alias node to update.</param>
    /// <param name="newName">The existing anchor name to reference.</param>
    /// <returns>True when the alias was updated.</returns>
    public bool RenameAlias(YamlDocument? document, YamlNode? alias, string? newName)
    {
        string replacement = newName == null ? "" : newName;
        if (document == null || alias == null || alias.Kind != YamlNodeKind.Alias || !IsValidAnchorName(replacement))
        {
            return false;
        }

        if (!ContainsReachableNode(document.Root, alias, new Dictionary<YamlNode, YamlNode>()))
        {
            return false;
        }

        RebuildAnchorTable(document);
        YamlNode? target = document.Anchors.Find(replacement);
        if (target == null)
        {
            return false;
        }

        alias.AliasName = replacement;
        alias.AliasTarget = target;
        return true;
    }

    /// <summary>Retargets one alias to an anchored node in its document.</summary>
    /// <param name="document">The document containing the alias and target.</param>
    /// <param name="alias">The alias node to update.</param>
    /// <param name="target">The anchored target node.</param>
    /// <returns>True when the alias was retargeted.</returns>
    public bool RetargetAlias(YamlDocument? document, YamlNode? alias, YamlNode? target)
    {
        if (document == null || alias == null || target == null || alias.Kind != YamlNodeKind.Alias)
        {
            return false;
        }

        if (!ContainsReachableNode(document.Root, alias, new Dictionary<YamlNode, YamlNode>()))
        {
            return false;
        }

        RebuildAnchorTable(document);
        if (target.Anchor == "" || document.Anchors.Find(target.Anchor) != target)
        {
            return false;
        }

        return alias.RetargetAlias(target);
    }

    /// <summary>Retargets every alias that currently references one target.</summary>
    /// <param name="document">The document to update.</param>
    /// <param name="oldTarget">The current alias target.</param>
    /// <param name="newTarget">The replacement anchored target.</param>
    /// <returns>The number of aliases changed.</returns>
    public int RetargetAliases(YamlDocument? document, YamlNode? oldTarget, YamlNode? newTarget)
    {
        if (document == null || document.Root == null || oldTarget == null || newTarget == null)
        {
            return 0;
        }

        RebuildAnchorTable(document);
        if (newTarget.Anchor == "" || document.Anchors.Find(newTarget.Anchor) != newTarget)
        {
            return 0;
        }

        return document.Root.RetargetAliasesByTarget(oldTarget, newTarget);
    }

    /// <summary>Resolves one alias name against its document anchor table.</summary>
    /// <param name="document">The document containing the alias.</param>
    /// <param name="alias">The alias node to resolve.</param>
    /// <returns>True when a target was found and assigned.</returns>
    public bool ResolveAlias(YamlDocument? document, YamlNode? alias)
    {
        if (document == null || alias == null || alias.Kind != YamlNodeKind.Alias)
        {
            return false;
        }

        if (!ContainsReachableNode(document.Root, alias, new Dictionary<YamlNode, YamlNode>()))
        {
            return false;
        }

        RebuildAnchorTable(document);
        YamlNode? target = document.Anchors.Find(alias.AliasName);
        alias.AliasTarget = target;
        return target != null;
    }

    /// <summary>Resolves every reachable alias and clears targets for unknown names.</summary>
    /// <param name="document">The document to update.</param>
    /// <returns>The number of aliases resolved.</returns>
    public int ResolveAliases(YamlDocument? document)
    {
        if (document == null)
        {
            return 0;
        }

        RebuildAnchorTable(document);
        return ResolveAliasesInGraph(document.Root, document.Anchors, new Dictionary<YamlNode, YamlNode>());
    }

    internal static bool IsValidAnchorName(string name)
    {
        if (name == null || name == "")
        {
            return false;
        }

        int index = 0;
        while (index < name.Length)
        {
            char value = name[index];
            if (value <= ' ' || value == ',' || value == '[' || value == ']' || value == '{' || value == '}')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static void CollectAnchors(YamlNode? node, YamlAnchorTable table, Dictionary<YamlNode, YamlNode> visited)
    {
        if (node == null || table == null || visited == null || visited.ContainsKey(node))
        {
            return;
        }

        visited[node] = node;
        if (node.Anchor != "")
        {
            table.Register(node.Anchor, node, node.Span);
        }

        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            CollectAnchors(node.Children[childIndex], table, visited);
            childIndex = childIndex + 1;
        }

        int pairIndex = 0;
        while (pairIndex < node.Pairs.Count)
        {
            YamlPair pair = node.Pairs[pairIndex];
            CollectAnchors(pair.Key, table, visited);
            CollectAnchors(pair.Value, table, visited);
            pairIndex = pairIndex + 1;
        }
    }

    internal static bool ContainsReachableNode(YamlNode? node, YamlNode target, Dictionary<YamlNode, YamlNode> visited)
    {
        if (node == null || target == null || visited == null || visited.ContainsKey(node))
        {
            return false;
        }

        if (node == target)
        {
            return true;
        }

        visited[node] = node;
        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            if (ContainsReachableNode(node.Children[childIndex], target, visited))
            {
                return true;
            }

            childIndex = childIndex + 1;
        }

        int pairIndex = 0;
        while (pairIndex < node.Pairs.Count)
        {
            YamlPair pair = node.Pairs[pairIndex];
            if (ContainsReachableNode(pair.Key, target, visited) || ContainsReachableNode(pair.Value, target, visited))
            {
                return true;
            }

            pairIndex = pairIndex + 1;
        }

        return false;
    }

    internal static int ResolveAliasesInGraph(YamlNode? node, YamlAnchorTable table, Dictionary<YamlNode, YamlNode> visited)
    {
        if (node == null || table == null || visited == null || visited.ContainsKey(node))
        {
            return 0;
        }

        visited[node] = node;
        int count = 0;
        if (node.Kind == YamlNodeKind.Alias)
        {
            node.AliasTarget = table.Find(node.AliasName);
            if (node.AliasTarget != null)
            {
                count = count + 1;
            }
        }

        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            count = count + ResolveAliasesInGraph(node.Children[childIndex], table, visited);
            childIndex = childIndex + 1;
        }

        int pairIndex = 0;
        while (pairIndex < node.Pairs.Count)
        {
            YamlPair pair = node.Pairs[pairIndex];
            count = count + ResolveAliasesInGraph(pair.Key, table, visited);
            count = count + ResolveAliasesInGraph(pair.Value, table, visited);
            pairIndex = pairIndex + 1;
        }

        return count;
    }

    /// <summary>Adds a placed comment to a document.</summary>
    /// <param name="document">The document to update.</param>
    /// <param name="text">The comment text.</param>
    /// <param name="placement">The comment placement.</param>
    /// <returns>True when the comment was added.</returns>
    public bool AddDocumentComment(YamlDocument? document, string? text, YamlTriviaPlacement placement)
    {
        if (document == null || placement == YamlTriviaPlacement.Inline || !YamlTrivia.IsCommentPlacement(placement))
        {
            return false;
        }

        document.AddTrivia(YamlTrivia.Create(YamlTriviaKind.Comment, placement, text, YamlSourceSpan.Empty()));
        return true;
    }

    /// <summary>Adds a placed comment to a node.</summary>
    /// <param name="node">The node to update.</param>
    /// <param name="text">The comment text.</param>
    /// <param name="placement">The comment placement.</param>
    /// <returns>True when the comment was added.</returns>
    public bool AddNodeComment(YamlNode? node, string? text, YamlTriviaPlacement placement)
    {
        if (node == null || !YamlTrivia.IsCommentPlacement(placement))
        {
            return false;
        }

        node.AddTrivia(YamlTrivia.Create(YamlTriviaKind.Comment, placement, text, YamlSourceSpan.Empty()));
        return true;
    }

    /// <summary>Adds a placed comment to a mapping pair.</summary>
    /// <param name="pair">The pair to update.</param>
    /// <param name="text">The comment text.</param>
    /// <param name="placement">The comment placement.</param>
    /// <returns>True when the comment was added.</returns>
    public bool AddPairComment(YamlPair? pair, string? text, YamlTriviaPlacement placement)
    {
        if (pair == null || !YamlTrivia.IsCommentPlacement(placement))
        {
            return false;
        }

        pair.AddTrivia(YamlTrivia.Create(YamlTriviaKind.Comment, placement, text, YamlSourceSpan.Empty()));
        return true;
    }

    /// <summary>Adds a placed blank line to a document.</summary>
    /// <param name="document">The document to update.</param>
    /// <param name="placement">The blank-line placement.</param>
    /// <returns>True when the blank line was added.</returns>
    public bool AddDocumentBlankLine(YamlDocument? document, YamlTriviaPlacement placement)
    {
        if (document == null || !YamlTrivia.IsBlankLinePlacement(placement))
        {
            return false;
        }

        document.AddTrivia(YamlTrivia.Create(YamlTriviaKind.BlankLine, placement, "", YamlSourceSpan.Empty()));
        return true;
    }

    /// <summary>Adds a placed blank line to a node.</summary>
    /// <param name="node">The node to update.</param>
    /// <param name="placement">The blank-line placement.</param>
    /// <returns>True when the blank line was added.</returns>
    public bool AddNodeBlankLine(YamlNode? node, YamlTriviaPlacement placement)
    {
        if (node == null || !YamlTrivia.IsBlankLinePlacement(placement))
        {
            return false;
        }

        node.AddTrivia(YamlTrivia.Create(YamlTriviaKind.BlankLine, placement, "", YamlSourceSpan.Empty()));
        return true;
    }

    /// <summary>Adds a placed blank line to a mapping pair.</summary>
    /// <param name="pair">The pair to update.</param>
    /// <param name="placement">The blank-line placement.</param>
    /// <returns>True when the blank line was added.</returns>
    public bool AddPairBlankLine(YamlPair? pair, YamlTriviaPlacement placement)
    {
        if (pair == null || !YamlTrivia.IsBlankLinePlacement(placement))
        {
            return false;
        }

        pair.AddTrivia(YamlTrivia.Create(YamlTriviaKind.BlankLine, placement, "", YamlSourceSpan.Empty()));
        return true;
    }

    /// <summary>Removes document trivia by zero-based index.</summary>
    /// <param name="document">The document to update.</param>
    /// <param name="index">The zero-based trivia index.</param>
    /// <returns>True when trivia was removed.</returns>
    public bool RemoveDocumentTrivia(YamlDocument? document, int index)
    {
        return document != null && document.RemoveTrivia(index);
    }

    /// <summary>Removes node trivia by zero-based index.</summary>
    /// <param name="node">The node to update.</param>
    /// <param name="index">The zero-based trivia index.</param>
    /// <returns>True when trivia was removed.</returns>
    public bool RemoveNodeTrivia(YamlNode? node, int index)
    {
        return node != null && node.RemoveTrivia(index);
    }

    /// <summary>Removes pair trivia by zero-based index.</summary>
    /// <param name="pair">The pair to update.</param>
    /// <param name="index">The zero-based trivia index.</param>
    /// <returns>True when trivia was removed.</returns>
    public bool RemovePairTrivia(YamlPair? pair, int index)
    {
        return pair != null && pair.RemoveTrivia(index);
    }

    /// <summary>Creates an independent copy of a node graph.</summary>
    /// <param name="node">The node graph to copy.</param>
    /// <returns>The copied graph, or an empty node when null.</returns>
    public YamlNode CloneNode(YamlNode? node)
    {
        if (node == null)
        {
            return YamlNode.Empty(YamlSourceSpan.Empty());
        }

        return node.Clone();
    }

    /// <summary>Applies YAML merge keys in a node graph.</summary>
    /// <param name="node">The node graph to update.</param>
    /// <returns>The number of applied merge entries.</returns>
    public int ApplyMergeKeys(YamlNode? node)
    {
        if (node == null)
        {
            return 0;
        }

        return node.ApplyMergeKeys();
    }

    /// <summary>Resolves the YAML tag for scalar text.</summary>
    /// <param name="value">The scalar text.</param>
    /// <param name="style">The scalar presentation style.</param>
    /// <param name="options">The schema settings; pass null to use defaults.</param>
    /// <returns>The resolved tag.</returns>
    public string ResolveScalarTag(string value, YamlScalarStyle style, YamlSchemaOptions? options)
    {
        return YamlScalarResolver.ResolveScalarTag(value, style, options);
    }

    /// <summary>Normalizes a standard YAML tag shorthand.</summary>
    /// <param name="tag">The tag to normalize.</param>
    /// <returns>The normalized tag.</returns>
    public string NormalizeStandardTag(string? tag)
    {
        return YamlScalarResolver.NormalizeStandardTag(tag);
    }

    /// <summary>Reports whether a tag denotes a standard YAML type.</summary>
    /// <param name="tag">The tag to inspect.</param>
    /// <returns>True for a standard tag.</returns>
    public bool IsStandardTag(string? tag)
    {
        return YamlScalarResolver.IsStandardTag(tag);
    }

    /// <summary>Applies a configured public-field naming convention.</summary>
    /// <param name="name">The field name.</param>
    /// <param name="convention">The naming convention.</param>
    /// <returns>The converted name.</returns>
    public string ApplyFieldNaming(string? name, YamlNamingConvention convention)
    {
        return YamlFieldNaming.Apply(name, convention);
    }
}
