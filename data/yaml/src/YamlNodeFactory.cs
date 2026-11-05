namespace yaml;

/// <summary>
/// Creates commonly used YAML nodes and mapping pairs with safe default styles.
/// </summary>
internal static class YamlNodeFactory
{
    /// <summary>
    /// Creates an empty YAML node or source range.
    /// </summary>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode Empty()
    {
        return YamlNode.Empty(YamlSourceSpan.Empty());
    }

    /// <summary>
    /// Creates an empty YAML node or source range.
    /// </summary>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode Empty(YamlSourceSpan? span)
    {
        return YamlNode.Empty(span);
    }

    /// <summary>
    /// Converts a JSON-compatible CLR object graph into YAML nodes.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode FromJsonValue(object? value)
    {
        return YamlJsonConverter.FromJsonValue(value);
    }

    /// <summary>
    /// Creates a scalar node with the requested text and presentation style.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode Scalar(string? value)
    {
        return Scalar(value, YamlScalarStyle.Plain);
    }

    /// <summary>
    /// Creates a scalar node with the requested text and presentation style.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <param name="style">The presentation style assigned to the value.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode Scalar(string? value, YamlScalarStyle style)
    {
        return Scalar(value, style, YamlSourceSpan.Empty());
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
        return YamlNode.Scalar(value, style, span);
    }

    /// <summary>
    /// Creates a plain scalar and assigns the supplied YAML tag.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <param name="tag">The YAML tag in shorthand or expanded form.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode TaggedScalar(string? value, string? tag)
    {
        YamlNode node = Scalar(value);
        node.Tag = tag == null ? "" : tag;
        return node;
    }

    /// <summary>
    /// Creates an unquoted plain scalar.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode PlainScalar(string? value)
    {
        return Scalar(value, YamlScalarStyle.Plain);
    }

    /// <summary>
    /// Creates a scalar that requests single-quoted emission.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode SingleQuotedScalar(string? value)
    {
        return Scalar(value, YamlScalarStyle.SingleQuoted);
    }

    /// <summary>
    /// Creates a scalar that requests double-quoted emission and escaping.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode DoubleQuotedScalar(string? value)
    {
        return Scalar(value, YamlScalarStyle.DoubleQuoted);
    }

    /// <summary>
    /// Creates a scalar that requests literal block emission with line breaks preserved.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode LiteralScalar(string? value)
    {
        return Scalar(value, YamlScalarStyle.Literal);
    }

    /// <summary>
    /// Creates a scalar that requests folded block emission.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode FoldedScalar(string? value)
    {
        return Scalar(value, YamlScalarStyle.Folded);
    }

    /// <summary>
    /// Creates a sequence node with the requested style or initial child.
    /// </summary>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode Sequence()
    {
        return Sequence(YamlCollectionStyle.Block);
    }

    /// <summary>
    /// Creates a sequence node with the requested style or initial child.
    /// </summary>
    /// <param name="style">The presentation style assigned to the value.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode Sequence(YamlCollectionStyle style)
    {
        return Sequence(style, YamlSourceSpan.Empty());
    }

    /// <summary>
    /// Creates a sequence node with the requested style or initial child.
    /// </summary>
    /// <param name="style">The presentation style assigned to the value.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode Sequence(YamlCollectionStyle style, YamlSourceSpan? span)
    {
        return YamlNode.Sequence(style, span);
    }

    /// <summary>
    /// Creates an empty sequence that requests bracketed flow notation.
    /// </summary>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode FlowSequence()
    {
        return Sequence(YamlCollectionStyle.Flow);
    }

    /// <summary>
    /// Creates a sequence node with the requested style or initial child.
    /// </summary>
    /// <param name="child">The sequence child node to process.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode SequenceWithChild(YamlNode? child)
    {
        YamlNode node = Sequence();
        node.AddChild(child);
        return node;
    }

    /// <summary>
    /// Creates a mapping node with the requested style or initial pair.
    /// </summary>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode Mapping()
    {
        return Mapping(YamlCollectionStyle.Block);
    }

    /// <summary>
    /// Creates a mapping node with the requested style or initial pair.
    /// </summary>
    /// <param name="style">The presentation style assigned to the value.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode Mapping(YamlCollectionStyle style)
    {
        return Mapping(style, YamlSourceSpan.Empty());
    }

    /// <summary>
    /// Creates a mapping node with the requested style or initial pair.
    /// </summary>
    /// <param name="style">The presentation style assigned to the value.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode Mapping(YamlCollectionStyle style, YamlSourceSpan? span)
    {
        return YamlNode.Mapping(style, span);
    }

    /// <summary>
    /// Creates an empty mapping that requests braced flow notation.
    /// </summary>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode FlowMapping()
    {
        return Mapping(YamlCollectionStyle.Flow);
    }

    /// <summary>
    /// Creates a mapping node with the requested style or initial pair.
    /// </summary>
    /// <param name="pair">The mapping pair to process.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode MappingWithPair(YamlPair? pair)
    {
        YamlNode node = Mapping();
        node.AddPair(pair);
        return node;
    }

    /// <summary>
    /// Creates an alias node for the supplied anchor name.
    /// </summary>
    /// <param name="aliasName">The anchor name referenced by the alias, without the asterisk marker.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode Alias(string? aliasName)
    {
        return Alias(aliasName, YamlSourceSpan.Empty());
    }

    /// <summary>
    /// Creates an alias node for the supplied anchor name.
    /// </summary>
    /// <param name="aliasName">The anchor name referenced by the alias, without the asterisk marker.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode Alias(string? aliasName, YamlSourceSpan? span)
    {
        return YamlNode.Alias(aliasName, span);
    }

    /// <summary>
    /// Creates an alias node for the supplied anchor name.
    /// </summary>
    /// <param name="aliasName">The anchor name referenced by the alias, without the asterisk marker.</param>
    /// <param name="target">The target node or CLR type associated with the operation.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode AliasTo(string? aliasName, YamlNode? target)
    {
        YamlNode node = Alias(aliasName);
        node.AliasTarget = target;
        return node;
    }

    /// <summary>
    /// Creates a mapping pair from the supplied key and value.
    /// </summary>
    /// <param name="key">The mapping key or path key.</param>
    /// <param name="value">The value to process.</param>
    /// <returns>The resulting YAML pair.</returns>
    internal static YamlPair Pair(YamlNode? key, YamlNode? value)
    {
        return Pair(key, value, YamlSourceSpan.Empty());
    }

    /// <summary>
    /// Creates a mapping pair from the supplied key and value.
    /// </summary>
    /// <param name="key">The mapping key or path key.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML pair.</returns>
    internal static YamlPair Pair(YamlNode? key, YamlNode? value, YamlSourceSpan? span)
    {
        YamlNode pairKey = key == null ? Empty() : key;
        YamlNode pairValue = value == null ? Empty() : value;
        return YamlPair.Create(pairKey, pairValue, span);
    }

    /// <summary>
    /// Creates a mapping pair from the supplied key and value.
    /// </summary>
    /// <param name="key">The mapping key or path key.</param>
    /// <param name="value">The value to process.</param>
    /// <returns>The resulting YAML pair.</returns>
    internal static YamlPair Pair(string? key, YamlNode? value)
    {
        return Pair(PlainScalar(key), value);
    }

    /// <summary>
    /// Creates a mapping pair from the supplied key and value.
    /// </summary>
    /// <param name="key">The mapping key or path key.</param>
    /// <param name="value">The value to process.</param>
    /// <returns>The resulting YAML pair.</returns>
    internal static YamlPair Pair(string? key, string? value)
    {
        return Pair(PlainScalar(key), PlainScalar(value));
    }

    /// <summary>
    /// Creates a mapping pair from the supplied key and value.
    /// </summary>
    /// <param name="key">The mapping key or path key.</param>
    /// <param name="keyTag">The key tag used by the operation.</param>
    /// <param name="value">The value to process.</param>
    /// <returns>The resulting YAML pair.</returns>
    internal static YamlPair PairWithTaggedKey(string? key, string? keyTag, YamlNode? value)
    {
        YamlNode node = TaggedScalar(key, keyTag);
        return Pair(node, value);
    }

    /// <summary>
    /// Assigns a tag to a node and returns the same node for fluent construction.
    /// </summary>
    /// <param name="node">The YAML node to process.</param>
    /// <param name="tag">The YAML tag in shorthand or expanded form.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode WithTag(YamlNode? node, string? tag)
    {
        YamlNode result = node == null ? Empty() : node;
        result.Tag = tag == null ? "" : tag;
        return result;
    }

    /// <summary>
    /// Assigns an anchor name to a node and returns the same node for fluent construction.
    /// </summary>
    /// <param name="node">The YAML node to process.</param>
    /// <param name="anchor">The anchor used by the operation.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode WithAnchor(YamlNode? node, string? anchor)
    {
        YamlNode result = node == null ? Empty() : node;
        result.Anchor = anchor == null ? "" : anchor;
        return result;
    }
}
