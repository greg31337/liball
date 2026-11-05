namespace sip;

/// <summary>Registers event packages and applies package-specific body state rules.</summary>
public interface ISipEventPackageRegistry
{
    /// <summary>Registers one custom event package.</summary>
    /// <param name="eventPackage">The case-sensitive event package token.</param>
    /// <param name="handler">The custom body handler.</param>
    /// <param name="contentTypes">The media types accepted by the handler.</param>
    /// <returns>True when the package was added.</returns>
    bool RegisterEventPackage(
        string eventPackage, ISipEventPackage handler, SipMediaType[] contentTypes);
    /// <summary>Removes one custom event package.</summary>
    /// <param name="eventPackage">The case-sensitive event package token.</param>
    /// <returns>True when the package was removed.</returns>
    bool RemoveEventPackage(string eventPackage);
    /// <summary>Gets detached descriptions of all registered packages.</summary>
    /// <returns>The package descriptions in registration order.</returns>
    SipEventPackageRegistration[] GetEventPackageRegistrations();
    /// <summary>Processes one event body and updates its subscription state.</summary>
    /// <param name="request">The body processing operation.</param>
    /// <returns>The package processing outcome.</returns>
    SipEventPackageResult ProcessEventBody(SipEventPackageRequest request);
    /// <summary>Gets the latest accepted state for one subscription.</summary>
    /// <param name="subscriptionId">The subscription identifier.</param>
    /// <returns>The detached state, or null when no state exists.</returns>
    SipEventPackageState? GetEventPackageState(string subscriptionId);
    /// <summary>Clears state retained for one subscription.</summary>
    /// <param name="subscriptionId">The subscription identifier.</param>
    /// <returns>True when state was removed.</returns>
    bool ClearEventPackageState(string subscriptionId);
}
