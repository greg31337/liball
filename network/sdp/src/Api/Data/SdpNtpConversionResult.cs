using System;

namespace sdp;

/// <summary>Contains the result of converting between decimal NTP seconds and a .NET instant.</summary>
public class SdpNtpConversionResult
{
    /// <summary>Indicates whether the input has an exact representation in the destination form.</summary>
    public bool Success = false;

    /// <summary>Contains the converted decimal NTP seconds when conversion succeeds.</summary>
    public string NtpValue = string.Empty;

    /// <summary>Contains the converted UTC instant when conversion succeeds.</summary>
    public DateTimeOffset DateTimeValue = default(DateTimeOffset);

    /// <summary>Contains a stable diagnostic code when conversion fails.</summary>
    public string DiagnosticCode = string.Empty;

    /// <summary>Contains a human-readable explanation when conversion fails.</summary>
    public string Detail = string.Empty;
}
