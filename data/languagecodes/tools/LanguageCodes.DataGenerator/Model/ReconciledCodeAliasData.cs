namespace LanguageCodes.DataGenerator.Model;

internal sealed class ReconciledCodeAliasData
{
    internal ReconciledCodeAliasData(ReconciledCodeAlias[] aliases)
    {
        Aliases = aliases;
    }

    internal ReconciledCodeAlias[] Aliases { get; }
}
