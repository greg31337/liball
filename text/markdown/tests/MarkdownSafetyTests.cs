using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownSafetyTests
{
    [Test]
    public void HtmlRendering_EscapesRawHtmlByDefault()
    {
        string html = Markdown.ToHtml(
            "<script>alert(1)</script>\n" +
            "\n" +
            "text <img src=x onerror=alert(1)> <span>inline</span>");

        Assert.That(html, Is.EqualTo(
            "&lt;script&gt;alert(1)&lt;/script&gt;\n" +
            "<p>text &lt;img src=x onerror=alert(1)&gt; &lt;span&gt;inline&lt;/span&gt;</p>\n"));
    }

    [Test]
    public void GfmRendering_FiltersDisallowedRawHtmlUnlessUnsafeHtmlIsAllowed()
    {
        MarkdownPipeline rawHtml = MarkdownPipelineBuilder.GfmPreset()
            .AllowRawHtml()
            .Build();
        MarkdownPipeline unsafeHtml = MarkdownPipelineBuilder.GfmPreset()
            .AllowUnsafeHtml()
            .Build();

        Assert.That(
            Markdown.ToHtml("<script>alert(1)</script>", rawHtml),
            Is.EqualTo("&lt;script>alert(1)&lt;/script>\n"));
        Assert.That(
            Markdown.ToHtml("<script>alert(1)</script>", unsafeHtml),
            Is.EqualTo("<script>alert(1)</script>\n"));
    }

    [Test]
    public void HtmlRendering_SuppressesDangerousLinkSchemesByDefault()
    {
        MarkdownDocument document = CreateDestinationDocument(
            "javascript",
            "javascript:alert(1)",
            "vbscript",
            "vbscript:msgbox(1)",
            "file",
            "file:///etc/passwd");

        string html = Markdown.ToHtml(document);

        Assert.That(html, Is.EqualTo(
            "<p><a>javascript</a> <a>vbscript</a> <a>file</a></p>\n"));
    }

    [Test]
    public void HtmlRendering_TrimsControlsBeforeCheckingDangerousSchemes()
    {
        MarkdownDocument document = CreateDestinationDocument(
            "link",
            " \t\nJaVaScRiPt:alert(1)\r ",
            "image",
            " \t\nJaVaScRiPt:alert(1)\r ");

        string html = Markdown.ToHtml(document);

        Assert.That(html, Is.EqualTo("<p><a>link</a> <img alt=\"image\"></p>\n"));
    }

    [Test]
    public void HtmlRendering_AllowsConfiguredUnsafeLinks()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .AllowUnsafeLinks()
            .Build();
        MarkdownDocument document = CreateDestinationDocument(
            "link",
            "javascript:alert(1)",
            "image",
            "javascript:alert(1)");

        string html = Markdown.ToHtml(document, pipeline);

        Assert.That(html, Is.EqualTo(
            "<p><a href=\"javascript:alert(1)\">link</a> " +
            "<img src=\"javascript:alert(1)\" alt=\"image\"></p>\n"));
    }

    [Test]
    public void HtmlRendering_AllowsDataImagesButNotDataLinks()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        paragraph.AppendChild(CreateLink("link", "data:image/png;base64,abc"));
        paragraph.AppendChild(CreateText(" "));
        paragraph.AppendChild(CreateImage("png", "data:image/png;base64,abc"));
        paragraph.AppendChild(CreateText(" "));
        paragraph.AppendChild(CreateImage("svg", "data:image/svg+xml;base64,abc"));
        document.Root.AppendChild(paragraph);

        string html = Markdown.ToHtml(document);

        Assert.That(html, Is.EqualTo(
            "<p><a>link</a> <img src=\"data:image/png;base64,abc\" alt=\"png\"> <img alt=\"svg\"></p>\n"));
    }

    public static MarkdownDocument CreateDestinationDocument(
        string firstLabel,
        string firstDestination,
        string secondLabel,
        string secondDestination)
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        paragraph.AppendChild(CreateLink(firstLabel, firstDestination));
        paragraph.AppendChild(CreateText(" "));
        paragraph.AppendChild(CreateImage(secondLabel, secondDestination));
        document.Root.AppendChild(paragraph);
        return document;
    }

    public static MarkdownDocument CreateDestinationDocument(
        string firstLabel,
        string firstDestination,
        string secondLabel,
        string secondDestination,
        string thirdLabel,
        string thirdDestination)
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        paragraph.AppendChild(CreateLink(firstLabel, firstDestination));
        paragraph.AppendChild(CreateText(" "));
        paragraph.AppendChild(CreateLink(secondLabel, secondDestination));
        paragraph.AppendChild(CreateText(" "));
        paragraph.AppendChild(CreateLink(thirdLabel, thirdDestination));
        document.Root.AppendChild(paragraph);
        return document;
    }

    public static MarkdownNode CreateLink(string text, string destination)
    {
        MarkdownNode link = MarkdownNode.Create(MarkdownNodeKind.Link);
        link.SetAttribute("destination", destination);
        link.AppendChild(CreateText(text));
        return link;
    }

    public static MarkdownNode CreateImage(string alt, string destination)
    {
        MarkdownNode image = MarkdownNode.Create(MarkdownNodeKind.Image);
        image.SetAttribute("destination", destination);
        image.AppendChild(CreateText(alt));
        return image;
    }

    public static MarkdownNode CreateText(string text)
    {
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Text);
        node.SetLiteral(text);
        return node;
    }
}
