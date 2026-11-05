using System.Threading;

namespace iso20022;

/// <summary>Specifies an explicit artifact-store root, coordinate, and bounded operation policy.</summary>
public class ArtifactStoreRequest
{
    /// <summary>The caller-selected artifact-store root directory.</summary>
    public string RootPath = string.Empty;
    /// <summary>The coordinate to open, or the coordinate expected when storing an artifact.</summary>
    public ArtifactCoordinate Coordinate = new ArtifactCoordinate();
    /// <summary>The manifest to persist with a stored artifact.</summary>
    public ArtifactManifest Artifact = new ArtifactManifest();
    /// <summary>Whether an already stored byte-identical artifact may be reused.</summary>
    public bool AllowIdenticalArtifactReuse = true;
    /// <summary>Whether content read from the store must be checked against its manifest hash.</summary>
    public bool VerifyHash = true;
    /// <summary>The maximum number of artifact bytes accepted or returned.</summary>
    public int MaxArtifactBytes = Iso20022Limits.MaxArtifactBytes;
    /// <summary>The cancellation request observed during bounded artifact operations.</summary>
    public CancellationToken CancellationToken;
}
