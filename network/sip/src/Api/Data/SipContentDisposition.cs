namespace sip;

/// <summary>
/// Holds a recognized body disposition and its original token.
/// </summary>
public class SipContentDisposition
{
    /// <summary>The recognized body disposition.</summary>
    public SipBodyDisposition Disposition;

    /// <summary>The original disposition token, including an extension disposition.</summary>
    public string? DispositionToken;

    /// <summary>The ordered disposition parameters.</summary>
    public SipUriParameter[] Parameters = new SipUriParameter[0];
}
