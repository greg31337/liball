namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgCascade"/>.</summary>
public interface ISvgCascade
{
    /// <summary>Cascades an ordered list of parsed stylesheets with default options.</summary>
    /// <param name="node">The target SVG element.</param>
    /// <param name="styleSheets">The stylesheets in collection order.</param>
    /// <returns>The cascaded property values and diagnostics.</returns>
    SvgCascadeResult ResolveWithNodeAndStyleSheets(SvgNode node, System.Collections.Generic.List<SvgStyleSheet> styleSheets);
    /// <summary>Cascades an ordered list of parsed stylesheets.</summary>
    /// <param name="node">The target SVG element.</param>
    /// <param name="styleSheets">The stylesheets in collection order.</param>
    /// <param name="options">The property, inheritance, environment, and limit options.</param>
    /// <returns>The cascaded property values and diagnostics.</returns>
    SvgCascadeResult ResolveWithNodeAndStyleSheetsAndOptions(SvgNode node, System.Collections.Generic.List<SvgStyleSheet> styleSheets, SvgCascadeOptions options);
    /// <summary>Cascades a collected set of embedded, imported, and caller stylesheets.</summary>
    /// <param name="node">The target SVG element.</param>
    /// <param name="collection">The ordered stylesheet collection.</param>
    /// <param name="options">The property, inheritance, environment, and limit options.</param>
    /// <returns>The cascaded property values and diagnostics.</returns>
    SvgCascadeResult ResolveWithNodeAndCollectionAndOptions(SvgNode node, SvgStyleSheetCollection? collection, SvgCascadeOptions options);
}
