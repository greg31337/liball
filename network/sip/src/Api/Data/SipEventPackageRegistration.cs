namespace sip;

/// <summary>Describes one registered SIP event package.</summary>
public class SipEventPackageRegistration
{
    /// <summary>The event package token.</summary>
    public string? EventPackage;
    /// <summary>The accepted body media types.</summary>
    public SipMediaType[] ContentTypes = new SipMediaType[0];
    /// <summary>Indicates a library-provided package.</summary>
    public bool BuiltIn;
    /// <summary>Indicates support for resource-list aggregation metadata.</summary>
    public bool SupportsResourceLists;
}
