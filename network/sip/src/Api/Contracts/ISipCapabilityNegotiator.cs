namespace sip;

/// <summary>Inspects, advertises, and negotiates SIP capabilities and caller preferences.</summary>
public interface ISipCapabilityNegotiator
{
    /// <summary>Inspects capability and caller-preference headers without applying local policy.</summary>
    /// <param name="message">The SIP request or response to inspect.</param>
    /// <returns>The parsed, detached capability information.</returns>
    SipCapabilityNegotiation InspectCapabilityMessage(SipMessage message);
    /// <summary>Checks an incoming message against local methods, extensions, media, and events.</summary>
    /// <param name="message">The SIP request or response to negotiate.</param>
    /// <param name="localCapabilities">The locally supported capabilities.</param>
    /// <returns>The compatibility result and mutual capability sets.</returns>
    SipCapabilityNegotiation NegotiateCapabilityMessage(
        SipMessage message, SipCapabilitySet localCapabilities);
    /// <summary>Adds normalized capability advertisement fields to a detached message.</summary>
    /// <param name="message">The message to copy and augment.</param>
    /// <param name="capabilities">The Allow, Supported, Accept, and Allow-Events values.</param>
    /// <param name="featureCapabilities">The Feature-Caps values to insert before existing values.</param>
    /// <returns>The detached augmented message or a validation failure.</returns>
    SipCapabilityMessageResult AddCapabilityAdvertisement(
        SipMessage message, SipCapabilitySet capabilities,
        SipExtensionHeaderValue[] featureCapabilities);
    /// <summary>Adds RFC 3841 caller-preference fields to a detached message.</summary>
    /// <param name="message">The request to copy and augment.</param>
    /// <param name="preferences">The caller preferences to add.</param>
    /// <returns>The detached augmented request or a validation failure.</returns>
    SipCapabilityMessageResult AddCallerPreferenceHeaders(
        SipMessage message, SipCallerPreferences preferences);
    /// <summary>Matches one request's caller preferences to one callee contact.</summary>
    /// <param name="request">The request carrying caller preferences.</param>
    /// <param name="contact">The contact carrying callee feature parameters.</param>
    /// <returns>The eligibility and preference score for the contact.</returns>
    SipCallerPreferenceMatch MatchCallerPreferences(
        SipMessage request, SipContactValue contact);
}
