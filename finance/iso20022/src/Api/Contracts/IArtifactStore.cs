using System.IO;

namespace iso20022;

/// <summary>Stores and opens verified artifacts beneath caller-selected roots.</summary>
public interface IArtifactStore
{
    /// <summary>Atomically stores one artifact stream and its manifest.</summary>
    /// <param name="input">The content stream to store.</param><param name="request">The explicit root, manifest, and operation policy.</param><returns>The artifact-store result.</returns>
    ArtifactStoreResult StoreArtifactStream(Stream? input, ArtifactStoreRequest? request);
    /// <summary>Opens one verified artifact by coordinate.</summary>
    /// <param name="request">The explicit root, coordinate, and operation policy.</param><returns>The artifact-store result.</returns>
    ArtifactStoreResult OpenArtifact(ArtifactStoreRequest? request);
}
