using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class DiagramBlockRulesTests
{
    [Test]
    public void DiagramBlockRule_CapturesRecognizedFencedCodeMetadata()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "```mermaid theme=dark\n" +
            "graph TD\n" +
            "  A-->B\n" +
            "```\n" +
            "\n" +
            "after",
            MarkdownPipeline.AdvancedPreset());

        MarkdownNode diagram = document.Root.GetChild(0);

        Assert.That(diagram.Kind, Is.EqualTo(MarkdownNodeKind.DiagramBlock));
        Assert.That(diagram.Literal, Is.EqualTo("graph TD\n  A-->B"));
        Assert.That(diagram.GetAttributeValue("info"), Is.EqualTo("mermaid theme=dark"));
        Assert.That(diagram.GetAttributeValue("diagramType"), Is.EqualTo("mermaid"));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
    }

    [Test]
    public void DiagramBlockRule_PreservesFencedCodeFallbackForUnknownMetadata()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "```csharp\n" +
            "Console.WriteLine();\n" +
            "```",
            MarkdownPipeline.AdvancedPreset());

        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.FencedCodeBlock));
    }

    [Test]
    public void DiagramBlockRule_DoesNotParseWithoutDiagramFeature()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "```mermaid\n" +
            "graph TD\n" +
            "```",
            MarkdownPipeline.CommonMarkPreset());

        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.FencedCodeBlock));
    }

    [Test]
    public void DiagramBlockRule_AllowsUnclosedFenceToEndAtDocumentEnd()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "```plantuml\n" +
            "@startuml\n" +
            "Alice -> Bob",
            MarkdownPipeline.AdvancedPreset());

        MarkdownNode diagram = document.Root.GetChild(0);

        Assert.That(diagram.Kind, Is.EqualTo(MarkdownNodeKind.DiagramBlock));
        Assert.That(diagram.GetAttributeValue("diagramType"), Is.EqualTo("plantuml"));
        Assert.That(diagram.Literal, Is.EqualTo("@startuml\nAlice -> Bob"));
    }

    [Test]
    public void DiagramBlockRule_RenderersPreserveSourceAndExposeHtmlMetadata()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.AdvancedPreset();
        MarkdownDocument document = MarkdownParser.Parse(
            "```mermaid\n" +
            "graph TD\n" +
            "  A-->B\n" +
            "```",
            pipeline);

        string html = HtmlRenderer.Render(document, pipeline);
        string markdown = MarkdownRenderer.Render(document, pipeline);
        string plainText = PlainTextRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo(
            "<pre><code class=\"language-mermaid\" data-diagram=\"mermaid\">graph TD\n" +
            "  A--&gt;B\n" +
            "</code></pre>\n"));
        Assert.That(markdown, Is.EqualTo(
            "```mermaid\n" +
            "graph TD\n" +
            "  A-->B\n" +
            "```\n"));
        Assert.That(plainText, Is.EqualTo("graph TD\n  A-->B\n"));
    }

    [Test]
    public void DiagramBlockRule_IsRegisteredForAdvancedPipelines()
    {
        MarkdownRuleList rules = MarkdownPipeline.AdvancedPreset().GetBlockRules();
        MarkdownPipelineBuilder builder = MarkdownPipelineBuilder.AdvancedPreset();

        builder.UseGfm();

        Assert.That(rules.Contains(DiagramBlockRules.DiagramBlockRuleName), Is.True);
        Assert.That(rules.IndexOf(DiagramBlockRules.DiagramBlockRuleName), Is.LessThan(rules.IndexOf(CommonMarkBlockRules.FencedCodeBlockRuleName)));
        Assert.That(builder.GetBlockRules().Contains(DiagramBlockRules.DiagramBlockRuleName), Is.False);
    }

    [Test]
    public void DiagramBlockRule_HelperMethodsClassifyDiagramTypes()
    {
        Assert.That(DiagramBlockRules.GetDiagramType("mermaid theme=dark"), Is.EqualTo("mermaid"));
        Assert.That(DiagramBlockRules.GetDiagramType("graphviz"), Is.EqualTo("graphviz"));
        Assert.That(DiagramBlockRules.GetDiagramType("csharp"), Is.EqualTo(""));
        Assert.That(DiagramBlockRules.IsDiagramType("D2"), Is.True);
        Assert.That(DiagramBlockRules.IsDiagramType("plantuml"), Is.True);
        Assert.That(DiagramBlockRules.IsDiagramType("dot"), Is.True);
        Assert.That(DiagramBlockRules.IsDiagramType("unknown"), Is.False);
    }

    [Test]
    public void DiagramBlockRule_RejectsInvalidArguments()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("", MarkdownPipeline.AdvancedPreset());

        Assert.That(() => DiagramBlockRules.RegisterDiagramBlockRules(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DiagramBlockRules.TryParseDiagramBlock(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DiagramBlockRules.GetDiagramType(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DiagramBlockRules.IsDiagramType(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(DiagramBlockRules.TryParseDiagramBlock(context), Is.False);
    }
}
