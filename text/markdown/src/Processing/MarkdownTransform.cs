using System;

namespace markdown;

/// <summary>
/// Defines a named document-level or node-level transformation.
/// </summary>
internal class MarkdownTransform : IMarkdownTransform
{
    /// <summary>
    /// Stores the name value used by this instance.
    /// </summary>
    internal string Name;
    /// <summary>
    /// Stores the document handler value used by this instance.
    /// </summary>
    internal Action<MarkdownDocument, MarkdownTransformContext>? DocumentHandler;
    /// <summary>
    /// Stores the node handler value used by this instance.
    /// </summary>
    internal Action<MarkdownNode, MarkdownTransformContext>? NodeHandler;

    /// <summary>
    /// Initializes a new <see cref="MarkdownTransform"/> instance.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    internal MarkdownTransform(string name)
    {
        MarkdownRuleRegistration.ValidateName(name);

        Name = name;
        DocumentHandler = null;
        NodeHandler = null;
    }

    /// <summary>
    /// Creates a transform that invokes a callback once for the whole document.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <param name="handler">The callback invoked by the configured operation.</param>
    /// <returns>The resulting <see cref="MarkdownTransform"/> value.</returns>
    internal static MarkdownTransform ForDocument(string name, Action<MarkdownDocument, MarkdownTransformContext> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException("handler");
        }

        MarkdownTransform transform = new MarkdownTransform(name);
        transform.DocumentHandler = handler;
        return transform;
    }

    /// <summary>
    /// Creates a transform that invokes a callback for every node in depth-first order.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <param name="handler">The callback invoked by the configured operation.</param>
    /// <returns>The resulting <see cref="MarkdownTransform"/> value.</returns>
    internal static MarkdownTransform ForNode(string name, Action<MarkdownNode, MarkdownTransformContext> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException("handler");
        }

        MarkdownTransform transform = new MarkdownTransform(name);
        transform.NodeHandler = handler;
        return transform;
    }

    /// <summary>
    /// Returns the stable name used to register and order this transform.
    /// </summary>
    /// <returns>The stable transform name.</returns>
    public string GetName()
    {
        return Name;
    }

    /// <summary>
    /// Invokes this transform's document callback and then its node callback, when configured.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <param name="context">The context shared by the current operation.</param>
    public void Apply(MarkdownDocument document, MarkdownTransformContext context)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (DocumentHandler != null)
        {
            DocumentHandler(document, context);
        }

        if (NodeHandler != null)
        {
            ApplyToNodes(document, context);
        }
    }

    /// <summary>
    /// Applies to nodes.
    /// </summary>
    /// <param name="document">The Markdown document to process.</param>
    /// <param name="context">The context shared by the current operation.</param>
    internal void ApplyToNodes(MarkdownDocument document, MarkdownTransformContext context)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        MarkdownNodeVisitor visitor = new MarkdownNodeVisitor();
        visitor.EnterNode = visitContext =>
        {
            if (NodeHandler != null)
            {
                NodeHandler(visitContext.Node, context);
            }

            return MarkdownTreeWalkResult.Continue;
        };

        MarkdownTreeWalker.Walk(document, visitor);
    }
}
