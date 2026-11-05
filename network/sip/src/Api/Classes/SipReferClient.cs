namespace sip;

/// <summary>Creates REFER requests and consumes refer-event responses and notifications.</summary>
public class SipReferClient : ISipReferClient
{
    SipReferClientCore core;

    /// <summary>Initializes a REFER client.</summary>
    public SipReferClient()
    {
        core = new SipReferClientCore(
            new SipCryptographicRandomSource());
    }

    /// <summary>Configures the dialog layer used for in-dialog REFER requests.</summary>
    /// <param name="dialogLayer">The dialog layer.</param>
    public void ConfigureReferDialogLayer(ISipDialogLayer dialogLayer)
    {
        core.ConfigureDialogLayer(dialogLayer);
    }

    /// <summary>Configures authentication used for REFER challenge retries.</summary>
    /// <param name="authenticationClient">The authentication client.</param>
    public void ConfigureReferAuthenticationClient(
        ISipAuthenticationClient authenticationClient)
    {
        core.ConfigureAuthentication(authenticationClient);
    }

    /// <summary>Creates one REFER request.</summary>
    /// <param name="request">The transfer request.</param>
    /// <returns>The initial transfer event.</returns>
    public SipReferEvent SendRefer(SipReferRequest request)
    {
        return core.Send(request);
    }

    /// <summary>Processes one REFER response.</summary>
    /// <param name="message">The REFER response.</param>
    /// <returns>The resulting transfer event.</returns>
    public SipReferEvent ReceiveReferResponse(SipMessage message)
    {
        return core.ReceiveResponse(message);
    }

    /// <summary>Processes one refer-event NOTIFY request.</summary>
    /// <param name="message">The NOTIFY request.</param>
    /// <returns>The resulting transfer event and NOTIFY response.</returns>
    public SipReferEvent ReceiveReferNotification(SipMessage message)
    {
        return core.ReceiveNotification(message);
    }

    /// <summary>Polls the next queued transfer event.</summary>
    /// <returns>The next event, or an empty event when none is queued.</returns>
    public SipReferEvent PollReferEvent()
    {
        return core.PollEvent();
    }
}
