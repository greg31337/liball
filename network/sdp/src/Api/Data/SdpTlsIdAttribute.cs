namespace sdp;

/// <summary>Represents an identifier for a TLS connection or DTLS association.</summary>
public class SdpTlsIdAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;

    /// <summary>Contains the TLS connection or DTLS association identifier.</summary>
    public string Id = string.Empty;
}
