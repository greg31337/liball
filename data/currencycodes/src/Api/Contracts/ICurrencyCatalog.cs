using System;

namespace currencycodes;

/// <summary>
/// Defines currency and territory catalog operations.
/// </summary>
public interface ICurrencyCatalog
{
    /// <summary>
    /// Gets the versions and checksums that identify the generated data snapshot.
    /// </summary>
    /// <returns>Detached data-version information.</returns>
    DataVersionInfo GetCurrencyDataVersion();

    /// <summary>
    /// Gets currency designations in the requested status scope.
    /// </summary>
    /// <param name="currencySet">The status scope to include.</param>
    /// <returns>Detached currency snapshots in canonical order.</returns>
    CurrencyInfo[] GetCurrencyCatalog(CurrencySet currencySet);

    /// <summary>
    /// Gets every known territory and special entity.
    /// </summary>
    /// <returns>Detached territory snapshots in canonical order.</returns>
    TerritoryInfo[] GetTerritoryCatalog();

    /// <summary>
    /// Gets every known territory and currency usage association.
    /// </summary>
    /// <returns>Detached usage snapshots in canonical order.</returns>
    CurrencyUsage[] GetCurrencyUsageCatalog();

    /// <summary>
    /// Finds a currency by its alphabetic code in the requested status scope.
    /// </summary>
    /// <param name="code">The alphabetic currency code.</param>
    /// <param name="currencySet">The status scope to search.</param>
    /// <returns>The structured lookup result.</returns>
    CurrencyLookupResult FindCurrencyByAlphabeticCode(string code, CurrencySet currencySet);

    /// <summary>
    /// Finds currencies by their canonical three-digit numeric code in the requested status scope.
    /// </summary>
    /// <param name="code">The numeric currency code.</param>
    /// <param name="currencySet">The status scope to search.</param>
    /// <returns>The structured lookup result.</returns>
    CurrencyLookupResult FindCurrenciesByNumericCode(string code, CurrencySet currencySet);

    /// <summary>
    /// Finds currencies by official name using the requested comparison and status scope.
    /// </summary>
    /// <param name="name">The official currency name.</param>
    /// <param name="matchMode">The ordinal comparison to use.</param>
    /// <param name="currencySet">The status scope to search.</param>
    /// <returns>The structured lookup result.</returns>
    CurrencyLookupResult FindCurrenciesByOfficialName(
        string name,
        NameMatchMode matchMode,
        CurrencySet currencySet);

    /// <summary>
    /// Gets the required currency identified by alphabetic code in the requested status scope.
    /// </summary>
    /// <param name="code">The alphabetic currency code.</param>
    /// <param name="currencySet">The status scope to search.</param>
    /// <returns>A detached currency snapshot.</returns>
    /// <exception cref="InvalidCurrencyCodeException">The code is malformed, unassigned, or outside the requested scope.</exception>
    CurrencyInfo GetRequiredCurrencyByAlphabeticCode(string code, CurrencySet currencySet);

    /// <summary>
    /// Finds a territory by its alpha-2 code.
    /// </summary>
    /// <param name="code">The alpha-2 territory code.</param>
    /// <returns>The structured lookup result.</returns>
    TerritoryLookupResult FindTerritoryByAlpha2Code(string code);

    /// <summary>
    /// Finds a territory by its alpha-3 code.
    /// </summary>
    /// <param name="code">The alpha-3 territory code.</param>
    /// <returns>The structured lookup result.</returns>
    TerritoryLookupResult FindTerritoryByAlpha3Code(string code);

    /// <summary>
    /// Finds territories by their canonical three-digit numeric code.
    /// </summary>
    /// <param name="code">The numeric territory code.</param>
    /// <returns>The structured lookup result.</returns>
    TerritoryLookupResult FindTerritoriesByNumericCode(string code);

    /// <summary>
    /// Finds territories by canonical name using the requested comparison.
    /// </summary>
    /// <param name="name">The canonical territory name.</param>
    /// <param name="matchMode">The ordinal comparison to use.</param>
    /// <returns>The structured lookup result.</returns>
    TerritoryLookupResult FindTerritoriesByOfficialName(string name, NameMatchMode matchMode);

    /// <summary>
    /// Gets currencies currently associated with a territory.
    /// </summary>
    /// <param name="territoryCode">The alpha-2 territory code or stable territory key.</param>
    /// <param name="tenderOnly">Indicates whether to include only legal-tender associations.</param>
    /// <returns>Detached currency snapshots in canonical order.</returns>
    CurrencyInfo[] GetCurrentCurrenciesForTerritory(string territoryCode, bool tenderOnly);

    /// <summary>
    /// Gets currencies associated with a territory on a date.
    /// </summary>
    /// <param name="territoryCode">The alpha-2 territory code or stable territory key.</param>
    /// <param name="date">The date to query.</param>
    /// <param name="tenderOnly">Indicates whether to include only legal-tender associations.</param>
    /// <returns>Detached currency snapshots in canonical order.</returns>
    CurrencyInfo[] GetCurrenciesForTerritoryOnDate(
        string territoryCode,
        DateOnly date,
        bool tenderOnly);

    /// <summary>
    /// Gets complete currency usage history for a territory.
    /// </summary>
    /// <param name="territoryCode">The alpha-2 territory code or stable territory key.</param>
    /// <returns>Detached usage snapshots in canonical order.</returns>
    CurrencyUsage[] GetCurrencyUsageHistoryForTerritory(string territoryCode);

    /// <summary>
    /// Gets territories currently associated with a currency.
    /// </summary>
    /// <param name="currencyCode">The alphabetic currency code.</param>
    /// <param name="tenderOnly">Indicates whether to include only legal-tender associations.</param>
    /// <returns>Detached territory snapshots in canonical order.</returns>
    TerritoryInfo[] GetCurrentTerritoriesForCurrency(string currencyCode, bool tenderOnly);

    /// <summary>
    /// Gets complete territory usage history for a currency.
    /// </summary>
    /// <param name="currencyCode">The alphabetic currency code.</param>
    /// <returns>Detached usage snapshots in canonical order.</returns>
    CurrencyUsage[] GetCurrencyUsageHistoryForCurrency(string currencyCode);

    /// <summary>
    /// Gets amount-format metadata for an alphabetic currency code.
    /// </summary>
    /// <param name="code">The alphabetic currency code.</param>
    /// <returns>Detached ISO and CLDR amount metadata.</returns>
    CurrencyFormatMetadata GetCurrencyFormatMetadata(string code);
}
