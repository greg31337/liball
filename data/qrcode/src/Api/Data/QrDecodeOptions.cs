namespace qrcode;

/// <summary>Controls accepted symbol families, parsing strictness, recovery, and decode resource limits.</summary>
public class QrDecodeOptions
{
    /// <summary>Whether QR Code Model 2 may be decoded.</summary>
    public bool AllowModel2 = true;

    /// <summary>Whether Micro QR Code may be decoded.</summary>
    public bool AllowMicro = true;

    /// <summary>Whether rectangular Micro QR Code may be decoded.</summary>
    public bool AllowRectangularMicro = true;

    /// <summary>Whether legacy QR Code Model 1 may be decoded.</summary>
    public bool AllowLegacyModel1 = true;

    /// <summary>Whether malformed headers, padding, and control ordering are rejected.</summary>
    public bool Strict = true;

    /// <summary>The expected module polarity or automatic polarity selection.</summary>
    public QrPolarity Polarity = QrPolarity.Automatic;

    /// <summary>Whether mirrored symbol geometry may be examined.</summary>
    public bool TryMirrored = true;

    /// <summary>Whether reversed-reflectance symbols may be examined.</summary>
    public bool TryInverted = true;

    /// <summary>Whether bounded format, sampling, and error-correction recovery is enabled.</summary>
    public bool EnableRecovery = true;

    /// <summary>Whether unsupported ECI payloads retain their raw bytes without text conversion.</summary>
    public bool PreserveUnsupportedEciBytes = true;

    /// <summary>Whether byte segments without ECI may use deterministic encoding inference.</summary>
    public bool EnableByteEncodingInference;

    /// <summary>Whether byte payloads with non-default encodings require an explicit ECI designator.</summary>
    public bool RequireExplicitEci;

    /// <summary>Whether the non-ISO Hanzi compatibility extension may be decoded.</summary>
    public bool EnableHanziCompatibility;

    /// <summary>The largest accepted matrix dimension in modules.</summary>
    public int MaximumMatrixDimension = 177;

    /// <summary>The maximum number of parsed payload and control segments.</summary>
    public int MaximumSegments = 1024;

    /// <summary>The maximum number of BCH/RS trials shared across matrix, metadata, module-value, and image retries.</summary>
    /// <remarks>An image sample with no recognizable symbol family consumes one attempt. Orientation discovery and each sampling or refinement operation have fixed internal bounds.</remarks>
    public int MaximumRecoveryAttempts = 64;

    /// <summary>The maximum total bytes allocated for one decode operation.</summary>
    public int MaximumAllocationBytes = 16777216;
}
