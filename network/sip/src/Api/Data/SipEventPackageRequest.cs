namespace sip;

/// <summary>Describes one event-package body processing operation.</summary>
public class SipEventPackageRequest
{
    /// <summary>The subscription identifier used to maintain package state.</summary>
    public string? SubscriptionId;
    /// <summary>The event package token.</summary>
    public string? EventPackage;
    /// <summary>The event body bytes.</summary>
    public byte[] Body = new byte[0];
    /// <summary>The event body media type.</summary>
    public SipMediaType? ContentType;
    /// <summary>The secure body parsing options.</summary>
    public SipParseOptions Options = new SipParseOptions();
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
