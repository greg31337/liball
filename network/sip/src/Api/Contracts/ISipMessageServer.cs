namespace sip;

/// <summary>Validates and accepts pager-mode MESSAGE requests.</summary>
public interface ISipMessageServer
{
    /// <summary>Configures authentication used for protected MESSAGE requests.</summary>
    /// <param name="authenticationServer">The authentication server.</param>
    /// <param name="nonceOptions">The nonce and realm policy.</param>
    void ConfigureInstantMessageAuthenticationServer(
        ISipAuthenticationServer authenticationServer,
        SipNonceOptions nonceOptions);
    /// <summary>Processes one incoming MESSAGE request.</summary>
    /// <param name="message">The MESSAGE request.</param>
    /// <param name="options">The receiving policy.</param>
    /// <returns>The receive or rejection event and response.</returns>
    SipInstantMessageEvent ReceiveInstantMessage(
        SipMessage message, SipInstantMessageServerOptions options);
    /// <summary>Polls the next accepted instant-message event.</summary>
    /// <returns>The next event, or an empty event when none is queued.</returns>
    SipInstantMessageEvent PollReceivedInstantMessageEvent();
}
