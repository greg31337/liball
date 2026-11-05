namespace sip;

/// <summary>Holds a parsed Digest authentication challenge.</summary>
public class SipDigestChallenge
{
    /// <summary>The authentication scheme token.</summary>
    public string? Scheme;
    /// <summary>The opaque value following an unrecognized authentication scheme.</summary>
    public string? SchemeData;
    /// <summary>The realm value.</summary>
    public string? Realm;
    /// <summary>The nonce value.</summary>
    public string? Nonce;
    /// <summary>The opaque value.</summary>
    public string? Opaque;
    /// <summary>The selected digest algorithm.</summary>
    public SipDigestAlgorithm Algorithm;
    /// <summary>The original algorithm token, including an unrecognized algorithm.</summary>
    public string? AlgorithmToken;
    /// <summary>The offered quality-of-protection values.</summary>
    public SipDigestQuality[] Qualities = new SipDigestQuality[0];
    /// <summary>The offered quality-of-protection tokens, including unrecognized values.</summary>
    public string[] QualityTokens = new string[0];
    /// <summary>The protected domain value.</summary>
    public string? Domain;
    /// <summary>Indicates a stale nonce.</summary>
    public bool Stale;
    /// <summary>Indicates that the stale parameter was supplied.</summary>
    public bool HasStale;
    /// <summary>Indicates UTF-8 credentials.</summary>
    public bool Utf8;
    /// <summary>Indicates userhash use.</summary>
    public bool UserHash;
    /// <summary>Indicates that the userhash parameter was supplied.</summary>
    public bool HasUserHash;
    /// <summary>The ordered unknown authentication parameters.</summary>
    public SipUriParameter[] Parameters = new SipUriParameter[0];
    /// <summary>The complete original challenge value when preservation was requested.</summary>
    public string? OriginalText;
}
