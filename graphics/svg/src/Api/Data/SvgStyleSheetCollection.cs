namespace svg;

/// <summary>Associates one stylesheet with collection order and source provenance.</summary>
public class SvgCollectedStyleSheet
{
    /// <summary>The parsed stylesheet.</summary>
    public SvgStyleSheet StyleSheet;
    /// <summary>How the stylesheet entered the collection.</summary>
    public SvgStyleSheetSourceKind SourceKind;
    /// <summary>The originating SVG style element, or <c>null</c> for caller and imported stylesheets.</summary>
    public SvgNode? SourceNode;
    /// <summary>The source span that introduced the stylesheet, or <c>null</c>.</summary>
    public SvgSourceSpan? ReferenceSource;
    /// <summary>The exact media attribute or import condition text, or an empty string.</summary>
    public string Media;
    /// <summary>The zero-based order in the complete collection.</summary>
    public int CollectionOrder;
    /// <summary>The normalized resource URI for an imported sheet, or an empty string.</summary>
    public string ResolvedUri;
    /// <summary>The import nesting depth, or zero for embedded and caller sheets.</summary>
    public int ImportDepth;

    /// <summary>Creates one collected stylesheet entry.</summary>
    /// <param name="styleSheet">The parsed stylesheet.</param>
    /// <param name="sourceKind">How the stylesheet entered the collection.</param>
    /// <param name="sourceNode">The originating style element, or <c>null</c>.</param>
    /// <param name="referenceSource">The introducing source span, or <c>null</c>.</param>
    /// <param name="media">The exact media text, or an empty string.</param>
    /// <param name="collectionOrder">The zero-based collection order.</param>
    public SvgCollectedStyleSheet(SvgStyleSheet styleSheet, SvgStyleSheetSourceKind sourceKind, SvgNode? sourceNode, SvgSourceSpan? referenceSource, string media, int collectionOrder)
    {
        if (styleSheet == null) throw new System.ArgumentNullException("styleSheet");
        if (sourceKind < SvgStyleSheetSourceKind.Embedded || sourceKind > SvgStyleSheetSourceKind.Imported) throw new System.ArgumentOutOfRangeException("sourceKind");
        if (media == null) throw new System.ArgumentNullException("media");
        if (collectionOrder < 0) throw new System.ArgumentOutOfRangeException("collectionOrder");
        if (sourceKind == SvgStyleSheetSourceKind.Embedded && sourceNode == null) throw new System.ArgumentNullException("sourceNode");
        if (sourceKind != SvgStyleSheetSourceKind.Embedded && sourceNode != null) throw new System.ArgumentException("Only an embedded stylesheet can have a source node.", "sourceNode");
        StyleSheet = styleSheet;
        SourceKind = sourceKind;
        SourceNode = sourceNode;
        ReferenceSource = referenceSource;
        Media = media;
        CollectionOrder = collectionOrder;
        ResolvedUri = "";
        ImportDepth = 0;
    }

    /// <summary>Tests the entry's media text against a style environment.</summary>
    /// <param name="environment">The concrete style environment.</param>
    /// <param name="limits">The CSS token and character limits.</param>
    /// <returns><c>true</c> for an unconditional entry or a matching valid query.</returns>
    internal bool MatchesMedia(SvgStyleEnvironment environment, SvgCssLimits limits)
    {
        if (environment == null) throw new System.ArgumentNullException("environment");
        if (limits == null) throw new System.ArgumentNullException("limits");
        return Media.Length == 0 || environment.Matches(Media, limits);
    }
}

/// <summary>Contains ordered embedded, caller, and import-expanded stylesheets.</summary>
public class SvgStyleSheetCollection
{
    /// <summary>The ordered collected stylesheet entries.</summary>
    public System.Collections.Generic.List<SvgCollectedStyleSheet> StyleSheets;
    /// <summary>Collection and stylesheet diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>Whether collection and every included stylesheet are valid.</summary>
    public bool IsValid;
    /// <summary>Resource acquisition totals for imported stylesheets.</summary>
    public SvgResourceSummary Resources;

    /// <summary>Creates a collection and copies its entries and diagnostics.</summary>
    /// <param name="styleSheets">The ordered stylesheet entries.</param>
    /// <param name="diagnostics">The collection diagnostics.</param>
    public SvgStyleSheetCollection(System.Collections.Generic.List<SvgCollectedStyleSheet> styleSheets, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        if (styleSheets == null) throw new System.ArgumentNullException("styleSheets");
        if (diagnostics == null) throw new System.ArgumentNullException("diagnostics");
        StyleSheets = new System.Collections.Generic.List<SvgCollectedStyleSheet>(styleSheets);
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>(diagnostics);
        Resources = new SvgResourceSummary();
        IsValid = AllValid(StyleSheets, Diagnostics);
    }

    internal static bool AllValid(System.Collections.Generic.List<SvgCollectedStyleSheet> styleSheets, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index] != null && diagnostics[index].Severity == SvgDiagnosticSeverity.Error) return false;
            index = index + 1;
        }
        index = 0;
        while (index < styleSheets.Count)
        {
            if (styleSheets[index] == null || !styleSheets[index].StyleSheet.IsValid) return false;
            index = index + 1;
        }
        return true;
    }
}
