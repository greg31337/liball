namespace sip;

/// <summary>Stores detached event-state publication records.</summary>
public interface ISipPublicationStore
{
    /// <summary>Gets detached snapshots of all stored publications.</summary>
    /// <returns>The stored publication snapshots.</returns>
    SipPublication[] GetPublications();
    /// <summary>Creates or replaces one publication by stable identifier.</summary>
    /// <param name="publication">The detached publication record.</param>
    void StorePublication(SipPublication publication);
    /// <summary>Removes one publication by stable identifier.</summary>
    /// <param name="publicationId">The stable publication identifier.</param>
    /// <returns>True when a record was removed.</returns>
    bool RemovePublication(string publicationId);
}
