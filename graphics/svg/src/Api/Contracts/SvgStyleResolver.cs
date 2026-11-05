namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgStyleResolver"/>.</summary>
public interface ISvgStyleResolver
{
    /// <summary>Computes one element using ordered stylesheets and cache defaults.</summary>
    /// <param name="node">The target SVG element.</param>
    /// <param name="styleSheets">The stylesheets in cascade order.</param>
    /// <returns>The cached or newly computed style.</returns>
    SvgComputedStyle? Resolve(SvgNode node, System.Collections.Generic.List<SvgStyleSheet> styleSheets);
    /// <summary>Computes one element using ordered stylesheets and explicit options.</summary>
    /// <param name="node">The target SVG element.</param>
    /// <param name="styleSheets">The stylesheets in cascade order.</param>
    /// <param name="options">The cascade, environment, font, and cache options.</param>
    /// <returns>The cached or newly computed style.</returns>
    SvgComputedStyle? ResolveWithOptions(SvgNode node, System.Collections.Generic.List<SvgStyleSheet> styleSheets, SvgStyleResolverOptions options);
    /// <summary>Computes one element using a collected stylesheet set and explicit options.</summary>
    /// <param name="node">The target SVG element.</param>
    /// <param name="collection">The collected embedded, imported, and caller stylesheets.</param>
    /// <param name="options">The cascade, environment, font, and cache options.</param>
    /// <returns>The cached or newly computed style.</returns>
    SvgComputedStyle? ResolveCollection(SvgNode node, SvgStyleSheetCollection collection, SvgStyleResolverOptions options);
    /// <summary>Computes every document element using a collected stylesheet set.</summary>
    /// <param name="document">The source SVG document.</param>
    /// <param name="collection">The collected stylesheet set.</param>
    /// <param name="options">The cascade, environment, font, and cache options.</param>
    /// <returns>The preorder computed-style tree.</returns>
    SvgComputedStyleTree ResolveDocument(SvgDocument document, SvgStyleSheetCollection collection, SvgStyleResolverOptions options);
    /// <summary>Computes every document element using ordered stylesheets.</summary>
    /// <param name="document">The source SVG document.</param>
    /// <param name="styleSheets">The stylesheets in cascade order.</param>
    /// <param name="options">The cascade, environment, font, and cache options.</param>
    /// <returns>The preorder computed-style tree.</returns>
    SvgComputedStyleTree ResolveDocumentFromStyleSheets(SvgDocument document, System.Collections.Generic.List<SvgStyleSheet> styleSheets, SvgStyleResolverOptions options);
    /// <summary>Returns current cache counters.</summary>
    /// <returns>An independent statistics snapshot.</returns>
    SvgStyleResolverStatistics GetStatistics();
    /// <summary>Invalidates every cached computed style.</summary>
    /// <returns>The invalidation result.</returns>
    SvgStyleInvalidationResult InvalidateAll();
    /// <summary>Invalidates styles sharing a tree with a changed node.</summary>
    /// <param name="node">A node in the changed tree.</param>
    /// <returns>The invalidation result.</returns>
    SvgStyleInvalidationResult InvalidateTree(SvgNode node);
    /// <summary>Invalidates selector and inheritance dependencies for one changed attribute.</summary>
    /// <param name="node">The owning element.</param>
    /// <param name="attributeName">The case-sensitive XML local name.</param>
    /// <returns>The invalidation result.</returns>
    SvgStyleInvalidationResult InvalidateAttribute(SvgNode node, string attributeName);
    /// <summary>Invalidates entries that used one stylesheet identity.</summary>
    /// <param name="styleSheet">The changed stylesheet.</param>
    /// <returns>The invalidation result.</returns>
    SvgStyleInvalidationResult InvalidateStyleSheet(SvgStyleSheet styleSheet);
    /// <summary>Invalidates dependent styles below one custom-property definition.</summary>
    /// <param name="node">The defining element.</param>
    /// <param name="customPropertyName">The case-sensitive custom-property name.</param>
    /// <returns>The invalidation result.</returns>
    SvgStyleInvalidationResult InvalidateCustomProperty(SvgNode node, string customPropertyName);
    /// <summary>Invalidates entries using one viewport environment.</summary>
    /// <param name="environment">The changed environment.</param>
    /// <returns>The invalidation result.</returns>
    SvgStyleInvalidationResult InvalidateViewport(SvgStyleEnvironment environment);
    /// <summary>Invalidates entries using one media environment.</summary>
    /// <param name="environment">The changed environment.</param>
    /// <returns>The invalidation result.</returns>
    SvgStyleInvalidationResult InvalidateMedia(SvgStyleEnvironment environment);
    /// <summary>Invalidates all styles that can depend on caller font data.</summary>
    /// <returns>The invalidation result.</returns>
    SvgStyleInvalidationResult InvalidateFonts();
    /// <summary>Invalidates entries using one host interaction-state object.</summary>
    /// <param name="state">The changed interaction state.</param>
    /// <returns>The invalidation result.</returns>
    SvgStyleInvalidationResult InvalidateInteraction(ISvgDynamicSelectorState state);
}
