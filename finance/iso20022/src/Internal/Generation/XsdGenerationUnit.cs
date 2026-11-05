namespace iso20022;

internal class XsdGenerationUnit
{
    internal string Identity = string.Empty;
    internal string NamespaceUri = string.Empty;
    internal string FileStem = string.Empty;
    internal RepositorySnapshot? Repository;
    internal MessageDefinitionMetadata? Message;
    internal SchemaArtifact? Schema;
}
