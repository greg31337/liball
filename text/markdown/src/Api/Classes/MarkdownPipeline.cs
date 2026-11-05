using System;

namespace markdown;

/// <summary>
/// Represents a reusable snapshot of parser, transformation, rendering, safety, and resource-limit settings.
/// </summary>
/// <remarks>
/// A pipeline copies its options and rule lists when it is created. Query methods also return copies, so a built pipeline can be reused safely and is not changed by later builder edits.
/// </remarks>
public class MarkdownPipeline : IMarkdownPipeline
{
    internal MarkdownOptions Options;
    internal MarkdownRuleList BlockRules;
    internal MarkdownRuleList InlineRules;
    internal MarkdownRuleList TransformRules;
    internal MarkdownRuleList HtmlRenderRules;

    /// <summary>
    /// Initializes a new <see cref="MarkdownPipeline"/> instance.
    /// </summary>
    public MarkdownPipeline()
    {
        Options = MarkdownOptions.CommonMarkPreset();
        BlockRules = new MarkdownRuleList();
        InlineRules = new MarkdownRuleList();
        TransformRules = new MarkdownRuleList();
        HtmlRenderRules = new MarkdownRuleList();
        CommonMarkBlockRules.RegisterCommonMarkBlockRules(BlockRules);
        CommonMarkInlineRules.RegisterCommonMarkInlineRules(InlineRules);
    }

    /// <summary>
    /// Creates a pipeline from copied options.
    /// </summary>
    /// <param name="options">The options to copy.</param>
    internal static MarkdownPipeline Create(MarkdownOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        MarkdownPipeline pipeline = new MarkdownPipeline();
        pipeline.Options = options.Copy();
        pipeline.BlockRules = new MarkdownRuleList();
        pipeline.InlineRules = new MarkdownRuleList();
        pipeline.TransformRules = new MarkdownRuleList();
        pipeline.HtmlRenderRules = new MarkdownRuleList();
        if (pipeline.Options.Features.CommonMark)
        {
            CommonMarkBlockRules.RegisterCommonMarkBlockRules(pipeline.BlockRules);
            CommonMarkInlineRules.RegisterCommonMarkInlineRules(pipeline.InlineRules);
        }

        if (pipeline.Options.Features.GfmTables)
        {
            GfmBlockRules.RegisterGfmBlockRules(pipeline.BlockRules);
        }

        if (pipeline.Options.Features.GfmStrikethrough)
        {
            GfmInlineRules.RegisterGfmStrikethroughRule(pipeline.InlineRules);
        }

        if (pipeline.Options.Features.GfmBareAutolinks)
        {
            GfmInlineRules.RegisterGfmBareAutolinkRule(pipeline.InlineRules);
        }

        if (pipeline.Options.Features.Footnotes)
        {
            FootnoteBlockRules.RegisterFootnoteBlockRules(pipeline.BlockRules);
            FootnoteInlineRules.RegisterFootnoteInlineRules(pipeline.InlineRules);
        }

        if (pipeline.Options.Features.DefinitionLists)
        {
            DefinitionListBlockRules.RegisterDefinitionListBlockRules(pipeline.BlockRules);
        }

        if (pipeline.Options.Features.FrontMatter)
        {
            FrontMatterBlockRules.RegisterFrontMatterBlockRules(pipeline.BlockRules);
        }

        if (pipeline.Options.Features.Abbreviations)
        {
            AbbreviationBlockRules.RegisterAbbreviationBlockRules(pipeline.BlockRules);
        }

        if (pipeline.Options.Features.Attributes)
        {
            GenericAttributeRules.RegisterBlockRule(pipeline.BlockRules);
            GenericAttributeRules.RegisterInlineRule(pipeline.InlineRules);
        }

        if (pipeline.Options.Features.Superscript
            || pipeline.Options.Features.Subscript
            || pipeline.Options.Features.InsertedText
            || pipeline.Options.Features.MarkedText)
        {
            TextFormattingInlineRules.RegisterTextFormattingInlineRules(pipeline.InlineRules);
        }

        if (pipeline.Options.Features.Math)
        {
            MathBlockRules.RegisterMathBlockRules(pipeline.BlockRules);
            MathInlineRules.RegisterMathInlineRules(pipeline.InlineRules);
        }

        if (pipeline.Options.Features.Diagrams)
        {
            DiagramBlockRules.RegisterDiagramBlockRules(pipeline.BlockRules);
        }

        if (pipeline.Options.Features.CustomFencedBlocks)
        {
            CustomFencedBlockRules.RegisterCustomFencedBlockRules(pipeline.BlockRules);
        }

        if (pipeline.Options.Features.Emoji)
        {
            EmojiInlineRules.RegisterEmojiInlineRules(pipeline.InlineRules);
        }

        if (pipeline.Options.Features.CustomInlineDelimiters)
        {
            CustomInlineDelimiterRules.RegisterCustomInlineDelimiterRules(pipeline.InlineRules);
        }

        if (pipeline.Options.Features.Admonitions)
        {
            AdmonitionBlockRules.RegisterAdmonitionBlockRules(pipeline.BlockRules);
        }

        if (pipeline.Options.Features.CustomContainers)
        {
            CustomContainerBlockRules.RegisterCustomContainerBlockRules(pipeline.BlockRules);
        }

        if (pipeline.Options.Features.Details)
        {
            DetailsBlockRules.RegisterDetailsBlockRules(pipeline.BlockRules);
        }

        return pipeline;
    }

    internal static MarkdownPipeline Create(
        MarkdownOptions options,
        MarkdownRuleList blockRules,
        MarkdownRuleList inlineRules,
        MarkdownRuleList transformRules,
        MarkdownRuleList htmlRenderRules)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (blockRules == null)
        {
            throw new ArgumentNullException("blockRules");
        }

        if (inlineRules == null)
        {
            throw new ArgumentNullException("inlineRules");
        }

        if (transformRules == null)
        {
            throw new ArgumentNullException("transformRules");
        }

        if (htmlRenderRules == null)
        {
            throw new ArgumentNullException("htmlRenderRules");
        }

        MarkdownPipeline pipeline = new MarkdownPipeline();
        pipeline.Options = options.Copy();
        pipeline.BlockRules = blockRules.Copy();
        pipeline.InlineRules = inlineRules.Copy();
        pipeline.TransformRules = transformRules.Copy();
        pipeline.HtmlRenderRules = htmlRenderRules.Copy();
        return pipeline;
    }

    /// <summary>
    /// Creates a pipeline configured for CommonMark syntax and safe rendering defaults.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownPipeline"/> value.</returns>
    internal static MarkdownPipeline CommonMarkPreset()
    {
        return Create(MarkdownOptions.CommonMarkPreset());
    }

    /// <summary>
    /// Creates a pipeline configured for CommonMark plus GitHub Flavored Markdown extensions.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownPipeline"/> value.</returns>
    internal static MarkdownPipeline GfmPreset()
    {
        return Create(MarkdownOptions.GfmPreset());
    }

    /// <summary>
    /// Creates a pipeline configured with all built-in Markdown extensions.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownPipeline"/> value.</returns>
    internal static MarkdownPipeline AdvancedPreset()
    {
        return MarkdownPipeline.Create(MarkdownOptions.AdvancedPreset());
    }

    /// <summary>
    /// Creates a pipeline with all syntax features disabled for caller-defined configuration.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownPipeline"/> value.</returns>
    internal static MarkdownPipeline ZeroPreset()
    {
        return MarkdownPipeline.Create(MarkdownOptions.ZeroPreset());
    }

    /// <summary>
    /// Creates an independent pipeline with the same options and registered rule objects.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownPipeline"/> value.</returns>
    internal MarkdownPipeline Copy()
    {
        return MarkdownPipeline.Create(Options, BlockRules, InlineRules, TransformRules, HtmlRenderRules);
    }

    /// <summary>
    /// Returns a detached copy of the effective options.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownOptions"/> value.</returns>
    public MarkdownOptions GetOptions()
    {
        return Options.Copy();
    }

    /// <summary>
    /// Returns a detached copy of the enabled feature set.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownFeatureSet"/> value.</returns>
    public MarkdownFeatureSet GetFeatures()
    {
        return Options.Features.Copy();
    }

    /// <summary>
    /// Returns a detached copy of the ordered block-rule registrations.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownRuleList"/> value.</returns>
    internal MarkdownRuleList GetBlockRules()
    {
        return BlockRules.Copy();
    }

    /// <summary>
    /// Returns a detached copy of the ordered inline-rule registrations.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownRuleList"/> value.</returns>
    internal MarkdownRuleList GetInlineRules()
    {
        return InlineRules.Copy();
    }

    /// <summary>
    /// Returns a detached copy of the ordered transform registrations.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownRuleList"/> value.</returns>
    internal MarkdownRuleList GetTransformRules()
    {
        return TransformRules.Copy();
    }

    /// <summary>
    /// Returns a detached copy of the ordered HTML rendering-rule registrations.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownRuleList"/> value.</returns>
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
    /// Reports whether CommonMark parsing is enabled.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool UsesCommonMark()
    {
        return Options.Features.CommonMark;
    }

    /// <summary>
    /// Reports whether every GFM feature is enabled.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool UsesGfm()
    {
        return Options.Features.GfmTables
            && Options.Features.GfmTaskLists
            && Options.Features.GfmStrikethrough
            && Options.Features.GfmBareAutolinks
            && Options.Features.GfmDisallowedRawHtml;
    }

    /// <summary>
    /// Reports whether every advanced extension is enabled.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool UsesAdvancedExtensions()
    {
        return Options.Features.Footnotes
            && Options.Features.DefinitionLists
            && Options.Features.HeadingIds
            && Options.Features.Attributes
            && Options.Features.FrontMatter
            && Options.Features.Abbreviations
            && Options.Features.Superscript
            && Options.Features.Subscript
            && Options.Features.InsertedText
            && Options.Features.MarkedText
            && Options.Features.Typography
            && Options.Features.Math
            && Options.Features.Diagrams
            && Options.Features.CjkSoftBreaks
            && Options.Features.Emoji
            && Options.Features.Admonitions
            && Options.Features.CustomContainers
            && Options.Features.CustomFencedBlocks
            && Options.Features.CustomInlineDelimiters
            && Options.Features.Details;
    }

    /// <summary>
    /// Reports whether raw HTML may be emitted.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool AllowsRawHtml()
    {
        return Options.AllowRawHtml;
    }

    /// <summary>
    /// Reports whether raw HTML may bypass safety filtering.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool AllowsUnsafeHtml()
    {
        return Options.AllowUnsafeHtml;
    }

    /// <summary>
    /// Reports whether unsafe URI schemes may be emitted.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool AllowsUnsafeLinks()
    {
        return Options.AllowUnsafeLinks;
    }

    /// <summary>
    /// Reports whether original source trivia is retained.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool TracksTrivia()
    {
        return Options.TrackTrivia;
    }
}
