using System;

namespace currencycodes;

/// <summary>
/// Defines exception-less currency and amount validation operations.
/// </summary>
public interface ICurrencyValidator
{
    /// <summary>
    /// Validates alphabetic currency-code syntax independently of assignment.
    /// </summary>
    /// <param name="code">The currency code text to validate.</param>
    /// <returns>The structured currency-validation result.</returns>
    CurrencyValidationResult ValidateCurrencyAlphabeticSyntax(string? code);

    /// <summary>
    /// Validates canonical numeric currency-code syntax independently of assignment.
    /// </summary>
    /// <param name="code">The currency code text to validate.</param>
    /// <returns>The structured currency-validation result.</returns>
    CurrencyValidationResult ValidateCurrencyNumericSyntax(string? code);

    /// <summary>
    /// Validates that a currency is assigned in the requested status scope.
    /// </summary>
    /// <param name="code">The currency code to validate.</param>
    /// <param name="currencySet">The status scope to validate against.</param>
    /// <returns>The structured currency-validation result.</returns>
    CurrencyValidationResult ValidateCurrencyAssignment(string? code, CurrencySet currencySet);

    /// <summary>
    /// Validates that a currency is valid on a date using known source intervals.
    /// </summary>
    /// <param name="code">The alphabetic currency code to validate.</param>
    /// <param name="date">The date to validate.</param>
    /// <returns>The structured currency-validation result.</returns>
    CurrencyValidationResult ValidateCurrencyOnDate(string? code, DateOnly date);

    /// <summary>
    /// Validates territory-code membership.
    /// </summary>
    /// <param name="territoryCode">The territory code to validate.</param>
    /// <returns>The structured currency-validation result.</returns>
    CurrencyValidationResult ValidateTerritoryAssignment(string? territoryCode);

    /// <summary>
    /// Validates a territory and currency association on a date.
    /// </summary>
    /// <param name="territoryCode">The territory code to validate.</param>
    /// <param name="currencyCode">The alphabetic currency code to validate.</param>
    /// <param name="date">The date to validate.</param>
    /// <param name="tenderOnly">Indicates whether the association must be legal tender.</param>
    /// <returns>The structured currency-validation result.</returns>
    CurrencyValidationResult ValidateTerritoryCurrencyAssociation(
        string? territoryCode,
        string? currencyCode,
        DateOnly date,
        bool tenderOnly);

    /// <summary>
    /// Validates an amount against the selected currency metadata.
    /// </summary>
    /// <param name="amount">The amount to validate.</param>
    /// <param name="code">The currency code selecting the metadata.</param>
    /// <param name="usage">The amount-rule category to apply.</param>
    /// <returns>The structured amount-validation result.</returns>
    AmountValidationResult ValidateCurrencyAmount(decimal amount, string? code, AmountUsage usage);
}
