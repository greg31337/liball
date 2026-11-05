namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgReferenceResolver"/>.</summary>
public interface ISvgReferenceResolver
{
    /// <summary>Resolves one element's optional unnamespaced or legacy XLink href.</summary>
    /// <param name="node">The source SVG element.</param>
    /// <returns>The selected reference and available target.</returns>
    SvgReferenceResult ResolveWithNode(SvgNode node);
    /// <summary>Resolves one element's href with explicit requiredness, external document, and URI bound.</summary>
    /// <param name="node">The source SVG element.</param>
    /// <param name="options">The resolution options.</param>
    /// <returns>The selected reference and available target.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when the node is not an element.</exception>
    SvgReferenceResult ResolveWithNodeAndOptions(SvgNode node, SvgReferenceResolveOptions options);
    /// <summary>Resolves a computed URL reference without adding a synthetic DOM attribute.</summary>
    /// <param name="node">The element whose base URI and document provide reference context.</param>
    /// <param name="value">The unwrapped computed IRI text.</param>
    /// <param name="source">The computed declaration source span, or <c>null</c>.</param>
    /// <param name="options">The resolution options.</param>
    /// <returns>The resolved reference with URI and target provenance.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when the node is not an element.</exception>
    SvgReferenceResult ResolveValue(SvgNode node, string value, SvgSourceSpan? source, SvgReferenceResolveOptions options);
    /// <summary>Finds a percent-encoded fragment identifier in a document's current ID index.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="fragment">The fragment text with or without a leading number sign.</param>
    /// <returns>The first exact ID match in document order, the document root for an empty fragment, or <c>null</c>.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    SvgNode? FindFragment(SvgDocument document, string fragment);
}
