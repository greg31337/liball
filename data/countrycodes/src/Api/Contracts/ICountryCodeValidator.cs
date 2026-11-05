namespace countrycodes;

/// <summary>
/// Defines country-code validation operations.
/// </summary>
public interface ICountryCodeValidator
{
    /// <summary>
    /// Validates an alpha-2 country code under an explicit policy.
    /// </summary>
    /// <param name="value">The alpha-2 value to validate.</param>
    /// <param name="policy">The policy to apply after syntax validation.</param>
    /// <returns>The structured validation outcome.</returns>
    ValidationResult ValidateCountryAlpha2(string value, ValidationPolicy policy);

    /// <summary>
    /// Validates an alpha-3 country code under an explicit policy.
    /// </summary>
    /// <param name="value">The alpha-3 value to validate.</param>
    /// <param name="policy">The policy to apply after syntax validation.</param>
    /// <returns>The structured validation outcome.</returns>
    ValidationResult ValidateCountryAlpha3(string value, ValidationPolicy policy);

    /// <summary>
    /// Validates a numeric-3 country code under an explicit policy.
    /// </summary>
    /// <param name="value">The numeric-3 value to validate.</param>
    /// <param name="policy">The policy to apply after syntax validation.</param>
    /// <returns>The structured validation outcome.</returns>
    ValidationResult ValidateCountryNumericCode(string value, ValidationPolicy policy);

    /// <summary>
    /// Validates an ISO 3166-3 alpha-4 code under an explicit policy.
    /// </summary>
    /// <param name="value">The alpha-4 value to validate.</param>
    /// <param name="policy">The policy to apply after syntax validation.</param>
    /// <returns>The structured validation outcome.</returns>
    ValidationResult ValidateCountryAlpha4(string value, ValidationPolicy policy);

    /// <summary>
    /// Validates an automatically detected country identifier under an explicit policy.
    /// </summary>
    /// <param name="value">The country identifier to validate.</param>
    /// <param name="policy">The policy to apply after syntax validation.</param>
    /// <returns>The structured validation outcome.</returns>
    ValidationResult ValidateAnyCountryCode(string value, ValidationPolicy policy);

    /// <summary>
    /// Validates a full subdivision code under an explicit policy.
    /// </summary>
    /// <param name="value">The subdivision code to validate.</param>
    /// <param name="policy">The policy to apply after syntax validation.</param>
    /// <returns>The structured validation outcome.</returns>
    ValidationResult ValidateSubdivisionCode(string value, ValidationPolicy policy);

    /// <summary>
    /// Validates a BCP 47 locale tag under an explicit policy.
    /// </summary>
    /// <param name="value">The locale tag to validate.</param>
    /// <param name="policy">The policy to apply after syntax validation.</param>
    /// <returns>The structured validation outcome.</returns>
    ValidationResult ValidateLocaleTag(string value, ValidationPolicy policy);

    /// <summary>
    /// Validates a name input under an explicit policy.
    /// </summary>
    /// <param name="value">The name input to validate.</param>
    /// <param name="policy">The policy to apply after syntax validation.</param>
    /// <returns>The structured validation outcome.</returns>
    ValidationResult ValidateNameInput(string value, ValidationPolicy policy);
}
