namespace sip;

/// <summary>Holds the latest accepted state for one subscription and event package.</summary>
public class SipEventPackageState
{
    /// <summary>The subscription identifier.</summary>
    public string? SubscriptionId;
    /// <summary>The event package token.</summary>
    public string? EventPackage;
    /// <summary>The latest body media type.</summary>
    public SipMediaType? ContentType;
    /// <summary>The latest securely parsed body.</summary>
    public SipXmlBody? XmlBody;
    /// <summary>Indicates that the latest body represented full state.</summary>
    public bool FullState;
    /// <summary>Indicates resource-list content or aggregation metadata.</summary>
    public bool ResourceList;
    /// <summary>The latest package document version, or negative one when absent.</summary>
    public long Version = -1;
    /// <summary>The stable entity or list URI.</summary>
    public string? Entity;
    /// <summary>The latest operation correlation identifier.</summary>
    public string? CorrelationId;
}
