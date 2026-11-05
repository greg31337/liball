namespace LanguageCodes.DataGenerator.Model;

internal sealed class ReconciledCodeAlias
{
    internal ReconciledCodeAlias(
        string value,
        ReconciledCodeAliasKind kind,
        string? entityKey,
        string? preferredValue,
        bool isDeprecated,
        ReconciledCodeAliasProvenance[] provenance)
    {
        Value = value;
        Kind = kind;
        EntityKey = entityKey;
        PreferredValue = preferredValue;
        IsDeprecated = isDeprecated;
        Provenance = provenance;
    }

    internal string Value { get; }

    internal ReconciledCodeAliasKind Kind { get; }

    internal string? EntityKey { get; }

    internal string? PreferredValue { get; }

    internal bool IsDeprecated { get; }

    internal ReconciledCodeAliasProvenance[] Provenance { get; }
}
