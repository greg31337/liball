using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class PublicPipelineExtensibilityTests
{
    [Test]
    public void PublicPresets_EnableExactlyTheirSyntaxGroups()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline commonMark = markdown.CreateCommonMarkPipeline();
        MarkdownPipeline gfm = markdown.CreateGfmPipeline();
        MarkdownPipeline advanced = markdown.CreateAdvancedPipeline();
        MarkdownPipeline zero = markdown.CreatePipelineBuilder().UseZero().Build();

        Assert.That(commonMark.UsesCommonMark(), Is.True);
        Assert.That(commonMark.UsesGfm(), Is.False);
        Assert.That(commonMark.UsesAdvancedExtensions(), Is.False);
        Assert.That(gfm.UsesCommonMark(), Is.True);
        Assert.That(gfm.UsesGfm(), Is.True);
        Assert.That(gfm.UsesAdvancedExtensions(), Is.False);
        Assert.That(advanced.UsesCommonMark(), Is.True);
        Assert.That(advanced.UsesGfm(), Is.True);
        Assert.That(advanced.UsesAdvancedExtensions(), Is.True);
        Assert.That(zero.UsesCommonMark(), Is.False);
        Assert.That(zero.UsesGfm(), Is.False);
        Assert.That(zero.UsesAdvancedExtensions(), Is.False);

        Assert.That(markdown.RenderHtmlSourceWithPipeline("~~old~~", commonMark), Is.EqualTo("<p>~~old~~</p>\n"));
        Assert.That(markdown.RenderHtmlSourceWithPipeline("~~old~~", gfm), Is.EqualTo("<p><del>old</del></p>\n"));
        Assert.That(
            markdown.GetDataServices().CreateTreeOperations().FindFirstInDocument(
                markdown.ParseSourceWithPipeline("Math $x$", advanced),
                MarkdownNodeKind.MathSpan),
            Is.Not.Null);
        Assert.That(markdown.ParseSourceWithPipeline("text", zero).Root.Children, Is.Empty);
    }

    [Test]
    public void PublicOptions_ConfigureFormattingSafetyAndEveryResourceLimit()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline configured = markdown.CreatePipelineBuilder()
            .AllowUnsafeHtml()
            .AllowUnsafeLinks()
            .TrackTrivia()
            .UseXhtml()
            .UseHardWrap()
            .SetDeterministicOutput(false)
            .SetSoftBreakMode(MarkdownSoftBreakMode.Space)
            .SetLineEnding("\r\n")
            .SetMaximumNestingDepth(11)
            .SetMaximumInputSize(120)
            .SetMaximumReferenceDefinitions(13)
            .SetMaximumOutputSize(140)
            .SetMaximumLineWidth(12)
            .Build();
        MarkdownOptions options = configured.GetOptions();

        Assert.That(configured.AllowsRawHtml(), Is.True);
        Assert.That(configured.AllowsUnsafeHtml(), Is.True);
        Assert.That(configured.AllowsUnsafeLinks(), Is.True);
        Assert.That(configured.TracksTrivia(), Is.True);
        Assert.That(options.Xhtml, Is.True);
        Assert.That(options.HardWrap, Is.True);
        Assert.That(options.DeterministicOutput, Is.False);
        Assert.That(options.SoftBreakMode, Is.EqualTo(MarkdownSoftBreakMode.Space));
        Assert.That(options.LineEnding, Is.EqualTo("\r\n"));
        Assert.That(options.MaximumNestingDepth, Is.EqualTo(11));
        Assert.That(options.MaximumInputSize, Is.EqualTo(120));
        Assert.That(options.MaximumReferenceDefinitions, Is.EqualTo(13));
        Assert.That(options.MaximumOutputSize, Is.EqualTo(140));
        Assert.That(options.MaximumLineWidth, Is.EqualTo(12));
        Assert.That(
            markdown.RenderHtmlSourceWithPipeline("a\nb", configured),
            Is.EqualTo("<p>a<br />\r\nb</p>\r\n"));
        Assert.That(
            markdown.RenderHtmlSourceWithPipeline("<b>raw</b>", configured),
            Is.EqualTo("<p><b>raw</b></p>\r\n"));

        MarkdownPipeline inputLimit = markdown.CreatePipelineBuilder().SetMaximumInputSize(3).Build();
        MarkdownPipeline nestingLimit = markdown.CreatePipelineBuilder().SetMaximumNestingDepth(1).Build();
        MarkdownPipeline referenceLimit = markdown.CreatePipelineBuilder().SetMaximumReferenceDefinitions(1).Build();
        MarkdownPipeline outputLimit = markdown.CreatePipelineBuilder().SetMaximumOutputSize(4).Build();
        MarkdownPipeline wrapping = markdown.CreatePipelineBuilder()
            .SetMaximumLineWidth(12)
            .SetLineEnding("\r\n")
            .Build();

        Assert.That(
            HasDiagnostic(markdown.ParseSourceWithPipeline("four", inputLimit), "MD_LIMIT_INPUT_SIZE"),
            Is.True);
        Assert.That(
            HasDiagnostic(markdown.ParseSourceWithPipeline("text", nestingLimit), "MD_LIMIT_NESTING_DEPTH"),
            Is.True);
        Assert.That(
            HasDiagnostic(
                markdown.ParseSourceWithPipeline("[a]: /a\n[b]: /b", referenceLimit),
                "MD_LIMIT_REFERENCE_DEFINITIONS"),
            Is.True);
        Assert.That(
            () => markdown.RenderHtmlSourceWithPipeline("text", outputLimit),
            Throws.TypeOf<InvalidOperationException>());
        Assert.That(
            markdown.RenderMarkdownSourceWithPipeline("alpha beta gamma", wrapping),
            Is.EqualTo("alpha beta\r\ngamma\r\n"));
    }

    [Test]
    public void BuiltPipeline_IsAnImmutableSnapshotOfOptionsMappingsAndRegistries()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        IMarkdownTransform transform = new TestTransform(
            "snapshot.transform",
            (document, context) => markdown.GetDataServices()
                .CreateNodeOperations(document.Root)
                .SetAttribute("snapshot", "true"));
        builder.UseAdvancedExtensions()
            .AllowUnsafeLinks()
            .MapEmojiShortcode("ship", "SHIP")
            .RegisterCustomInlineDelimiter("kbd", "%%", "%%")
            .AddTransform(transform);
        MarkdownPipeline snapshot = builder.Build();

        MarkdownOptions optionsCopy = snapshot.GetOptions();
        MarkdownFeatureSet featuresCopy = snapshot.GetFeatures();
        MarkdownRuleRegistration[] registrationsCopy = snapshot.GetTransformRegistrations();
        optionsCopy.AllowUnsafeLinks = false;
        optionsCopy.Features.Emoji = false;
        featuresCopy.Emoji = false;
        registrationsCopy[0].Name = "changed";
        registrationsCopy[0].Rule = new object();
        builder.UseZero()
            .ClearEmojiShortcodes()
            .ClearCustomInlineDelimiters()
            .ClearTransforms();

        MarkdownDocument document = markdown.ParseSourceWithPipeline(":ship: %%Ctrl%%", snapshot);
        IMarkdownTreeOperations trees = markdown.GetDataServices().CreateTreeOperations();
        MarkdownPipeline secondSnapshot = snapshot.GetOptions().Features.Emoji
            ? markdown.CreatePipelineFromOptions(snapshot.GetOptions())
            : markdown.CreateCommonMarkPipeline();

        Assert.That(snapshot.UsesAdvancedExtensions(), Is.True);
        Assert.That(snapshot.AllowsUnsafeLinks(), Is.True);
        Assert.That(trees.FindFirstInDocument(document, MarkdownNodeKind.Emoji)!.Data.Emoji!.Replacement, Is.EqualTo("SHIP"));
        Assert.That(trees.FindFirstInDocument(document, MarkdownNodeKind.CustomInline), Is.Not.Null);
        Assert.That(document.Root.Attributes[0].Name, Is.EqualTo("snapshot"));
        Assert.That(snapshot.GetTransformRegistrations()[0].Name, Is.EqualTo("snapshot.transform"));
        Assert.That(secondSnapshot.GetFeatures().Emoji, Is.True);
        Assert.That(builder.GetTransformRegistrations(), Is.Empty);
    }

    [Test]
    public void OrderedRegistries_SupportAddInsertReplaceRemoveClearAndSnapshotIsolation()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        builder.ClearBlockRules().ClearInlineRules().ClearTransforms().ClearHtmlRenderRules();

        IMarkdownBlockRule blockCenter = new TestBlockRule("block.center", context => false);
        IMarkdownBlockRule blockBefore = new TestBlockRule("block.before", context => false);
        IMarkdownBlockRule blockAfter = new TestBlockRule("block.after", context => false);
        IMarkdownBlockRule blockReplacement = new TestBlockRule("block.replacement", context => false);
        IMarkdownInlineRule inlineCenter = new TestInlineRule("inline.center", context => false);
        IMarkdownInlineRule inlineBefore = new TestInlineRule("inline.before", context => false);
        IMarkdownInlineRule inlineAfter = new TestInlineRule("inline.after", context => false);
        IMarkdownInlineRule inlineReplacement = new TestInlineRule("inline.replacement", context => false);
        IMarkdownTransform transformCenter = new TestTransform("transform.center", (document, context) => { });
        IMarkdownTransform transformBefore = new TestTransform("transform.before", (document, context) => { });
        IMarkdownTransform transformAfter = new TestTransform("transform.after", (document, context) => { });
        IMarkdownTransform transformReplacement = new TestTransform("transform.replacement", (document, context) => { });
        IHtmlRenderRule renderCenter = new TestHtmlRenderRule("render.center", (context, node) => false);
        IHtmlRenderRule renderBefore = new TestHtmlRenderRule("render.before", (context, node) => false);
        IHtmlRenderRule renderAfter = new TestHtmlRenderRule("render.after", (context, node) => false);
        IHtmlRenderRule renderReplacement = new TestHtmlRenderRule("render.replacement", (context, node) => false);

        builder.AddBlockRule(blockCenter)
            .InsertBlockRuleBefore("block.center", blockBefore)
            .InsertBlockRuleAfter("block.center", blockAfter)
            .ReplaceBlockRule("block.center", blockReplacement)
            .RemoveBlockRule("block.before")
            .AddInlineRule(inlineCenter)
            .InsertInlineRuleBefore("inline.center", inlineBefore)
            .InsertInlineRuleAfter("inline.center", inlineAfter)
            .ReplaceInlineRule("inline.center", inlineReplacement)
            .RemoveInlineRule("inline.before")
            .AddTransform(transformCenter)
            .InsertTransformBefore("transform.center", transformBefore)
            .InsertTransformAfter("transform.center", transformAfter)
            .ReplaceTransform("transform.center", transformReplacement)
            .RemoveTransform("transform.before")
            .AddHtmlRenderRule(renderCenter)
            .InsertHtmlRenderRuleBefore("render.center", renderBefore)
            .InsertHtmlRenderRuleAfter("render.center", renderAfter)
            .ReplaceHtmlRenderRule("render.center", renderReplacement)
            .RemoveHtmlRenderRule("render.before");
        MarkdownPipeline snapshot = builder.Build();

        AssertNames(builder.GetBlockRuleRegistrations(), "block.replacement", "block.after");
        AssertNames(builder.GetInlineRuleRegistrations(), "inline.replacement", "inline.after");
        AssertNames(builder.GetTransformRegistrations(), "transform.replacement", "transform.after");
        AssertNames(builder.GetHtmlRenderRuleRegistrations(), "render.replacement", "render.after");

        builder.ClearBlockRules().ClearInlineRules().ClearTransforms().ClearHtmlRenderRules();
        Assert.That(builder.GetBlockRuleRegistrations(), Is.Empty);
        Assert.That(builder.GetInlineRuleRegistrations(), Is.Empty);
        Assert.That(builder.GetTransformRegistrations(), Is.Empty);
        Assert.That(builder.GetHtmlRenderRuleRegistrations(), Is.Empty);
        AssertNames(snapshot.GetBlockRuleRegistrations(), "block.replacement", "block.after");
        AssertNames(snapshot.GetInlineRuleRegistrations(), "inline.replacement", "inline.after");
        AssertNames(snapshot.GetTransformRegistrations(), "transform.replacement", "transform.after");
        AssertNames(snapshot.GetHtmlRenderRuleRegistrations(), "render.replacement", "render.after");
    }

    [Test]
    public void CustomBlockAndInlineTriggers_ConsumeInputAndPublishDiagnostics()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownBlockRule blockRule = new TestBlockRule(
            "custom.percent-block",
            context =>
            {
                string line = context.GetCurrentLineText();
                if (!line.StartsWith("%% ", StringComparison.Ordinal))
                {
                    return false;
                }

                MarkdownNode paragraph = new MarkdownNode();
                paragraph.Kind = MarkdownNodeKind.Paragraph;
                paragraph.Literal = line.Substring(3);
                paragraph.Span = context.GetCurrentLineSpan();
                context.AppendBlock(paragraph);
                context.AddDiagnostic(
                    MarkdownDiagnosticSeverity.Info,
                    "CUSTOM_BLOCK",
                    "custom block matched",
                    context.GetCurrentLineSpan());
                context.AdvanceLine();
                return true;
            });
        IMarkdownInlineRule inlineRule = new TestInlineRule(
            "custom.mention",
            "@",
            context =>
            {
                if (!context.GetRemainingText().StartsWith("@x", StringComparison.Ordinal))
                {
                    return false;
                }

                MarkdownNode text = new MarkdownNode();
                text.Kind = MarkdownNodeKind.Text;
                text.Literal = "mention";
                context.AppendInline(text);
                context.AddDiagnostic(
                    MarkdownDiagnosticSeverity.Info,
                    "CUSTOM_INLINE",
                    "custom inline matched",
                    context.GetContainer().Span);
                context.AdvanceBy(2);
                return true;
            });
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .AddBlockRule(blockRule)
            .AddInlineRule(inlineRule)
            .Build();

        MarkdownDocument document = markdown.ParseSourceWithPipeline("%% block @x", pipeline);

        Assert.That(markdown.RenderPlainTextDocumentWithPipeline(document, pipeline), Is.EqualTo("block mention\n"));
        Assert.That(HasDiagnostic(document, "CUSTOM_BLOCK"), Is.True);
        Assert.That(HasDiagnostic(document, "CUSTOM_INLINE"), Is.True);
        Assert.That(document.Diagnostics.Items.Count, Is.EqualTo(2));
    }

    [Test]
    public void CustomRulesThatReportSuccessWithoutProgress_RecoverDeterministically()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownBlockRule blockRule = new TestBlockRule("custom.stalled-block", context => true);
        IMarkdownInlineRule inlineRule = new TestInlineRule(
            "custom.stalled-inline",
            "@",
            context => context.GetCurrentCharacter() == '@');
        MarkdownPipeline blockPipeline = markdown.CreatePipelineBuilder()
            .AddBlockRule(blockRule)
            .Build();
        MarkdownPipeline inlinePipeline = markdown.CreatePipelineBuilder()
            .AddInlineRule(inlineRule)
            .Build();

        MarkdownDocument block = markdown.ParseSourceWithPipeline("line", blockPipeline);
        MarkdownDocument inline = markdown.ParseSourceWithPipeline("before @ after", inlinePipeline);

        Assert.That(HasDiagnostic(block, "MD_BLOCK_RULE_NO_PROGRESS"), Is.True);
        Assert.That(HasDiagnostic(inline, "MD_INLINE_RULE_NO_PROGRESS"), Is.True);
        Assert.That(markdown.RenderPlainTextDocumentWithPipeline(inline, inlinePipeline), Does.Contain("before"));
    }

    [Test]
    public void TransformsAndHtmlRules_ExecuteInConfiguredOrder()
    {
        IMarkdown markdown = new Markdown();
        List<string> transformCalls = new List<string>();
        List<string> renderCalls = new List<string>();
        IMarkdownTransform center = new TestTransform(
            "transform.center",
            (document, context) => transformCalls.Add("center"));
        IMarkdownTransform before = new TestTransform(
            "transform.before",
            (document, context) => transformCalls.Add("before"));
        IMarkdownTransform after = new TestTransform(
            "transform.after",
            (document, context) => transformCalls.Add("after"));
        IMarkdownTransform replacement = new TestTransform(
            "transform.replacement",
            (document, context) => transformCalls.Add("replacement"));
        IHtmlRenderRule firstRenderer = new TestHtmlRenderRule(
            "render.first",
            (context, node) =>
            {
                if (node.Kind == MarkdownNodeKind.Heading)
                {
                    renderCalls.Add("first");
                }

                return false;
            });
        IHtmlRenderRule handlingRenderer = new TestHtmlRenderRule(
            "render.handling",
            (context, node) =>
            {
                if (node.Kind != MarkdownNodeKind.Heading)
                {
                    return false;
                }

                renderCalls.Add("handling");
                context.Writer.Write("<custom-heading>");
                markdown.RenderHtmlChildren(context, node);
                context.Writer.Write("</custom-heading>");
                markdown.WriteHtmlLineEnding(context);
                return true;
            });
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .AddTransform(center)
            .InsertTransformBefore("transform.center", before)
            .InsertTransformAfter("transform.center", after)
            .ReplaceTransform("transform.center", replacement)
            .AddHtmlRenderRule(firstRenderer)
            .InsertHtmlRenderRuleAfter("render.first", handlingRenderer)
            .Build();

        MarkdownDocument document = markdown.ParseSourceWithPipeline("# Heading", pipeline);
        string html = markdown.RenderHtmlDocumentWithPipeline(document, pipeline);

        Assert.That(transformCalls, Is.EqualTo(new[] { "before", "replacement", "after" }));
        Assert.That(renderCalls, Is.EqualTo(new[] { "first", "handling" }));
        Assert.That(html, Is.EqualTo("<custom-heading>Heading</custom-heading>\n"));
    }

    [Test]
    public void OneBuiltInPipeline_CanBeReusedConcurrentlyForParseAndRender()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .UseAdvancedExtensions()
            .MapEmojiShortcode("ship", "🚢")
            .Build();
        string source =
            "# Title\n\n" +
            "| A | B |\n| --- | --- |\n| ~~old~~ | :ship: |\n\n" +
            "Math $x$ and [link](/url).";
        string expected = RenderAll(markdown, pipeline, source);
        Task<string>[] tasks = new Task<string>[48];
        int index = 0;
        while (index < tasks.Length)
        {
            tasks[index] = Task.Run(() => RenderAll(markdown, pipeline, source));
            index = index + 1;
        }

        Task.WhenAll(tasks).GetAwaiter().GetResult();

        index = 0;
        while (index < tasks.Length)
        {
            Assert.That(tasks[index].Result, Is.EqualTo(expected), "concurrent operation " + index.ToString());
            index = index + 1;
        }
    }

    public static string RenderAll(IMarkdown markdown, MarkdownPipeline pipeline, string source)
    {
        MarkdownDocument document = markdown.ParseSourceWithPipeline(source, pipeline);
        return markdown.RenderHtmlDocumentWithPipeline(document, pipeline)
            + "\u001f"
            + markdown.RenderMarkdownDocumentWithPipeline(document, pipeline)
            + "\u001f"
            + markdown.RenderPlainTextDocumentWithPipeline(document, pipeline);
    }

    public static bool HasDiagnostic(MarkdownDocument document, string code)
    {
        int index = 0;
        while (index < document.Diagnostics.Items.Count)
        {
            if (document.Diagnostics.Items[index].Code == code)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    public static void AssertNames(MarkdownRuleRegistration[] registrations, params string[] names)
    {
        Assert.That(registrations.Length, Is.EqualTo(names.Length));
        int index = 0;
        while (index < names.Length)
        {
            Assert.That(registrations[index].Name, Is.EqualTo(names[index]));
            index = index + 1;
        }
    }
}
