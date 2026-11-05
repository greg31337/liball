using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownPipelineTests
{
    [Test]
    public void Pipeline_DefaultsToCommonMark()
    {
        MarkdownPipeline pipeline = new MarkdownPipeline();

        Assert.That(pipeline.UsesCommonMark(), Is.True);
        Assert.That(pipeline.UsesGfm(), Is.False);
        Assert.That(pipeline.UsesAdvancedExtensions(), Is.False);
        Assert.That(pipeline.AllowsUnsafeHtml(), Is.False);
        Assert.That(pipeline.AllowsUnsafeLinks(), Is.False);
    }

    [Test]
    public void Pipeline_StaticPresetsCreateExpectedPipelines()
    {
        Assert.That(MarkdownPipeline.CommonMarkPreset().UsesCommonMark(), Is.True);
        Assert.That(MarkdownPipeline.GfmPreset().UsesGfm(), Is.True);
        Assert.That(MarkdownPipeline.AdvancedPreset().UsesAdvancedExtensions(), Is.True);
        Assert.That(MarkdownPipeline.ZeroPreset().UsesCommonMark(), Is.False);
    }

    [Test]
    public void Pipeline_CopiesConstructorOptions()
    {
        MarkdownOptions options = MarkdownOptions.GfmPreset();
        MarkdownPipeline pipeline = MarkdownPipeline.Create(options);

        options.Features.DisableAll();
        options.AllowUnsafeLinks = true;

        Assert.That(pipeline.UsesGfm(), Is.True);
        Assert.That(pipeline.AllowsUnsafeLinks(), Is.False);
    }

    [Test]
    public void Pipeline_GetOptionsAndGetFeaturesReturnCopies()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.AdvancedPreset();

        MarkdownOptions options = pipeline.GetOptions();
        MarkdownFeatureSet features = pipeline.GetFeatures();

        options.Features.DisableAll();
        features.DisableAll();

        Assert.That(pipeline.UsesAdvancedExtensions(), Is.True);
    }

    [Test]
    public void Pipeline_CopyCreatesIndependentPipeline()
    {
        MarkdownOptions options = MarkdownOptions.GfmPreset();
        options.AllowRawHtml = true;
        MarkdownPipeline pipeline = MarkdownPipeline.Create(options);
        MarkdownPipeline copy = pipeline.Copy();

        MarkdownOptions mutated = copy.GetOptions();
        mutated.Features.DisableAll();

        Assert.That(copy.UsesGfm(), Is.True);
        Assert.That(copy.AllowsRawHtml(), Is.True);
        Assert.That(pipeline.UsesGfm(), Is.True);
    }

    [Test]
    public void Builder_BuildSnapshotsCurrentOptions()
    {
        MarkdownPipelineBuilder builder = MarkdownPipelineBuilder.GfmPreset()
            .AllowUnsafeHtml()
            .AllowUnsafeLinks()
            .TrackTrivia()
            .SetMaximumInputSize(123);

        MarkdownPipeline pipeline = builder.Build();

        builder.UseZero().DisableHtml().SetMaximumInputSize(1);

        MarkdownOptions options = pipeline.GetOptions();

        Assert.That(pipeline.UsesGfm(), Is.True);
        Assert.That(pipeline.AllowsRawHtml(), Is.True);
        Assert.That(pipeline.AllowsUnsafeHtml(), Is.True);
        Assert.That(pipeline.AllowsUnsafeLinks(), Is.True);
        Assert.That(pipeline.TracksTrivia(), Is.True);
        Assert.That(options.MaximumInputSize, Is.EqualTo(123));
    }

    [Test]
    public void Builder_BuildSnapshotsRuleLists()
    {
        IMarkdownBlockRule blockRule = new TestBlockRule("paragraph", context => false);
        IMarkdownInlineRule inlineRule = new TestInlineRule("text", context => false);
        IMarkdownTransform transformRule = new TestTransform("normalize", (document, context) => { });
        IHtmlRenderRule htmlRule = new TestHtmlRenderRule("paragraph-html", (context, node) => false);

        MarkdownPipelineBuilder builder = new MarkdownPipelineBuilder()
            .AddNamedBlockRule("paragraph", blockRule)
            .AddNamedInlineRule("text", inlineRule)
            .AddNamedTransform("normalize", transformRule)
            .AddNamedHtmlRenderRule("paragraph-html", htmlRule);

        MarkdownPipeline pipeline = builder.Build();

        builder.AddNamedBlockRule("heading", new TestBlockRule("heading", context => false));
        MarkdownRuleList blockRules = pipeline.GetBlockRules();
        blockRules.Add("mutated-copy", new object());

        Assert.That(pipeline.GetBlockRules().GetCount(), Is.EqualTo(1));
        Assert.That(pipeline.GetInlineRules().FindRule("text"), Is.SameAs(inlineRule));
        Assert.That(pipeline.GetTransformRules().FindRule("normalize"), Is.SameAs(transformRule));
        Assert.That(pipeline.GetHtmlRenderRules().FindRule("paragraph-html"), Is.SameAs(htmlRule));
    }

    [Test]
    public void Pipeline_CopiesConstructorRuleLists()
    {
        MarkdownOptions options = MarkdownOptions.CommonMarkPreset();
        MarkdownRuleList blockRules = new MarkdownRuleList();
        MarkdownRuleList inlineRules = new MarkdownRuleList();
        MarkdownRuleList transformRules = new MarkdownRuleList();
        MarkdownRuleList htmlRenderRules = new MarkdownRuleList();

        blockRules.Add("paragraph", new object());
        MarkdownPipeline pipeline = MarkdownPipeline.Create(options, blockRules, inlineRules, transformRules, htmlRenderRules);
        blockRules.Add("heading", new object());

        Assert.That(pipeline.GetBlockRules().GetCount(), Is.EqualTo(1));
    }

    [Test]
    public void Pipeline_RejectsNullOptions()
    {
        Assert.That(() => MarkdownPipeline.Create(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownPipeline.Create(null!, new MarkdownRuleList(), new MarkdownRuleList(), new MarkdownRuleList(), new MarkdownRuleList()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownPipeline.Create(new MarkdownOptions(), null!, new MarkdownRuleList(), new MarkdownRuleList(), new MarkdownRuleList()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownPipeline.Create(new MarkdownOptions(), new MarkdownRuleList(), null!, new MarkdownRuleList(), new MarkdownRuleList()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownPipeline.Create(new MarkdownOptions(), new MarkdownRuleList(), new MarkdownRuleList(), null!, new MarkdownRuleList()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownPipeline.Create(new MarkdownOptions(), new MarkdownRuleList(), new MarkdownRuleList(), new MarkdownRuleList(), null!), Throws.TypeOf<ArgumentNullException>());
    }
}
