namespace sip;

/// <summary>Registers INFO packages and manages RFC 6086 dialog negotiation.</summary>
public class SipInfoPackageFramework : ISipInfoPackageFramework
{
    SipInfoPackageFrameworkCore core;

    /// <summary>Initializes an empty INFO package framework.</summary>
    public SipInfoPackageFramework()
    {
        core = new SipInfoPackageFrameworkCore();
    }

    /// <summary>Configures the dialog layer used to create in-dialog INFO requests.</summary>
    /// <param name="dialogLayer">The dialog layer.</param>
    public void ConfigureInfoDialogLayer(ISipDialogLayer dialogLayer)
    {
        core.ConfigureDialogLayer(dialogLayer);
    }

    /// <summary>Registers one INFO package by its case-sensitive name.</summary>
    /// <param name="package">The case-sensitive package name.</param>
    /// <param name="handler">The package handler.</param>
    /// <param name="contentTypes">The media types accepted for the package.</param>
    /// <returns>True when the package was added.</returns>
    public bool RegisterInfoPackage(
        string package, ISipInfoPackage handler, SipMediaType[] contentTypes)
    {
        return core.RegisterPackage(package, handler, contentTypes);
    }

    /// <summary>Configures the application handler for allowed legacy INFO usages.</summary>
    /// <param name="handler">The legacy INFO handler.</param>
    public void ConfigureLegacyInfoHandler(ISipInfoPackage handler)
    {
        core.ConfigureLegacyHandler(handler);
    }

    /// <summary>Removes one registered INFO package.</summary>
    /// <param name="package">The case-sensitive package name.</param>
    /// <returns>True when the package was removed.</returns>
    public bool RemoveInfoPackage(string package)
    {
        return core.RemovePackage(package);
    }

    /// <summary>Adds the current local Recv-Info advertisement to an eligible message.</summary>
    /// <param name="message">The request or response to copy and advertise on.</param>
    /// <returns>The message copy containing the advertisement.</returns>
    public SipMessage AddReceiveInfoAdvertisement(SipMessage message)
    {
        return core.AddAdvertisement(message);
    }

    /// <summary>Updates remote package willingness for one dialog from Recv-Info.</summary>
    /// <param name="dialogId">The associated dialog.</param>
    /// <param name="message">The received request or response.</param>
    /// <returns>The resulting negotiation snapshot.</returns>
    public SipInfoNegotiation UpdateInfoNegotiation(
        SipDialogId dialogId, SipMessage message)
    {
        return core.UpdateNegotiation(dialogId, message);
    }

    /// <summary>Gets negotiated package state for one dialog.</summary>
    /// <param name="dialogId">The associated dialog.</param>
    /// <returns>The current negotiation snapshot.</returns>
    public SipInfoNegotiation GetInfoNegotiation(SipDialogId dialogId)
    {
        return core.GetNegotiation(dialogId);
    }

    /// <summary>Creates one in-dialog INFO request.</summary>
    /// <param name="request">The INFO operation.</param>
    /// <returns>The creation result and generated request.</returns>
    public SipInfoResult SendInfo(SipInfoRequest request)
    {
        return core.Send(request);
    }

    /// <summary>Validates and dispatches one incoming in-dialog INFO request.</summary>
    /// <param name="dialogId">The associated local dialog identifier.</param>
    /// <param name="message">The INFO request.</param>
    /// <param name="options">The receiving policy.</param>
    /// <returns>The package result and generated response.</returns>
    public SipInfoResult ReceiveInfo(
        SipDialogId dialogId, SipMessage message, SipInfoOptions options)
    {
        return core.Receive(dialogId, message, options);
    }
}
