namespace http;

/// <summary>
/// Provides coding names from the IANA HTTP Transfer Coding Registry snapshot.
/// </summary>
public static class HttpTransferCodings
{
    /// <summary>
    /// The registered <c>chunked</c> transfer-coding token.
    /// </summary>
    public const string Chunked = "chunked";

    /// <summary>
    /// The registered <c>compress</c> transfer-coding token.
    /// </summary>
    public const string Compress = "compress";

    /// <summary>
    /// The registered <c>deflate</c> transfer-coding token.
    /// </summary>
    public const string Deflate = "deflate";

    /// <summary>
    /// The registered <c>gzip</c> transfer-coding token.
    /// </summary>
    public const string Gzip = "gzip";

    /// <summary>
    /// The registered <c>identity</c> transfer-coding token.
    /// </summary>
    public const string Identity = "identity";

    /// <summary>
    /// The registered <c>trailers</c> transfer-coding token.
    /// </summary>
    public const string Trailers = "trailers";

    /// <summary>
    /// The registered <c>x-compress</c> transfer-coding token.
    /// </summary>
    public const string XCompress = "x-compress";

    /// <summary>
    /// The registered <c>x-gzip</c> transfer-coding token.
    /// </summary>
    public const string XGzip = "x-gzip";
}
