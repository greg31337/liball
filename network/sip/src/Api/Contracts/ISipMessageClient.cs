namespace sip;

/// <summary>Sends pager-mode MESSAGE requests and correlates responses.</summary>
public interface ISipMessageClient
{
    /// <summary>Configures authentication used for MESSAGE challenge retries.</summary>
    /// <param name="authenticationClient">The authentication client.</param>
    void ConfigureInstantMessageAuthenticationClient(
        ISipAuthenticationClient authenticationClient);
    /// <summary>Sends one instant message.</summary>
    /// <param name="request">The instant-message request.</param>
    /// <returns>The resulting event.</returns>
    SipInstantMessageEvent SendInstantMessage(SipInstantMessageRequest request);
    /// <summary>Processes one MESSAGE response.</summary>
    /// <param name="message">The response message.</param>
    /// <returns>The resulting event.</returns>
    SipInstantMessageEvent ReceiveInstantMessageResponse(SipMessage message);
    /// <summary>Polls the next queued instant-message event.</summary>
    /// <returns>The next event, or an empty event when none is queued.</returns>
    SipInstantMessageEvent PollInstantMessageEvent();
}
