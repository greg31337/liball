namespace svg;

/// <summary>Contains bounded resolved-document cache activity.</summary>
public class SvgResolvedDocumentCacheStatistics
{
    /// <summary>The current cache entry count.</summary>
    public int CacheEntries;
    /// <summary>The cumulative current-snapshot hit count.</summary>
    public long CacheHits;
    /// <summary>The cumulative cache miss count.</summary>
    public long CacheMisses;
    /// <summary>The cumulative stale or explicit invalidation count.</summary>
    public long InvalidatedEntries;
    /// <summary>The cumulative capacity eviction count.</summary>
    public long EvictedEntries;

    internal SvgResolvedDocumentCacheStatistics(int entries, long hits, long misses, long invalidated, long evicted)
    {
        CacheEntries = entries;
        CacheHits = hits;
        CacheMisses = misses;
        InvalidatedEntries = invalidated;
        EvictedEntries = evicted;
    }
}

/// <summary>Contains one version-owned computed snapshot of an editable SVG document.</summary>
public class SvgResolvedDocument
{
    /// <summary>The editable source document.</summary>
    public SvgDocument? SourceDocument;
    /// <summary>The editable document version captured during resolution.</summary>
    public long SourceVersion;
    /// <summary>The resolved document element, or <c>null</c> after an early failure.</summary>
    public SvgResolvedNode? Root;
    /// <summary>Every resolved authored and instance node in processing preorder.</summary>
    public System.Collections.Generic.List<SvgResolvedNode> Nodes;
    /// <summary>The collected embedded, imported, and caller stylesheets.</summary>
    public SvgStyleSheetCollection? StyleSheets;
    /// <summary>The computed authored-element style tree.</summary>
    public SvgComputedStyleTree? ComputedStyles;
    /// <summary>The normalized scene, which remains <c>null</c> until scene construction is requested.</summary>
    public SvgScene? Scene;
    /// <summary>The bounded deterministically ordered resolution diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The library feature report captured for this resolution.</summary>
    public SvgFeatureReport FeatureReport;
    /// <summary>The resource activity performed by resolution stages.</summary>
    public SvgResourceSummary Resources;
    /// <summary>Whether every required current stage completed without an error.</summary>
    public bool Success;
    internal SvgResolvedCacheKey? CacheKey;

    internal SvgResolvedDocument(SvgDocument? document)
    {
        SourceDocument = document;
        SourceVersion = document == null ? 0 : document.Version;
        Root = null;
        Nodes = new System.Collections.Generic.List<SvgResolvedNode>();
        StyleSheets = null;
        ComputedStyles = null;
        Scene = null;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        FeatureReport = new SvgCapabilities().GetLibraryReport();
        Resources = new SvgResourceSummary();
        Success = false;
        CacheKey = null;
    }

    /// <summary>Finds the authored resolved node for a source identity.</summary>
    /// <param name="source">The authored source node.</param>
    /// <returns>The authored resolved node, or <c>null</c>.</returns>
    internal SvgResolvedNode? FindSource(SvgNode? source)
    {
        if (source == null) return null;
        int index = 0;
        while (index < Nodes.Count)
        {
            SvgResolvedNode node = Nodes[index];
            if (node.InstancePath.Length == 0 && object.ReferenceEquals(node.SourceNode, source)) return node;
            index = index + 1;
        }
        return null;
    }

    /// <summary>Finds one globally identified use-instance node.</summary>
    /// <param name="instancePath">The exact nonempty instance path.</param>
    /// <returns>The matching resolved instance node, or <c>null</c>.</returns>
    internal SvgResolvedNode? FindInstance(string? instancePath)
    {
        if (instancePath == null || instancePath.Length == 0) return null;
        int index = 0;
        while (index < Nodes.Count)
        {
            if (Nodes[index].InstancePath == instancePath) return Nodes[index];
            index = index + 1;
        }
        return null;
    }

    /// <summary>Builds and attaches a normalized scene using bounded defaults.</summary>
    /// <returns>The complete scene construction outcome.</returns>
    internal SvgSceneBuildResult BuildScene()
    {
        return new SvgSceneBuilder().BuildWithDocument(this);
    }

    /// <summary>Builds a normalized scene using explicit bounds and attachment behavior.</summary>
    /// <param name="options">The scene construction options.</param>
    /// <returns>The complete or bounded partial construction outcome.</returns>
    internal SvgSceneBuildResult BuildSceneWithOptions(SvgSceneBuildOptions options)
    {
        return new SvgSceneBuilder().BuildWithDocumentAndOptions(this, options);
    }

    /// <summary>Checks source, environment, stylesheet, capability, and nested resolver versions.</summary>
    /// <returns><c>true</c> when this snapshot remains reusable.</returns>
    internal bool IsCurrent()
    {
        if (SourceDocument == null || SourceDocument.Version != SourceVersion || Root == null || !Root.IsCurrent()) return false;
        return CacheKey != null && CacheKey.IsLive();
    }

    /// <summary>Aggregates current renderable resolved-node drawing bounds.</summary>
    /// <param name="options">The traversal, viewport, stroke, marker, and effect options.</param>
    /// <returns>The aggregate world-space bounds and contribution counts.</returns>
    internal SvgDocumentBoundsResult GetDrawingBounds(SvgDocumentBoundsOptions options)
    {
        return new SvgDrawingBounds().Document(this, options);
    }

    /// <summary>Aggregates current renderable resolved-node drawing bounds with deterministic defaults.</summary>
    /// <returns>The aggregate world-space bounds and contribution counts.</returns>
    internal SvgDocumentBoundsResult GetDefaultDrawingBounds()
    {
        return new SvgDrawingBounds().Document(this, new SvgDocumentBoundsOptions());
    }

    /// <summary>Returns the topmost resolved shape hit at a world-space point.</summary>
    /// <param name="point">The finite world-space query point.</param>
    /// <returns>The topmost source or instance match.</returns>
    internal SvgDocumentHitTestResult HitTest(SvgPoint point)
    {
        return new SvgHitTesting().Document(this, point, new SvgDocumentHitTestOptions());
    }

    /// <summary>Returns topmost or all resolved shape hits at a world-space point.</summary>
    /// <param name="point">The finite world-space query point.</param>
    /// <param name="options">The traversal, result, viewport, and per-node options.</param>
    /// <returns>Topmost-first source and instance matches.</returns>
    internal SvgDocumentHitTestResult HitTestWithOptions(SvgPoint point, SvgDocumentHitTestOptions options)
    {
        return new SvgHitTesting().Document(this, point, options);
    }

    /// <summary>Returns all resolved shape hits at a world-space point.</summary>
    /// <param name="point">The finite world-space query point.</param>
    /// <returns>Every topmost-first source and instance match.</returns>
    internal SvgDocumentHitTestResult HitTestAll(SvgPoint point)
    {
        SvgDocumentHitTestOptions options = new SvgDocumentHitTestOptions();
        options.ReturnAll = true;
        return new SvgHitTesting().Document(this, point, options);
    }

    /// <summary>Builds a platform-neutral accessibility tree with bounded defaults.</summary>
    /// <returns>The complete or failed accessibility-tree result.</returns>
    internal SvgAccessibilityTree BuildAccessibilityTree()
    {
        return new SvgAccessibilityTreeBuilder().BuildWithDocument(this);
    }

    /// <summary>Builds a bounded platform-neutral accessibility tree.</summary>
    /// <param name="options">The traversal, text, and semantic limits.</param>
    /// <returns>The complete or failed accessibility-tree result.</returns>
    internal SvgAccessibilityTree BuildAccessibilityTreeWithOptions(SvgAccessibilityTreeOptions options)
    {
        return new SvgAccessibilityTreeBuilder().BuildWithDocumentAndOptions(this, options);
    }
}
