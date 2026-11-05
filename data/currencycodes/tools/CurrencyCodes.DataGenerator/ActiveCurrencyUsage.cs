using System.Collections.Generic;

namespace CurrencyCodes.DataGenerator;

internal sealed class ActiveCurrencyUsage
{
    internal ActiveCurrencyUsage(
        string sourceEntityName,
        string canonicalTerritoryName,
        string? territoryKind,
        string? territoryMatchCode,
        string? territoryAlpha2Code,
        string? territoryAlpha3Code,
        string? territoryNumericCodeText,
        IReadOnlyList<string> territoryReplacementCodes,
        bool hasCurrencyAssignment,
        string? alphabeticCode,
        string sourceCurrencyName)
    {
        SourceEntityName = sourceEntityName;
        CanonicalTerritoryName = canonicalTerritoryName;
        TerritoryKind = territoryKind;
        TerritoryMatchCode = territoryMatchCode;
        TerritoryAlpha2Code = territoryAlpha2Code;
        TerritoryAlpha3Code = territoryAlpha3Code;
        TerritoryNumericCodeText = territoryNumericCodeText;
        TerritoryReplacementCodes = territoryReplacementCodes;
        HasCurrencyAssignment = hasCurrencyAssignment;
        AlphabeticCode = alphabeticCode;
        SourceCurrencyName = sourceCurrencyName;
    }

    internal string SourceEntityName { get; }

    internal string CanonicalTerritoryName { get; }

    internal string? TerritoryKind { get; }

    internal string? TerritoryMatchCode { get; }

    internal string? TerritoryAlpha2Code { get; }

    internal string? TerritoryAlpha3Code { get; }

    internal string? TerritoryNumericCodeText { get; }

    internal IReadOnlyList<string> TerritoryReplacementCodes { get; }

    internal bool HasCurrencyAssignment { get; }

    internal string? AlphabeticCode { get; }

    internal string SourceCurrencyName { get; }
}
