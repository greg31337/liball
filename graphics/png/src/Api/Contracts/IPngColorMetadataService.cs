namespace png;

/// <summary>
/// Resolves PNG color metadata and normalizes decoded pixels toward an sRGB-like representation.
/// </summary>
public interface IPngColorMetadataService
{
    /// <summary>
    /// Resolves the effective color metadata using PNG cICP, iCCP, sRGB, and cHRM/gAMA precedence.
    /// </summary>
    /// <param name="metadata">The parsed or caller-created metadata.</param>
    /// <returns>The highest-precedence color metadata.</returns>
    PngColorMetadata ResolveColorMetadata(PngMetadata metadata);
    /// <summary>
    /// Resolves the effective color metadata while enforcing caller-supplied limits.
    /// </summary>
    /// <param name="metadata">The parsed or caller-created metadata.</param>
    /// <param name="limits">The limits used when an embedded profile must be decompressed.</param>
    /// <returns>The highest-precedence color metadata.</returns>
    PngColorMetadata ResolveColorMetadataWithLimits(PngMetadata metadata, PngSecurityLimits limits);
    /// <summary>
    /// Normalizes decoded pixels using the effective color metadata and requested rendering operations.
    /// </summary>
    /// <param name="image">The decoded image and its metadata.</param>
    /// <param name="options">The normalization options.</param>
    /// <returns>The normalized pixels, selected metadata, and diagnostics.</returns>
    PngColorNormalizationResult NormalizeToSrgb(PngImage image, PngColorNormalizationOptions options);
}
