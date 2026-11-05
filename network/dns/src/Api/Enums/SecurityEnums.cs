namespace dns;

/// <summary>Identifies a DNSSEC or transaction-security algorithm number.</summary>
public enum DnsDnssecAlgorithm
{
    /// <summary>Identifies the DELETE marker used by child delegation signaling.</summary>
    Delete = 0,

    /// <summary>Identifies the deprecated RSA with MD5 algorithm.</summary>
    RsaMd5 = 1,

    /// <summary>Identifies the Diffie-Hellman algorithm.</summary>
    DiffieHellman = 2,

    /// <summary>Identifies DSA with SHA-1.</summary>
    DsaSha1 = 3,

    /// <summary>Identifies RSA with SHA-1.</summary>
    RsaSha1 = 5,

    /// <summary>Identifies DSA with SHA-1 for NSEC3.</summary>
    DsaNsec3Sha1 = 6,

    /// <summary>Identifies RSA with SHA-1 for NSEC3.</summary>
    RsaNsec3Sha1 = 7,

    /// <summary>Identifies RSA with SHA-256.</summary>
    RsaSha256 = 8,

    /// <summary>Identifies RSA with SHA-512.</summary>
    RsaSha512 = 10,

    /// <summary>Identifies the deprecated GOST R 34.10-2001 algorithm.</summary>
    EccGost = 12,

    /// <summary>Identifies ECDSA P-256 with SHA-256.</summary>
    EcdsaP256Sha256 = 13,

    /// <summary>Identifies ECDSA P-384 with SHA-384.</summary>
    EcdsaP384Sha384 = 14,

    /// <summary>Identifies Ed25519.</summary>
    Ed25519 = 15,

    /// <summary>Identifies Ed448.</summary>
    Ed448 = 16,

    /// <summary>Identifies SM2 with SM3.</summary>
    Sm2Sm3 = 17,

    /// <summary>Identifies ML-DSA-44.</summary>
    MlDsa44 = 18,

    /// <summary>Identifies GOST R 34.10-2012.</summary>
    EccGost12 = 23,

    /// <summary>Identifies the reserved indirect-key algorithm.</summary>
    Indirect = 252,

    /// <summary>Identifies a private algorithm selected by a domain name.</summary>
    PrivateDns = 253,

    /// <summary>Identifies a private algorithm selected by an object identifier.</summary>
    PrivateOid = 254
}

/// <summary>Identifies a DNSSEC delegation-signer digest algorithm.</summary>
public enum DnsDigestAlgorithm
{
    /// <summary>Identifies reserved digest algorithm zero.</summary>
    Reserved = 0,

    /// <summary>Identifies SHA-1.</summary>
    Sha1 = 1,

    /// <summary>Identifies SHA-256.</summary>
    Sha256 = 2,

    /// <summary>Identifies GOST R 34.11-94.</summary>
    GostR341194 = 3,

    /// <summary>Identifies SHA-384.</summary>
    Sha384 = 4,

    /// <summary>Identifies SM3.</summary>
    Sm3 = 6
}

/// <summary>Identifies an NSEC3 hash algorithm.</summary>
public enum DnsNsec3HashAlgorithm
{
    /// <summary>Identifies reserved hash algorithm zero.</summary>
    Reserved = 0,

    /// <summary>Identifies SHA-1.</summary>
    Sha1 = 1
}

/// <summary>Identifies an error code carried in TSIG data.</summary>
public enum DnsTsigError
{
    /// <summary>Indicates that no TSIG error occurred.</summary>
    NoError = 0,

    /// <summary>Indicates a signature failure.</summary>
    BadSignature = 16,

    /// <summary>Indicates that the key was not recognized.</summary>
    BadKey = 17,

    /// <summary>Indicates that the signature is outside its accepted time window.</summary>
    BadTime = 18,

    /// <summary>Indicates invalid signature truncation.</summary>
    BadTruncation = 22
}

/// <summary>Identifies an Extended DNS Error information code.</summary>
public enum DnsExtendedErrorCode
{
    /// <summary>Identifies another error not represented by a more specific code.</summary>
    Other = 0,

    /// <summary>Indicates an unsupported DNSKEY algorithm.</summary>
    UnsupportedDnskeyAlgorithm = 1,

    /// <summary>Indicates an unsupported DS digest type.</summary>
    UnsupportedDsDigestType = 2,

    /// <summary>Indicates that a stale answer was served.</summary>
    StaleAnswer = 3,

    /// <summary>Indicates a forged answer.</summary>
    ForgedAnswer = 4,

    /// <summary>Indicates indeterminate DNSSEC validation.</summary>
    DnssecIndeterminate = 5,

    /// <summary>Indicates bogus DNSSEC validation.</summary>
    DnssecBogus = 6,

    /// <summary>Indicates an expired signature.</summary>
    SignatureExpired = 7,

    /// <summary>Indicates a signature that is not yet valid.</summary>
    SignatureNotYetValid = 8,

    /// <summary>Indicates a missing DNSKEY.</summary>
    DnskeyMissing = 9,

    /// <summary>Indicates missing RRSIG records.</summary>
    RrsigsMissing = 10,

    /// <summary>Indicates that no zone-key bit is set.</summary>
    NoZoneKeyBitSet = 11,

    /// <summary>Indicates a missing NSEC record.</summary>
    NsecMissing = 12,

    /// <summary>Indicates a cached error.</summary>
    CachedError = 13,

    /// <summary>Indicates that the server is not ready.</summary>
    NotReady = 14,

    /// <summary>Indicates that the response was blocked.</summary>
    Blocked = 15,

    /// <summary>Indicates that the response was censored.</summary>
    Censored = 16,

    /// <summary>Indicates that the response was filtered.</summary>
    Filtered = 17,

    /// <summary>Indicates that the operation was prohibited.</summary>
    Prohibited = 18,

    /// <summary>Indicates that a stale nonexistent-domain answer was served.</summary>
    StaleNameErrorAnswer = 19,

    /// <summary>Indicates that the responding server is not authoritative.</summary>
    NotAuthoritative = 20,

    /// <summary>Indicates that the requested feature is not supported.</summary>
    NotSupported = 21,

    /// <summary>Indicates that no authoritative server was reachable.</summary>
    NoReachableAuthority = 22,

    /// <summary>Indicates a network error.</summary>
    NetworkError = 23,

    /// <summary>Indicates invalid DNS data.</summary>
    InvalidData = 24,

    /// <summary>Indicates a signature that expired before it became valid.</summary>
    SignatureExpiredBeforeValid = 25,

    /// <summary>Indicates that a request was received too early.</summary>
    TooEarly = 26,

    /// <summary>Indicates an unsupported NSEC3 iteration count.</summary>
    UnsupportedNsec3Iterations = 27,

    /// <summary>Indicates an inability to conform to policy.</summary>
    UnableToConformToPolicy = 28,

    /// <summary>Indicates a synthesized answer.</summary>
    Synthesized = 29,

    /// <summary>Indicates an invalid query type.</summary>
    InvalidQueryType = 30,

    /// <summary>Indicates that the request was rate limited.</summary>
    RateLimited = 31,

    /// <summary>Indicates that a quota was exceeded.</summary>
    OverQuota = 32,

    /// <summary>Indicates a configured negative trust anchor.</summary>
    NegativeTrustAnchor = 33,

    /// <summary>Indicates new-delegation-only policy.</summary>
    NewDelegationOnly = 34,

    /// <summary>Indicates blocking by an upstream DNS server.</summary>
    BlockedByUpstream = 35
}

/// <summary>Identifies the DNSSEC security state of an answer or operation.</summary>
public enum DnsSecurityStatus
{
    /// <summary>Indicates that the security state could not be determined.</summary>
    Indeterminate = 0,

    /// <summary>Indicates a validated chain of trust.</summary>
    Secure = 1,

    /// <summary>Indicates a proven absence of a chain of trust.</summary>
    Insecure = 2,

    /// <summary>Indicates failed DNSSEC validation.</summary>
    Bogus = 3,

    /// <summary>Indicates that validation requires an unsupported algorithm.</summary>
    UnsupportedAlgorithm = 4
}

/// <summary>Identifies local policy for a DNSSEC algorithm.</summary>
public enum DnsAlgorithmPolicy
{
    /// <summary>Indicates that the implementation does not support the algorithm.</summary>
    Unsupported = 0,

    /// <summary>Allows both signing and validation with the algorithm.</summary>
    Allowed = 1,

    /// <summary>Allows validation but not signing with the algorithm.</summary>
    ValidateOnly = 2,

    /// <summary>Forbids use of the algorithm.</summary>
    Forbidden = 3
}
