using System.Collections.Generic;
using NUnit.Framework;

namespace markdown.Tests;

public class ExtensionFeatureCase
{
    public string FeatureName;
    public string Source;
    public MarkdownNodeKind ExpectedKind;
    public string AttributeName;
    public string AttributeValue;
    public string ExpectedLiteral;
    public string HtmlFragment;
    public string MarkdownFragment;
    public string PlainTextFragment;
    public string DisabledTextFragment;

    public ExtensionFeatureCase(
        string featureName,
        string source,
        MarkdownNodeKind expectedKind,
        string attributeName,
        string attributeValue,
        string expectedLiteral,
        string htmlFragment,
        string markdownFragment,
        string plainTextFragment,
        string disabledTextFragment)
    {
        FeatureName = featureName;
        Source = source;
        ExpectedKind = expectedKind;
        AttributeName = attributeName;
        AttributeValue = attributeValue;
        ExpectedLiteral = expectedLiteral;
        HtmlFragment = htmlFragment;
        MarkdownFragment = markdownFragment;
        PlainTextFragment = plainTextFragment;
        DisabledTextFragment = disabledTextFragment;
    }

    public override string ToString()
    {
        return FeatureName;
    }
}

[TestFixture]
public class ExtensionFeatureMatrixTests
{
    [TestCaseSource(nameof(ExtensionCases))]
    public void IndividualExtensionFlag_ControlsPublicTreeAttributesAndOutputs(ExtensionFeatureCase feature)
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline enabledPipeline = CreatePipeline(markdown, feature.FeatureName, true);
        MarkdownPipeline disabledPipeline = CreatePipeline(markdown, feature.FeatureName, false);

        MarkdownDocument enabled = markdown.ParseSourceWithPipeline(feature.Source, enabledPipeline);
        MarkdownDocument disabled = markdown.ParseSourceWithPipeline(feature.Source, disabledPipeline);
        MarkdownNode? node = FindFirstKind(enabled.Root, feature.ExpectedKind);
        string html = markdown.RenderHtmlDocumentWithPipeline(enabled, enabledPipeline);
        string normalized = markdown.RenderMarkdownDocumentWithPipeline(enabled, enabledPipeline);
        string plainText = markdown.RenderPlainTextDocumentWithPipeline(enabled, enabledPipeline);
        string debugTree = markdown.RenderDebugTreeDocumentWithPipeline(enabled, enabledPipeline);
        string disabledHtml = markdown.RenderHtmlDocumentWithPipeline(disabled, disabledPipeline);
        string disabledText = markdown.RenderPlainTextDocumentWithPipeline(disabled, disabledPipeline);

        Assert.That(enabled.Diagnostics.Items, Is.Empty, feature.FeatureName + " enabled diagnostics");
        Assert.That(disabled.Diagnostics.Items, Is.Empty, feature.FeatureName + " disabled diagnostics");
        Assert.That(node, Is.Not.Null, feature.FeatureName + " AST kind");
        Assert.That(node!.Data.Kind, Is.EqualTo(feature.ExpectedKind), feature.FeatureName + " typed data");
        Assert.That(debugTree, Does.Contain(feature.ExpectedKind.ToString()), feature.FeatureName + " debug tree");
        if (feature.AttributeName.Length > 0)
        {
            Assert.That(
                node.GetAttributeValue(feature.AttributeName),
                Is.EqualTo(feature.AttributeValue),
                feature.FeatureName + " attribute");
        }

        if (feature.ExpectedLiteral.Length > 0)
        {
            Assert.That(node.Literal, Is.EqualTo(feature.ExpectedLiteral), feature.FeatureName + " literal");
        }

        Assert.That(html, Does.Contain(feature.HtmlFragment), feature.FeatureName + " HTML");
        Assert.That(normalized, Does.Contain(feature.MarkdownFragment), feature.FeatureName + " Markdown");
        Assert.That(plainText, Does.Contain(feature.PlainTextFragment), feature.FeatureName + " plain text");
        Assert.That(disabledText, Does.Contain(feature.DisabledTextFragment), feature.FeatureName + " disabled fallback");
        Assert.That(disabledHtml, Is.Not.EqualTo(html), feature.FeatureName + " disabled HTML behavior");
    }

    [Test]
    public void GfmFlags_ComposeAcrossBlockAndInlinePrecedence()
    {
        IMarkdown markdown = new Markdown();
        MarkdownFeatureSet features = CommonMarkFeatures();
        features.GfmTables = true;
        features.GfmTaskLists = true;
        features.GfmStrikethrough = true;
        features.GfmBareAutolinks = true;
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder().SetFeatures(features).Build();
        string source =
            "| Work | Link |\n" +
            "| --- | --- |\n" +
            "| ~~old~~ | https://example.com |\n" +
            "\n" +
            "- [x] done";

        MarkdownDocument document = markdown.ParseSourceWithPipeline(source, pipeline);
        string html = markdown.RenderHtmlDocumentWithPipeline(document, pipeline);

        Assert.That(document.Diagnostics.Items, Is.Empty);
        Assert.That(FindFirstKind(document.Root, MarkdownNodeKind.Table), Is.Not.Null);
        Assert.That(FindFirstKind(document.Root, MarkdownNodeKind.Strikethrough), Is.Not.Null);
        Assert.That(FindFirstKind(document.Root, MarkdownNodeKind.Link), Is.Not.Null);
        Assert.That(FindFirstKind(document.Root, MarkdownNodeKind.TaskListItem), Is.Not.Null);
        Assert.That(html, Does.Contain("<del>old</del>"));
        Assert.That(html, Does.Contain("href=\"https://example.com\""));
        Assert.That(html, Does.Contain("type=\"checkbox\""));
    }

    [Test]
    public void AdvancedFlags_ComposeWithAttributesAndCustomMappings()
    {
        IMarkdown markdown = new Markdown();
        MarkdownFeatureSet features = CommonMarkFeatures();
        features.Attributes = true;
        features.Math = true;
        features.Emoji = true;
        features.CustomInlineDelimiters = true;
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .SetFeatures(features)
            .MapEmojiShortcode("ship", "SHIP")
            .RegisterCustomInlineDelimiter("kbd", "%%", "%%")
            .Build();
        string source = "*important*{.accent} $x$ :ship: %%Ctrl%%";

        MarkdownDocument document = markdown.ParseSourceWithPipeline(source, pipeline);
        MarkdownNode emphasis = FindFirstKind(document.Root, MarkdownNodeKind.Emphasis)!;
        MarkdownNode emoji = FindFirstKind(document.Root, MarkdownNodeKind.Emoji)!;
        string html = markdown.RenderHtmlDocumentWithPipeline(document, pipeline);
        string normalized = markdown.RenderMarkdownDocumentWithPipeline(document, pipeline);
        string plainText = markdown.RenderPlainTextDocumentWithPipeline(document, pipeline);

        Assert.That(document.Diagnostics.Items, Is.Empty);
        Assert.That(emphasis.GetAttributeValue("class"), Is.EqualTo("accent"));
        Assert.That(FindFirstKind(document.Root, MarkdownNodeKind.MathSpan), Is.Not.Null);
        Assert.That(emoji.GetAttributeValue("replacement"), Is.EqualTo("SHIP"));
        Assert.That(FindFirstKind(document.Root, MarkdownNodeKind.CustomInline), Is.Not.Null);
        Assert.That(html, Does.Contain("<em class=\"accent\">important</em>"));
        Assert.That(html, Does.Contain("<span class=\"math\">x</span> SHIP"));
        Assert.That(normalized, Is.EqualTo(source + "\n"));
        Assert.That(plainText, Is.EqualTo("important x SHIP Ctrl\n"));
    }

    public static IEnumerable<ExtensionFeatureCase> ExtensionCases()
    {
        yield return Case(
            "GfmTables", "| A | B |\n| --- | :---: |\n| x | y |", MarkdownNodeKind.Table,
            "columns", "2", "", "<table>", "| A | B |", "AB", "| A | B |");
        yield return Case(
            "GfmTaskLists", "- [x] done", MarkdownNodeKind.TaskListItem,
            "checked", "true", "", "type=\"checkbox\"", "- [x] done", "done", "[x] done");
        yield return Case(
            "GfmStrikethrough", "~~old~~", MarkdownNodeKind.Strikethrough,
            "", "", "", "<del>old</del>", "~~old~~", "old", "~~old~~");
        yield return Case(
            "GfmBareAutolinks", "Visit https://example.com", MarkdownNodeKind.Link,
            "destination", "https://example.com", "", "href=\"https://example.com\"", "[https://example.com](https://example.com)", "https://example.com", "https://example.com");
        yield return Case(
            "GfmDisallowedRawHtml", "<script>x</script>", MarkdownNodeKind.HtmlBlock,
            "", "", "<script>x</script>", "&lt;script>x&lt;/script>", "<script>x</script>", "<script>x</script>", "<script>x</script>");
        yield return Case(
            "Footnotes", "Text[^n].\n\n[^n]: note", MarkdownNodeKind.FootnoteReference,
            "resolved", "true", "", "class=\"footnote-ref\"", "[^n]", "Text", "Text^n");
        yield return Case(
            "DefinitionLists", "Term\n: Meaning", MarkdownNodeKind.DefinitionList,
            "", "", "", "<dl>", ": Meaning", "Term\nMeaning", "Term\n: Meaning");
        yield return Case(
            "HeadingIds", "# Hello", MarkdownNodeKind.Heading,
            "id", "hello", "Hello", "id=\"hello\"", "# Hello", "Hello", "Hello");
        yield return Case(
            "Attributes", "*text*{.accent}", MarkdownNodeKind.Emphasis,
            "class", "accent", "", "class=\"accent\"", "{.accent}", "text", "text{.accent}");
        yield return Case(
            "FrontMatter", "---\ntitle: Test\n---\n# Title", MarkdownNodeKind.FrontMatter,
            "format", "yaml", "title: Test", "<h1>Title</h1>", "title: Test", "Title", "title: Test");
        yield return Case(
            "Abbreviations", "*[HTML]: Hyper Text Markup Language\n\nHTML", MarkdownNodeKind.Abbreviation,
            "title", "Hyper Text Markup Language", "HTML", "<abbr title=\"Hyper Text Markup Language\">HTML</abbr>", "*[HTML]:", "HTML", "*[HTML]: Hyper Text Markup Language");
        yield return Case(
            "Superscript", "H^2^O", MarkdownNodeKind.Superscript,
            "", "", "", "<sup>2</sup>", "H^2^O", "H2O", "H^2^O");
        yield return Case(
            "Subscript", "H~2~O", MarkdownNodeKind.Subscript,
            "", "", "", "<sub>2</sub>", "H~2~O", "H2O", "H~2~O");
        yield return Case(
            "InsertedText", "++new++", MarkdownNodeKind.InsertedText,
            "", "", "", "<ins>new</ins>", "++new++", "new", "++new++");
        yield return Case(
            "MarkedText", "==mark==", MarkdownNodeKind.MarkedText,
            "", "", "", "<mark>mark</mark>", "==mark==", "mark", "==mark==");
        yield return Case(
            "Typography", "\"Hi\"...", MarkdownNodeKind.Text,
            "", "", "“Hi”…", "“Hi”…", "“Hi”…", "“Hi”…", "\"Hi\"...");
        yield return Case(
            "Math", "Math $x < y$", MarkdownNodeKind.MathSpan,
            "delimiter", "$", "x < y", "class=\"math\">x &lt; y</span>", "$x < y$", "x < y", "$x < y$");
        yield return Case(
            "Diagrams", "```mermaid\ngraph TD\n```", MarkdownNodeKind.DiagramBlock,
            "diagramType", "mermaid", "graph TD", "data-diagram=\"mermaid\"", "```mermaid", "graph TD", "graph TD");
        yield return Case(
            "Emoji", "Hello :ship:", MarkdownNodeKind.Emoji,
            "replacement", "SHIP", ":ship:", "Hello SHIP", ":ship:", "Hello SHIP", "Hello :ship:");
        yield return Case(
            "CjkSoftBreaks", "漢字\n仮名", MarkdownNodeKind.SoftLineBreak,
            "", "", "", "<p>漢字仮名</p>", "漢字\n仮名", "漢字仮名", "漢字\n仮名");
        yield return Case(
            "Admonitions", "> [!NOTE]\n> Alert text.", MarkdownNodeKind.AdmonitionBlock,
            "type", "note", "Alert text.", "class=\"admonition note\"", "> [!NOTE]", "Alert text.", "[!NOTE]");
        yield return Case(
            "CustomContainers", "::: panel \"Panel\"\nContent.\n:::", MarkdownNodeKind.CustomContainerBlock,
            "name", "panel", "Content.", "class=\"custom-container panel\"", "::: panel", "Content.", "::: panel");
        yield return Case(
            "CustomFencedBlocks", "```chart\na < b\n```", MarkdownNodeKind.CustomFencedBlock,
            "infoKey", "chart", "a < b", "data-custom-fenced=\"chart\"", "```chart", "a < b", "a < b");
        yield return Case(
            "CustomInlineDelimiters", "Press %%Ctrl%%.", MarkdownNodeKind.CustomInline,
            "name", "kbd", "", "class=\"custom-inline kbd\"", "%%Ctrl%%", "Press Ctrl.", "Press %%Ctrl%%.");
        yield return Case(
            "Details", "??? details \"More\"\nContent.\n???", MarkdownNodeKind.DetailsBlock,
            "", "", "Content.", "<details>", "??? details \"More\"", "More\nContent.", "??? details \"More\"");
    }

    public static ExtensionFeatureCase Case(
        string featureName,
        string source,
        MarkdownNodeKind expectedKind,
        string attributeName,
        string attributeValue,
        string expectedLiteral,
        string htmlFragment,
        string markdownFragment,
        string plainTextFragment,
        string disabledTextFragment)
    {
        return new ExtensionFeatureCase(
            featureName,
            source,
            expectedKind,
            attributeName,
            attributeValue,
            expectedLiteral,
            htmlFragment,
            markdownFragment,
            plainTextFragment,
            disabledTextFragment);
    }

    public static MarkdownPipeline CreatePipeline(IMarkdown markdown, string featureName, bool enabled)
    {
        MarkdownFeatureSet features = CommonMarkFeatures();
        if (enabled)
        {
            EnableFeature(features, featureName);
        }

        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        builder.SetFeatures(features);
        if (featureName == "GfmDisallowedRawHtml")
        {
            builder.AllowRawHtml();
        }
        else if (featureName == "Emoji")
        {
            builder.MapEmojiShortcode("ship", "SHIP");
        }
        else if (featureName == "CjkSoftBreaks")
        {
            builder.SetSoftBreakMode(MarkdownSoftBreakMode.Cjk);
        }
        else if (featureName == "CustomContainers")
        {
            builder.RegisterCustomContainer("panel");
        }
        else if (featureName == "CustomFencedBlocks")
        {
            builder.RegisterCustomFencedBlock("chart");
        }
        else if (featureName == "CustomInlineDelimiters")
        {
            builder.RegisterCustomInlineDelimiter("kbd", "%%", "%%");
        }

        return builder.Build();
    }

    public static MarkdownFeatureSet CommonMarkFeatures()
    {
        MarkdownFeatureSet features = new MarkdownFeatureSet();
        features.CommonMark = true;
        return features;
    }

    public static void EnableFeature(MarkdownFeatureSet features, string featureName)
    {
        if (featureName == "GfmTables") features.GfmTables = true;
        else if (featureName == "GfmTaskLists") features.GfmTaskLists = true;
        else if (featureName == "GfmStrikethrough") features.GfmStrikethrough = true;
        else if (featureName == "GfmBareAutolinks") features.GfmBareAutolinks = true;
        else if (featureName == "GfmDisallowedRawHtml") features.GfmDisallowedRawHtml = true;
        else if (featureName == "Footnotes") features.Footnotes = true;
        else if (featureName == "DefinitionLists") features.DefinitionLists = true;
        else if (featureName == "HeadingIds") features.HeadingIds = true;
        else if (featureName == "Attributes") features.Attributes = true;
        else if (featureName == "FrontMatter") features.FrontMatter = true;
        else if (featureName == "Abbreviations") features.Abbreviations = true;
        else if (featureName == "Superscript") features.Superscript = true;
        else if (featureName == "Subscript") features.Subscript = true;
        else if (featureName == "InsertedText") features.InsertedText = true;
        else if (featureName == "MarkedText") features.MarkedText = true;
        else if (featureName == "Typography") features.Typography = true;
        else if (featureName == "Math") features.Math = true;
        else if (featureName == "Diagrams") features.Diagrams = true;
        else if (featureName == "Emoji") features.Emoji = true;
        else if (featureName == "CjkSoftBreaks") features.CjkSoftBreaks = true;
        else if (featureName == "Admonitions") features.Admonitions = true;
        else if (featureName == "CustomContainers") features.CustomContainers = true;
        else if (featureName == "CustomFencedBlocks") features.CustomFencedBlocks = true;
        else if (featureName == "CustomInlineDelimiters") features.CustomInlineDelimiters = true;
        else if (featureName == "Details") features.Details = true;
    }

    public static MarkdownNode? FindFirstKind(MarkdownNode node, MarkdownNodeKind kind)
    {
        if (node.Kind == kind)
        {
            return node;
        }

        int index = 0;
        while (index < node.Children.Count)
        {
            MarkdownNode? found = FindFirstKind(node.Children[index], kind);
            if (found != null)
            {
                return found;
            }

            index = index + 1;
        }

        return null;
    }
}
