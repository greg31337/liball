namespace sip;

/// <summary>Coordinates SIP stack lifecycle, dispatch, sending, and unified events.</summary>
public interface ISipEndpoint
{
    /// <summary>Registers an application-provided transport before shutdown.</summary>
    /// <param name="transport">The transport implementation.</param>
    void RegisterEndpointTransport(ISipTransport transport);
    /// <summary>Registers an application-provided transport content coding.</summary>
    /// <param name="contentCoding">The content-coding implementation.</param>
    /// <returns>True when the coding was registered.</returns>
    bool RegisterEndpointContentCoding(
        ISipTransportContentCoding contentCoding);
    /// <summary>Registers an application SIP extension.</summary>
    /// <param name="extension">The extension implementation.</param>
    /// <returns>True when the extension was registered.</returns>
    bool RegisterEndpointExtension(ISipExtension extension);
    /// <summary>Removes an application SIP extension.</summary>
    /// <param name="extension">The extension implementation.</param>
    /// <returns>True when the extension was removed.</returns>
    bool RemoveEndpointExtension(ISipExtension extension);
    /// <summary>Configures the composed authentication client and server.</summary>
    /// <param name="credentialProvider">The shared credential provider.</param>
    void ConfigureEndpointAuthentication(
        ISipCredentialProvider credentialProvider);
    /// <summary>Starts the endpoint.</summary>
    /// <param name="options">The stack options.</param>
    void StartEndpoint(SipStackOptions options);
    /// <summary>Dispatches one received transport message.</summary>
    /// <param name="message">The received message envelope.</param>
    void ReceiveEndpointMessage(SipTransportMessage message);
    /// <summary>Sends one endpoint request.</summary>
    /// <param name="request">The send request.</param>
    /// <returns>The send result.</returns>
    SipSendResult SendEndpointMessage(SipSendRequest request);
    /// <summary>Advances endpoint transaction timers.</summary>
    /// <param name="nowMilliseconds">The monotonic timestamp.</param>
    void AdvanceEndpointTime(long nowMilliseconds);
    /// <summary>Polls the next unified stack event.</summary>
    /// <returns>The next event.</returns>
    SipStackEvent PollStackEvent();
    /// <summary>Stops the endpoint.</summary>
    /// <param name="cancellation">The shutdown request.</param>
    void StopEndpoint(SipCancellation cancellation);
    /// <summary>Gets the current endpoint lifecycle state.</summary>
    /// <returns>The current state.</returns>
    SipEndpointState GetEndpointState();
    /// <summary>Gets the composed transport manager.</summary>
    /// <returns>The endpoint transport manager.</returns>
    ISipTransportManager GetEndpointTransportManager();
    /// <summary>Gets the composed transaction layer.</summary>
    /// <returns>The endpoint transaction layer.</returns>
    ISipTransactionLayer GetEndpointTransactionLayer();
    /// <summary>Gets the composed dialog layer.</summary>
    /// <returns>The endpoint dialog layer.</returns>
    ISipDialogLayer GetEndpointDialogLayer();
    /// <summary>Gets the composed DNS resolver.</summary>
    /// <returns>The endpoint DNS resolver.</returns>
    ISipDnsResolver GetEndpointDnsResolver();
    /// <summary>Gets the composed message codec.</summary>
    /// <returns>The endpoint message codec.</returns>
    ISipMessageCodec GetEndpointMessageCodec();
    /// <summary>Gets the composed extension registry.</summary>
    /// <returns>The endpoint extension registry.</returns>
    ISipExtensionRegistry GetEndpointExtensionRegistry();
    /// <summary>Gets the configured authentication client.</summary>
    /// <returns>The endpoint authentication client.</returns>
    ISipAuthenticationClient GetEndpointAuthenticationClient();
    /// <summary>Gets the configured authentication server.</summary>
    /// <returns>The endpoint authentication server.</returns>
    ISipAuthenticationServer GetEndpointAuthenticationServer();
}
