namespace cookies;

/// <summary>
/// Serializes cookie models without retaining mutable serializer state.
/// </summary>
public class CookieSerializer : ICookieSerializer
{
    /// <summary>
    /// Creates a cookie serializer.
    /// </summary>
    public CookieSerializer()
    {
    }

    /// <summary>
    /// Serializes one Set-Cookie model using deterministic attribute ordering.
    /// </summary>
    /// <param name="cookie">The cookie model to serialize.</param>
    /// <param name="options">The serialization options.</param>
    /// <returns>The Set-Cookie field value.</returns>
    /// <exception cref="CookieSerializationException">The model or configuration is invalid.</exception>
    public string SerializeSetCookie(SetCookie cookie, CookieSerializationOptions options)
    {
        CookieStringSerializationResult result = SetCookieSerializerCore.Serialize(cookie, options);
        if (!result.Success)
        {
            throw new CookieSerializationException(CreateFailureMessage(result.ErrorCode));
        }

        return result.Value;
    }

    /// <summary>
    /// Serializes ordered cookie pairs into one Cookie field value.
    /// </summary>
    /// <param name="pairs">The pairs in output order.</param>
    /// <param name="options">The serialization options.</param>
    /// <returns>The Cookie field value.</returns>
    /// <exception cref="CookieSerializationException">A pair, limit, or configuration is invalid.</exception>
    public string SerializeCookiePairs(CookiePair[] pairs, CookieSerializationOptions options)
    {
        CookieStringSerializationResult result = CookieRequestSerializerCore.SerializePairs(pairs, options);
        if (!result.Success)
        {
            throw new CookieSerializationException(CreateFailureMessage(result.ErrorCode));
        }

        return result.Value;
    }

    /// <summary>
    /// Serializes ordered cookie pairs into bounded complete Cookie field values.
    /// </summary>
    /// <param name="pairs">The pairs in output order.</param>
    /// <param name="options">The serialization and splitting options.</param>
    /// <returns>The ordered fields, or an explicit request-header limit failure.</returns>
    /// <exception cref="CookieSerializationException">A pair or configuration is invalid.</exception>
    public CookieSelectionResult SerializeCookieFields(
        CookiePair[] pairs,
        CookieSerializationOptions options)
    {
        CookieSelectionResult result = CookieRequestSerializerCore.SerializeFields(pairs, options);
        if (!result.Success && result.ErrorCode != CookieErrorCode.HeaderLimitExceeded)
        {
            throw new CookieSerializationException(CreateFailureMessage(result.ErrorCode));
        }

        return result;
    }

    internal static string CreateFailureMessage(CookieErrorCode errorCode)
    {
        return $"Cookie serialization failed with error code {(int)errorCode}.";
    }
}
