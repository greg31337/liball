namespace sip;

/// <summary>Holds a Digest authorization or Authentication-Info value.</summary>
public class SipDigestAuthorization
{
    /// <summary>The authentication scheme token, or null for Authentication-Info.</summary>
    public string? Scheme;
    /// <summary>The opaque value following an unrecognized authentication scheme.</summary>
    public string? SchemeData;
    /// <summary>The authentication user name or userhash.</summary>
    public string? UserName;
    /// <summary>The RFC 5987 extended user name value, when supplied instead of username.</summary>
    public string? UserNameExtended;
    /// <summary>The realm value.</summary>
    public string? Realm;
    /// <summary>The nonce value.</summary>
    public string? Nonce;
    /// <summary>The request-target digest URI.</summary>
    public string? DigestUri;
    /// <summary>The hexadecimal digest response.</summary>
    public string? Response;
    /// <summary>The digest algorithm.</summary>
    public SipDigestAlgorithm Algorithm;
    /// <summary>The original algorithm token, including an unrecognized algorithm.</summary>
    public string? AlgorithmToken;
    /// <summary>The selected quality of protection.</summary>
    public SipDigestQuality Quality;
    /// <summary>The original quality-of-protection token, including an unrecognized value.</summary>
    public string? QualityToken;
    /// <summary>The nonce count, or negative one when absent.</summary>
    public long NonceCount = -1;
    /// <summary>The client nonce.</summary>
    public string? ClientNonce;
    /// <summary>The opaque value.</summary>
    public string? Opaque;
    /// <summary>The next nonce supplied by Authentication-Info.</summary>
    public string? NextNonce;
    /// <summary>Indicates userhash use.</summary>
    public bool UserHash;
    /// <summary>Indicates that the userhash parameter was supplied.</summary>
    public bool HasUserHash;
    /// <summary>The ordered unknown authentication parameters.</summary>
    public SipUriParameter[] Parameters = new SipUriParameter[0];
    /// <summary>The complete original credential or Authentication-Info value when preservation was requested.</summary>
    public string? OriginalText;
}
