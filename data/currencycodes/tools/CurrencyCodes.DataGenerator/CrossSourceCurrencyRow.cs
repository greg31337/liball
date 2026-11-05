namespace CurrencyCodes.DataGenerator;

internal sealed class CrossSourceCurrencyRow
{
    internal CrossSourceCurrencyRow(
        string sourceList,
        string entityName,
        string officialName,
        string? alphabeticCode,
        string? isoNumericCodeText,
        bool hasIsoMinorUnit,
        int isoMinorUnitDigits,
        bool hasCldrNumericCode,
        string? cldrSourceNumericCodeText,
        string? cldrNumericCodeText,
        int cldrNumericCodeValue,
        bool hasCldrFormatRule,
        int cldrStandardDigits,
        int cldrStandardRounding,
        int cldrCashDigits,
        int cldrCashRounding,
        bool cldrStandardDigitsExplicit,
        bool cldrStandardRoundingExplicit,
        bool cldrCashDigitsExplicit,
        bool cldrCashRoundingExplicit,
        bool identityConflict,
        bool identityConflictReviewed)
    {
        SourceList = sourceList;
        EntityName = entityName;
        OfficialName = officialName;
        AlphabeticCode = alphabeticCode;
        IsoNumericCodeText = isoNumericCodeText;
        HasIsoMinorUnit = hasIsoMinorUnit;
        IsoMinorUnitDigits = isoMinorUnitDigits;
        HasCldrNumericCode = hasCldrNumericCode;
        CldrSourceNumericCodeText = cldrSourceNumericCodeText;
        CldrNumericCodeText = cldrNumericCodeText;
        CldrNumericCodeValue = cldrNumericCodeValue;
        HasCldrFormatRule = hasCldrFormatRule;
        CldrStandardDigits = cldrStandardDigits;
        CldrStandardRounding = cldrStandardRounding;
        CldrCashDigits = cldrCashDigits;
        CldrCashRounding = cldrCashRounding;
        CldrStandardDigitsExplicit = cldrStandardDigitsExplicit;
        CldrStandardRoundingExplicit = cldrStandardRoundingExplicit;
        CldrCashDigitsExplicit = cldrCashDigitsExplicit;
        CldrCashRoundingExplicit = cldrCashRoundingExplicit;
        IdentityConflict = identityConflict;
        IdentityConflictReviewed = identityConflictReviewed;
    }

    internal string SourceList { get; }

    internal string EntityName { get; }

    internal string OfficialName { get; }

    internal string? AlphabeticCode { get; }

    internal string? IsoNumericCodeText { get; }

    internal bool HasIsoMinorUnit { get; }

    internal int IsoMinorUnitDigits { get; }

    internal bool HasCldrNumericCode { get; }

    internal string? CldrSourceNumericCodeText { get; }

    internal string? CldrNumericCodeText { get; }

    internal int CldrNumericCodeValue { get; }

    internal bool HasCldrFormatRule { get; }

    internal int CldrStandardDigits { get; }

    internal int CldrStandardRounding { get; }

    internal int CldrCashDigits { get; }

    internal int CldrCashRounding { get; }

    internal bool CldrStandardDigitsExplicit { get; }

    internal bool CldrStandardRoundingExplicit { get; }

    internal bool CldrCashDigitsExplicit { get; }

    internal bool CldrCashRoundingExplicit { get; }

    internal bool IdentityConflict { get; }

    internal bool IdentityConflictReviewed { get; }
}
