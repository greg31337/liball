using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownTransformPipelineTests
{
    [Test]
    public void Parser_RunsDocumentTransformsAfterBuiltInTransforms()
    {
        MarkdownTransform transform = MarkdownTransform.ForDocument(
            "capture-heading-id",
            (document, context) =>
            {
                MarkdownNode heading = document.Root.GetChild(0);
                heading.SetAttribute("seen-id", heading.GetAttributeValue("id"));
                heading.SetAttribute("source-length", context.Source.Length.ToString());
            });
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.AdvancedPreset()
            .AddTransform(transform)
            .Build();

        MarkdownDocument document = MarkdownParser.Parse("# Title", pipeline);
        MarkdownNode parsedHeading = document.Root.GetChild(0);

        Assert.That(parsedHeading.GetAttributeValue("id"), Is.EqualTo("title"));
        Assert.That(parsedHeading.GetAttributeValue("seen-id"), Is.EqualTo("title"));
        Assert.That(parsedHeading.GetAttributeValue("source-length"), Is.EqualTo("7"));
    }

    [Test]
    public void NodeTransform_VisitsEveryNodeAndCanRewriteText()
    {
        MarkdownTransform transform = MarkdownTransform.ForNode(
            "uppercase-text",
            (node, context) =>
            {
                if (node.Kind == MarkdownNodeKind.Text)
                {
                    node.SetLiteral(node.Literal.ToUpperInvariant());
                }
            });
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .AddTransform(transform)
            .Build();

        string text = Markdown.ToPlainText("hello *world*", pipeline);

        Assert.That(text, Is.EqualTo("HELLO WORLD\n"));
    }

    [Test]
    public void TransformPipeline_AppliesTransformsInRegistrationOrder()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        document.Root.AppendChild(paragraph);
        MarkdownTransform first = MarkdownTransform.ForDocument(
            "first",
            (currentDocument, context) =>
            {
                MarkdownNode text = MarkdownNode.Create(MarkdownNodeKind.Text);
                text.SetLiteral("a");
                currentDocument.Root.GetChild(0).AppendChild(text);
            });
        MarkdownTransform second = MarkdownTransform.ForDocument(
            "second",
            (currentDocument, context) =>
            {
                MarkdownNode text = MarkdownNode.Create(MarkdownNodeKind.Text);
                text.SetLiteral(currentDocument.Root.GetChild(0).GetChild(0).Literal + "b");
                currentDocument.Root.GetChild(0).AppendChild(text);
            });
        MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
            .AddTransform(first)
            .AddTransform(second)
            .Build();

        MarkdownDocument returned = MarkdownTransformPipeline.Apply(document, pipeline);

        Assert.That(returned, Is.SameAs(document));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("a"));
        Assert.That(paragraph.GetChild(1).Literal, Is.EqualTo("ab"));
    }

    [Test]
    public void TransformContext_AddsDiagnosticsToDocument()
    {
        MarkdownTransform transform = MarkdownTransform.ForDocument(
            "diagnostic",
            (document, context) =>
            {
                context.AddDiagnosticValues(
                    MarkdownDiagnosticSeverity.Warning,
                    "MD_TEST",
                    "test diagnostic",
                    document.Root.Span);
            });
        MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
            .AddTransform(transform)
            .Build();
        MarkdownDocument document = new MarkdownDocument();

        MarkdownTransformPipeline.Apply(document, pipeline);

        Assert.That(document.HasDiagnostics(), Is.True);
        Assert.That(document.GetDiagnostics()[0].Code, Is.EqualTo("MD_TEST"));
        Assert.That(document.GetDiagnostics()[0].Severity, Is.EqualTo(MarkdownDiagnosticSeverity.Warning));
    }

    [Test]
    public void TransformPipeline_ReportsInvalidRegisteredRule()
    {
        MarkdownRuleList transformRules = new MarkdownRuleList();
        transformRules.Add("bad", new object());
        MarkdownPipeline pipeline = MarkdownPipeline.Create(
            MarkdownOptions.CommonMarkPreset(),
            MarkdownPipeline.CommonMarkPreset().GetBlockRules(),
            MarkdownPipeline.CommonMarkPreset().GetInlineRules(),
            transformRules,
            new MarkdownRuleList());

        MarkdownDocument document = MarkdownParser.Parse("text", pipeline);

        Assert.That(document.HasErrors(), Is.True);
        Assert.That(document.GetDiagnostics()[0].Code, Is.EqualTo(MarkdownTransformPipeline.InvalidRuleDiagnosticCode));
    }

    [Test]
    public void Builder_RegistersSetsRemovesAndClearsTypedTransforms()
    {
        MarkdownTransform first = MarkdownTransform.ForDocument(
            "rewrite",
            (document, context) =>
            {
                document.Root.SetAttribute("value", "first");
            });
        MarkdownTransform second = MarkdownTransform.ForDocument(
            "rewrite",
            (document, context) =>
            {
                document.Root.SetAttribute("value", "second");
            });
        MarkdownTransform kept = MarkdownTransform.ForDocument(
            "kept",
            (document, context) =>
            {
                document.Root.SetAttribute("kept", "true");
            });

        MarkdownPipelineBuilder builder = new MarkdownPipelineBuilder()
            .AddTransform(first)
            .SetTransform(second)
            .AddTransform(kept)
            .RemoveTransform("missing")
            .RemoveTransform("kept");

        Assert.That(builder.GetTransformRules().GetCount(), Is.EqualTo(1));
        Assert.That(builder.GetTransformRules().FindRule("rewrite"), Is.SameAs(second));

        builder.ClearTransforms();

        Assert.That(builder.GetTransformRules().GetCount(), Is.EqualTo(0));
    }

    [Test]
    public void TransformTypesRejectInvalidArguments()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownPipeline pipeline = MarkdownPipeline.CommonMarkPreset();
        MarkdownTransformContext context = new MarkdownTransformContext(document, pipeline);
        MarkdownTransform transform = new MarkdownTransform("empty");

        Assert.That(() => new MarkdownTransform(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownTransform(""), Throws.TypeOf<ArgumentException>());
        Assert.That(() => MarkdownTransform.ForDocument("document", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownTransform.ForDocument("document", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownTransform.ForNode("node", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownTransformContext.Create((MarkdownParseContext)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownTransformContext(null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownTransformContext(document, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownTransformPipeline.Apply((MarkdownDocument)null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownTransformPipeline.Apply(document, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownTransformPipeline.Apply((MarkdownParseContext)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownTransformPipeline.Run(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => transform.Apply(null!, context), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => transform.Apply(document, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => transform.ApplyToNodes(null!, context), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => transform.ApplyToNodes(document, null!), Throws.TypeOf<ArgumentNullException>());
    }
}
