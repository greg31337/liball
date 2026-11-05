using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownPipelineBuilderPublicConfigurationTests
{
    [Test]
    public void PublicBuilder_ConfiguresSafetyFormattingTriviaAndLimits()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();

        MarkdownPipelineBuilder returned = builder
            .AllowRawHtml()
            .AllowUnsafeHtml()
            .AllowUnsafeLinks()
            .TrackTrivia()
            .UseXhtml()
            .UseHardWrap()
            .SetDeterministicOutput(false)
            .SetSoftBreakMode(MarkdownSoftBreakMode.Space)
            .UseCjkSoftBreaks()
            .SetLineEnding("\r\n")
            .SetMaximumNestingDepth(11)
            .SetMaximumInputSize(1200)
            .SetMaximumReferenceDefinitions(13)
            .SetMaximumOutputSize(1400)
            .SetMaximumLineWidth(72);

        MarkdownOptions options = builder.GetOptions();

        Assert.That(returned, Is.SameAs(builder));
        Assert.That(options.AllowRawHtml, Is.True);
        Assert.That(options.AllowUnsafeHtml, Is.True);
        Assert.That(options.AllowUnsafeLinks, Is.True);
        Assert.That(options.TrackTrivia, Is.True);
        Assert.That(options.Xhtml, Is.True);
        Assert.That(options.HardWrap, Is.True);
        Assert.That(options.DeterministicOutput, Is.False);
        Assert.That(options.SoftBreakMode, Is.EqualTo(MarkdownSoftBreakMode.Cjk));
        Assert.That(options.Features.CjkSoftBreaks, Is.True);
        Assert.That(options.LineEnding, Is.EqualTo("\r\n"));
        Assert.That(options.MaximumNestingDepth, Is.EqualTo(11));
        Assert.That(options.MaximumInputSize, Is.EqualTo(1200));
        Assert.That(options.MaximumReferenceDefinitions, Is.EqualTo(13));
        Assert.That(options.MaximumOutputSize, Is.EqualTo(1400));
        Assert.That(options.MaximumLineWidth, Is.EqualTo(72));

        builder.DisableHtml();

        Assert.That(builder.GetOptions().AllowRawHtml, Is.False);
        Assert.That(builder.GetOptions().AllowUnsafeHtml, Is.False);
    }

    [Test]
    public void PublicBuilder_SetFeaturesCopiesEveryFeatureFlag()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        MarkdownFeatureSet features = CreateAllFeatures();

        builder.SetFeatures(features);
        features.CommonMark = false;
        MarkdownFeatureSet configured = builder.GetOptions().Features;

        Assert.That(configured.CommonMark, Is.True);
        Assert.That(configured.GfmTables, Is.True);
        Assert.That(configured.GfmTaskLists, Is.True);
        Assert.That(configured.GfmStrikethrough, Is.True);
        Assert.That(configured.GfmBareAutolinks, Is.True);
        Assert.That(configured.GfmDisallowedRawHtml, Is.True);
        Assert.That(configured.Footnotes, Is.True);
        Assert.That(configured.DefinitionLists, Is.True);
        Assert.That(configured.HeadingIds, Is.True);
        Assert.That(configured.Attributes, Is.True);
        Assert.That(configured.FrontMatter, Is.True);
        Assert.That(configured.Abbreviations, Is.True);
        Assert.That(configured.Superscript, Is.True);
        Assert.That(configured.Subscript, Is.True);
        Assert.That(configured.InsertedText, Is.True);
        Assert.That(configured.MarkedText, Is.True);
        Assert.That(configured.Typography, Is.True);
        Assert.That(configured.Math, Is.True);
        Assert.That(configured.Diagrams, Is.True);
        Assert.That(configured.Emoji, Is.True);
        Assert.That(configured.CjkSoftBreaks, Is.True);
        Assert.That(configured.Admonitions, Is.True);
        Assert.That(configured.CustomContainers, Is.True);
        Assert.That(configured.CustomFencedBlocks, Is.True);
        Assert.That(configured.CustomInlineDelimiters, Is.True);
        Assert.That(configured.Details, Is.True);
    }

    [Test]
    public void PublicBuilder_FeatureChangesSynchronizeParserRules()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        MarkdownFeatureSet features = new MarkdownFeatureSet();
        features.CommonMark = true;
        features.GfmStrikethrough = true;
        features.Math = true;

        builder.SetFeatures(features);
        MarkdownDocument enabled = markdown.ParseSourceWithPipeline("~~old~~ and $x$", builder.Build());

        Assert.That(FindFirstKind(enabled.Root, MarkdownNodeKind.Strikethrough), Is.Not.EqualTo(null));
        Assert.That(FindFirstKind(enabled.Root, MarkdownNodeKind.MathSpan), Is.Not.EqualTo(null));

        features.GfmStrikethrough = false;
        features.Math = false;
        builder.SetFeatures(features);
        MarkdownDocument disabled = markdown.ParseSourceWithPipeline("~~old~~ and $x$", builder.Build());

        Assert.That(FindFirstKind(disabled.Root, MarkdownNodeKind.Strikethrough), Is.EqualTo(null));
        Assert.That(FindFirstKind(disabled.Root, MarkdownNodeKind.MathSpan), Is.EqualTo(null));
    }

    [Test]
    public void PublicBuilder_SetOptionsSynchronizesFeatureRules()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        MarkdownOptions options = new MarkdownOptions();
        options.Features.GfmStrikethrough = true;

        builder.SetOptions(options);
        MarkdownDocument document = markdown.ParseSourceWithPipeline("~~old~~", builder.Build());

        Assert.That(FindFirstKind(document.Root, MarkdownNodeKind.Strikethrough), Is.Not.EqualTo(null));
    }

    [Test]
    public void PublicBuilder_ConfiguresAndClearsCustomSyntaxMappings()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        builder.UseAdvancedExtensions()
            .MapEmojiShortcode("ship", "SHIP")
            .RegisterCustomContainer("details")
            .RegisterCustomFencedBlock("chart")
            .RegisterCustomInlineDelimiter("kbd", "%%", "%%");
        MarkdownPipeline configured = builder.Build();

        MarkdownDocument emoji = markdown.ParseSourceWithPipeline(":ship:", configured);
        MarkdownDocument container = markdown.ParseSourceWithPipeline("::: details\ninside\n:::\n", configured);
        MarkdownDocument fenced = markdown.ParseSourceWithPipeline("```chart\nvalue\n```\n", configured);
        MarkdownDocument inline = markdown.ParseSourceWithPipeline("Press %%Ctrl%%.", configured);

        Assert.That(FindFirstKind(emoji.Root, MarkdownNodeKind.Emoji)?.GetAttributeValue("replacement"), Is.EqualTo("SHIP"));
        Assert.That(FindFirstKind(container.Root, MarkdownNodeKind.CustomContainerBlock), Is.Not.EqualTo(null));
        Assert.That(FindFirstKind(fenced.Root, MarkdownNodeKind.CustomFencedBlock), Is.Not.EqualTo(null));
        Assert.That(FindFirstKind(inline.Root, MarkdownNodeKind.CustomInline), Is.Not.EqualTo(null));

        builder.ClearEmojiShortcodes()
            .ClearCustomContainers()
            .ClearCustomFencedBlocks()
            .ClearCustomInlineDelimiters();
        MarkdownPipeline cleared = builder.Build();

        MarkdownDocument clearedEmoji = markdown.ParseSourceWithPipeline(":ship:", cleared);
        Assert.That(FindFirstKind(clearedEmoji.Root, MarkdownNodeKind.Emoji)?.GetAttributeValue("replacement"), Is.EqualTo(""));
        Assert.That(FindFirstKind(markdown.ParseSourceWithPipeline("::: details\ninside\n:::\n", cleared).Root, MarkdownNodeKind.CustomContainerBlock), Is.EqualTo(null));
        Assert.That(FindFirstKind(markdown.ParseSourceWithPipeline("```chart\nvalue\n```\n", cleared).Root, MarkdownNodeKind.CustomFencedBlock), Is.EqualTo(null));
        Assert.That(FindFirstKind(markdown.ParseSourceWithPipeline("Press %%Ctrl%%.", cleared).Root, MarkdownNodeKind.CustomInline), Is.EqualTo(null));
    }

    [Test]
    public void PublicBuilder_RejectsInvalidConfigurationValues()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();

        Assert.That(() => builder.SetFeatures(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.SetLineEnding("invalid"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => builder.SetMaximumNestingDepth(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => builder.SetMaximumInputSize(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => builder.SetMaximumReferenceDefinitions(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => builder.SetMaximumOutputSize(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => builder.MapEmojiShortcode("bad name", "x"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => builder.RegisterCustomContainer("bad name"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => builder.RegisterCustomFencedBlock("bad name"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => builder.RegisterCustomInlineDelimiter("kbd", "", "%%"), Throws.TypeOf<ArgumentException>());
    }

    static MarkdownFeatureSet CreateAllFeatures()
    {
        MarkdownFeatureSet features = new MarkdownFeatureSet();
        features.CommonMark = true;
        features.GfmTables = true;
        features.GfmTaskLists = true;
        features.GfmStrikethrough = true;
        features.GfmBareAutolinks = true;
        features.GfmDisallowedRawHtml = true;
        features.Footnotes = true;
        features.DefinitionLists = true;
        features.HeadingIds = true;
        features.Attributes = true;
        features.FrontMatter = true;
        features.Abbreviations = true;
        features.Superscript = true;
        features.Subscript = true;
        features.InsertedText = true;
        features.MarkedText = true;
        features.Typography = true;
        features.Math = true;
        features.Diagrams = true;
        features.Emoji = true;
        features.CjkSoftBreaks = true;
        features.Admonitions = true;
        features.CustomContainers = true;
        features.CustomFencedBlocks = true;
        features.CustomInlineDelimiters = true;
        features.Details = true;
        return features;
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
            MarkdownNode? found = FindFirstKind(node.GetChild(index), kind);
            if (found != null)
            {
                return found;
            }

            index++;
        }

        return null;
    }
}
