namespace sip;

/// <summary>Creates pager-mode MESSAGE requests and processes their responses.</summary>
public class SipMessageClient : ISipMessageClient
{
    SipMessageClientCore core;

    /// <summary>Initializes a pager-mode MESSAGE client.</summary>
    public SipMessageClient()
    {
        core = new SipMessageClientCore(
            new SipCryptographicRandomSource());
    }

    /// <summary>Configures authentication used for MESSAGE challenge retries.</summary>
    /// <param name="authenticationClient">The authentication client.</param>
    public void ConfigureInstantMessageAuthenticationClient(
        ISipAuthenticationClient authenticationClient)
    {
        core.ConfigureAuthentication(authenticationClient);
    }

    /// <summary>Sends one instant message.</summary>
    /// <param name="request">The instant-message request.</param>
    /// <returns>The resulting event.</returns>
    public SipInstantMessageEvent SendInstantMessage(
        SipInstantMessageRequest request)
    {
        return core.Send(request);
    }

    /// <summary>Processes one MESSAGE response.</summary>
    /// <param name="message">The response message.</param>
    /// <returns>The resulting event.</returns>
    public SipInstantMessageEvent ReceiveInstantMessageResponse(
        SipMessage message)
    {
        return core.ReceiveResponse(message);
    }

    /// <summary>Polls the next queued instant-message event.</summary>
    /// <returns>The next event, or an empty event when none is queued.</returns>
    public SipInstantMessageEvent PollInstantMessageEvent()
    {
        return core.PollEvent();
    }
}
