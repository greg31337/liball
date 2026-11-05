namespace avif;

/// <summary>
/// Configures derived-image reconstruction, image transforms, tone mapping, and packed rendering.
/// </summary>
public class AvifRenderOptions
{
    /// <summary>Whether the clean-aperture crop is applied.</summary>
    public bool ApplyCleanAperture;

    /// <summary>Whether an item rotation transform is applied.</summary>
    public bool ApplyRotation;

    /// <summary>Whether an item mirror transform is applied.</summary>
    public bool ApplyMirror;

    /// <summary>Whether reconciled Exif orientation is applied.</summary>
    public bool ApplyExifOrientation;

    /// <summary>The gain-map and tone-map rendering policy.</summary>
    public AvifToneMapMode ToneMapMode;

    /// <summary>The target display headroom as a ratio relative to reference white.</summary>
    public float DisplayHeadroom;

    /// <summary>Whether an available alpha channel is retained.</summary>
    public bool PreserveAlpha;

    /// <summary>The color-conversion settings used for packed output.</summary>
    public AvifColorConversionOptions ColorConversion;

    /// <summary>The resource limits used by direct rendering operations.</summary>
    public AvifSecurityLimits Limits;

    /// <summary>
    /// Creates options that apply normative transforms and select tone mapping from display headroom.
    /// </summary>
    public AvifRenderOptions()
    {
        ApplyCleanAperture = true;
        ApplyRotation = true;
        ApplyMirror = true;
        ApplyExifOrientation = true;
        ToneMapMode = AvifToneMapMode.Automatic;
        DisplayHeadroom = 1.0f;
        PreserveAlpha = true;
        ColorConversion = new AvifColorConversionOptions();
        Limits = new AvifSecurityLimits();
    }
}
