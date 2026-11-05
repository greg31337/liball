namespace pop3;

/// <summary>Provides listener-neutral configuration for POP3 server sessions.</summary>
public class Pop3ServerOptions
{
    /// <summary>The human-readable server greeting text.</summary>
    public string GreetingText = "";

    /// <summary>The text advertised by the IMPLEMENTATION capability.</summary>
    public string ImplementationText = "";

    /// <summary>The transport security mode expected for accepted connections.</summary>
    public Pop3SecurityMode SecurityMode;

    /// <summary>The server TLS handshake options.</summary>
    public Pop3TlsServerOptions Tls = new Pop3TlsServerOptions();

    /// <summary>A custom TLS provider, or <c>null</c> to use the library default.</summary>
    public IPop3ServerTlsProvider? TlsProvider;

    /// <summary>The authentication provider, or <c>null</c> until configured.</summary>
    public IPop3AuthenticationProvider? AuthenticationProvider;

    /// <summary>The mailbox provider, or <c>null</c> until configured.</summary>
    public IPop3MailboxProvider? MailboxProvider;

    /// <summary>The available server-side SASL mechanism factories.</summary>
    public IPop3SaslServerFactory[] SaslFactories = new IPop3SaslServerFactory[0];

    /// <summary>The minimum enabled inactivity period in milliseconds.</summary>
    public int InactivityTimeoutMilliseconds = Pop3Defaults.DefaultServerInactivityTimeoutMilliseconds;

    /// <summary>The maximum stream read time in milliseconds.</summary>
    public int ReadTimeoutMilliseconds = Pop3Defaults.DefaultReadTimeoutMilliseconds;

    /// <summary>The maximum stream write time in milliseconds.</summary>
    public int WriteTimeoutMilliseconds = Pop3Defaults.DefaultWriteTimeoutMilliseconds;

    /// <summary>The maximum authentication exchange time in milliseconds.</summary>
    public int AuthenticationTimeoutMilliseconds = Pop3Defaults.DefaultAuthenticationTimeoutMilliseconds;

    /// <summary>The internal transport buffer size in bytes.</summary>
    public int BufferSize = Pop3Defaults.DefaultBufferSize;

    /// <summary>The maximum accepted unbounded message-line size in octets.</summary>
    public int MaximumMessageLineOctets = Pop3Defaults.DefaultMaximumMessageLineOctets;

    /// <summary>The maximum accepted SASL payload-line size in octets.</summary>
    public int MaximumSaslLineOctets = Pop3Defaults.DefaultMaximumSaslLineOctets;

    /// <summary>Indicates whether TOP support is enabled.</summary>
    public bool EnableTop = true;

    /// <summary>Indicates whether USER and PASS authentication is enabled.</summary>
    public bool EnableUserPassword = true;

    /// <summary>Indicates whether legacy APOP authentication is enabled.</summary>
    public bool EnableApop;

    /// <summary>Indicates whether credentials may be accepted over an unprotected transport.</summary>
    public bool AllowUnprotectedCredentials;

    /// <summary>Indicates whether command pipelining is enabled.</summary>
    public bool EnablePipelining = true;

    /// <summary>Indicates whether UIDL support is enabled.</summary>
    public bool EnableUidl = true;

    /// <summary>Indicates whether extended response codes are enabled.</summary>
    public bool EnableResponseCodes = true;

    /// <summary>Indicates whether UTF8 message mode is enabled.</summary>
    public bool EnableUtf8;

    /// <summary>Indicates whether UTF-8 authentication identifiers are enabled.</summary>
    public bool EnableUtf8User;

    /// <summary>The language provider, or <c>null</c> when LANG is disabled.</summary>
    public IPop3LanguageProvider? LanguageProvider;

    /// <summary>The default language tag, or <c>null</c> to use provider policy.</summary>
    public string? DefaultLanguage;

    /// <summary>The registered custom server extensions.</summary>
    public IPop3ServerExtension[] Extensions = new IPop3ServerExtension[0];

    /// <summary>A protocol logger, or <c>null</c> when protocol logging is disabled.</summary>
    public IPop3ProtocolLogger? Logger;

    /// <summary>A transfer observer, or <c>null</c> when progress reports are not required.</summary>
    public IPop3TransferObserver? TransferObserver;
}
