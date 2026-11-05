namespace countrycodes;

/// <summary>
/// Defines country catalog operations.
/// </summary>
public interface ICountryCatalog
{
    /// <summary>
    /// Gets all current countries in stable alpha-2 order.
    /// </summary>
    /// <returns>Detached current country records.</returns>
    Country[] GetCountries();

    /// <summary>
    /// Gets countries having the requested assignment status.
    /// </summary>
    /// <param name="status">The assignment status to select.</param>
    /// <returns>Detached country records in stable code order.</returns>
    Country[] GetCountriesByAssignmentStatus(CodeAssignmentStatus status);

    /// <summary>
    /// Gets the regional-indicator flag emoji for a currently assigned alpha-2 code.
    /// </summary>
    /// <param name="value">The alpha-2 code whose emoji is requested.</param>
    /// <returns>A structured available or unavailable result.</returns>
    CountryFlagEmojiResult GetCountryFlagEmoji(string value);

    /// <summary>
    /// Finds a country by an alpha-2 code.
    /// </summary>
    /// <param name="value">The alpha-2 code to find.</param>
    /// <returns>The structured lookup outcome.</returns>
    CountryLookupResult FindCountryByAlpha2(string value);

    /// <summary>
    /// Finds a country by an alpha-2 code span.
    /// </summary>
    /// <param name="value">The alpha-2 code span to find.</param>
    /// <returns>The structured lookup outcome.</returns>
    CountryLookupResult FindCountryByAlpha2Span(global::System.ReadOnlySpan<char> value);

    /// <summary>
    /// Finds a country by an alpha-3 code.
    /// </summary>
    /// <param name="value">The alpha-3 code to find.</param>
    /// <returns>The structured lookup outcome.</returns>
    CountryLookupResult FindCountryByAlpha3(string value);

    /// <summary>
    /// Finds a country by an alpha-3 code span.
    /// </summary>
    /// <param name="value">The alpha-3 code span to find.</param>
    /// <returns>The structured lookup outcome.</returns>
    CountryLookupResult FindCountryByAlpha3Span(global::System.ReadOnlySpan<char> value);

    /// <summary>
    /// Finds a country by a zero-padded numeric-3 code.
    /// </summary>
    /// <param name="value">The numeric-3 code to find.</param>
    /// <returns>The structured lookup outcome.</returns>
    CountryLookupResult FindCountryByNumericCode(string value);

    /// <summary>
    /// Finds a country by its integer numeric value.
    /// </summary>
    /// <param name="value">The integer numeric value to find.</param>
    /// <returns>The structured lookup outcome.</returns>
    CountryLookupResult FindCountryByNumericValue(int value);

    /// <summary>
    /// Finds countries by an exact canonical name, official name, or alias.
    /// </summary>
    /// <param name="value">The name to find.</param>
    /// <returns>The structured lookup outcome.</returns>
    CountryLookupResult FindCountryByName(string value);

    /// <summary>
    /// Finds a country by an automatically detected identifier.
    /// </summary>
    /// <param name="value">The identifier to detect and find.</param>
    /// <returns>The structured lookup outcome.</returns>
    CountryLookupResult FindCountryByAnyIdentifier(string value);

    /// <summary>
    /// Gets the country identified by an alpha-2 code or throws a focused exception.
    /// </summary>
    /// <param name="value">The required alpha-2 code.</param>
    /// <returns>The detached matching country.</returns>
    /// <exception cref="InvalidCountryCodeException">The input is invalid.</exception>
    /// <exception cref="CountryNotFoundException">No country matches the input.</exception>
    Country GetRequiredCountryByAlpha2(string value);

    /// <summary>
    /// Gets the country identified by an alpha-3 code or throws a focused exception.
    /// </summary>
    /// <param name="value">The required alpha-3 code.</param>
    /// <returns>The detached matching country.</returns>
    /// <exception cref="InvalidCountryCodeException">The input is invalid.</exception>
    /// <exception cref="CountryNotFoundException">No country matches the input.</exception>
    Country GetRequiredCountryByAlpha3(string value);

    /// <summary>
    /// Gets the country identified by a numeric-3 code or throws a focused exception.
    /// </summary>
    /// <param name="value">The required numeric-3 code.</param>
    /// <returns>The detached matching country.</returns>
    /// <exception cref="InvalidCountryCodeException">The input is invalid.</exception>
    /// <exception cref="CountryNotFoundException">No country matches the input.</exception>
    Country GetRequiredCountryByNumericCode(string value);

    /// <summary>
    /// Gets the country identified by an integer numeric value or throws a focused exception.
    /// </summary>
    /// <param name="value">The required integer numeric value.</param>
    /// <returns>The detached matching country.</returns>
    /// <exception cref="InvalidCountryCodeException">The input is outside the numeric range.</exception>
    /// <exception cref="CountryNotFoundException">No country matches the value.</exception>
    Country GetRequiredCountryByNumericValue(int value);

    /// <summary>
    /// Gets the country identified by an exact name or throws a focused exception.
    /// </summary>
    /// <param name="value">The required country name.</param>
    /// <returns>The detached matching country.</returns>
    /// <exception cref="InvalidCountryCodeException">The input is invalid.</exception>
    /// <exception cref="CountryNotFoundException">No country matches the name.</exception>
    /// <exception cref="AmbiguousCountryException">More than one country matches the name.</exception>
    Country GetRequiredCountryByName(string value);

    /// <summary>
    /// Gets the country identified by an automatically detected value or throws a focused exception.
    /// </summary>
    /// <param name="value">The required country identifier.</param>
    /// <returns>The detached matching country.</returns>
    /// <exception cref="InvalidCountryCodeException">The input is invalid.</exception>
    /// <exception cref="CountryNotFoundException">No country matches the input.</exception>
    /// <exception cref="AmbiguousCountryException">More than one country matches the input.</exception>
    Country GetRequiredCountryByAnyIdentifier(string value);

    /// <summary>
    /// Converts an alpha-2 country code to its canonical alpha-3 code.
    /// </summary>
    /// <param name="value">The alpha-2 code to convert.</param>
    /// <returns>The canonical alpha-3 code.</returns>
    /// <exception cref="InvalidCountryCodeException">The input is invalid.</exception>
    /// <exception cref="CountryNotFoundException">No country matches the input.</exception>
    string ConvertCountryAlpha2ToAlpha3(string value);

    /// <summary>
    /// Converts an alpha-2 country code to its canonical zero-padded numeric-3 code.
    /// </summary>
    /// <param name="value">The alpha-2 code to convert.</param>
    /// <returns>The canonical zero-padded numeric-3 code.</returns>
    /// <exception cref="InvalidCountryCodeException">The input is invalid.</exception>
    /// <exception cref="CountryNotFoundException">No country matches the input.</exception>
    string ConvertCountryAlpha2ToNumericCode(string value);

    /// <summary>
    /// Converts an alpha-3 country code to its canonical alpha-2 code.
    /// </summary>
    /// <param name="value">The alpha-3 code to convert.</param>
    /// <returns>The canonical alpha-2 code.</returns>
    /// <exception cref="InvalidCountryCodeException">The input is invalid.</exception>
    /// <exception cref="CountryNotFoundException">No country matches the input.</exception>
    string ConvertCountryAlpha3ToAlpha2(string value);

    /// <summary>
    /// Converts an alpha-3 country code to its canonical zero-padded numeric-3 code.
    /// </summary>
    /// <param name="value">The alpha-3 code to convert.</param>
    /// <returns>The canonical zero-padded numeric-3 code.</returns>
    /// <exception cref="InvalidCountryCodeException">The input is invalid.</exception>
    /// <exception cref="CountryNotFoundException">No country matches the input.</exception>
    string ConvertCountryAlpha3ToNumericCode(string value);

    /// <summary>
    /// Converts a numeric-3 country code to its canonical alpha-2 code.
    /// </summary>
    /// <param name="value">The zero-padded numeric-3 code to convert.</param>
    /// <returns>The canonical alpha-2 code.</returns>
    /// <exception cref="InvalidCountryCodeException">The input is invalid.</exception>
    /// <exception cref="CountryNotFoundException">No country matches the input.</exception>
    string ConvertCountryNumericCodeToAlpha2(string value);

    /// <summary>
    /// Converts a numeric-3 country code to its canonical alpha-3 code.
    /// </summary>
    /// <param name="value">The zero-padded numeric-3 code to convert.</param>
    /// <returns>The canonical alpha-3 code.</returns>
    /// <exception cref="InvalidCountryCodeException">The input is invalid.</exception>
    /// <exception cref="CountryNotFoundException">No country matches the input.</exception>
    string ConvertCountryNumericCodeToAlpha3(string value);
}
