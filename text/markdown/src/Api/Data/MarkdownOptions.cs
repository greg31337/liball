using System;

namespace markdown;

/// <summary>
/// Stores parser and renderer settings that can be copied into a pipeline.
/// </summary>
public class MarkdownOptions
{
    /// <summary>
    /// The syntax features enabled during parsing and rendering.
    /// </summary>
    public MarkdownFeatureSet Features;
    /// <summary>
    /// The maximum AST nesting depth. Inputs beyond this depth produce a diagnostic and are recovered safely.
    /// </summary>
    public int MaximumNestingDepth;
    /// <summary>
    /// The maximum accepted input length in characters.
    /// </summary>
    public int MaximumInputSize;
    /// <summary>
    /// The maximum number of link and footnote definitions retained during parsing.
    /// </summary>
    public int MaximumReferenceDefinitions;
    /// <summary>
    /// The maximum number of characters a renderer may emit.
    /// </summary>
    public int MaximumOutputSize;
    /// <summary>
    /// The preferred maximum line width for normalized Markdown paragraphs, or zero to disable wrapping.
    /// </summary>
    public int MaximumLineWidth;
    /// <summary>
    /// The maximum permitted include nesting depth.
    /// </summary>
    public int MaximumIncludeDepth;
    /// <summary>
    /// Indicates whether original source spelling and whitespace are retained for round-trip rendering.
    /// </summary>
    public bool TrackTrivia;
    /// <summary>
    /// Indicates whether raw HTML nodes may be emitted instead of suppressed.
    /// </summary>
    public bool AllowRawHtml;
    /// <summary>
    /// Indicates whether raw HTML bypasses the safe rendering restrictions.
    /// </summary>
    public bool AllowUnsafeHtml;
    /// <summary>
    /// Indicates whether destinations with unsafe URI schemes may be emitted.
    /// </summary>
    public bool AllowUnsafeLinks;
    /// <summary>
    /// Indicates whether empty HTML elements use XHTML-style closing syntax.
    /// </summary>
    public bool Xhtml;
    /// <summary>
    /// Indicates whether every soft source break renders as a hard line break.
    /// </summary>
    public bool HardWrap;
    /// <summary>
    /// Indicates whether rendering avoids culture-sensitive and environment-dependent output.
    /// </summary>
    public bool DeterministicOutput;
    /// <summary>
    /// Controls how soft line breaks are rendered.
    /// </summary>
    public MarkdownSoftBreakMode SoftBreakMode;
    /// <summary>
    /// The line-ending sequence emitted by renderers and generators.
    /// </summary>
    public string LineEnding;
    /// <summary>
    /// Indicates whether standalone <c>!INCLUDE "name"</c> directives are expanded before parsing.
    /// </summary>
    public bool ProcessIncludes;
    /// <summary>
    /// The canonical name assigned to the root source for relative resolution and attribution.
    /// </summary>
    public string IncludeRootName;
    /// <summary>
    /// The caller-supplied resolver used when include processing is enabled.
    /// </summary>
    public IMarkdownIncludeResolver? IncludeResolver;
    /// <summary>The caller URI policy used for links, or <c>null</c> for safe defaults.</summary>
    public IMarkdownUriPolicy? LinkUriPolicy;
    /// <summary>The caller URI policy used for images, or <c>null</c> for safe defaults.</summary>
    public IMarkdownUriPolicy? ImageUriPolicy;
    /// <summary>The render-time link destination rewriter, or <c>null</c>.</summary>
    public IMarkdownDestinationRewriter? LinkUriRewriter;
    /// <summary>The render-time image destination rewriter, or <c>null</c>.</summary>
    public IMarkdownDestinationRewriter? ImageUriRewriter;
    /// <summary>The caller raw-HTML decision policy, or <c>null</c> for built-in behavior.</summary>
    public IMarkdownRawHtmlPolicy? RawHtmlPolicy;
    internal MarkdownEmojiMap EmojiShortcodes;
    internal MarkdownCustomContainerMap CustomContainers;
    internal MarkdownCustomFencedBlockMap CustomFencedBlocks;
    internal MarkdownCustomInlineDelimiterMap CustomInlineDelimiters;

    /// <summary>
    /// Initializes a new <see cref="MarkdownOptions"/> instance.
    /// </summary>
    public MarkdownOptions()
    {
        Features = MarkdownFeatureSet.CommonMarkPreset();
        MaximumNestingDepth = 100;
        MaximumInputSize = 10485760;
        MaximumReferenceDefinitions = 10000;
        MaximumOutputSize = 52428800;
        MaximumLineWidth = 0;
        MaximumIncludeDepth = 16;
        TrackTrivia = false;
        AllowRawHtml = false;
        AllowUnsafeHtml = false;
        AllowUnsafeLinks = false;
        Xhtml = false;
        HardWrap = false;
        DeterministicOutput = true;
        SoftBreakMode = MarkdownSoftBreakMode.Preserve;
        LineEnding = "\n";
        ProcessIncludes = false;
        IncludeRootName = "";
        IncludeResolver = null;
        LinkUriPolicy = null;
        ImageUriPolicy = null;
        LinkUriRewriter = null;
        ImageUriRewriter = null;
        RawHtmlPolicy = null;
        EmojiShortcodes = new MarkdownEmojiMap();
        CustomContainers = new MarkdownCustomContainerMap();
        CustomFencedBlocks = new MarkdownCustomFencedBlockMap();
        CustomInlineDelimiters = new MarkdownCustomInlineDelimiterMap();
    }

    /// <summary>
    /// Creates a pipeline configured for CommonMark syntax and safe rendering defaults.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownOptions"/> value.</returns>
    internal static MarkdownOptions CommonMarkPreset()
    {
        MarkdownOptions options = new MarkdownOptions();
        options.Features = MarkdownFeatureSet.CommonMarkPreset();
        return options;
    }

    /// <summary>
    /// Creates a pipeline configured for CommonMark plus GitHub Flavored Markdown extensions.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownOptions"/> value.</returns>
    internal static MarkdownOptions GfmPreset()
    {
        MarkdownOptions options = new MarkdownOptions();
        options.Features = MarkdownFeatureSet.GfmPreset();
        return options;
    }

    /// <summary>
    /// Creates a pipeline configured with all built-in Markdown extensions.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownOptions"/> value.</returns>
    internal static MarkdownOptions AdvancedPreset()
    {
        MarkdownOptions options = new MarkdownOptions();
        options.Features = MarkdownFeatureSet.AdvancedPreset();
        return options;
    }

    /// <summary>
    /// Creates a pipeline with all syntax features disabled for caller-defined configuration.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownOptions"/> value.</returns>
    internal static MarkdownOptions ZeroPreset()
    {
        MarkdownOptions options = new MarkdownOptions();
        options.Features = MarkdownFeatureSet.ZeroPreset();
        return options;
    }

    /// <summary>
    /// Creates a detached copy of the value.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownOptions"/> value.</returns>
    internal MarkdownOptions Copy()
    {
        Validate();
        MarkdownOptions copy = new MarkdownOptions();
        copy.Features = Features.Copy();
        copy.MaximumNestingDepth = MaximumNestingDepth;
        copy.MaximumInputSize = MaximumInputSize;
        copy.MaximumReferenceDefinitions = MaximumReferenceDefinitions;
        copy.MaximumOutputSize = MaximumOutputSize;
        copy.MaximumLineWidth = MaximumLineWidth;
        copy.MaximumIncludeDepth = MaximumIncludeDepth;
        copy.TrackTrivia = TrackTrivia;
        copy.AllowRawHtml = AllowRawHtml;
        copy.AllowUnsafeHtml = AllowUnsafeHtml;
        copy.AllowUnsafeLinks = AllowUnsafeLinks;
        copy.Xhtml = Xhtml;
        copy.HardWrap = HardWrap;
        copy.DeterministicOutput = DeterministicOutput;
        copy.SoftBreakMode = SoftBreakMode;
        copy.LineEnding = LineEnding;
        copy.ProcessIncludes = ProcessIncludes;
        copy.IncludeRootName = IncludeRootName;
        copy.IncludeResolver = IncludeResolver;
        copy.LinkUriPolicy = LinkUriPolicy;
        copy.ImageUriPolicy = ImageUriPolicy;
        copy.LinkUriRewriter = LinkUriRewriter;
        copy.ImageUriRewriter = ImageUriRewriter;
        copy.RawHtmlPolicy = RawHtmlPolicy;
        copy.EmojiShortcodes = EmojiShortcodes.Copy();
        copy.CustomContainers = CustomContainers.Copy();
        copy.CustomFencedBlocks = CustomFencedBlocks.Copy();
        copy.CustomInlineDelimiters = CustomInlineDelimiters.Copy();
        return copy;
    }

    internal void Validate()
    {
        if (Features == null)
        {
            throw new ArgumentNullException("Features");
        }

        ValidatePositiveLimit(MaximumNestingDepth, "MaximumNestingDepth");
        ValidatePositiveLimit(MaximumInputSize, "MaximumInputSize");
        ValidatePositiveLimit(MaximumReferenceDefinitions, "MaximumReferenceDefinitions");
        ValidatePositiveLimit(MaximumOutputSize, "MaximumOutputSize");
        if (MaximumLineWidth < 0)
        {
            throw new ArgumentOutOfRangeException("MaximumLineWidth");
        }

        ValidatePositiveLimit(MaximumIncludeDepth, "MaximumIncludeDepth");
        SetSoftBreakMode(SoftBreakMode);
        SetLineEnding(LineEnding);
        if (IncludeRootName == null)
        {
            throw new ArgumentNullException("IncludeRootName");
        }
    }

    /// <summary>
    /// Sets feature set.
    /// </summary>
    /// <param name="features">The feature set to copy.</param>
    internal void SetFeatureSet(MarkdownFeatureSet features)
    {
        if (features == null)
        {
            throw new ArgumentNullException("features");
        }

        Features = features.Copy();
    }

    /// <summary>
    /// Sets maximum nesting depth.
    /// </summary>
    /// <param name="value">The value to apply.</param>
    internal void SetMaximumNestingDepth(int value)
    {
        ValidatePositiveLimit(value, "value");
        MaximumNestingDepth = value;
    }

    /// <summary>
    /// Sets maximum input size.
    /// </summary>
    /// <param name="value">The value to apply.</param>
    internal void SetMaximumInputSize(int value)
    {
        ValidatePositiveLimit(value, "value");
        MaximumInputSize = value;
    }

    /// <summary>
    /// Sets maximum reference definitions.
    /// </summary>
    /// <param name="value">The value to apply.</param>
    internal void SetMaximumReferenceDefinitions(int value)
    {
        ValidatePositiveLimit(value, "value");
        MaximumReferenceDefinitions = value;
    }

    /// <summary>
    /// Sets maximum output size.
    /// </summary>
    /// <param name="value">The value to apply.</param>
    internal void SetMaximumOutputSize(int value)
    {
        ValidatePositiveLimit(value, "value");
        MaximumOutputSize = value;
    }

    /// <summary>
    /// Sets the normalized Markdown paragraph wrapping width.
    /// </summary>
    /// <param name="value">Zero to disable wrapping, or a positive line width.</param>
    internal void SetMaximumLineWidth(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        MaximumLineWidth = value;
    }

    /// <summary>
    /// Sets maximum include depth.
    /// </summary>
    /// <param name="value">The value to apply.</param>
    internal void SetMaximumIncludeDepth(int value)
    {
        ValidatePositiveLimit(value, "value");
        MaximumIncludeDepth = value;
    }

    internal void SetSoftBreakMode(MarkdownSoftBreakMode mode)
    {
        if (mode != MarkdownSoftBreakMode.Preserve
            && mode != MarkdownSoftBreakMode.Space
            && mode != MarkdownSoftBreakMode.HardBreak
            && mode != MarkdownSoftBreakMode.Cjk)
        {
            throw new ArgumentOutOfRangeException("mode");
        }

        SoftBreakMode = mode;
    }

    /// <summary>
    /// Sets line ending.
    /// </summary>
    /// <param name="lineEnding">The line-ending sequence to emit; use <c>\n</c> or <c>\r\n</c>.</param>
    internal void SetLineEnding(string lineEnding)
    {
        if (lineEnding == null)
        {
            throw new ArgumentNullException("lineEnding");
        }

        if (lineEnding != "\n" && lineEnding != "\r\n" && lineEnding != "\r")
        {
            throw new ArgumentException("Line ending must be LF, CRLF, or CR.");
        }

        LineEnding = lineEnding;
    }

    internal static void ValidatePositiveLimit(int value, string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }
}
