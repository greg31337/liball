namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgAccessibilityTreeBuilder"/>.</summary>
public interface ISvgAccessibilityTreeBuilder
{
    /// <summary>Builds an accessibility tree with secure deterministic defaults.</summary>
    /// <param name="document">The current resolved SVG document.</param>
    /// <returns>The complete or failed accessibility-tree result.</returns>
    SvgAccessibilityTree BuildWithDocument(SvgResolvedDocument document);
    /// <summary>Builds a bounded accessibility tree with explicit options.</summary>
    /// <param name="document">The current resolved SVG document.</param>
    /// <param name="options">The traversal, text, and semantic limits.</param>
    /// <returns>The complete or failed accessibility-tree result.</returns>
    SvgAccessibilityTree BuildWithDocumentAndOptions(SvgResolvedDocument document, SvgAccessibilityTreeOptions options);
}
