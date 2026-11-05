using System;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownProcessingOperationsTests
{
    [Test]
    public void FacadeCreatesProcessingOperationsAndNormalizesWithPipeline()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = (IMarkdownDataServices)markdown;
        IMarkdownProcessingOperations operations = services.CreateProcessingOperations();
        MarkdownDocument document = markdown.ParseSource("# Heading\n\ntext\n");
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder().SetLineEnding("\r\n").Build();

        string commonMark = operations.NormalizeDocument(document);
        string configured = operations.NormalizeDocumentWithPipeline(document, pipeline);

        Assert.That(operations, Is.TypeOf<MarkdownProcessingOperations>());
        Assert.That(commonMark, Is.EqualTo("# Heading\n\ntext\n"));
        Assert.That(configured, Is.EqualTo("# Heading\r\n\r\ntext\r\n"));
    }

    [Test]
    public void ExplicitDocumentAndNodeTransformsRunInSuppliedOrder()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownProcessingOperations operations = ((IMarkdownDataServices)markdown).CreateProcessingOperations();
        MarkdownDocument document = markdown.ParseSource("value");
        MarkdownPipeline pipeline = markdown.CreateCommonMarkPipeline();
        IMarkdownTransform[] documentTransforms = new IMarkdownTransform[]
        {
            new AppendDocumentTransform("first", "A"),
            new AppendDocumentTransform("second", "B")
        };
        IMarkdownNodeTransform[] nodeTransforms = new IMarkdownNodeTransform[]
        {
            new AppendTextNodeTransform("third", "C"),
            new AppendTextNodeTransform("fourth", "D")
        };

        MarkdownDocument documentResult = operations.ApplyDocumentTransformsInOrder(
            document,
            documentTransforms,
            pipeline);
        MarkdownDocument nodeResult = operations.ApplyNodeTransformsInOrder(
            document,
            nodeTransforms,
            pipeline);
        MarkdownNode text = ((IMarkdownDataServices)markdown)
            .CreateTreeOperations()
            .FindFirstInDocument(document, MarkdownNodeKind.Text)!;

        Assert.That(documentResult, Is.SameAs(document));
        Assert.That(nodeResult, Is.SameAs(document));
        Assert.That(text.Literal, Is.EqualTo("valueABCD"));
    }

    [Test]
    public void RegisteredPipelineTransformsRunInRegistrationOrder()
    {
        IMarkdownProcessingOperations operations = new MarkdownProcessingOperations();
        MarkdownDocument document = new MarkdownDocument();
        MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
            .AddTransform(new AppendDocumentTransform("first", "1"))
            .AddTransform(new AppendDocumentTransform("second", "2"))
            .Build();

        MarkdownDocument result = operations.ApplyPipelineTransforms(document, pipeline);

        Assert.That(result, Is.SameAs(document));
        Assert.That(document.Source, Is.EqualTo("12"));
    }

    [Test]
    public void TransformsRetainDiagnosticsSpansAndSourceMapNodeIdentity()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = (IMarkdownDataServices)markdown;
        IMarkdownProcessingOperations operations = services.CreateProcessingOperations();
        MarkdownDocument document = markdown.ParseSource("text");
        MarkdownDiagnostic diagnostic = new MarkdownDiagnostic();
        diagnostic.Code = "MD_EXISTING";
        diagnostic.Message = "existing";
        diagnostic.Span = document.Root.Span;
        document.Diagnostics.Items.Add(diagnostic);
        MarkdownDiagnosticCollection diagnostics = document.Diagnostics;
        MarkdownSourceMapEntry[] before = services.GetSourceMapEntries(document);

        operations.ApplyNodeTransform(document, new AppendTextNodeTransform("edit", "!"));

        MarkdownSourceMapEntry[] after = services.GetSourceMapEntries(document);
        Assert.That(document.Diagnostics, Is.SameAs(diagnostics));
        Assert.That(document.Diagnostics.Items.Count, Is.EqualTo(1));
        Assert.That(document.Diagnostics.Items[0].Code, Is.EqualTo("MD_EXISTING"));
        Assert.That(after.Length, Is.EqualTo(before.Length));
        for (int index = 0; index < before.Length; index++)
        {
            Assert.That(after[index].Node, Is.SameAs(before[index].Node));
            Assert.That(after[index].Span.Start.Offset, Is.EqualTo(before[index].Span.Start.Offset));
            Assert.That(after[index].Span.End.Offset, Is.EqualTo(before[index].Span.End.Offset));
        }
    }

    [Test]
    public void ResolvesReferencesAndRewritesDefinitionsLinksAndImageReferences()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = (IMarkdownDataServices)markdown;
        IMarkdownProcessingOperations operations = services.CreateProcessingOperations();
        MarkdownDocument document = markdown.ParseSource(
            "[old]: /one \"One\"\n[next]: /two \"Two\"\n\n[link][old] ![image][old]\n");
        IMarkdownTreeOperations trees = services.CreateTreeOperations();
        MarkdownNode[] definitions = trees.FindAllInDocument(document, MarkdownNodeKind.LinkReferenceDefinition);
        MarkdownNode link = trees.FindFirstInDocument(document, MarkdownNodeKind.Link)!;
        MarkdownNode image = trees.FindFirstInDocument(document, MarkdownNodeKind.Image)!;

        services.CreateNodeOperations(definitions[0]).SetAttribute("destination", "/changed");
        int resolved = operations.ResolveReferences(document);

        Assert.That(resolved, Is.EqualTo(2));
        Assert.That(services.CreateNodeOperations(link).GetAttributeValue("destination"), Is.EqualTo("/changed"));
        Assert.That(services.CreateNodeOperations(image).GetAttributeValue("destination"), Is.EqualTo("/changed"));

        int definitionsRewritten = operations.RewriteLinkDefinitions(
            document,
            new PrefixDestinationRewriter("/base"));
        int linksRewritten = operations.RewriteLinkDestinations(
            document,
            new SuffixDestinationRewriter("?tracked"));
        int imagesRewritten = operations.RewriteImageReferences(
            document,
            new FixedReferenceRewriter("next"));

        Assert.That(definitionsRewritten, Is.EqualTo(2));
        Assert.That(linksRewritten, Is.EqualTo(1));
        Assert.That(imagesRewritten, Is.EqualTo(1));
        Assert.That(services.CreateNodeOperations(definitions[0]).GetAttributeValue("destination"), Is.EqualTo("/base/changed"));
        Assert.That(services.CreateNodeOperations(link).GetAttributeValue("destination"), Is.EqualTo("/base/changed?tracked"));
        Assert.That(services.CreateNodeOperations(image).GetAttributeValue("referenceLabel"), Is.EqualTo("next"));
        Assert.That(services.CreateNodeOperations(image).GetAttributeValue("destination"), Is.EqualTo("/base/two"));
        Assert.That(services.CreateNodeOperations(image).GetAttributeValue("title"), Is.EqualTo("Two"));
    }

    [Test]
    public void MissingReferenceClearsStaleResolvedDestination()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = (IMarkdownDataServices)markdown;
        IMarkdownProcessingOperations operations = services.CreateProcessingOperations();
        MarkdownDocument document = markdown.ParseSource("[old]: /one\n\n![image][old]\n");
        MarkdownNode image = services.CreateTreeOperations()
            .FindFirstInDocument(document, MarkdownNodeKind.Image)!;

        int rewritten = operations.RewriteImageReferences(
            document,
            new FixedReferenceRewriter("missing"));

        Assert.That(rewritten, Is.EqualTo(1));
        Assert.That(services.CreateNodeOperations(image).GetAttributeValue("referenceLabel"), Is.EqualTo("missing"));
        Assert.That(services.CreateNodeOperations(image).GetAttributeValue("destination"), Is.EqualTo(""));
        Assert.That(services.CreateNodeOperations(image).ContainsAttribute("destination"), Is.False);
    }

    [Test]
    public void ProcessingOperationsRejectNullCollaboratorsAndNullRewriteResults()
    {
        IMarkdownProcessingOperations operations = new MarkdownProcessingOperations();
        MarkdownDocument document = new MarkdownDocument();
        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().Build();

        Assert.That(() => operations.NormalizeDocument(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(
            () => operations.ApplyDocumentTransformsInOrder(document, null!, pipeline),
            Throws.TypeOf<ArgumentNullException>());
        Assert.That(
            () => operations.ApplyNodeTransformsInOrder(document, new IMarkdownNodeTransform[] { null! }, pipeline),
            Throws.TypeOf<ArgumentNullException>());
        Assert.That(
            () => operations.RewriteLinkDestinations(document, null!),
            Throws.TypeOf<ArgumentNullException>());

        MarkdownDocument linked = new Markdown().ParseSource("[link](/one)");
        Assert.That(
            () => operations.RewriteLinkDestinations(linked, new NullDestinationRewriter()),
            Throws.TypeOf<InvalidOperationException>());
    }

    private sealed class AppendDocumentTransform : IMarkdownTransform
    {
        private readonly string name;
        private readonly string suffix;

        public AppendDocumentTransform(string name, string suffix)
        {
            this.name = name;
            this.suffix = suffix;
        }

        public string GetName()
        {
            return name;
        }

        public void Apply(MarkdownDocument document, MarkdownTransformContext context)
        {
            MarkdownNode? text = FindFirst(document.Root, MarkdownNodeKind.Text);
            if (text == null)
            {
                document.Source = document.Source + suffix;
            }
            else
            {
                text.Literal = text.Literal + suffix;
            }
        }
    }

    private sealed class AppendTextNodeTransform : IMarkdownNodeTransform
    {
        private readonly string name;
        private readonly string suffix;

        public AppendTextNodeTransform(string name, string suffix)
        {
            this.name = name;
            this.suffix = suffix;
        }

        public string GetNodeTransformName()
        {
            return name;
        }

        public void ApplyToNode(MarkdownNode node, MarkdownTransformContext context)
        {
            if (node.Kind == MarkdownNodeKind.Text)
            {
                node.Literal = node.Literal + suffix;
            }
        }
    }

    private sealed class PrefixDestinationRewriter : IMarkdownDestinationRewriter
    {
        private readonly string prefix;

        public PrefixDestinationRewriter(string prefix)
        {
            this.prefix = prefix;
        }

        public string RewriteDestination(MarkdownNode node, string destination)
        {
            return prefix + destination;
        }
    }

    private sealed class SuffixDestinationRewriter : IMarkdownDestinationRewriter
    {
        private readonly string suffix;

        public SuffixDestinationRewriter(string suffix)
        {
            this.suffix = suffix;
        }

        public string RewriteDestination(MarkdownNode node, string destination)
        {
            return destination + suffix;
        }
    }

    private sealed class FixedReferenceRewriter : IMarkdownReferenceRewriter
    {
        private readonly string replacement;

        public FixedReferenceRewriter(string replacement)
        {
            this.replacement = replacement;
        }

        public string RewriteReference(MarkdownNode node, string referenceLabel)
        {
            return replacement;
        }
    }

    private sealed class NullDestinationRewriter : IMarkdownDestinationRewriter
    {
        public string RewriteDestination(MarkdownNode node, string destination)
        {
            return null!;
        }
    }

    private static MarkdownNode? FindFirst(MarkdownNode node, MarkdownNodeKind kind)
    {
        if (node.Kind == kind)
        {
            return node;
        }

        for (int index = 0; index < node.Children.Count; index++)
        {
            MarkdownNode? found = FindFirst(node.Children[index], kind);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
