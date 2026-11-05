using System.Collections.Generic;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class HeadingIdGeneratorTests
{
    [Test]
    public void HeadingIdGenerator_AddsIdsAndHandlesCollisionsInAdvancedPipeline()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "# Hello, World!\n" +
            "\n" +
            "## Hello *World*\n" +
            "\n" +
            "# !!!",
            MarkdownPipeline.AdvancedPreset());

        Assert.That(document.Root.GetChild(0).GetAttributeValue("id"), Is.EqualTo("hello-world"));
        Assert.That(document.Root.GetChild(1).GetAttributeValue("id"), Is.EqualTo("hello-world-1"));
        Assert.That(document.Root.GetChild(2).GetAttributeValue("id"), Is.EqualTo("section"));
    }

    [Test]
    public void HeadingIdGenerator_RendersHeadingIdsToHtml()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "# Hello, World!\n" +
            "\n" +
            "## Hello *World*",
            MarkdownPipeline.AdvancedPreset());

        string html = HtmlRenderer.Render(document, MarkdownPipeline.AdvancedPreset());

        Assert.That(html, Is.EqualTo(
            "<h1 id=\"hello-world\">Hello, World!</h1>\n" +
            "<h2 id=\"hello-world-1\">Hello <em>World</em></h2>\n"));
    }

    [Test]
    public void HeadingIdGenerator_DoesNotRunWithoutHeadingIdFeature()
    {
        MarkdownDocument document = MarkdownParser.Parse("# Hello", MarkdownPipeline.CommonMarkPreset());

        string html = HtmlRenderer.Render(document, MarkdownPipeline.CommonMarkPreset());

        Assert.That(document.Root.GetChild(0).GetAttributeValue("id"), Is.EqualTo(""));
        Assert.That(html, Is.EqualTo("<h1>Hello</h1>\n"));
    }

    [Test]
    public void HeadingIdGenerator_ReservesExistingHeadingIds()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode first = CreateHeading("Hello World");
        MarkdownNode second = CreateHeading("Hello World");
        first.SetAttribute("id", "hello-world");
        document.Root.AppendChild(first);
        document.Root.AppendChild(second);

        HeadingIdGenerator.Apply(document);

        Assert.That(first.GetAttributeValue("id"), Is.EqualTo("hello-world"));
        Assert.That(second.GetAttributeValue("id"), Is.EqualTo("hello-world-1"));
    }

    [Test]
    public void HeadingIdGenerator_EscapesManualHeadingIdsInHtml()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode heading = CreateHeading("Hello");
        heading.SetAttribute("id", "a&b");
        document.Root.AppendChild(heading);

        string html = HtmlRenderer.Render(document, MarkdownPipeline.CommonMarkPreset());

        Assert.That(html, Is.EqualTo("<h1 id=\"a&amp;b\">Hello</h1>\n"));
    }

    [Test]
    public void HeadingIdGenerator_HelperMethodsGenerateSlugs()
    {
        List<string> usedIds = new List<string>();
        MarkdownNode heading = MarkdownNode.Create(MarkdownNodeKind.Heading);
        MarkdownNode text = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownNode code = MarkdownNode.Create(MarkdownNodeKind.CodeSpan);
        text.SetLiteral("API ");
        code.SetLiteral("v1");
        heading.AppendChild(text);
        heading.AppendChild(code);

        Assert.That(HeadingIdGenerator.GetHeadingText(heading), Is.EqualTo("API v1"));
        Assert.That(HeadingIdGenerator.GenerateHeadingId(" Hello,   World! "), Is.EqualTo("hello-world"));
        Assert.That(HeadingIdGenerator.GenerateHeadingId("!!!"), Is.EqualTo("section"));
        Assert.That(HeadingIdGenerator.MakeUniqueHeadingId(usedIds, "hello"), Is.EqualTo("hello"));
        Assert.That(HeadingIdGenerator.MakeUniqueHeadingId(usedIds, "hello"), Is.EqualTo("hello-1"));
        Assert.That(HeadingIdGenerator.ContainsHeadingId(usedIds, "hello"), Is.True);
        HeadingIdGenerator.ReserveHeadingId(usedIds, "manual");
        Assert.That(HeadingIdGenerator.ContainsHeadingId(usedIds, "manual"), Is.True);
    }

    [Test]
    public void HeadingIdGenerator_RejectsInvalidArguments()
    {
        List<string> usedIds = new List<string>();
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode heading = CreateHeading("Hello");

        Assert.That(() => HeadingIdGenerator.Apply(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingIdGenerator.ApplyNode(null!, usedIds), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingIdGenerator.ApplyNode(heading, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingIdGenerator.ApplyHeading(null!, usedIds), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingIdGenerator.ApplyHeading(heading, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingIdGenerator.ApplyHeading(paragraph, usedIds), Throws.TypeOf<ArgumentException>());
        Assert.That(() => HeadingIdGenerator.GetHeadingText(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingIdGenerator.GenerateHeadingId(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingIdGenerator.MakeUniqueHeadingId(null!, "id"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingIdGenerator.MakeUniqueHeadingId(usedIds, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingIdGenerator.ReserveHeadingId(null!, "id"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingIdGenerator.ReserveHeadingId(usedIds, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingIdGenerator.ContainsHeadingId(null!, "id"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HeadingIdGenerator.ContainsHeadingId(usedIds, null!), Throws.TypeOf<ArgumentNullException>());
    }

    static MarkdownNode CreateHeading(string text)
    {
        MarkdownNode heading = MarkdownNode.Create(MarkdownNodeKind.Heading);
        MarkdownNode child = MarkdownNode.Create(MarkdownNodeKind.Text);
        heading.SetAttribute("level", "1");
        child.SetLiteral(text);
        heading.AppendChild(child);
        return heading;
    }
}
