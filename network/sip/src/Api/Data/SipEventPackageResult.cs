namespace sip;

/// <summary>Holds the outcome of processing one event-package body.</summary>
public class SipEventPackageResult
{
    /// <summary>The detached processing request.</summary>
    public SipEventPackageRequest? Request;
    /// <summary>The event package token.</summary>
    public string? EventPackage;
    /// <summary>The accepted event body media type.</summary>
    public SipMediaType? ContentType;
    /// <summary>The securely parsed body for a built-in package.</summary>
    public SipXmlBody? XmlBody;
    /// <summary>Indicates that the body was accepted.</summary>
    public bool Accepted;
    /// <summary>The protocol status corresponding to the outcome.</summary>
    public int StatusCode;
    /// <summary>Indicates that a built-in package processed the body.</summary>
    public bool BuiltIn;
    /// <summary>Indicates that the body represents full package state.</summary>
    public bool FullState;
    /// <summary>Indicates that the body represents a partial state update.</summary>
    public bool PartialState;
    /// <summary>Indicates resource-list content or aggregation metadata.</summary>
    public bool ResourceList;
    /// <summary>The package document version, or negative one when absent.</summary>
    public long Version = -1;
    /// <summary>The entity or list URI identified by the body.</summary>
    public string? Entity;
    /// <summary>The failure message.</summary>
    public string? ErrorMessage;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
