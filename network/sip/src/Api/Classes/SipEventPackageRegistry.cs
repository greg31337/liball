namespace sip;

/// <summary>Registers event packages and applies package-specific body state rules.</summary>
public class SipEventPackageRegistry : ISipEventPackageRegistry
{
    SipEventPackageRegistryCore core;

    /// <summary>Initializes a registry containing the standard event packages.</summary>
    public SipEventPackageRegistry()
    {
        core = new SipEventPackageRegistryCore();
    }

    /// <summary>Registers one custom event package.</summary>
    /// <param name="eventPackage">The case-sensitive event package token.</param>
    /// <param name="handler">The custom body handler.</param>
    /// <param name="contentTypes">The media types accepted by the handler.</param>
    /// <returns>True when the package was added.</returns>
    public bool RegisterEventPackage(
        string eventPackage, ISipEventPackage handler, SipMediaType[] contentTypes)
    {
        return core.RegisterPackage(eventPackage, handler, contentTypes);
    }

    /// <summary>Removes one custom event package.</summary>
    /// <param name="eventPackage">The case-sensitive event package token.</param>
    /// <returns>True when the package was removed.</returns>
    public bool RemoveEventPackage(string eventPackage)
    {
        return core.RemovePackage(eventPackage);
    }

    /// <summary>Gets detached descriptions of all registered packages.</summary>
    /// <returns>The package descriptions in registration order.</returns>
    public SipEventPackageRegistration[] GetEventPackageRegistrations()
    {
        return core.GetRegistrations();
    }

    /// <summary>Processes one event body and updates its subscription state.</summary>
    /// <param name="request">The body processing operation.</param>
    /// <returns>The package processing outcome.</returns>
    public SipEventPackageResult ProcessEventBody(SipEventPackageRequest request)
    {
        return core.ProcessBody(request);
    }

    /// <summary>Gets the latest accepted state for one subscription.</summary>
    /// <param name="subscriptionId">The subscription identifier.</param>
    /// <returns>The detached state, or null when no state exists.</returns>
    public SipEventPackageState? GetEventPackageState(string subscriptionId)
    {
        return core.GetState(subscriptionId);
    }

    /// <summary>Clears state retained for one subscription.</summary>
    /// <param name="subscriptionId">The subscription identifier.</param>
    /// <returns>True when state was removed.</returns>
    public bool ClearEventPackageState(string subscriptionId)
    {
        return core.ClearState(subscriptionId);
    }
}
