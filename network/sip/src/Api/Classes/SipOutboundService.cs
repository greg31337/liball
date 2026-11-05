namespace sip;

/// <summary>Coordinates high-level Outbound, GRUU, push, and dialog-target helpers.</summary>
public class SipOutboundService : ISipOutboundService
{
    /// <summary>Initializes an Outbound and push helper service.</summary>
    public SipOutboundService()
    {
    }

    /// <summary>Creates a validated registration Contact.</summary>
    /// <param name="request">The Contact-generation request.</param>
    /// <returns>The generated Contact and required option tags.</returns>
    public SipOutboundResult CreateRegistrationContact(
        SipOutboundContactRequest request)
    {
        return SipOutboundServiceCore.CreateRegistrationContact(request);
    }

    /// <summary>Creates a peer-safe dialog Contact from registration metadata.</summary>
    /// <param name="registrationContact">The registration Contact to sanitize.</param>
    /// <returns>The detached dialog Contact without private registration parameters.</returns>
    public SipOutboundResult CreateDialogContact(
        SipContactValue registrationContact)
    {
        return SipOutboundServiceCore.CreateDialogContact(
            registrationContact);
    }

    /// <summary>Selects a public, temporary, or fallback Contact routing URI.</summary>
    /// <param name="contact">The Contact containing possible GRUUs.</param>
    /// <param name="preferTemporary">Whether a temporary GRUU is preferred.</param>
    /// <param name="requireSecure">Whether the selected target must use SIPS.</param>
    /// <returns>The selected detached URI.</returns>
    public SipOutboundResult SelectGruuTarget(SipContactValue contact,
        bool preferTemporary, bool requireSecure)
    {
        return SipOutboundServiceCore.SelectGruu(
            contact, preferTemporary, requireSecure);
    }

    /// <summary>Selects a binding and flow, requesting push when a flow is dormant.</summary>
    /// <param name="request">The routing candidates and policy inputs.</param>
    /// <param name="pushPolicy">The application push provider policy.</param>
    /// <returns>The route or accepted push request.</returns>
    public SipOutboundResult SelectBindingRoute(SipOutboundRouteRequest request,
        ISipPushNotificationPolicy pushPolicy)
    {
        return SipOutboundServiceCore.SelectBindingRoute(request, pushPolicy);
    }

    /// <summary>Applies a valid target-refresh Contact to a detached dialog snapshot.</summary>
    /// <param name="dialog">The dialog to copy and update.</param>
    /// <param name="message">The target-refresh request or successful response.</param>
    /// <returns>The detached dialog and update status.</returns>
    public SipOutboundResult UpdateDialogTarget(
        SipDialogSnapshot dialog, SipMessage message)
    {
        return SipOutboundServiceCore.UpdateDialogTarget(dialog, message);
    }

    /// <summary>Checks whether a REGISTER response confirms the requested push provider.</summary>
    /// <param name="response">The REGISTER response to inspect.</param>
    /// <param name="requestedContact">The Contact used to request push support.</param>
    /// <returns>The matching response Contact and support result.</returns>
    public SipOutboundResult InspectPushRegistration(
        SipMessage response, SipContactValue requestedContact)
    {
        return SipOutboundServiceCore.InspectPushRegistration(
            response, requestedContact);
    }
}
