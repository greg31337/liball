namespace sip;

/// <summary>Stores publication records in process memory.</summary>
public class SipMemoryPublicationStore : ISipPublicationStore
{
    object gate;
    System.Collections.Generic.List<SipPublication> publications;

    /// <summary>Initializes an empty publication store.</summary>
    public SipMemoryPublicationStore()
    {
        gate = new object();
        publications = new System.Collections.Generic.List<SipPublication>();
    }

    /// <summary>Gets detached snapshots of all stored publications.</summary>
    /// <returns>The stored publication snapshots.</returns>
    public SipPublication[] GetPublications()
    {
        lock (gate)
        {
            SipPublication[] result = new SipPublication[publications.Count];
            int index = 0;
            while (index < publications.Count)
            {
                result[index] = SipPublicationModelCopy.CopyPublication(publications[index]);
                index = index + 1;
            }
            return result;
        }
    }

    /// <summary>Creates or replaces one publication by stable identifier.</summary>
    /// <param name="publication">The detached publication record.</param>
    public void StorePublication(SipPublication publication)
    {
        if (publication == null || publication.PublicationId == null
            || publication.PublicationId.Length == 0)
            throw new System.ArgumentException(
                "A publication with a stable identifier is required.", "publication");
        lock (gate)
        {
            int index = FindPublication(publication.PublicationId);
            SipPublication copy = SipPublicationModelCopy.CopyPublication(publication);
            if (index < 0) publications.Add(copy);
            else publications[index] = copy;
        }
    }

    /// <summary>Removes one publication by stable identifier.</summary>
    /// <param name="publicationId">The stable publication identifier.</param>
    /// <returns>True when a record was removed.</returns>
    public bool RemovePublication(string publicationId)
    {
        if (publicationId == null || publicationId.Length == 0)
            throw new System.ArgumentException(
                "A publication identifier is required.", "publicationId");
        lock (gate)
        {
            int index = FindPublication(publicationId);
            if (index < 0) return false;
            publications.RemoveAt(index);
            return true;
        }
    }

    int FindPublication(string publicationId)
    {
        int index = 0;
        while (index < publications.Count)
        {
            if (publications[index].PublicationId == publicationId) return index;
            index = index + 1;
        }
        return -1;
    }
}
