namespace sip;

/// <summary>Registers INFO packages and manages RFC 6086 dialog negotiation.</summary>
public interface ISipInfoPackageFramework
{
    /// <summary>Configures the dialog layer used to create in-dialog INFO requests.</summary>
    /// <param name="dialogLayer">The dialog layer.</param>
    void ConfigureInfoDialogLayer(ISipDialogLayer dialogLayer);
    /// <summary>Registers one INFO package by its case-sensitive name.</summary>
    /// <param name="package">The case-sensitive package name.</param>
    /// <param name="handler">The package handler.</param>
    /// <param name="contentTypes">The media types accepted for the package.</param>
    /// <returns>True when the package was added.</returns>
    bool RegisterInfoPackage(
        string package, ISipInfoPackage handler, SipMediaType[] contentTypes);
    /// <summary>Configures the application handler for allowed legacy INFO usages.</summary>
    /// <param name="handler">The legacy INFO handler.</param>
    void ConfigureLegacyInfoHandler(ISipInfoPackage handler);
    /// <summary>Removes one registered INFO package.</summary>
    /// <param name="package">The case-sensitive package name.</param>
    /// <returns>True when the package was removed.</returns>
    bool RemoveInfoPackage(string package);
    /// <summary>Adds the current local Recv-Info advertisement to an eligible message.</summary>
    /// <param name="message">The request or response to copy and advertise on.</param>
    /// <returns>The message copy containing the advertisement.</returns>
    SipMessage AddReceiveInfoAdvertisement(SipMessage message);
    /// <summary>Updates remote package willingness for one dialog from Recv-Info.</summary>
    /// <param name="dialogId">The associated dialog.</param>
    /// <param name="message">The received request or response.</param>
    /// <returns>The resulting negotiation snapshot.</returns>
    SipInfoNegotiation UpdateInfoNegotiation(
        SipDialogId dialogId, SipMessage message);
    /// <summary>Gets negotiated package state for one dialog.</summary>
    /// <param name="dialogId">The associated dialog.</param>
    /// <returns>The current negotiation snapshot.</returns>
    SipInfoNegotiation GetInfoNegotiation(SipDialogId dialogId);
    /// <summary>Creates one in-dialog INFO request.</summary>
    /// <param name="request">The INFO operation.</param>
    /// <returns>The creation result and generated request.</returns>
    SipInfoResult SendInfo(SipInfoRequest request);
    /// <summary>Validates and dispatches one incoming in-dialog INFO request.</summary>
    /// <param name="dialogId">The associated local dialog identifier.</param>
    /// <param name="message">The INFO request.</param>
    /// <param name="options">The receiving policy.</param>
    /// <returns>The package result and generated response.</returns>
    SipInfoResult ReceiveInfo(
        SipDialogId dialogId, SipMessage message, SipInfoOptions options);
}
