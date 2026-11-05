namespace sip;

/// <summary>Holds top-level SIP stack component options and limits.</summary>
public class SipStackOptions
{
    /// <summary>The common parse options.</summary>
    public SipParseOptions ParseOptions = new SipParseOptions();
    /// <summary>The common write options.</summary>
    public SipWriteOptions WriteOptions = new SipWriteOptions();
    /// <summary>The DNS options.</summary>
    public SipDnsOptions DnsOptions = new SipDnsOptions();
    /// <summary>The transaction options.</summary>
    public SipTransactionOptions TransactionOptions = new SipTransactionOptions();
    /// <summary>The dialog options.</summary>
    public SipDialogOptions DialogOptions = new SipDialogOptions();
    /// <summary>The shared TLS options for configured TLS transports.</summary>
    public SipTlsOptions TlsOptions = new SipTlsOptions();
    /// <summary>The shared WebSocket options for configured WebSocket transports.</summary>
    public SipWebSocketOptions WebSocketOptions = new SipWebSocketOptions();
    /// <summary>The configured transports.</summary>
    public SipTransportOptions[] Transports = new SipTransportOptions[0];
    /// <summary>The maximum unified event count.</summary>
    public int MaxEventCount = SipDefaults.MaxQueueCount;
}
