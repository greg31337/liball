namespace svg;

/// <summary>Describes one node or attribute region in a document source map.</summary>
public class SvgSourceMapEntry
{
    /// <summary>The relationship between the current region and authored source.</summary>
    public SvgSourceMapKind Kind;
    /// <summary>The mapped node, or <c>null</c> for an attribute entry.</summary>
    public SvgNode? Node;
    /// <summary>The mapped attribute, or <c>null</c> for a node entry.</summary>
    public SvgAttribute? Attribute;
    /// <summary>The retained original source span, or <c>null</c> when unavailable or generated.</summary>
    public SvgSourceSpan? SourceSpan;

    internal SvgSourceMapEntry(SvgNode? node, SvgAttribute? attribute)
    {
        if ((node == null) == (attribute == null)) throw new System.ArgumentException("Exactly one source-map target is required.");
        Kind = node == null ? (attribute ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceMapKind : node.SourceMapKind;
        Node = node;
        Attribute = attribute;
        SourceSpan = node == null ? (attribute ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan : node.SourceSpan;
    }
}

/// <summary>Provides deterministic source-region snapshots for an editable document.</summary>
public class SvgSourceMap
{
    /// <summary>The document described by this source map.</summary>
    public SvgDocument Document;

    internal SvgSourceMap(SvgDocument document)
    {
        Document = document;
    }

    /// <summary>Returns the current source-map entry for an owned node.</summary>
    /// <param name="node">The document node or an owned node.</param>
    /// <returns>A newly allocated source-map entry.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>node</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the node does not belong to this map's document.</exception>
    internal SvgSourceMapEntry GetNode(SvgNode node)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        if (!object.ReferenceEquals(node, Document.DocumentNode) && !object.ReferenceEquals(node.OwnerDocument, Document)) throw new System.ArgumentException("The node does not belong to this source map's document.", "node");
        return new SvgSourceMapEntry(node, null);
    }

    /// <summary>Returns the current source-map entry for an owned attribute.</summary>
    /// <param name="attribute">The owned attribute.</param>
    /// <returns>A newly allocated source-map entry.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>attribute</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the attribute does not belong to this map's document.</exception>
    internal SvgSourceMapEntry GetAttribute(SvgAttribute attribute)
    {
        if (attribute == null) throw new System.ArgumentNullException("attribute");
        if (!object.ReferenceEquals(attribute.OwnerDocument, Document)) throw new System.ArgumentException("The attribute does not belong to this source map's document.", "attribute");
        return new SvgSourceMapEntry(null, attribute);
    }

    /// <summary>Returns all attached node and attribute entries in deterministic preorder.</summary>
    /// <returns>A newly allocated source-map snapshot.</returns>
    internal System.Collections.Generic.List<SvgSourceMapEntry> GetEntries()
    {
        System.Collections.Generic.List<SvgSourceMapEntry> entries = new System.Collections.Generic.List<SvgSourceMapEntry>();
        AppendEntries(Document.DocumentNode, entries);
        return entries;
    }

    /// <summary>Returns attached entries having one source-map kind in deterministic preorder.</summary>
    /// <param name="kind">The source-map kind to include.</param>
    /// <returns>A newly allocated filtered source-map snapshot.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>kind</c> is invalid.</exception>
    internal System.Collections.Generic.List<SvgSourceMapEntry> GetEntriesByKind(SvgSourceMapKind kind)
    {
        if (kind < SvgSourceMapKind.Original || kind > SvgSourceMapKind.Rewritten) throw new System.ArgumentOutOfRangeException("kind");
        System.Collections.Generic.List<SvgSourceMapEntry> all = GetEntries();
        System.Collections.Generic.List<SvgSourceMapEntry> entries = new System.Collections.Generic.List<SvgSourceMapEntry>();
        int index = 0;
        while (index < all.Count)
        {
            if (all[index].Kind == kind) entries.Add(all[index]);
            index = index + 1;
        }
        return entries;
    }

    internal static void AppendEntries(SvgNode node, System.Collections.Generic.List<SvgSourceMapEntry> entries)
    {
        entries.Add(new SvgSourceMapEntry(node, null));
        int attributeIndex = 0;
        while (attributeIndex < node.Attributes.Count)
        {
            entries.Add(new SvgSourceMapEntry(null, node.Attributes[attributeIndex]));
            attributeIndex = attributeIndex + 1;
        }
        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            AppendEntries(node.Children[childIndex], entries);
            childIndex = childIndex + 1;
        }
    }
}
