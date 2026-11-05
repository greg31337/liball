using System.Collections.Generic;

namespace sdp;

/// <summary>Collects effective TLS, DTLS, and legacy SDES signaling.</summary>
public class SdpDtlsDescription
{
    /// <summary>Contains certificate fingerprints in preference order.</summary>
    public List<SdpFingerprintAttribute> Fingerprints = new List<SdpFingerprintAttribute>();
    /// <summary>Identifies a recognized setup role.</summary>
    public SdpDtlsSetupRole SetupRole = SdpDtlsSetupRole.Unknown;
    /// <summary>Contains the original setup-role token.</summary>
    public string SetupRoleToken = string.Empty;
    /// <summary>Contains the connection-reuse token.</summary>
    public string ConnectionRole = string.Empty;
    /// <summary>Contains the TLS identifier.</summary>
    public string TlsId = string.Empty;
    /// <summary>Contains legacy SDES crypto attributes in source order.</summary>
    public List<SdpCryptoAttribute> CryptoAttributes = new List<SdpCryptoAttribute>();
}
