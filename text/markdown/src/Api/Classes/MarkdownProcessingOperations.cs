using System;
using System.Collections.Generic;

namespace markdown;

/// <summary>
/// Provides normalization, ordered transforms, reference resolution, and link and image rewrites.
/// </summary>
/// <remarks>
/// Mutation operations update the supplied tree in place. Existing diagnostics, source spans, source attribution, and node identities are retained.
/// </remarks>
public class MarkdownProcessingOperations : IMarkdownProcessingOperations
{
    /// <summary>Initializes document processing operations.</summary>
    public MarkdownProcessingOperations()
    {
    }

    /// <summary>Renders a document as normalized CommonMark.</summary>
    /// <param name="document">The document to normalize.</param>
    /// <returns>The normalized Markdown.</returns>
    public string NormalizeDocument(MarkdownDocument document)
    {
        ValidateDocument(document);
        return Markdown.Normalize(document);
    }

    /// <summary>Renders a document as normalized Markdown using a supplied pipeline.</summary>
    /// <param name="document">The document to normalize.</param>
    /// <param name="pipeline">The rendering pipeline.</param>
    /// <returns>The normalized Markdown.</returns>
    public string NormalizeDocumentWithPipeline(MarkdownDocument document, MarkdownPipeline pipeline)
    {
        ValidateDocumentAndPipeline(document, pipeline);
        return Markdown.Normalize(document, pipeline);
    }

    /// <summary>Applies one document transform using CommonMark settings.</summary>
    /// <param name="document">The document to mutate.</param>
    /// <param name="transform">The transform to apply.</param>
    /// <returns>The same document instance.</returns>
    public MarkdownDocument ApplyDocumentTransform(MarkdownDocument document, IMarkdownTransform transform)
    {
        return ApplyDocumentTransformWithPipeline(document, transform, MarkdownPipeline.CommonMarkPreset());
    }

    /// <summary>Applies one document transform using supplied pipeline settings.</summary>
    /// <param name="document">The document to mutate.</param>
    /// <param name="transform">The transform to apply.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The same document instance.</returns>
    public MarkdownDocument ApplyDocumentTransformWithPipeline(
        MarkdownDocument document,
        IMarkdownTransform transform,
        MarkdownPipeline pipeline)
    {
        if (transform == null)
        {
            throw new ArgumentNullException("transform");
        }

        IMarkdownTransform[] transforms = new IMarkdownTransform[] { transform };
        return ApplyDocumentTransformsInOrder(document, transforms, pipeline);
    }

    /// <summary>Applies document transforms in array order using supplied pipeline settings.</summary>
    /// <param name="document">The document to mutate.</param>
    /// <param name="transforms">The transforms in application order.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The same document instance.</returns>
    public MarkdownDocument ApplyDocumentTransformsInOrder(
        MarkdownDocument document,
        IMarkdownTransform[] transforms,
        MarkdownPipeline pipeline)
    {
        ValidateDocumentAndPipeline(document, pipeline);
        ValidateDocumentTransforms(transforms);
        MarkdownTransformContext context = new MarkdownTransformContext(document, pipeline);
        int index = 0;
        while (index < transforms.Length)
        {
            transforms[index].Apply(document, context);
            index = index + 1;
        }

        return document;
    }

    /// <summary>Applies one node transform in preorder using CommonMark settings.</summary>
    /// <param name="document">The document whose nodes are visited.</param>
    /// <param name="transform">The node transform to apply.</param>
    /// <returns>The same document instance.</returns>
    public MarkdownDocument ApplyNodeTransform(MarkdownDocument document, IMarkdownNodeTransform transform)
    {
        return ApplyNodeTransformWithPipeline(document, transform, MarkdownPipeline.CommonMarkPreset());
    }

    /// <summary>Applies one node transform in preorder using supplied pipeline settings.</summary>
    /// <param name="document">The document whose nodes are visited.</param>
    /// <param name="transform">The node transform to apply.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The same document instance.</returns>
    public MarkdownDocument ApplyNodeTransformWithPipeline(
        MarkdownDocument document,
        IMarkdownNodeTransform transform,
        MarkdownPipeline pipeline)
    {
        if (transform == null)
        {
            throw new ArgumentNullException("transform");
        }

        IMarkdownNodeTransform[] transforms = new IMarkdownNodeTransform[] { transform };
        return ApplyNodeTransformsInOrder(document, transforms, pipeline);
    }

    /// <summary>Applies node transforms in array order, with each transform visiting a preorder snapshot.</summary>
    /// <param name="document">The document whose nodes are visited.</param>
    /// <param name="transforms">The transforms in application order.</param>
    /// <param name="pipeline">The processing pipeline.</param>
    /// <returns>The same document instance.</returns>
    public MarkdownDocument ApplyNodeTransformsInOrder(
        MarkdownDocument document,
        IMarkdownNodeTransform[] transforms,
        MarkdownPipeline pipeline)
    {
        ValidateDocumentAndPipeline(document, pipeline);
        ValidateNodeTransforms(transforms);
        MarkdownTransformContext context = new MarkdownTransformContext(document, pipeline);
        int transformIndex = 0;
        while (transformIndex < transforms.Length)
        {
            List<MarkdownNode> nodes = new List<MarkdownNode>();
            AddPreorderNodes(document.Root, nodes);
            int nodeIndex = 0;
            while (nodeIndex < nodes.Count)
            {
                transforms[transformIndex].ApplyToNode(nodes[nodeIndex], context);
                nodeIndex = nodeIndex + 1;
            }

            transformIndex = transformIndex + 1;
        }

        return document;
    }

    /// <summary>Applies transforms registered in a pipeline in registration order.</summary>
    /// <param name="document">The document to mutate.</param>
    /// <param name="pipeline">The pipeline containing the ordered transforms.</param>
    /// <returns>The same document instance.</returns>
    public MarkdownDocument ApplyPipelineTransforms(MarkdownDocument document, MarkdownPipeline pipeline)
    {
        ValidateDocumentAndPipeline(document, pipeline);
        return MarkdownTransformPipeline.Apply(document, pipeline);
    }

    /// <summary>Resolves link and image reference nodes against the first matching definition.</summary>
    /// <param name="document">The document to mutate.</param>
    /// <returns>The number of references resolved.</returns>
    public int ResolveReferences(MarkdownDocument document)
    {
        ValidateDocument(document);
        return ResolveReferenceKinds(document, true, true);
    }

    internal static int ResolveReferenceKinds(
        MarkdownDocument document,
        bool resolveLinks,
        bool resolveImages)
    {
        MarkdownLinkReferenceMap references = BuildReferenceMap(document.Root);
        List<MarkdownNode> nodes = new List<MarkdownNode>();
        AddPreorderNodes(document.Root, nodes);
        int resolvedCount = 0;
        int index = 0;
        while (index < nodes.Count)
        {
            MarkdownNode node = nodes[index];
            if (((resolveLinks && node.Kind == MarkdownNodeKind.Link)
                    || (resolveImages && node.Kind == MarkdownNodeKind.Image))
                && node.GetAttributeValue("referenceLabel").Length > 0)
            {
                MarkdownNode? definition = references.Find(node.GetAttributeValue("referenceLabel"));
                if (definition == null)
                {
                    node.RemoveAttribute("destination");
                    node.RemoveAttribute("title");
                }
                else
                {
                    node.SetAttribute("destination", definition.GetAttributeValue("destination"));
                    string title = definition.GetAttributeValue("title");
                    if (title.Length == 0)
                    {
                        node.RemoveAttribute("title");
                    }
                    else
                    {
                        node.SetAttribute("title", title);
                    }

                    resolvedCount = resolvedCount + 1;
                }
            }

            index = index + 1;
        }

        return resolvedCount;
    }

    /// <summary>Rewrites every link-reference definition destination, then resolves dependent references.</summary>
    /// <param name="document">The document to mutate.</param>
    /// <param name="rewriter">The destination callback.</param>
    /// <returns>The number of definitions offered to the callback.</returns>
    public int RewriteLinkDefinitions(MarkdownDocument document, IMarkdownDestinationRewriter rewriter)
    {
        ValidateDocumentAndDestinationRewriter(document, rewriter);
        List<MarkdownNode> nodes = new List<MarkdownNode>();
        AddPreorderNodes(document.Root, nodes);
        int count = 0;
        int index = 0;
        while (index < nodes.Count)
        {
            MarkdownNode node = nodes[index];
            if (node.Kind == MarkdownNodeKind.LinkReferenceDefinition)
            {
                string rewritten = rewriter.RewriteDestination(node, node.GetAttributeValue("destination"));
                ValidateRewriteResult(rewritten);
                node.SetAttribute("destination", rewritten);
                string label = node.GetAttributeValue("label");
                node.SetAttribute("normalizedLabel", MarkdownLinkReferenceMap.NormalizeLabel(label));
                count = count + 1;
            }

            index = index + 1;
        }

        ResolveReferenceKinds(document, true, true);
        return count;
    }

    /// <summary>Rewrites every link node destination.</summary>
    /// <param name="document">The document to mutate.</param>
    /// <param name="rewriter">The destination callback.</param>
    /// <returns>The number of links offered to the callback.</returns>
    public int RewriteLinkDestinations(MarkdownDocument document, IMarkdownDestinationRewriter rewriter)
    {
        ValidateDocumentAndDestinationRewriter(document, rewriter);
        List<MarkdownNode> nodes = new List<MarkdownNode>();
        AddPreorderNodes(document.Root, nodes);
        int count = 0;
        int index = 0;
        while (index < nodes.Count)
        {
            MarkdownNode node = nodes[index];
            if (node.Kind == MarkdownNodeKind.Link)
            {
                string rewritten = rewriter.RewriteDestination(node, node.GetAttributeValue("destination"));
                ValidateRewriteResult(rewritten);
                node.SetAttribute("destination", rewritten);
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    /// <summary>Rewrites labels on reference-style image nodes, then resolves their destinations.</summary>
    /// <param name="document">The document to mutate.</param>
    /// <param name="rewriter">The reference-label callback.</param>
    /// <returns>The number of image references offered to the callback.</returns>
    public int RewriteImageReferences(MarkdownDocument document, IMarkdownReferenceRewriter rewriter)
    {
        ValidateDocument(document);
        if (rewriter == null)
        {
            throw new ArgumentNullException("rewriter");
        }

        List<MarkdownNode> nodes = new List<MarkdownNode>();
        AddPreorderNodes(document.Root, nodes);
        int count = 0;
        int index = 0;
        while (index < nodes.Count)
        {
            MarkdownNode node = nodes[index];
            string referenceLabel = node.GetAttributeValue("referenceLabel");
            if (node.Kind == MarkdownNodeKind.Image && referenceLabel.Length > 0)
            {
                string rewritten = rewriter.RewriteReference(node, referenceLabel);
                ValidateRewriteResult(rewritten);
                if (rewritten.Length == 0)
                {
                    node.RemoveAttribute("referenceLabel");
                }
                else
                {
                    node.SetAttribute("referenceLabel", rewritten);
                }

                count = count + 1;
            }

            index = index + 1;
        }

        ResolveReferenceKinds(document, false, true);
        return count;
    }

    internal static void ValidateDocument(MarkdownDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }
    }

    internal static void ValidateDocumentAndPipeline(MarkdownDocument document, MarkdownPipeline pipeline)
    {
        ValidateDocument(document);
        if (pipeline == null)
        {
            throw new ArgumentNullException("pipeline");
        }
    }

    internal static void ValidateDocumentTransforms(IMarkdownTransform[] transforms)
    {
        if (transforms == null)
        {
            throw new ArgumentNullException("transforms");
        }

        int index = 0;
        while (index < transforms.Length)
        {
            if (transforms[index] == null)
            {
                throw new ArgumentNullException("transforms");
            }

            index = index + 1;
        }
    }

    internal static void ValidateNodeTransforms(IMarkdownNodeTransform[] transforms)
    {
        if (transforms == null)
        {
            throw new ArgumentNullException("transforms");
        }

        int index = 0;
        while (index < transforms.Length)
        {
            if (transforms[index] == null)
            {
                throw new ArgumentNullException("transforms");
            }

            index = index + 1;
        }
    }

    internal static void ValidateDocumentAndDestinationRewriter(
        MarkdownDocument document,
        IMarkdownDestinationRewriter rewriter)
    {
        ValidateDocument(document);
        if (rewriter == null)
        {
            throw new ArgumentNullException("rewriter");
        }
    }

    internal static void ValidateRewriteResult(string rewritten)
    {
        if (rewritten == null)
        {
            throw new InvalidOperationException("A Markdown rewriter returned a null value.");
        }
    }

    internal static void AddPreorderNodes(MarkdownNode node, List<MarkdownNode> nodes)
    {
        nodes.Add(node);
        MarkdownNode[] children = node.GetChildren();
        int index = 0;
        while (index < children.Length)
        {
            AddPreorderNodes(children[index], nodes);
            index = index + 1;
        }
    }

    internal static MarkdownLinkReferenceMap BuildReferenceMap(MarkdownNode root)
    {
        MarkdownLinkReferenceMap references = new MarkdownLinkReferenceMap();
        List<MarkdownNode> nodes = new List<MarkdownNode>();
        AddPreorderNodes(root, nodes);
        int index = 0;
        while (index < nodes.Count)
        {
            if (nodes[index].Kind == MarkdownNodeKind.LinkReferenceDefinition)
            {
                string label = nodes[index].GetAttributeValue("label");
                nodes[index].SetAttribute("normalizedLabel", MarkdownLinkReferenceMap.NormalizeLabel(label));
                references.Add(nodes[index]);
            }

            index = index + 1;
        }

        return references;
    }
}
