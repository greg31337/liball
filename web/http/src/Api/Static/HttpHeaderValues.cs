namespace http;

/// <summary>
/// Provides common case-normalized HTTP field-value tokens and directive names.
/// </summary>
public static class HttpHeaderValues
{
    /// <summary>
    /// The <c>close</c> connection option.
    /// </summary>
    public const string Close = "close";

    /// <summary>
    /// The <c>keep-alive</c> connection option.
    /// </summary>
    public const string KeepAlive = "keep-alive";

    /// <summary>
    /// The <c>upgrade</c> connection option.
    /// </summary>
    public const string Upgrade = "upgrade";

    /// <summary>
    /// The <c>trailers</c> transfer-coding option.
    /// </summary>
    public const string Trailers = "trailers";

    /// <summary>
    /// The <c>bytes</c> range-unit token.
    /// </summary>
    public const string Bytes = "bytes";

    /// <summary>
    /// The <c>chunked</c> transfer-coding token.
    /// </summary>
    public const string Chunked = "chunked";

    /// <summary>
    /// The <c>identity</c> content-coding token.
    /// </summary>
    public const string Identity = "identity";

    /// <summary>
    /// The <c>max-age</c> cache directive name.
    /// </summary>
    public const string MaxAge = "max-age";

    /// <summary>
    /// The <c>s-maxage</c> cache directive name.
    /// </summary>
    public const string SMaxAge = "s-maxage";

    /// <summary>
    /// The <c>max-stale</c> cache directive name.
    /// </summary>
    public const string MaxStale = "max-stale";

    /// <summary>
    /// The <c>min-fresh</c> cache directive name.
    /// </summary>
    public const string MinFresh = "min-fresh";

    /// <summary>
    /// The <c>no-cache</c> cache directive name.
    /// </summary>
    public const string NoCache = "no-cache";

    /// <summary>
    /// The <c>no-store</c> cache directive name.
    /// </summary>
    public const string NoStore = "no-store";

    /// <summary>
    /// The <c>no-transform</c> cache directive name.
    /// </summary>
    public const string NoTransform = "no-transform";

    /// <summary>
    /// The <c>only-if-cached</c> cache directive name.
    /// </summary>
    public const string OnlyIfCached = "only-if-cached";

    /// <summary>
    /// The <c>must-revalidate</c> cache directive name.
    /// </summary>
    public const string MustRevalidate = "must-revalidate";

    /// <summary>
    /// The <c>proxy-revalidate</c> cache directive name.
    /// </summary>
    public const string ProxyRevalidate = "proxy-revalidate";

    /// <summary>
    /// The <c>must-understand</c> cache directive name.
    /// </summary>
    public const string MustUnderstand = "must-understand";

    /// <summary>
    /// The <c>private</c> cache directive name.
    /// </summary>
    public const string Private = "private";

    /// <summary>
    /// The <c>public</c> cache directive name.
    /// </summary>
    public const string Public = "public";

    /// <summary>
    /// The <c>immutable</c> cache extension directive name.
    /// </summary>
    public const string Immutable = "immutable";

    /// <summary>
    /// The <c>stale-if-error</c> cache extension directive name.
    /// </summary>
    public const string StaleIfError = "stale-if-error";

    /// <summary>
    /// The <c>stale-while-revalidate</c> cache extension directive name.
    /// </summary>
    public const string StaleWhileRevalidate = "stale-while-revalidate";
}
