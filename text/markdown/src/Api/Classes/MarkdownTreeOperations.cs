using System;
using System.Collections.Generic;

namespace markdown;

/// <summary>
/// Provides cloning, kind-based search, and depth-first traversal operations for Markdown trees.
/// </summary>
public class MarkdownTreeOperations : IMarkdownTreeOperations
{
    /// <summary>
    /// Initializes tree operations.
    /// </summary>
    public MarkdownTreeOperations()
    {
    }

    /// <summary>Deeply clones a node and all descendants.</summary>
    /// <param name="node">The node to clone.</param>
    /// <returns>The detached node clone.</returns>
    public MarkdownNode CloneNode(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        return node.CloneNode();
    }

    /// <summary>Deeply clones a document, its tree, and diagnostics.</summary>
    /// <param name="document">The document to clone.</param>
    /// <returns>The detached document clone.</returns>
    public MarkdownDocument CloneDocument(MarkdownDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        return document.CloneDocument();
    }

    /// <summary>Finds the first matching node in preorder.</summary>
    /// <param name="root">The subtree root to include in the search.</param>
    /// <param name="kind">The node kind to find.</param>
    /// <returns>The first matching node, or <c>null</c>.</returns>
    public MarkdownNode? FindFirstInNode(MarkdownNode root, MarkdownNodeKind kind)
    {
        if (root == null)
        {
            throw new ArgumentNullException("root");
        }

        return FindFirstRecursive(root, kind);
    }

    /// <summary>Finds the first matching document node in preorder.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="kind">The node kind to find.</param>
    /// <returns>The first matching node, or <c>null</c>.</returns>
    public MarkdownNode? FindFirstInDocument(MarkdownDocument document, MarkdownNodeKind kind)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        return FindFirstRecursive(document.Root, kind);
    }

    /// <summary>Finds every matching node in preorder.</summary>
    /// <param name="root">The subtree root to include in the search.</param>
    /// <param name="kind">The node kind to find.</param>
    /// <returns>The matching nodes in preorder.</returns>
    public MarkdownNode[] FindAllInNode(MarkdownNode root, MarkdownNodeKind kind)
    {
        if (root == null)
        {
            throw new ArgumentNullException("root");
        }

        List<MarkdownNode> matches = new List<MarkdownNode>();
        AddMatchesRecursive(root, kind, matches);
        return matches.ToArray();
    }

    /// <summary>Finds every matching document node in preorder.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="kind">The node kind to find.</param>
    /// <returns>The matching nodes in preorder.</returns>
    public MarkdownNode[] FindAllInDocument(MarkdownDocument document, MarkdownNodeKind kind)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        List<MarkdownNode> matches = new List<MarkdownNode>();
        AddMatchesRecursive(document.Root, kind, matches);
        return matches.ToArray();
    }

    /// <summary>Walks a subtree with preorder enter and postorder leave callbacks.</summary>
    /// <param name="root">The subtree root to walk.</param>
    /// <param name="visitor">The visitor receiving traversal callbacks.</param>
    /// <returns>The traversal result.</returns>
    public MarkdownTreeWalkResult WalkNode(MarkdownNode root, IMarkdownNodeVisitor visitor)
    {
        return MarkdownTreeWalker.Walk(root, visitor);
    }

    /// <summary>Walks a document with preorder enter and postorder leave callbacks.</summary>
    /// <param name="document">The document to walk.</param>
    /// <param name="visitor">The visitor receiving traversal callbacks.</param>
    /// <returns>The traversal result.</returns>
    public MarkdownTreeWalkResult WalkDocument(MarkdownDocument document, IMarkdownNodeVisitor visitor)
    {
        return MarkdownTreeWalker.Walk(document, visitor);
    }

    internal static MarkdownNode? FindFirstRecursive(MarkdownNode node, MarkdownNodeKind kind)
    {
        if (node.Kind == kind)
        {
            return node;
        }

        MarkdownNode[] children = node.GetChildren();
        int index = 0;
        while (index < children.Length)
        {
            MarkdownNode? match = FindFirstRecursive(children[index], kind);
            if (match != null)
            {
                return match;
            }

            index = index + 1;
        }

        return null;
    }

    internal static void AddMatchesRecursive(MarkdownNode node, MarkdownNodeKind kind, List<MarkdownNode> matches)
    {
        if (node.Kind == kind)
        {
            matches.Add(node);
        }

        MarkdownNode[] children = node.GetChildren();
        int index = 0;
        while (index < children.Length)
        {
            AddMatchesRecursive(children[index], kind, matches);
            index = index + 1;
        }
    }
}
