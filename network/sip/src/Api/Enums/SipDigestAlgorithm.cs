namespace sip;

/// <summary>Identifies a SIP Digest algorithm.</summary>
public enum SipDigestAlgorithm
{
    /// <summary>The algorithm is unknown.</summary>
    Unknown = 0,
    /// <summary>MD5 compatibility mode.</summary>
    Md5 = 1,
    /// <summary>MD5 session mode.</summary>
    Md5Sess = 2,
    /// <summary>SHA-256.</summary>
    Sha256 = 3,
    /// <summary>SHA-256 session mode.</summary>
    Sha256Sess = 4,
    /// <summary>SHA-512/256.</summary>
    Sha512256 = 5,
    /// <summary>SHA-512/256 session mode.</summary>
    Sha512256Sess = 6
}
