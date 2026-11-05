namespace sdp;

/// <summary>Maps an offered RTP payload type to a negotiated payload type.</summary>
public class SdpPayloadMapping
{
    /// <summary>Contains the zero-based media index.</summary>
    public int MediaIndex = -1;
    /// <summary>Contains the offered payload type.</summary>
    public int OfferedPayloadType = 0;
    /// <summary>Contains the negotiated payload type.</summary>
    public int NegotiatedPayloadType = 0;
    /// <summary>Contains the matched encoding name.</summary>
    public string EncodingName = string.Empty;
}
