using System;

namespace currencycodes;

/// <summary>
/// Provides currency catalog, normalization, validation, and amount operations.
/// </summary>
public class CurrencyCodeService : ICurrencyCatalog, ICurrencyNormalizer, ICurrencyValidator, ICurrencyAmountRules
{
    internal CurrencyDataStore store;

    /// <summary>
    /// Initializes a service backed by the generated currency data snapshot.
    /// </summary>
    public CurrencyCodeService()
    {
        store = new CurrencyDataStore();
    }

    /// <summary>
    /// Gets the versions and checksums that identify the generated data snapshot.
    /// </summary>
    /// <returns>Detached data-version information.</returns>
    public DataVersionInfo GetCurrencyDataVersion()
    {
        return CurrencySnapshotFactory.CreateDataVersionInfo();
    }

    /// <summary>
    /// Gets currency designations in the requested status scope.
    /// </summary>
    /// <param name="currencySet">The status scope to include.</param>
    /// <returns>Detached currency snapshots in canonical order.</returns>
    public CurrencyInfo[] GetCurrencyCatalog(CurrencySet currencySet)
    {
        return store.GetCurrencyCatalogSnapshots(currencySet);
    }

    /// <summary>
    /// Gets every known territory and special entity.
    /// </summary>
    /// <returns>Detached territory snapshots in canonical order.</returns>
    public TerritoryInfo[] GetTerritoryCatalog()
    {
        return store.GetTerritoryCatalogSnapshots();
    }

    /// <summary>
    /// Gets every known territory and currency usage association.
    /// </summary>
    /// <returns>Detached usage snapshots in canonical order.</returns>
    public CurrencyUsage[] GetCurrencyUsageCatalog()
    {
        return store.GetCurrencyUsageCatalogSnapshots();
    }

    /// <summary>
    /// Finds a currency by alphabetic code in the requested status scope.
    /// </summary>
    /// <param name="code">The alphabetic currency code.</param>
    /// <param name="currencySet">The status scope to search.</param>
    /// <returns>The structured lookup result.</returns>
    public CurrencyLookupResult FindCurrencyByAlphabeticCode(
        string code,
        CurrencySet currencySet)
    {
        return store.FindCurrencyAlphabeticLookup(code, currencySet);
    }

    /// <summary>
    /// Finds currencies by numeric code in the requested status scope.
    /// </summary>
    /// <param name="code">The numeric currency code.</param>
    /// <param name="currencySet">The status scope to search.</param>
    /// <returns>The structured lookup result.</returns>
    public CurrencyLookupResult FindCurrenciesByNumericCode(
        string code,
        CurrencySet currencySet)
    {
        return store.FindCurrencyNumericLookup(code, currencySet);
    }

    /// <summary>
    /// Finds currencies by official name in the requested status scope.
    /// </summary>
    /// <param name="name">The official currency name.</param>
    /// <param name="matchMode">The ordinal comparison to use.</param>
    /// <param name="currencySet">The status scope to search.</param>
    /// <returns>The structured lookup result.</returns>
    public CurrencyLookupResult FindCurrenciesByOfficialName(
        string name,
        NameMatchMode matchMode,
        CurrencySet currencySet)
    {
        return store.FindCurrencyOfficialNameLookup(name, matchMode, currencySet);
    }

    /// <summary>
    /// Gets a required currency by alphabetic code in the requested status scope.
    /// </summary>
    /// <param name="code">The alphabetic currency code.</param>
    /// <param name="currencySet">The status scope to search.</param>
    /// <returns>A detached currency snapshot.</returns>
    /// <exception cref="InvalidCurrencyCodeException">The code is malformed, unassigned, or outside the requested scope.</exception>
    public CurrencyInfo GetRequiredCurrencyByAlphabeticCode(
        string code,
        CurrencySet currencySet)
    {
        return store.GetRequiredCurrencyAlphabeticSnapshot(code, currencySet);
    }

    /// <summary>
    /// Finds a territory by alpha-2 code.
    /// </summary>
    /// <param name="code">The alpha-2 territory code.</param>
    /// <returns>The structured lookup result.</returns>
    public TerritoryLookupResult FindTerritoryByAlpha2Code(string code)
    {
        return store.FindTerritoryAlpha2Lookup(code);
    }

    /// <summary>
    /// Finds a territory by alpha-3 code.
    /// </summary>
    /// <param name="code">The alpha-3 territory code.</param>
    /// <returns>The structured lookup result.</returns>
    public TerritoryLookupResult FindTerritoryByAlpha3Code(string code)
    {
        return store.FindTerritoryAlpha3Lookup(code);
    }

    /// <summary>
    /// Finds territories by numeric code.
    /// </summary>
    /// <param name="code">The numeric territory code.</param>
    /// <returns>The structured lookup result.</returns>
    public TerritoryLookupResult FindTerritoriesByNumericCode(string code)
    {
        return store.FindTerritoryNumericLookup(code);
    }

    /// <summary>
    /// Finds territories by official name.
    /// </summary>
    /// <param name="name">The official territory name.</param>
    /// <param name="matchMode">The ordinal comparison to use.</param>
    /// <returns>The structured lookup result.</returns>
    public TerritoryLookupResult FindTerritoriesByOfficialName(
        string name,
        NameMatchMode matchMode)
    {
        return store.FindTerritoryOfficialNameLookup(name, matchMode);
    }

    /// <summary>
    /// Gets currencies currently associated with a territory.
    /// </summary>
    /// <param name="territoryCode">The territory code or stable territory key.</param>
    /// <param name="tenderOnly">Whether to include only legal-tender associations.</param>
    /// <returns>Detached currency snapshots in canonical order.</returns>
    public CurrencyInfo[] GetCurrentCurrenciesForTerritory(
        string territoryCode,
        bool tenderOnly)
    {
        return store.GetCurrentCurrenciesForTerritorySnapshots(territoryCode, tenderOnly);
    }

    /// <summary>
    /// Gets currencies associated with a territory on a date.
    /// </summary>
    /// <param name="territoryCode">The territory code or stable territory key.</param>
    /// <param name="date">The date to query.</param>
    /// <param name="tenderOnly">Whether to include only legal-tender associations.</param>
    /// <returns>Detached currency snapshots in canonical order.</returns>
    public CurrencyInfo[] GetCurrenciesForTerritoryOnDate(
        string territoryCode,
        DateOnly date,
        bool tenderOnly)
    {
        return store.GetCurrenciesForTerritoryOnDateSnapshots(territoryCode, date, tenderOnly);
    }

    /// <summary>
    /// Gets complete currency usage history for a territory.
    /// </summary>
    /// <param name="territoryCode">The territory code or stable territory key.</param>
    /// <returns>Detached usage snapshots in canonical order.</returns>
    public CurrencyUsage[] GetCurrencyUsageHistoryForTerritory(string territoryCode)
    {
        return store.GetCurrencyUsageHistoryForTerritorySnapshots(territoryCode);
    }

    /// <summary>
    /// Gets territories currently associated with a currency.
    /// </summary>
    /// <param name="currencyCode">The alphabetic currency code.</param>
    /// <param name="tenderOnly">Whether to include only legal-tender associations.</param>
    /// <returns>Detached territory snapshots in canonical order.</returns>
    public TerritoryInfo[] GetCurrentTerritoriesForCurrency(
        string currencyCode,
        bool tenderOnly)
    {
        return store.GetCurrentTerritoriesForCurrencySnapshots(currencyCode, tenderOnly);
    }

    /// <summary>
    /// Gets complete territory usage history for a currency.
    /// </summary>
    /// <param name="currencyCode">The alphabetic currency code.</param>
    /// <returns>Detached usage snapshots in canonical order.</returns>
    public CurrencyUsage[] GetCurrencyUsageHistoryForCurrency(string currencyCode)
    {
        return store.GetCurrencyUsageHistoryForCurrencySnapshots(currencyCode);
    }

    /// <summary>
    /// Gets amount-format metadata for an alphabetic currency code.
    /// </summary>
    /// <param name="code">The alphabetic currency code.</param>
    /// <returns>Detached ISO and CLDR amount metadata.</returns>
    public CurrencyFormatMetadata GetCurrencyFormatMetadata(string code)
    {
        return store.GetCurrencyFormatMetadataSnapshot(code);
    }

    /// <summary>
    /// Normalizes an alphabetic currency code.
    /// </summary>
    /// <param name="code">The code text to normalize.</param>
    /// <returns>The structured normalization result.</returns>
    public NormalizationResult NormalizeCurrencyAlphabeticCode(string? code)
    {
        return CurrencyNormalizationRules.NormalizeCurrencyAlphabeticCode(code);
    }

    /// <summary>
    /// Normalizes a numeric currency code.
    /// </summary>
    /// <param name="code">The code text to normalize.</param>
    /// <returns>The structured normalization result.</returns>
    public NormalizationResult NormalizeCurrencyNumericCode(string? code)
    {
        return CurrencyNormalizationRules.NormalizeCurrencyNumericCode(code);
    }

    /// <summary>
    /// Normalizes an alpha-2 territory code.
    /// </summary>
    /// <param name="code">The code text to normalize.</param>
    /// <returns>The structured normalization result.</returns>
    public NormalizationResult NormalizeTerritoryAlpha2Code(string? code)
    {
        return CurrencyNormalizationRules.NormalizeTerritoryAlpha2Code(code);
    }

    /// <summary>
    /// Normalizes an alpha-3 territory code.
    /// </summary>
    /// <param name="code">The code text to normalize.</param>
    /// <returns>The structured normalization result.</returns>
    public NormalizationResult NormalizeTerritoryAlpha3Code(string? code)
    {
        return CurrencyNormalizationRules.NormalizeTerritoryAlpha3Code(code);
    }

    /// <summary>
    /// Validates alphabetic currency-code syntax independently of assignment.
    /// </summary>
    /// <param name="code">The currency code text to validate.</param>
    /// <returns>The structured validation result.</returns>
    public CurrencyValidationResult ValidateCurrencyAlphabeticSyntax(string? code)
    {
        return CurrencyValidationRules.ValidateCurrencyAlphabeticSyntax(code);
    }

    /// <summary>
    /// Validates numeric currency-code syntax independently of assignment.
    /// </summary>
    /// <param name="code">The currency code text to validate.</param>
    /// <returns>The structured validation result.</returns>
    public CurrencyValidationResult ValidateCurrencyNumericSyntax(string? code)
    {
        return CurrencyValidationRules.ValidateCurrencyNumericSyntax(code);
    }

    /// <summary>
    /// Validates currency assignment in the requested status scope.
    /// </summary>
    /// <param name="code">The alphabetic currency code to validate.</param>
    /// <param name="currencySet">The status scope to validate against.</param>
    /// <returns>The structured validation result.</returns>
    public CurrencyValidationResult ValidateCurrencyAssignment(
        string? code,
        CurrencySet currencySet)
    {
        return CurrencyValidationRules.ValidateCurrencyAssignment(store, code, currencySet);
    }

    /// <summary>
    /// Validates a currency on a date using known source intervals.
    /// </summary>
    /// <param name="code">The alphabetic currency code to validate.</param>
    /// <param name="date">The date to validate.</param>
    /// <returns>The structured validation result.</returns>
    public CurrencyValidationResult ValidateCurrencyOnDate(string? code, DateOnly date)
    {
        return CurrencyValidationRules.ValidateCurrencyOnDate(store, code, date);
    }

    /// <summary>
    /// Validates territory-code membership.
    /// </summary>
    /// <param name="territoryCode">The territory code to validate.</param>
    /// <returns>The structured validation result.</returns>
    public CurrencyValidationResult ValidateTerritoryAssignment(string? territoryCode)
    {
        return CurrencyValidationRules.ValidateTerritoryAssignment(store, territoryCode);
    }

    /// <summary>
    /// Validates a territory and currency association on a date.
    /// </summary>
    /// <param name="territoryCode">The territory code to validate.</param>
    /// <param name="currencyCode">The alphabetic currency code to validate.</param>
    /// <param name="date">The date to validate.</param>
    /// <param name="tenderOnly">Whether the association must be legal tender.</param>
    /// <returns>The structured validation result.</returns>
    public CurrencyValidationResult ValidateTerritoryCurrencyAssociation(
        string? territoryCode,
        string? currencyCode,
        DateOnly date,
        bool tenderOnly)
    {
        return CurrencyValidationRules.ValidateTerritoryCurrencyAssociation(
            store,
            territoryCode,
            currencyCode,
            date,
            tenderOnly);
    }

    /// <summary>
    /// Validates an amount against the selected currency metadata.
    /// </summary>
    /// <param name="amount">The amount to validate.</param>
    /// <param name="code">The alphabetic currency code selecting the metadata.</param>
    /// <param name="usage">The amount-rule category to apply.</param>
    /// <returns>The structured validation result.</returns>
    public AmountValidationResult ValidateCurrencyAmount(
        decimal amount,
        string? code,
        AmountUsage usage)
    {
        return CurrencyAmountValidationRules.ValidateCurrencyAmount(store, amount, code, usage);
    }

    /// <summary>
    /// Rounds an amount using the selected currency rule and midpoint behavior.
    /// </summary>
    /// <param name="amount">The amount to round.</param>
    /// <param name="code">The alphabetic currency code.</param>
    /// <param name="usage">The amount-rule category to apply.</param>
    /// <param name="midpointRounding">The midpoint rounding behavior to apply.</param>
    /// <returns>The rounded amount.</returns>
    public decimal RoundCurrencyAmount(
        decimal amount,
        string code,
        AmountUsage usage,
        MidpointRounding midpointRounding)
    {
        return CurrencyAmountRoundingRules.RoundCurrencyAmount(
            store,
            amount,
            code,
            usage,
            midpointRounding);
    }

    /// <summary>
    /// Converts an exact major-unit amount to signed minor units.
    /// </summary>
    /// <param name="amount">The major-unit amount.</param>
    /// <param name="code">The alphabetic currency code.</param>
    /// <param name="usage">The amount-rule category to apply.</param>
    /// <returns>The structured conversion result.</returns>
    public MinorUnitConversionResult ConvertCurrencyMajorToMinor(
        decimal amount,
        string code,
        AmountUsage usage)
    {
        return CurrencyMinorUnitConversionRules.ConvertCurrencyMajorToMinor(
            store,
            amount,
            code,
            usage);
    }

    /// <summary>
    /// Converts a signed minor-unit value to an exact major-unit amount.
    /// </summary>
    /// <param name="minorUnitValue">The signed minor-unit value.</param>
    /// <param name="code">The alphabetic currency code.</param>
    /// <param name="usage">The amount-rule category to apply.</param>
    /// <returns>The structured conversion result.</returns>
    public MinorUnitConversionResult ConvertCurrencyMinorToMajor(
        long minorUnitValue,
        string code,
        AmountUsage usage)
    {
        return CurrencyMinorUnitConversionRules.ConvertCurrencyMinorToMajor(
            store,
            minorUnitValue,
            code,
            usage);
    }

    /// <summary>
    /// Formats an amount as invariant currency text.
    /// </summary>
    /// <param name="amount">The amount to format.</param>
    /// <param name="code">The alphabetic currency code.</param>
    /// <param name="usage">The amount-rule category to apply.</param>
    /// <param name="midpointRounding">The midpoint rounding behavior to apply.</param>
    /// <returns>The structured text result.</returns>
    public AmountTextResult FormatInvariantCurrencyAmount(
        decimal amount,
        string code,
        AmountUsage usage,
        MidpointRounding midpointRounding)
    {
        return CurrencyAmountTextRules.FormatInvariantCurrencyAmount(
            store,
            amount,
            code,
            usage,
            midpointRounding);
    }

    /// <summary>
    /// Parses invariant currency amount text.
    /// </summary>
    /// <param name="text">The invariant text to parse.</param>
    /// <param name="usage">The amount-rule category to apply.</param>
    /// <returns>The structured text result.</returns>
    public AmountTextResult ParseInvariantCurrencyAmount(string? text, AmountUsage usage)
    {
        return CurrencyAmountTextRules.ParseInvariantCurrencyAmount(store, text, usage);
    }
}
