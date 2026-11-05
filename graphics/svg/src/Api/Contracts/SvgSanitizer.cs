namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgSanitizer"/>.</summary>
public interface ISvgSanitizer
{
    /// <summary>Sanitizes one SVG document using secure-static defaults.</summary>
    /// <param name="document">The editable source document, which is not mutated.</param>
    /// <returns>The independent sanitized document and removal summary.</returns>
    SvgSanitizationResult SanitizeWithDocument(SvgDocument document);
    /// <summary>Sanitizes one SVG document using explicit bounds and resource policy.</summary>
    /// <param name="document">The editable source document, which is not mutated.</param>
    /// <param name="options">The traversal, CSS, URI, resource, and link policy.</param>
    /// <returns>The independent sanitized document and removal summary.</returns>
    SvgSanitizationResult SanitizeWithDocumentAndOptions(SvgDocument? document, SvgSanitizationOptions options);
}
