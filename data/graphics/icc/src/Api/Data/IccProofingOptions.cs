namespace icc;

/// <summary>Controls soft-proof transform selection, gamut checking, and alarm output.</summary>
public class IccProofingOptions
{
    /// <summary>Gets or sets the rendering intent used from the source into the proof device.</summary>
    public IccRenderingIntent RenderingIntent;
    /// <summary>Gets or sets the proofing intent used from the proof device into the destination.</summary>
    public IccRenderingIntent ProofingIntent;
    /// <summary>Gets or sets the intent fallback policy for every profile direction.</summary>
    public IccIntentFallback IntentFallback;
    /// <summary>Gets or sets the interpolation algorithm.</summary>
    public IccInterpolationKind Interpolation;
    /// <summary>Gets or sets whether the proof profile gamut tag is evaluated.</summary>
    public bool EnableGamutCheck;
    /// <summary>Gets or sets the destination-space colour emitted for an out-of-gamut input.</summary>
    public double[] AlarmColor;
    /// <summary>Gets or sets whether destination and alarm values are clipped to destination ranges.</summary>
    public bool ClipOutput;
    /// <summary>Gets or sets whether transform tracing is enabled.</summary>
    public bool EnableTrace;

    /// <summary>Initializes perceptual rendering with media-relative proofing and gamut checking disabled.</summary>
    public IccProofingOptions()
    {
        RenderingIntent = IccRenderingIntent.Perceptual;
        ProofingIntent = IccRenderingIntent.MediaRelativeColorimetric;
        IntentFallback = IccIntentFallback.None;
        Interpolation = IccInterpolationKind.Multilinear;
        EnableGamutCheck = false;
        AlarmColor = new double[0];
        ClipOutput = true;
        EnableTrace = false;
    }
}
