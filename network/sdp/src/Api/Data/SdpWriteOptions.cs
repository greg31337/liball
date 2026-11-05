namespace sdp;

/// <summary>
/// Configures bounded canonical or lossless SDP generation.
/// </summary>
public class SdpWriteOptions
{
    /// <summary>Specifies whether canonical or preserved source formatting is used.</summary>
    public SdpWriteMode Mode = SdpWriteMode.Canonical;

    /// <summary>Specifies the line ending used when preserved source endings do not apply.</summary>
    public SdpLineEnding LineEnding = SdpLineEnding.CrLf;

    /// <summary>Indicates whether the generated document ends with a line ending.</summary>
    public bool IncludeFinalLineEnding = true;

    /// <summary>Indicates whether obsolete fields retained by the model are emitted.</summary>
    public bool IncludeObsoleteFields = true;

    /// <summary>Indicates whether unknown extension fields retained by the model are emitted.</summary>
    public bool IncludeUnknownFields = true;

    /// <summary>Contains the maximum number of generated text characters.</summary>
    public int MaximumOutputCharacters = SdpLimits.DefaultMaximumOutputCharacters;

    /// <summary>Contains the maximum number of generated UTF-8 bytes.</summary>
    public int MaximumOutputBytes = SdpLimits.DefaultMaximumOutputBytes;

    /// <summary>Contains the maximum number of diagnostics retained by one write.</summary>
    public int MaximumDiagnosticCount = SdpLimits.DefaultMaximumDiagnosticCount;
}
