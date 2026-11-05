using System;

namespace markdown;

/// <summary>
/// Provides a typed wrapper around an AST node that is valid in inline position.
/// </summary>
public class MarkdownInlineNode
{
    /// <summary>
    /// Stores the node value used by this instance.
    /// </summary>
    public MarkdownNode Node;

    /// <summary>
    /// Initializes a new <see cref="MarkdownInlineNode"/> instance.
    /// </summary>
    public MarkdownInlineNode()
    {
        Node = MarkdownNode.Create(MarkdownNodeKind.Text);
    }

    /// <summary>
    /// Creates an inline wrapper for a new node of the specified kind.
    /// </summary>
    /// <param name="kind">The node kind to match.</param>
    internal static MarkdownInlineNode Create(MarkdownNodeKind kind)
    {
        ValidateInlineKind(kind);
        MarkdownInlineNode inline = new MarkdownInlineNode();
        inline.Node = MarkdownNode.Create(kind);
        return inline;
    }

    /// <summary>
    /// Creates an inline wrapper with literal text.
    /// </summary>
    /// <param name="kind">The node kind to match.</param>
    /// <param name="literal">The literal value.</param>
    internal static MarkdownInlineNode Create(MarkdownNodeKind kind, string literal)
    {
        if (literal == null)
        {
            throw new ArgumentNullException("literal");
        }

        ValidateInlineKind(kind);
        MarkdownInlineNode inline = new MarkdownInlineNode();
        inline.Node = MarkdownNode.Create(kind);
        inline.Node.SetLiteral(literal);
        return inline;
    }

    /// <summary>
    /// Creates an inline wrapper with a source span.
    /// </summary>
    /// <param name="kind">The node kind to match.</param>
    /// <param name="span">The source span associated with the value.</param>
    internal static MarkdownInlineNode Create(MarkdownNodeKind kind, MarkdownSourceSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        ValidateInlineKind(kind);
        MarkdownInlineNode inline = new MarkdownInlineNode();
        inline.Node = MarkdownNode.Create(kind, span);
        return inline;
    }

    /// <summary>
    /// Creates an inline wrapper around an existing node.
    /// </summary>
    /// <param name="node">The AST node to process.</param>
    internal static MarkdownInlineNode Create(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        ValidateInlineKind(node.Kind);
        MarkdownInlineNode inline = new MarkdownInlineNode();
        inline.Node = node;
        return inline;
    }

    internal static void ValidateInlineKind(MarkdownNodeKind kind)
    {
        if (!MarkdownNodeKindFacts.IsInlineKind(kind))
        {
            throw new ArgumentException("Node kind is not an inline kind.");
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
