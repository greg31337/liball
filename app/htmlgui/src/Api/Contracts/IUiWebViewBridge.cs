namespace htmlgui;

/// <summary>Coordinates a validated, revision-aware WebView message session.</summary>
public interface IUiWebViewBridge
{
    /// <summary>Starts the transport and sends the bridge initialization handshake.</summary>
    /// <param name="session">The new session identifier.</param>
    /// <param name="nonce">The session nonce.</param>
    /// <param name="nowMilliseconds">The monotonic current time in milliseconds.</param>
    /// <returns>The exceptionless bridge result.</returns>
    UiResult StartUiBridge(string? session, string? nonce, long nowMilliseconds);

    /// <summary>Sends one correlated request.</summary>
    /// <param name="request">The request description.</param>
    /// <param name="nowMilliseconds">The monotonic current time in milliseconds.</param>
    /// <returns>The exceptionless bridge result.</returns>
    UiResult SendUiBridgeRequest(UiBridgeRequest? request, long nowMilliseconds);

    /// <summary>Sends one notification that requires acknowledgement.</summary>
    /// <param name="kind">The notification message kind.</param>
    /// <param name="payload">The optional portable payload.</param>
    /// <param name="nowMilliseconds">The monotonic current time in milliseconds.</param>
    /// <returns>The exceptionless bridge result.</returns>
    UiResult SendUiBridgeNotification(UiBridgeMessageKind kind, UiValue? payload, long nowMilliseconds);

    /// <summary>Validates and dispatches one inbound message.</summary>
    /// <param name="message">The inbound bridge envelope.</param>
    /// <param name="isMainFrame">Whether the message originated in the main frame.</param>
    /// <param name="nowMilliseconds">The monotonic current time in milliseconds.</param>
    /// <returns>The exceptionless bridge result.</returns>
    UiResult ReceiveUiBridgeMessage(UiBridgeEnvelope? message, bool isMainFrame, long nowMilliseconds);

    /// <summary>Sends an explicit acknowledgement for one received sequence.</summary>
    /// <param name="sequence">The received sequence to acknowledge.</param>
    /// <param name="nowMilliseconds">The monotonic current time in milliseconds.</param>
    /// <returns>The exceptionless bridge result.</returns>
    UiResult AcknowledgeUiBridgeMessage(int sequence, long nowMilliseconds);

    /// <summary>Sweeps request, heartbeat, and session timeouts.</summary>
    /// <param name="nowMilliseconds">The monotonic current time in milliseconds.</param>
    /// <returns>The exceptionless bridge result.</returns>
    UiResult SweepUiBridgeTimeouts(long nowMilliseconds);

    /// <summary>Restarts the transport and begins a new session handshake.</summary>
    /// <param name="session">The replacement session identifier.</param>
    /// <param name="nonce">The replacement session nonce.</param>
    /// <param name="lastAcknowledgedRevision">The last DOM revision acknowledged by the browser.</param>
    /// <param name="nowMilliseconds">The monotonic current time in milliseconds.</param>
    /// <returns>The exceptionless bridge result.</returns>
    UiResult ReconnectUiBridge(string? session, string? nonce, int lastAcknowledgedRevision, long nowMilliseconds);

    /// <summary>Begins an orderly bridge shutdown.</summary>
    /// <param name="nowMilliseconds">The monotonic current time in milliseconds.</param>
    /// <returns>The exceptionless bridge result.</returns>
    UiResult ShutdownUiBridge(long nowMilliseconds);

    /// <summary>Reads the negotiated or currently available transport capabilities.</summary>
    /// <returns>The detached capabilities.</returns>
    UiTransportCapabilities GetUiBridgeCapabilities();
}
