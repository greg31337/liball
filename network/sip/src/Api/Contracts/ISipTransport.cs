namespace sip;

/// <summary>Defines lifecycle, send, and polling operations for one SIP transport.</summary>
public interface ISipTransport
{
    /// <summary>Binds the transport.</summary>
    /// <param name="options">The common transport options.</param>
    /// <param name="tlsOptions">The TLS options.</param>
    /// <param name="webSocketOptions">The WebSocket options.</param>
    /// <returns>The bound endpoint.</returns>
    SipEndpointAddress BindTransport(SipTransportOptions options, SipTlsOptions tlsOptions, SipWebSocketOptions webSocketOptions);
    /// <summary>Connects to a remote endpoint.</summary>
    /// <param name="remoteAddress">The remote endpoint.</param>
    /// <returns>The connection metadata.</returns>
    SipConnectionInfo ConnectTransport(SipEndpointAddress remoteAddress);
    /// <summary>Sends one transport request.</summary>
    /// <param name="request">The send request.</param>
    /// <returns>The send result.</returns>
    SipSendResult SendTransport(SipSendRequest request);
    /// <summary>Polls the next transport event.</summary>
    /// <returns>The next event.</returns>
    SipTransportEvent PollTransportEvent();
    /// <summary>Closes the transport.</summary>
    void CloseTransport();
}
