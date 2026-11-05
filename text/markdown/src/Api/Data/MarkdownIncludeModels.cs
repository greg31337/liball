namespace markdown;

/// <summary>
/// Describes an include request relative to the source containing the directive.
/// </summary>
public class MarkdownIncludeContext
{
    /// <summary>
    /// The name written in the include directive.
    /// </summary>
    public string RequestedName;
    /// <summary>
    /// The canonical name of the source containing the directive, or an empty string for an unnamed root source.
    /// </summary>
    public string RelativeToName;
    /// <summary>
    /// The one-based nesting depth of the requested include.
    /// </summary>
    public int Depth;

    /// <summary>
    /// Initializes an empty include request context.
    /// </summary>
    public MarkdownIncludeContext()
    {
        RequestedName = "";
        RelativeToName = "";
        Depth = 1;
    }

    internal static MarkdownIncludeContext Create(string requestedName, string relativeToName, int depth)
    {
        MarkdownIncludeContext context = new MarkdownIncludeContext();
        context.RequestedName = requestedName;
        context.RelativeToName = relativeToName;
        context.Depth = depth;
        return context;
    }
}

/// <summary>
/// Reports whether an include was resolved and supplies its canonical source identity and content.
/// </summary>
public class MarkdownIncludeResult
{
    /// <summary>
    /// Indicates whether the requested source was found.
    /// </summary>
    public bool Found;
    /// <summary>
    /// The canonical source name used for nested relative requests, attribution, and cycle detection.
    /// </summary>
    public string SourceName;
    /// <summary>
    /// The Markdown content of the resolved source.
    /// </summary>
    public string Content;

    /// <summary>
    /// Initializes a not-found include result.
    /// </summary>
    public MarkdownIncludeResult()
    {
        Found = false;
        SourceName = "";
        Content = "";
    }
}

/// <summary>
/// Maps a region of expanded Markdown back to the source region that supplied it.
/// </summary>
public class MarkdownSourceAttribution
{
    /// <summary>
    /// The canonical source name, or an empty string for an unnamed root source.
    /// </summary>
    public string SourceName;
    /// <summary>
    /// The span occupied by the content in the expanded document source.
    /// </summary>
    public MarkdownSourceSpan ExpandedSpan;
    /// <summary>
    /// The span occupied by the content in its original source.
    /// </summary>
    public MarkdownSourceSpan OriginalSpan;
    /// <summary>
    /// The include nesting depth, where zero identifies root-source content.
    /// </summary>
    public int IncludeDepth;

    /// <summary>
    /// Initializes an empty source attribution.
    /// </summary>
    public MarkdownSourceAttribution()
    {
        SourceName = "";
        ExpandedSpan = new MarkdownSourceSpan();
        OriginalSpan = new MarkdownSourceSpan();
        IncludeDepth = 0;
    }

    internal static MarkdownSourceAttribution Create(
        string sourceName,
        MarkdownSourceSpan expandedSpan,
        MarkdownSourceSpan originalSpan,
        int includeDepth)
    {
        MarkdownSourceAttribution attribution = new MarkdownSourceAttribution();
        attribution.SourceName = sourceName;
        attribution.ExpandedSpan = expandedSpan.Copy();
        attribution.OriginalSpan = originalSpan.Copy();
        attribution.IncludeDepth = includeDepth;
        return attribution;
    }

    internal MarkdownSourceAttribution Copy()
    {
        return Create(SourceName, ExpandedSpan, OriginalSpan, IncludeDepth);
    }
}
