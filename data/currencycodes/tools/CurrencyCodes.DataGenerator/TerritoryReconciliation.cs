using System.Collections.Generic;

namespace CurrencyCodes.DataGenerator;

internal sealed class TerritoryReconciliation
{
    internal TerritoryReconciliation(
        string sourceList,
        string sourceEntityName,
        string canonicalName,
        string? territoryKind,
        string? cldrMatchCode,
        string? alpha2Code,
        string? alpha3Code,
        string? numericCodeText,
        bool hasNumericCode,
        int numericCodeValue,
        IReadOnlyList<string> replacementCodes,
        bool explicitOverride)
    {
        SourceList = sourceList;
        SourceEntityName = sourceEntityName;
        CanonicalName = canonicalName;
        TerritoryKind = territoryKind;
        CldrMatchCode = cldrMatchCode;
        Alpha2Code = alpha2Code;
        Alpha3Code = alpha3Code;
        NumericCodeText = numericCodeText;
        HasNumericCode = hasNumericCode;
        NumericCodeValue = numericCodeValue;
        ReplacementCodes = replacementCodes;
        ExplicitOverride = explicitOverride;
    }

    internal string SourceList { get; }

    internal string SourceEntityName { get; }

    internal string CanonicalName { get; }

    internal string? TerritoryKind { get; }

    internal string? CldrMatchCode { get; }

    internal string? Alpha2Code { get; }

    internal string? Alpha3Code { get; }

    internal string? NumericCodeText { get; }

    internal bool HasNumericCode { get; }

    internal int NumericCodeValue { get; }

    internal IReadOnlyList<string> ReplacementCodes { get; }

    internal bool ExplicitOverride { get; }
}
