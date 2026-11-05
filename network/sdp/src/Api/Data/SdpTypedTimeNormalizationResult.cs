namespace sdp;

/// <summary>Contains the result of normalizing an SDP typed-time value to seconds.</summary>
public class SdpTypedTimeNormalizationResult
{
    /// <summary>Indicates whether the typed-time value has an exact signed 64-bit seconds representation.</summary>
    public bool Success = false;

    /// <summary>Contains the normalized signed seconds when conversion succeeds.</summary>
    public long Seconds = 0;

    /// <summary>Contains a stable diagnostic code when normalization fails.</summary>
    public string DiagnosticCode = string.Empty;

    /// <summary>Contains a human-readable explanation when normalization fails.</summary>
    public string Detail = string.Empty;
}
