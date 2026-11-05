namespace png;

/// <summary>
/// Controls significant-bit recovery and optional background compositing during sRGB-like normalization.
/// </summary>
public class PngColorNormalizationOptions
{
    /// <summary>
    /// Initializes normalization with significant-bit handling enabled and background compositing disabled.
    /// </summary>
    public PngColorNormalizationOptions()
    {
    }

    /// <summary>
    /// Selects whether sBIT metadata is used to recover and rescale the original sample precision.
    /// </summary>
    public bool ApplySignificantBits = true;
    /// <summary>
    /// Selects whether alpha pixels are composited over an opaque background before color-transfer normalization.
    /// </summary>
    public bool CompositeOverBackground;
    /// <summary>
    /// Stores an explicit background in the source color encoding, or null to use bKGD metadata.
    /// </summary>
    public PngBackgroundColor? Background;
    /// <summary>
    /// Stores limits used while resolving compressed color metadata.
    /// </summary>
    public PngSecurityLimits Limits = new PngSecurityLimits();
}

/// <summary>
/// Stores sRGB-like normalized pixels together with the selected metadata and transformation diagnostics.
/// </summary>
public class PngColorNormalizationResult
{
    /// <summary>
    /// Initializes an empty normalization result.
    /// </summary>
    public PngColorNormalizationResult()
    {
    }

    /// <summary>
    /// Stores the independently allocated normalized pixels.
    /// </summary>
    public PngPixelBuffer Pixels = new PngPixelBuffer();
    /// <summary>
    /// Stores the highest-precedence color metadata used for the decision.
    /// </summary>
    public PngColorMetadata ColorMetadata = new PngColorMetadata();
    /// <summary>
    /// Stores non-fatal diagnostics for transformations that could not be completed.
    /// </summary>
    public PngWarning[] Warnings = new PngWarning[0];
    /// <summary>
    /// Indicates whether sBIT precision recovery was applied.
    /// </summary>
    public bool AppliedSignificantBits;
    /// <summary>
    /// Indicates whether a gamma transfer changed the color samples.
    /// </summary>
    public bool AppliedGammaTransform;
    /// <summary>
    /// Indicates whether alpha was composited over a background.
    /// </summary>
    public bool AppliedBackgroundComposite;
    /// <summary>
    /// Indicates whether the selected metadata was fully handled as an sRGB-like color space.
    /// </summary>
    public bool IsSrgbLike;
}
