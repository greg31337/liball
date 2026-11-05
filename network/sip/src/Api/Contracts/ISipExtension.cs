namespace sip;

/// <summary>Advertises and processes one application SIP extension.</summary>
public interface ISipExtension
{
    /// <summary>Returns the extension capabilities.</summary>
    /// <returns>The advertised capabilities.</returns>
    SipCapabilitySet GetExtensionCapabilities();
    /// <summary>Processes one extension request.</summary>
    /// <param name="message">The extension request.</param>
    /// <returns>The dispatch result.</returns>
    SipExtensionResult ProcessExtensionRequest(SipMessage message);
}
