namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgNestedDocumentLoader"/>.</summary>
public interface ISvgNestedDocumentLoader
{
    /// <summary>Acquires, parses, rebases, and selects one nested SVG document fragment.</summary>
    /// <param name="request">The resolved nested resource request.</param>
    /// <param name="options">The inherited load options and shared resource context.</param>
    /// <returns>The nested document, selected target, provenance, and diagnostics.</returns>
    SvgNestedDocumentResult Load(SvgNestedDocumentRequest? request, SvgNestedDocumentOptions options);
}
