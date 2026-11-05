namespace icc;

/// <summary>Controls DeviceLink construction from a profile sequence.</summary>
public class IccProfileLinkOptions
{
    /// <summary>Gets or sets the rendering intent.</summary>
    public IccRenderingIntent RenderingIntent;
    /// <summary>Gets or sets intent fallback behavior.</summary>
    public IccIntentFallback IntentFallback;
    /// <summary>Gets or sets the output profile version.</summary>
    public IccVersion Version;
    /// <summary>Gets or sets the uniform number of CLUT grid points in every input dimension.</summary>
    public int GridPointCount;
    /// <summary>Gets or sets the LUT or MPE type signature used to encode the baked transform.</summary>
    /// <remarks>Supported values are <see cref="IccTypeSignatures.Lut8"/>, <see cref="IccTypeSignatures.Lut16"/>, and <see cref="IccTypeSignatures.MultiProcessElements"/>.</remarks>
    public uint EncodingTypeSignature;
    /// <summary>Gets or sets the interpolation used while compiling and describing the baked CLUT.</summary>
    public IccInterpolationKind Interpolation;
    /// <summary>Gets or sets localized DeviceLink descriptions.</summary>
    public IccLocalizedString[] Descriptions;
    /// <summary>Gets or sets whether sequence information is embedded.</summary>
    public bool IncludeProfileSequence;

    /// <summary>Initializes version-4 perceptual link options.</summary>
    public IccProfileLinkOptions()
    {
        RenderingIntent = IccRenderingIntent.Perceptual;
        IntentFallback = IccIntentFallback.None;
        Version = new IccVersion();
        Version.Kind = IccVersionKind.Version4;
        Version.Major = 4;
        GridPointCount = 17;
        EncodingTypeSignature = IccTypeSignatures.MultiProcessElements;
        Interpolation = IccInterpolationKind.Multilinear;
        Descriptions = new IccLocalizedString[0];
        IncludeProfileSequence = true;
    }
}
