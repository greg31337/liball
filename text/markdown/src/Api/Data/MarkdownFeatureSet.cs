namespace markdown;

/// <summary>
/// Stores the syntax features enabled for a pipeline.
/// </summary>
public class MarkdownFeatureSet
{
    /// <summary>
    /// Indicates whether CommonMark syntax is enabled.
    /// </summary>
    public bool CommonMark;
    /// <summary>
    /// Indicates whether GFM tables syntax is enabled.
    /// </summary>
    public bool GfmTables;
    /// <summary>
    /// Indicates whether GFM task lists syntax is enabled.
    /// </summary>
    public bool GfmTaskLists;
    /// <summary>
    /// Indicates whether GFM strikethrough syntax is enabled.
    /// </summary>
    public bool GfmStrikethrough;
    /// <summary>
    /// Indicates whether GFM bare autolinks syntax is enabled.
    /// </summary>
    public bool GfmBareAutolinks;
    /// <summary>
    /// Indicates whether GFM disallowed raw HTML syntax is enabled.
    /// </summary>
    public bool GfmDisallowedRawHtml;
    /// <summary>
    /// Indicates whether footnotes syntax is enabled.
    /// </summary>
    public bool Footnotes;
    /// <summary>
    /// Indicates whether definition lists syntax is enabled.
    /// </summary>
    public bool DefinitionLists;
    /// <summary>
    /// Indicates whether heading IDs syntax is enabled.
    /// </summary>
    public bool HeadingIds;
    /// <summary>
    /// Indicates whether attributes syntax is enabled.
    /// </summary>
    public bool Attributes;
    /// <summary>
    /// Indicates whether front matter syntax is enabled.
    /// </summary>
    public bool FrontMatter;
    /// <summary>
    /// Indicates whether abbreviations syntax is enabled.
    /// </summary>
    public bool Abbreviations;
    /// <summary>
    /// Indicates whether superscript syntax is enabled.
    /// </summary>
    public bool Superscript;
    /// <summary>
    /// Indicates whether subscript syntax is enabled.
    /// </summary>
    public bool Subscript;
    /// <summary>
    /// Indicates whether inserted text syntax is enabled.
    /// </summary>
    public bool InsertedText;
    /// <summary>
    /// Indicates whether marked text syntax is enabled.
    /// </summary>
    public bool MarkedText;
    /// <summary>
    /// Indicates whether typography syntax is enabled.
    /// </summary>
    public bool Typography;
    /// <summary>
    /// Indicates whether math syntax is enabled.
    /// </summary>
    public bool Math;
    /// <summary>
    /// Indicates whether diagrams syntax is enabled.
    /// </summary>
    public bool Diagrams;
    /// <summary>
    /// Indicates whether emoji syntax is enabled.
    /// </summary>
    public bool Emoji;
    /// <summary>
    /// Indicates whether CJK soft breaks syntax is enabled.
    /// </summary>
    public bool CjkSoftBreaks;
    /// <summary>
    /// Indicates whether admonitions syntax is enabled.
    /// </summary>
    public bool Admonitions;
    /// <summary>
    /// Indicates whether custom containers syntax is enabled.
    /// </summary>
    public bool CustomContainers;
    /// <summary>
    /// Indicates whether custom fenced blocks syntax is enabled.
    /// </summary>
    public bool CustomFencedBlocks;
    /// <summary>
    /// Indicates whether custom inline delimiters syntax is enabled.
    /// </summary>
    public bool CustomInlineDelimiters;
    /// <summary>
    /// Indicates whether description/details block syntax is enabled.
    /// </summary>
    public bool Details;

    /// <summary>
    /// Initializes a new <see cref="MarkdownFeatureSet"/> instance.
    /// </summary>
    public MarkdownFeatureSet()
    {
        DisableAll();
    }

    /// <summary>
    /// Creates a feature set with all syntax features disabled for caller-defined configuration.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownFeatureSet"/> value.</returns>
    internal static MarkdownFeatureSet ZeroPreset()
    {
        return new MarkdownFeatureSet();
    }

    /// <summary>
    /// Creates a feature set configured for CommonMark syntax and safe rendering defaults.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownFeatureSet"/> value.</returns>
    internal static MarkdownFeatureSet CommonMarkPreset()
    {
        MarkdownFeatureSet features = new MarkdownFeatureSet();
        features.EnableCommonMark();
        return features;
    }

    /// <summary>
    /// Creates a feature set configured for CommonMark plus GitHub Flavored Markdown extensions.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownFeatureSet"/> value.</returns>
    internal static MarkdownFeatureSet GfmPreset()
    {
        MarkdownFeatureSet features = CommonMarkPreset();
        features.EnableGfm();
        return features;
    }

    /// <summary>
    /// Creates a feature set configured with all built-in Markdown extensions.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownFeatureSet"/> value.</returns>
    internal static MarkdownFeatureSet AdvancedPreset()
    {
        MarkdownFeatureSet features = GfmPreset();
        features.EnableAdvancedExtensions();
        return features;
    }

    /// <summary>
    /// Disables every built-in syntax feature.
    /// </summary>
    internal void DisableAll()
    {
        CommonMark = false;
        GfmTables = false;
        GfmTaskLists = false;
        GfmStrikethrough = false;
        GfmBareAutolinks = false;
        GfmDisallowedRawHtml = false;
        Footnotes = false;
        DefinitionLists = false;
        HeadingIds = false;
        Attributes = false;
        FrontMatter = false;
        Abbreviations = false;
        Superscript = false;
        Subscript = false;
        InsertedText = false;
        MarkedText = false;
        Typography = false;
        Math = false;
        Diagrams = false;
        Emoji = false;
        CjkSoftBreaks = false;
        Admonitions = false;
        CustomContainers = false;
        CustomFencedBlocks = false;
        CustomInlineDelimiters = false;
        Details = false;
    }

    /// <summary>
    /// Enables the CommonMark baseline syntax.
    /// </summary>
    internal void EnableCommonMark()
    {
        CommonMark = true;
    }

    /// <summary>
    /// Enables CommonMark and all GitHub Flavored Markdown features.
    /// </summary>
    internal void EnableGfm()
    {
        EnableCommonMark();
        GfmTables = true;
        GfmTaskLists = true;
        GfmStrikethrough = true;
        GfmBareAutolinks = true;
        GfmDisallowedRawHtml = true;
    }

    /// <summary>
    /// Enables GFM and every additional built-in extension.
    /// </summary>
    internal void EnableAdvancedExtensions()
    {
        EnableGfm();
        Footnotes = true;
        DefinitionLists = true;
        HeadingIds = true;
        Attributes = true;
        FrontMatter = true;
        Abbreviations = true;
        Superscript = true;
        Subscript = true;
        InsertedText = true;
        MarkedText = true;
        Typography = true;
        Math = true;
        Diagrams = true;
        Emoji = true;
        CjkSoftBreaks = true;
        Admonitions = true;
        CustomContainers = true;
        CustomFencedBlocks = true;
        CustomInlineDelimiters = true;
        Details = true;
    }

    /// <summary>
    /// Creates a detached copy of the value.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownFeatureSet"/> value.</returns>
    internal MarkdownFeatureSet Copy()
    {
        MarkdownFeatureSet copy = new MarkdownFeatureSet();
        copy.CommonMark = CommonMark;
        copy.GfmTables = GfmTables;
        copy.GfmTaskLists = GfmTaskLists;
        copy.GfmStrikethrough = GfmStrikethrough;
        copy.GfmBareAutolinks = GfmBareAutolinks;
        copy.GfmDisallowedRawHtml = GfmDisallowedRawHtml;
        copy.Footnotes = Footnotes;
        copy.DefinitionLists = DefinitionLists;
        copy.HeadingIds = HeadingIds;
        copy.Attributes = Attributes;
        copy.FrontMatter = FrontMatter;
        copy.Abbreviations = Abbreviations;
        copy.Superscript = Superscript;
        copy.Subscript = Subscript;
        copy.InsertedText = InsertedText;
        copy.MarkedText = MarkedText;
        copy.Typography = Typography;
        copy.Math = Math;
        copy.Diagrams = Diagrams;
        copy.Emoji = Emoji;
        copy.CjkSoftBreaks = CjkSoftBreaks;
        copy.Admonitions = Admonitions;
        copy.CustomContainers = CustomContainers;
        copy.CustomFencedBlocks = CustomFencedBlocks;
        copy.CustomInlineDelimiters = CustomInlineDelimiters;
        copy.Details = Details;
        return copy;
    }
}
