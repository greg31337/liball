namespace svg;

/// <summary>Reports one explicit computed-style cache invalidation.</summary>
public class SvgStyleInvalidationResult
{
    /// <summary>The invalidated dependency kind.</summary>
    public SvgStyleInvalidationKind Kind;
    /// <summary>The number of cache entries removed.</summary>
    public int RemovedEntries;

    internal SvgStyleInvalidationResult(SvgStyleInvalidationKind kind, int removedEntries)
    {
        Kind = kind;
        RemovedEntries = removedEntries;
    }
}

/// <summary>Contains a snapshot of computed-style cache activity.</summary>
public class SvgStyleResolverStatistics
{
    /// <summary>The current cache entry count.</summary>
    public int CacheEntries;
    /// <summary>The cumulative valid cache hit count.</summary>
    public long CacheHits;
    /// <summary>The cumulative cache miss count.</summary>
    public long CacheMisses;
    /// <summary>The cumulative explicitly invalidated entry count.</summary>
    public long InvalidatedEntries;
    /// <summary>The cumulative capacity eviction count.</summary>
    public long EvictedEntries;

    internal SvgStyleResolverStatistics(int entries, long hits, long misses, long invalidated, long evicted)
    {
        CacheEntries = entries;
        CacheHits = hits;
        CacheMisses = misses;
        InvalidatedEntries = invalidated;
        EvictedEntries = evicted;
    }
}

/// <summary>Configures computed-style resolution and its bounded cache.</summary>
public class SvgStyleResolverOptions
{
    /// <summary>The property metadata catalog.</summary>
    public SvgPropertyCatalog Properties;
    /// <summary>The CSS parser and cascade limits.</summary>
    public SvgCssLimits Limits;
    /// <summary>The selector scope, namespaces, interaction state, and work limit.</summary>
    public SvgSelectorMatchOptions SelectorOptions;
    /// <summary>The viewport and media environment.</summary>
    public SvgStyleEnvironment StyleEnvironment;
    /// <summary>Whether required SVG user-agent defaults participate.</summary>
    public bool IncludeUserAgentDefaults;
    /// <summary>The maximum retained computed styles.</summary>
    public int MaxCacheEntries;
    /// <summary>The maximum parsed paint fallback count.</summary>
    public int MaxPaintFallbacks;
    /// <summary>The caller-managed font database revision.</summary>
    public long FontVersion;

    /// <summary>Creates secure deterministic resolver defaults.</summary>
    public SvgStyleResolverOptions()
    {
        Properties = SvgPropertyCatalog.CreateDefault();
        Limits = new SvgCssLimits();
        SelectorOptions = new SvgSelectorMatchOptions();
        StyleEnvironment = new SvgStyleEnvironment();
        IncludeUserAgentDefaults = true;
        MaxCacheEntries = 4096;
        MaxPaintFallbacks = 16;
        FontVersion = 0;
    }
}

/// <summary>Contains computed styles for one document in element preorder.</summary>
public class SvgComputedStyleTree
{
    /// <summary>The source document.</summary>
    public SvgDocument? Document;
    /// <summary>The document version at resolution time.</summary>
    public long SourceVersion;
    /// <summary>The computed element styles in preorder.</summary>
    public System.Collections.Generic.List<SvgComputedStyle> Styles;
    /// <summary>The collected style diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>Whether every element style resolved successfully.</summary>
    public bool Success;

    internal SvgComputedStyleTree(SvgDocument? document)
    {
        Document = document;
        SourceVersion = document == null ? 0 : document.Version;
        Styles = new System.Collections.Generic.List<SvgComputedStyle>();
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Success = false;
    }

    /// <summary>Finds the computed style for an element identity.</summary>
    /// <param name="node">The source element.</param>
    /// <returns>The computed style, or <c>null</c>.</returns>
    internal SvgComputedStyle? Find(SvgNode node)
    {
        int index = 0;
        while (index < Styles.Count)
        {
            if (object.ReferenceEquals(Styles[index].Node, node)) return Styles[index];
            index = index + 1;
        }
        return null;
    }
}
