using System.Collections.Generic;

namespace CurrencyCodes.DataGenerator;

internal sealed class CldrTerritoryAlias
{
    internal CldrTerritoryAlias(
        string aliasCode,
        string reason,
        string replacementText,
        IReadOnlyList<string> replacementCodes)
    {
        AliasCode = aliasCode;
        Reason = reason;
        ReplacementText = replacementText;
        ReplacementCodes = replacementCodes;
    }

    internal string AliasCode { get; }

    internal string Reason { get; }

    internal string ReplacementText { get; }

    internal IReadOnlyList<string> ReplacementCodes { get; }
}
