namespace cookies;

/// <summary>
/// Parses Set-Cookie response fields and Cookie request fields.
/// </summary>
public interface ICookieParser
{
    /// <summary>
    /// Parses one Set-Cookie field value or throws for malformed input.
    /// </summary>
    /// <param name="fieldValue">The Set-Cookie field value.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsed cookie model.</returns>
    /// <exception cref="CookieParseException">The field value is malformed.</exception>
    SetCookie ParseSetCookie(string fieldValue, CookieParseOptions options);

    /// <summary>
    /// Parses one Set-Cookie field value without throwing for malformed input.
    /// </summary>
    /// <param name="fieldValue">The Set-Cookie field value.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parse result.</returns>
    SetCookieParseResult TryParseSetCookie(string fieldValue, CookieParseOptions options);

    /// <summary>
    /// Parses Cookie request field values or throws for malformed input.
    /// </summary>
    /// <param name="fieldValues">The Cookie field values in field order.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The ordered cookie pairs.</returns>
    /// <exception cref="CookieParseException">A field value contains malformed input that cannot be ignored.</exception>
    CookiePair[] ParseCookieFields(string[] fieldValues, CookieParseOptions options);

    /// <summary>
    /// Parses Cookie request field values without throwing for malformed input.
    /// </summary>
    /// <param name="fieldValues">The Cookie field values in field order.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parse result.</returns>
    CookieHeaderParseResult TryParseCookieFields(string[] fieldValues, CookieParseOptions options);
}
