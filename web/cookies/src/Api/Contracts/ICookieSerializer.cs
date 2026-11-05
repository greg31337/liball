namespace cookies;

/// <summary>
/// Serializes Set-Cookie response values and Cookie request values.
/// </summary>
public interface ICookieSerializer
{
    /// <summary>
    /// Serializes one validated Set-Cookie model.
    /// </summary>
    /// <param name="cookie">The cookie model to serialize.</param>
    /// <param name="options">The serialization options.</param>
    /// <returns>The Set-Cookie field value.</returns>
    /// <exception cref="CookieSerializationException">The cookie model cannot be serialized safely.</exception>
    string SerializeSetCookie(SetCookie cookie, CookieSerializationOptions options);

    /// <summary>
    /// Serializes ordered cookie pairs into one Cookie field value.
    /// </summary>
    /// <param name="pairs">The pairs in output order.</param>
    /// <param name="options">The serialization options.</param>
    /// <returns>The Cookie field value.</returns>
    /// <exception cref="CookieSerializationException">A cookie pair cannot be serialized safely.</exception>
    string SerializeCookiePairs(CookiePair[] pairs, CookieSerializationOptions options);

    /// <summary>
    /// Serializes ordered pairs into one or more bounded Cookie field values.
    /// </summary>
    /// <param name="pairs">The pairs in output order.</param>
    /// <param name="options">The serialization and splitting options.</param>
    /// <returns>The ordered field values and any serialization diagnostics.</returns>
    /// <exception cref="CookieSerializationException">A cookie pair or configured model is invalid.</exception>
    CookieSelectionResult SerializeCookieFields(CookiePair[] pairs, CookieSerializationOptions options);
}
