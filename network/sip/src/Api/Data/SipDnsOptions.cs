namespace sip;

/// <summary>Holds DNS resolver configuration and limits.</summary>
public class SipDnsOptions
{
    /// <summary>The configured name-server endpoints.</summary>
    public SipEndpointAddress[] NameServers = new SipEndpointAddress[0];
    /// <summary>The per-attempt timeout in milliseconds.</summary>
    public int TimeoutMilliseconds = 2000;
    /// <summary>The maximum query attempts.</summary>
    public int MaxAttempts = 3;
    /// <summary>The maximum accepted records.</summary>
    public int MaxRecordCount = SipDefaults.MaxDnsRecordCount;
    /// <summary>The maximum followed CNAME depth.</summary>
    public int MaxCnameDepth = SipDefaults.MaxDnsCnameDepth;
    /// <summary>Enables positive and negative caching.</summary>
    public bool EnableCache = true;
    /// <summary>Enables ENUM resolution.</summary>
    public bool EnableEnum;
}
