using System;

namespace markdown;

/// <summary>
/// Traverses Markdown nodes in depth-first enter-and-leave order.
/// </summary>
internal class MarkdownTreeWalker
{
    /// <summary>
    /// Walks the tree in depth-first enter-and-leave order.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <param name="visitor">The callbacks that receive traversal events.</param>
    /// <returns>The resulting <see cref="MarkdownTreeWalkResult"/> value.</returns>
    internal static MarkdownTreeWalkResult Walk(MarkdownDocument document, IMarkdownNodeVisitor visitor)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        return Walk(document.Root, visitor);
    }

    /// <summary>
    /// Walks the tree in depth-first enter-and-leave order.
    /// </summary>
    /// <param name="node">The AST node to process.</param>
    /// <param name="visitor">The callbacks that receive traversal events.</param>
    /// <returns>The resulting <see cref="MarkdownTreeWalkResult"/> value.</returns>
    internal static MarkdownTreeWalkResult Walk(MarkdownNode node, IMarkdownNodeVisitor visitor)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (visitor == null)
        {
            throw new ArgumentNullException("visitor");
        }

        return WalkNode(node, visitor, null, 0, -1);
    }

    internal static MarkdownTreeWalkResult WalkNode(MarkdownNode node, IMarkdownNodeVisitor visitor, MarkdownNode? parent, int depth, int childIndex)
    {
        MarkdownVisitContext context = new MarkdownVisitContext(node, parent, depth, childIndex);
        MarkdownTreeWalkResult result = visitor.VisitEnter(context);
        if (result != MarkdownTreeWalkResult.Continue
            && result != MarkdownTreeWalkResult.SkipChildren
            && result != MarkdownTreeWalkResult.Stop)
        {
            throw new InvalidOperationException("Node visitor returned an unsupported tree-walk result.");
        }

        if (result == MarkdownTreeWalkResult.Stop)
        {
            return MarkdownTreeWalkResult.Stop;
        }

        if (result == MarkdownTreeWalkResult.Continue)
        {
            MarkdownNode[] children = node.GetChildren();
            int index = 0;
            while (index < children.Length)
            {
                result = WalkNode(children[index], visitor, node, depth + 1, index);
                if (result == MarkdownTreeWalkResult.Stop)
                {
                    return MarkdownTreeWalkResult.Stop;
                }

                index = index + 1;
            }
        }

        visitor.VisitLeave(context);
        return MarkdownTreeWalkResult.Continue;
    }
}
