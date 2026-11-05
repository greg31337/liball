using System.IO;

namespace iso20022;

/// <summary>Contains the exception-less outcome of storing or opening one artifact.</summary>
public class ArtifactStoreResult
{
    /// <summary>Whether the requested artifact-store operation completed successfully.</summary>
    public bool Success;
    /// <summary>Whether the requested coordinate exists in the selected store.</summary>
    public bool Found;
    /// <summary>Whether a byte-identical existing artifact was reused.</summary>
    public bool ReusedExistingArtifact;
    /// <summary>The artifact content returned by an open operation, owned by the caller when present.</summary>
    public Stream? Content;
    /// <summary>The verified manifest stored or opened by the operation.</summary>
    public ArtifactManifest Artifact = new ArtifactManifest();
    /// <summary>The resolved path beneath the caller-selected root.</summary>
    public string StoragePath = string.Empty;
    /// <summary>The number of content bytes stored or opened.</summary>
    public long ByteCount;
    /// <summary>A stable error code when the operation did not succeed.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable error message when the operation did not succeed.</summary>
    public string ErrorMessage = string.Empty;
}
