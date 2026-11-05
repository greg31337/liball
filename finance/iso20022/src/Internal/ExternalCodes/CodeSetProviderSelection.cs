namespace iso20022;

internal class CodeSetProviderSelection
{
    internal bool Success;
    internal bool Found;
    internal RepositorySnapshot? Snapshot;
    internal ExternalCodeCatalog? Catalog;
    internal string ErrorCode = string.Empty;
    internal string ErrorMessage = string.Empty;
}
