using System.Collections.Generic;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownPublicRegistryTests
{
    [Test]
    public void BlockRegistry_SupportsOrderedMutationAndDetachedInspection()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        builder.ClearBlockRules();
        IMarkdownBlockRule center = new TestBlockRule("block.center", context => false);
        IMarkdownBlockRule before = new TestBlockRule("block.before", context => false);
        IMarkdownBlockRule after = new TestBlockRule("block.after", context => false);
        IMarkdownBlockRule replacement = new TestBlockRule("block.replacement", context => false);

        builder.AddBlockRule(center)
            .InsertBlockRuleBefore("block.center", before)
            .InsertBlockRuleAfter("block.center", after)
            .ReplaceBlockRule("block.center", replacement);

        MarkdownRuleRegistration[] registrations = builder.GetBlockRuleRegistrations();
        AssertRegistrationNames(registrations, "block.before", "block.replacement", "block.after");
        Assert.That(registrations[1].Rule, Is.SameAs(replacement));

        registrations[0].Name = "changed";
        registrations[0].Rule = new object();
        AssertRegistrationNames(
            builder.GetBlockRuleRegistrations(),
            "block.before",
            "block.replacement",
            "block.after");

        builder.RemoveBlockRule("block.before");
        IMarkdownPipeline pipeline = builder.Build();
        builder.ClearBlockRules();

        MarkdownRuleRegistration[] pipelineRegistrations = pipeline.GetBlockRuleRegistrations();
        pipelineRegistrations[0].Name = "pipeline.changed";
        pipelineRegistrations[0].Rule = new object();

        Assert.That(builder.GetBlockRuleRegistrations(), Is.Empty);
        AssertRegistrationNames(pipeline.GetBlockRuleRegistrations(), "block.replacement", "block.after");
        Assert.That(pipeline.GetBlockRuleRegistrations()[0].Rule, Is.SameAs(replacement));
    }

    [Test]
    public void InlineRegistry_SupportsOrderedMutationAndDetachedInspection()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        builder.ClearInlineRules();
        IMarkdownInlineRule center = new TestInlineRule("inline.center", context => false);
        IMarkdownInlineRule before = new TestInlineRule("inline.before", context => false);
        IMarkdownInlineRule after = new TestInlineRule("inline.after", context => false);
        IMarkdownInlineRule replacement = new TestInlineRule("inline.replacement", context => false);

        builder.AddInlineRule(center)
            .InsertInlineRuleBefore("inline.center", before)
            .InsertInlineRuleAfter("inline.center", after)
            .ReplaceInlineRule("inline.center", replacement)
            .RemoveInlineRule("inline.before");
        IMarkdownPipeline pipeline = builder.Build();

        AssertRegistrationNames(builder.GetInlineRuleRegistrations(), "inline.replacement", "inline.after");
        builder.ClearInlineRules();
        Assert.That(builder.GetInlineRuleRegistrations(), Is.Empty);
        AssertRegistrationNames(pipeline.GetInlineRuleRegistrations(), "inline.replacement", "inline.after");
    }

    [Test]
    public void TransformRegistry_UsesConfiguredEvaluationOrder()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        List<string> calls = new List<string>();
        IMarkdownTransform center = new TestTransform("transform.center", (document, context) => calls.Add("center"));
        IMarkdownTransform before = new TestTransform("transform.before", (document, context) => calls.Add("before"));
        IMarkdownTransform after = new TestTransform("transform.after", (document, context) => calls.Add("after"));
        IMarkdownTransform replacement = new TestTransform("transform.replacement", (document, context) => calls.Add("replacement"));

        builder.AddTransform(center)
            .InsertTransformBefore("transform.center", before)
            .InsertTransformAfter("transform.center", after)
            .ReplaceTransform("transform.center", replacement)
            .RemoveTransform("transform.before");
        MarkdownPipeline pipeline = builder.Build();

        markdown.ParseSourceWithPipeline("text", pipeline);

        Assert.That(calls, Is.EqualTo(new[] { "replacement", "after" }));
        AssertRegistrationNames(pipeline.GetTransformRegistrations(), "transform.replacement", "transform.after");
        builder.ClearTransforms();
        Assert.That(builder.GetTransformRegistrations(), Is.Empty);
        AssertRegistrationNames(pipeline.GetTransformRegistrations(), "transform.replacement", "transform.after");
    }

    [Test]
    public void HtmlRenderRegistry_UsesConfiguredEvaluationOrder()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        List<string> calls = new List<string>();
        IHtmlRenderRule center = new TestHtmlRenderRule("render.center", (context, node) =>
        {
            calls.Add("center");
            return false;
        });
        IHtmlRenderRule before = new TestHtmlRenderRule("render.before", (context, node) =>
        {
            calls.Add("before");
            return false;
        });
        IHtmlRenderRule after = new TestHtmlRenderRule("render.after", (context, node) =>
        {
            calls.Add("after");
            return false;
        });
        IHtmlRenderRule replacement = new TestHtmlRenderRule("render.replacement", (context, node) =>
        {
            calls.Add("replacement");
            return false;
        });

        builder.AddHtmlRenderRule(center)
            .InsertHtmlRenderRuleBefore("render.center", before)
            .InsertHtmlRenderRuleAfter("render.center", after)
            .ReplaceHtmlRenderRule("render.center", replacement)
            .RemoveHtmlRenderRule("render.before");
        MarkdownPipeline pipeline = builder.Build();

        markdown.RenderHtmlSourceWithPipeline("text", pipeline);

        Assert.That(calls.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(calls[0], Is.EqualTo("replacement"));
        Assert.That(calls[1], Is.EqualTo("after"));
        AssertRegistrationNames(pipeline.GetHtmlRenderRuleRegistrations(), "render.replacement", "render.after");
        builder.ClearHtmlRenderRules();
        Assert.That(builder.GetHtmlRenderRuleRegistrations(), Is.Empty);
        AssertRegistrationNames(pipeline.GetHtmlRenderRuleRegistrations(), "render.replacement", "render.after");
    }

    [Test]
    public void OrderedRegistries_RejectInvalidAnchorsReplacementsAndRules()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        IMarkdownBlockRule block = new TestBlockRule("block", context => false);
        IMarkdownInlineRule inline = new TestInlineRule("inline", context => false);
        IMarkdownTransform transform = new TestTransform("transform", (document, context) => { });
        IHtmlRenderRule renderer = new TestHtmlRenderRule("renderer", (context, node) => false);

        Assert.That(() => builder.InsertBlockRuleBefore("missing", block), Throws.TypeOf<ArgumentException>());
        Assert.That(() => builder.InsertInlineRuleAfter("missing", inline), Throws.TypeOf<ArgumentException>());
        Assert.That(() => builder.ReplaceTransform("missing", transform), Throws.TypeOf<ArgumentException>());
        Assert.That(() => builder.ReplaceHtmlRenderRule("missing", renderer), Throws.TypeOf<ArgumentException>());
        Assert.That(() => builder.InsertBlockRuleBefore("missing", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.InsertInlineRuleAfter("missing", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.InsertTransformBefore("missing", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.InsertHtmlRenderRuleAfter("missing", null!), Throws.TypeOf<ArgumentNullException>());

        builder.ClearBlockRules().AddBlockRule(block).AddBlockRule(new TestBlockRule("other", context => false));
        Assert.That(
            () => builder.ReplaceBlockRule("block", new TestBlockRule("other", context => false)),
            Throws.TypeOf<ArgumentException>());
    }

    static void AssertRegistrationNames(MarkdownRuleRegistration[] registrations, params string[] expected)
    {
        Assert.That(registrations.Length, Is.EqualTo(expected.Length));
        int index = 0;
        while (index < expected.Length)
        {
            Assert.That(registrations[index].Name, Is.EqualTo(expected[index]));
            index++;
        }
    }
}
