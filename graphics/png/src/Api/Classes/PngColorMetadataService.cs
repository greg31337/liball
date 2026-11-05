namespace png;

/// <summary>
/// Provides PNG color-metadata precedence and sRGB-like pixel normalization.
/// </summary>
public class PngColorMetadataService : IPngColorMetadataService
{
    /// <summary>
    /// Initializes a stateless PNG color-metadata service.
    /// </summary>
    public PngColorMetadataService()
    {
    }

    /// <summary>
    /// Resolves effective color metadata using the PNG-defined precedence.
    /// </summary>
    /// <param name="metadata">The parsed or caller-created metadata.</param>
    /// <returns>The selected color metadata.</returns>
    public PngColorMetadata ResolveColorMetadata(PngMetadata metadata)
    {
        return PngColorMetadataPrecedenceResolver.Resolve(metadata);
    }

    /// <summary>
    /// Resolves effective color metadata using the PNG-defined precedence and caller-supplied limits.
    /// </summary>
    /// <param name="metadata">The parsed or caller-created metadata.</param>
    /// <param name="limits">The limits used when an embedded profile must be decompressed.</param>
    /// <returns>The selected color metadata.</returns>
    public PngColorMetadata ResolveColorMetadataWithLimits(PngMetadata metadata, PngSecurityLimits limits)
    {
        return PngColorMetadataPrecedenceResolver.Resolve(metadata, limits);
    }

    /// <summary>
    /// Normalizes decoded pixels using significant-bit, color-transfer, and optional background operations.
    /// </summary>
    /// <param name="image">The decoded image and its metadata.</param>
    /// <param name="options">The normalization options.</param>
    /// <returns>The normalized pixels, selected metadata, and diagnostics.</returns>
    public PngColorNormalizationResult NormalizeToSrgb(PngImage image, PngColorNormalizationOptions options)
    {
        return PngColorMetadataNormalizer.Normalize(image, options);
    }
}
