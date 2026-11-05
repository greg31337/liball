namespace cookies;

/// <summary>
/// Identifies a stable cookie processing outcome or failure reason.
/// </summary>
public enum CookieErrorCode
{
    /// <summary>
    /// No error or noteworthy condition occurred.
    /// </summary>
    None = 0,

    /// <summary>
    /// The input does not follow the required cookie syntax.
    /// </summary>
    MalformedSyntax = 1,

    /// <summary>
    /// The input contains a header injection character.
    /// </summary>
    HeaderInjection = 2,

    /// <summary>
    /// The cookie name is invalid.
    /// </summary>
    InvalidName = 3,

    /// <summary>
    /// The cookie value is invalid.
    /// </summary>
    InvalidValue = 4,

    /// <summary>
    /// A cookie attribute is invalid.
    /// </summary>
    InvalidAttribute = 5,

    /// <summary>
    /// A cookie date is invalid.
    /// </summary>
    InvalidDate = 6,

    /// <summary>
    /// An unrecognized cookie attribute was preserved or ignored.
    /// </summary>
    UnknownAttribute = 7,

    /// <summary>
    /// A recognized cookie attribute was repeated.
    /// </summary>
    DuplicateAttribute = 8,

    /// <summary>
    /// A malformed input segment was ignored.
    /// </summary>
    IgnoredSegment = 9,

    /// <summary>
    /// Compatible processing recovered from a deployed deviation.
    /// </summary>
    CompatibilityRecovery = 10,

    /// <summary>
    /// The origin or request uses an unsupported URI scheme.
    /// </summary>
    UnsupportedScheme = 11,

    /// <summary>
    /// The origin or request host is absent or malformed.
    /// </summary>
    InvalidHost = 12,

    /// <summary>
    /// The host cannot be converted to valid IDNA form.
    /// </summary>
    InvalidIdna = 13,

    /// <summary>
    /// The cookie domain does not match the origin or request host.
    /// </summary>
    DomainMismatch = 14,

    /// <summary>
    /// The cookie domain is a public suffix.
    /// </summary>
    PublicSuffix = 15,

    /// <summary>
    /// Public-suffix protection is required but unavailable.
    /// </summary>
    PublicSuffixListUnavailable = 16,

    /// <summary>
    /// The cookie path is invalid or does not match the request path.
    /// </summary>
    InvalidPath = 17,

    /// <summary>
    /// A Secure cookie operation used an origin that is not secure.
    /// </summary>
    InsecureSecureCookie = 18,

    /// <summary>
    /// A non-HTTP API attempted a forbidden HttpOnly cookie operation.
    /// </summary>
    HttpOnlyApiViolation = 19,

    /// <summary>
    /// An insecure cookie attempted to overlay a Secure cookie.
    /// </summary>
    SecureOverlayViolation = 20,

    /// <summary>
    /// SameSite requirements prevent the cookie operation.
    /// </summary>
    SameSiteViolation = 21,

    /// <summary>
    /// Cookie name prefix requirements prevent the cookie operation.
    /// </summary>
    PrefixViolation = 22,

    /// <summary>
    /// A partitioned cookie operation lacks required partition context.
    /// </summary>
    MissingPartitionContext = 23,

    /// <summary>
    /// The cookie partition does not match the request partition.
    /// </summary>
    PartitionMismatch = 24,

    /// <summary>
    /// The cookie is expired and cannot be returned or imported.
    /// </summary>
    Expired = 25,

    /// <summary>
    /// An expired accepted cookie requested deletion of its identity.
    /// </summary>
    ExpiredDeletion = 26,

    /// <summary>
    /// Application policy rejected the cookie operation.
    /// </summary>
    PolicyRejection = 27,

    /// <summary>
    /// A cookie was evicted to satisfy a storage quota.
    /// </summary>
    QuotaEviction = 28,

    /// <summary>
    /// The combined cookie name and value exceed the configured limit.
    /// </summary>
    NameValueLimitExceeded = 29,

    /// <summary>
    /// A cookie attribute exceeds the configured limit.
    /// </summary>
    AttributeLimitExceeded = 30,

    /// <summary>
    /// An input line exceeds the configured limit.
    /// </summary>
    InputLimitExceeded = 31,

    /// <summary>
    /// A request Cookie field exceeds the configured limit.
    /// </summary>
    HeaderLimitExceeded = 32,

    /// <summary>
    /// A domain contains more cookies than the configured quota permits.
    /// </summary>
    PerDomainLimitExceeded = 33,

    /// <summary>
    /// The store contains more cookies than the configured quota permits.
    /// </summary>
    TotalLimitExceeded = 34,

    /// <summary>
    /// The requested persistent lifetime exceeds the configured limit.
    /// </summary>
    LifetimeLimitExceeded = 35,

    /// <summary>
    /// A cookie model cannot be serialized safely.
    /// </summary>
    SerializationFailure = 36,

    /// <summary>
    /// A cookie store operation failed.
    /// </summary>
    StoreFailure = 37,

    /// <summary>
    /// A snapshot is malformed or contains invalid data.
    /// </summary>
    InvalidSnapshot = 38,

    /// <summary>
    /// A snapshot uses an unsupported schema version.
    /// </summary>
    UnsupportedSnapshotVersion = 39,

    /// <summary>
    /// Imported data contains the same cookie identity more than once.
    /// </summary>
    DuplicateIdentity = 40,

    /// <summary>
    /// An import or export cannot preserve all cookie state.
    /// </summary>
    LossyImport = 41,

    /// <summary>
    /// A Netscape cookie-file line is malformed.
    /// </summary>
    MalformedNetscapeLine = 42,

    /// <summary>
    /// Cookie service options or dependencies are invalid.
    /// </summary>
    InvalidConfiguration = 43
}
