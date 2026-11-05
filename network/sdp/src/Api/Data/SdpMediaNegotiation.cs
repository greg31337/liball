using System.Collections.Generic;

namespace sdp;

/// <summary>Contains the negotiated outcome for one media description.</summary>
public class SdpMediaNegotiation
{
    /// <summary>Contains the zero-based media index.</summary>
    public int MediaIndex = -1;
    /// <summary>Contains the media identifier when available.</summary>
    public string MediaId = string.Empty;
    /// <summary>Identifies the recognized negotiated media kind.</summary>
    public SdpMediaKind MediaKind = SdpMediaKind.Unknown;
    /// <summary>Contains the registered or extension negotiated media-type token.</summary>
    public string MediaType = string.Empty;
    /// <summary>Contains the negotiated transport-protocol token.</summary>
    public string Protocol = string.Empty;
    /// <summary>Contains the zero-based media index supplying the negotiated transport.</summary>
    public int TransportMediaIndex = -1;
    /// <summary>Contains the zero-based negotiated BUNDLE-group index, or <c>-1</c> when unbundled.</summary>
    public int BundleGroupIndex = -1;
    /// <summary>Indicates whether the media description was accepted.</summary>
    public bool Accepted = false;
    /// <summary>Identifies the direction supplied by the offer.</summary>
    public SdpDirection OfferedDirection = SdpDirection.Unknown;
    /// <summary>Identifies the negotiated media direction.</summary>
    public SdpDirection Direction = SdpDirection.Unknown;
    /// <summary>Contains negotiated codecs in preference order.</summary>
    public List<SdpCodec> Codecs = new List<SdpCodec>();
    /// <summary>Contains payload-type mappings in format order.</summary>
    public List<SdpPayloadMapping> PayloadMappings = new List<SdpPayloadMapping>();
    /// <summary>Contains header-extension mappings in identifier order.</summary>
    public List<SdpHeaderExtensionMapping> HeaderExtensionMappings = new List<SdpHeaderExtensionMapping>();
    /// <summary>Contains negotiated SCTP signaling for application media when available.</summary>
    public SdpSctpDescription? Sctp = null;
    /// <summary>Contains the effective negotiated media projection when available.</summary>
    public SdpEffectiveMedia? EffectiveMedia = null;
    /// <summary>Contains rejection information when the media was not accepted.</summary>
    public SdpRejection? Rejection = null;
}
