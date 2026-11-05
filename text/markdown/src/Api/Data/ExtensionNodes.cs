using System;

namespace markdown;

/// <summary>
/// Provides a named wrapper around an extension AST node.
/// </summary>
public class MarkdownExtensionNode
{
    /// <summary>
    /// Stores the node value used by this instance.
    /// </summary>
    public MarkdownNode Node;
    /// <summary>
    /// Stores the extension name value used by this instance.
    /// </summary>
    public string ExtensionName;

    /// <summary>
    /// Initializes a new <see cref="MarkdownExtensionNode"/> instance.
    /// </summary>
    public MarkdownExtensionNode()
    {
        Node = MarkdownNode.Create(MarkdownNodeKind.ExtensionBlock);
        ExtensionName = "";
    }

    /// <summary>
    /// Creates a named extension wrapper for a new node.
    /// </summary>
    /// <param name="extensionName">The extension name value.</param>
    /// <param name="kind">The node kind to match.</param>
    internal static MarkdownExtensionNode Create(string extensionName, MarkdownNodeKind kind)
    {
        if (extensionName == null)
        {
            throw new ArgumentNullException("extensionName");
        }

        ValidateExtensionKind(kind);
        MarkdownExtensionNode extension = new MarkdownExtensionNode();
        extension.Node = MarkdownNode.Create(kind);
        extension.ExtensionName = extensionName;
        if (extension.Node.Data.Extension != null)
        {
            extension.Node.Data.Extension.Name = extensionName;
        }
        return extension;
    }

    /// <summary>
    /// Creates a named extension wrapper around an existing node.
    /// </summary>
    /// <param name="extensionName">The extension name value.</param>
    /// <param name="node">The AST node to process.</param>
    internal static MarkdownExtensionNode Create(string extensionName, MarkdownNode node)
    {
        if (extensionName == null)
        {
            throw new ArgumentNullException("extensionName");
        }

        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        ValidateExtensionKind(node.Kind);
        MarkdownExtensionNode extension = new MarkdownExtensionNode();
        extension.Node = node;
        extension.ExtensionName = extensionName;
        if (extension.Node.Data.Extension != null)
        {
            extension.Node.Data.Extension.Name = extensionName;
        }
        return extension;
    }

    internal static void ValidateExtensionKind(MarkdownNodeKind kind)
    {
        if (!MarkdownNodeKindFacts.IsExtensionKind(kind))
        {
            throw new ArgumentException("Node kind is not an extension kind.");
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
