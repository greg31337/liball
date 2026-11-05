namespace sip;

/// <summary>Holds the outcome of a SIP authentication operation.</summary>
public class SipAuthenticationResult
{
    /// <summary>The authentication outcome.</summary>
    public SipAuthenticationStatus Status;
    /// <summary>The authorized or challenged message.</summary>
    public SipMessage? Message;
    /// <summary>The generated Digest authorization.</summary>
    public SipDigestAuthorization? DigestAuthorization;
    /// <summary>The Bearer challenge or validated token metadata, without access-token disclosure.</summary>
    public SipBearerToken? BearerToken;
    /// <summary>The mutually authenticated peer certificate identity.</summary>
    public SipCertificateIdentity? CertificateIdentity;
    /// <summary>The application certificate-policy decision.</summary>
    public SipAuthorizationDecision? CertificateDecision;
    /// <summary>The established identity.</summary>
    public string? Identity;
    /// <summary>Indicates that a bounded retry is allowed.</summary>
    public bool RetryAllowed;
    /// <summary>The failure message.</summary>
    public string? ErrorMessage;
    /// <summary>The operation correlation identifier.</summary>
    public string? CorrelationId;
}
