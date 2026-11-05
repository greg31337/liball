namespace rtp;

/// <summary>
/// Identifies an RTP header-extension representation.
/// </summary>
public enum RtpHeaderExtensionFormat
{
    /// <summary>The extension uses an opaque RFC 3550 profile format.</summary>
    Raw = 0,

    /// <summary>The extension uses the RFC 8285 one-byte format.</summary>
    OneByte = 1,

    /// <summary>The extension uses the RFC 8285 two-byte format.</summary>
    TwoByte = 2
}
