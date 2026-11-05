namespace iso20022;

internal class ExternalCodeArtifactOpenResult
{
    internal bool Success;
    internal byte[] Content = [];
    internal string EntryName = string.Empty;
    internal ArtifactManifest Artifact = new ArtifactManifest();
    internal string ErrorCode = string.Empty;
    internal string ErrorMessage = string.Empty;
}
