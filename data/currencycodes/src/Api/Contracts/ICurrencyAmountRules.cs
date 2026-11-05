using System;

namespace currencycodes;

/// <summary>
/// Defines currency-aware amount conversion and invariant text operations.
/// </summary>
public interface ICurrencyAmountRules
{
    /// <summary>
    /// Rounds an amount using the selected currency rule and midpoint behavior.
    /// </summary>
    /// <param name="amount">The amount to round.</param>
    /// <param name="code">The alphabetic currency code.</param>
    /// <param name="usage">The amount-rule category to apply.</param>
    /// <param name="midpointRounding">The midpoint rounding behavior to apply.</param>
    /// <returns>The rounded amount.</returns>
    decimal RoundCurrencyAmount(
        decimal amount,
        string code,
        AmountUsage usage,
        MidpointRounding midpointRounding);

    /// <summary>
    /// Converts an exact major-unit amount to signed minor units.
    /// </summary>
    /// <param name="amount">The major-unit amount.</param>
    /// <param name="code">The alphabetic currency code.</param>
    /// <param name="usage">The amount-rule category to apply.</param>
    /// <returns>The structured conversion result.</returns>
    MinorUnitConversionResult ConvertCurrencyMajorToMinor(
        decimal amount,
        string code,
        AmountUsage usage);

    /// <summary>
    /// Converts a signed minor-unit value to its exact major-unit amount.
    /// </summary>
    /// <param name="minorUnitValue">The signed minor-unit value.</param>
    /// <param name="code">The alphabetic currency code.</param>
    /// <param name="usage">The amount-rule category to apply.</param>
    /// <returns>The structured conversion result.</returns>
    MinorUnitConversionResult ConvertCurrencyMinorToMajor(
        long minorUnitValue,
        string code,
        AmountUsage usage);

    /// <summary>
    /// Formats an amount as invariant currency text using explicit midpoint rounding.
    /// </summary>
    /// <param name="amount">The amount to format.</param>
    /// <param name="code">The alphabetic currency code.</param>
    /// <param name="usage">The amount-rule category to apply.</param>
    /// <param name="midpointRounding">The midpoint rounding behavior to apply.</param>
    /// <returns>The structured text result.</returns>
    AmountTextResult FormatInvariantCurrencyAmount(
        decimal amount,
        string code,
        AmountUsage usage,
        MidpointRounding midpointRounding);

    /// <summary>
    /// Parses invariant currency amount text.
    /// </summary>
    /// <param name="text">The invariant text to parse.</param>
    /// <param name="usage">The amount-rule category to apply.</param>
    /// <returns>The structured text result.</returns>
    AmountTextResult ParseInvariantCurrencyAmount(string? text, AmountUsage usage);
}
