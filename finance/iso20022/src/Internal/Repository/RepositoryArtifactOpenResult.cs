using System.IO;

namespace iso20022;

internal class RepositoryArtifactOpenResult
{
    internal bool Success;
    internal Stream? Content;
    internal bool WasArchive;
    internal string EntryName = string.Empty;
    internal string ContentMediaType = string.Empty;
    internal long ArtifactByteCount;
    internal long CompressedEntryByteCount;
    internal long ContentByteCount;
    internal ArtifactManifest Manifest = new ArtifactManifest();
    internal RepositoryProvenance Provenance = new RepositoryProvenance();
    internal IsoBinaryValue ContentHash = new IsoBinaryValue();
    internal string ErrorCode = string.Empty;
    internal string ErrorMessage = string.Empty;
}
