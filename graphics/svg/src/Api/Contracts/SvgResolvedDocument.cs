namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgDocumentResolver"/>.</summary>
public interface ISvgDocumentResolver
{
    /// <summary>Resolves an editable document using secure deterministic defaults.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <returns>The current resolved snapshot.</returns>
    SvgResolvedDocument Resolve(SvgDocument document);
    /// <summary>Resolves an editable document through current style, conditional, viewport, structure, geometry, reference, and resource stages.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="options">The host environment, resource policy, security limits, and cache bounds.</param>
    /// <returns>A cached current snapshot or a newly resolved document.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    SvgResolvedDocument ResolveWithOptions(SvgDocument document, SvgResolveOptions options);
    /// <summary>Invalidates every cached snapshot owned by one editable document.</summary>
    /// <param name="document">The editable source document.</param>
    /// <returns>The removed cache-entry count.</returns>
    int Invalidate(SvgDocument? document);
    /// <summary>Clears every resolved-document and computed-style cache entry.</summary>
    /// <returns>The removed resolved-document entry count.</returns>
    int ClearCache();
    /// <summary>Returns current resolved-document cache counters.</summary>
    /// <returns>An independent statistics snapshot.</returns>
    SvgResolvedDocumentCacheStatistics GetStatistics();
}
