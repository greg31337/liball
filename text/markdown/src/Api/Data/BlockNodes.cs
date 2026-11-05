using System;

namespace markdown;

/// <summary>
/// Provides a typed wrapper around an AST node that is valid in block position.
/// </summary>
public class MarkdownBlockNode
{
    /// <summary>
    /// Stores the node value used by this instance.
    /// </summary>
    public MarkdownNode Node;

    /// <summary>
    /// Initializes a new <see cref="MarkdownBlockNode"/> instance.
    /// </summary>
    public MarkdownBlockNode()
    {
        Node = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
    }

    /// <summary>
    /// Creates a block wrapper for a new node of the specified kind.
    /// </summary>
    /// <param name="kind">The node kind to match.</param>
    internal static MarkdownBlockNode Create(MarkdownNodeKind kind)
    {
        ValidateBlockKind(kind);
        MarkdownBlockNode block = new MarkdownBlockNode();
        block.Node = MarkdownNode.Create(kind);
        return block;
    }

    /// <summary>
    /// Creates a block wrapper for a new node with a source span.
    /// </summary>
    /// <param name="kind">The node kind to match.</param>
    /// <param name="span">The source span associated with the value.</param>
    internal static MarkdownBlockNode Create(MarkdownNodeKind kind, MarkdownSourceSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        ValidateBlockKind(kind);
        MarkdownBlockNode block = new MarkdownBlockNode();
        block.Node = MarkdownNode.Create(kind, span);
        return block;
    }

    /// <summary>
    /// Creates a block wrapper around an existing node.
    /// </summary>
    /// <param name="node">The AST node to process.</param>
    internal static MarkdownBlockNode Create(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        ValidateBlockKind(node.Kind);
        MarkdownBlockNode block = new MarkdownBlockNode();
        block.Node = node;
        return block;
    }

    internal static void ValidateBlockKind(MarkdownNodeKind kind)
    {
        if (!MarkdownNodeKindFacts.IsBlockKind(kind))
        {
            throw new ArgumentException("Node kind is not a block kind.");
        }
    }

    /// <summary>
    /// Returns node.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownNode"/> value.</returns>
    internal MarkdownNode GetNode()
    {
        return Node;
    }
}
