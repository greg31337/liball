using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownPipelineBuilderTests
{
    [Test]
    public void Builder_DefaultsToCommonMarkOptions()
    {
        MarkdownPipelineBuilder builder = new MarkdownPipelineBuilder();
        MarkdownOptions options = builder.GetOptions();

        Assert.That(options.Features.CommonMark, Is.True);
        Assert.That(options.Features.GfmTables, Is.False);
        Assert.That(options.AllowUnsafeHtml, Is.False);
        Assert.That(options.AllowUnsafeLinks, Is.False);
    }

    [Test]
    public void Builder_StaticPresetsCreateExpectedFeatureSets()
    {
        Assert.That(MarkdownPipelineBuilder.CommonMarkPreset().GetOptions().Features.CommonMark, Is.True);
        Assert.That(MarkdownPipelineBuilder.GfmPreset().GetOptions().Features.GfmTables, Is.True);
        Assert.That(MarkdownPipelineBuilder.AdvancedPreset().GetOptions().Features.Footnotes, Is.True);
        Assert.That(MarkdownPipelineBuilder.ZeroPreset().GetOptions().Features.CommonMark, Is.False);
        Assert.That(MarkdownPipelineBuilder.Create().GetOptions().Features.CommonMark, Is.True);
    }

    [Test]
    public void Builder_UsePresetMethodsReplaceFeatureSet()
    {
        MarkdownPipelineBuilder builder = new MarkdownPipelineBuilder();

        builder.UseGfm();
        Assert.That(builder.GetOptions().Features.GfmTables, Is.True);

        builder.UseAdvancedExtensions();
        Assert.That(builder.GetOptions().Features.Footnotes, Is.True);

        builder.UseZero();
        Assert.That(builder.GetOptions().Features.CommonMark, Is.False);

        builder.UseCommonMark();
        Assert.That(builder.GetOptions().Features.CommonMark, Is.True);
        Assert.That(builder.GetOptions().Features.GfmTables, Is.False);
    }

    [Test]
    public void Builder_ChainsSafetyAndRenderingOptions()
    {
        MarkdownPipelineBuilder builder = new MarkdownPipelineBuilder();
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
            .SetLineEnding("\r\n");

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
    }

    [Test]
    public void Builder_DisableHtmlClearsRawAndUnsafeHtml()
    {
        MarkdownPipelineBuilder builder = new MarkdownPipelineBuilder();

        builder.AllowUnsafeHtml();
        builder.DisableHtml();

        Assert.That(builder.GetOptions().AllowRawHtml, Is.False);
        Assert.That(builder.GetOptions().AllowUnsafeHtml, Is.False);
    }

    [Test]
    public void Builder_ChainsLimitOptions()
    {
        MarkdownPipelineBuilder builder = new MarkdownPipelineBuilder();

        builder
            .SetMaximumNestingDepth(3)
            .SetMaximumInputSize(4)
            .SetMaximumReferenceDefinitions(5)
            .SetMaximumOutputSize(6);

        MarkdownOptions options = builder.GetOptions();

        Assert.That(options.MaximumNestingDepth, Is.EqualTo(3));
        Assert.That(options.MaximumInputSize, Is.EqualTo(4));
        Assert.That(options.MaximumReferenceDefinitions, Is.EqualTo(5));
        Assert.That(options.MaximumOutputSize, Is.EqualTo(6));
    }

    [Test]
    public void Builder_CopiesConstructorSetOptionsAndGetOptions()
    {
        MarkdownOptions source = MarkdownOptions.GfmPreset();
        MarkdownPipelineBuilder builder = MarkdownPipelineBuilder.Create(source);

        source.Features.DisableAll();

        MarkdownOptions fromBuilder = builder.GetOptions();
        fromBuilder.Features.DisableAll();
        fromBuilder.SetMaximumInputSize(1);

        Assert.That(builder.GetOptions().Features.GfmTables, Is.True);
        Assert.That(builder.GetOptions().MaximumInputSize, Is.EqualTo(10485760));

        MarkdownOptions advanced = MarkdownOptions.AdvancedPreset();
        builder.SetOptions(advanced);
        advanced.Features.DisableAll();

        Assert.That(builder.GetOptions().Features.Footnotes, Is.True);
    }

    [Test]
    public void Builder_AddsRulesToOrderedCollections()
    {
        IMarkdownBlockRule blockRule = new TestBlockRule("block", context => false);
        IMarkdownInlineRule inlineRule = new TestInlineRule("inline", context => false);
        IMarkdownTransform transformRule = new TestTransform("transform", (document, context) => { });
        IHtmlRenderRule htmlRule = new TestHtmlRenderRule("html", (context, node) => false);

        MarkdownPipelineBuilder builder = new MarkdownPipelineBuilder()
            .AddNamedBlockRule("block", blockRule)
            .AddNamedInlineRule("inline", inlineRule)
            .AddNamedTransform("transform", transformRule)
            .AddNamedHtmlRenderRule("html", htmlRule);

        MarkdownRuleList blockRules = builder.GetBlockRules();
        blockRules.Add("mutated-copy", new object());

        Assert.That(builder.GetBlockRules().GetCount(), Is.EqualTo(1));
        Assert.That(builder.GetBlockRules().FindRule("block"), Is.SameAs(blockRule));
        Assert.That(builder.GetInlineRules().FindRule("inline"), Is.SameAs(inlineRule));
        Assert.That(builder.GetTransformRules().FindRule("transform"), Is.SameAs(transformRule));
        Assert.That(builder.GetHtmlRenderRules().FindRule("html"), Is.SameAs(htmlRule));
    }

    [Test]
    public void Builder_RejectsInvalidArguments()
    {
        MarkdownPipelineBuilder builder = new MarkdownPipelineBuilder();

        Assert.That(() => MarkdownPipelineBuilder.Create(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.SetOptions(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.SetLineEnding(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.SetLineEnding("\n\n"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => builder.SetMaximumNestingDepth(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => builder.SetMaximumInputSize(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => builder.SetMaximumReferenceDefinitions(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => builder.SetMaximumOutputSize(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => builder.AddNamedBlockRule(null!, new TestBlockRule("block", context => false)), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.AddNamedInlineRule("", new TestInlineRule("inline", context => false)), Throws.TypeOf<ArgumentException>());
        Assert.That(() => builder.AddNamedTransform("transform", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.AddNamedHtmlRenderRule("html", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.AddBlockRule((IMarkdownBlockRule)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.AddInlineRule((IMarkdownInlineRule)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.AddTransform((IMarkdownTransform)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.AddHtmlRenderRule((IHtmlRenderRule)null!), Throws.TypeOf<ArgumentNullException>());
    }
}
