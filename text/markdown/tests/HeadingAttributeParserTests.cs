using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class HeadingAttributeParserTests
{
    [Test]
    public void HeadingAttributeParser_ParsesAtxHeadingAttributes()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.AdvancedPreset();
        MarkdownDocument document = MarkdownParser.Parse(
            "# Title *x* {#custom .lead .wide data-kind=\"hero\" key=value}",
            pipeline);
        MarkdownNode heading = document.Root.GetChild(0);

        Assert.That(heading.Literal, Is.EqualTo("Title *x*"));
        Assert.That(heading.GetAttributeValue("id"), Is.EqualTo("custom"));
        Assert.That(heading.GetAttributeValue("class"), Is.EqualTo("lead wide"));
        Assert.That(heading.GetAttributeValue("data-kind"), Is.EqualTo("hero"));
        Assert.That(heading.GetAttributeValue("key"), Is.EqualTo("value"));
        Assert.That(heading.GetAttributeValue("explicitAttributes"), Is.EqualTo("true"));
        Assert.That(HtmlRenderer.Render(document, pipeline), Is.EqualTo(
            "<h1 id=\"custom\" class=\"lead wide\" data-kind=\"hero\" key=\"value\">Title <em>x</em></h1>\n"));
        Assert.That(MarkdownRenderer.Render(document, pipeline), Is.EqualTo(
            "# Title *x* {#custom .lead .wide data-kind=\"hero\" key=\"value\"}\n"));
    }

    [Test]
    public void HeadingAttributeParser_ParsesSetextHeadingAttributes()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "Title {#setext .lead}\n" +
            "---",
            MarkdownPipeline.AdvancedPreset());
        MarkdownNode heading = document.Root.GetChild(0);

        Assert.That(heading.Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(heading.GetAttributeValue("level"), Is.EqualTo("2"));
        Assert.That(heading.GetAttributeValue("id"), Is.EqualTo("setext"));
        Assert.That(heading.GetAttributeValue("class"), Is.EqualTo("lead"));
        Assert.That(heading.Literal, Is.EqualTo("Title"));
    }

    [Test]
    public void HeadingAttributeParser_DoesNotParseWithoutAttributeFeature()
    {
        MarkdownDocument document = MarkdownParser.Parse("# Title {#custom}", MarkdownPipeline.CommonMarkPreset());

        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo("Title {#custom}"));
        Assert.That(document.Root.GetChild(0).GetAttributeValue("id"), Is.EqualTo(""));
        Assert.That(HtmlRenderer.Render(document, MarkdownPipeline.CommonMarkPreset()), Is.EqualTo("<h1>Title {#custom}</h1>\n"));
    }

    [Test]
    public void HeadingAttributeParser_LeavesUnrecognizedBraceTextInHeading()
    {
        MarkdownDocument document = MarkdownParser.Parse("# Title {not attributes}", MarkdownPipeline.AdvancedPreset());
        MarkdownNode heading = document.Root.GetChild(0);

        Assert.That(heading.Literal, Is.EqualTo("Title {not attributes}"));
        Assert.That(heading.GetAttributeValue("explicitAttributes"), Is.EqualTo(""));
        Assert.That(heading.GetAttributeValue("id"), Is.EqualTo("title-not-attributes"));
    }

    [Test]
    public void HeadingAttributeParser_ExplicitIdsReserveGeneratedCollisions()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "# One {#one}\n" +
            "\n" +
            "# One",
            MarkdownPipeline.AdvancedPreset());

        Assert.That(document.Root.GetChild(0).GetAttributeValue("id"), Is.EqualTo("one"));
        Assert.That(document.Root.GetChild(1).GetAttributeValue("id"), Is.EqualTo("one-1"));
    }

    [Test]
    public void HeadingAttributeParser_HelperMethodsParseTokens()
    {
        List<string> tokens = HeadingAttributeParser.TokenizeAttributeBlock("#id .a .b data-kind=\"hero block\" key=value");
        MarkdownNode heading = MarkdownNode.Create(MarkdownNodeKind.Heading);
        heading.SetLiteral("Title {#id}");

        Assert.That(tokens.Count, Is.EqualTo(5));
        Assert.That(tokens[3], Is.EqualTo("data-kind=hero block"));
        Assert.That(HeadingAttributeParser.AreAttributeTokens(tokens), Is.True);
        Assert.That(HeadingAttributeParser.IsAttributeToken("#id"), Is.True);
        Assert.That(HeadingAttributeParser.IsAttributeToken("plain"), Is.False);
        Assert.That(HeadingAttributeParser.FindTrailingAttributeStart("Title {#id}  "), Is.EqualTo(6));
        Assert.That(HeadingAttributeParser.FindTrailingAttributeEnd("Title {#id}  "), Is.EqualTo(10));
        HeadingAttributeParser.ApplyAttributeToken(heading, ".lead");
        HeadingAttributeParser.ApplyAttributeToken(heading, ".wide");
        Assert.That(heading.GetAttributeValue("class"), Is.EqualTo("lead wide"));
    }

    [Test]
    public void HeadingAttributeParser_RejectsInvalidArguments()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.AdvancedPreset();
        MarkdownNode heading = MarkdownNode.Create(MarkdownNodeKind.Heading);
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        List<string> tokens = new List<string>();
        StringWriter writer = new StringWriter();
        HtmlRenderContext htmlContext = new HtmlRenderContext(new HtmlWriter(new StringWriter()), pipeline);
        MarkdownRenderContext markdownContext = new MarkdownRenderContext(new MarkdownWriter(new StringWriter()), pipeline);

        Assert.That(() => HeadingAttributeParser.TryApplyHeadingAttributes(null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingAttributeParser.TryApplyHeadingAttributes(heading, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingAttributeParser.TryApplyHeadingAttributes(paragraph, pipeline), Throws.TypeOf<ArgumentException>());
        Assert.That(() => HeadingAttributeParser.FindTrailingAttributeStart(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingAttributeParser.FindTrailingAttributeEnd(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingAttributeParser.TokenizeAttributeBlock(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingAttributeParser.AddToken(null!, writer), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingAttributeParser.AddToken(tokens, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingAttributeParser.AreAttributeTokens(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingAttributeParser.IsAttributeToken(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingAttributeParser.ApplyAttributeToken(null!, "#id"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingAttributeParser.ApplyAttributeToken(heading, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingAttributeParser.AddClass(null!, "lead"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingAttributeParser.AddClass(heading, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.WriteHeadingAttributes(null!, heading), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.WriteHeadingAttributes(htmlContext, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.ShouldRenderHeadingAttribute(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.WriteExplicitHeadingAttributes(null!, heading), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.WriteExplicitHeadingAttributes(markdownContext, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.GetExplicitHeadingAttributeText(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.ShouldRenderExplicitHeadingAttribute(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.EscapeHeadingAttributeValue(null!), Throws.TypeOf<ArgumentNullException>());
    }
}
