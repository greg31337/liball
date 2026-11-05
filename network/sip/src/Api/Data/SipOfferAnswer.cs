namespace sip;

/// <summary>Holds one offer/answer exchange and negotiated body state.</summary>
public class SipOfferAnswer
{
    /// <summary>The offer/answer state.</summary>
    public SipOfferAnswerState State;
    /// <summary>The local offer bytes.</summary>
    public byte[] LocalOffer = new byte[0];
    /// <summary>The remote offer bytes.</summary>
    public byte[] RemoteOffer = new byte[0];
    /// <summary>The local answer bytes.</summary>
    public byte[] LocalAnswer = new byte[0];
    /// <summary>The remote answer bytes.</summary>
    public byte[] RemoteAnswer = new byte[0];
    /// <summary>The body media type.</summary>
    public SipMediaType? ContentType;
    /// <summary>The negotiated body bytes.</summary>
    public byte[] NegotiatedBody = new byte[0];
    /// <summary>The media type of the negotiated body.</summary>
    public SipMediaType? NegotiatedContentType;
}
