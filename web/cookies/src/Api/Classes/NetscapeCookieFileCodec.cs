namespace cookies;

/// <summary>
/// Encodes and decodes Netscape, Mozilla, and curl cookie-file text.
/// </summary>
public class NetscapeCookieFileCodec : INetscapeCookieFileCodec
{
    internal ICookieClock Clock;

    /// <summary>
    /// Creates a cookie-file codec using the supplied deterministic UTC clock.
    /// </summary>
    /// <param name="clock">The clock used to classify imported expiry times.</param>
    /// <exception cref="CookieSerializationException">The required clock is absent.</exception>
    public NetscapeCookieFileCodec(ICookieClock clock)
    {
        if (clock == null)
        {
            throw new CookieSerializationException(
                CookieServiceConfigurationValidator.CreateFailureMessage());
        }

        Clock = clock;
    }

    /// <summary>
    /// Serializes stored cookies to Netscape cookie-file text.
    /// </summary>
    /// <param name="cookies">The stored cookies to serialize.</param>
    /// <param name="options">The lossy export options.</param>
    /// <returns>The serialized cookie-file text.</returns>
    /// <exception cref="CookieSerializationException">A cookie cannot be represented safely.</exception>
    public string SerializeNetscapeCookieFile(
        StoredCookie[] cookies,
        NetscapeCookieFileOptions options)
    {
        CookieStringSerializationResult result =
            NetscapeCookieFileWriter.WriteFile(cookies, options, Clock);
        if (!result.Success)
        {
            throw new CookieSerializationException(
                CookieSerializer.CreateFailureMessage(result.ErrorCode));
        }

        return result.Value;
    }

    /// <summary>
    /// Parses Netscape cookie-file text or throws for malformed input.
    /// </summary>
    /// <param name="text">The cookie-file text.</param>
    /// <param name="options">The import options.</param>
    /// <returns>The parsed cookie entries.</returns>
    /// <exception cref="CookieParseException">The cookie-file text contains malformed input.</exception>
    public StoredCookie[] ParseNetscapeCookieFile(
        string text,
        NetscapeCookieFileOptions options)
    {
        NetscapeCookieFileParseResult result =
            TryParseNetscapeCookieFile(text, options);
        if (!result.Success)
        {
            throw new CookieParseException(
                CookieParser.CreateFailureMessage(result.ErrorCode));
        }

        return CookieGraphCloner.CloneStoredCookies(result.Cookies);
    }

    /// <summary>
    /// Parses Netscape cookie-file text without throwing for malformed input.
    /// </summary>
    /// <param name="text">The cookie-file text.</param>
    /// <param name="options">The import options.</param>
    /// <returns>The cookie-file parse result.</returns>
    public NetscapeCookieFileParseResult TryParseNetscapeCookieFile(
        string text,
        NetscapeCookieFileOptions options)
    {
        return CookieGraphCloner.CloneNetscapeParseResult(
            NetscapeCookieFileReader.ReadFile(text, options, Clock));
    }
}
