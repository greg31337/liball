using System;

namespace markdown;

/// <summary>
/// Associates an AST node with its source span and tree location.
/// </summary>
public class MarkdownSourceMapEntry
{
    /// <summary>
    /// Stores the node value used by this instance.
    /// </summary>
    public MarkdownNode Node;
    /// <summary>
    /// Stores the span value used by this instance.
    /// </summary>
    public MarkdownSourceSpan Span;
    /// <summary>
    /// Stores the depth value used by this instance.
    /// </summary>
    public int Depth;
    /// <summary>
    /// Stores the child index value used by this instance.
    /// </summary>
    public int ChildIndex;

    /// <summary>
    /// Creates a source-map entry using the node's current span.
    /// </summary>
    /// <param name="node">The AST node to process.</param>
    /// <param name="depth">The zero-based depth in the syntax tree.</param>
    /// <param name="childIndex">The zero-based index within the parent node.</param>
    internal static MarkdownSourceMapEntry Create(MarkdownNode node, int depth, int childIndex)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        return new MarkdownSourceMapEntry(node, node.Span, depth, childIndex);
    }

    /// <summary>
    /// Creates a source-map entry with an explicit span.
    /// </summary>
    /// <param name="node">The AST node to process.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <param name="depth">The zero-based depth in the syntax tree.</param>
    /// <param name="childIndex">The zero-based index within the parent node.</param>
    public MarkdownSourceMapEntry(MarkdownNode node, MarkdownSourceSpan span, int depth, int childIndex)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (depth < 0)
        {
            throw new ArgumentOutOfRangeException("depth");
        }

        if (childIndex < -1)
        {
            throw new ArgumentOutOfRangeException("childIndex");
        }

        Node = node;
        Span = span.Copy();
        Depth = depth;
        ChildIndex = childIndex;
    }

    /// <summary>
    /// Creates a detached copy of the value.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownSourceMapEntry"/> value.</returns>
    internal MarkdownSourceMapEntry Copy()
    {
        return new MarkdownSourceMapEntry(Node, Span, Depth, ChildIndex);
    }

    /// <summary>
    /// Checks whether the value contains offset.
    /// </summary>
    /// <param name="offset">The zero-based absolute source offset.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool ContainsOffset(int offset)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        return Span.ContainsOffset(offset);
    }

    /// <summary>
    /// Checks whether the value contains position.
    /// </summary>
    /// <param name="position">The source position to locate.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool ContainsPosition(MarkdownSourcePosition position)
    {
        if (position == null)
        {
            throw new ArgumentNullException("position");
        }

        return ContainsOffset(position.Offset);
    }
}
