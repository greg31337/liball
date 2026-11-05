namespace sdp;

/// <summary>
/// Describes a parser or writer diagnostic without exposing sensitive input values.
/// </summary>
public class SdpDiagnostic
{
    /// <summary>Contains the stable diagnostic code.</summary>
    public string Code = string.Empty;

    /// <summary>Identifies the diagnostic severity.</summary>
    public SdpValidationSeverity Severity = SdpValidationSeverity.Unknown;

    /// <summary>Identifies the session or media scope associated with the diagnostic.</summary>
    public SdpScope Scope = SdpScope.Unknown;

    /// <summary>Contains the related field discriminator, or the null character when unavailable.</summary>
    public char FieldName = '\0';

    /// <summary>Contains the related attribute name when applicable.</summary>
    public string AttributeName = string.Empty;

    /// <summary>Contains the zero-based media index, or <c>-1</c> for session scope.</summary>
    public int MediaIndex = -1;

    /// <summary>Contains a human-readable detail that excludes sensitive values.</summary>
    public string Detail = string.Empty;

    /// <summary>Identifies the affected model value.</summary>
    public SdpSourcePath Path = new SdpSourcePath();

    /// <summary>Identifies the diagnostic's source position.</summary>
    public SdpSourceLocation Location = new SdpSourceLocation();
}
