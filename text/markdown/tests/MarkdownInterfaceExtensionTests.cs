using System.Collections.Generic;

namespace markdown.Tests;

[TestFixture]
public class MarkdownInterfaceExtensionTests
{
    [Test]
    public void BlockRuleInterface_ExtendsParserBeforeParagraphFallback()
    {
        IMarkdownBlockRule rule = new TestBlockRule(
            "test.bang-block",
            context =>
            {
                string line = context.GetCurrentLineText();
                if (!line.StartsWith("!!! ", StringComparison.Ordinal))
                {
                    return false;
                }

                MarkdownNode paragraph = MarkdownNode.Create(
                    MarkdownNodeKind.Paragraph,
                    context.GetCurrentLineSpan());
                paragraph.SetLiteral(line.Substring(4));
                context.AppendBlock(paragraph);
                context.AdvanceLine();
                return true;
            });
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .AddBlockRule(rule)
            .Build();

        MarkdownDocument document = MarkdownParser.Parse("!!! custom", pipeline);

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(Markdown.ToPlainText(document, pipeline), Is.EqualTo("custom\n"));
        Assert.That(pipeline.GetBlockRules().IndexOf(rule.GetName()),
            Is.LessThan(pipeline.GetBlockRules().IndexOf(CommonMarkBlockRules.ParagraphRuleName)));
    }

    [Test]
    public void InlineRuleInterface_ExtendsParserBeforeTextFallback()
    {
        IMarkdownInlineRule rule = new TestInlineRule(
            "test.mention",
            "@",
            context =>
            {
                string remaining = context.GetRemainingText();
                if (!remaining.StartsWith("@user", StringComparison.Ordinal))
                {
                    return false;
                }

                MarkdownNode text = MarkdownNode.Create(MarkdownNodeKind.Text);
                text.SetLiteral("MENTION");
                context.AppendInline(text);
                context.AdvanceBy(5);
                return true;
            });
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .AddInlineRule(rule)
            .Build();

        MarkdownDocument document = MarkdownParser.Parse("hello @user", pipeline);

        Assert.That(Markdown.ToPlainText(document, pipeline), Is.EqualTo("hello MENTION\n"));
        Assert.That(pipeline.GetInlineRules().IndexOf(rule.GetName()),
            Is.LessThan(pipeline.GetInlineRules().IndexOf(CommonMarkInlineRules.TextRuleName)));
    }

    [Test]
    public void TransformInterface_AllowsStatefulTestDouble()
    {
        int calls = 0;
        IMarkdownTransform transform = new TestTransform(
            "test.count-transform",
            (document, context) =>
            {
                calls = calls + 1;
                document.Root.SetAttribute("transformed", "true");
            });
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .AddTransform(transform)
            .Build();

        MarkdownDocument document = MarkdownParser.Parse("text", pipeline);

        Assert.That(calls, Is.EqualTo(1));
        Assert.That(document.Root.GetAttributeValue("transformed"), Is.EqualTo("true"));
    }

    [Test]
    public void HtmlRenderRuleInterface_HandlesSelectedNodesAndFallsBackForOthers()
    {
        int calls = 0;
        IHtmlRenderRule rule = new TestHtmlRenderRule(
            "test.heading-renderer",
            (context, node) =>
            {
                calls = calls + 1;
                if (node.Kind != MarkdownNodeKind.Heading)
                {
                    return false;
                }

                context.Writer.Write("<title>");
                HtmlRenderer.RenderChildren(context, node);
                context.Writer.Write("</title>");
                HtmlRenderer.WriteLineEnding(context);
                return true;
            });
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .AddHtmlRenderRule(rule)
            .Build();
        MarkdownDocument document = MarkdownParser.Parse("# Heading\n\nbody", pipeline);

        string html = HtmlRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<title>Heading</title>\n<p>body</p>\n"));
        Assert.That(calls, Is.GreaterThan(2));
    }

    [Test]
    public void NodeVisitorInterface_AllowsFocusedTraversalDouble()
    {
        MarkdownDocument document = MarkdownParser.Parse("text *emphasis*");
        List<MarkdownNodeKind> entered = new List<MarkdownNodeKind>();
        List<MarkdownNodeKind> left = new List<MarkdownNodeKind>();
        IMarkdownNodeVisitor visitor = new TestNodeVisitor(
            context =>
            {
                entered.Add(context.Node.Kind);
                return MarkdownTreeWalkResult.Continue;
            },
            context =>
            {
                left.Add(context.Node.Kind);
            });

        MarkdownTreeWalkResult result = MarkdownTreeWalker.Walk(document, visitor);

        Assert.That(result, Is.EqualTo(MarkdownTreeWalkResult.Continue));
        Assert.That(entered[0], Is.EqualTo(MarkdownNodeKind.Document));
        Assert.That(left[left.Count - 1], Is.EqualTo(MarkdownNodeKind.Document));
        Assert.That(entered.Count, Is.EqualTo(left.Count));
    }
}
