using System.Collections.Generic;

namespace CurrencyCodes.DataGenerator;

internal sealed class HistoricalCurrencyUsage
{
    internal HistoricalCurrencyUsage(
        int currencyIdentityIndex,
        string alphabeticCode,
        string sourceEntityName,
        string canonicalTerritoryName,
        string? territoryKind,
        string? territoryMatchCode,
        string? territoryAlpha2Code,
        string? territoryAlpha3Code,
        string? territoryNumericCodeText,
        IReadOnlyList<string> territoryReplacementCodes)
    {
        CurrencyIdentityIndex = currencyIdentityIndex;
        AlphabeticCode = alphabeticCode;
        SourceEntityName = sourceEntityName;
        CanonicalTerritoryName = canonicalTerritoryName;
        TerritoryKind = territoryKind;
        TerritoryMatchCode = territoryMatchCode;
        TerritoryAlpha2Code = territoryAlpha2Code;
        TerritoryAlpha3Code = territoryAlpha3Code;
        TerritoryNumericCodeText = territoryNumericCodeText;
        TerritoryReplacementCodes = territoryReplacementCodes;
    }

    internal int CurrencyIdentityIndex { get; }

    internal string AlphabeticCode { get; }

    internal string SourceEntityName { get; }

    internal string CanonicalTerritoryName { get; }

    internal string? TerritoryKind { get; }

    internal string? TerritoryMatchCode { get; }

    internal string? TerritoryAlpha2Code { get; }

    internal string? TerritoryAlpha3Code { get; }

    internal string? TerritoryNumericCodeText { get; }

    internal IReadOnlyList<string> TerritoryReplacementCodes { get; }
}
