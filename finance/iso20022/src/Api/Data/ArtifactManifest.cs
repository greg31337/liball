namespace iso20022;

/// <summary>Records the identity, origin, integrity, and publication metadata of an artifact.</summary>
public class ArtifactManifest
{
    /// <summary>The stable artifact coordinate.</summary>
    public ArtifactCoordinate Coordinate = new ArtifactCoordinate();
    /// <summary>The origin URI supplied for the artifact.</summary>
    public string OriginUri = string.Empty;
    /// <summary>The retrieval time when known.</summary>
    public IsoDateTimeValue? RetrievedAt;
    /// <summary>The artifact publication date when known.</summary>
    public IsoPartialDateValue? PublicationDate;
    /// <summary>The artifact content hash.</summary>
    public IsoBinaryValue Hash = new IsoBinaryValue();
    /// <summary>The name of the content-hash algorithm.</summary>
    public string HashAlgorithm = string.Empty;
    /// <summary>The artifact media type.</summary>
    public string MediaType = string.Empty;
    /// <summary>The artifact license notice.</summary>
    public string LicenseNotice = string.Empty;
    /// <summary>Whether the artifact is approved rather than draft.</summary>
    public bool IsApproved;
}
