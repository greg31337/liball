namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgValidator"/>.</summary>
public interface ISvgValidator
{
    /// <summary>Validates one SVG document against strict SVG 2 structural defaults.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <returns>The completed or failed validation result.</returns>
    SvgValidationResult ValidateWithDocument(SvgDocument document);
    /// <summary>Validates one SVG document against an explicit profile and bounds.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="options">The profile, catalogs, and traversal limits.</param>
    /// <returns>The completed or failed validation result.</returns>
    SvgValidationResult ValidateWithDocumentAndOptions(SvgDocument? document, SvgValidationOptions? options);
}
