namespace icc;

/// <summary>Controls transform selection, interpolation, and PCS handling.</summary>
public class IccTransformOptions
{
    /// <summary>Gets or sets transform direction.</summary>
    public IccTransformDirection Direction;
    /// <summary>Gets or sets rendering intent.</summary>
    public IccRenderingIntent RenderingIntent;
    /// <summary>Gets or sets intent fallback policy.</summary>
    public IccIntentFallback IntentFallback;
    /// <summary>Gets or sets interpolation.</summary>
    public IccInterpolationKind Interpolation;
    /// <summary>Gets or sets PCS encoding.</summary>
    public IccPcsEncodingKind PcsEncoding;
    /// <summary>Gets or sets whether black-point compensation is applied.</summary>
    public bool BlackPointCompensation;
    /// <summary>Gets or sets whether destination values are clipped to their declared encoding ranges.</summary>
    public bool ClipOutput;
    /// <summary>Gets or sets whether transform tracing is enabled.</summary>
    public bool EnableTrace;

    /// <summary>Initializes a perceptual device-to-PCS transform.</summary>
    public IccTransformOptions()
    {
        Direction = IccTransformDirection.DeviceToPcs;
        RenderingIntent = IccRenderingIntent.Perceptual;
        IntentFallback = IccIntentFallback.None;
        Interpolation = IccInterpolationKind.Multilinear;
        PcsEncoding = IccPcsEncodingKind.Normalized;
        BlackPointCompensation = false;
        ClipOutput = true;
        EnableTrace = false;
    }
}
