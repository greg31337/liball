using System.Collections.Generic;

namespace sdp;

/// <summary>Represents a certificate fingerprint attribute.</summary>
public class SdpFingerprintAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;
    /// <summary>Contains the registered or extension hash-algorithm token.</summary>
    public string Algorithm = string.Empty;
    /// <summary>Contains the original colon-separated fingerprint.</summary>
    public string Fingerprint = string.Empty;
    /// <summary>Contains parsed fingerprint octets.</summary>
    public List<byte> Digest = new List<byte>();
}
