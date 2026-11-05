using System;

namespace markdown;

/// <summary>
/// Builds a configured Markdown pipeline through chainable methods.
/// </summary>
/// <remarks>
/// The builder is mutable. <see cref="MarkdownPipelineBuilder.Build"/> copies its current state, so later builder changes do not affect an existing pipeline.
/// </remarks>
public class MarkdownPipelineBuilder : IMarkdownPipelineBuilder
{
    internal const string CodeBlockRenderRuleName = "html.code-block-hook";
    internal const string LinkRenderRuleName = "html.link-hook";
    internal const string ImageRenderRuleName = "html.image-hook";
    internal const string RawHtmlRenderRuleName = "html.raw-html-hook";
    internal MarkdownOptions Options;
    internal MarkdownRuleList BlockRules;
    internal MarkdownRuleList InlineRules;
    internal MarkdownRuleList TransformRules;
    internal MarkdownRuleList HtmlRenderRules;

    /// <summary>
    /// Initializes a new <see cref="MarkdownPipelineBuilder"/> instance.
    /// </summary>
    public MarkdownPipelineBuilder()
    {
        Options = MarkdownOptions.CommonMarkPreset();
        BlockRules = new MarkdownRuleList();
        InlineRules = new MarkdownRuleList();
        TransformRules = new MarkdownRuleList();
        HtmlRenderRules = new MarkdownRuleList();
    }

    /// <summary>
    /// Creates a builder from copied options.
    /// </summary>
    /// <param name="options">The options to copy.</param>
    internal static MarkdownPipelineBuilder Create(MarkdownOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        MarkdownPipelineBuilder builder = new MarkdownPipelineBuilder();
        builder.Options = options.Copy();
        builder.SynchronizeFeatureRules();
        return builder;
    }

    /// <summary>
    /// Creates a builder initialized with CommonMark and safe rendering defaults.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownPipelineBuilder"/> value.</returns>
    internal static MarkdownPipelineBuilder Create()
    {
        return new MarkdownPipelineBuilder();
    }

    /// <summary>
    /// Creates a pipeline configured for CommonMark syntax and safe rendering defaults.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownPipelineBuilder"/> value.</returns>
    internal static MarkdownPipelineBuilder CommonMarkPreset()
    {
        return Create(MarkdownOptions.CommonMarkPreset());
    }

    /// <summary>
    /// Creates a pipeline configured for CommonMark plus GitHub Flavored Markdown extensions.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownPipelineBuilder"/> value.</returns>
    internal static MarkdownPipelineBuilder GfmPreset()
    {
        return Create(MarkdownOptions.GfmPreset());
    }

    /// <summary>
    /// Creates a pipeline configured with all built-in Markdown extensions.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownPipelineBuilder"/> value.</returns>
    internal static MarkdownPipelineBuilder AdvancedPreset()
    {
        return Create(MarkdownOptions.AdvancedPreset());
    }

    /// <summary>
    /// Creates a pipeline with all syntax features disabled for caller-defined configuration.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownPipelineBuilder"/> value.</returns>
    internal static MarkdownPipelineBuilder ZeroPreset()
    {
        return MarkdownPipelineBuilder.Create(MarkdownOptions.ZeroPreset());
    }

    /// <summary>
    /// Returns a detached copy of the builder's current options.
    /// </summary>
    /// <returns>Options that callers may modify without changing this builder.</returns>
    public MarkdownOptions GetOptions()
    {
        return Options.Copy();
    }

    /// <summary>
    /// Creates an immutable pipeline snapshot from the builder's current state.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownPipeline"/> value.</returns>
    public MarkdownPipeline Build()
    {
        return MarkdownPipeline.Create(Options, BlockRules, InlineRules, TransformRules, HtmlRenderRules);
    }

    /// <summary>
    /// Replaces the current options with a detached copy of the supplied options.
    /// </summary>
    /// <param name="options">The options to copy.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder SetOptions(MarkdownOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        Options = options.Copy();
        SynchronizeFeatureRules();
        return this;
    }

    /// <summary>
    /// Replaces every syntax feature flag with a detached copy of the supplied feature set.
    /// </summary>
    /// <param name="features">The feature set to copy.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder SetFeatures(MarkdownFeatureSet features)
    {
        Options.SetFeatureSet(features);
        SynchronizeFeatureRules();
        return this;
    }

    /// <summary>
    /// Replaces the syntax rules and feature flags with the CommonMark preset.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder UseCommonMark()
    {
        Options.SetFeatureSet(MarkdownFeatureSet.CommonMarkPreset());
        SynchronizeFeatureRules();
        return this;
    }

    /// <summary>
    /// Replaces the syntax rules and feature flags with the GFM preset.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder UseGfm()
    {
        Options.SetFeatureSet(MarkdownFeatureSet.GfmPreset());
        SynchronizeFeatureRules();
        return this;
    }

    /// <summary>
    /// Replaces the syntax rules and feature flags with the complete built-in extension preset.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder UseAdvancedExtensions()
    {
        Options.SetFeatureSet(MarkdownFeatureSet.AdvancedPreset());
        SynchronizeFeatureRules();
        return this;
    }

    /// <summary>
    /// Removes all built-in parser rules and disables every syntax feature.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder UseZero()
    {
        Options.SetFeatureSet(MarkdownFeatureSet.ZeroPreset());
        SynchronizeFeatureRules();
        return this;
    }

    internal void SynchronizeFeatureRules()
    {
        if (Options.Features.CommonMark)
        {
            CommonMarkBlockRules.RegisterCommonMarkBlockRules(BlockRules);
            CommonMarkInlineRules.RegisterCommonMarkInlineRules(InlineRules);
        }
        else
        {
            BlockRules.Remove(CommonMarkBlockRules.BlankLineRuleName);
            BlockRules.Remove(CommonMarkBlockRules.BlockQuoteRuleName);
            BlockRules.Remove(CommonMarkBlockRules.BulletListRuleName);
            BlockRules.Remove(CommonMarkBlockRules.AtxHeadingRuleName);
            BlockRules.Remove(CommonMarkBlockRules.ThematicBreakRuleName);
            BlockRules.Remove(CommonMarkBlockRules.FencedCodeBlockRuleName);
            BlockRules.Remove(CommonMarkBlockRules.HtmlBlockRuleName);
            BlockRules.Remove(CommonMarkBlockRules.IndentedCodeBlockRuleName);
            BlockRules.Remove(CommonMarkBlockRules.LinkReferenceDefinitionRuleName);
            BlockRules.Remove(CommonMarkBlockRules.OrderedListRuleName);
            BlockRules.Remove(CommonMarkBlockRules.ParagraphRuleName);
            InlineRules.Remove(CommonMarkInlineRules.CodeSpanRuleName);
            InlineRules.Remove(CommonMarkInlineRules.HardLineBreakRuleName);
            InlineRules.Remove(CommonMarkInlineRules.BackslashEscapeRuleName);
            InlineRules.Remove(CommonMarkInlineRules.EntityReferenceRuleName);
            InlineRules.Remove(CommonMarkInlineRules.AutolinkRuleName);
            InlineRules.Remove(CommonMarkInlineRules.HtmlInlineRuleName);
            InlineRules.Remove(CommonMarkInlineRules.ImageRuleName);
            InlineRules.Remove(CommonMarkInlineRules.LinkRuleName);
            InlineRules.Remove(CommonMarkInlineRules.EmphasisRuleName);
            InlineRules.Remove(CommonMarkInlineRules.SoftLineBreakRuleName);
            InlineRules.Remove(CommonMarkInlineRules.TextRuleName);
        }

        if (Options.Features.GfmTables)
        {
            GfmBlockRules.RegisterGfmBlockRules(BlockRules);
        }
        else
        {
            BlockRules.Remove(GfmBlockRules.TableRuleName);
        }

        if (Options.Features.GfmStrikethrough)
        {
            GfmInlineRules.RegisterGfmStrikethroughRule(InlineRules);
        }
        else
        {
            InlineRules.Remove(GfmInlineRules.StrikethroughRuleName);
        }

        if (Options.Features.GfmBareAutolinks)
        {
            GfmInlineRules.RegisterGfmBareAutolinkRule(InlineRules);
        }
        else
        {
            InlineRules.Remove(GfmInlineRules.BareAutolinkRuleName);
        }

        if (Options.Features.Footnotes)
        {
            FootnoteBlockRules.RegisterFootnoteBlockRules(BlockRules);
            FootnoteInlineRules.RegisterFootnoteInlineRules(InlineRules);
        }
        else
        {
            BlockRules.Remove(FootnoteBlockRules.DefinitionRuleName);
            InlineRules.Remove(FootnoteInlineRules.ReferenceRuleName);
        }

        if (Options.Features.DefinitionLists)
        {
            DefinitionListBlockRules.RegisterDefinitionListBlockRules(BlockRules);
        }
        else
        {
            BlockRules.Remove(DefinitionListBlockRules.DefinitionListRuleName);
        }

        if (Options.Features.FrontMatter)
        {
            FrontMatterBlockRules.RegisterFrontMatterBlockRules(BlockRules);
        }
        else
        {
            BlockRules.Remove(FrontMatterBlockRules.FrontMatterRuleName);
        }

        if (Options.Features.Abbreviations)
        {
            AbbreviationBlockRules.RegisterAbbreviationBlockRules(BlockRules);
        }
        else
        {
            BlockRules.Remove(AbbreviationBlockRules.DefinitionRuleName);
        }

        if (Options.Features.Attributes)
        {
            GenericAttributeRules.RegisterBlockRule(BlockRules);
            GenericAttributeRules.RegisterInlineRule(InlineRules);
        }
        else
        {
            BlockRules.Remove(GenericAttributeRules.BlockRuleName);
            InlineRules.Remove(GenericAttributeRules.InlineRuleName);
        }

        if (Options.Features.Superscript
            || Options.Features.Subscript
            || Options.Features.InsertedText
            || Options.Features.MarkedText)
        {
            TextFormattingInlineRules.RegisterTextFormattingInlineRules(InlineRules);
        }
        else
        {
            InlineRules.Remove(TextFormattingInlineRules.TextFormattingRuleName);
        }

        if (Options.Features.Math)
        {
            MathBlockRules.RegisterMathBlockRules(BlockRules);
            MathInlineRules.RegisterMathInlineRules(InlineRules);
        }
        else
        {
            BlockRules.Remove(MathBlockRules.MathBlockRuleName);
            InlineRules.Remove(MathInlineRules.MathSpanRuleName);
        }

        if (Options.Features.Diagrams)
        {
            DiagramBlockRules.RegisterDiagramBlockRules(BlockRules);
        }
        else
        {
            BlockRules.Remove(DiagramBlockRules.DiagramBlockRuleName);
        }

        if (Options.Features.CustomFencedBlocks)
        {
            CustomFencedBlockRules.RegisterCustomFencedBlockRules(BlockRules);
        }
        else
        {
            BlockRules.Remove(CustomFencedBlockRules.CustomFencedBlockRuleName);
        }

        if (Options.Features.Emoji)
        {
            EmojiInlineRules.RegisterEmojiInlineRules(InlineRules);
        }
        else
        {
            InlineRules.Remove(EmojiInlineRules.EmojiRuleName);
        }

        if (Options.Features.CustomInlineDelimiters)
        {
            CustomInlineDelimiterRules.RegisterCustomInlineDelimiterRules(InlineRules);
        }
        else
        {
            InlineRules.Remove(CustomInlineDelimiterRules.CustomInlineDelimiterRuleName);
        }

        if (Options.Features.Admonitions)
        {
            AdmonitionBlockRules.RegisterAdmonitionBlockRules(BlockRules);
        }
        else
        {
            BlockRules.Remove(AdmonitionBlockRules.AdmonitionBlockRuleName);
        }

        if (Options.Features.CustomContainers)
        {
            CustomContainerBlockRules.RegisterCustomContainerBlockRules(BlockRules);
        }
        else
        {
            BlockRules.Remove(CustomContainerBlockRules.CustomContainerBlockRuleName);
        }

        if (Options.Features.Details)
        {
            DetailsBlockRules.RegisterDetailsBlockRules(BlockRules);
        }
        else
        {
            BlockRules.Remove(DetailsBlockRules.DetailsBlockRuleName);
        }
    }

    /// <summary>
    /// Disables HTML.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder DisableHtml()
    {
        Options.AllowRawHtml = false;
        Options.AllowUnsafeHtml = false;
        return this;
    }

    /// <summary>
    /// Allows raw HTML.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder AllowRawHtml()
    {
        Options.AllowRawHtml = true;
        return this;
    }

    /// <summary>
    /// Allows unsafe HTML.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder AllowUnsafeHtml()
    {
        Options.AllowRawHtml = true;
        Options.AllowUnsafeHtml = true;
        return this;
    }

    /// <summary>
    /// Allows unsafe links.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder AllowUnsafeLinks()
    {
        Options.AllowUnsafeLinks = true;
        return this;
    }

    /// <summary>Sets the policy used to allow or reject rewritten link destinations.</summary>
    /// <param name="policy">The link URI policy.</param>
    /// <returns>This builder.</returns>
    public MarkdownPipelineBuilder SetLinkUriPolicy(IMarkdownUriPolicy policy)
    {
        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        Options.LinkUriPolicy = policy;
        return this;
    }

    /// <summary>Clears the custom link URI policy.</summary>
    /// <returns>This builder.</returns>
    public MarkdownPipelineBuilder ClearLinkUriPolicy()
    {
        Options.LinkUriPolicy = null;
        return this;
    }

    /// <summary>Sets the policy used to allow or reject rewritten image destinations.</summary>
    /// <param name="policy">The image URI policy.</param>
    /// <returns>This builder.</returns>
    public MarkdownPipelineBuilder SetImageUriPolicy(IMarkdownUriPolicy policy)
    {
        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        Options.ImageUriPolicy = policy;
        return this;
    }

    /// <summary>Clears the custom image URI policy.</summary>
    /// <returns>This builder.</returns>
    public MarkdownPipelineBuilder ClearImageUriPolicy()
    {
        Options.ImageUriPolicy = null;
        return this;
    }

    /// <summary>Sets the render-time link destination rewrite hook.</summary>
    /// <param name="rewriter">The destination rewriter.</param>
    /// <returns>This builder.</returns>
    public MarkdownPipelineBuilder RewriteLinkUrisWith(IMarkdownDestinationRewriter rewriter)
    {
        if (rewriter == null)
        {
            throw new ArgumentNullException("rewriter");
        }

        Options.LinkUriRewriter = rewriter;
        return this;
    }

    /// <summary>Clears the render-time link destination rewrite hook.</summary>
    /// <returns>This builder.</returns>
    public MarkdownPipelineBuilder ClearLinkUriRewriter()
    {
        Options.LinkUriRewriter = null;
        return this;
    }

    /// <summary>Sets the render-time image destination rewrite hook.</summary>
    /// <param name="rewriter">The destination rewriter.</param>
    /// <returns>This builder.</returns>
    public MarkdownPipelineBuilder RewriteImageUrisWith(IMarkdownDestinationRewriter rewriter)
    {
        if (rewriter == null)
        {
            throw new ArgumentNullException("rewriter");
        }

        Options.ImageUriRewriter = rewriter;
        return this;
    }

    /// <summary>Clears the render-time image destination rewrite hook.</summary>
    /// <returns>This builder.</returns>
    public MarkdownPipelineBuilder ClearImageUriRewriter()
    {
        Options.ImageUriRewriter = null;
        return this;
    }

    /// <summary>Sets the explicit raw-HTML filtering policy.</summary>
    /// <param name="policy">The raw-HTML policy.</param>
    /// <returns>This builder.</returns>
    public MarkdownPipelineBuilder SetRawHtmlPolicy(IMarkdownRawHtmlPolicy policy)
    {
        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        Options.RawHtmlPolicy = policy;
        return this;
    }

    /// <summary>Clears the custom raw-HTML policy.</summary>
    /// <returns>This builder.</returns>
    public MarkdownPipelineBuilder ClearRawHtmlPolicy()
    {
        Options.RawHtmlPolicy = null;
        return this;
    }

    /// <summary>
    /// Enables tracking of trivia.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder TrackTrivia()
    {
        Options.TrackTrivia = true;
        return this;
    }

    /// <summary>
    /// Configures XHTML.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder UseXhtml()
    {
        Options.Xhtml = true;
        return this;
    }

    /// <summary>
    /// Configures hard wrap.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder UseHardWrap()
    {
        Options.HardWrap = true;
        return this;
    }

    /// <summary>
    /// Sets deterministic output.
    /// </summary>
    /// <param name="value">The value to apply.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder SetDeterministicOutput(bool value)
    {
        Options.DeterministicOutput = value;
        return this;
    }

    /// <summary>
    /// Sets soft break mode.
    /// </summary>
    /// <param name="mode">The soft-break rendering mode.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder SetSoftBreakMode(MarkdownSoftBreakMode mode)
    {
        Options.SetSoftBreakMode(mode);
        return this;
    }

    /// <summary>
    /// Configures CJK soft breaks.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder UseCjkSoftBreaks()
    {
        Options.Features.CjkSoftBreaks = true;
        Options.SoftBreakMode = MarkdownSoftBreakMode.Cjk;
        return this;
    }

    /// <summary>
    /// Sets line ending.
    /// </summary>
    /// <param name="lineEnding">The line-ending sequence to emit; use <c>\n</c> or <c>\r\n</c>.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder SetLineEnding(string lineEnding)
    {
        Options.SetLineEnding(lineEnding);
        return this;
    }

    /// <summary>
    /// Maps emoji shortcode.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <param name="replacement">The replacement text.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder MapEmojiShortcode(string name, string replacement)
    {
        Options.EmojiShortcodes.Set(name, replacement);
        return this;
    }

    /// <summary>
    /// Clears emoji shortcodes.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder ClearEmojiShortcodes()
    {
        Options.EmojiShortcodes.Clear();
        return this;
    }

    /// <summary>
    /// Registers custom container.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder RegisterCustomContainer(string name)
    {
        Options.CustomContainers.Set(name);
        return this;
    }

    /// <summary>
    /// Clears custom containers.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder ClearCustomContainers()
    {
        Options.CustomContainers.Clear();
        return this;
    }

    /// <summary>
    /// Registers custom fenced block.
    /// </summary>
    /// <param name="infoKey">The information-string key used to recognize the fenced block.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder RegisterCustomFencedBlock(string infoKey)
    {
        Options.CustomFencedBlocks.Set(infoKey);
        return this;
    }

    /// <summary>
    /// Clears custom fenced blocks.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder ClearCustomFencedBlocks()
    {
        Options.CustomFencedBlocks.Clear();
        return this;
    }

    /// <summary>
    /// Registers custom inline delimiter.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <param name="openingDelimiter">The opening delimiter text.</param>
    /// <param name="closingDelimiter">The closing delimiter text.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder RegisterCustomInlineDelimiter(string name, string openingDelimiter, string closingDelimiter)
    {
        Options.CustomInlineDelimiters.Set(name, openingDelimiter, closingDelimiter);
        return this;
    }

    /// <summary>
    /// Clears custom inline delimiters.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder ClearCustomInlineDelimiters()
    {
        Options.CustomInlineDelimiters.Clear();
        return this;
    }

    /// <summary>
    /// Registers an HTML hook for fenced and indented code blocks.
    /// </summary>
    /// <remarks>
    /// The callback writes through the supplied context and returns <c>true</c> when it handled the node. Returning <c>false</c> selects the built-in renderer.
    /// </remarks>
    /// <param name="handler">The callback invoked for each code block.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder RenderCodeBlocksWith(Func<HtmlRenderContext, MarkdownNode, bool> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException("handler");
        }

        HtmlRenderRules.Set(
            CodeBlockRenderRuleName,
            MarkdownHtmlRenderRule.Create(
                CodeBlockRenderRuleName,
                MarkdownNodeKind.FencedCodeBlock,
                MarkdownNodeKind.IndentedCodeBlock,
                handler));
        return this;
    }

    /// <summary>
    /// Clears code block render hook.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder ClearCodeBlockRenderHook()
    {
        HtmlRenderRules.Remove(CodeBlockRenderRuleName);
        return this;
    }

    /// <summary>
    /// Registers an HTML hook for link nodes.
    /// </summary>
    /// <remarks>
    /// The callback writes through the supplied context and returns <c>true</c> when it handled the node. Returning <c>false</c> selects the built-in renderer.
    /// </remarks>
    /// <param name="handler">The callback invoked for each link.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder RenderLinksWith(Func<HtmlRenderContext, MarkdownNode, bool> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException("handler");
        }

        HtmlRenderRules.Set(
            LinkRenderRuleName,
            new MarkdownHtmlRenderRule(LinkRenderRuleName, MarkdownNodeKind.Link, handler));
        return this;
    }

    /// <summary>
    /// Clears link render hook.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder ClearLinkRenderHook()
    {
        HtmlRenderRules.Remove(LinkRenderRuleName);
        return this;
    }

    /// <summary>
    /// Registers an HTML hook for image nodes.
    /// </summary>
    /// <remarks>
    /// The callback writes through the supplied context and returns <c>true</c> when it handled the node. Returning <c>false</c> selects the built-in renderer.
    /// </remarks>
    /// <param name="handler">The callback invoked for each image.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder RenderImagesWith(Func<HtmlRenderContext, MarkdownNode, bool> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException("handler");
        }

        HtmlRenderRules.Set(
            ImageRenderRuleName,
            new MarkdownHtmlRenderRule(ImageRenderRuleName, MarkdownNodeKind.Image, handler));
        return this;
    }

    /// <summary>
    /// Clears image render hook.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder ClearImageRenderHook()
    {
        HtmlRenderRules.Remove(ImageRenderRuleName);
        return this;
    }

    /// <summary>
    /// Registers an HTML hook for block and inline raw HTML nodes.
    /// </summary>
    /// <remarks>
    /// The callback runs before the normal raw-HTML safety decision and returns <c>true</c> when it handled the node. Returning <c>false</c> selects the built-in safety behavior.
    /// </remarks>
    /// <param name="handler">The callback invoked for each raw HTML node.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder RenderRawHtmlWith(Func<HtmlRenderContext, MarkdownNode, bool> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException("handler");
        }

        HtmlRenderRules.Set(
            RawHtmlRenderRuleName,
            MarkdownHtmlRenderRule.Create(
                RawHtmlRenderRuleName,
                MarkdownNodeKind.HtmlBlock,
                MarkdownNodeKind.HtmlInline,
                handler));
        return this;
    }

    /// <summary>
    /// Clears raw HTML render hook.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder ClearRawHtmlRenderHook()
    {
        HtmlRenderRules.Remove(RawHtmlRenderRuleName);
        return this;
    }

    /// <summary>
    /// Sets maximum nesting depth.
    /// </summary>
    /// <param name="value">The value to apply.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder SetMaximumNestingDepth(int value)
    {
        Options.SetMaximumNestingDepth(value);
        return this;
    }

    /// <summary>
    /// Sets maximum input size.
    /// </summary>
    /// <param name="value">The value to apply.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder SetMaximumInputSize(int value)
    {
        Options.SetMaximumInputSize(value);
        return this;
    }

    /// <summary>
    /// Sets maximum reference definitions.
    /// </summary>
    /// <param name="value">The value to apply.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder SetMaximumReferenceDefinitions(int value)
    {
        Options.SetMaximumReferenceDefinitions(value);
        return this;
    }

    /// <summary>
    /// Sets maximum output size.
    /// </summary>
    /// <param name="value">The value to apply.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder SetMaximumOutputSize(int value)
    {
        Options.SetMaximumOutputSize(value);
        return this;
    }

    /// <summary>
    /// Sets the preferred maximum width for normalized Markdown paragraphs.
    /// </summary>
    /// <param name="value">Zero to disable wrapping, or a positive line width.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder SetMaximumLineWidth(int value)
    {
        Options.SetMaximumLineWidth(value);
        return this;
    }

    /// <summary>
    /// Enables include preprocessing with a caller-supplied resolver.
    /// </summary>
    /// <param name="resolver">The resolver invoked for each include request.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder UseIncludes(IMarkdownIncludeResolver resolver)
    {
        if (resolver == null)
        {
            throw new ArgumentNullException("resolver");
        }

        Options.IncludeResolver = resolver;
        Options.ProcessIncludes = true;
        return this;
    }

    /// <summary>
    /// Disables include preprocessing.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder DisableIncludes()
    {
        Options.ProcessIncludes = false;
        return this;
    }

    /// <summary>
    /// Sets the canonical name of the root source used for relative include resolution and attribution.
    /// </summary>
    /// <param name="sourceName">The root source name, or an empty string for an unnamed source.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder SetIncludeRootName(string sourceName)
    {
        if (sourceName == null)
        {
            throw new ArgumentNullException("sourceName");
        }

        Options.IncludeRootName = sourceName;
        return this;
    }

    /// <summary>
    /// Sets the maximum permitted include nesting depth.
    /// </summary>
    /// <param name="value">The positive include depth limit.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder SetMaximumIncludeDepth(int value)
    {
        Options.SetMaximumIncludeDepth(value);
        return this;
    }

    /// <summary>
    /// Returns a detached copy of the ordered block-rule registrations.
    /// </summary>
    /// <returns>A copy of the block-rule list.</returns>
    internal MarkdownRuleList GetBlockRules()
    {
        return BlockRules.Copy();
    }

    /// <summary>
    /// Returns a detached copy of the ordered inline-rule registrations.
    /// </summary>
    /// <returns>A copy of the inline-rule list.</returns>
    internal MarkdownRuleList GetInlineRules()
    {
        return InlineRules.Copy();
    }

    /// <summary>
    /// Returns a detached copy of the ordered transform registrations.
    /// </summary>
    /// <returns>A copy of the transform-rule list.</returns>
    internal MarkdownRuleList GetTransformRules()
    {
        return TransformRules.Copy();
    }

    /// <summary>
    /// Returns a detached copy of the ordered HTML rendering-rule registrations.
    /// </summary>
    /// <returns>A copy of the HTML rendering-rule list.</returns>
    internal MarkdownRuleList GetHtmlRenderRules()
    {
        return HtmlRenderRules.Copy();
    }

    /// <summary>
    /// Returns detached snapshots of the ordered block-rule registrations.
    /// </summary>
    /// <returns>The registrations in evaluation order.</returns>
    public MarkdownRuleRegistration[] GetBlockRuleRegistrations()
    {
        return BlockRules.GetRegistrations();
    }

    /// <summary>
    /// Returns detached snapshots of the ordered inline-rule registrations.
    /// </summary>
    /// <returns>The registrations in evaluation order.</returns>
    public MarkdownRuleRegistration[] GetInlineRuleRegistrations()
    {
        return InlineRules.GetRegistrations();
    }

    /// <summary>
    /// Returns detached snapshots of the ordered transform registrations.
    /// </summary>
    /// <returns>The registrations in evaluation order.</returns>
    public MarkdownRuleRegistration[] GetTransformRegistrations()
    {
        return TransformRules.GetRegistrations();
    }

    /// <summary>
    /// Returns detached snapshots of the ordered HTML rendering-rule registrations.
    /// </summary>
    /// <returns>The registrations in evaluation order.</returns>
    public MarkdownRuleRegistration[] GetHtmlRenderRuleRegistrations()
    {
        return HtmlRenderRules.GetRegistrations();
    }

    /// <summary>
    /// Adds a block rule before the built-in paragraph fallback when it is present.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <param name="rule">The block rule to register.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    internal MarkdownPipelineBuilder AddNamedBlockRule(string name, IMarkdownBlockRule rule)
    {
        int fallbackIndex = BlockRules.IndexOf(CommonMarkBlockRules.ParagraphRuleName);
        if (fallbackIndex < 0)
        {
            BlockRules.Add(name, rule);
        }
        else
        {
            BlockRules.Insert(fallbackIndex, name, rule);
        }

        return this;
    }

    /// <summary>
    /// Adds a block rule using the stable name returned by the rule.
    /// </summary>
    /// <param name="rule">The block rule to register.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder AddBlockRule(IMarkdownBlockRule rule)
    {
        if (rule == null)
        {
            throw new ArgumentNullException("rule");
        }

        return AddNamedBlockRule(rule.GetName(), rule);
    }

    /// <summary>
    /// Inserts a block rule immediately before an existing named registration.
    /// </summary>
    /// <param name="existingName">The name of the registration that will follow the inserted rule.</param>
    /// <param name="rule">The block rule to insert.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder InsertBlockRuleBefore(string existingName, IMarkdownBlockRule rule)
    {
        if (rule == null)
        {
            throw new ArgumentNullException("rule");
        }

        BlockRules.InsertBefore(existingName, rule.GetName(), rule);
        return this;
    }

    /// <summary>
    /// Inserts a block rule immediately after an existing named registration.
    /// </summary>
    /// <param name="existingName">The name of the registration that will precede the inserted rule.</param>
    /// <param name="rule">The block rule to insert.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder InsertBlockRuleAfter(string existingName, IMarkdownBlockRule rule)
    {
        if (rule == null)
        {
            throw new ArgumentNullException("rule");
        }

        BlockRules.InsertAfter(existingName, rule.GetName(), rule);
        return this;
    }

    /// <summary>
    /// Replaces a named block-rule registration without changing its position.
    /// </summary>
    /// <param name="existingName">The name of the registration to replace.</param>
    /// <param name="rule">The replacement block rule, registered under the name it returns.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder ReplaceBlockRule(string existingName, IMarkdownBlockRule rule)
    {
        if (rule == null)
        {
            throw new ArgumentNullException("rule");
        }

        BlockRules.Replace(existingName, rule.GetName(), rule);
        return this;
    }

    /// <summary>
    /// Removes a named block-rule registration when present.
    /// </summary>
    /// <param name="name">The registration name.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder RemoveBlockRule(string name)
    {
        BlockRules.Remove(name);
        return this;
    }

    /// <summary>
    /// Removes every block-rule registration.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder ClearBlockRules()
    {
        BlockRules.Clear();
        return this;
    }

    /// <summary>
    /// Adds an inline rule before the built-in text fallback when it is present.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <param name="rule">The inline rule to register.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    internal MarkdownPipelineBuilder AddNamedInlineRule(string name, IMarkdownInlineRule rule)
    {
        int fallbackIndex = InlineRules.IndexOf(CommonMarkInlineRules.TextRuleName);
        if (fallbackIndex < 0)
        {
            InlineRules.Add(name, rule);
        }
        else
        {
            InlineRules.Insert(fallbackIndex, name, rule);
        }

        return this;
    }

    /// <summary>
    /// Adds an inline rule using the stable name returned by the rule.
    /// </summary>
    /// <param name="rule">The inline rule to register.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder AddInlineRule(IMarkdownInlineRule rule)
    {
        if (rule == null)
        {
            throw new ArgumentNullException("rule");
        }

        return AddNamedInlineRule(rule.GetName(), rule);
    }

    /// <summary>
    /// Inserts an inline rule immediately before an existing named registration.
    /// </summary>
    /// <param name="existingName">The name of the registration that will follow the inserted rule.</param>
    /// <param name="rule">The inline rule to insert.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder InsertInlineRuleBefore(string existingName, IMarkdownInlineRule rule)
    {
        if (rule == null)
        {
            throw new ArgumentNullException("rule");
        }

        InlineRules.InsertBefore(existingName, rule.GetName(), rule);
        return this;
    }

    /// <summary>
    /// Inserts an inline rule immediately after an existing named registration.
    /// </summary>
    /// <param name="existingName">The name of the registration that will precede the inserted rule.</param>
    /// <param name="rule">The inline rule to insert.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder InsertInlineRuleAfter(string existingName, IMarkdownInlineRule rule)
    {
        if (rule == null)
        {
            throw new ArgumentNullException("rule");
        }

        InlineRules.InsertAfter(existingName, rule.GetName(), rule);
        return this;
    }

    /// <summary>
    /// Replaces a named inline-rule registration without changing its position.
    /// </summary>
    /// <param name="existingName">The name of the registration to replace.</param>
    /// <param name="rule">The replacement inline rule, registered under the name it returns.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder ReplaceInlineRule(string existingName, IMarkdownInlineRule rule)
    {
        if (rule == null)
        {
            throw new ArgumentNullException("rule");
        }

        InlineRules.Replace(existingName, rule.GetName(), rule);
        return this;
    }

    /// <summary>
    /// Removes a named inline-rule registration when present.
    /// </summary>
    /// <param name="name">The registration name.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder RemoveInlineRule(string name)
    {
        InlineRules.Remove(name);
        return this;
    }

    /// <summary>
    /// Removes every inline-rule registration.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder ClearInlineRules()
    {
        InlineRules.Clear();
        return this;
    }

    /// <summary>
    /// Adds transform.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <param name="rule">The transform to register.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    internal MarkdownPipelineBuilder AddNamedTransform(string name, IMarkdownTransform rule)
    {
        TransformRules.Add(name, rule);
        return this;
    }

    /// <summary>
    /// Adds transform.
    /// </summary>
    /// <param name="transform">The transform to register or replace.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder AddTransform(IMarkdownTransform transform)
    {
        if (transform == null)
        {
            throw new ArgumentNullException("transform");
        }

        TransformRules.Add(transform.GetName(), transform);
        return this;
    }

    /// <summary>
    /// Inserts a transform immediately before an existing named registration.
    /// </summary>
    /// <param name="existingName">The name of the registration that will follow the inserted transform.</param>
    /// <param name="transform">The transform to insert.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder InsertTransformBefore(string existingName, IMarkdownTransform transform)
    {
        if (transform == null)
        {
            throw new ArgumentNullException("transform");
        }

        TransformRules.InsertBefore(existingName, transform.GetName(), transform);
        return this;
    }

    /// <summary>
    /// Inserts a transform immediately after an existing named registration.
    /// </summary>
    /// <param name="existingName">The name of the registration that will precede the inserted transform.</param>
    /// <param name="transform">The transform to insert.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder InsertTransformAfter(string existingName, IMarkdownTransform transform)
    {
        if (transform == null)
        {
            throw new ArgumentNullException("transform");
        }

        TransformRules.InsertAfter(existingName, transform.GetName(), transform);
        return this;
    }

    /// <summary>
    /// Replaces a named transform registration without changing its position.
    /// </summary>
    /// <param name="existingName">The name of the registration to replace.</param>
    /// <param name="transform">The replacement transform, registered under the name it returns.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder ReplaceTransform(string existingName, IMarkdownTransform transform)
    {
        if (transform == null)
        {
            throw new ArgumentNullException("transform");
        }

        TransformRules.Replace(existingName, transform.GetName(), transform);
        return this;
    }

    /// <summary>
    /// Sets transform.
    /// </summary>
    /// <param name="transform">The transform to register or replace.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    internal MarkdownPipelineBuilder SetTransform(IMarkdownTransform transform)
    {
        if (transform == null)
        {
            throw new ArgumentNullException("transform");
        }

        TransformRules.Set(transform.GetName(), transform);
        return this;
    }

    /// <summary>
    /// Removes transform.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <returns>The resulting <see cref="MarkdownPipelineBuilder"/> value.</returns>
    public MarkdownPipelineBuilder RemoveTransform(string name)
    {
        TransformRules.Remove(name);
        return this;
    }

    /// <summary>
    /// Clears transforms.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder ClearTransforms()
    {
        TransformRules.Clear();
        return this;
    }

    /// <summary>
    /// Adds HTML render rule.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <param name="rule">The HTML rendering rule to register.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    internal MarkdownPipelineBuilder AddNamedHtmlRenderRule(string name, IHtmlRenderRule rule)
    {
        HtmlRenderRules.Add(name, rule);
        return this;
    }

    /// <summary>
    /// Adds an HTML rendering rule using the stable name returned by the rule.
    /// </summary>
    /// <param name="rule">The HTML rendering rule to register.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder AddHtmlRenderRule(IHtmlRenderRule rule)
    {
        if (rule == null)
        {
            throw new ArgumentNullException("rule");
        }

        return AddNamedHtmlRenderRule(rule.GetName(), rule);
    }

    /// <summary>
    /// Inserts an HTML rendering rule immediately before an existing named registration.
    /// </summary>
    /// <param name="existingName">The name of the registration that will follow the inserted rule.</param>
    /// <param name="rule">The HTML rendering rule to insert.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder InsertHtmlRenderRuleBefore(string existingName, IHtmlRenderRule rule)
    {
        if (rule == null)
        {
            throw new ArgumentNullException("rule");
        }

        HtmlRenderRules.InsertBefore(existingName, rule.GetName(), rule);
        return this;
    }

    /// <summary>
    /// Inserts an HTML rendering rule immediately after an existing named registration.
    /// </summary>
    /// <param name="existingName">The name of the registration that will precede the inserted rule.</param>
    /// <param name="rule">The HTML rendering rule to insert.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder InsertHtmlRenderRuleAfter(string existingName, IHtmlRenderRule rule)
    {
        if (rule == null)
        {
            throw new ArgumentNullException("rule");
        }

        HtmlRenderRules.InsertAfter(existingName, rule.GetName(), rule);
        return this;
    }

    /// <summary>
    /// Replaces a named HTML rendering-rule registration without changing its position.
    /// </summary>
    /// <param name="existingName">The name of the registration to replace.</param>
    /// <param name="rule">The replacement HTML rendering rule, registered under the name it returns.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder ReplaceHtmlRenderRule(string existingName, IHtmlRenderRule rule)
    {
        if (rule == null)
        {
            throw new ArgumentNullException("rule");
        }

        HtmlRenderRules.Replace(existingName, rule.GetName(), rule);
        return this;
    }

    /// <summary>
    /// Removes a named HTML rendering-rule registration when present.
    /// </summary>
    /// <param name="name">The registration name.</param>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder RemoveHtmlRenderRule(string name)
    {
        HtmlRenderRules.Remove(name);
        return this;
    }

    /// <summary>
    /// Removes every HTML rendering-rule registration.
    /// </summary>
    /// <returns>This builder, so calls can be chained.</returns>
    public MarkdownPipelineBuilder ClearHtmlRenderRules()
    {
        HtmlRenderRules.Clear();
        return this;
    }
}
