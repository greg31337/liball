namespace pop3;

/// <summary>Contains raw and interpreted capabilities for the current POP3 session.</summary>
public class Pop3CapabilitySet
{
    /// <summary>The raw capability entries in the order supplied by the peer.</summary>
    public Pop3Capability[] Entries = new Pop3Capability[0];

    /// <summary>Indicates whether the TOP command is advertised.</summary>
    public bool SupportsTop;

    /// <summary>Indicates whether USER and PASS authentication is advertised.</summary>
    public bool SupportsUser;

    /// <summary>The advertised SASL mechanisms in peer preference order.</summary>
    public string[] SaslMechanisms = new string[0];

    /// <summary>Indicates whether extended response codes are advertised.</summary>
    public bool SupportsResponseCodes;

    /// <summary>Indicates whether a LOGIN-DELAY value is advertised.</summary>
    public bool HasLoginDelay;

    /// <summary>The advertised minimum delay between login attempts, in seconds.</summary>
    public int LoginDelaySeconds;

    /// <summary>Indicates whether the advertised login delay is tracked per user.</summary>
    public bool LoginDelayPerUser;

    /// <summary>Indicates whether command pipelining is advertised.</summary>
    public bool SupportsPipelining;

    /// <summary>Indicates whether an EXPIRE policy is advertised.</summary>
    public bool HasExpire;

    /// <summary>Indicates whether the advertised expiration policy is <c>NEVER</c>.</summary>
    public bool ExpireNever;

    /// <summary>The advertised minimum retention period in days when expiration is not <c>NEVER</c>.</summary>
    public int ExpireDays;

    /// <summary>Indicates whether the advertised expiration policy varies per user.</summary>
    public bool ExpirePerUser;

    /// <summary>Indicates whether unique-id listing is advertised.</summary>
    public bool SupportsUidl;

    /// <summary>The advertised server implementation text, or <c>null</c> when absent.</summary>
    public string? Implementation;

    /// <summary>Indicates whether explicit TLS upgrade is advertised.</summary>
    public bool SupportsStls;

    /// <summary>Indicates whether authentication response codes are advertised.</summary>
    public bool SupportsAuthenticationResponseCode;

    /// <summary>Indicates whether UTF8 message mode is advertised.</summary>
    public bool SupportsUtf8;

    /// <summary>Indicates whether UTF-8 authentication identifiers are advertised.</summary>
    public bool SupportsUtf8User;

    /// <summary>Indicates whether language listing and selection are advertised.</summary>
    public bool SupportsLang;
}
