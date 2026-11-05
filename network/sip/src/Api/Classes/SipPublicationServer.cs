namespace sip;

/// <summary>Authorizes, stores, updates, removes, and expires event-state publications.</summary>
public class SipPublicationServer : ISipPublicationServer
{
    SipPublicationServerCore core;

    /// <summary>Initializes a publication server with an in-memory store.</summary>
    public SipPublicationServer()
    {
        core = new SipPublicationServerCore(
            new SipCryptographicRandomSource());
    }

    /// <summary>Configures the publication record store.</summary>
    /// <param name="publicationStore">The publication store.</param>
    public void ConfigurePublicationStore(ISipPublicationStore publicationStore)
    {
        core.ConfigureStore(publicationStore);
    }

    /// <summary>Configures application authorization for publications.</summary>
    /// <param name="authorizationPolicy">The publication authorization policy.</param>
    public void ConfigurePublicationAuthorizationPolicy(
        ISipPublicationAuthorizationPolicy authorizationPolicy)
    {
        core.ConfigureAuthorization(authorizationPolicy);
    }

    /// <summary>Configures event-package body validation.</summary>
    /// <param name="eventPackageRegistry">The event-package registry.</param>
    public void ConfigurePublicationEventPackageRegistry(
        ISipEventPackageRegistry eventPackageRegistry)
    {
        core.ConfigureEventPackages(eventPackageRegistry);
    }

    /// <summary>Processes one PUBLISH request.</summary>
    /// <param name="request">The publication request.</param>
    /// <returns>The resulting event.</returns>
    public SipPublicationEvent ProcessPublication(SipPublicationRequest request)
    {
        return core.Process(request);
    }

    /// <summary>Expires publications at a monotonic timestamp.</summary>
    /// <param name="nowMilliseconds">The monotonic timestamp.</param>
    public void ExpirePublications(long nowMilliseconds)
    {
        core.Expire(nowMilliseconds);
    }

    /// <summary>Gets detached snapshots of all stored publications.</summary>
    /// <returns>The stored publication snapshots.</returns>
    public SipPublication[] GetPublications()
    {
        return core.GetPublications();
    }

    /// <summary>Polls the next queued server-publication event.</summary>
    /// <returns>The next event, or an empty event when none is queued.</returns>
    public SipPublicationEvent PollPublicationEvent()
    {
        return core.PollEvent();
    }
}
