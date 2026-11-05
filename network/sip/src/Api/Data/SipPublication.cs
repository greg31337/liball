namespace sip;

/// <summary>Holds one stored event-state publication.</summary>
public class SipPublication
{
    /// <summary>The stable publication identifier.</summary>
    public string? PublicationId;
    /// <summary>The published resource.</summary>
    public SipUri? Target;
    /// <summary>The publisher identity.</summary>
    public SipUri? Sender;
    /// <summary>The event package token.</summary>
    public string? EventPackage;
    /// <summary>The current opaque entity tag.</summary>
    public string? EntityTag;
    /// <summary>The published event-state body.</summary>
    public byte[] Body = new byte[0];
    /// <summary>The event-state body media type.</summary>
    public SipMediaType? ContentType;
    /// <summary>The monotonic expiration timestamp.</summary>
    public long ExpiresAtMilliseconds = -1;
    /// <summary>The last monotonic update timestamp.</summary>
    public long UpdatedAtMilliseconds = -1;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
