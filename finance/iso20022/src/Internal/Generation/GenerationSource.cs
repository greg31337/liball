namespace iso20022;

internal class GenerationSource
{
    internal GenerationSourceKind Kind;
    internal int Ordinal;
    internal string Identifier = string.Empty;
    internal string Edition = string.Empty;
    internal string NamespaceUri = string.Empty;
    internal string Version = string.Empty;
    internal string Profile = string.Empty;
    internal ArtifactManifest Artifact = new ArtifactManifest();
    internal RepositorySnapshot? Repository;
    internal SchemaArtifact? Schema;
}
