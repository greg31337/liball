using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class CjkSoftBreakTests
{
    [Test]
    public void CjkSoftBreaks_CollapseBetweenCjkCharactersInHtmlAndPlainText()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.AdvancedPreset()
            .UseCjkSoftBreaks()
            .Build();
        MarkdownDocument document = MarkdownParser.Parse("漢字\n仮名", pipeline);

        string html = HtmlRenderer.Render(document, pipeline);
        string plainText = PlainTextRenderer.Render(document, pipeline);
        string markdown = MarkdownRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<p>漢字仮名</p>\n"));
        Assert.That(plainText, Is.EqualTo("漢字仮名\n"));
        Assert.That(markdown, Is.EqualTo("漢字\n仮名\n"));
    }

    [Test]
    public void CjkSoftBreaks_PreserveBreakWhenAdjacentCharactersAreNotBothCjk()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.AdvancedPreset()
            .UseCjkSoftBreaks()
            .Build();
        MarkdownDocument document = MarkdownParser.Parse("漢字\nabc\n仮名", pipeline);

        string html = HtmlRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<p>漢字\nabc\n仮名</p>\n"));
    }

    [Test]
    public void CjkSoftBreaks_DoNotCollapseWithoutFeature()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .SetSoftBreakMode(MarkdownSoftBreakMode.Cjk)
            .Build();
        MarkdownDocument document = MarkdownParser.Parse("漢字\n仮名", pipeline);

        string html = HtmlRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<p>漢字\n仮名</p>\n"));
    }

    [Test]
    public void CjkSoftBreaks_WorkAcrossFormattedInlineSiblings()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.AdvancedPreset()
            .UseCjkSoftBreaks()
            .Build();
        MarkdownDocument document = MarkdownParser.Parse("漢**字**\n仮名", pipeline);

        string html = HtmlRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<p>漢<strong>字</strong>仮名</p>\n"));
    }

    [Test]
    public void CjkSoftBreaks_HelperMethodsClassifyCharacters()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.AdvancedPreset()
            .UseCjkSoftBreaks()
            .Build();
        MarkdownDocument document = MarkdownParser.Parse("漢\n字", pipeline);
        MarkdownNode softBreak = FindFirstKind(document.Root, MarkdownNodeKind.SoftLineBreak)!;

        Assert.That(CjkSoftBreaks.IsCjkCharacter('漢'), Is.True);
        Assert.That(CjkSoftBreaks.IsCjkCharacter('あ'), Is.True);
        Assert.That(CjkSoftBreaks.IsCjkCharacter('한'), Is.True);
        Assert.That(CjkSoftBreaks.IsCjkCharacter('a'), Is.False);
        Assert.That(CjkSoftBreaks.GetPreviousCharacter(softBreak), Is.EqualTo('漢'));
        Assert.That(CjkSoftBreaks.GetNextCharacter(softBreak), Is.EqualTo('字'));
        Assert.That(CjkSoftBreaks.ShouldCollapse(pipeline, pipeline.GetOptions(), softBreak), Is.True);
    }

    [Test]
    public void CjkSoftBreaks_RejectInvalidArguments()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.AdvancedPreset()
            .UseCjkSoftBreaks()
            .Build();
        MarkdownNode text = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownNode softBreak = MarkdownNode.Create(MarkdownNodeKind.SoftLineBreak);

        Assert.That(() => CjkSoftBreaks.ShouldCollapse(null!, pipeline.GetOptions(), softBreak), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CjkSoftBreaks.ShouldCollapse(pipeline, null!, softBreak), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CjkSoftBreaks.ShouldCollapse(pipeline, pipeline.GetOptions(), null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CjkSoftBreaks.ShouldCollapse(pipeline, pipeline.GetOptions(), text), Throws.TypeOf<ArgumentException>());
        Assert.That(() => CjkSoftBreaks.GetPreviousCharacter(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CjkSoftBreaks.GetNextCharacter(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CjkSoftBreaks.GetPlainText(null!), Throws.TypeOf<ArgumentNullException>());
    }

    static MarkdownNode? FindFirstKind(MarkdownNode node, MarkdownNodeKind kind)
    {
        if (node.Kind == kind)
        {
            return node;
        }

        int index = 0;
        while (index < node.GetChildCount())
        {
            MarkdownNode? result = FindFirstKind(node.GetChild(index), kind);
            if (result != null)
            {
                return result;
            }

            index = index + 1;
        }

        return null;
    }
}
