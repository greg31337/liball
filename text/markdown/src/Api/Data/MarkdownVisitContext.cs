using System;

namespace markdown;

/// <summary>
/// Describes a node's location during tree traversal.
/// </summary>
public class MarkdownVisitContext
{
    /// <summary>
    /// Stores the node value used by this instance.
    /// </summary>
    public MarkdownNode Node;
    /// <summary>
    /// Stores the parent value used by this instance.
    /// </summary>
    public MarkdownNode? Parent;
    /// <summary>
    /// Stores the depth value used by this instance.
    /// </summary>
    public int Depth;
    /// <summary>
    /// Stores the child index value used by this instance.
    /// </summary>
    public int ChildIndex;

    /// <summary>
    /// Initializes a new <see cref="MarkdownVisitContext"/> instance.
    /// </summary>
    /// <param name="node">The AST node to process.</param>
    /// <param name="parent">The node's parent, or <c>null</c> for a root.</param>
    /// <param name="depth">The zero-based depth in the syntax tree.</param>
    /// <param name="childIndex">The zero-based index within the parent node.</param>
    public MarkdownVisitContext(MarkdownNode node, MarkdownNode? parent, int depth, int childIndex)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
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
        Parent = parent;
        Depth = depth;
        ChildIndex = childIndex;
    }

    /// <summary>
    /// Reports whether the current node has no parent.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsRoot()
    {
        return Parent == null;
    }

    /// <summary>
    /// Reports whether the current node is the document root.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsDocument()
    {
        return MarkdownNodeKindFacts.IsDocumentKind(Node.Kind);
    }

    /// <summary>
    /// Reports whether the current node is valid in block position.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsBlock()
    {
        return MarkdownNodeKindFacts.IsBlockKind(Node.Kind);
    }

    /// <summary>
    /// Reports whether the current node is valid in inline position.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsInline()
    {
        return MarkdownNodeKindFacts.IsInlineKind(Node.Kind);
    }

    /// <summary>
    /// Reports whether the current node belongs to an extension syntax.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsExtension()
    {
        return MarkdownNodeKindFacts.IsExtensionKind(Node.Kind);
    }
}
