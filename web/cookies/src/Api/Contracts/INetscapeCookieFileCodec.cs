namespace cookies;

/// <summary>
/// Encodes and decodes Netscape, Mozilla, and curl cookie-file text.
/// </summary>
public interface INetscapeCookieFileCodec
{
    /// <summary>
    /// Serializes stored cookies to Netscape cookie-file text.
    /// </summary>
    /// <param name="cookies">The stored cookies to serialize.</param>
    /// <param name="options">The lossy export options.</param>
    /// <returns>The serialized cookie-file text.</returns>
    /// <exception cref="CookieSerializationException">A cookie cannot be represented safely.</exception>
    string SerializeNetscapeCookieFile(StoredCookie[] cookies, NetscapeCookieFileOptions options);

    /// <summary>
    /// Parses Netscape cookie-file text or throws for malformed input.
    /// </summary>
    /// <param name="text">The cookie-file text.</param>
    /// <param name="options">The import options.</param>
    /// <returns>The parsed cookie entries.</returns>
    /// <exception cref="CookieParseException">The cookie-file text contains malformed input.</exception>
    StoredCookie[] ParseNetscapeCookieFile(string text, NetscapeCookieFileOptions options);

    /// <summary>
    /// Parses Netscape cookie-file text without throwing for malformed input.
    /// </summary>
    /// <param name="text">The cookie-file text.</param>
    /// <param name="options">The import options.</param>
    /// <returns>The cookie-file parse result.</returns>
    NetscapeCookieFileParseResult TryParseNetscapeCookieFile(string text, NetscapeCookieFileOptions options);
}
