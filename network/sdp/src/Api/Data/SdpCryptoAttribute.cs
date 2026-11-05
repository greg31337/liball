using System.Collections.Generic;

namespace sdp;

/// <summary>Represents a legacy SDES crypto attribute.</summary>
public class SdpCryptoAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;
    /// <summary>Contains the crypto tag.</summary>
    public int Tag = 0;
    /// <summary>Contains the registered or extension crypto-suite token.</summary>
    public string Suite = string.Empty;
    /// <summary>Contains sensitive key-parameter tokens in source order.</summary>
    public List<string> KeyParameters = new List<string>();
    /// <summary>Contains session-parameter tokens in source order.</summary>
    public List<string> SessionParameters = new List<string>();
}
