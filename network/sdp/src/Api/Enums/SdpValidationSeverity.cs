namespace sdp;

/// <summary>
/// Identifies the severity of an SDP validation issue or diagnostic.
/// </summary>
public enum SdpValidationSeverity
{
    /// <summary>The severity is unknown.</summary>
    Unknown = 0,

    /// <summary>The issue is informational and does not indicate invalid input.</summary>
    Information = 1,

    /// <summary>The issue describes a tolerated condition that may affect interoperability.</summary>
    Warning = 2,

    /// <summary>The issue makes the selected validation profile unsuccessful.</summary>
    Error = 3,

    /// <summary>The issue prevented further safe processing.</summary>
    Fatal = 4
}
