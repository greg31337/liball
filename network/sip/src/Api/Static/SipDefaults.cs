namespace sip;

/// <summary>
/// Provides interoperable protocol defaults and conservative resource limits.
/// </summary>
public static class SipDefaults
{
    /// <summary>The initial Max-Forwards value for a generated request.</summary>
    public const int MaxForwards = 70;

    /// <summary>The default registration lifetime in seconds.</summary>
    public const int RegistrationExpiresSeconds = 3600;

    /// <summary>The default subscription lifetime in seconds.</summary>
    public const int SubscriptionExpiresSeconds = 3600;

    /// <summary>The default SIP transaction T1 interval in milliseconds.</summary>
    public const int T1Milliseconds = 500;

    /// <summary>The default SIP transaction T2 interval in milliseconds.</summary>
    public const int T2Milliseconds = 4000;

    /// <summary>The default SIP transaction T4 interval in milliseconds.</summary>
    public const int T4Milliseconds = 5000;

    /// <summary>The message size above which UDP should not be selected automatically.</summary>
    public const int UdpSwitchThresholdBytes = 1300;

    /// <summary>The maximum accepted SIP message size in bytes.</summary>
    public const int MaxMessageBytes = 1048576;

    /// <summary>The maximum accepted SIP body size in bytes.</summary>
    public const int MaxBodyBytes = 1048576;

    /// <summary>The maximum aggregate header section size in bytes.</summary>
    public const int MaxHeaderBytes = 65536;

    /// <summary>The maximum start-line size in bytes.</summary>
    public const int MaxStartLineBytes = 8192;

    /// <summary>The maximum individual header line size in bytes.</summary>
    public const int MaxHeaderLineBytes = 8192;

    /// <summary>The maximum number of header fields in one message.</summary>
    public const int MaxHeaderCount = 256;

    /// <summary>The maximum accepted URI size in bytes.</summary>
    public const int MaxUriBytes = 4096;

    /// <summary>The maximum number of parameters in one parsed value.</summary>
    public const int MaxParameterCount = 128;

    /// <summary>The maximum number of elements in a parsed list.</summary>
    public const int MaxListValueCount = 256;

    /// <summary>The maximum nested multipart depth.</summary>
    public const int MaxMimeDepth = 16;

    /// <summary>The maximum XML element depth.</summary>
    public const int MaxXmlDepth = 64;

    /// <summary>The maximum number of DNS records accepted in one result.</summary>
    public const int MaxDnsRecordCount = 256;

    /// <summary>The maximum number of followed DNS CNAME records.</summary>
    public const int MaxDnsCnameDepth = 16;

    /// <summary>The maximum number of automatically followed redirects.</summary>
    public const int MaxRedirectCount = 8;

    /// <summary>The maximum number of authentication attempts for one operation.</summary>
    public const int MaxAuthenticationAttempts = 3;

    /// <summary>The default maximum number of queued events or messages.</summary>
    public const int MaxQueueCount = 1024;

    /// <summary>The default maximum number of live transactions.</summary>
    public const int MaxTransactionCount = 10000;

    /// <summary>The default maximum number of live dialogs.</summary>
    public const int MaxDialogCount = 10000;

    /// <summary>The default maximum number of live reliable connections.</summary>
    public const int MaxConnectionCount = 1024;

    /// <summary>The default upper interval for reliable outbound keepalives in seconds.</summary>
    public const int OutboundReliableKeepAliveSeconds = 120;

    /// <summary>The time allowed for a reliable outbound keepalive response.</summary>
    public const int OutboundPongTimeoutMilliseconds = 10000;

    /// <summary>The recovery base in seconds when every outbound flow has failed.</summary>
    public const int OutboundAllFailedRetrySeconds = 30;

    /// <summary>The recovery base in seconds while another outbound flow remains active.</summary>
    public const int OutboundPartialFailureRetrySeconds = 90;

    /// <summary>The maximum outbound flow recovery delay in seconds.</summary>
    public const int OutboundMaximumRetrySeconds = 1800;

    /// <summary>The default maximum number of bindings for one address of record.</summary>
    public const int MaxRegistrationBindingCount = 1000;

    /// <summary>The default maximum number of proxy branches for one request.</summary>
    public const int MaxProxyBranchCount = 64;
}
