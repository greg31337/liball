namespace avif;

/// <summary>
/// Configures AVIF, MIAF, HEIF, and AV1 validation behavior.
/// </summary>
public class AvifValidationOptions
{
    /// <summary>The conformance policy applied by validation.</summary>
    public AvifValidationMode Mode;

    /// <summary>Whether unknown non-essential structures are accepted and exposed.</summary>
    public bool AllowUnknownNonEssentialData;

    /// <summary>Whether encoded AV1 payload syntax and semantics are validated.</summary>
    public bool ValidateEncodedPayload;

    /// <summary>Whether validation also performs pixel decode and decode-process checks.</summary>
    public bool DecodePixelData;

    /// <summary>Whether informational diagnostics are collected.</summary>
    public bool CollectInformation;

    /// <summary>Whether warning diagnostics are collected.</summary>
    public bool CollectWarnings;

    /// <summary>Whether validation stops after the first error.</summary>
    public bool StopAfterFirstError;

    /// <summary>The resource limits applied by validation.</summary>
    public AvifSecurityLimits Limits;

    /// <summary>
    /// Creates strict validation options that collect all warnings without decoding pixels.
    /// </summary>
    public AvifValidationOptions()
    {
        Mode = AvifValidationMode.Strict;
        AllowUnknownNonEssentialData = true;
        ValidateEncodedPayload = true;
        DecodePixelData = false;
        CollectInformation = false;
        CollectWarnings = true;
        StopAfterFirstError = false;
        Limits = new AvifSecurityLimits();
    }
}
