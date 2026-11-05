namespace sip;

/// <summary>Validates conditional publication operations and expiry.</summary>
public interface ISipPublicationServer
{
    /// <summary>Configures the publication record store.</summary>
    /// <param name="publicationStore">The publication store.</param>
    void ConfigurePublicationStore(ISipPublicationStore publicationStore);
    /// <summary>Configures application authorization for publications.</summary>
    /// <param name="authorizationPolicy">The publication authorization policy.</param>
    void ConfigurePublicationAuthorizationPolicy(
        ISipPublicationAuthorizationPolicy authorizationPolicy);
    /// <summary>Configures event-package body validation.</summary>
    /// <param name="eventPackageRegistry">The event-package registry.</param>
    void ConfigurePublicationEventPackageRegistry(
        ISipEventPackageRegistry eventPackageRegistry);
    /// <summary>Processes one PUBLISH request.</summary>
    /// <param name="request">The publication request.</param>
    /// <returns>The resulting event.</returns>
    SipPublicationEvent ProcessPublication(SipPublicationRequest request);
    /// <summary>Expires publications at a monotonic timestamp.</summary>
    /// <param name="nowMilliseconds">The monotonic timestamp.</param>
    void ExpirePublications(long nowMilliseconds);
    /// <summary>Gets detached snapshots of all stored publications.</summary>
    /// <returns>The stored publication snapshots.</returns>
    SipPublication[] GetPublications();
    /// <summary>Polls the next queued server-publication event.</summary>
    /// <returns>The next event, or an empty event when none is queued.</returns>
    SipPublicationEvent PollPublicationEvent();
}
