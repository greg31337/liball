namespace cookies;

/// <summary>
/// Stores, selects, manages, and snapshots cookies independently of a transport.
/// </summary>
public interface ICookieJar
{
    /// <summary>
    /// Processes one raw Set-Cookie field value.
    /// </summary>
    /// <param name="fieldValue">The Set-Cookie field value.</param>
    /// <param name="context">The cookie-setting context.</param>
    /// <returns>The storage outcome.</returns>
    CookieSetResult StoreSetCookieHeader(string fieldValue, CookieSetContext context);

    /// <summary>
    /// Processes independent raw Set-Cookie field values in order.
    /// </summary>
    /// <param name="fieldValues">The Set-Cookie field values.</param>
    /// <param name="context">The cookie-setting context.</param>
    /// <returns>The ordered batch outcome.</returns>
    CookieBatchSetResult StoreSetCookieHeaders(string[] fieldValues, CookieSetContext context);

    /// <summary>
    /// Processes a producer-supplied Set-Cookie model.
    /// </summary>
    /// <param name="cookie">The cookie model.</param>
    /// <param name="context">The cookie-setting context.</param>
    /// <returns>The storage outcome.</returns>
    CookieSetResult StoreSetCookieModel(SetCookie cookie, CookieSetContext context);

    /// <summary>
    /// Selects stored cookies for a request.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <returns>The ordered selection outcome.</returns>
    CookieSelectionResult SelectRequestCookies(CookieRequestContext context);

    /// <summary>
    /// Selects and serializes Cookie request fields.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <returns>The ordered selection and field values.</returns>
    CookieSelectionResult CreateRequestCookieFields(CookieRequestContext context);

    /// <summary>
    /// Queries cookies retained by the jar.
    /// </summary>
    /// <param name="query">The management query.</param>
    /// <returns>Independent matching stored cookie models.</returns>
    StoredCookie[] QueryJarCookies(CookieStoreQuery query);

    /// <summary>
    /// Removes cookies selected by management filters.
    /// </summary>
    /// <param name="query">The removal query.</param>
    /// <returns>The number of cookies removed.</returns>
    int RemoveJarCookies(CookieRemovalQuery query);

    /// <summary>
    /// Removes cookies expired at the injected clock instant.
    /// </summary>
    /// <returns>The number of cookies removed.</returns>
    int PurgeExpiredCookies();

    /// <summary>
    /// Captures a lossless cookie-jar snapshot.
    /// </summary>
    /// <returns>The independent snapshot model.</returns>
    CookieJarSnapshot ExportJarSnapshot();

    /// <summary>
    /// Imports a validated cookie-jar snapshot.
    /// </summary>
    /// <param name="snapshot">The snapshot to import.</param>
    /// <param name="mode">The merge or replacement behavior.</param>
    /// <returns>The import outcome.</returns>
    CookieImportResult ImportJarSnapshot(CookieJarSnapshot snapshot, CookieImportMode mode);
}
