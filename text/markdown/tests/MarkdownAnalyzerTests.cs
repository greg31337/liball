using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownAnalyzerTests
{
    [Test]
    public void Analyze_ExtractsHeadingsLinksImagesAndPlainText()
    {
        string source =
            "# Title\n" +
            "\n" +
            "See [docs](https://example.com \"Docs\") and ![Logo **mark**](/logo.png \"Logo\").\n" +
            "\n" +
            "## Title";
        MarkdownPipeline pipeline = MarkdownPipeline.AdvancedPreset();

        MarkdownDocumentAnalysis analysis = MarkdownAnalyzer.Analyze(source, pipeline);

        Assert.That(analysis.GetHeadingCount(), Is.EqualTo(2));
        Assert.That(analysis.Headings[0].Level, Is.EqualTo(1));
        Assert.That(analysis.Headings[0].Text, Is.EqualTo("Title"));
        Assert.That(analysis.Headings[0].Id, Is.EqualTo("title"));
        Assert.That(analysis.Headings[1].Level, Is.EqualTo(2));
        Assert.That(analysis.Headings[1].Id, Is.EqualTo("title-1"));

        Assert.That(analysis.GetLinkCount(), Is.EqualTo(1));
        Assert.That(analysis.Links[0].Destination, Is.EqualTo("https://example.com"));
        Assert.That(analysis.Links[0].Title, Is.EqualTo("Docs"));
        Assert.That(analysis.Links[0].Text, Is.EqualTo("docs"));

        Assert.That(analysis.GetImageCount(), Is.EqualTo(1));
        Assert.That(analysis.Images[0].Destination, Is.EqualTo("/logo.png"));
        Assert.That(analysis.Images[0].Title, Is.EqualTo("Logo"));
        Assert.That(analysis.Images[0].AltText, Is.EqualTo("Logo mark"));

        Assert.That(analysis.PlainText, Is.EqualTo(
            "Title\n" +
            "\n" +
            "See docs and Logo mark.\n" +
            "\n" +
            "Title\n"));
        Assert.That(analysis.WordCount, Is.EqualTo(7));
        Assert.That(analysis.CharacterCount, Is.EqualTo(analysis.PlainText.Length));
    }

    [Test]
    public void Analyze_CountsNodesByKindFamily()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode text = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownNode link = MarkdownNode.Create(MarkdownNodeKind.Link);
        MarkdownNode linkText = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownNode image = MarkdownNode.Create(MarkdownNodeKind.Image);
        MarkdownNode imageText = MarkdownNode.Create(MarkdownNodeKind.Text);

        text.SetLiteral("alpha ");
        link.SetAttribute("destination", "/docs");
        linkText.SetLiteral("beta");
        image.SetAttribute("destination", "/logo.png");
        imageText.SetLiteral("gamma");
        link.AppendChild(linkText);
        image.AppendChild(imageText);
        paragraph.AppendChild(text);
        paragraph.AppendChild(link);
        paragraph.AppendChild(image);
        document.Root.AppendChild(paragraph);

        MarkdownDocumentAnalysis analysis = MarkdownAnalyzer.Analyze(document);

        Assert.That(analysis.NodeCount, Is.EqualTo(7));
        Assert.That(analysis.DocumentCount, Is.EqualTo(1));
        Assert.That(analysis.BlockCount, Is.EqualTo(1));
        Assert.That(analysis.InlineCount, Is.EqualTo(5));
        Assert.That(analysis.ExtensionCount, Is.EqualTo(0));
        Assert.That(analysis.TextNodeCount, Is.EqualTo(3));
        Assert.That(analysis.GetLinkCount(), Is.EqualTo(1));
        Assert.That(analysis.GetImageCount(), Is.EqualTo(1));
    }

    [Test]
    public void AnalysisSnapshotAccessorsReturnCopies()
    {
        MarkdownDocumentAnalysis analysis = MarkdownAnalyzer.Analyze("# Title\n\n[link](/url)\n\n![alt](/image.png)");

        MarkdownHeadingInfo[] headings = analysis.GetHeadings();
        MarkdownLinkInfo[] links = analysis.GetLinks();
        MarkdownImageInfo[] images = analysis.GetImages();

        headings[0].Text = "changed";
        links[0].Text = "changed";
        images[0].AltText = "changed";

        Assert.That(analysis.Headings[0].Text, Is.EqualTo("Title"));
        Assert.That(analysis.Links[0].Text, Is.EqualTo("link"));
        Assert.That(analysis.Images[0].AltText, Is.EqualTo("alt"));
    }

    [Test]
    public void CountWords_CountsLetterAndDigitRuns()
    {
        Assert.That(MarkdownAnalyzer.CountWords("one, two\n3.5 _x"), Is.EqualTo(5));
        Assert.That(MarkdownAnalyzer.CountWords(" \t\n"), Is.EqualTo(0));
    }

    [Test]
    public void AnalyzerRejectsInvalidArguments()
    {
        MarkdownDocumentAnalysis analysis = new MarkdownDocumentAnalysis();
        MarkdownNode heading = MarkdownNode.Create(MarkdownNodeKind.Heading);
        MarkdownNode link = MarkdownNode.Create(MarkdownNodeKind.Link);
        MarkdownNode image = MarkdownNode.Create(MarkdownNodeKind.Image);
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownPipeline pipeline = MarkdownPipeline.CommonMarkPreset();

        Assert.That(() => MarkdownAnalyzer.Analyze((MarkdownDocument)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownAnalyzer.Analyze((string)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownAnalyzer.Analyze("", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownAnalyzer.AnalyzeNode(null!, paragraph), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownAnalyzer.AnalyzeNode(analysis, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownAnalyzer.AddHeading(null!, heading), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownAnalyzer.AddHeading(analysis, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownAnalyzer.AddHeading(analysis, paragraph), Throws.TypeOf<ArgumentException>());
        Assert.That(() => MarkdownAnalyzer.AddLink(null!, link), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownAnalyzer.AddLink(analysis, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownAnalyzer.AddLink(analysis, paragraph), Throws.TypeOf<ArgumentException>());
        Assert.That(() => MarkdownAnalyzer.AddImage(null!, image), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownAnalyzer.AddImage(analysis, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownAnalyzer.AddImage(analysis, paragraph), Throws.TypeOf<ArgumentException>());
        Assert.That(() => MarkdownAnalyzer.CountWords(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownHeadingInfo(null!, 1, "", ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownHeadingInfo(heading, 0, "", ""), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => new MarkdownHeadingInfo(heading, 1, null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownHeadingInfo(heading, 1, "", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownLinkInfo(null!, "", "", ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownLinkInfo(link, null!, "", ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownLinkInfo(link, "", null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownLinkInfo(link, "", "", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownImageInfo(null!, "", "", ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownImageInfo(image, null!, "", ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownImageInfo(image, "", null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownImageInfo(image, "", "", null!), Throws.TypeOf<ArgumentNullException>());

        Assert.That(MarkdownAnalyzer.Analyze("", pipeline).NodeCount, Is.EqualTo(1));
    }
}
