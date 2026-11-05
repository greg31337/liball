namespace sip;

/// <summary>Identifies a DNS record kind used by SIP resolution.</summary>
public enum SipDnsRecordKind
{
    /// <summary>The record kind is unknown.</summary>
    Unknown = 0,
    /// <summary>A NAPTR record.</summary>
    Naptr = 1,
    /// <summary>An SRV record.</summary>
    Srv = 2,
    /// <summary>An IPv4 address record.</summary>
    A = 3,
    /// <summary>An IPv6 address record.</summary>
    Aaaa = 4,
    /// <summary>A canonical-name record.</summary>
    Cname = 5
}
