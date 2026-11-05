namespace yaml;

/// <summary>
/// Defines construction, conversion, query, and transformation operations for YAML data objects.
/// </summary>
public interface IYamlModel
{
    /// <summary>Creates an empty operation result.</summary>
    /// <returns>The created operation result.</returns>
    public YamlResult CreateResult();

    /// <summary>Creates a tag directive.</summary>
    /// <param name="handle">The tag handle including exclamation-mark delimiters.</param>
    /// <param name="prefix">The prefix expanded by the tag handle.</param>
    /// <param name="span">The source span; pass null to use an empty span.</param>
    /// <returns>The created tag directive.</returns>
    public YamlTagDirective CreateTagDirective(string? handle, string? prefix, YamlSourceSpan? span);

    /// <summary>Creates an empty node.</summary>
    /// <param name="span">The source span; pass null to use an empty span.</param>
    /// <returns>The created node.</returns>
    public YamlNode CreateEmptyNode(YamlSourceSpan? span);

    /// <summary>Creates a scalar node.</summary>
    /// <param name="value">The scalar text.</param>
    /// <param name="style">The scalar presentation style.</param>
    /// <param name="span">The source span; pass null to use an empty span.</param>
    /// <returns>The created node.</returns>
    public YamlNode CreateScalarNode(string? value, YamlScalarStyle style, YamlSourceSpan? span);

    /// <summary>Creates a sequence node.</summary>
    /// <param name="style">The collection presentation style.</param>
    /// <param name="span">The source span; pass null to use an empty span.</param>
    /// <returns>The created node.</returns>
    public YamlNode CreateSequenceNode(YamlCollectionStyle style, YamlSourceSpan? span);

    /// <summary>Creates a mapping node.</summary>
    /// <param name="style">The collection presentation style.</param>
    /// <param name="span">The source span; pass null to use an empty span.</param>
    /// <returns>The created node.</returns>
    public YamlNode CreateMappingNode(YamlCollectionStyle style, YamlSourceSpan? span);

    /// <summary>Creates an alias node.</summary>
    /// <param name="aliasName">The referenced anchor name.</param>
    /// <param name="span">The source span; pass null to use an empty span.</param>
    /// <returns>The created node.</returns>
    public YamlNode CreateAliasNode(string? aliasName, YamlSourceSpan? span);

    /// <summary>Creates a mapping pair.</summary>
    /// <param name="key">The key node.</param>
    /// <param name="value">The value node.</param>
    /// <param name="span">The source span; pass null to derive it from the nodes.</param>
    /// <returns>The created pair.</returns>
    public YamlPair CreatePair(YamlNode? key, YamlNode? value, YamlSourceSpan? span);

    /// <summary>Converts a supported CLR value to a YAML node graph.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The created node graph.</returns>
    public YamlNode FromJsonValue(object? value);

    /// <summary>Converts a YAML node graph to JSON-compatible CLR values.</summary>
    /// <param name="node">The node graph to convert.</param>
    /// <returns>The converted value.</returns>
    public object? ToJsonValue(YamlNode? node);

    /// <summary>Parses a YAML model path.</summary>
    /// <param name="text">The path text.</param>
    /// <returns>The parsed path and any validation error.</returns>
    public YamlPath ParsePath(string? text);

    /// <summary>Finds a node by path.</summary>
    /// <param name="node">The root node.</param>
    /// <param name="path">The path text.</param>
    /// <returns>The matching node, or null.</returns>
    public YamlNode? FindPath(YamlNode? node, string? path);

    /// <summary>Replaces a node selected by path.</summary>
    /// <param name="node">The root node.</param>
    /// <param name="path">The path text.</param>
    /// <param name="value">The replacement node.</param>
    /// <returns>True when a node was replaced.</returns>
    public bool ReplacePath(YamlNode? node, string? path, YamlNode? value);

    /// <summary>Removes a node selected by path.</summary>
    /// <param name="node">The root node.</param>
    /// <param name="path">The path text.</param>
    /// <returns>True when a node was removed.</returns>
    public bool RemovePath(YamlNode? node, string? path);

    /// <summary>Returns the number of items in a sequence node.</summary>
    /// <param name="node">The sequence node.</param>
    /// <returns>The item count, or zero for another node kind or null.</returns>
    public int SequenceItemCount(YamlNode? node);

    /// <summary>Returns a sequence item by zero-based index.</summary>
    /// <param name="node">The sequence node.</param>
    /// <param name="index">The zero-based item index.</param>
    /// <returns>The matching item, or null.</returns>
    public YamlNode? GetSequenceItem(YamlNode? node, int index);

    /// <summary>Appends an item to a sequence.</summary>
    /// <param name="node">The sequence node.</param>
    /// <param name="item">The item to append.</param>
    /// <returns>True when the item was appended.</returns>
    public bool AddSequenceItem(YamlNode? node, YamlNode? item);

    /// <summary>Inserts an item into a sequence.</summary>
    /// <param name="node">The sequence node.</param>
    /// <param name="index">The zero-based insertion index.</param>
    /// <param name="item">The item to insert.</param>
    /// <returns>True when the item was inserted.</returns>
    public bool InsertSequenceItem(YamlNode? node, int index, YamlNode? item);

    /// <summary>Replaces an indexed sequence item.</summary>
    /// <param name="node">The sequence node.</param>
    /// <param name="index">The zero-based item index.</param>
    /// <param name="item">The replacement item.</param>
    /// <returns>True when the item was replaced.</returns>
    public bool SetSequenceItem(YamlNode? node, int index, YamlNode? item);

    /// <summary>Removes an indexed sequence item.</summary>
    /// <param name="node">The sequence node.</param>
    /// <param name="index">The zero-based item index.</param>
    /// <returns>True when the item was removed.</returns>
    public bool RemoveSequenceItem(YamlNode? node, int index);

    /// <summary>Returns the number of ordered pairs in a mapping node.</summary>
    /// <param name="node">The mapping node.</param>
    /// <returns>The pair count, or zero for another node kind or null.</returns>
    public int MappingPairCount(YamlNode? node);

    /// <summary>Returns an ordered mapping pair by zero-based index.</summary>
    /// <param name="node">The mapping node.</param>
    /// <param name="index">The zero-based pair index.</param>
    /// <returns>The matching pair, or null.</returns>
    public YamlPair? GetMappingPair(YamlNode? node, int index);

    /// <summary>Finds the first pair with a matching scalar key.</summary>
    /// <param name="node">The mapping node.</param>
    /// <param name="key">The scalar key text.</param>
    /// <returns>The matching pair, or null.</returns>
    public YamlPair? FindMappingPair(YamlNode? node, string? key);

    /// <summary>Finds the first value with a matching scalar key.</summary>
    /// <param name="node">The mapping node.</param>
    /// <param name="key">The scalar key text.</param>
    /// <returns>The matching value, or null.</returns>
    public YamlNode? FindMappingValue(YamlNode? node, string? key);

    /// <summary>Appends an ordered pair to a mapping.</summary>
    /// <param name="node">The mapping node.</param>
    /// <param name="pair">The pair to append.</param>
    /// <returns>True when the pair was appended.</returns>
    public bool AddMappingPair(YamlNode? node, YamlPair? pair);

    /// <summary>Inserts an ordered pair into a mapping.</summary>
    /// <param name="node">The mapping node.</param>
    /// <param name="index">The zero-based insertion index.</param>
    /// <param name="pair">The pair to insert.</param>
    /// <returns>True when the pair was inserted.</returns>
    public bool InsertMappingPair(YamlNode? node, int index, YamlPair? pair);

    /// <summary>Replaces an ordered pair by zero-based index.</summary>
    /// <param name="node">The mapping node.</param>
    /// <param name="index">The zero-based pair index.</param>
    /// <param name="pair">The replacement pair.</param>
    /// <returns>True when the pair was replaced.</returns>
    public bool SetMappingPair(YamlNode? node, int index, YamlPair? pair);

    /// <summary>Removes an ordered pair by zero-based index.</summary>
    /// <param name="node">The mapping node.</param>
    /// <param name="index">The zero-based pair index.</param>
    /// <returns>True when the pair was removed.</returns>
    public bool RemoveMappingPair(YamlNode? node, int index);

    /// <summary>Appends a scalar-keyed value to a mapping.</summary>
    /// <param name="node">The mapping node.</param>
    /// <param name="key">The scalar key text.</param>
    /// <param name="value">The value to append.</param>
    /// <returns>True when the value was appended.</returns>
    public bool AddMappingValue(YamlNode? node, string? key, YamlNode? value);

    /// <summary>Updates the first value with a matching scalar key.</summary>
    /// <param name="node">The mapping node.</param>
    /// <param name="key">The scalar key text.</param>
    /// <param name="value">The replacement value.</param>
    /// <returns>True when the value was updated.</returns>
    public bool SetMappingValue(YamlNode? node, string? key, YamlNode? value);

    /// <summary>Removes the first pair with a matching scalar key.</summary>
    /// <param name="node">The mapping node.</param>
    /// <param name="key">The scalar key text.</param>
    /// <returns>True when the pair was removed.</returns>
    public bool RemoveMappingValue(YamlNode? node, string? key);

    /// <summary>Finds a node using an already parsed path.</summary>
    /// <param name="node">The root node.</param>
    /// <param name="path">The parsed path.</param>
    /// <returns>The matching node, or null.</returns>
    public YamlNode? FindParsedPath(YamlNode? node, YamlPath? path);

    /// <summary>Replaces a node using an already parsed path.</summary>
    /// <param name="node">The root node.</param>
    /// <param name="path">The parsed path.</param>
    /// <param name="value">The replacement node.</param>
    /// <returns>True when a node was replaced.</returns>
    public bool ReplaceParsedPath(YamlNode? node, YamlPath? path, YamlNode? value);

    /// <summary>Removes a node using an already parsed path.</summary>
    /// <param name="node">The root node.</param>
    /// <param name="path">The parsed path.</param>
    /// <returns>True when a node was removed.</returns>
    public bool RemoveParsedPath(YamlNode? node, YamlPath? path);

    /// <summary>Changes the presentation style of a scalar node.</summary>
    /// <param name="node">The scalar node.</param>
    /// <param name="style">The requested scalar style.</param>
    /// <returns>True when the style was assigned.</returns>
    public bool SetScalarStyle(YamlNode? node, YamlScalarStyle style);

    /// <summary>Changes the presentation style of a sequence or mapping node.</summary>
    /// <param name="node">The collection node.</param>
    /// <param name="style">The requested collection style.</param>
    /// <returns>True when the style was assigned.</returns>
    public bool SetCollectionStyle(YamlNode? node, YamlCollectionStyle style);

    /// <summary>Assigns or clears a node tag.</summary>
    /// <param name="node">The node to update.</param>
    /// <param name="tag">The tag text, or null to clear it.</param>
    /// <returns>True when a node was updated.</returns>
    public bool SetTag(YamlNode? node, string? tag);

    /// <summary>Finds a document anchor by name.</summary>
    /// <param name="document">The document whose anchor table should be inspected.</param>
    /// <param name="name">The anchor name.</param>
    /// <returns>The anchored node, or null.</returns>
    public YamlNode? FindAnchor(YamlDocument? document, string? name);

    /// <summary>Rebuilds a document anchor table from its reachable node graph.</summary>
    /// <param name="document">The document to update.</param>
    /// <returns>The number of registered anchors.</returns>
    public int RebuildAnchorTable(YamlDocument? document);

    /// <summary>Renames an anchor and every alias that references it.</summary>
    /// <param name="document">The document to update.</param>
    /// <param name="oldName">The current anchor name.</param>
    /// <param name="newName">The replacement anchor name.</param>
    /// <returns>True when the anchor was renamed or already had the requested name.</returns>
    public bool RenameAnchor(YamlDocument? document, string? oldName, string? newName);

    /// <summary>Renames every matching alias to an existing document anchor.</summary>
    /// <param name="document">The document to update.</param>
    /// <param name="oldName">The alias name to replace.</param>
    /// <param name="newName">The existing anchor name to reference.</param>
    /// <returns>The number of aliases changed.</returns>
    public int RenameAliases(YamlDocument? document, string? oldName, string? newName);

    /// <summary>Renames one alias to an existing document anchor.</summary>
    /// <param name="document">The document containing the alias and target.</param>
    /// <param name="alias">The alias node to update.</param>
    /// <param name="newName">The existing anchor name to reference.</param>
    /// <returns>True when the alias was updated.</returns>
    public bool RenameAlias(YamlDocument? document, YamlNode? alias, string? newName);

    /// <summary>Retargets one alias to an anchored node in its document.</summary>
    /// <param name="document">The document containing the alias and target.</param>
    /// <param name="alias">The alias node to update.</param>
    /// <param name="target">The anchored target node.</param>
    /// <returns>True when the alias was retargeted.</returns>
    public bool RetargetAlias(YamlDocument? document, YamlNode? alias, YamlNode? target);

    /// <summary>Retargets every alias that currently references one target.</summary>
    /// <param name="document">The document to update.</param>
    /// <param name="oldTarget">The current alias target.</param>
    /// <param name="newTarget">The replacement anchored target.</param>
    /// <returns>The number of aliases changed.</returns>
    public int RetargetAliases(YamlDocument? document, YamlNode? oldTarget, YamlNode? newTarget);

    /// <summary>Resolves one alias name against its document anchor table.</summary>
    /// <param name="document">The document containing the alias.</param>
    /// <param name="alias">The alias node to resolve.</param>
    /// <returns>True when a target was found and assigned.</returns>
    public bool ResolveAlias(YamlDocument? document, YamlNode? alias);

    /// <summary>Resolves every reachable alias and clears targets for unknown names.</summary>
    /// <param name="document">The document to update.</param>
    /// <returns>The number of aliases resolved.</returns>
    public int ResolveAliases(YamlDocument? document);

    /// <summary>Adds a placed comment to a document.</summary>
    /// <param name="document">The document to update.</param>
    /// <param name="text">The comment text.</param>
    /// <param name="placement">The comment placement.</param>
    /// <returns>True when the comment was added.</returns>
    public bool AddDocumentComment(YamlDocument? document, string? text, YamlTriviaPlacement placement);

    /// <summary>Adds a placed comment to a node.</summary>
    /// <param name="node">The node to update.</param>
    /// <param name="text">The comment text.</param>
    /// <param name="placement">The comment placement.</param>
    /// <returns>True when the comment was added.</returns>
    public bool AddNodeComment(YamlNode? node, string? text, YamlTriviaPlacement placement);

    /// <summary>Adds a placed comment to a mapping pair.</summary>
    /// <param name="pair">The pair to update.</param>
    /// <param name="text">The comment text.</param>
    /// <param name="placement">The comment placement.</param>
    /// <returns>True when the comment was added.</returns>
    public bool AddPairComment(YamlPair? pair, string? text, YamlTriviaPlacement placement);

    /// <summary>Adds a placed blank line to a document.</summary>
    /// <param name="document">The document to update.</param>
    /// <param name="placement">The blank-line placement.</param>
    /// <returns>True when the blank line was added.</returns>
    public bool AddDocumentBlankLine(YamlDocument? document, YamlTriviaPlacement placement);

    /// <summary>Adds a placed blank line to a node.</summary>
    /// <param name="node">The node to update.</param>
    /// <param name="placement">The blank-line placement.</param>
    /// <returns>True when the blank line was added.</returns>
    public bool AddNodeBlankLine(YamlNode? node, YamlTriviaPlacement placement);

    /// <summary>Adds a placed blank line to a mapping pair.</summary>
    /// <param name="pair">The pair to update.</param>
    /// <param name="placement">The blank-line placement.</param>
    /// <returns>True when the blank line was added.</returns>
    public bool AddPairBlankLine(YamlPair? pair, YamlTriviaPlacement placement);

    /// <summary>Removes document trivia by zero-based index.</summary>
    /// <param name="document">The document to update.</param>
    /// <param name="index">The zero-based trivia index.</param>
    /// <returns>True when trivia was removed.</returns>
    public bool RemoveDocumentTrivia(YamlDocument? document, int index);

    /// <summary>Removes node trivia by zero-based index.</summary>
    /// <param name="node">The node to update.</param>
    /// <param name="index">The zero-based trivia index.</param>
    /// <returns>True when trivia was removed.</returns>
    public bool RemoveNodeTrivia(YamlNode? node, int index);

    /// <summary>Removes pair trivia by zero-based index.</summary>
    /// <param name="pair">The pair to update.</param>
    /// <param name="index">The zero-based trivia index.</param>
    /// <returns>True when trivia was removed.</returns>
    public bool RemovePairTrivia(YamlPair? pair, int index);

    /// <summary>Creates an independent copy of a node graph.</summary>
    /// <param name="node">The node graph to copy.</param>
    /// <returns>The copied graph, or an empty node when null.</returns>
    public YamlNode CloneNode(YamlNode? node);

    /// <summary>Applies YAML merge keys in a node graph.</summary>
    /// <param name="node">The node graph to update.</param>
    /// <returns>The number of applied merge entries.</returns>
    public int ApplyMergeKeys(YamlNode? node);

    /// <summary>Resolves the YAML tag for scalar text.</summary>
    /// <param name="value">The scalar text.</param>
    /// <param name="style">The scalar presentation style.</param>
    /// <param name="options">The schema settings; pass null to use defaults.</param>
    /// <returns>The resolved tag.</returns>
    public string ResolveScalarTag(string value, YamlScalarStyle style, YamlSchemaOptions? options);

    /// <summary>Normalizes a standard YAML tag shorthand.</summary>
    /// <param name="tag">The tag to normalize.</param>
    /// <returns>The normalized tag.</returns>
    public string NormalizeStandardTag(string? tag);

    /// <summary>Reports whether a tag denotes a standard YAML type.</summary>
    /// <param name="tag">The tag to inspect.</param>
    /// <returns>True for a standard tag.</returns>
    public bool IsStandardTag(string? tag);

    /// <summary>Applies a configured public-field naming convention.</summary>
    /// <param name="name">The field name.</param>
    /// <param name="convention">The naming convention.</param>
    /// <returns>The converted name.</returns>
    public string ApplyFieldNaming(string? name, YamlNamingConvention convention);
}
