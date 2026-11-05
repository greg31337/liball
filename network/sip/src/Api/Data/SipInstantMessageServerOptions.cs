namespace sip;

/// <summary>Holds pager-mode MESSAGE receiving and content policy.</summary>
public class SipInstantMessageServerOptions
{
    /// <summary>The maximum accepted body size in bytes.</summary>
    public int MaxBodyBytes = SipDefaults.MaxBodyBytes;
    /// <summary>Requires configured authentication.</summary>
    public bool RequireAuthentication;
    /// <summary>Allows MESSAGE requests outside a dialog.</summary>
    public bool AllowOutOfDialog = true;
    /// <summary>Allows MESSAGE requests within a dialog.</summary>
    public bool AllowInDialog = true;
    /// <summary>The accepted media types, or an empty array to accept any valid media type.</summary>
    public SipMediaType[] AcceptedContentTypes = new SipMediaType[0];
}
